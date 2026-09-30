#!/usr/bin/env bash
# scripts/hd2d-clip.sh — 動く見比べ (2026-09-30 P08。計画 docs/design/hd2d-slice-plan-2026-09-30.md §6)。
# det のまま連番で撮り (hd2d-shots.sh・1行1起動)、行ごとに動画にする。ffmpeg があれば mp4、無ければ PIL でアニメ PNG。
# 本家は動画を置かない (公式トレーラーの戦闘場面のリンクと時刻だけを比較シートに添える = 著作物のため)。
#
#   scripts/hd2d-clip.sh <出力名> [--list scripts/hd2d-states/clips.txt] [--phone-tier] [--only 正規表現] [--skip N] [--no-shoot]
#     出力名     unity/Shots/hd2d/<出力名>/ (撮った連番) と <出力名>/clips/<行>.mp4 (か .png = アニメ PNG)
#     --list     撮る一覧 (既定 clips.txt = S-wolf の待機と R1 を約4秒 = 80 枚 × 3 フレーム。見本の行つき)
#     --skip N   先頭の N 枚を捨てる (待機の行は登場の撮影キー entershots で撮るので、最初の約 0.7 秒 = 14 枚は登場)
#     --no-shoot 撮らずに、もうある連番から動画だけ作る
#   コマの速さ = 60 ÷ (playevery か enterevery か endevery か fireevery。無ければ 3) コマ/秒 (det は 1 フレーム = 1/60 秒)
set -u
REPO="$(cd "$(dirname "$0")/.." && pwd)"
NAME="${1:-}"
if [ -z "$NAME" ]; then sed -n '2,13p' "$0"; exit 2; fi
shift
LIST="$REPO/scripts/hd2d-states/clips.txt"; PASS=(); SKIP=0; SHOOT=1; ONLY=""
while [ $# -gt 0 ]; do
  case "$1" in
    --list) LIST="$2"; shift 2 ;;
    --phone-tier|--phone) PASS+=("$1"); shift ;;
    --only) ONLY="$2"; PASS+=(--only "$2"); shift 2 ;;
    --skip) SKIP="$2"; shift 2 ;;
    --no-shoot) SHOOT=0; shift ;;
    *) echo "不明な引数: $1"; exit 2 ;;
  esac
done
case "$NAME" in /*) OUT="$NAME" ;; *) OUT="$REPO/unity/Shots/hd2d/$NAME" ;; esac
if [ "$SHOOT" = 1 ]; then
  bash "$REPO/scripts/hd2d-shots.sh" "$LIST" "$OUT" "${PASS[@]}" || echo "撮れなかった行がある (_status.txt)"
fi
mkdir -p "$OUT/clips"
python3 - "$LIST" "$OUT" "$SKIP" "$ONLY" <<'PY'
import sys, os, re, glob, shutil, subprocess
from PIL import Image
lst, out, skip, only = sys.argv[1], sys.argv[2], int(sys.argv[3]), sys.argv[4]
ffmpeg = shutil.which('ffmpeg')
rows = []
for line in open(lst, encoding='utf-8'):
    line = line.strip()
    if not line or line.startswith('#') or '|' not in line:
        continue
    n, st = line.split('|', 1)
    if only and not re.search(only, n):
        continue
    m = re.search(r'(?:playevery|enterevery|endevery|fireevery)=(\d+)', st)
    rows.append((n, 60.0 / int(m.group(1)) if m else 20.0))
for n, fps in rows:
    for dev in ('PC', 'PH'):
        frames = sorted(glob.glob(os.path.join(out, '%s-%s-*.png' % (dev, n))), key=lambda p: int(re.search(r'-(\d+)\.png$', p).group(1)))
        frames = [f for f in frames if re.search(r'-%s-\d+\.png$' % re.escape(n), f)][skip:]
        if len(frames) < 2:
            continue
        base = os.path.join(out, 'clips', '%s-%s' % (dev, n))
        if ffmpeg:
            lst_path = base + '.frames.txt'
            with open(lst_path, 'w') as f:
                for p in frames:
                    f.write("file '%s'\nduration %.5f\n" % (os.path.abspath(p), 1.0 / fps))
                f.write("file '%s'\n" % os.path.abspath(frames[-1]))
            cmd = [ffmpeg, '-y', '-loglevel', 'error', '-f', 'concat', '-safe', '0', '-i', lst_path,
                   '-vf', 'scale=trunc(iw/2)*2:trunc(ih/2)*2', '-r', '%g' % fps, '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '16', base + '.mp4']
            r = subprocess.run(cmd)
            os.remove(lst_path)
            print('%s.mp4 (%d 枚・%.0f コマ/秒) %s' % (base, len(frames), fps, 'ok' if r.returncode == 0 else 'ffmpeg 失敗'))
        else:
            ims = [Image.open(p).convert('RGB') for p in frames]
            ims[0].save(base + '.png', save_all=True, append_images=ims[1:], duration=int(1000 / fps), loop=0)
            print('%s.png (アニメ PNG・%d 枚・%.0f コマ/秒)' % (base, len(frames), fps))
PY
