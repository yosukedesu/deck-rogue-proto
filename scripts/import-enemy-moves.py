#!/usr/bin/env python3
"""scripts/import-enemy-moves.py — dot（Codex）の敵のコマの台帳（Godot 枝 authored-frames/<敵>/clipset.json）から、
**技ごとのコマ列**を Unity の Resources/Art/enemies/dot/<enemyId>/ へ取り込む（2026-10-28。dot の Godot 枝の続きを Unity へ移す §4-2）。

台帳の clips は 「<語幹>prepare → <語幹>contact|impact|release|roar|call|hold|gesture|pulse|apply|applied|beat|extend
 → (<語幹>rebound → <語幹>contact2|contactb) → recovery|<語幹>return|<語幹>recovery」 の名で並ぶ。
技 id（enemies.json の moves[].id）の語幹（club_wild→wild・war_roar→warroar・acid_spit→spit…）で引き、引けない技は ALIASES で補う。
素の攻撃（windup/impact/recovery・prepare/contact）は、語幹で引けない攻撃の技が順に使う。

出力（Unity の StageUnits.BindUnit が読む）:
  <id>_m_<技>_<n>.png   … 技のコマ（予備動作→当たり→（跳ね返り→2発目）→戻り）
  <id>_moves.json       … { "<技>": { "frames": [{"file": "<id>_m_<技>_<n>", "dur": 秒}, …], "contact": 当たりのコマの番号（無ければ -1）, "kind": 技の種類 } }
法線は別に scripts/sprite-normals.py normals <png...> で。
使い方: python3 scripts/import-enemy-moves.py [--branch origin/codex/unity-parity-20261013] [--only enemy_brute,...] [--dry]
"""
import argparse, io, json, os, re, subprocess, sys
from PIL import Image
ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
OUT = os.path.join(ROOT, 'unity', 'Assets', 'Resources', 'Art', 'enemies', 'dot')
SLUG_IDS = {'brute': 'enemy_brute', 'probe': 'enemy_probe', 'wolf': 'enemy_wolf'}
CONTACT = ('contact', 'impact', 'release', 'contacta', 'contact1', 'beat', 'roar', 'call', 'hold', 'gesture', 'pulse', 'apply', 'applied', 'extend', 'reach')
# 語幹の規約で引けない技 → 台帳のコマの鍵を名指し（2026-10-28 の棚卸し: 285 技のうち 18）
ALIASES = {
    ('enemy_brute', 'rage_flurry'): ['windup', 'impact', 'rebound', 'impact', 'recovery'],
    ('enemy_chimera_2', 'swelling_claws'): ['clawprepare', 'clawcontacta', 'clawrebound', 'clawcontactb', 'recovery'],
    ('enemy_chimera_3', 'lacerate'): ['clawprepare', 'clawcontacta', 'clawrebound', 'clawcontactb', 'recovery'],
    ('enemy_apprentice_colossus', 'swing'): ['attackprepare', 'attackcontact', 'recovery'],
    ('enemy_crab_cannon', 'recharge'): ['recharge'],
    ('enemy_crossbow_archer', 'aimed_shot'): ['windup', 'release', 'recovery'],
    ('enemy_drummer', 'war_drum'): ['prepare', 'beat', 'recovery'],
    ('enemy_iron_clam', 'pressurize'): ['pressureprepare', 'pressurepulse', 'pressurereturn'],
    ('enemy_elite_stab_book', 'stab2'): ['stabprepare', 'stabcontact1', 'stabrebound', 'stabcontact2', 'recovery'],
    ('enemy_elite_stab_book', 'stab3'): ['stabprepare', 'stabcontact1', 'stabrebound', 'stabcontact2', 'recovery'],
    ('enemy_elite_stab_book', 'stab4'): ['stabprepare', 'stabcontact1', 'stabrebound', 'stabcontact2', 'recovery'],
    ('enemy_thief', 'sneak'): ['sneak', 'turn'],
    ('enemy_thief', 'snatch'): ['reach', 'stow', 'recovery'],
    ('enemy_thief', 'run_off'): ['run', 'runpass', 'flee'],
    ('enemy_warden', 'bell_hushed'): ['hushprepare', 'hushcontact', 'recovery'],
    ('enemy_winch_warden', 'release_orbs'): ['summonprepare', 'summonapplied', 'recovery'],
    ('enemy_winch_warden', 'rewind'): ['rewind'],
    ('enemy_wolf', 'rend'): ['windup', 'impact', 'recovery'],
}

def git_show(branch, path):
    return subprocess.run(['git', 'show', f'{branch}:{path}'], cwd=ROOT, check=True, capture_output=True).stdout

def stems(mid):
    w = mid.split('_')
    out = [mid.replace('_', ''), mid] + w[::-1] + [w[0]]
    # claws→claw などの複数形
    out += [s[:-1] for s in list(out) if s.endswith('s') and len(s) > 3]
    seen = []
    for s in out:
        if s not in seen: seen.append(s)
    return seen

