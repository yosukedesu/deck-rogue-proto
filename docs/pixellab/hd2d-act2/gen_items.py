#!/usr/bin/env python3
"""docs/pixellab/hd2d-act2/gen_items.py — 幕2「先代の坑道」の箱庭の絵をそろえる (2026-10-03 HD-2D 段2 レーン D。PixelLab は使わない＝生成 0 回)。

計画 docs/design/hd2d-stage2-plan-2026-10-02.md §2 D・約束 docs/design/hd2d-stage2/contracts.md C4・分析書 §9。
今ある絵 (unity/Assets/Resources/Art/props/act2_*・Art/tiles/act2_*) は読むだけ。出力は新しい置き場 Art/stage/act2/ だけ。
幕1 の道具 (scripts/stage-palette.py・tile-calm.py・sprite-normals.py・docs/pixellab/hd2d-act1/gen_art.py・relief-clean.py) は
import して使う (1 文字も変えない)。gen_art は import した後に PAL・PALRGB・EARTH などを幕2 の色表へ差し替えて呼ぶ。

流れ (`all` が全部を順に回す。種は固定＝同じ入力なら同じ絵):
  1. calm    : PixelLab のタイル (cliff・stone・plank・wood) を tile-calm で静かな面に (色表へ写す前の下書き → 作業場)
  2. relief  : 既存の小物 42 枚の足元と皿を切る・等角投影の物を仕分ける (下書き → 作業場)
  3. palette : 1・2 の色 ＋ 発光の差し色 ＋ 本家 ot2 の錨 (暗部と灯の真下の無彩色の床) から色表 palette-act2.json を作る
  4. map     : 1・2 を色表へ写して Art/stage/act2/{tiles,relief}/ へ
  5. gen     : コード生成のタイル (座席の土 top_earth_seat・段の土 top_earth/side_earth・鉄 side_iron/top_iron) と小札 litter/
  6. normals : 半立体の法線 <名前>_n.png (sprite-normals の normals-stage と同じ関数)・発光 <名前>_e.png (炉・提灯・結晶・鉱)
  7. index   : index.json・keyflip.json
  8. lint    : 全部の PNG の大きさ・半透明・面積・色表からの距離・タイルのざらつきの表 (work/lint.md・lint.json)

使い方:
  python3 -B docs/pixellab/hd2d-act2/gen_items.py all [--work <dir>] [--sheet <png>]
  python3 -B docs/pixellab/hd2d-act2/gen_items.py lint                      # 点検だけ (Art/stage/act2 の今の絵)
  python3 -B docs/pixellab/hd2d-act2/gen_items.py sheet --out <png>         # 全部の絵を並べたシート
  python3 -B docs/pixellab/hd2d-act2/gen_items.py honke <ot_921570_2.jpg>   # 本家の錨の色を測り直す (結果は HONKE_* に手で写す)
本家の画はリポジトリに入れない (~/.cache の中だけ)。色の錨は下の HONKE_* に数字だけ書いてある (測り方は honke)。
"""
import argparse
import importlib.util
import json
import os
import shutil
import sys
import tempfile

import numpy as np
from PIL import Image, ImageDraw

sys.dont_write_bytecode = True   # docs/ と scripts/ に __pycache__ を作らない

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
ART = os.path.join(REPO, 'unity', 'Assets', 'Resources', 'Art')
PROPS = os.path.join(ART, 'props')
SRC_TILES = os.path.join(ART, 'tiles')
OUT = os.path.join(ART, 'stage', 'act2')
PAL_BASE = os.path.join(HERE, 'palette-act2')          # → .json と見本の .png
PAL_JSON = PAL_BASE + '.json'
WORK_DOCS = os.path.join(HERE, 'work')                  # 点検の表だけ (PNG の下書きは作業場 = --work)


def _load(modname, path):
    spec = importlib.util.spec_from_file_location(modname, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


SP = _load('stage_palette', os.path.join(REPO, 'scripts', 'stage-palette.py'))
TC = _load('tile_calm', os.path.join(REPO, 'scripts', 'tile-calm.py'))
SN = _load('sprite_normals', os.path.join(REPO, 'scripts', 'sprite-normals.py'))
RC = _load('relief_clean_act1', os.path.join(REPO, 'docs', 'pixellab', 'hd2d-act1', 'relief-clean.py'))
GA = _load('gen_art_act1', os.path.join(REPO, 'docs', 'pixellab', 'hd2d-act1', 'gen_art.py'))


# ================================================================ 本家 ot2 の色の錨 (2026-10-03 に honke で測った・絵には入れない)
# 測った所 (1920×1080 の画素): 暗部 = 右の壁 (1350,250)-(1650,600)・鍾乳石のあたり・上端・手前の岩 → k-means 5 の暗い 4 つ。
# 床 = 灯の真下と真ん中の床 (900,780)-(1400,900) ほか → k-means 5。分析書の数字 (21,44,50)・(105,105,110) もそのまま足す。
HONKE_DARK = [(3, 21, 23), (11, 43, 53), (21, 44, 50), (34, 61, 69), (63, 81, 90)]
HONKE_FLOOR = [(25, 48, 48), (55, 72, 73), (92, 96, 93), (105, 105, 110), (115, 112, 108), (139, 129, 122)]
# 床の灰白の段: 本家の床の暖かい灰 (115,112,108)・(139,129,122) の色相のまま明るさを 9 段に伸ばした物 (コード生成の床と段の土が使う。
# 既存の小物とタイルには無彩色の灰の段がほとんど無く、近い色を選ぶと青か紫に寄った＝灯の真下が無彩色に読めない)
FLOOR_RAMP = [(46, 45, 44), (56, 55, 53), (66, 65, 63), (80, 79, 76), (94, 93, 90), (106, 105, 102), (118, 115, 110), (132, 127, 120), (146, 140, 132)]


def honke_measure(path):
    """本家 ot2 の暗部と床の色を k-means で測る (HONKE_* を作った手順。結果を表示するだけ)"""
    a = np.array(Image.open(path).convert('RGB'))

    def px(rects):
        return np.concatenate([a[y0:y1, x0:x1].reshape(-1, 3) for x0, y0, x1, y1 in rects])
    regions = {
        'dark': px([(1350, 250, 1650, 600), (1500, 150, 1700, 300), (900, 0, 1400, 90), (1000, 980, 1800, 1080)]),
        'floor': px([(900, 780, 1400, 900), (1100, 650, 1500, 740), (1100, 760, 1800, 900)]),
    }
    for name, P in regions.items():
        q = (P // 4) * 4 + 2
        u, c = np.unique(q, axis=0, return_counts=True)
        lab = SP.rgb8_to_oklab(u)
        sc = np.array([1.5, 1.0, 1.0])
        cen = SP.weighted_kmeans(lab * sc, c.astype(float), 5) / sc
        rgb = SP.oklab_to_rgb8(cen)
        o = np.argsort(cen[:, 0])
        print(name, [tuple(int(v) for v in rgb[i]) for i in o])


# ================================================================ 小さな道具
def hsv(a):
    rgb = a[..., :3].astype(np.float64) / 255.0
    mx, mn = rgb.max(-1), rgb.min(-1)
    d = np.maximum(mx - mn, 1e-9)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    h = np.where(mx == r, ((g - b) / d) % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) * 60.0
    s = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-9), 0.0)
    return h, s, mx


def luma(rgb):
    rgb = np.asarray(rgb, dtype=np.float64)
    return 0.299 * rgb[..., 0] + 0.587 * rgb[..., 1] + 0.114 * rgb[..., 2]


def load_rgba(path):
    return np.array(Image.open(path).convert('RGBA'))


def save(img, path, mode=None):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    if mode is None:
        mode = 'RGBA' if img.shape[-1] == 4 else 'RGB'
    Image.fromarray(np.ascontiguousarray(img).astype(np.uint8), mode).save(path, optimize=True)
    return path


