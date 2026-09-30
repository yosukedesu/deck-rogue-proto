#!/usr/bin/env bash
# scripts/typecheck-unity/run.sh — Unity を起動せずに unity/Assets の C# を型検査する (2026-09-30 HD-2D 見本 P00)。
#
# Unity の作業コピーは1つしか無いので、並列で作業するレーンは Unity でコンパイルできない。代わりにこれを回して
# 自分の変更の型が通るかを確かめる (compile・build・撮影は波の終わりに統合担当がまとめて行う)。
#
#   scripts/typecheck-unity/run.sh            # runtime → editor → player (Windows・Android の定義) の順に検査
#   scripts/typecheck-unity/run.sh --quick    # runtime だけ (エディタの定義。いちばん速い)
#   scripts/typecheck-unity/run.sh --grep HD2DFlags   # 表示するエラーを絞る (数は全部を数える)
#   scripts/typecheck-unity/run.sh --warn     # 警告 (CS0618 の古い API など) の行も出す。警告では落ちない (Unity と同じ)
#
# 検査の中身
#   runtime        = Assembly-CSharp 相当 (Assets の Editor 以外)。定義は Unity のエディタと同じ (UNITY_EDITOR つき)
#   editor         = Assembly-CSharp-Editor 相当 (Assets/**/Editor)。いま作った runtime を参照する
#   player-win     = runtime をプレイヤー (Windows) の定義で。#if UNITY_EDITOR の外でエディタ専用 API を使った漏れを拾う
#   player-android = 同じく Android の定義で
# 参照 (Unity の Managed の DLL・パッケージの DLL・エンジンの DLL) と定義は、Unity の作業コピー
# (WIN_DIR、既定 /mnt/d/deck-rogue/unity-batch) が最後のコンパイルで書いた応答ファイル
# Library/Bee/artifacts/<dag>/Assembly-CSharp*.rsp から gen-props.py が取る (Unity と同じ入力)。
# 作業コピーが無い・一度もコンパイルしていない時は、統合担当に scripts/unity-win.sh compile を頼む。
#
# 注意
#   - 同じ作業ツリーで他のレーンが書きかけのファイルのエラーも出る。自分のファイルのエラーだけを見る (--grep)。
#   - 検査するのはリポジトリの unity/Assets (作業コピーではない)。エンジン (Packages/com.deckrogue.engine) は
#     作業コピーでコンパイル済みの DLL を参照する (レーンはエンジンを触らない)。
#   - シェーダは検査しない (P03 の HD2DShaderCheck)。
#   - 出力は毎回の一時フォルダ。同時に何本走らせてもぶつからない。
# 終了コード: 0 = エラー0 / 1 = エラーあり / 2 = 準備の失敗
set -u
HERE="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$HERE/../.." && pwd)"
WIN_DIR="${WIN_DIR:-/mnt/d/deck-rogue/unity-batch}"
UNITY_SRC="${UNITY_SRC:-$REPO/unity/}"   # 検査する unity/ (末尾の / まで。試験用に差し替えられる)
DOTNET="${DOTNET:-$(command -v dotnet 2>/dev/null || echo "$HOME/.dotnet/dotnet")}"
QUICK=0; GREP=""; WARN=0
while [ $# -gt 0 ]; do
  case "$1" in
    --quick) QUICK=1; shift ;;
    --warn) WARN=1; shift ;;
    --grep) GREP="${2:-}"; shift 2 ;;
    -h|--help) sed -n '2,30p' "$0"; exit 0 ;;
    *) echo "不明な引数: $1"; exit 2 ;;
  esac
done
[ -x "$DOTNET" ] || { echo "dotnet が無い ($DOTNET)"; exit 2; }
ART="$WIN_DIR/Library/Bee/artifacts"
[ -d "$ART" ] || { echo "Unity の作業コピーがコンパイルされていない: $ART (統合担当に scripts/unity-win.sh compile を頼む)"; exit 2; }

