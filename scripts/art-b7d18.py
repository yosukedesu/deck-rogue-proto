#!/usr/bin/env python3
"""B7–D18 (レリック・アイコン・意図・マップの駒・幕2/3のタイルと小物・背景・情景・UI部品) の PixelLab 生産ライン (2026-09-11)。
  python3 scripts/art-b7d18.py orders <descriptions.json> <scratch_dir>   # 発注書 docs/pixellab/b7d18-<cat>.json を書く (2シード)
  python3 scripts/art-b7d18.py sheet <scratch_dir> <out_dir>              # 判定用の比較シート (A/B)
  python3 scripts/art-b7d18.py apply <judge.json> <scratch_dir> [--dry]   # 採用した絵を Art/<種別>/ へ (UI 部品は後処理つき)
descriptions.json: {relics:[{id,description,negative}], icons:[...], env:[...]} (workflow art-b7-d18-descriptions の出力)。
判定 judge.json: [{id, pick: A|B|none, passed, problem, fix_hint}] (A=seed 23, B=seed 41)"""
import sys, json, os, shutil
from PIL import Image, ImageDraw, ImageFont

ART = 'unity/Assets/Resources/Art'
SEEDS = (23, 41)
STATUS = ['sword', 'shield', 'heart', 'energy', 'draw', 'growth', 'momentum', 'burn', 'exposed', 'gold', 'exhaust', 'pierce', 'skull', 'crown',
          'hammer', 'question', 'chest', 'flag', 'map', 'star', 'counter', 'set', 'crest_physical', 'crest_spell', 'crest_reaction', 'crest_permanent']
INTENTS = ['attack', 'defend', 'buff', 'rally', 'heal', 'hex', 'destroy-set', 'destroy-token', 'steal-gold', 'flee', 'mill', 'rest', 'hatch']
NODES = ['battle', 'elite', 'boss', 'shop', 'campfire', 'workshop', 'unknown', 'treasure']
PROPS = {'act2_crystal': (32, 48), 'act2_minecart': (48, 32), 'act2_stalactite': (48, 48), 'act2_roots': (48, 48),
         'act3_spire': (48, 96), 'act3_gate': (96, 96), 'act3_aqueduct': (128, 48), 'act3_pillar_fallen': (64, 32)}

# 末尾の定型 (敵・小物の発注書と同じ言い回し)
TAIL_OBJ = ', isolated pixel art game sprite of a single object, centered, nothing else in the frame, completely empty transparent background, no scenery, no ground, no sky, HD-2D Octopath Traveler style, detailed shading, selective dark outline, no text'
TAIL_ICON = ', flat pixel art game icon, bold simple silhouette readable at a small size, thin dark ink outline, one muted accent color with a cream highlight, completely empty transparent background, no text, no frame'
TAIL_NODE = ', pixel art map marker icon, chunky readable silhouette, painterly shading, thin dark ink outline, muted storybook palette, completely empty transparent background, no text, no frame'
NEG_OBJ = 'scene, background, landscape, sky, moon, ground plane, frame, border, text, watermark, blurry, daylight, people, character, face, hands'
NEG_ICON = 'text, letters, number, frame, border, background, scene, gradient background, people, character, face, hands, blurry, photo, realistic, 3d render'
NEG_TILE = 'blurry, glow, bloom, gradient background, text, watermark, extra limbs, side profile, realistic, object, border, frame'
NEG_SCENE = 'person, human, figure, silhouette of a person, hands, face, portrait, text, letters, watermark, border, frame, ui, blurry, daylight, bright saturated colors'

def table():
    """id → (category, out name, size, defaults-key)"""
    t = {}
    for s in STATUS: t[s] = ('icons', s, (32, 32), 'icon')
    for k in INTENTS: t['intent_' + k] = ('icons', 'intent_' + k, (32, 32), 'icon')
    for n in NODES: t['node_' + n] = ('map', 'node_' + n, (32, 32), 'node')
    for a in (2, 3):
        for k in ('grass', 'dirt', 'stone', 'cliff'): t[f'act{a}_{k}'] = ('tiles', f'act{a}_{k}', (32, 32), 'tile_side' if k == 'cliff' else 'tile')
    for p, sz in PROPS.items(): t[p] = ('props', p, sz, 'prop')
    for a in (1, 2, 3): t[f'act{a}'] = ('bg', f'act{a}', (384, 216), 'bg')
    for s in ('campfire', 'workshop', 'shop', 'event'): t[s] = ('scenes', s, (240, 132), 'scene')
    t['cost_orb'] = ('ui', 'cost_orb', (32, 32), 'ui')
    for r in ('common', 'uncommon', 'rare'): t['gem_' + r] = ('ui', 'gem_' + r, (32, 32), 'ui')
    t['slash'] = ('fx', 'slash', (64, 16), 'fx')
    return t