# ================================================================ 1. タイル (PixelLab の元を静かな面に)
# 名前: (元の絵と手直しの並び, contrast の出発点, 下限, 彩度, 明るさの足し (Oklab L), 門 (ラプラシアン分散), 回し方, 理由)
# 手直し: 'flipY' = 上下反転 / 'boards:<順>' = 板の列を並べ替える (板の継ぎ目の列から列までを 1 枚とみなす) /
#         'rot90' = 90° 回す。板と材木は元が 1 枚しか無いので、並べ替えと反転で 4 種にする (実行時の flipX とは別の絵になる)
PLANK_BOARDS = [0, 16, 32, 48]            # act2_plank の板の継ぎ目の列 (暗い縦の線。幅 16 の板 4 枚)
WOOD_BOARDS = [10, 38, 57]                # act2_wood の板の継ぎ目の列 (幅 28・19・17。最後の板は右端から左端へ回り込む)
CALM = {
    'side_cliff': dict(src=[('act2_cliff_a', ''), ('act2_cliff_b', ''), ('act2_cliff_a', 'flipY'), ('act2_cliff_d', '')],
                       contrast=1 / 3, min=0.2, sat=0.8, lift=0.0, maxlap=800, rot='flipX', face='side',
                       note='壁・岩棚の側面。Art/tiles/act2_cliff_a・b・d を tile-calm (明暗 1/3 から、ざらつき 640 を超えれば 1/5 まで自動で絞る・彩度 0.8)。'
                            'act2_cliff_c は小さな丸い石が詰まった「泡状の石」(集合体の柄) なので使わず、c は cliff_a の上下反転'),
    'top_cliff': dict(src=[('act2_cliff_b', 'flipY'), ('act2_cliff_a', 'flipY'), ('act2_cliff_d', 'flipY'), ('act2_cliff_b', '')],
                      contrast=0.25, min=0.15, sat=0.6, lift=0.05, maxlap=800, rot='rot4', face='top',
                      note='岩棚の天面。側面と同じ岩 (cliff_c は使わない) を上下反転などで 1/4 に絞り、砂ぼこりで少し明るく (L +0.05)・彩度 0.6 (本家の段は天面が立面より明るい)'),
    'top_stone': dict(src=[('act2_stone_a', ''), ('act2_stone_b', ''), ('act2_stone_c', 'flipY'), ('act2_stone_d', '')],
                      contrast=0.25, min=0.15, sat=0.8, lift=0.0, maxlap=800, rot='flipX', face='top',
                      note='一段目の敷石 (宿場の石畳)。Art/tiles/act2_stone_a〜d を calm 0.25。stone_c は stone_a と同じ絵なので上下反転して別の種に。'
                           '石が横の列に並ぶ絵なので rot4 で回さない (flipX だけ)'),
    'side_plank': dict(src=[('act2_plank', ''), ('act2_plank', 'boards:2,3,0,1|flipY'), ('act2_plank', 'boards:1,3,0,2'), ('act2_plank', 'boards:3,0,2,1|flipY')],
                       contrast=0.4, min=0.2, sat=0.9, lift=0.0, maxlap=800, rot='none', face='side',
                       note='板張り (歩廊の囲い・棚の面)。Art/tiles/act2_plank (1 枚) の板の列を並べ替え・上下反転して 4 種。'
                            '板の継ぎ目の暗い列がいつも左端 (0 列) と 16・32・48 列に来るので回さない (flipX で左右を反転すると継ぎ目が 63 列へ移り、'
                            '隣のタイルとの間で継ぎ目が 2 テクセル幅になる所と、継ぎ目が消えて幅 32 の板になる所がでたらめに出る)'),
    'top_plank': dict(src=[('act2_plank', 'boards:1,2,3,0'), ('act2_plank', 'boards:3,1,0,2|flipY'), ('act2_plank', 'boards:2,0,1,3'), ('act2_plank', 'flipY')],
                      contrast=0.3, min=0.18, sat=0.85, lift=0.03, maxlap=800, rot='none', face='top',
                      note='歩廊の床。板は縦 (テクスチャの v＝天面では奥行き s の向き＝歩廊を横切る板)。側面より 1 段静かに・少し明るく (踏まれて擦れた面)。'
                           '継ぎ目が左端の列に来るので side_plank と同じく回さない'),
    'side_timber': dict(src=[('act2_wood', ''), ('act2_wood', 'boards:1,0,2|flipY'), ('act2_wood', 'boards:2,1,0'), ('act2_wood', 'boards:0,2,1|flipY')],
                        contrast=0.45, min=0.25, sat=0.9, lift=-0.02, maxlap=800, rot='flipX', face='side',
                        note='支保工の柱・梁・枕木の側面。Art/tiles/act2_wood (1 枚) の板の列を並べ替え・上下反転して 4 種。少し暗く (古い坑木)。'
                             '板の継ぎ目は tile-calm の後に中の列 (4〜60) へ散って左右の端には来ないので、flipX で反転しても継ぎ目は 2 重にも消えもしない '
                             '(幕1 の side_wood と同じ flipX。回さない none でもよい)。'
                             '10/7 に PixelLab の act2_side_timber で差し替える予定 (発注書の必須 1)'),
    'top_timber': dict(src=[('act2_wood', 'boards:2,0,1'), ('act2_wood', 'boards:1,2,0|flipY'), ('act2_wood', 'flipY'), ('act2_wood', 'boards:0,2,1')],
                       contrast=0.35, min=0.2, sat=0.85, lift=0.0, maxlap=800, rot='flipX', face='top',
                       note='柱・梁・枕木の天面。木目は縦 (天面では奥行き s の向き＝枕木の長さの向き)。継ぎ目は端に来ないので flipX でよい (side_timber と同じ)'),
}


def _boards(a, seams, order):
    """板の継ぎ目の列 seams で切った板を order の順に並べ直す (最後の板は右端から左端へ回り込む)"""
    w = a.shape[1]
    cols = []
    for i, s in enumerate(seams):
        e = seams[i + 1] if i + 1 < len(seams) else seams[0] + w
        cols.append([(x % w) for x in range(s, e)])
    idx = [c for k in order for c in cols[k]]
    # 左端は必ず板の継ぎ目の列。ただし tile-calm の make_seamless (半分ずらしたキルティング) の後も左端に残るのは、
    # 継ぎ目が 16 列おきの act2_plank だけ (32 ずらしても継ぎ目の列に重なる)。act2_wood (幅 28・19・17) は継ぎ目が中へ散る
    return a[:, idx]


def _variant(a, how, src):
    for step in [s for s in how.split('|') if s]:
        if step == 'flipY':
            a = a[::-1]
        elif step == 'rot90':
            a = np.rot90(a)
        elif step.startswith('boards:'):
            order = [int(v) for v in step[7:].split(',')]
            seams = PLANK_BOARDS if 'plank' in src else WOOD_BOARDS
            a = _boards(a, seams, order)
        else:
            sys.exit('知らない手直し: ' + step)
    return np.ascontiguousarray(a)


def _lift(tile, dl, sat=1.0):
    """Oklab の L を dl だけ足し、a・b を sat 倍 (砂ぼこり・擦れた面)"""
    if dl == 0 and sat == 1.0:
        return tile
    lab = SP.rgb8_to_oklab(tile[..., :3].astype(np.float64))
    lab[..., 0] += dl
    lab[..., 1] *= sat
    lab[..., 2] *= sat
    out = tile.copy()
    out[..., :3] = SP.oklab_to_rgb8(lab)
    return out


def make_calm(work):
    """タイルを tile-calm で静かな面に (色表へ写す前の下書き。作業場 work/calm/<名前>_<a..d>.png)。戻り値: 点検の表"""
    res = {}
    d = os.path.join(work, 'calm')
    os.makedirs(d, exist_ok=True)
    for name, spec in CALM.items():
        for i, (src, how) in enumerate(spec['src']):
            v = 'abcd'[i]
            a = load_rgba(os.path.join(SRC_TILES, src + '.png'))
            if a.shape[:2] != (64, 64):
                sys.exit(f'{src} は 64×64 でない')
            t = _variant(a, how, src)
            seed = (sum(map(ord, name + v)) * 7919) % (2 ** 31)
            o, nspk, used = TC.calm(t, spec['contrast'], spec['sat'], 12, 0.10, 14.0, seed,
                                    target_lap=spec['maxlap'] * 0.8, min_contrast=spec['min'])
            o = _lift(o, spec['lift'])
            path = save(o, os.path.join(d, f'{name}_{v}.png'))
            c = TC.verdict(TC.check(o), spec['maxlap'])
            c['src'] = f'Art/tiles/{src}' + (f' ({how})' if how else '')
            c['contrastUsed'] = round(used, 3)
            c['before'] = TC.check(t)['lapvar']
            res[f'{name}_{v}'] = c
            print(f'{name}_{v}: {"OK" if c["pass"] else "NG " + ",".join(c["fail"])}  lapvar {c["lapvar"]} (元 {c["before"]})  contrast {used:.3f}  ← {c["src"]}')
    return res


