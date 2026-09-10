#!/usr/bin/env python3
"""このは 立ち絵 (128px・4頭身) の武器バリエーションの発注書を作る (2026-09-11)。
  python3 scripts/konoha-tachie-order.py <journal.jsonl> <out.json> <scratch_dir> [--outfit std|wa|both] [--prefix v]
journal = workflow の結果 (各行 result.concepts[]: name_ja/concept_ja/weapon_en/negative_extra)。
頭と尾の文は docs/pixellab/konoha-tachie-control.json (対照＝元の大斧) と同じ。outfit=wa は服だけ和装に差し替える。"""
import sys, json
J, OUT, SC = sys.argv[1], sys.argv[2], sys.argv[3]
opts = {a[2:]: sys.argv[i + 1] for i, a in enumerate(sys.argv) if a.startswith('--')}
outfit = opts.get('outfit', 'std'); prefix = opts.get('prefix', 'v')
ctrl = json.load(open('docs/pixellab/konoha-tachie-control.json', encoding='utf-8'))
HEAD_STD, TAIL, DEF = ctrl['head'], ctrl['tail'], ctrl['defaults']
# 和装: 決定済みの核 (焦げ茶のおさげ・緑の目・深緑・橙の首巻き) は保ち、外套→頭巾と半纏、靴→脚絆と地下足袋
HEAD_WA = HEAD_STD.replace(
    'a deep green hooded cloak with the hood down over a leather chest piece and a cream shirt, short leather boots, ',
    'a deep green zukin hood and a short deep green hanten work jacket with sleeves tied back by a cord (tasuki) over a cream kimono-style shirt, a leather apron, dark work trousers with kyahan leg wraps and jika-tabi split-toe boots, ')
if HEAD_WA == HEAD_STD:
    HEAD_WA = HEAD_STD.replace('a deep green hooded cloak', 'a deep green zukin hood and a short deep green hanten work jacket with sleeves tied back by a cord (tasuki)').replace('short leather boots', 'kyahan leg wraps and jika-tabi split-toe boots')
rows = [json.loads(l) for l in open(J, encoding='utf-8') if '"type":"result"' in l]
items, meta, n = [], [], 0
for r in rows:
    for c in r['result']['concepts']:
        n += 1
        for kind in (['std', 'wa'] if outfit == 'both' else [outfit]):
            idx = f'{prefix}{n}_{kind}'
            head = HEAD_WA if kind == 'wa' else HEAD_STD
            items.append({'id': idx, 'out': f'{SC}/{idx}.png', 'seed': 11, 'description': head + c['weapon_en'].strip().rstrip('.') + TAIL, 'negative': DEF['negative'] + ', ' + c['negative_extra']})
            meta.append({'id': idx, 'n': n, 'name_ja': c['name_ja'], 'concept_ja': c['concept_ja'], 'outfit': kind})
json.dump({'_note': f'このは 立ち絵の武器案 ({J} から生成。outfit={outfit})', 'defaults': DEF, 'items': items, 'meta': meta}, open(OUT, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
print(f'{len(items)} items → {OUT}')
for m in meta: print(' ', m['id'], '|', m['name_ja'])
