#!/usr/bin/env python3
"""判定 (workflow enemy-art-judge の結果 JSON) に従って、採用した敵の絵を Art/enemies へ写す。
  python3 scripts/enemy-apply-act23.py <judge.json> <scratch_dir> [--dry]
judge.json: [{id, pick: A|B|none, passed, problem, fix_hint}] (A=seed23, B=seed41)。不合格は写さず一覧に出す"""
import sys, json, shutil, os
J, SC = sys.argv[1], sys.argv[2]; dry = '--dry' in sys.argv
rows = json.load(open(J, encoding='utf-8'))
D = 'unity/Assets/Resources/Art/enemies'
ok, ng = [], []
for r in rows:
    if r['pick'] == 'none' or not r.get('passed', False):
        ng.append(r); continue
    sd = 23 if r['pick'] == 'A' else 41
    src = f'{SC}/{r["id"]}__{sd}.png'; meta = f'{SC}/{r["id"]}__{sd}.pixellab.json'
    if not os.path.exists(src): ng.append({**r, 'problem': 'file missing'}); continue
    if not dry:
        shutil.copy(src, f'{D}/{r["id"]}.png')
        if os.path.exists(meta):
            m = json.load(open(meta, encoding='utf-8')); m['judge'] = {'pick': r['pick'], 'seed': sd, 'at': '2026-09-11'}
            json.dump(m, open(f'{D}/{r["id"]}.pixellab.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    ok.append((r['id'], sd))
print(f'applied {len(ok)}, rejected {len(ng)}' + (' (dry)' if dry else ''))
for r in ng: print('  NG', r['id'], '|', r.get('problem', ''), '|', r.get('fix_hint', ''))