# ================================================================ 2. 半立体 (既存の小物の足元と皿を切る)
# 区分: keep = そのまま (色表へ写すだけ。薄い影の皿だけ切る物あり) / cut = 足元と皿を切る (relief-clean の手直し) /
#       iso = 等角投影 (low top-down) で頼んだ物。使う物は足元を切り、使わない物は held (10/7 まで置かない) か discard (捨てる)
# 手直し (どれも省略可。上から順に):
#   band=(y0, x0, x1)  : 行 y0 から下で、列 x0〜x1 の外を透明に (皿は本体より横に広い)
#   ground=(y0, 種類)  : 行 y0 から下で、地面の色の画素を透明に (並べて書けば順に)。種類 sand=砂と黄土 / grass=草の緑 /
#                        weed=草の暗い縁取り (暗い青緑) / dirt=茶色の土 / rock=灰紫の岩 (結晶の根元) / shadow=色みの無い灰の影の帯 / soil=暗い土 (彩度の低い暗い茶と紫)
#   below=y            : 行 y から下を全部透明に (本体の下端)
#   rects=[(x0,y0,x1,y1)] : 矩形を透明に (はみ出た欠片)
# 最後に relief-clean の drop_specks (6 画素未満・最大の塊の 3% 未満の欠片) と trim (外接の四角＋余白 2) を通す。
RELIEF = [
    # ---- cut (16 枚): 足元と皿を切る
    ('barrel_stack', 'cut', dict(band=(37, 9, 38), ground=(40, 'sand'), below=42), '樽の山。下の砂の皿を切る (樽の下端の箍は残す)'),
    ('crate_stack', 'cut', dict(ground=(30, 'grass'), band=(34, 10, 37), below=45), '木箱の山。下の草の台を切る'),
    ('crystal_big', 'cut', dict(ground=(74, 'rock+grass')), '大結晶。根元の岩と苔を切る (岩は 3D の rock に任せる)。結晶の明るい所は _e.png'),
    ('gear_big', 'cut', dict(ground=(27, 'grass+sand+weed'), band=(29, 6, 33), below=35), '大歯車。足元の草と砂を切る'),
    ('gear_pile', 'cut', dict(ground=(11, 'grass'), below=25), '歯車の山。間の草と下の灰の地面の帯を切る'),
    ('hearth', 'cut', dict(ground=[(66, 'grass+sand'), (70, 'soil')], band=(68, 8, 71), below=72), '炉。下の砂の皿と草を切る (鍋とスコップは残す)。炎は _e.png・暖色は舞台の点光源'),
    ('lumber', 'cut', dict(ground=(0, 'grass'), band=(19, 16, 66), below=26), '材木。まわりの草と下の地面の帯を切る'),
    ('minecart', 'cut', dict(band=(29, 13, 51), ground=(29, 'sand+dirt'), below=36), 'トロッコ。下の軌道と土の皿を切る (レールは 3D の rail)。鉱の結晶は _e.png'),
    ('minecart_tipped', 'cut', dict(band=(23, 11, 55), ground=(24, 'sand'), below=26), '倒れたトロッコ。下の砂の皿を切る'),
    ('sacks', 'cut', dict(band=(20, 7, 40), below=23), '袋。下の砂の皿を切る (袋と砂が同じ色なので行で切る)'),
    ('shelter', 'cut', dict(ground=(38, 'grass'), band=(45, 16, 79), below=50), '差し掛け。足元の草と土を切る (3D の rig＋布が本命・これは予備)'),
    ('stall_a', 'cut', dict(ground=(56, 'grass+sand'), band=(56, 11, 86), below=65), '屋台。足元の草と土の皿を切る (3D の rig＋布が本命・これは予備)'),
    ('stall_b', 'cut', dict(ground=(40, 'grass'), band=(45, 16, 72), below=53), '小屋。足元の草と土を切る (3D の block が本命・これは予備)'),
    ('toolrack', 'cut', dict(ground=(41, 'grass+sand'), band=(46, 10, 40), below=51), '道具掛け。足元の土の皿と草を切る'),
    ('winch', 'cut', dict(ground=(44, 'grass+sand'), band=(47, 12, 61), below=52), '巻き上げ機。足元の砂の皿と草を切る'),
    ('wreck', 'cut', dict(ground=(48, 'grass+sand'), band=(50, 9, 84), below=57), '機械の残骸。足元の砂と草を切る'),
    # ---- keep (13 枚): そのまま (色表へ写すだけ)。薄い皿・影の帯だけ切る物は手直しを書いた
    ('bell_post', 'keep', dict(ground=(56, 'grass'), below=61), '鐘の柱。根元の草の芽だけ切る'),
    ('crate', 'keep', dict(), '木箱'),
    ('crate_open', 'keep', dict(band=(23, 6, 33), below=27), '開いた木箱。下の薄い土の皿を切る'),
    ('crystal', 'keep', dict(), '小結晶 (根元の暗い岩ごと)。結晶の明るい所は _e.png'),
    ('lantern_hang', 'keep', dict(), '吊り提灯 (光が描き込み)。ガラスは _e.png。10/7 に光の無い提灯で差し替えるか決める'),
    ('lantern_hang_b', 'keep', dict(), '吊り提灯 (小)。ガラスは _e.png'),
    ('ore_pile', 'keep', dict(), '鉱の山。鉱の結晶は _e.png'),
    ('pulley', 'keep', dict(), '滑車 (吊り物)'),
    ('roots_hang', 'keep', dict(), '垂れ根 (吊り物・上に苔の塊)'),
    ('stalactite_a', 'keep', dict(), '鍾乳石 (吊り物)'),
    ('trough', 'keep', dict(ground=(23, 'shadow'), below=26), '水桶。下の灰の影の帯を切る'),
    ('tunnel_side', 'keep', dict(ground=(70, 'grass+sand+weed'), band=(73, 14, 63), below=91), '脇坑の枠。足元の草と砂を切る (下端は行で平らに)'),
    ('rope_coil', 'discard', dict(), '縄の輪は却下済みの柄 (art-bible §8-3「縄の輪も捨てる」)'),
    # ---- iso (13 枚): 等角投影で頼んだ物
    ('ladder', 'iso', dict(ground=(72, 'shadow'), below=74), '梯子。ほぼ正面の絵なので足元の灰の影だけ切って使う (3D の fence/rig で組むなら要らない)'),
    ('lantern', 'iso', dict(), '壁掛けの提灯 (光が描き込み・等角)。ガラスは _e.png。10/7 の lantern_head が来るまでの代わり'),
    ('lantern_post', 'iso', dict(below=80), '灯柱。等角の台座 (下の円盤) を切って柱だけ使う。灯は _e.png。3D の marker＋頭の絵が本命'),
    ('roots', 'iso', dict(), '垂れ根の束 (吊り物は向きの問題が無い)'),
    ('stalactite', 'iso', dict(), '鍾乳石の束 (吊り物は向きの問題が無い)'),
    ('tunnel_mouth', 'iso', dict(below=113), '坑口の枠。柱の台の下 (砂と岩) を行で切る。3D (marker×2＋block の梁＋黒い card) が本命・これは予備'),
    ('barrel', 'held', dict(), '単体の樽は等角投影 (天面と台座が見える) しか無い。10/7 の barrel_front まで置かない (樽の山 barrel_stack で代わる)'),
    ('post_brace', 'held', dict(), '支保工の柱は 3D (block/rig) で組む。絵は等角で縄の巻きつき (却下の柄に近い)。置かない'),
    ('gear', 'discard', dict(), '等角の小歯車。gear_big と小札の gear_* で足りる'),
    ('rail_a', 'discard', dict(), 'レールは 3D の rail で組む (寝かせた札は潰れる)'),
    ('rail_b', 'discard', dict(), 'レールは 3D の rail で組む'),
    ('rubble_rock', 'discard', dict(), '岩は 3D の rock (低ポリの多面体)'),
    ('stall', 'discard', dict(), '旧の屋台 (等角・砂の台)。stall_a/b に置き換え済み'),
]


def _ground_mask(a, kinds):
    h, s, v = hsv(a)
    al = a[..., 3] >= 128
    m = np.zeros(al.shape, bool)
    for k in kinds.split('+'):
        if k == 'sand':      # 砂と黄土 (明るい黄土〜淡い橙)
            m |= (h >= 12) & (h <= 52) & (s >= 0.18) & (v >= 0.42)
        elif k == 'grass':   # 草の緑 (結晶の青緑は 160° より上なので外す)
            m |= (h >= 50) & (h <= 160) & (s >= 0.18)
        elif k == 'rock':    # 青紫〜灰紫の岩 (結晶の根元。色相 195〜290°か色みの無い灰)。結晶は緑〜青緑 (90〜185°) なので残る
            m |= ((h >= 195) & (h <= 290)) | (s < 0.12)
        elif k == 'shadow':  # 色みの無い灰の影の帯
            m |= s < 0.15
        elif k == 'soil':    # 暗い土 (彩度の低い暗い茶と紫)
            m |= (v < 0.45) & (s < 0.55) & ~((h >= 150) & (h <= 200))
        elif k == 'dirt':    # 茶色の土 (暗めの黄土〜茶。トロッコの車輪の紺は残る)
            m |= (h >= 8) & (h <= 40) & (s >= 0.3) & (v >= 0.22) & (v <= 0.62)
        elif k == 'weed':    # 草の暗い縁取り (暗い青緑。grass の 160° より上で明るさの低い物)
            m |= (h >= 140) & (h <= 200) & (s >= 0.2) & (v < 0.42)
        else:
            sys.exit('知らない地面の種類: ' + k)
    return m & al


def clean_relief(a, ops):
    out = a.copy()
    H, W = out.shape[:2]
    if 'band' in ops:
        y0, x0, x1 = ops['band']
        sl = out[y0:]
        xs = np.arange(W)
        sl[:, (xs < x0) | (xs > x1), 3] = 0
    if 'ground' in ops:
        for y0, kinds in (ops['ground'] if isinstance(ops['ground'], list) else [ops['ground']]):
            m = _ground_mask(out, kinds)
            m[:y0] = False
            out[m, 3] = 0
    if 'below' in ops:
        out[ops['below']:, :, 3] = 0
    for (x0, y0, x1, y1) in ops.get('rects', []):
        out[y0:y1, x0:x1, 3] = 0
    out[out[..., 3] < 128] = 0          # 半透明は作らない (α は 0 か 255)
    out[out[..., 3] >= 128, 3] = 255
    out = RC.drop_specks(out)
    al = out[..., 3] >= 128
    ys, xs = np.nonzero(al)
    off = (max(0, int(xs.min()) - 2), max(0, int(ys.min()) - 2))   # RC.trim の余白 2 と同じ = 元の絵の座標からの引き算
    return RC.trim(out), off


