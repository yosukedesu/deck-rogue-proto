#!/usr/bin/env python3
"""scripts/import-enemy-dotset.py — dot（Codex）の敵のコマ（Godot 枝 origin/codex/unity-parity-20261013 の authored-frames/<敵>/）を
Unity の Resources/Art/enemies/dot/<enemyId>/ へ取り込む（2026-10-26 ユーザー裁定「1ドット＝3px で取り込む。まず待機の 8 コマ」）。

取り込む物（ファイル名は舞台の決まり <名前>_<動き>_<n>.png に合わせる。StageUnits.BindUnit が AnimFolder から引く）:
  <id>.png            … rest.png（一枚絵＝板の元。Creature.Get が enemies/<id>.png より先に見る）
  <id>_idle_<n>.png   … idle_00〜07
  <id>_hurt_0.png     … hurt.png     <id>_block_0.png … block.png（無い敵もある）
  <id>_attack_<n>.png … 技のコマから 予備動作（*prepare）→当たり（*contact／impact／release）→戻り（recovery）の順に最大 3 枚
  <id>_clipset.json   … dot の台帳そのまま（枠・足の位置・コマの秒・技の一覧。後で技ごとのコマを使う時の元）
法線 <名前>_n.png は別に scripts/sprite-normals.py normals で作る。
使い方: python3 scripts/import-enemy-dotset.py [--branch origin/codex/unity-parity-20261013] [--only enemy_axe_ogre,...]
"""
import argparse, io, json, os, subprocess, sys
from PIL import Image
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
OUT = os.path.join(ROOT, 'unity', 'Assets', 'Resources', 'Art', 'enemies', 'dot')

# 台帳に enemyId が無い 3 体 (dot の古い台帳) はフォルダ名から
SLUG_IDS = {'brute': 'enemy_brute', 'probe': 'enemy_probe', 'wolf': 'enemy_wolf'}

def git_show(branch, path):
    return subprocess.run(['git', 'show', f'{branch}:{path}'], cwd=ROOT, check=True, capture_output=True).stdout

def pick_attack(clips):
    """技のコマ: 予備動作→当たり→戻り。無い段は飛ばす"""
    names = list(clips.keys())
    def first(pred):
        for n in names:
            if pred(n) and clips[n]: return n
        return None
    prep = first(lambda n: n.endswith('prepare') and not n.startswith('guard') and not n.startswith('gasp'))
    hit = first(lambda n: n.endswith('contact') or n in ('impact', 'release', 'contacta'))
    rec = 'recovery' if 'recovery' in clips else None
    out = []
    for n in (prep, hit, rec):
        if n: out.append(clips[n][0]['file'])
    return out

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--branch', default='origin/codex/unity-parity-20261013')
    ap.add_argument('--only', default='')
    a = ap.parse_args()
    only = set(x for x in a.only.split(',') if x)
    paths = subprocess.run(['git', 'ls-tree', '-r', '--name-only', a.branch], cwd=ROOT, check=True, capture_output=True).stdout.decode().split('\n')
    sets = [p for p in paths if p.startswith('authored-frames/') and p.endswith('/clipset.json')]
    n_enemy = n_file = 0; problems = []
    for cs in sets:
        d = os.path.dirname(cs)
        j = json.loads(git_show(a.branch, cs))
        eid = j.get('enemyId') or j.get('enemy') or j.get('id') or SLUG_IDS.get(os.path.basename(d))
        if not eid:
            problems.append(f'{d}: enemyId が無い (keys={sorted(j.keys())[:8]})'); continue
        if only and eid not in only: continue
        w, h = j.get('frameCanvas') or j.get('sourceCanvas')
        clips = j.get('clips', {})
        want = {}   # 出力名 → 元ファイル
        if 'rest' in clips: want[f'{eid}.png'] = clips['rest'][0]['file']
        for i, fr in enumerate(clips.get('idle', [])): want[f'{eid}_idle_{i}.png'] = fr['file']
        if 'hurt' in clips: want[f'{eid}_hurt_0.png'] = clips['hurt'][0]['file']
        if 'block' in clips: want[f'{eid}_block_0.png'] = clips['block'][0]['file']
        for i, f in enumerate(pick_attack(clips)): want[f'{eid}_attack_{i}.png'] = f
        od = os.path.join(OUT, eid); os.makedirs(od, exist_ok=True)
        for out, src in want.items():
            data = git_show(a.branch, f'{d}/{src}')
            im = Image.open(io.BytesIO(data))
            if im.size != (w, h): problems.append(f'{eid}: {src} は {im.size} (台帳の枠 {w}x{h})')
            with open(os.path.join(od, out), 'wb') as fp: fp.write(data)
            n_file += 1
        with open(os.path.join(od, f'{eid}_clipset.json'), 'wb') as fp: fp.write(git_show(a.branch, cs))
        n_enemy += 1
    print(f'enemies {n_enemy}, png {n_file}, problems {len(problems)}')
    for p in problems: print('  ', p)

if __name__ == '__main__':
    main()