def by_stem(clips, mid, kind):
    keys = list(clips.keys())
    for s in stems(mid):
        pre = [k for k in keys if k in (s + 'prepare', s + 'windup', s + '_prepare')]
        if not pre: continue
        p = pre[0]
        hit = [k for k in keys if k.startswith(s) and k != p and not k.endswith('rebound') and any(k.endswith(x) for x in CONTACT)]
        hit.sort(key=lambda k: (0 if k.endswith(('contact', 'impact', 'release', 'contacta', 'contact1')) else 1))
        seq = [p] + hit[:1]
        reb = [k for k in keys if k == s + 'rebound']
        second = [k for k in keys if k.startswith(s) and (k.endswith('contact2') or k.endswith('contactb'))]
        if reb and second: seq += [reb[0], second[0]]
        rec = [k for k in keys if k in (s + 'recovery', s + 'return', s + 'reopen')]
        if rec: seq.append(rec[0])
        elif 'recovery' in clips and kind == 'attack': seq.append('recovery')
        return seq
    return None

def generic(clips, kind):
    if kind != 'attack': return None
    g = []
    for k in ('windup', 'prepare'):
        if k in clips: g.append(k); break
    for k in ('impact', 'contact', 'release', 'thrust'):
        if k in clips: g.append(k); break
    if 'rebound' in clips and 'contact2' in clips: g += ['rebound', 'contact2']
    if 'recovery' in clips: g.append('recovery')
    return g if len(g) >= 2 else None

def guard(clips):
    if 'guardprepare' not in clips: return None
    g = ['guardprepare'] + (['guardhold'] if 'guardhold' in clips else [])
    g += [k for k in ('guardreturn', 'guardrecovery') if k in clips][:1]
    return g

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--branch', default='origin/codex/unity-parity-20261013')
    ap.add_argument('--only', default='')
    ap.add_argument('--dry', action='store_true')
    a = ap.parse_args()
    only = set(x for x in a.only.split(',') if x)
    enemies = {e['id']: e for e in json.load(open(os.path.join(ROOT, 'src', 'data', 'enemies.json'), encoding='utf-8'))}
    paths = subprocess.run(['git', 'ls-tree', '-r', '--name-only', a.branch], cwd=ROOT, check=True, capture_output=True).stdout.decode().split('\n')
    sets = [p for p in paths if p.startswith('authored-frames/') and p.endswith('/clipset.json')]
    n_enemy = n_move = n_png = 0; misses = []; problems = []
    for cs in sets:
        d = os.path.dirname(cs)
        j = json.loads(git_show(a.branch, cs))
        eid = j.get('enemyId') or SLUG_IDS.get(os.path.basename(d))
        if eid not in enemies: problems.append(f'{d}: enemies.json に無い ({eid})'); continue
        if only and eid not in only: continue
        clips = j.get('clips', {})
        w, h = j.get('frameCanvas') or j.get('sourceCanvas')
        moves = {}; generic_used = False
        for m in enemies[eid].get('moves', []):
            mid = m['id']; kind = m.get('kind', '')
            seq = ALIASES.get((eid, mid))
            if seq is None: seq = by_stem(clips, mid, kind)
            if seq is None and kind == 'attack' and not generic_used:
                seq = generic(clips, kind)
                if seq: generic_used = True
            if seq is None and kind == 'defend': seq = guard(clips)
            if not seq or any(k not in clips for k in seq):
                misses.append((eid, mid, kind, [k for k in (seq or []) if k not in clips])); continue
            frames = []
            for k in seq:
                for fr in clips[k]: frames.append((fr['file'], float(fr['duration']), k))
            contact = -1
            for i, (_, _, k) in enumerate(frames):
                if k not in ('windup', 'prepare') and any(k.endswith(x) for x in CONTACT) and not k.endswith('rebound'):
                    contact = i; break
            # 素の攻撃 (windup→impact) の当たりは impact
            if contact < 0:
                for i, (_, _, k) in enumerate(frames):
                    if k in ('impact', 'contact', 'release', 'thrust', 'beat'): contact = i; break
            moves[mid] = {'frames': [{'file': f'{eid}_m_{mid}_{i}', 'dur': round(dur, 4)} for i, (_, dur, _) in enumerate(frames)], 'contact': contact, 'kind': kind, 'clips': seq}
            if not a.dry:
                od = os.path.join(OUT, eid); os.makedirs(od, exist_ok=True)
                for i, (src, _, _) in enumerate(frames):
                    data = git_show(a.branch, f'{d}/{src}')
                    im = Image.open(io.BytesIO(data))
                    if im.size != (w, h): problems.append(f'{eid}: {src} は {im.size} (台帳の枠 {w}x{h})')
                    with open(os.path.join(od, f'{eid}_m_{mid}_{i}.png'), 'wb') as fp: fp.write(data)
                    n_png += 1
            n_move += 1
        if not a.dry and moves:
            with open(os.path.join(OUT, eid, f'{eid}_moves.json'), 'w', encoding='utf-8') as fp: json.dump(moves, fp, ensure_ascii=False, indent=1)
        n_enemy += 1
    print(f'enemies {n_enemy}, moves {n_move}, png {n_png}, misses {len(misses)}, problems {len(problems)}')
    for m in misses: print('  MISS', m)
    for p in problems: print('  ', p)

if __name__ == '__main__':
    main()