def make_relief(work):
    """作業場 work/relief/<名前>.png (色表へ写す前)。戻り値: 名前 → 記録"""
    d = os.path.join(work, 'relief')
    os.makedirs(d, exist_ok=True)
    rec = {}
    for name, group, ops, why in RELIEF:
        src = os.path.join(PROPS, 'act2_' + name + '.png')
        a = load_rgba(src)
        r = dict(group=group, src='Art/props/act2_' + name, why=why, srcDots=[int(a.shape[1]), int(a.shape[0])])
        if group in ('held', 'discard'):
            rec[name] = r
            continue
        o, off = clean_relief(a, ops)
        save(o, os.path.join(d, name + '.png'))
        r['dots'] = [int(o.shape[1]), int(o.shape[0])]
        r['offset'] = list(off)   # 切り詰めで動いた量 (元の絵の座標 − この値 = 切った後の座標。発光の範囲 rect を写す)
        r['ops'] = {k: v for k, v in ops.items()}
        rec[name] = r
        print(f'{name:16s} {group:7s} {a.shape[1]}×{a.shape[0]} → {o.shape[1]}×{o.shape[0]}  {why}')
    return rec


# ================================================================ 6b. 発光 (炉・提灯・結晶・鉱)
# 帯は sprite-normals の PRESETS と同じ形 (色相 度・彩度・明るさ)。rect = 光らせてよい範囲 (無ければ全体)
EMISSION = {
    'hearth': dict(bands=[{'name': '炎', 'hue': (0, 58), 'sat': (0.45, 1.0), 'val': (0.62, 1.0)}], rect=(20, 44, 58, 70)),
    'crystal': dict(bands=[{'name': '結晶', 'hue': (130, 200), 'sat': (0.12, 1.0), 'val': (0.62, 1.0)}]),
    'crystal_big': dict(bands=[{'name': '結晶', 'hue': (130, 200), 'sat': (0.12, 1.0), 'val': (0.62, 1.0)}]),
    'ore_pile': dict(bands=[{'name': '鉱の結晶', 'hue': (140, 200), 'sat': (0.2, 1.0), 'val': (0.55, 1.0)}]),
    'minecart': dict(bands=[{'name': '鉱の結晶', 'hue': (140, 200), 'sat': (0.25, 1.0), 'val': (0.5, 1.0)}]),
    'lantern': dict(bands=[{'name': 'ガラスの灯', 'hue': (0, 58), 'sat': (0.35, 1.0), 'val': (0.7, 1.0)}]),
    'lantern_hang': dict(bands=[{'name': 'ガラスの灯', 'hue': (20, 60), 'sat': (0.2, 1.0), 'val': (0.66, 1.0)}]),
    'lantern_hang_b': dict(bands=[{'name': 'ガラスの灯', 'hue': (20, 70), 'sat': (0.15, 1.0), 'val': (0.6, 1.0)},
                                  {'name': 'ガラスの照り', 'hue': (0, 360), 'sat': (0.0, 0.2), 'val': (0.85, 1.0)}]),
    'lantern_post': dict(bands=[{'name': 'ガラスの灯', 'hue': (12, 50), 'sat': (0.3, 1.0), 'val': (0.5, 1.0)}]),
}


def emission_of(name, a, off=(0, 0)):
    """発光のマスク。rect は元の絵 (Art/props/act2_*) の座標で書き、off (切り詰めで動いた量) を引いて当てる"""
    spec = EMISSION[name]
    m, cnt = SN.emission_mask(a, spec['bands'])
    if 'rect' in spec:
        x0, y0, x1, y1 = spec['rect']
        x0, x1, y0, y1 = x0 - off[0], x1 - off[0], y0 - off[1], y1 - off[1]
        keep = np.zeros(a.shape[:2], bool)
        keep[y0:y1, x0:x1] = True
        m[~keep, :3] = 0
    return m


# ================================================================ 3. 色表
def build_palette(work, rec, k_bulk=36, k_accent=8):
    """色表 = (1) calm 済みタイルと切った後の小物の色 (画素数の重み・Oklab・明るさ 1.5 倍の k-means 36)
    ＋ (2) 発光の差し色 (炎・提灯・結晶。画素数の平方根の重みの k-means 8＝少ない画素でも残る)
    ＋ (3) 本家 ot2 の錨 (暗部と灯の真下の無彩色の床 11) ＋ 床の灰白の段 9。同じ色に丸まった物を落として明るさ順に並べる
    (stage-palette の形で保存。合計 64 以下)"""
    files = sorted([os.path.join(work, 'calm', f) for f in os.listdir(os.path.join(work, 'calm'))]) + \
        sorted([os.path.join(work, 'relief', f) for f in os.listdir(os.path.join(work, 'relief'))])
    px = np.concatenate([SP.opaque_pixels(load_rgba(f))[0] for f in files], 0)
    uniq, cnt = np.unique(px.reshape(-1, 3), axis=0, return_counts=True)
    sc = np.array([1.5, 1.0, 1.0])
    lab = SP.rgb8_to_oklab(uniq)
    bulk = SP.oklab_to_rgb8(SP.weighted_kmeans(lab * sc, cnt.astype(np.float64), k_bulk) / sc)
    # 差し色: 発光の帯に入る画素だけ
    acc = []
    for name in EMISSION:
        p = os.path.join(work, 'relief', name + '.png')
        if not os.path.exists(p):
            continue
        a = load_rgba(p)
        m = emission_of(name, a, rec[name].get('offset', (0, 0)))
        hit = m[..., :3].max(-1) > 0
        acc.append(a[..., :3][hit])
    acc = np.concatenate(acc, 0)
    ua, ca = np.unique(acc, axis=0, return_counts=True)
    accent = SP.oklab_to_rgb8(SP.weighted_kmeans(SP.rgb8_to_oklab(ua) * sc, np.sqrt(ca.astype(np.float64)), k_accent) / sc)
    anchors = np.array(HONKE_DARK + HONKE_FLOOR + FLOOR_RAMP, dtype=np.uint8)
    rgb = np.unique(np.concatenate([bulk, accent, anchors], 0), axis=0)
    lab2 = SP.rgb8_to_oklab(rgb)
    order = np.lexsort((np.arctan2(lab2[:, 2], lab2[:, 1]), lab2[:, 0]))
    rgb = rgb[order]
    SP.save_palette(rgb, PAL_BASE)
    print(f'色表 {len(rgb)} 色 (塊 {len(bulk)}・差し色 {len(accent)}・本家の錨と床の段 {len(anchors)}) → {os.path.relpath(PAL_JSON, REPO)}')
    if len(rgb) > 64:
        sys.exit('色表が 64 色を超えた')
    return rgb


def set_palette_on_gen_art(pal):
    """gen_art (幕1) の色表と土・石の色の並びを幕2 の物へ差し替える (gen_art のファイルは触らない。import した写しの中だけ)"""
    GA.PAL = ['#%02x%02x%02x' % tuple(int(v) for v in c) for c in pal]
    GA.PALRGB = [tuple(int(v) for v in c) for c in pal]
    GA.PAL_JSON = PAL_JSON


def ramp(pal, targets):
    """目標の RGB に Oklab でいちばん近い色表の色を、並べた順に返す (同じ色を2度使わない＝段が潰れない。
    gen_art の earth_tile_r3 は隣の段の明るさの差で割るので、同じ色が続くと 0 で割る)"""
    pl = SP.rgb8_to_oklab(pal)
    out, used = [], set()
    for t in targets:
        d = ((pl - SP.rgb8_to_oklab(np.array(t))) ** 2).sum(1)
        for i in np.argsort(d):
            if int(i) not in used:
                used.add(int(i))
                out.append(tuple(int(v) for v in pal[int(i)]))
                break
    return out


# ================================================================ 4. 色表へ写す
def map_all(work, pal):
    """戻り値: 絵の名前 → 写す前の色表からの距離 p95 (ΔE_ok×100。写した後は 0)"""
    before = {}
    for sub in ('calm', 'relief'):
        dst = os.path.join(OUT, 'tiles' if sub == 'calm' else 'relief')
        os.makedirs(dst, exist_ok=True)
        for f in sorted(os.listdir(os.path.join(work, sub))):
            a = load_rgba(os.path.join(work, sub, f))
            d = SP.palette_distance(a, pal)
            before[os.path.splitext(f)[0]] = round(float(np.percentile(d, 95)), 2) if len(d) else 0.0
            o = SP.map_image(a, pal)
            if sub == 'calm':
                save(o[..., :3], os.path.join(dst, f), 'RGB')
            else:
                save(o, os.path.join(dst, f))
    return before


# ================================================================ 5. コード生成 (座席の土・段の土・鉄・小札)
def _tile_rng(seed):
    return np.random.default_rng(seed)


