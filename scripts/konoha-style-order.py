#!/usr/bin/env python3
"""このは ちびの「日本のキャラデザ風デフォルメ」スタイル案の発注書を作る (2026-09-11)。
  python3 scripts/konoha-style-order.py <journal.jsonl> <out.json> <scratch_dir> [--seeds 11,61] [--size 64] [--prefix s]
journal = workflow の結果 (各行 result.variants[]: name_ja/intent_ja/description/negative/params{view,direction,outline,shading,detail,guidance})。"""
import sys, json
J, OUT, SC = sys.argv[1], sys.argv[2], sys.argv[3]
opts = {a[2:]: sys.argv[i + 1] for i, a in enumerate(sys.argv) if a.startswith('--')}
seeds = [int(x) for x in opts.get('seeds', '11,61').split(',')]; size = int(opts.get('size', 64)); prefix = opts.get('prefix', 's')
rows = [json.loads(l) for l in open(J, encoding='utf-8') if '"type":"result"' in l]
items, meta, n = [], [], 0
for r in rows:
    for v in r['result']['variants']:
        n += 1; p = v['params']
        for sd in seeds:
            idx = f'{prefix}{n}_{sd}'
            items.append({'id': idx, 'out': f'{SC}/{idx}.png', 'seed': sd, 'size': [size, size], 'description': v['description'], 'negative': v['negative'],
                          'view': p['view'], 'direction': p['direction'], 'outline': p['outline'], 'shading': p['shading'], 'detail': p['detail'], 'guidance': p['guidance']})
        meta.append({'n': n, 'name_ja': v['name_ja'], 'intent_ja': v['intent_ja']})
json.dump({'_note': f'このは ちびのスタイル案 ({J} から生成。size={size})', 'defaults': {'engine': 'pixflux', 'no_background': True}, 'items': items, 'meta': meta},
          open(OUT, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
print(f'{len(items)} items → {OUT}')
for m in meta: print(' ', m['n'], '|', m['name_ja'], '|', m['intent_ja'][:50])
