#!/usr/bin/env python3
"""カード挿絵 (80×48) の一括生産: 発注文 → 2シードの発注書 → A/B 比較シート → 判定 → Art/cards へ適用 (2026-09-19 白から)。

  python3 scripts/card-art.py orders <descriptions.json> <scratch_dir> [--seeds 11,37] [--only id,id] [--out manifest.json] [--force-seeds]
      発注文 (id/name/description/negative_extra/doll) から `node scripts/pixellab.mjs gen` 用の発注書
      docs/pixellab/cards-<color>-orders.json を書く (--out で別名)。出力先は <scratch_dir>/<id>__<seed>.png
  python3 scripts/card-art.py sheet <descriptions.json> <scratch_dir> <out_dir> [--only id,id] [--with-current]
      札ごとに A|B (3倍) を並べた比較シート (8枚/1シート)。--with-current は左に Art/cards の現行版も並べる (作り直しの比較用)
  python3 scripts/card-art.py apply <descriptions.json> <judge.json> <scratch_dir> [--dry]
      判定 [{id, pick: A|B|none, passed, problem, fix_hint, seed?, dir?}] を Art/cards/<id>.png へ写す
      (作り直し分は seed と dir を行に書く)
  札ごとの seed: 発注文の item に "seeds": [a, b] があればそれを使う (同じ seed×似た文で全札が同じ絵に潰れるのを防ぐ。人形 2026-09-19)

発注文 (descriptions.json): { "color", "style", "doll" (人形の共通記述。description の {doll} に展開),
  "defaults" (pixellab.mjs の defaults), "negative", "negative_doll",
  "items": [ { "id", "name", "ja" (何を描くかの日本語メモ), "description" ({style}/{doll} 展開),
               "doll": true (negative_doll を使う), "negative_extra": "..." } ] }
"""
import sys, os, json, datetime
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, 'unity/Assets/Resources/Art/cards')
DEFAULT_SEEDS = (11, 37)

def load(p):
    return json.load(open(p, encoding='utf-8'))

FORCE_SEEDS = False   # --force-seeds: 札ごとの seeds を無視して --seeds を使う (3シードの作り直し用)
def seeds_for(it, seeds):
    return tuple(it['seeds']) if it.get('seeds') and not FORCE_SEEDS else tuple(seeds)

def expand(spec, it):
    d = it['description'].replace('{style}', spec['style']).replace('{doll}', spec.get('doll', ''))
    neg = spec['negative_doll'] if it.get('doll') else spec['negative']
    if it.get('negative_extra'): neg = neg + ', ' + it['negative_extra']
    return d, neg