def sand_seat_tile(seed, C, n=64, grain=(0.04, 0.02), gw=(4, 7), gh=(3, 5), pebbles=(1, 3)):
    """座席の帯の床: 灯の真下で無彩色の灰白に読める踏み固めた砂の床 (art-bible §3-3 の幕2 の例外＝砂の粒)。
    C['g'] = 床の灰白の段 (FLOOR_RAMP の 9 段)。
      - 大きな斑: 巻き戻しの値ノイズ (4・8 マス) を 4 段 (段 4・5・6・7＝明るさ 93〜127。暗い染みにしない) に。境目は乱数のディザを入れない (1 テクセルのちらつきは
        画面では潰れて見えないのに、タイルのざらつきだけ上げる)
      - 砂の粒: 白色の乱数で置く (等間隔にならない)。1 粒 = 横 4〜6・縦 3〜4 テクセル (角を丸める) で 1 段暗いか明るい。
        見下ろし 5° で縦が約 1/8 に潰れるので、縦 1 テクセルの粒は画面に残らない。縦 3〜4 の粒が画面で 1〜2px の横の粒になる
        (本家 ot2 の床の「横に並ぶ砂の斑」)。rot4 で回っても形が同じに見えるよう縦横の差を小さく
      - 小石 1〜3 (左上が明るく右下に影・間を 22 以上空ける)
    門: タイルのざらつき ≤800 (art-bible §3-3 の奥の段と同じ値・幕1 の座席の土 top_path_seat_r3 は 715〜864)・
    画面の模型 (screen_fineness) は灯なしで 48〜54・光溜まりで 88〜98 (分析書の目標 40〜90 は撮影の値。下の SEAT_GRAIN_STEPS)"""
    rng = _tile_rng(seed)
    f = 0.6 * GA.periodic_noise(n, 4, rng) + 0.4 * GA.periodic_noise(n, 8, rng)
    f = (f - f.min()) / max(1e-6, f.max() - f.min())
    lev = np.where(f < 0.18, 4, np.where(f < 0.5, 5, np.where(f < 0.88, 6, 7)))
    base = lev.copy()
    g = rng.random((n, n))
    for y in range(n):
        for x in range(n):
            if g[y, x] < grain[0]:
                d = -1
            elif g[y, x] > 1.0 - grain[1]:
                d = 1
            else:
                continue
            ww, hh = int(rng.integers(*gw)), int(rng.integers(*gh))
            for yy in range(hh):
                x0 = 1 if (yy in (0, hh - 1) and ww > 2 and rng.random() < 0.6) else 0
                for xx in range(x0, ww - x0):
                    lev[(y + yy) % n, (x + xx) % n] = int(np.clip(base[(y + yy) % n, (x + xx) % n] + d, 0, len(C['g']) - 1))
    G = C['g']
    img = np.zeros((n, n, 3), np.uint8)
    for i, c in enumerate(G):
        img[lev == i] = c
    S = C['stone']
    for (sx, sy) in GA.poisson(n, int(rng.integers(pebbles[0], pebbles[1] + 1)), 22, rng):
        w = int(rng.integers(2, 4))
        h = 1 if (w == 2 and rng.random() < 0.5) else 2
        for yy in range(h):
            for xx in range(w):
                img[(sy + yy) % n, (sx + xx) % n] = S[3] if (xx == 0 and yy == 0) else S[2] if yy == 0 else S[1]
        for xx in range(1, w + 1):
            img[(sy + h) % n, (sx + xx) % n] = G[3]
    return img


def earth_strata_tile(seed, C, n=64):
    """段と slab の前の面 (side_earth): 横に流れる地層の筋 (2〜4 テクセルの帯)・途切れ途切れの暗い継ぎ目・埋まった小石。
    床より暗い土 (段の前の面は灯を受けにくい)。C['e'] = 暗→明 5 段 (床の灰白の段の暗い側)"""
    rng = _tile_rng(seed)
    E = C['e']
    f = GA.periodic_noise(n, 4, rng)
    rows = GA.periodic_noise(n, 16, rng)[:, 0]
    wob = GA.periodic_noise(n, 4, rng)
    img = np.zeros((n, n, 3), np.uint8)
    for y in range(n):
        for x in range(n):
            yy = (y + int(round((wob[y, x] - 0.5) * 6))) % n
            v = 0.6 * rows[yy] + 0.4 * f[y, x]
            img[y, x] = E[1] if v < 0.32 else E[2] if v < 0.56 else E[3] if v < 0.8 else E[4]
    # 地層の暗い継ぎ目 (横の細い線・途切れ途切れ・長さはばらばら)
    for _ in range(int(rng.integers(2, 5))):
        y = int(rng.integers(0, n))
        x = int(rng.integers(0, n))
        L = int(rng.integers(8, 22))
        for i in range(L):
            if rng.random() < 0.8:
                img[y % n, (x + i) % n] = E[0]
            if rng.random() < 0.15:
                y += 1 if rng.random() < 0.5 else -1
    # 埋まった小石 (上の縁が明るく、下に暗い影)
    S = C['stone']
    for (sx, sy) in GA.poisson(n, int(rng.integers(2, 5)), 18, rng):
        w = int(rng.integers(2, 5))
        h = int(rng.integers(1, 3))
        for yy in range(h):
            for xx in range(w):
                img[(sy + yy) % n, (sx + xx) % n] = S[2] if yy == 0 else S[1]
        for xx in range(w):
            img[(sy + h) % n, (sx + xx) % n] = E[0]
    return img


def iron_tile(seed, C, top, n=64):
    """鉄 (レール)。レールは幅 0.08 unit (2 テクセル) なので模様はほとんど見えない＝色の段と細い筋だけ。
    side_iron = 暗い鉄の地に、横の圧延の筋 (1 段明るい・途切れ途切れ) と小さな錆の点 (まばら・不規則) /
    top_iron = 車輪に磨かれた明るい鋼。レールの向き (テクスチャの u＝道の向き t) に長い光る筋"""
    rng = _tile_rng(seed)
    I = C['iron']
    img = np.zeros((n, n, 3), np.uint8)
    if top:
        img[:] = I[4]
        for _ in range(int(rng.integers(10, 16))):
            y, x, L = int(rng.integers(0, n)), int(rng.integers(0, n)), int(rng.integers(8, 30))
            col = I[5] if rng.random() < 0.7 else I[3]
            for i in range(L):
                img[y, (x + i) % n] = col
    else:
        img[:] = I[1]
        for _ in range(int(rng.integers(8, 14))):
            y, x, L = int(rng.integers(0, n)), int(rng.integers(0, n)), int(rng.integers(6, 22))
            col = I[2] if rng.random() < 0.7 else I[0]
            for i in range(L):
                if rng.random() < 0.85:
                    img[y, (x + i) % n] = col
        R = C['rust']
        for (sx, sy) in GA.poisson(n, int(rng.integers(3, 6)), 14, rng):
            for _ in range(int(rng.integers(2, 5))):
                dx, dy = int(rng.integers(-1, 2)), int(rng.integers(-1, 2))
                img[(sy + dy) % n, (sx + dx) % n] = R[int(rng.integers(0, len(R)))]
    return img


def _sprite(rows, cols):
    return GA.sprite(rows, cols)


def litter_items(C):
    """小札 (背丈 12 ドット以下・座席の帯にも置ける)。どれも不規則 (粒を等間隔に並べない)"""
    S, E, B, W = C['rock'], C['e'], C['brass'], C['chip']   # 砂利と小石は壁の青灰の岩の欠片 (暖かい灰の床から浮く)
    k = E[0]
    out = {}
    # 砂利: 小石 3〜5 個の不規則な塊 (大きさと間がばらばら)
    out['gravel_1'] = _sprite(['..hm.....', '.hmms.hm.', '..kks.mss', '.hm...kk.', 'hmms.....', '.kk......'],
                              {'h': S[3], 'm': S[2], 's': S[1], 'k': k})
    out['gravel_2'] = _sprite(['.hm..........', 'hmms...hm....', '.kk...hmms.hm', '.......kkk.ms', '...hm.......k', '..hmms.......', '...kk........'],
                              {'h': S[3], 'm': S[2], 's': S[1], 'k': k})
    out['gravel_3'] = _sprite(['......hm.', '.hm..hmms', 'hmms..kk.', '.kk......'],
                              {'h': S[2], 'm': S[1], 's': S[0], 'k': k})
    out['gravel_4'] = _sprite(['....hm....', '.hmhmms...', 'hmms.kk.hm', '.kk.....ms', '.........k'],
                              {'h': S[3], 'm': S[2], 's': S[1], 'k': k})
    # 小石: 1 個ずつ (左上が明るく右下に影)
    out['pebble_1'] = _sprite(['.hm.', 'hmms', '.kks'], {'h': S[3], 'm': S[2], 's': S[1], 'k': k})
    out['pebble_2'] = _sprite(['..hmm.', '.hmmms', 'hmmsss', '.kkkk.'], {'h': S[3], 'm': S[2], 's': S[1], 'k': k})
    out['pebble_3'] = _sprite(['hm.', 'mss', 'kk.'], {'h': S[2], 'm': S[1], 's': S[0], 'k': k})
    out['pebble_4'] = _sprite(['.hmm..', 'hmmmms', '.mssss', '..kkk.'], {'h': S[4], 'm': S[2], 's': S[1], 'k': k})
    # 落ちた歯車: 半分埋まった小さな歯車 (真鍮と鉄。光の側が明るい)
    out['gear_1'] = _sprite(['.h.h.', 'hbmbm', 'bm.mb', 'kbmbk'], {'h': B[2], 'b': B[1], 'm': B[0], 'k': k})
    out['gear_2'] = _sprite(['..h..h..', '.hbbbbm.', 'hbm..mbm', '.bm..mb.', 'kkbmmbkk'], {'h': B[2], 'b': B[1], 'm': B[0], 'k': k})
    out['gear_3'] = _sprite(['.h..h.', 'hbmmbm', 'bm..mb', '.bmmb.', '.kkkk.'], {'h': C['iron'][4], 'b': C['iron'][3], 'm': C['iron'][1], 'k': k})
    # 木くず: 細い木片 (長さと向きがばらばら)
    out['chips_1'] = _sprite(['....lw', '..lwd.', 'wd....', '.k..lw'], {'l': W[2], 'w': W[1], 'd': W[0], 'k': k})
    out['chips_2'] = _sprite(['lww....', '..dk.lw', '.....d.'], {'l': W[2], 'w': W[1], 'd': W[0], 'k': k})
    out['chips_3'] = _sprite(['.lw..', 'wd...', '...lw', 'lw.dk'], {'l': W[2], 'w': W[1], 'd': W[0], 'k': k})
    out['chips_4'] = _sprite(['......lwd', '.lwd.....', '...k..lw.'], {'l': W[2], 'w': W[1], 'd': W[0], 'k': k})
    return out