# いちばん新しくコンパイルされた dag を選ぶ (E=エディタ・P=プレイヤー)。プレイヤーは定義で Windows / Android を見分ける
newest_dag() {   # $1 = E|P  $2 = 定義の目印 (空なら何でも)
  local best="" bt=0 d t
  for d in "$ART"/*"$1".dag; do
    [ -f "$d/Assembly-CSharp.rsp" ] || continue
    if [ -n "$2" ] && ! grep -q -- "-define:$2\$" "$d/Assembly-CSharp.rsp"; then continue; fi
    t=$(stat -c %Y "$d/Assembly-CSharp.ref.dll" 2>/dev/null || stat -c %Y "$d/Assembly-CSharp.rsp")
    if [ "$t" -gt "$bt" ]; then bt=$t; best="$d"; fi
  done
  echo "$best"
}
EDAG="$(newest_dag E "")"
[ -n "$EDAG" ] || { echo "エディタの応答ファイルが無い ($ART/*E.dag/Assembly-CSharp.rsp)"; exit 2; }

OUT="$(mktemp -d "${TMPDIR:-/tmp}/typecheck-unity.XXXXXX")"
trap 'rm -rf "$OUT"' EXIT
TOTAL_ERR=0
T0=$(date +%s)

check() {   # $1 = pass  $2 = rsp  以降 = gen-props.py への追加の引数
  local pass="$1" rsp="$2"; shift 2
  local props="$OUT/$pass.props" log="$OUT/$pass.log"
  if ! python3 "$HERE/gen-props.py" "$rsp" "$props" "$WIN_DIR" "$@" > "$OUT/$pass.gen" 2>&1; then
    echo "[$pass] 参照の準備に失敗:"; cat "$OUT/$pass.gen"; TOTAL_ERR=$((TOTAL_ERR + 1)); return
  fi
  "$DOTNET" build "$HERE/TypeCheck.csproj" -nologo -v:q -clp:NoSummary \
    -p:TcPass="$pass" -p:TcProps="$props" -p:UnityDir="$UNITY_SRC" \
    -p:BaseIntermediateOutputPath="$OUT/obj/$pass/" -p:OutputPath="$OUT/bin/$pass/" -p:IntermediateOutputPath="$OUT/obj/$pass/i/" \
    > "$log" 2>&1
  local rc=$?
  # エラーの行 (同じ行が重複して出るので一意に)。パスはリポジトリからの相対に
  local errs; errs=$(grep -E "error (CS|MSB|NU)[0-9]+" "$log" | sed -E "s#\[[^]]*TypeCheck\.csproj[^]]*\]##; s#$REPO/##g" | sort -u)
  local nerr; nerr=$(printf "%s" "$errs" | grep -c . )
  local warns; warns=$(grep -E "warning CS[0-9]+" "$log" | sed -E "s#\[[^]]*TypeCheck\.csproj[^]]*\]##; s#$REPO/##g" | sort -u)
  local nwarn; nwarn=$(printf "%s" "$warns" | grep -c .)
  if [ "$nerr" -eq 0 ] && [ "$rc" -ne 0 ]; then
    echo "[$pass] ビルドが失敗したがエラーの行が無い (exit $rc):"; tail -20 "$log"; TOTAL_ERR=$((TOTAL_ERR + 1)); return
  fi
  TOTAL_ERR=$((TOTAL_ERR + nerr))
  echo "[$pass] エラー $nerr・警告 $nwarn  ($(basename "$(dirname "$rsp")")/$(basename "$rsp"))"
  if [ "$nerr" -gt 0 ]; then
    if [ -n "$GREP" ]; then printf "%s\n" "$errs" | grep -- "$GREP" | head -60; else printf "%s\n" "$errs" | head -60; fi
  fi
  if [ "$WARN" = 1 ] && [ "$nwarn" -gt 0 ]; then
    if [ -n "$GREP" ]; then printf "%s\n" "$warns" | grep -- "$GREP" | head -40; else printf "%s\n" "$warns" | head -40; fi
  fi
}

check runtime "$EDAG/Assembly-CSharp.rsp" --drop Assembly-CSharp --drop Assembly-CSharp-Editor
if [ "$QUICK" = 0 ]; then
  if [ -f "$OUT/bin/runtime/Assembly-CSharp.dll" ] && [ -f "$EDAG/Assembly-CSharp-Editor.rsp" ]; then
    check editor "$EDAG/Assembly-CSharp-Editor.rsp" --drop Assembly-CSharp --drop Assembly-CSharp-Editor --add-ref "$OUT/bin/runtime/Assembly-CSharp.dll"
  else
    echo "[editor] 飛ばした (runtime が通らなかった)"
  fi
  PW="$(newest_dag P UNITY_STANDALONE_WIN)"; PA="$(newest_dag P UNITY_ANDROID)"
  [ -n "$PW" ] && check player-win "$PW/Assembly-CSharp.rsp" --drop Assembly-CSharp --drop Assembly-CSharp-Editor || echo "[player-win] 応答ファイルが無いので飛ばした"
  [ -n "$PA" ] && check player-android "$PA/Assembly-CSharp.rsp" --drop Assembly-CSharp --drop Assembly-CSharp-Editor || echo "[player-android] 応答ファイルが無いので飛ばした"
fi
echo "型検査: エラー $TOTAL_ERR ($(( $(date +%s) - T0 ))秒)"
[ "$TOTAL_ERR" -eq 0 ] && exit 0 || exit 1
