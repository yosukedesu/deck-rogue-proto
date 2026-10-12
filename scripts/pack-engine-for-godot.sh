#!/usr/bin/env bash
# scripts/pack-engine-for-godot.sh — Godot 移植（dot・クラウド）へ渡す「エンジンの包み」を1つのコミットから作る。
#
# 使い方: [SELFTEST=0] [EXTRA_JOURNALS="a.json b.json"] scripts/pack-engine-for-godot.sh [<commit>=HEAD] [<出力先>=../deck-rogue-godot-sync]
#   出力: <出力先>/<短いSHA>/ と同名の .zip
#   中身: C# エンジン（Runtime 全部）・データ JSON・ゴールデン（runs＋events）・TS エンジン（正）と sim・生成と照合のスクリプト・
#         EngineTests・package.json／lock／tsconfig（同じ依存関係で再現するため）・基準 25270c9 からの patch・
#         出来事の実例 events-sample.json（HEAD が commit と同じで作業ツリーが綺麗な時）・MANIFEST.md・SHA256SUMS
#   ファイルは作業ツリーでなく <commit> の中身を git show で取り出す＝コミット ID と中身が必ず一致する。
#   SELFTEST=1（既定）: 展開した包みだけで npm ci → gen:csharp（Types.g.cs が同一か）→ goldens:verify → tsc（sim）→ C# verify を通し、
#   結果を MANIFEST.md と selftest.log に書く（元の作業ツリーにしか無いファイルへ依存していないことの確認＝dot の返信 4）。
#
# 運用（2026-10-12 裁定「Godot へ一本化」）: TS が正 → C# へ翻訳し verify → この包みを dot へ → Godot で再照合。
set -euo pipefail
shopt -s nullglob

cd "$(git rev-parse --show-toplevel)"
REPO="$PWD"
COMMIT="${1:-HEAD}"
OUT_ROOT="${2:-../deck-rogue-godot-sync}"
BASE="25270c9c511a2ceb4dff4806cf9deb7c50c78709"   # dot の既知 main（2026-10-04）
FULL="$(git rev-parse "$COMMIT")"
SHORT="$(git rev-parse --short "$COMMIT")"
mkdir -p "$OUT_ROOT"
OUT_ROOT="$(cd "$OUT_ROOT" && pwd)"
OUT="$OUT_ROOT/$SHORT"
rm -rf "$OUT"
mkdir -p "$OUT"
DOTNET="${DOTNET:-$(command -v dotnet || echo "$HOME/.dotnet/dotnet")}"

# 包みに入れる場所（コミットの中のパスのまま）
PATHS=(
  "unity/Packages/com.deckrogue.engine/Runtime"   # C# エンジン 25 ファイル（Reactions/・Generated/ を含む）
  "unity/Packages/com.deckrogue.engine/package.json"
  "src/data"                                       # JSON 14 ファイル
  "goldens/runs"                                   # ゴールデン 13 本
  "goldens/events"                                 # 通常のゴールデンとは別＝出来事の実例のジャーナル（CascadeRetargeted／ImpulseKept）
  "src/engine"                                     # TS エンジン（正）。テストも含める＝仕様書
  "src/sim"                                        # ボット・ゴールデンの生成と照合（verify-goldens が golden-driver を読む）・event-scan
  "src/ui/log.ts"                                  # src/sim/play.ts が読む（tsconfig.sim の型チェックに要る）
  "src/ui/vocab.ts"
  "scripts/gen-csharp-types.ts"                    # types.ts・run.ts・map.ts → Types.g.cs
  "scripts/dump-goldens.ts"
  "scripts/verify-goldens.ts"
  "scripts/dump-events.ts"                         # 新しい出来事の実例（events-sample.json）を同じコミットから出す
  "scripts/find-event-journals.ts"                 # 出来事が出るジャーナルをボットで探して goldens/events に書く
  "scripts/check-unity-colors.ts"                  # src/sim/unity-colors.test が読む（型チェック用。テスト自体は unity/Assets が無いので包みでは落ちる）
  "unity/EngineTests"                              # dotnet run -- verify
  "unity/PORTING.md"
  "docs/unity-port.md"                             # src/engine/run.test が読む
  "package.json"                                   # dot の依頼（返信 3）: 型の再生成と TS の検証を同じ依存関係で
  "package-lock.json"
  "tsconfig.json"
  "tsconfig.app.json"
  "tsconfig.node.json"
  "tsconfig.sim.json"
  "vite.config.ts"
)