def gen_colors(pal):
    """コード生成の色の並び (どれも色表の色だけ)。目標の色に Oklab でいちばん近い色表の色を選ぶ (同じ色を2度使わない)"""
    return {
        # 床の灰白 (FLOOR_RAMP の 9 段そのもの＝色表に錨として入れてある)
        'g': ramp(pal, FLOOR_RAMP),
        # 段の土 (床の段の暗い側 5 段)
        'e': ramp(pal, FLOOR_RAMP[:5]),
        # 小石 (暖かい灰・暗→明 5 段)
        'stone': ramp(pal, [(58, 58, 60), (78, 78, 80), (100, 100, 102), (124, 122, 120), (146, 142, 138)]),
        # 岩の欠片 (壁の青灰・暗→明 5 段。いちばん明るいのは光の当たる角の淡い砂色)
        'rock': ramp(pal, [(53, 65, 86), (71, 88, 104), (96, 111, 121), (113, 126, 134), (175, 167, 150)]),
        # 鉄 (暗→明 6 段。0〜2 = 側面・3〜5 = 磨かれた天面)
        'iron': ramp(pal, [(30, 30, 36), (44, 46, 52), (58, 62, 70), (84, 94, 104), (100, 112, 122), (116, 128, 136)]),
        'rust': ramp(pal, [(115, 72, 70), (133, 89, 77)]),
        'brass': ramp(pal, [(96, 70, 40), (140, 104, 52), (190, 150, 80)]),
        'chip': ramp(pal, [(84, 64, 50), (118, 92, 68), (150, 122, 92)]),
    }


# 座席の床の砂の粒の量の倍率 (多い方から試し、ざらつきが門の 0.96 倍以下に入った最初の量)。
# 2026-10-03 反証の直し: 門を 500 (レーンが自分で置いた値。約束にも art-bible にも無い) から 800 へ緩め、量を 1.4 → 2.6 まで広げた。
# 500 では画面の模型が灯なしで 36〜41＝分析書の目標「細かさ 40〜90」の下の端か下だった。800 で 48〜54 (光溜まりで 88〜98)。
# 量を増やしても粒が重なって細かさは頭打ちになる (門 1,000・量 3.0 でも 53〜59) ので、撮影で足りなければ次に効くのは量でなく粒の明暗の幅
SEAT_GRAIN_STEPS = (2.6, 2.4, 2.2, 2.0, 1.8, 1.6, 1.4, 1.2, 1.0)

GEN_TILES = {
    # 名前: (作る関数, 種の頭, 回し方, 面, 門, 説明)
    'top_earth_seat': ('seat', 1100, 'rot4', 'top', 800,
                       '座席の帯の床 (コード生成)。灰白の踏み固めた砂の床・砂の粒 (横 4〜6×縦 3〜4 テクセル・白色の乱数で置く＝等間隔にしない)・小石 1〜3。'
                       '灯の真下で無彩色に読める明るさ (本家 ot2 の床 (92,96,93)〜(139,129,122))。art-bible §3-3 の幕2 の例外 (砂の粒・画面の細かさ 40〜90)'),
    'top_earth': ('earth', 1201, 'rot4', 'top', 800,
                  '段の天面の土 (コード生成)。gen_art の earth_tile_r2 (幕1 二周目の座席の土の作り＝斑・掻いた跡・小石 2〜3) を幕2 の床の灰白の段の暗い側で。'
                  '座席の床より暗い (段の天面は帯の外)'),
    'side_earth': ('strata', 1300, 'flipX', 'side', 800, '段と slab の前の面 (コード生成)。横に流れる地層の筋・埋まった小石・暗めの土'),
    'side_iron': ('iron_side', 1400, 'flipX', 'side', 800, 'レールの側面 (コード生成)。暗い鉄に小さな錆の斑'),
    'top_iron': ('iron_top', 1500, 'flipX', 'top', 800, 'レールの天面 (コード生成)。車輪に磨かれた明るい鋼'),
}


def make_gen(pal):
    set_palette_on_gen_art(pal)
    C = gen_colors(pal)
    # gen_art の earth_tile_r2 が読む土・石・草の並びを幕2 の物へ (暗→明 7 段。草の芽は土の色＝坑道の床に緑を置かない)
    GA.EARTH = list(C['g'][:7])
    GA.STONE = list(C['stone'])
    GA.GREEN = [C['g'][1], C['g'][2], C['g'][2], C['g'][3], C['g'][3]]
    res = {}
    dst = os.path.join(OUT, 'tiles')
    os.makedirs(dst, exist_ok=True)
    for name, (kind, seed0, rot, face, gate, note) in GEN_TILES.items():
        for i, v in enumerate('abcd'):
            seed = seed0 + i
            if kind == 'seat':
                # 粒の量はタイルごとに、ざらつきが門 (gate) の 0.96 倍以下になる最初の量 (大きな斑の出方で同じ量でも 650〜830 にばらつく)
                for k in SEAT_GRAIN_STEPS:
                    img = sand_seat_tile(seed, C, grain=(0.04 * k, 0.02 * k))
                    lv = TC.check(np.dstack([img, np.full(img.shape[:2], 255, np.uint8)]))['lapvar']
                    if lv <= gate * 0.96:
                        break
            elif kind == 'earth':
                img = GA.earth_tile_r2(seed)
            elif kind == 'strata':
                img = earth_strata_tile(seed, C)
            elif kind == 'iron_side':
                img = iron_tile(seed, C, False)
            else:
                img = iron_tile(seed, C, True)
            img = SP.map_image(np.dstack([img, np.full(img.shape[:2], 255, np.uint8)]), pal)[..., :3]
            save(img, os.path.join(dst, f'{name}_{v}.png'), 'RGB')
            c = TC.verdict(TC.check(np.dstack([img, np.full(img.shape[:2], 255, np.uint8)])), gate)
            c['src'] = f'コード生成 (gen_items.py {kind} 種 {seed}' + (f'・砂の粒の量 ×{k}' if kind == 'seat' else '') + ')'
            res[f'{name}_{v}'] = c
    ldst = os.path.join(OUT, 'litter')
    os.makedirs(ldst, exist_ok=True)
    lit = {}
    for name, img in litter_items(C).items():
        img = SP.map_image(img, pal)
        save(img, os.path.join(ldst, name + '.png'))
        lit[name] = [int(img.shape[1]), int(img.shape[0])]
    return res, lit


# ================================================================ 6. 法線と発光
# 暗い穴 (坑口・脇坑の中の暗がり) は法線を平らにする: 輪郭からの丸みで膨らませると、穴が枕のように盛り上がって光る
NORMAL_FLAT_DARK = {'tunnel_mouth': 34, 'tunnel_side': 34}   # 名前: この明るさ (輝度) より暗い画素は平らな法線


def make_normals_emission(rec):
    d = os.path.join(OUT, 'relief')
    for name, r in rec.items():
        if r['group'] in ('held', 'discard'):
            continue
        p = os.path.join(d, name + '.png')
        a = load_rgba(p)
        nm = SN.stage_normal_map(a)
        if name in NORMAL_FLAT_DARK:
            dark = (luma(a[..., :3]) < NORMAL_FLAT_DARK[name]) & (a[..., 3] >= 128)
            nm[dark, 0], nm[dark, 1], nm[dark, 2] = 128, 128, 255
        save(nm, os.path.join(d, name + '_n.png'))
        r['normal'] = 'Art/stage/act2/relief/' + name + '_n'
        if name in EMISSION:
            m = emission_of(name, a, r.get('offset', (0, 0)))
            n = int((m[..., :3].max(-1) > 0).sum())
            save(m, os.path.join(d, name + '_e.png'))
            r['emission'] = 'Art/stage/act2/relief/' + name + '_e'
            r['emissionPx'] = n
            r['emissionFrac'] = round(n / max(1, int((a[..., 3] >= 128).sum())), 3)


