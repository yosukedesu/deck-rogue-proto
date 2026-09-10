#!/usr/bin/env python3
"""生成した敵 (id__seed.png) を「敵ごとの2シード比較」の小さなシート (4倍・ラベル付き) と、幕ごとの一覧シートにする。
  python3 scripts/enemy-pairs-sheet.py <scratch_dir> <todo.json> <out_dir>"""
import sys, json, os
from PIL import Image, ImageDraw, ImageFont
SC, TODO, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(OUT, exist_ok=True)
todo = json.load(open(TODO, encoding='utf-8'))
try:
    import subprocess
    fp = subprocess.run(['fc-list', ':lang=ja', 'file'], capture_output=True, text=True).stdout.split('\n')[0].rstrip(': ').strip()
    font = ImageFont.truetype(fp, 20); small = ImageFont.truetype(fp, 15)
except Exception:
    font = small = ImageFont.load_default()
k = 4
for e in todo:
    ims = []
    for sd in (23, 41):
        p = f'{SC}/{e["id"]}__{sd}.png'
        ims.append(Image.open(p).convert('RGBA') if os.path.exists(p) else None)
    if not any(ims): continue
    w = e['size'] * k
    sh = Image.new('RGBA', (w * 2 + 36, w + 48), (60, 58, 80, 255)); d = ImageDraw.Draw(sh)
    for i, im in enumerate(ims):
        if im is None: continue
        big = im.resize((im.width * k, im.height * k), Image.NEAREST); sh.paste(big, (12 + i * (w + 12), 40), big)
        d.text((12 + i * (w + 12), 6), f'{"A" if i == 0 else "B"} seed {23 if i == 0 else 41}', fill=(245, 240, 225, 255), font=small)
    d.text((w + 12 - 40, 6), f'{e["name"]} ({e["kind"]}, {e["size"]}px)', fill=(245, 240, 225, 255), font=small)
    sh.save(f'{OUT}/{e["id"]}.png')
# 幕ごとの一覧 (seed 23 のみ・3倍)
for act in (2, 3):
    es = [e for e in todo if e['act'] == act]
    cols = 8; k2 = 3; cell = 96 * k2; rows = (len(es) + cols - 1) // cols
    sh = Image.new('RGBA', (cols * (cell + 10) + 10, rows * (cell + 36) + 10), (60, 58, 80, 255)); d = ImageDraw.Draw(sh)
    for n, e in enumerate(es):
        p = f'{SC}/{e["id"]}__23.png'
        if not os.path.exists(p): continue
        im = Image.open(p).convert('RGBA'); big = im.resize((im.width * k2, im.height * k2), Image.NEAREST)
        x = 10 + (n % cols) * (cell + 10); y = 10 + (n // cols) * (cell + 36)
        sh.paste(big, (x + (cell - big.width) // 2, y + cell - big.height), big)
        d.text((x + 2, y + cell + 6), e['name'], fill=(245, 240, 225, 255), font=small)
    sh.save(f'{OUT}/sheet_act{act}.png')
print('pairs:', len([f for f in os.listdir(OUT) if f.startswith('enemy_')]), 'sheets written')