n=0
while IFS= read -r f; do
  mkdir -p "$OUT/$(dirname "$f")"
  git show "$FULL:$f" > "$OUT/$f"
  n=$((n+1))
done < <(git ls-tree -r --name-only "$FULL" -- "${PATHS[@]}")

git diff "$BASE" "$FULL" -- "${PATHS[@]}" > "$OUT/from-25270c9.patch"

# 新しい出来事の実例（dot の返信 3）: 作業ツリーが <commit> と同じ時だけ、同じエンジンでゴールデンを再生して出す
EVENTS_NOTE="events-sample.json は作っていない（作業ツリーが commit と違う。HEAD を commit に合わせて npx tsx scripts/dump-events.ts --out events-sample.json を実行）"
if [ "$(git rev-parse HEAD)" = "$FULL" ] && [ -z "$(git status --porcelain -- src/engine src/sim src/data goldens scripts/dump-events.ts)" ]; then
  npx tsx scripts/dump-events.ts --label golden --out "$OUT/events-sample.json" goldens/runs/*.json goldens/events/*.json 2> "$OUT/events-sample.summary.txt"
  EVENTS_NOTE="events-sample.json = goldens/runs $(ls goldens/runs/*.json | wc -l) 本＋goldens/events $(ls goldens/events/*.json 2>/dev/null | wc -l) 本を同じエンジンで再生した7種の出来事の実例（要約 events-sample.summary.txt）"
  if [ -n "${EXTRA_JOURNALS:-}" ]; then
    # コミットの外にあるジャーナル（Opus のラン等）も同じエンジンで再生して別ファイルに
    npx tsx scripts/dump-events.ts --label "extra: コミット外のジャーナル（同じエンジンで再生）" --out "$OUT/events-sample-extra.json" $EXTRA_JOURNALS 2> "$OUT/events-sample-extra.summary.txt"
    EVENTS_NOTE="$EVENTS_NOTE／events-sample-extra.json = コミット外のジャーナル（$EXTRA_JOURNALS）の実例"
  fi
fi

# 自己検証: 展開した包みだけで再現できるか（dot の返信 4）
ST_LINES=()
if [ "${SELFTEST:-1}" = "1" ]; then
  LOG="$OUT/selftest.log"
  : > "$LOG"
  step() { # 名前 コマンド...
    local name="$1"; shift
    if ( cd "$OUT" && "$@" ) >> "$LOG" 2>&1; then ST_LINES+=("  - $name: OK"); else ST_LINES+=("  - $name: **NG**（selftest.log）"); fi
  }
  step "npm ci（package-lock.json と同じ依存関係）" npm ci --no-audit --no-fund --loglevel=error
  TYPES="unity/Packages/com.deckrogue.engine/Runtime/Generated/Types.g.cs"
  cp "$OUT/$TYPES" "$OUT/Types.g.cs.shipped"
  step "npm run gen:csharp（types.ts・run.ts・map.ts から生成）" npm run -s gen:csharp
  if cmp -s "$OUT/$TYPES" "$OUT/Types.g.cs.shipped"; then ST_LINES+=("  - 生成した Types.g.cs は同梱の物と同一: OK"); else ST_LINES+=("  - 生成した Types.g.cs が同梱の物と**違う**"); fi
  rm -f "$OUT/Types.g.cs.shipped"
  step "TS ゴールデン照合（goldens/runs＋goldens/events）" npx tsx scripts/verify-goldens.ts $(cd "$OUT" && ls goldens/runs/*.json goldens/events/*.json 2>/dev/null)
  step "tsc（tsconfig.sim＝engine＋sim＋data の型）" npx tsc --noEmit -p tsconfig.sim.json
  step "C# ゴールデン照合（dotnet run -- verify）" bash -c "cd unity/EngineTests && '$DOTNET' run -- verify \$(ls ../../goldens/runs/*.json ../../goldens/events/*.json 2>/dev/null) --data ../../src/data"
  rm -rf "$OUT/node_modules" "$OUT/unity/EngineTests/bin" "$OUT/unity/EngineTests/obj"
else
  ST_LINES+=("  - 省略（SELFTEST=0）")
fi

cs=$(find "$OUT/unity/Packages/com.deckrogue.engine/Runtime" -name '*.cs' | wc -l)
js=$(find "$OUT/src/data" -name '*.json' | wc -l)
gd=$(find "$OUT/goldens/runs" -name '*.json' | wc -l)
ge=0; [ -d "$OUT/goldens/events" ] && ge=$(find "$OUT/goldens/events" -name '*.json' | wc -l)
steps=$(node -e '
  const fs=require("fs"),p=process.argv[1];let t=0;
  for(const f of fs.readdirSync(p)){const j=JSON.parse(fs.readFileSync(p+"/"+f,"utf8"));t+=(j.commands||[]).length}
  console.log(t)' "$OUT/goldens/runs")

{
  echo "# Godot 同期の包み"
  echo
  echo "- commit: \`$FULL\`（$(git log -1 --format='%ad %s' --date=short "$FULL" | cut -c1-120)）"
  echo "- branch: $(git branch --contains "$FULL" --format='%(refname:short)' | tr '\n' ' ')"
  echo "- base: \`$BASE\`（from-25270c9.patch は base → commit。上のパスに限る）"
  echo "- 作成: $(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo "- C# エンジン: $cs ファイル／データ JSON: $js／ゴールデン: goldens/runs $gd 本・合計 $steps 手＋goldens/events $ge 本（出来事の実例用・通常のゴールデンとは別）／ファイル総数: $n"
  echo "- 出来事の実例: $EVENTS_NOTE"
  echo "- 同梱の依存: package.json・package-lock.json・tsconfig×4・vite.config.ts（\`npm ci\` → \`npm run gen:csharp\`・\`npm run goldens:verify\`・\`npx tsc --noEmit -p tsconfig.sim.json\` が同じ依存関係で再現できる。\`npm run typecheck\`（tsc -b）は src/ui が無いので包みでは通らない）"
  echo "- 展開した包みだけでの自己検証（作業ツリーに依存していないことの確認）:"
  printf '%s\n' "${ST_LINES[@]}"
  echo
  echo "## 手順（dot）"
  echo "1. \`unity/Packages/com.deckrogue.engine/Runtime\` と \`src/data\` を Godot へ複製する"
  echo "2. \`Generated/Types.g.cs\` は手で編集しない。必要なら \`npm run gen:csharp\`（scripts/gen-csharp-types.ts が src/engine/types.ts・run.ts・map.ts を読む）で生成し直す"
  echo "3. Godot でゴールデン $gd 本（＋events $ge 本）を照合する（\`unity/EngineTests\` の verify と同じ判定）"
  echo "4. 中身は SHA256SUMS で確かめてから"
  echo "5. 包みで落ちるテスト: src/sim/unity-colors.test・unity-vocab.test（unity/Assets/Game が無い）。Godot には関係ない"
} > "$OUT/MANIFEST.md"

( cd "$OUT" && find . -type f ! -name SHA256SUMS -print0 | sort -z | xargs -0 sha256sum > SHA256SUMS )

if command -v zip >/dev/null 2>&1; then
  ( cd "$OUT_ROOT" && rm -f "$SHORT.zip" && zip -qr "$SHORT.zip" "$SHORT" )
  echo "zip: $OUT_ROOT/$SHORT.zip ($(du -h "$OUT_ROOT/$SHORT.zip" | cut -f1))"
else
  ( cd "$OUT_ROOT" && tar -czf "$SHORT.tar.gz" "$SHORT" )
  echo "tar: $OUT_ROOT/$SHORT.tar.gz"
fi
echo "dir: $OUT"
sed -n '1,20p' "$OUT/MANIFEST.md"
