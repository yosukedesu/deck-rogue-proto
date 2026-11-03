#!/usr/bin/env python3
"""連写 (<prefix>-k.png) を k の順に並べたシート (2026-11-04 見た目・UX・演出の磨き 束 F)。

1 束 16 行×10 コマを 1 枚ずつ Read すると輪が止まるので、連続のコマを横に並べて 1 枚で見る (大手の「8 コマを並べて見る」)。PIL だけ。

  python3 scripts/juice-sheet.py <dir> <prefix> <out.png> [--cols N] [--width W]
      <dir> の <prefix>-k.png (k は 0 から。J-outcome-0.png …。行の名前そのもの J-map-1.png も prefix=J-map で拾う) を k の順に並べる。
      --cols 1 行に並べる数 (既定 6)・--width 1 コマの幅 px (既定 384)。各コマの上に名前の札 (ファイル名と k)。
  python3 scripts/juice-sheet.py --pairs <before_dir> <after_dir> <out.png> [--width W] [--prefix P]
      同じ名前の PNG を 左=before・右=after で並べる (名前の札つき)。--prefix で絞る (省略は両方にある全部)。
  python3 scripts/juice-sheet.py --all <dir> <out_dir> [--cols N] [--width W]
      <dir> の全ての行 (名前-k.png の名前ごと) をそれぞれ <out_dir>/<名前>.png に。

名前の規約は Autopilot の Shot: states の行は「行名-k.png」(k は 1 から)、連写は「行名-<口>-i.png」(i は 0 から) …
だが -statesfile の中では Shot が行名の後ろに連番を付けるので実際は「行名-k.png」だけになる。どちらでも末尾の数字で並べる。
"""
import argparse
import os
import re
import sys

try:
    from PIL import Image, ImageDraw, ImageFont
except ImportError:
    print("PIL (pillow) が要る: pip install pillow", file=sys.stderr)
    sys.exit(2)

NUM_RE = re.compile(r"^(.*?)-(\d+)\.png$")
LABEL_H = 22
PAD = 6
BG = (26, 28, 51)        # 夜色の下地 (紙の UI の色ではないので規約の対象外。シートは資料)
LABEL_BG = (15, 17, 32)
INK = (244, 236, 214)


def font(size=13):
    for cand in (
        "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc",
        "/usr/share/fonts/truetype/noto/NotoSansCJK-Regular.ttc",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
    ):
        if os.path.exists(cand):
            try:
                return ImageFont.truetype(cand, size)
            except Exception:
                pass
    return ImageFont.load_default()


def list_frames(d, prefix):
    """<prefix>-k.png を k の順に。prefix が無ければ名前ごとに辞書で返す"""
    out = []
    for f in os.listdir(d):
        m = NUM_RE.match(f)
        if not m:
            continue
        name, k = m.group(1), int(m.group(2))
        if prefix is not None and name != prefix:
            continue
        out.append((name, k, os.path.join(d, f)))
    out.sort(key=lambda t: (t[0], t[1]))
    return out


def group_by_name(frames):
    groups = {}
    for name, k, p in frames:
        groups.setdefault(name, []).append((k, p))
    return groups


def thumb(path, width):
    im = Image.open(path).convert("RGB")
    h = max(1, round(im.height * width / im.width))
    return im.resize((width, h), Image.LANCZOS)


def sheet(frames, cols, width, title=None):
    """frames: [(label, path)] → PIL Image。cols 列に折り返し、各コマの上に名前の札"""
    if not frames:
        return None
    thumbs = [(lab, thumb(p, width)) for lab, p in frames]
    th = max(t.height for _, t in thumbs)
    cell_w, cell_h = width + PAD, th + LABEL_H + PAD
    rows = (len(thumbs) + cols - 1) // cols
    top = LABEL_H + PAD if title else PAD
    im = Image.new("RGB", (cols * cell_w + PAD, rows * cell_h + top), BG)
    dr = ImageDraw.Draw(im)
    f = font(13)
    if title:
        dr.text((PAD, 4), title, fill=INK, font=f)
    for i, (lab, t) in enumerate(thumbs):
        r, c = divmod(i, cols)
        x, y = PAD + c * cell_w, top + r * cell_h
        dr.rectangle([x, y, x + width, y + LABEL_H - 2], fill=LABEL_BG)
        dr.text((x + 4, y + 3), lab, fill=INK, font=f)
        im.paste(t, (x, y + LABEL_H))
    return im


