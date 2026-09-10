#!/usr/bin/env python3
"""幕2/3の敵の PixelLab 発注書を、workflow の「種の記述」(journal.jsonl) から系統別に作る (2026-09-11)。
  python3 scripts/enemy-orders-act23.py <journal.jsonl> <todo.json> <scratch_dir> [--seeds 23,41]
出力: docs/pixellab/enemies-act23-{beast,machine,hybrid,revenant}.json"""
import sys, json
J, TODO, SC = sys.argv[1], sys.argv[2], sys.argv[3]
opts = {a[2:]: sys.argv[i + 1] for i, a in enumerate(sys.argv) if a.startswith('--')}
seeds = [int(x) for x in opts.get('seeds', '23,41').split(',')]
todo = {e['id']: e for e in json.load(open(TODO, encoding='utf-8'))}
rows = [json.loads(l) for l in open(J, encoding='utf-8') if '"type":"result"' in l]
desc = {}
for r in rows:
    for it in (r.get('result') or {}).get('items', []): desc[it['id']] = it
beast = json.load(open('docs/pixellab/enemies-act1-beasts.json', encoding='utf-8'))
machine = json.load(open('docs/pixellab/enemies-act1-machines.json', encoding='utf-8'))
STYLE = {
    'beast': beast['style'],
    'machine': machine['style'],
    'hybrid': beast['style'] + ', with two or three mismatched parts of tarnished pale metal grafted onto the body: a riveted plate, a brass joint with a small cog, a small pale glowing lens, stitched seams where metal meets fur',
    'revenant': 'pixel art in the style of a gentle watercolor picture book illustration of the faint remains of a miner who never came back: a small floating pale translucent wisp or shroud shape with soft dissolving edges, no face, no limbs, faintly glowing from within, carrying one small mining prop, palette of cream paper, pale silver, faded sage, dusty blue-grey and ink, thin soft outline, low contrast',
}
NEG = {'beast': beast['defaults']['negative'], 'machine': machine['defaults']['negative'],
       'hybrid': beast['defaults']['negative'], 'revenant': beast['defaults']['negative'] + ', face, eyes, arms, legs, hands, body, torso, robe, cloak, figure, silhouette of a person'}
SHADING = {'beast': 'medium shading', 'machine': 'detailed shading', 'hybrid': 'medium shading', 'revenant': 'basic shading'}
NOTE = {'beast': '幕2/3の獣 (露頭でマナを浴びて育った生き物＝水彩絵本×淡い光)', 'machine': '幕2/3の古機 (野生化した古代の採掘機械＝金属質)',
        'hybrid': '幕2/3の継ぎ物 (生き物に古代の部品を継いだもの＝獣の画材＋金属の継ぎ手)', 'revenant': '幕2/3の名残 (坑で消えた者の名残＝顔も手足も無い小さな漂う影)'}
out = {k: [] for k in STYLE}
missing = [i for i in todo if i not in desc]
for i, e in todo.items():
    d = desc.get(i)
    if not d: continue
    for sd in seeds:
        out[e['kind']].append({'id': f'{i}__{sd}', 'out': f'{SC}/{i}__{sd}.png', 'seed': sd, 'size': [e['size'], e['size']],
                               'description': d['description'].strip().rstrip('.') + ', {style}',
                               'negative': NEG[e['kind']] + (', ' + d['negative_extra'] if d.get('negative_extra') else '')})
for k, items in out.items():
    doc = {'_note': f"{NOTE[k]} (2026-09-11 ユーザー「2,3幕の敵画像もpixellab作成して適用して」)。種の記述は workflow enemy-art-descriptions-act23。各2シード ({','.join(map(str, seeds))}) から選ぶ。form_ja: " +
                    '; '.join(f"{i}={desc[i]['form_ja']}" for i in todo if i in desc and todo[i]['kind'] == k),
           'defaults': {'engine': 'pixflux', 'view': 'low top-down', 'direction': 'south-west', 'outline': 'selective outline', 'shading': SHADING[k],
                        'detail': 'highly detailed', 'no_background': True, 'guidance': 9},
           'style': STYLE[k], 'items': items}
    json.dump(doc, open(f'docs/pixellab/enemies-act23-{k}.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print(k, len(items) // len(seeds), 'enemies')
if missing: print('MISSING descriptions:', missing)