DEFAULTS = {
    'relic': dict(engine='pixflux', view='side', outline='selective outline', shading='detailed shading', detail='highly detailed', no_background=True, guidance=8),
    'icon': dict(engine='pixflux', view='side', outline='single color black outline', shading='basic shading', detail='low detail', no_background=True, guidance=9),
    'node': dict(engine='pixflux', view='side', outline='selective outline', shading='medium shading', detail='medium detail', no_background=True, guidance=8),
    'tile': dict(engine='pixflux', view='high top-down', outline='lineless', shading='medium shading', detail='medium detail', no_background=False, guidance=8),
    'tile_side': dict(engine='pixflux', view='side', outline='lineless', shading='medium shading', detail='medium detail', no_background=False, guidance=8),
    'prop': dict(engine='pixflux', view='low top-down', outline='selective outline', shading='detailed shading', detail='highly detailed', no_background=True, guidance=8),
    'bg': dict(engine='pixflux', view='side', outline='lineless', shading='medium shading', detail='medium detail', no_background=False, guidance=8),
    'scene': dict(engine='pixflux', view='low top-down', outline='selective outline', shading='detailed shading', detail='highly detailed', no_background=False, guidance=8),
    'ui': dict(engine='pixflux', view='side', outline='selective outline', shading='detailed shading', detail='medium detail', no_background=True, guidance=8),
    'fx': dict(engine='pixflux', view='side', outline='lineless', shading='flat shading', detail='low detail', no_background=True, guidance=8),
}

def norm_id(raw, t):
    """workflow が返した id を表の id に寄せる (intent_/node_/bg_ の有無の揺れ)"""
    if raw in t: return raw
    for pre in ('intent_', 'node_', 'bg_', 'scene_', 'tile_', 'prop_'):
        if raw.startswith(pre) and raw[len(pre):] in t: return raw[len(pre):]
    for pre in ('intent_', 'node_'):
        if pre + raw in t: return pre + raw
    if raw.startswith('bg_') and raw[3:] in t: return raw[3:]
    return None