def cmd_single(args):
    frames = list_frames(args.dir, args.prefix)
    if not frames:
        print("無い: " + os.path.join(args.dir, args.prefix + "-*.png"), file=sys.stderr)
        return 1
    labels = [("%s-%d" % (n, k), p) for n, k, p in frames]
    im = sheet(labels, args.cols, args.width, title=args.prefix + "  (%d コマ)" % len(labels))
    os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
    im.save(args.out)
    print("%s  %dx%d  %d コマ" % (args.out, im.width, im.height, len(labels)))
    return 0


def cmd_all(args):
    groups = group_by_name(list_frames(args.dir, None))
    if not groups:
        print("無い: " + args.dir, file=sys.stderr)
        return 1
    os.makedirs(args.out_dir, exist_ok=True)
    for name, lst in sorted(groups.items()):
        labels = [("%s-%d" % (name, k), p) for k, p in lst]
        im = sheet(labels, args.cols, args.width, title=name + "  (%d コマ)" % len(labels))
        out = os.path.join(args.out_dir, name + ".png")
        im.save(out)
        print("%s  %d コマ" % (out, len(labels)))
    return 0


def cmd_pairs(args):
    a = group_by_name(list_frames(args.before, None))
    b = group_by_name(list_frames(args.after, None))
    names = sorted(set(a) & set(b))
    if args.prefix:
        names = [n for n in names if n == args.prefix or n.startswith(args.prefix + "-")]
    if not names:
        print("両方にある名前が無い", file=sys.stderr)
        return 1
    rows = []   # (label, before_path|None, after_path|None)
    for n in names:
        da, db = dict(a[n]), dict(b[n])
        for k in sorted(set(da) | set(db)):
            rows.append(("%s-%d" % (n, k), da.get(k), db.get(k)))
    width = args.width
    f = font(13)
    th_rows = []
    for lab, pa, pb in rows:
        ta = thumb(pa, width) if pa else None
        tb = thumb(pb, width) if pb else None
        th_rows.append((lab, ta, tb))
    cell_h_list = [max((t.height for t in (ta, tb) if t is not None), default=100) for _, ta, tb in th_rows]
    total_h = LABEL_H + PAD + sum(h + LABEL_H + PAD for h in cell_h_list)
    im = Image.new("RGB", (2 * (width + PAD) + PAD, total_h), BG)
    dr = ImageDraw.Draw(im)
    dr.text((PAD, 4), "左: 前 (%s)   右: 後 (%s)" % (args.before, args.after), fill=INK, font=f)
    y = LABEL_H + PAD
    for (lab, ta, tb), h in zip(th_rows, cell_h_list):
        for col, t in enumerate((ta, tb)):
            x = PAD + col * (width + PAD)
            dr.rectangle([x, y, x + width, y + LABEL_H - 2], fill=LABEL_BG)
            dr.text((x + 4, y + 3), lab + ("  (無し)" if t is None else ""), fill=INK, font=f)
            if t is not None:
                im.paste(t, (x, y + LABEL_H))
        y += h + LABEL_H + PAD
    os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
    im.save(args.out)
    print("%s  %dx%d  %d 組" % (args.out, im.width, im.height, len(rows)))
    return 0


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--cols", type=int, default=6)
    ap.add_argument("--width", type=int, default=384)
    ap.add_argument("--pairs", nargs=2, metavar=("BEFORE_DIR", "AFTER_DIR"))
    ap.add_argument("--all", nargs=1, metavar="DIR")
    ap.add_argument("--prefix", default=None, help="--pairs で絞る名前")
    ap.add_argument("rest", nargs="*")
    args = ap.parse_args()
    if args.pairs:
        if len(args.rest) != 1:
            ap.error("--pairs <before_dir> <after_dir> <out.png>")
        args.before, args.after, args.out = args.pairs[0], args.pairs[1], args.rest[0]
        return cmd_pairs(args)
    if args.all:
        if len(args.rest) != 1:
            ap.error("--all <dir> <out_dir>")
        args.dir, args.out_dir = args.all[0], args.rest[0]
        return cmd_all(args)
    if len(args.rest) != 3:
        ap.error("<dir> <prefix> <out.png>")
    args.dir, args.prefix, args.out = args.rest
    return cmd_single(args)


if __name__ == "__main__":
    sys.exit(main())