def cmd_orders(desc_path, scratch, seeds, only=None, out=None):
    spec = load(desc_path)
    os.makedirs(scratch, exist_ok=True)
    items = []
    for it in spec['items']:
        if only and it['id'] not in only: continue
        d, neg = expand(spec, it)
        for sd in seeds_for(it, seeds):
            items.append({'id': f'{it["id"]}__{sd}', 'out': f'{scratch}/{it["id"]}__{sd}.png', 'seed': sd, 'description': d, 'negative': neg})
    out = out or os.path.join(ROOT, 'docs/pixellab', f'cards-{spec["color"]}-orders.json')
    json.dump({'_note': f'{spec.get("_note", "")} ({len(items) // len(seeds)} 札 × {len(seeds)} シード {",".join(map(str, seeds))}。scripts/card-art.py orders が {os.path.basename(desc_path)} から生成)',
               'defaults': spec['defaults'], 'items': items}, open(out, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print(f'{out}: {len(items)} 件')

def font(size=15):
    try:
        import subprocess
        fp = subprocess.run(['fc-list', ':lang=ja', 'file'], capture_output=True, text=True).stdout.split('\n')[0].rstrip(': ').strip()
        return ImageFont.truetype(fp, size)
    except Exception:
        return ImageFont.load_default()

def cmd_sheet(desc_path, scratch, out, seeds, only=None, with_current=False):
    spec = load(desc_path); os.makedirs(out, exist_ok=True); f = font()
    items = [it for it in spec['items'] if not only or it['id'] in only]
    k = 3; cw, ch = 80 * k, 48 * k; per = 8; cols = 2
    ncol = 3 if with_current else 2
    for pg in range(0, len(items), per):
        chunk = items[pg:pg + per]; rows = (len(chunk) + cols - 1) // cols
        cellW = cw * ncol + 24 * (ncol - 1); cellH = ch + 44
        sh = Image.new('RGBA', (cols * (cellW + 16) + 16, rows * (cellH + 10) + 16), (60, 58, 80, 255)); dr = ImageDraw.Draw(sh)
        for n, it in enumerate(chunk):
            x = 16 + (n % cols) * (cellW + 16); y = 16 + (n // cols) * (cellH + 10)
            sds = seeds_for(it, seeds)
            dr.text((x, y), f'{it["id"]}  {it["name"]}  (' + ('old | ' if with_current else '') + f'A=seed{sds[0]} | B=seed{sds[1]})', fill=(245, 240, 225, 255), font=f)
            paths = ([f'{ART}/{it["id"]}.png'] if with_current else []) + [f'{scratch}/{it["id"]}__{sd}.png' for sd in sds]
            for i, p in enumerate(paths):
                if not os.path.exists(p): continue
                im = Image.open(p).convert('RGBA'); big = im.resize((im.width * k, im.height * k), Image.NEAREST)
                bx = x + i * (cw + 24); by = y + 22
                dr.rectangle([bx - 2, by - 2, bx + cw + 2, by + ch + 2], outline=(120, 118, 140, 255))
                sh.paste(big, (bx, by), big)
        name = f'{out}/{spec["color"]}_{pg // per + 1:02d}.png'; sh.save(name)
        print(name, [it['id'] for it in chunk])

def cmd_apply(desc_path, judge_path, scratch, seeds, dry):
    spec = load(desc_path); by = {it['id']: it for it in spec['items']}
    rows = load(judge_path); ok, ng = [], []
    for r in rows:
        if r.get('pick') == 'old': ok.append((r['id'], 'old')); continue   # 既に Art/cards にある版を残す (第2稿で旧版が勝った札)
        if r.get('pick', 'none') == 'none' or not r.get('passed', False): ng.append(r); continue
        sds = seeds_for(by.get(r['id'], {}), seeds)
        sd = r.get('seed') or (sds[0] if r['pick'] == 'A' else sds[1])
        base = r.get('dir') or scratch
        src = f'{base}/{r["id"]}__{sd}.png'; meta = f'{base}/{r["id"]}__{sd}.pixellab.json'
        if not os.path.exists(src): ng.append({**r, 'problem': 'file missing'}); continue
        dst = f'{ART}/{r["id"]}.png'
        if not dry:
            os.makedirs(ART, exist_ok=True)
            Image.open(src).convert('RGBA').save(dst)
            if os.path.exists(meta):
                m = load(meta); m['id'] = r['id']
                m['judge'] = {'pick': r['pick'], 'seed': sd, 'at': datetime.date.today().isoformat(), 'note': r.get('problem', '')}
                json.dump(m, open(f'{ART}/{r["id"]}.pixellab.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
        ok.append((r['id'], sd))
    print(f'applied {len(ok)}, rejected {len(ng)}' + (' (dry)' if dry else ''))
    for r in ng: print('  NG', r['id'], '|', r.get('problem', ''), '|', r.get('fix_hint', ''))

if __name__ == '__main__':
    a = sys.argv[1:]
    seeds = DEFAULT_SEEDS
    if '--seeds' in a:
        i = a.index('--seeds'); seeds = tuple(int(s) for s in a[i + 1].split(',')); del a[i:i + 2]
    only = None
    if '--only' in a:
        i = a.index('--only'); only = set(a[i + 1].split(',')); del a[i:i + 2]
    out = None
    if '--out' in a:
        i = a.index('--out'); out = a[i + 1]; del a[i:i + 2]
    dry = '--dry' in a; a = [x for x in a if x != '--dry']
    if '--force-seeds' in a: FORCE_SEEDS = True; a = [x for x in a if x != '--force-seeds']
    with_current = '--with-current' in a; a = [x for x in a if x != '--with-current']
    c = a[0]
    if c == 'orders': cmd_orders(a[1], a[2], seeds, only, out)
    elif c == 'sheet': cmd_sheet(a[1], a[2], a[3], seeds, only, with_current)
    elif c == 'apply': cmd_apply(a[1], a[2], a[3], seeds, dry)
    else: print(__doc__); sys.exit(1)