# ================================================================ 5b. 画面での細かさの模型 (座席の床)
def screen_fineness(tile, mean_to=None, sx=4, sy=0.5, rep=6):
    """座席の床が画面でどう見えるかの模型: タイルを rep×rep に敷き、横は 1 テクセル = sx px (PC の座席の 1 ドット 4px)、
    縦は見下ろし 5° で約 1/8 に潰れる＝1 テクセル = sy px (面積の平均で縮める)。明るさを mean_to にそろえ (無ければそのまま)、
    本家の物差しと同じ「σ0.7 のぼかしの後のラプラシアン分散」を返す (本家 ot2 の床は 50〜90。分析書の目標 40〜90)"""
    l = luma(tile[..., :3])
    big = np.tile(l, (rep, rep))
    H, W = big.shape
    # 横: 最近傍で sx 倍
    big = np.repeat(big, sx, axis=1)
    # 縦: 1/sy テクセルを 1 px に平均
    k = int(round(1 / sy))
    big = big[:(H // k) * k].reshape(H // k, k, -1).mean(1)
    if mean_to:
        big = big * (mean_to / max(1e-6, big.mean()))
    s = 0.7
    r = int(3 * s) + 1
    x = np.arange(-r, r + 1)
    kern = np.exp(-x * x / (2 * s * s))
    kern /= kern.sum()
    Hb, Wb = big.shape
    p = np.pad(big, r, mode='wrap')
    t = sum(kv * p[:, i:i + Wb] for i, kv in enumerate(kern))          # 横 (高さは Hb + 2r のまま)
    t = sum(kv * t[i:i + Hb, :] for i, kv in enumerate(kern))          # 縦 → Hb × Wb
    lp = t[:-2, 1:-1] + t[2:, 1:-1] + t[1:-1, :-2] + t[1:-1, 2:] - 4 * t[1:-1, 1:-1]
    return float(lp[4:-4, 4:-4].var())


# ================================================================ 7. 一覧
# 発光 _e.png を読む口は、2026-10-03 の時点でキャラの板 (StageUnits) にしか無い。箱庭の道 (Diorama・DioramaTextures) は
# 半立体の _n だけ読み、_e には触れない＝今は炉・提灯のガラス・結晶は描き込まれた色のままで、発光としては効かない。
# 光らせるなら統合 (か S) が半立体の材料に _e の地図を足す口を作る (レーン D の絵はそれを待って置いてあるだけ)
EMISSION_UNUSED = '箱庭は未使用 (2026-10-03: Diorama・DioramaTextures は半立体の _n だけ読み _e を読まない。光らせるには半立体の材料に _e の口が要る＝統合か S への申し送り)'


def write_index(calm_res, gen_res, rec, lit):
    mats = {}
    for name, spec in CALM.items():
        keys = [f'{name}_{v}' for v in 'abcd']
        mats[name] = dict(face=spec['face'], rot=spec['rot'], art=[f'Art/stage/act2/tiles/{k}' for k in keys], calm=1, sat=1,
                          note=spec['note'], src=[calm_res[k]['src'] for k in keys],
                          lapvar=[calm_res[k]['lapvar'] for k in keys], pass_=[calm_res[k]['pass'] for k in keys],
                          contrastUsed=[calm_res[k]['contrastUsed'] for k in keys],
                          palP95Before=[calm_res[k].get('palP95Before') for k in keys])
    for name, (kind, seed0, rot, face, gate, note) in GEN_TILES.items():
        keys = [f'{name}_{v}' for v in 'abcd']
        m = dict(face=face, rot=rot, art=[f'Art/stage/act2/tiles/{k}' for k in keys], calm=1, sat=1, note=note,
                 src=[gen_res[k]['src'] for k in keys], lapvar=[gen_res[k]['lapvar'] for k in keys], pass_=[gen_res[k]['pass'] for k in keys],
                 gateLapvar=gate)
        if name == 'top_earth_seat':
            m['screenFineness'] = [gen_res[k].get('screenFineness') for k in keys]
            m['screenFinenessLit'] = [gen_res[k].get('screenFinenessLit') for k in keys]
            m['satMedian'] = [gen_res[k].get('satMedian') for k in keys]
        mats[name] = m
    for m in mats.values():
        m['pass'] = m.pop('pass_')
    relief, held, discarded = {}, {}, {}
    for name, r in rec.items():
        if r['group'] == 'held':
            held[name] = dict(from_=r['src'], reason=r['why'])
            continue
        if r['group'] == 'discard':
            discarded[name] = dict(from_=r['src'], reason=r['why'])
            continue
        w, h = r['dots']
        kind = {'cut': '足元と皿を切った', 'keep': 'そのまま' if not r.get('ops') else 'そのまま (薄い皿・影の帯・草だけ切った)',
                'iso': '等角で頼んだ絵 (使える物だけ。足元を切った)' if r.get('ops') else '等角で頼んだ絵 (吊り物・正面に近い物はそのまま)'}[r['group']]
        e = dict(art='Art/stage/act2/relief/' + name, kind=kind,
                 group=r['group'], dots=[w, h], unitsAt25=[round(w / 25.0, 2), round(h / 25.0, 2)], from_=r['src'], srcDots=r['srcDots'],
                 note=r['why'], normal=r.get('normal'), palP95Before=r.get('palP95Before'))
        if 'emission' in r:
            e['emission'] = r['emission']
            e['emissionFrac'] = r['emissionFrac']
            e['emissionUse'] = EMISSION_UNUSED
        if r.get('ops'):
            e['ops'] = r['ops']
        relief[name] = e
    litter = {name: dict(art='Art/stage/act2/litter/' + name, dots=d, note={
        'gravel': '砂利 (小石 3〜5 個の不規則な塊)', 'pebble': '小石 1 個 (左上が明るく右下に影)',
        'gear': '落ちて半分埋まった小さな歯車 (真鍮・鉄)', 'chips': '木くず (細い木片)'}[name.split('_')[0]]) for name, d in lit.items()}

    def fix(o):
        if isinstance(o, dict):
            return {('from' if k == 'from_' else k): fix(v) for k, v in o.items()}
        if isinstance(o, list):
            return [fix(v) for v in o]
        if isinstance(o, tuple):
            return [fix(v) for v in o]
        return o
    idx = {
        '_': 'HD-2D 段2 の幕2「先代の坑道」の箱庭の絵の一覧 (2026-10-03 レーン D が書く。作り方は docs/pixellab/hd2d-act2/README.md・'
             '作り直しは python3 -B docs/pixellab/hd2d-act2/gen_items.py all)。art は Resources からのパス (拡張子なし)。'
             'タイルは calm 済み＝読み込み側で明暗を絞り直さない (DioramaTileCandidate の calm=1・sat=1)。'
             '色はどれも docs/pixellab/hd2d-act2/palette-act2.json へ写してある (半立体と小札も)。'
             '半立体の発光 emission (_e) は箱庭の道がまだ読まない (各項目の emissionUse)。'
             '幕1 の Art/stage/act1・今の舞台の Art/tiles・Art/props は読むだけで変えていない',
        'tileSize': 64,
        'texelsPerUnit': 25,
        'palette': 'docs/pixellab/hd2d-act2/palette-act2.json',
        'materials': mats,
        'relief': relief,
        'litter': litter,
        'held': held,
        'discarded': discarded,
    }
    os.makedirs(OUT, exist_ok=True)
    json.dump(fix(idx), open(os.path.join(OUT, 'index.json'), 'w'), ensure_ascii=False, indent=1)
    json.dump({'_': 'HD-2D 段2 レーン D (2026-10-03)。keyflip = 画像ファイルを左右反転した絵 (StageUnitLit の _KeyFlip=1)・lightFromRight = 光が右から描き込まれた絵。'
                    '幕2 の舞台の絵は左右反転して置いた物が無いので空。キャラの絵の表は幕1 の Art/stage/act1/keyflip がどの幕でも読まれる',
               'keyflip': [], 'lightFromRight': []}, open(os.path.join(OUT, 'keyflip.json'), 'w'), ensure_ascii=False, indent=1)


# ================================================================ 8. 点検
def lint(pal=None):
    if pal is None:
        pal = SP.load_palette(PAL_JSON)
    rows = []
    import glob
    for p in sorted(glob.glob(os.path.join(OUT, '**', '*.png'), recursive=True)):
        rel = os.path.relpath(p, OUT)
        a = load_rgba(p)
        base = os.path.splitext(os.path.basename(p))[0]
        gen = base.endswith('_n') or base.endswith('_e')
        semi = int(((a[..., 3] > 0) & (a[..., 3] < 255)).sum())
        area = int(a.shape[0] * a.shape[1])
        opq = float((a[..., 3] >= 128).mean())
        d = SP.palette_distance(a, pal) if not gen else np.zeros(0)
        p95 = round(float(np.percentile(d, 95)), 2) if len(d) else None
        r = dict(file=rel.replace(os.sep, '/'), w=int(a.shape[1]), h=int(a.shape[0]), area=area, opaque=round(opq, 3), semi=semi, palP95=p95)
        ok = semi == 0 and area <= 30000 and (gen or p95 == 0.0)
        if rel.startswith('litter'):
            ok &= a.shape[0] <= 12
            r['litterH'] = int(a.shape[0])
        if rel.startswith('tiles'):
            ok &= a.shape[:2] == (64, 64)
            c = TC.check(a)
            r['lapvar'] = c['lapvar']
            h, s, v = hsv(a)
            r['satMedian'] = round(float(np.median(s)), 3)
            r['lumaMean'] = round(float(luma(a[..., :3]).mean()), 1)
        r['ok'] = bool(ok)
        rows.append(r)
    os.makedirs(WORK_DOCS, exist_ok=True)
    json.dump(rows, open(os.path.join(WORK_DOCS, 'lint.json'), 'w'), ensure_ascii=False, indent=1)
    L = ['# 幕2 の絵の点検 (gen_items.py lint が書く)', '',
         '門: 半透明 0 (α は 0 か 255)・面積 30,000 以下・色表からの距離 p95 = 0 (法線 _n と発光 _e は色でないので測らない)・'
         '小札は背丈 12 以下・タイルは 64×64。ざらつき = ラプラシアン分散 (tile-calm の check と同じ)。', '',
         '| 絵 | 大きさ | 面積 | 不透明 | 半透明 | 色表 p95 | ざらつき | 彩度の中央値 | 明るさ | 判定 |', '|---|---|---|---|---|---|---|---|---|---|']
    for r in rows:
        L.append(f"| {r['file']} | {r['w']}×{r['h']} | {r['area']} | {r['opaque']} | {r['semi']} | {r['palP95'] if r['palP95'] is not None else '—'} | "
                 f"{r.get('lapvar', '—')} | {r.get('satMedian', '—')} | {r.get('lumaMean', '—')} | {'OK' if r['ok'] else 'NG'} |")
    ng = [r['file'] for r in rows if not r['ok']]
    L += ['', f'合計 {len(rows)} 枚・NG {len(ng)}' + (': ' + ', '.join(ng) if ng else '')]
    open(os.path.join(WORK_DOCS, 'lint.md'), 'w').write('\n'.join(L) + '\n')
    print(f'点検 {len(rows)} 枚・NG {len(ng)}' + (': ' + ', '.join(ng) if ng else ''))
    return rows


# ================================================================ シート
def sheet(out, work=None):
    """全部の絵を夜の地 (#262a3a) に並べる: タイルは 2×2 に敷いて 2 倍、半立体は 3 倍 (法線と発光を右に)、小札は 6 倍"""
    import glob
    items = []
    for name in list(CALM) + list(GEN_TILES):
        row = []
        for v in 'abcd':
            p = os.path.join(OUT, 'tiles', f'{name}_{v}.png')
            if os.path.exists(p):
                t = Image.open(p).convert('RGBA')
                big = Image.new('RGBA', (128, 128))
                for x0 in (0, 64):
                    for y0 in (0, 64):
                        big.paste(t, (x0, y0))
                row.append(big.resize((256, 256), Image.NEAREST))
        items.append(('tiles/' + name, row))
    for p in sorted(glob.glob(os.path.join(OUT, 'relief', '*.png'))):
        b = os.path.splitext(os.path.basename(p))[0]
        if b.endswith('_n') or b.endswith('_e'):
            continue
        row = []
        for suf in ('', '_n', '_e'):
            q = os.path.join(OUT, 'relief', b + suf + '.png')
            if os.path.exists(q):
                im = Image.open(q).convert('RGBA')
                if suf == '_e':   # 発光は黒地で見せる
                    bg = Image.new('RGBA', im.size, (0, 0, 0, 255))
                    bg.alpha_composite(im)
                    im = bg
                row.append(im.resize((im.width * 3, im.height * 3), Image.NEAREST))
        if work:   # 切る前の元 (左に薄く)
            src = os.path.join(PROPS, 'act2_' + b + '.png')
            if os.path.exists(src):
                im = Image.open(src).convert('RGBA')
                row.insert(0, im.resize((im.width * 3, im.height * 3), Image.NEAREST))
        items.append(('relief/' + b, row))
    lrow = []
    for p in sorted(glob.glob(os.path.join(OUT, 'litter', '*.png'))):
        im = Image.open(p).convert('RGBA')
        lrow.append(im.resize((im.width * 8, im.height * 8), Image.NEAREST))
    items.append(('litter (8 倍)', lrow))
    Wmax = 2600
    x = y = 10
    rowh = 0
    pos = []
    for label, ims in items:
        w = sum(i.width for i in ims) + 12 * max(0, len(ims) - 1)
        if x + w > Wmax and x > 10:
            x = 10
            y += rowh + 30
            rowh = 0
        pos.append((x, y))
        x += w + 28
        rowh = max(rowh, max([i.height for i in ims] + [10]))
    sh = Image.new('RGBA', (Wmax, y + rowh + 40), (38, 42, 58, 255))
    dr = ImageDraw.Draw(sh)
    for (label, ims), (px, py) in zip(items, pos):
        dr.text((px, py), label, fill=(230, 226, 210, 255))
        cx = px
        for im in ims:
            sh.alpha_composite(im, (cx, py + 16))
            cx += im.width + 12
    os.makedirs(os.path.dirname(out) or '.', exist_ok=True)
    sh.convert('RGB').save(out)
    print('シート →', out)


# ================================================================ まとめて
WORK_MARK = '.hd2d-act2-work'          # 作業場の目印 (all が作った作業場だけを消す)
WORK_SUBDIRS = {'calm', 'relief'}       # all が作業場に作る物 (目印の無い前の版の作業場もこれだけなら消してよい)


def prepare_work(work):
    """作業場を空にして作る。消してよいのは「all が作った作業場」だけ: リポジトリの中・リポジトリを含むフォルダは止める
    (README の点検の表の置き場 work/ と同じ名前を --work に渡すと点検の表が消え、--work . ならカレントごと消える)。
    中身があるなら、目印 WORK_MARK があるか、中身が calm/・relief/ だけの時だけ消す"""
    work = os.path.abspath(work)
    if os.path.commonpath([work, REPO]) in (work, REPO):
        sys.exit(f'作業場 {work} はリポジトリの中かリポジトリを含むので使えない (リポジトリの外を --work に渡す。既定は一時フォルダ)')
    if os.path.exists(work):
        if not os.path.isdir(work):
            sys.exit(f'作業場 {work} はフォルダでない')
        names = set(os.listdir(work))
        if names and WORK_MARK not in names and not names <= WORK_SUBDIRS:
            sys.exit(f'作業場 {work} に all が作っていない物がある ({", ".join(sorted(names)[:5])}…)。消さずに止める (空のフォルダか新しい名前を --work に渡す)')
        shutil.rmtree(work)
    os.makedirs(work)
    open(os.path.join(work, WORK_MARK), 'w').write('gen_items.py all の作業場 (毎回消して作り直す)\n')
    return work


def run_all(work, sheet_out=None):
    work = prepare_work(work)
    print('== 1. タイル (calm)')
    calm_res = make_calm(work)
    print('== 2. 半立体 (足元と皿を切る)')
    rec = make_relief(work)
    print('== 3. 色表')
    pal = build_palette(work, rec)
    print('== 4. 色表へ写す')
    # 前の出力を消してから書く (名前を変えた絵が残らないように)
    if os.path.exists(OUT):
        shutil.rmtree(OUT)
    before = map_all(work, pal)
    for k in calm_res:
        calm_res[k]['palP95Before'] = before.get(k)
    for k, r in rec.items():
        if k in before:
            r['palP95Before'] = before[k]
    for k in calm_res:   # 写した後の値で点検し直す
        a = load_rgba(os.path.join(OUT, 'tiles', k + '.png'))
        c = TC.verdict(TC.check(a), CALM[k.rsplit('_', 1)[0]]['maxlap'])
        for kk in ('lapvar', 'pass', 'fail', 'edgeMeanDiffLR', 'edgeMeanDiffTB', 'seamRatioX', 'seamRatioY'):
            calm_res[k][kk] = c[kk]
    print('== 5. コード生成 (土・鉄・小札)')
    gen_res, lit = make_gen(pal)
    for k, c in gen_res.items():
        if k.startswith('top_earth_seat'):
            a = load_rgba(os.path.join(OUT, 'tiles', k + '.png'))
            c['screenFineness'] = round(screen_fineness(a), 1)
            c['screenFinenessLit'] = round(screen_fineness(a, mean_to=luma(a[..., :3]).mean() * 1.35), 1)   # 灯の光溜まり (pool level 1.35) で明るくなった時
            c['satMedian'] = round(float(np.median(hsv(a)[1])), 3)
        print(f"{k}: {'OK' if c['pass'] else 'NG ' + ','.join(c['fail'])}  lapvar {c['lapvar']}" +
              (f"  画面の細かさ {c['screenFineness']} (光溜まりで {c['screenFinenessLit']})  彩度の中央値 {c['satMedian']}" if 'screenFineness' in c else ''))
    print('== 6. 法線と発光')
    make_normals_emission(rec)
    print('== 7. 一覧')
    write_index(calm_res, gen_res, rec, lit)
    print('== 8. 点検')
    lint(pal)
    os.makedirs(WORK_DOCS, exist_ok=True)
    json.dump({k: v for k, v in list(calm_res.items()) + list(gen_res.items())}, open(os.path.join(WORK_DOCS, 'tiles.json'), 'w'),
              ensure_ascii=False, indent=1)
    if sheet_out:
        sheet(sheet_out, work)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest='cmd', required=True)
    a = sub.add_parser('all')
    a.add_argument('--work', default=os.path.join(tempfile.gettempdir(), 'hd2d-act2-work'), help='下書きの作業場 (リポジトリの外。毎回消して作り直す)')
    a.add_argument('--sheet')
    sub.add_parser('lint')
    s = sub.add_parser('sheet')
    s.add_argument('--out', required=True)
    h = sub.add_parser('honke')
    h.add_argument('image')
    args = ap.parse_args()
    if args.cmd == 'all':
        run_all(os.path.abspath(args.work), args.sheet)
    elif args.cmd == 'lint':
        lint()
    elif args.cmd == 'sheet':
        sheet(args.out)
    elif args.cmd == 'honke':
        honke_measure(args.image)


if __name__ == '__main__':
    main()
