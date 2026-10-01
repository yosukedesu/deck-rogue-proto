#!/usr/bin/env python3
"""レーン C 段1: act1_layout_w5.json (W5 = e30c510 の写し) と act1_layout_r2t.json (試しの設計図) を作る。
計画 docs/design/hd2d-round2-plan-2026-10-01.md §2 レーン C 段1 の 0・1。既定の act1_layout.json は触らない。
書式は今のファイルと同じ (layoutio.dump_layout。e30c510 の act1_layout.json をバイトまで同じに書き戻せることを確かめてある)"""
import copy
import json
import os
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from layoutio import dump_layout  # noqa: E402

REPO = '/home/yosuke/projects/deck-rogue-proto'
OUT = REPO + '/unity/Assets/Resources/Stage/'
W5_COMMIT = 'e30c510'

# 坑口の櫓の新しい置き場 (place.py で解いた: s 40 の地面 (段3 の天面 4.5) で、PC の画面の x の中心が 175 = 霧の帯の外の左)
RIG_T, RIG_S = -3.09, 40.0
# 外す柵 = 坑口の櫓の横の2つ (新しいカメラで霧の帯の真ん中 = PC 行 295〜348・x 716〜910 と 1124〜1305。後者は狼の意図の札に 1,628px 掛かる)
FENCES_OUT = [(4.4, 17.2), (11.9, 17.6)]
BACKDROP_SCALE = 110.0   # 32 ドット × 1.28 unit × 110 = 幅 140.8 unit。深さ 140 で画面の横 (16:9 96.8・スマホ 2.17 で 118.1・21:9 で 129.0 unit) を覆う


def main():
    txt = subprocess.check_output(['git', '-C', REPO, 'show', W5_COMMIT + ':unity/Assets/Resources/Stage/act1_layout.json']).decode()
    W5 = json.loads(txt)
    assert dump_layout(W5) == txt, '書式の写しが合わない'

    # ---- 0. W5 の写し (以後触らない)
    w5 = copy.deepcopy(W5)
    w5['_doc'] = W5['_doc'] + ('【このファイルは W5 (%s) の act1_layout.json の写し。二周目 (計画 docs/design/hd2d-round2-plan-2026-10-01.md §2 レーン C 段1) が'
                              '比べる用・戻す用に残した = 光の設計図の layout キー (値 act1_layout_w5。look_act1_w5 が指す) で組む。既定では読まない。以後触らない】' % W5_COMMIT)
    open(OUT + 'act1_layout_w5.json', 'w', encoding='utf-8').write(dump_layout(w5))

    # ---- 1. 試しの設計図 = W5 の配置の写し + 奥の端の直しだけ
    L = copy.deepcopy(W5)
    L['_doc'] = W5['_doc'] + (
        '【二周目の試しの設計図 (計画 docs/design/hd2d-round2-plan-2026-10-01.md §2 レーン C 段1・§3-2 の試しのビルド)。W5 (act1_layout_w5.json) の配置の写しに、奥の端の直しだけ: '
        '(1) 段3 (terrace-3) の奥 back 46→70 (天面 4.5 は W5 のまま) '
        '(2) カメラに付く背景の板 = 額縁 backdrop (source backdropPlain・32 ドットの無地・flat・深さ 140・scale %g)。flat の source は足元の中心が原点 (DioramaMesh.Card) なので、'
        'vy 0.5 = 板の下の辺が画面の真ん中の行・そこから上を全部覆う (下の半分は地面が覆う)。距離の霧で霧の色に満ち、霧の芯 (lobe) で真ん中が明るく端が暗くなる '
        '(3) 坑口の櫓を霧の帯の外へ (t 8.2・s 19.8 → t %g・s %g = PC の画面 x 123〜225) '
        '(4) 坑口の櫓の横の柵2つ (t 4.4・s 17.2 と t 11.9・s 17.6) を外した。'
        '光の設計図 look_act1_r2tA〜C の layout キー (値 act1_layout_r2t) で組む。既定では読まない。'
        '配置の計算は scratchpad/hd2d/r2/lane-C/place.py】' % (BACKDROP_SCALE, RIG_T, RIG_S))
    # (1) 段3 の奥
    t3 = [p for p in L['parts'] if p['kind'] == 'slab' and p.get('name') == 'terrace-3']
    assert len(t3) == 1 and t3[0]['back'] == 46.0
    t3[0]['back'] = 70.0
    # (3) 櫓
    rig = [p for p in L['parts'] if p['kind'] == 'rig']
    assert len(rig) == 1 and (rig[0]['t'], rig[0]['s']) == (8.2, 19.8)
    rig[0]['t'] = RIG_T
    rig[0]['s'] = RIG_S
    # (4) 柵 (種類と位置で探す。seed は全部の部品に書いてあるので、行を消しても後ろの部品の形は変わらない)
    before = len(L['parts'])
    keep = []
    removed = []
    for p in L['parts']:
        if p['kind'] == 'fence' and (p.get('t'), p.get('s')) in FENCES_OUT:
            removed.append(p)
            continue
        keep.append(p)
    assert len(removed) == 2, removed
    for p in keep:
        if p['kind'] in ('rock', 'block', 'fence', 'marker', 'tree', 'rig', 'card', 'relief', 'litter'):
            assert 'seed' in p, p   # 番号から種を作る部品が無いこと (行を消すと形が変わるので)
    L['parts'] = keep
    # (2) 背景の板
    assert 'backdropPlain' not in L['sources']
    L['sources']['backdropPlain'] = {
        '_doc': '二周目 レーン C 段1 (計画 §2 レーン C・K2・K10): カメラの奥 (深さ 140) に敷く背景の板の絵 (レーン D の relief/backdrop_plain = 32×32 の不透明の無地・色表の暗い紺緑 #204043)。'
                '霧で上書きされるので色は目立たない。flat = 平らな札 (半立体にしない = 額縁の depth キーが膨らみの比に読まれて板がカメラへ膨らむのを避ける)',
        'art': ['Art/stage/act1/relief/backdrop_plain'],
        'depth': 0.0,
        'cells': 8,
        'flat': True,
    }
    L['parts'].append({
        'kind': 'frame', 'name': 'backdrop', 'src': 'backdropPlain', 'vx': 0.5, 'vy': 0.5, 'depth': 140.0, 'scale': BACKDROP_SCALE, 'shadow': False,
        '_doc': 'カメラに付く背景の板 (奥の端を霧に沈める)。flat の札は足元の中心が原点なので vy 0.5 = 板の下の辺が画面の真ん中の行。幅 140.8 unit・高さ 140.8 unit',
    })
    open(OUT + 'act1_layout_r2t.json', 'w', encoding='utf-8').write(dump_layout(L))
    print('w5 parts', len(w5['parts']), '/ r2t parts', len(L['parts']), '(W5 %d − 柵 2 + 背景の板 1)' % before)


if __name__ == '__main__':
    main()