def cmd_orders(desc_path, scratch):
    d = json.load(open(desc_path, encoding='utf-8'))
    t = table()
    orders = {'relics': [], 'icons': [], 'env': [], 'ui': []}
    seen = set()
    # レリック
    for it in d.get('relics', []):
        rid = it['id']; seen.add(rid)
        for sd in SEEDS:
            orders['relics'].append(dict(id=f'{rid}__{sd}', out=f'{scratch}/relics/{rid}__{sd}.png', seed=sd, size=[32, 32],
                                         description=it['description'].rstrip('.') + TAIL_OBJ, negative=(NEG_OBJ + ', ' + it.get('negative', '')).rstrip(', '), **DEFAULTS['relic']))
    for grp in ('icons', 'env'):
        for it in d.get(grp, []):
            rid = norm_id(it['id'], t)
            if rid is None: print('unknown id:', it['id']); continue
            cat, name, size, dk = t[rid]; seen.add(rid)
            desc = it['description'].rstrip('.')
            neg = it.get('negative', '')
            if dk == 'icon': desc += TAIL_ICON; neg = NEG_ICON + ', ' + neg
            elif dk == 'node': desc += TAIL_NODE; neg = NEG_ICON + ', ' + neg
            elif dk == 'prop': desc += TAIL_OBJ; neg = NEG_OBJ + ', ' + neg
            elif dk in ('tile', 'tile_side'): neg = NEG_TILE + ', ' + neg
            elif dk in ('bg', 'scene'): neg = NEG_SCENE + ', ' + neg
            for sd in SEEDS:
                orders[grp].append(dict(id=f'{rid}__{sd}', out=f'{scratch}/{cat}/{name}__{sd}.png', seed=sd, size=list(size), description=desc, negative=neg.rstrip(', '), **DEFAULTS[dk]))
    # UI 部品 (説明はここに固定)
    UI = {
        'cost_orb': ('a round polished honey-amber glass orb with a soft inner glow and a small white specular highlight at the upper left, thin dark brass rim, the orb fills the frame edge to edge', 'ui'),
        'gem_common': ('a small faceted gray-silver gemstone, diamond cut seen from the front, subtle light facets, the gem fills the frame', 'ui'),
        'gem_uncommon': ('a small faceted sky-blue sapphire gemstone, diamond cut seen from the front, bright light facets, the gem fills the frame', 'ui'),
        'gem_rare': ('a small faceted honey-gold gemstone, diamond cut seen from the front, bright light facets, the gem fills the frame', 'ui'),
        'slash': ('a horizontal white slash streak of light, bright cream-white center thinning to sharp points at both ends, faint teal edge, motion trail, nothing else', 'fx'),
    }
    for rid, (desc, dk) in UI.items():
        cat, name, size, _ = t[rid]
        for sd in SEEDS:
            orders['ui'].append(dict(id=f'{rid}__{sd}', out=f'{scratch}/{cat}/{name}__{sd}.png', seed=sd, size=list(size), description=desc + (TAIL_OBJ if dk == 'ui' else ', pixel art game effect sprite, completely empty transparent background, no text'), negative=NEG_OBJ, **DEFAULTS[dk]))
    missing = [k for k in t if k not in seen and k not in UI]
    if missing: print('説明が無い id:', missing)
    for grp, items in orders.items():
        p = f'docs/pixellab/b7d18-{grp}.json'
        json.dump({'_note': f'B7–D18 の発注書 ({grp})。scripts/art-b7d18.py orders が生成。2シード (23/41) を scratch に出し、sheet → 判定 → apply で Art/ へ写す', 'items': items}, open(p, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
        print(p, len(items))

def font():
    try:
        import subprocess
        fp = subprocess.run(['fc-list', ':lang=ja', 'file'], capture_output=True, text=True).stdout.split('\n')[0].rstrip(': ').strip()
        return ImageFont.truetype(fp, 16)
    except Exception:
        return ImageFont.load_default()

def cmd_sheet(scratch, out, only=None):
    os.makedirs(out, exist_ok=True)
    t = table(); f = font()
    groups = {}
    for cat in ('relics', 'icons', 'map', 'tiles', 'props', 'bg', 'scenes', 'ui', 'fx'):
        if only and cat not in only: continue
        d = f'{scratch}/{cat}'
        if not os.path.isdir(d): continue
        names = sorted({fn.rsplit('__', 1)[0] for fn in os.listdir(d) if fn.endswith('.png')})
        groups[cat] = names
    for cat, names in groups.items():
        # 1 シートに最大 12 項目 (A/B 並び)。倍率は寸法で決める
        per = 12 if cat in ('relics', 'icons', 'map', 'tiles', 'ui', 'fx') else (6 if cat == 'props' else 3)
        for pg in range(0, len(names), per):
            chunk = names[pg:pg + per]
            cells = []
            for nm in chunk:
                ims = []
                for sd in SEEDS:
                    p = f'{scratch}/{cat}/{nm}__{sd}.png'
                    ims.append(Image.open(p).convert('RGBA') if os.path.exists(p) else None)
                cells.append((nm, ims))
            w0 = max(im.width for _, ims in cells for im in ims if im) ; h0 = max(im.height for _, ims in cells for im in ims if im)
            k = 6 if w0 <= 32 else 4 if w0 <= 64 else 3 if w0 <= 128 else 2
            cw, ch = w0 * k, h0 * k
            cols = 1 if cat in ('bg', 'scenes') else 2 if cat == 'props' else 3
            rows = (len(cells) + cols - 1) // cols
            cellW = cw * 2 + 30; cellH = ch + 44
            sh = Image.new('RGBA', (cols * (cellW + 16) + 16, rows * (cellH + 12) + 16), (60, 58, 80, 255)); dr = ImageDraw.Draw(sh)
            for n, (nm, ims) in enumerate(cells):
                x = 16 + (n % cols) * (cellW + 16); y = 16 + (n // cols) * (cellH + 12)
                dr.text((x, y), f'{nm}  (A=seed{SEEDS[0]} | B=seed{SEEDS[1]})', fill=(245, 240, 225, 255), font=f)
                for i, im in enumerate(ims):
                    if im is None: continue
                    big = im.resize((im.width * k, im.height * k), Image.NEAREST)
                    bx = x + i * (cw + 30); by = y + 24
                    dr.rectangle([bx - 2, by - 2, bx + cw + 2, by + ch + 2], outline=(120, 118, 140, 255))
                    # 透過の絵は市松の下地で見せる
                    if cat not in ('tiles', 'bg', 'scenes'):
                        chk = Image.new('RGBA', (cw, ch), (90, 88, 110, 255)); cd = ImageDraw.Draw(chk)
                        for yy in range(0, ch, 16):
                            for xx in range(0, cw, 16):
                                if (xx // 16 + yy // 16) % 2 == 0: cd.rectangle([xx, yy, xx + 15, yy + 15], fill=(76, 74, 96, 255))
                        sh.paste(chk, (bx, by))
                    sh.paste(big, (bx + (cw - big.width) // 2, by + (ch - big.height) // 2), big)
            sh.save(f'{out}/{cat}_{pg // per + 1:02d}.png')
            print(f'{out}/{cat}_{pg // per + 1:02d}.png', chunk)

def crop_to_content(im, pad=0):
    b = im.getbbox()
    if not b: return im
    return im.crop((max(0, b[0] - pad), max(0, b[1] - pad), min(im.width, b[2] + pad), min(im.height, b[3] + pad)))

def fit_square(im, n):
    """内容を n×n に収める (縮小は整数比でなければ LANCZOS = UI 部品は「なめらか」でよい)"""
    im = crop_to_content(im)
    s = min(n / im.width, n / im.height)
    w, h = max(1, round(im.width * s)), max(1, round(im.height * s))
    small = im.resize((w, h), Image.NEAREST if s >= 1 else Image.LANCZOS)
    out = Image.new('RGBA', (n, n), (0, 0, 0, 0)); out.paste(small, ((n - w) // 2, (n - h) // 2), small)
    return out

def cmd_apply(judge_path, scratch, dry):
    rows = json.load(open(judge_path, encoding='utf-8'))
    t = table(); ok, ng = [], []
    for r in rows:
        rid = r['id'] if r['id'].startswith('relic_') else norm_id(r['id'], t)
        if rid is None: ng.append({**r, 'problem': 'unknown id'}); continue
        if r.get('pick', 'none') == 'none' or not r.get('passed', False): ng.append(r); continue
        sd = r.get('seed') or (SEEDS[0] if r['pick'] == 'A' else SEEDS[1])   # 作り直しは seed と dir を行に書く
        base = r.get('dir') or scratch
        if rid.startswith('relic_'): cat, name = 'relics', rid
        else: cat, name, _, _ = t[rid]
        src = f'{base}/{cat}/{name}__{sd}.png'; meta = f'{base}/{cat}/{name}__{sd}.pixellab.json'
        if not os.path.exists(src): ng.append({**r, 'problem': 'file missing'}); continue
        dst = f'{ART}/{cat}/{name}.png'
        if not dry:
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            im = Image.open(src).convert('RGBA')
            if name == 'cost_orb': im = fit_square(im, 26)          # 26 ドット×2 = カードのコスト玉 52px
            elif name.startswith('gem_'): im = fit_square(im, 12)   # 12 ドット×2 = 帯の宝石 24px
            im.save(dst)
            if os.path.exists(meta):
                m = json.load(open(meta, encoding='utf-8')); m['judge'] = {'pick': r['pick'], 'seed': sd, 'at': '2026-09-11', 'note': r.get('problem', '')}
                json.dump(m, open(f'{ART}/{cat}/{name}.pixellab.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
        ok.append((name, sd))
    print(f'applied {len(ok)}, rejected {len(ng)}' + (' (dry)' if dry else ''))
    for r in ng: print('  NG', r['id'], '|', r.get('problem', ''), '|', r.get('fix_hint', ''))

if __name__ == '__main__':
    c = sys.argv[1]
    if c == 'orders': cmd_orders(sys.argv[2], sys.argv[3])
    elif c == 'sheet': cmd_sheet(sys.argv[2], sys.argv[3], sys.argv[4].split(',') if len(sys.argv) > 4 else None)
    elif c == 'apply': cmd_apply(sys.argv[2], sys.argv[3], '--dry' in sys.argv)
    else: print(__doc__)
