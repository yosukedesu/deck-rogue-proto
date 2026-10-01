#!/usr/bin/env python3
"""HD-2D 見本 三周目の物差し (2026-10-01 レーン F 段1・R12)。

計画 docs/design/hd2d-round3-plan-2026-10-01.md §1 と分析 docs/design/hd2d-r3-analysis-2026-10-01.md §3-2 の物差しを1場面ずつ測る。
合否は目で決める (裁定済み)。ここの数字は「どの層が足りないか」を言葉にする補助。二周目の物差し (N1〜N23) は scripts/hd2d-r2-targets.py のまま
(この道具はそれを読み込んで、霧の帯の頂点の行 N1・頂点の明るさ N3・帯の暗い筋 N7・縦の断面を借りる)。

★ 測り方を本家と二周目で回して決めた (--calibrate)。本家 ot16 (夜の森) が入り、二周目 (r2-slice の最終の撮影) が外れる物差しだけを「目安」、
  本家も二周目も入る物 (二周目の良い所を壊さない) を「歯止め」、分けられなかった物を「参考」(数字は出す・合否は付けない) にした。
  ot7 (洞窟)・ot11 (昼の村) は場面が違う参考 (森の物差し = 幹の本数・σ・帯の頂点 は ot16 だけで決めた)。分類の根拠は下の CALIB と --calibrate の表。

輝度 = 0.2126R + 0.7152G + 0.0722B (sRGB の値 0〜255。hd2d-r2-targets.py と同じ)。
舞台の画素 = UI なし (hideui) の画からキャラを除いた物。キャラ = unitsonly の画 (マゼンタ以外) を 4px 太らせた型。unitsonly が無ければ
  同じフォルダの unitsonly から主人公の型を借り (下の「画の選び方」)、それも無ければ layout.json の unitBoxes の板の矩形 (4px 足す) で除き、
  キャラの物差しは「近似」(体 = 絵の矩形の真ん中) = 合否なし。本家は REFS の UI とキャラの矩形で除く。
基準の行は画面の固定の窓でなく、霧の芯 (N1 の頂点の行)・足元の行 (layout.json の seats。本家は REFS)・dumplayout の矩形から取る。
スマホ相当 (1920×886) は主人公の1ドットの px ÷ 4 = sc で、足元・芯からの距離とぼかしの幅を縮める (hd2d-r2-targets.py と同じ)。

物差し (★ = 目安・◆ = 歯止め・— = 参考)
  ◆ G1 帯の頂点 N3 (hd2d-r2-targets の N3 = 中央の列の 10 行の平均を5つでならした最大)。PC ≥145 (二周目 154・本家 ot16 152.5。模型 A 133・B 123 で外れる)
       参考に peak31 (中央の列の行ごとの中央値の 31 行の移動平均の最大 = 反証 score の「霧の頂点」。本家 ot16 171・二周目 144)
  ◆ G2 舞台の p95 ≥125 (PH ≥118)   ◆ G3 暗い画素 (<60) ≤0.66 (PH ≤0.70)   (森を足しても霧は光る。反証 score M1)
  — M1 層の数: 中央の列 (x600〜1400) の4行ごとの中央値を3つでならし、8px 離れた差が 15 以上の所 (鋭い段) を数えた数 + 1。
       本家 ot16 は 3 (層の境目は明るさの段でなく形で分かれる = layers §1-2)。二周目 3〜4 → 分けられない
  ★ M2 上の覆い: 上端〜(芯 − 60px) の窓で、局所の標準偏差 (σ3px のガウスの窓) が 3 以上の画素の割合 = 「霧でない物」。本家 ot16 0.61・ot7 0.50・二周目 0.17
  ◆ N5c 同じ窓の 暗い (<60)・霞 (>130)・細かさ (σ1 の勾配の平均) — 窓を芯基準に直した N5 (分析 §3-2)
  — N6ac 同じ窓の中央の列 (x700〜1220。本家 900〜1300) の中央値 ÷ N3 — 芯基準の N6a
  ★ N2s 帯の芯の σ: 中央の列の縦の断面から、頂点の 400px 上〜250px 下の最小を地にして、地より上の分が半分以上の所に ln をあてた放物線の σ (PC の px)。
       本家 ot16 69・二周目 129 (地が暗いほど太く見える N2 の半値幅の代わり)
  ★ T1 幹の本数 = 帯 (芯 ±80px) を縦に横切る「暗く見える筋」の数 (N7 の count。地の 0.85 倍より暗く 12px 以上)。本家 ot16 7・二周目 3
  — M3 幹の太さの分散 (筋の幅の 標準偏差 ÷ 平均)・M4 幹の暗さの分散 (筋の 暗さ÷地 の標準偏差)。この筋の見つけ方では本家と二周目が分かれない
  ★ M5b 帯の幹の樹皮の読める率: 帯の筋の中 (左右 15% を除く・σ0.7 でぼかした輝度) で、横の勾配の最大が 4 を超える行の割合 (筋の高さで重みづけ)。
       本家 ot16 0.47・二周目 0.01 (分析 §11: 1 ドット幅の筋はぼかしで消える)
  ★ M5n 近い幹の樹皮の読める率: 両端の太い幹 (N7 の thick) の 行 100〜(足元 − 40) で、同じく横の勾配の最大が 6 を超える行の割合。本家 ot16 0.23・二周目 0.09
  ★ M6 地面の縁の硬さ: 座席の帯 (いちばん高い足元の 150px 上〜いちばん低い足元の 120px 下・x150〜1770) で、σ1 の縦の勾配の「列ごとの最大」の中央値 ÷
       帯の勾配の中央値 (縁石のような横に続く硬い縁ほど大きい)。本家 6.6〜7.3・二周目 12.6〜13.2・W5 18.1・模型 11〜15
  — M7 立った草の株: 座席の帯 (足元の 120px 上〜40px 下) で、σ3 のぼかしとの差が ±8 を超える塊のうち 高さ 8px 以上・幅 8px 以下・縦長の物の数 / 100px。
       本家 ot16 2.0・二周目 2.5〜4.8 → 分けられない (本家の草は低いコントラストでぼけている)
  ★ L1 空の帯の数: 60 行 (PH は H/1080 倍) ごとのラプラシアン分散 (σ0.7 でぼかした後・キャラの縁 2px を除く) が 2 未満の帯の数。本家 2・二周目 8
  — L2 層ごとの細かさの比: 上の帯の分散の p90 ÷ max(p10, 1) と 最大 ÷ max(最小, 2) (床つき)
  ★ E1 手前の縁: 足元の 106px 下 (本家 ot16 は行 860) より下の縁の数 / 1万画素 (2px おきの行と列でコントラスト 14 以上の縁。hd2d-r2-targets の sharp_frac と
       同じ走査)。本家 ot16 40・二周目 0.3〜2 (UI なしでも暗幕が残っていた。三周目から hideui は暗幕も消す = Autopilot)
  ★ E2 帯の中の縁: 芯 ±70px・x 28〜80% (本家 ot16 は [850,380,600,140]) の縁の数 / 1万画素。本家 ot16 225・二周目 45〜69
  ★ C1 体 ÷ すぐ後ろの窓 (M9): 主人公の体の中央値 ÷ 体の外 20〜60px の輪 (他のキャラ 4px と UI を除く) の中央値。本家 ot16 1.05・二周目 1.76 (ひなた 2.07)
  ★ C2 敵の鋭い縁の割合: 敵の型 (1px 太らせた) の中の強い縁 (隣の画素の差の極大・±4px の区間のコントラスト 30 以上) のうち、1画素で切り替わる (10〜90% の間に
       画素が無い) 物の割合。本家 ot16 の魔物 0.07・二周目 0.38〜0.45。縁 1px の中間色 (裁定 Q2) で下がる見込み
  ★ C3 主人公の鋭い縁の割合 (C2 と同じ測り方)。本家 0.06〜0.07・二周目 0.18〜0.21
  — M10 縁の幅 p50: 主人公の型の中の強い縁 (C2 の縁) の 10〜90% の幅 (画素)。本家 4・二周目 4〜5 (分析の「本家 1〜2・二周目 0〜1」はこの測り方では出ない)
  ◆ N13r 主人公の体の中央値 ≥90 (本家 97 基準に下げた N13。二周目 136)
  ★ N15r 足の真下 ÷ 左右 (hd2d-char-metrics の contact と同じ測り方。本家は主人公の矩形の真ん中 40% を足の幅とした近似): 0.85〜1.10 (本家側)。
       敵の値 (enemy) は参考に並べる

画の選び方 (2026-10-02 段1b レーン F2。試しの撮影 1 回目で、名前 T3-ogre-hideui を渡すと -unitsonly を見つけられず近似の型で測り、
  T3-ogre と別の値が出ていた／wolf・quad の「主人公の体 78.8」は近似の型 = 絵の矩形の真ん中に背景が入った値だった)
  名前は末尾の -hideui・-unitsonly・-uionly を外した「場面」にそろえる (同じ場面は1列。T3-ogre と T3-ogre-hideui は同じ列)。
  舞台の物差し = UI なしの画 (<場面>-hideui-1.png、無ければ state に hideui=1 がある <場面>-1.png)。
  UI の物差し (U1〜U4・参考) = UI ありの画 (<場面>-1.png で hideui=1 でない物)。UI ありの画は舞台の物差しには使わない。
  UI ありの画しか無い場面 (試し 1 回目の T3-char-soft・T3-ui・T3-r2) は、その画の UI を layout.json の UI の矩形で除いて測り、
    列の見出しに「(UI あり)」を付け、舞台の物差しは合否を付けない (値の後ろに ≈)。
  キャラの型 = <場面>-unitsonly-1.png。無い時は、同じフォルダの unitsonly からキャラごとに借りる: 同じキャラ (layout の units の id)・同じ板
    (boardRawPx・boardPx が ±0.6px・1ドットの px)・同じ画面の大きさ・同じ時刻 (±0.05 秒) の物 (DET=1 の撮影はキャラの絵が場面をまたいで同じ画素。
    T3-ogre-unitsonly から 狼・4体・霧の変種・char-soft の主人公、char-soft のオーガも借りられる)。借りた型の輪郭の画素の 50% 以上で勾配 >20・
    輪郭の勾配 ÷ 3〜6px 離れた所 ≥1.45 の時だけ採る (板がずれた T3-ui・別のリーダーの人形9体は借りない)。
    借りられなければ近似 (体 = 絵の矩形の真ん中・縁は板の矩形の中の全部)。近似の型の物差しは合否を付けない (値の後ろに ≈)。

UI の物差し (参考・合否なし。UI ありの画と同じ場面の UI なしの画の差が 12 を超える画素 = UI。UI なしの画が双子でなければ layout.json の UI の矩形)
  U1 UI の覆い (画面全体の割合)   U2 絵の窓 (上部バーの下〜主人公の足元) を UI が覆う割合
  U3 手札の外の UI の明るさ p95 と、それ ÷ 帯の頂点 N3 (1 を超える = 札が霧の芯より明るい)   U4 手札の外で N3 より明るい UI の画素 (/1万画素)
     (手札の矩形 = layout.json の hand。紙は手札・確認の窓・メニューだけ・舞台の上の札は夜色 = 2026-09-30 の裁定。暗幕 desk-shade も UI に入る)

使い方
  python3 scripts/hd2d-r3-targets.py <撮影のフォルダ> <場面> [<場面> …]   # 上の「画の選び方」。名前に -hideui を付けても付けなくても同じ列
  python3 scripts/hd2d-r3-targets.py --all <撮影のフォルダ>                # フォルダの場面を全部 (UI ありだけの場面は「(UI あり)」の列)
  python3 scripts/hd2d-r3-targets.py --image <画.png> [--layout <.layout.json>] [--units <unitsonly.png>]
  python3 scripts/hd2d-r3-targets.py --ref ot16 [--ref ot7]               # 本家 (hd2d-r2-targets の REFS。--ref-dir で置き場を足す)
  python3 scripts/hd2d-r3-targets.py --calibrate                           # 本家 3 枚・二周目の最終 (5場面)・W5・模型 A/B を測り、分類の根拠の表を出す
  共通: [--md 表.md] [--json 結果.json] [--quiet] [--r2 <二周目の最終の撮影 (r2-slice)>] [--w5 <W5 の shots>]
"""
import argparse
import importlib.util
import json
import math
import os
import sys
import warnings

import numpy as np

sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)

_spec = importlib.util.spec_from_file_location('hd2d_r2_targets', os.path.join(HERE, 'hd2d-r2-targets.py'))
T = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(T)

CACHE = os.path.expanduser('~/.cache/deck-rogue')
# 二周目の最終の撮影 (2026-10-01・hideui に暗幕が残っている版)。三周目の r3-slice.txt の「二周目の写し」(PC-R2-*) を撮ったらそちらが本当の比べる元
DEFAULT_R2 = os.path.join(CACHE, 'hd2d-r2', 'shots-final', 'r2-slice')
# W5 の最終の撮影 (hero62 = 主人公62 の狼・オーガ / slice = 3体・4体・人形9体)。~/.cache を先に探す
W5_CANDIDATES = [os.path.join(CACHE, 'hd2d-w5', 'shots'),
                 '/tmp/claude-1000/-home-yosuke-projects-deck-rogue-proto/2379737a-3c0d-4f8d-8002-16334720cb9f/scratchpad/hd2d/final/shots']
MOCKS = [('模型A 全部 (M5)', os.path.join(CACHE, 'hd2d-r3', 'proposals-A', 'M5-all-hideui.png')),
         ('模型B 全部 (mock-06)', os.path.join(CACHE, 'hd2d-r3', 'proposals-B', 'mock-06_B6_front.png'))]
REF_DIRS = T.DEFAULT_REF_DIRS + [os.path.join(CACHE, 'hd2d-ref', 'steam')]


def default_w5():
    for d in W5_CANDIDATES:
        if os.path.isdir(os.path.join(d, 'hero62')):
            return d
    return W5_CANDIDATES[0]


# ---- 目標と分類 (--calibrate で決めた。2026-10-01 の本家 3 枚・二周目 5 場面・W5・模型 A/B の値は CALIB)
#   cls: 目安 = 本家 ot16 が入り二周目が外れる / 歯止め = 本家も二周目も入る (壊さない) / 参考 = 分けられない (合否なし)
CHECKS = [
    dict(id='G1', key='guard.peak', label='帯の頂点 N3 (森を足しても霧は光る)', PC=[145, None], PH=[145, None], cls='歯止め'),
    dict(id='G2', key='guard.p95', label='舞台の p95', PC=[125, None], PH=[118, None], cls='歯止め'),
    dict(id='G3', key='guard.dark60', label='暗い画素 (<60) の割合', PC=[None, 0.66], PH=[None, 0.70], cls='歯止め'),
    dict(id='M1', key='M1.layers', label='層の数 (中央の列の鋭い段 + 1)', PC=[6, 7], PH=[6, 7], cls='参考'),
    dict(id='M2', key='M2.cover', label='上の覆い (芯−60 より上の模様の割合)', PC=[0.35, 0.70], PH=[0.35, 0.70], cls='目安'),
    dict(id='N5c', key='N5c.dark', label='芯基準の窓の暗い塊 (<60)', PC=[0.45, None], PH=[0.45, None], cls='歯止め'),
    dict(id='N5c', key='N5c.haze', label='芯基準の窓の霞 (>130)', PC=[None, 0.08], PH=[None, 0.08], cls='歯止め'),
    dict(id='N5c', key='N5c.meanGrad', label='芯基準の窓の細かさ (勾配の平均)', PC=[None, None], PH=[None, None], cls='参考'),
    dict(id='N6ac', key='N6ac.ratio', label='芯基準の上の真ん中 ÷ N3', PC=[0.45, 0.70], PH=[0.45, 0.70], cls='参考'),
    dict(id='N2s', key='N2s.sigma', label='帯の芯の σ (px)', PC=[50, 110], PH=[50, 110], cls='目安'),
    dict(id='T1', key='trunks.dark', label='幹の本数 (暗く見える本数)', PC=[6, 8], PH=[6, 8], cls='目安'),
    dict(id='M3', key='trunks.widthCV', label='幹の太さの分散 (幅の CV)', PC=[None, None], PH=[None, None], cls='参考'),
    dict(id='M4', key='trunks.ratioSD', label='幹の暗さの分散 (暗さ÷地の SD)', PC=[None, None], PH=[None, None], cls='参考'),
    dict(id='M5b', key='M5.band', label='帯の幹の樹皮の読める率 (勾配>4 の行)', PC=[0.30, None], PH=[0.30, None], cls='目安'),
    dict(id='M5n', key='M5.near', label='近い幹の樹皮の読める率 (勾配>6 の行)', PC=[0.18, None], PH=[0.18, None], cls='目安'),
    dict(id='M6', key='M6.hard', label='地面の縁の硬さ (列ごとの最大の縦の勾配 ÷ 中央値)', PC=[None, 9.0], PH=[None, 9.0], cls='目安'),
    dict(id='M7', key='M7.per100', label='立った草の株 (/100px)', PC=[8, 12], PH=[8, 12], cls='参考'),
    dict(id='L1', key='strips.empty', label='空の帯 (60行ごとの細かさ <2) の数', PC=[None, 2], PH=[None, 2], cls='目安'),
    dict(id='L2', key='strips.ratioP', label='層ごとの細かさの比 p90÷max(p10,1)', PC=[None, None], PH=[None, None], cls='参考'),
    dict(id='L2', key='strips.ratioFloor', label='層ごとの細かさの比 最大÷max(最小,2)', PC=[None, None], PH=[None, None], cls='参考'),
    dict(id='E1', key='edges.front', label='手前の縁 (/1万画素)', PC=[30, None], PH=[30, None], cls='目安'),
    dict(id='E2', key='edges.band', label='帯の中の縁 (/1万画素)', PC=[100, None], PH=[100, None], cls='目安'),
    dict(id='C1', key='chars.heroOverWindow', label='体 ÷ すぐ後ろの窓 (M9)', PC=[None, 1.5], PH=[None, 1.5], cls='目安'),
    dict(id='C2', key='chars.enemySharp', label='敵の鋭い縁の割合 (1画素の段差)', PC=[None, 0.30], PH=[None, 0.30], cls='目安'),
    dict(id='C3', key='chars.heroSharp', label='主人公の鋭い縁の割合 (1画素の段差)', PC=[None, 0.15], PH=[None, 0.15], cls='目安'),
    dict(id='M10', key='chars.heroEdgeWidth', label='縁の幅 p50 (px)', PC=[1, 2], PH=[1, 2], cls='参考'),
    dict(id='N13r', key='chars.heroBody', label='主人公の体の中央値 (本家 97 基準)', PC=[90, None], PH=[90, None], cls='歯止め'),
    dict(id='N15r', key='chars.heroContact', label='足の真下 ÷ 左右 (主人公・本家側)', PC=[0.85, 1.10], PH=[0.85, 1.10], cls='目安'),
]
CLS_MARK = {'目安': '★', '歯止め': '◆', '参考': '—'}
# 型が近似の時に合否を付けない物差し (主人公の型・敵の型)
HERO_KEYS = {'chars.heroOverWindow', 'chars.heroSharp', 'chars.heroEdgeWidth', 'chars.heroBody', 'chars.heroContact'}
ENEMY_KEYS = {'chars.enemySharp'}
# UI の物差し (UI ありの画。参考 = 合否なし)
UI_CHECKS = [
    dict(id='U1', key='ui.cover', label='UI の覆い (画面全体の割合)', cls='参考'),
    dict(id='U2', key='ui.pictureCover', label='絵の窓 (上部バーの下〜足元) を UI が覆う割合', cls='参考'),
    dict(id='U3', key='ui.p95', label='手札の外の UI の明るさ p95', cls='参考'),
    dict(id='U3r', key='ui.p95OverPeak', label='UI の p95 ÷ 帯の頂点 N3 (>1 = 霧の芯より明るい)', cls='参考'),
    dict(id='U4', key='ui.brightOutsideHand', label='手札の外で N3 より明るい UI の画素 (/1万画素)', cls='参考'),
]
SUFFIXES = ('-hideui', '-unitsonly', '-uionly')

# --calibrate の結果の控え (2026-10-01 統合前・レーン F)。本家 ot16｜二周目 PC オーガ (r2-slice の最終・hideui に暗幕あり)
CALIB = {
    'guard.peak': (152.5, 153.9), 'guard.p95': (131.3, 129.2), 'guard.dark60': (0.650, 0.634),
    'M2.cover': (0.614, 0.177), 'N2s.sigma': (69.3, 128.6), 'trunks.dark': (7, 3), 'M5.band': (0.47, 0.01), 'M5.near': (0.23, 0.09),
    'M6.hard': (6.71, 12.9), 'strips.empty': (2, 8), 'edges.front': (40.4, 0.5), 'edges.band': (225.1, 69.1),
    'chars.heroOverWindow': (1.05, 1.76), 'chars.enemySharp': (0.07, 0.45), 'chars.heroSharp': (0.07, 0.21),
    'chars.heroContact': (0.95, 0.71), 'chars.heroBody': (96.9, 135.0),
}


# ---- 道具

def lum(a):
    return T.lum(a)


def rnd(v, n=3):
    return T.rnd(v, n)


def localstd(g, s):
    m = T.gblur(g, s)
    return np.sqrt(np.maximum(T.gblur(g * g, s) - m * m, 0))


def lap(g):
    o = np.zeros_like(g)
    o[1:-1, 1:-1] = g[:-2, 1:-1] + g[2:, 1:-1] + g[1:-1, :-2] + g[1:-1, 2:] - 4 * g[1:-1, 1:-1]
    return o


def edge_count(g, ok, minC=14, step=2):
    """hd2d-r2-targets の sharp_frac と同じ走査で縁の数 (ok = 17px の区間が全部その中にある所だけ数える)"""
    H, W = g.shape
    n = 0
    profs = [(g[j, :], ok[j, :]) for j in range(0, H, step)] + [(g[:, i], ok[:, i]) for i in range(0, W, step)]
    for prof, pv in profs:
        L_ = len(prof)
        if L_ < 40:
            continue
        d = np.zeros(L_); d[1:-1] = prof[2:] - prof[:-2]; ad = np.abs(d)
        last = -10
        for i in np.nonzero(ad >= 8)[0]:
            if i < 16 or i >= L_ - 16 or i < last + 3:
                continue
            if ad[i] != ad[i - 2:i + 3].max() or not pv[i - 8:i + 9].all():
                continue
            seg = prof[i - 8:i + 9]
            if abs(seg[-3:].mean() - seg[:3].mean()) >= minC:
                n += 1; last = i
    return n


def short_edges(g, region, minC=30, half=4):
    """region の中の強い縁 (隣の画素の差の極大) ごとに ±half の区間の 10→90% の間の画素の数 (0 = 1画素で切り替わる)"""
    out = []
    for horiz in (True, False):
        G = g if horiz else g.T; R = region if horiz else region.T
        d = np.zeros_like(G); d[:, 1:] = G[:, 1:] - G[:, :-1]; ad = np.abs(d)
        ys, xs = np.nonzero((ad >= minC * 0.3) & R)
        for j, i in zip(ys, xs):
            if i < half + 1 or i >= G.shape[1] - half - 1:
                continue
            if ad[j, i] < ad[j, i - 1] or ad[j, i] < ad[j, i + 1] or ad[j, i] == ad[j, i - 1]:
                continue
            seg = G[j, i - half - 1:i + half + 1]
            a0 = seg[:2].mean(); a1 = seg[-2:].mean(); C = a1 - a0
            if abs(C) < minC:
                continue
            s = (seg - a0) / C
            out.append(int(((s > 0.1) & (s < 0.9)).sum()))
    return np.array(out, int)


def comps(mask):
    ys, xs, roots = T.label8(mask)
    out = []
    if len(ys) == 0:
        return out
    order = np.argsort(roots); r = roots[order]; ys = ys[order]; xs = xs[order]
    cuts = np.nonzero(np.diff(r))[0] + 1
    for a, b in zip(np.r_[0, cuts], np.r_[cuts, len(r)]):
        yy = ys[a:b]; xx = xs[a:b]
        out.append((int(yy.min()), int(yy.max()), int(xx.min()), int(xx.max()), int(b - a)))
    return out


def feet_rows(s):
    """いちばん高い足元・いちばん低い足元 (画面の行)。本家は REFS の主人公とキャラの矩形の下端"""
    if s.kind == 'ref':
        d = T.REFS[s.name]
        fe = [d['heroFeet']] + [c[1] + c[3] for c in d['chars']]
        return float(min(fe)), float(max(fe))
    fs = [q['px'][1] for q in s.seats.values() if q.get('px')] if s.seats else []
    if not fs:
        fs = [s.feet]
    return float(min(fs)), float(max(fs))


# ---- 測る

def measure(s, base=None):
    """s = hd2d-r2-targets の Shot。base = その measure() の結果 (無ければ測る)"""
    warnings.simplefilter('ignore', RuntimeWarning)
    ensure_masks(s)
    if base is None:
        base = T.measure(s)
    H, W = s.H, s.W; sc = s.sc; hs = H / 1080.0
    L = lum(s.a); valid = ~s.excl
    Ln = L.copy(); Ln[s.excl] = np.nan
    r = dict(name=s.name, kind=s.kind, dev=s.dev, path=s.path, size=[W, H], sc=rnd(sc), charMask=s.charMask,
             feet=rnd(s.feet, 1), notes=list(s.notes), uiOnly=bool(getattr(s, 'uiOnly', False)),
             src=dict(stage=os.path.basename(s.path) if s.path else None, units=getattr(s, 'unitsSrc', None),
                      donor=getattr(s, 'donor', None)))
    if getattr(s, 'lay', None):
        r['state'] = s.lay.get('state')
        dio = ((s.lay.get('extra') or {}).get('diorama') or {})
        if dio:   # 箱庭の門 (Diorama.Check。三周目は result=OK を必ず = 計画 §1)
            r['diorama'] = dict(ok=dio.get('ok'), failures=dio.get('failures'), layout=dio.get('layout'), triangles=dio.get('triangles'),
                                materials=dio.get('materials'), byKind=dio.get('byKind'), gates=dio.get('gates'))
    pk = int(base['N1']['peakRow']); n3 = base['N3']['peak']
    r['core'] = dict(peakRow=pk, feetMinusPeak=base['N1']['feetMinusPeak'], N3=n3, N2fwhm=base['N2']['fwhm'])
    # ---- 歯止め
    vv = L[valid]
    r['guard'] = dict(peak=n3, p95=rnd(float(np.percentile(vv, 95)), 1), dark60=rnd(float((vv < 60).mean())),
                      bright130=rnd(float((vv > 130).mean())), median=rnd(float(np.median(vv)), 1), peak31=rnd(peak31(s, Ln), 1))
    # ---- M1
    r['M1'] = m1_layers(s, Ln)
    # ---- 芯基準の窓 (M2・N5c・N6ac)
    y1 = int(max(20, pk - 60 * sc))
    win = valid[:y1]
    ls = localstd(L, 3.0 * sc)[:y1][win]
    r['M2'] = dict(rows=[0, y1], cover=rnd(float((ls > 3).mean())) if ls.size else None)
    up = L[:y1][win]
    g = T.gblur(L, 1.0)
    gx = np.zeros_like(g); gy = np.zeros_like(g); gx[:, 1:-1] = g[:, 2:] - g[:, :-2]; gy[1:-1] = g[2:] - g[:-2]
    r['N5c'] = dict(rows=[0, y1], dark=rnd(float((up < 60).mean())) if up.size else None, haze=rnd(float((up > 130).mean())) if up.size else None,
                    meanGrad=rnd(float(np.hypot(gx, gy)[:y1][win].mean()), 2) if up.size else None)
    x0c, x1c = s.center
    mid = T.med(T.region_vals(L, valid, [x0c, 0, x1c - x0c, y1]))
    r['N6ac'] = dict(median=rnd(mid, 1), ratio=rnd(mid / n3 if (mid is not None and n3) else None))
    # ---- N2s
    r['N2s'] = sigma_fit(base, s)
    # ---- 幹 (N7 の筋)
    st = base['N7']['streaks'] or []
    ws = np.array([q['width'] for q in st], float); rt = np.array([q['ratio'] for q in st], float)
    r['trunks'] = dict(dark=len(st), widths=[int(v) for v in ws], ratios=[rnd(v, 2) for v in rt],
                       widthCV=rnd(float(ws.std() / ws.mean()), 2) if len(ws) > 1 else None,
                       ratioSD=rnd(float(rt.std()), 3) if len(rt) > 1 else None, band=base['N7']['band'])
    # ---- M5 樹皮
    Lb = T.gblur(L, 0.7); Lb[s.excl] = np.nan
    b0, b1 = base['N7']['band']
    r['M5'] = dict(band=rnd(bark_mean(Lb, [(q['x'][0], q['x'][1], b0, b1) for q in st], 4.0), 2),
                   near=rnd(bark_mean(Lb, [(q['x'][0], q['x'][1], int(100 * hs), int(s.feet - 40 * sc)) for q in (base['N7']['thick'] or [])], 6.0), 2),
                   bandBoxes=len(st), nearBoxes=len(base['N7']['thick'] or []))
    # ---- M6・M7 (座席の帯)
    r['M6'] = m6_hard(s, L, valid)
    r['M7'] = m7_tufts(s, L, valid)
    # ---- 60 行ごとの細かさ
    r['strips'] = strips(s, L, valid)
    # ---- 縁の密度 (手前・帯)
    if s.kind == 'ref' and s.name == 'ot16':
        fr = [0, 860, W, H - 860]; bd = [850, 380, 600, 140]
    elif s.kind == 'ref':
        fr = [0, s.feet + 77, W, H]; bd = [int(W * 0.28), pk - 70, int(W * 0.52), 140]
    else:
        fr = [0, s.feet + 106 * sc, W, H]; bd = [int(W * 0.28), pk - 70 * sc, int(W * 0.52), 140 * sc]
    r['edges'] = dict(front=edge_density(L, valid, fr), band=edge_density(L, valid, bd),
                      frontRect=[rnd(q, 0) for q in fr], bandRect=[rnd(q, 0) for q in bd])
    # ---- キャラ
    r['chars'] = chars(s, L)
    return r


def peak31(s, Ln):
    x0, x1 = s.center
    with warnings.catch_warnings():
        warnings.simplefilter('ignore', RuntimeWarning)
        p = np.nanmedian(Ln[:, x0:x1], axis=1)
    idx = np.arange(len(p)); m = np.isfinite(p)
    if m.sum() < 40:
        return None
    p = np.interp(idx, idx[m], p[m])
    k = max(3, int(round(31 * s.H / 1080.0)) | 1)
    sm = np.convolve(np.pad(p, k // 2, mode='edge'), np.ones(k) / k, mode='valid')
    return float(sm.max())


def m1_layers(s, Ln, k=2, thr=15, step=4, smooth=3):
    W = s.W
    x0, x1 = int(600 * W / 1920), int(1400 * W / 1920)
    p = []
    for y in range(0, s.H, step):
        blk = Ln[y:y + step, x0:x1]; f = np.isfinite(blk)
        p.append(float(np.median(blk[f])) if f.sum() > 20 else np.nan)
    p = np.array(p); idx = np.arange(len(p)); m = np.isfinite(p)
    if m.sum() < 10:
        return dict(layers=None)
    p = np.interp(idx, idx[m], p[m])
    p = np.convolve(np.pad(p, smooth // 2, mode='edge'), np.ones(smooth) / smooth, mode='valid')
    d = np.zeros_like(p); d[k:-k] = p[2 * k:] - p[:-2 * k]
    steps = T.runs(np.abs(d) >= thr)
    return dict(layers=len(steps) + 1, steps=[[a * step, b * step, rnd(float(np.max(np.abs(d[a:b]))), 0)] for a, b in steps])


def sigma_fit(base, s):
    rows = np.array(base['profile']['rows']) + 5
    sm = np.array([np.nan if v is None else v for v in base['profile']['smooth']], float)
    if not np.isfinite(sm).any():
        return dict(sigma=None)
    ip = int(np.nanargmax(sm[3:-3])) + 3; sc = s.sc
    lo = max(0, ip - int(40 * sc)); hi = min(len(sm), ip + int(25 * sc))
    bse = float(np.nanmin(sm[lo:hi])); v = sm - bse; vm = v[ip]
    if vm <= 1:
        return dict(sigma=None, base=rnd(bse, 1))
    a = ip
    while a - 1 >= lo and v[a - 1] >= 0.5 * vm:
        a -= 1
    b = ip
    while b + 1 < hi and v[b + 1] >= 0.5 * vm:
        b += 1
    idx = np.arange(a, b + 1)
    if len(idx) < 3:
        return dict(sigma=None, base=rnd(bse, 1))
    y = rows[idx] - rows[ip]; z = np.log(np.maximum(v[idx], 1e-3))
    c = np.linalg.lstsq(np.vstack([np.ones_like(y), y, y * y]).T, z, rcond=None)[0]
    sg = math.sqrt(-1 / (2 * c[2])) / sc if c[2] < 0 else None
    return dict(sigma=rnd(sg, 1), base=rnd(bse, 1), halfUp=int(rows[ip] - rows[a]), halfDown=int(rows[b] - rows[ip]))


def bark_mean(Lb, boxes, thr, shrink=0.15):
    num = den = 0
    for x0, x1, y0, y1 in boxes:
        w = x1 - x0; a = x0 + int(round(w * shrink)); b = x1 - int(round(w * shrink))
        if b - a < 3 or y1 - y0 < 10:
            continue
        sub = Lb[max(0, y0):y1, max(0, a - 1):b + 1]
        gx = np.abs(sub[:, 2:] - sub[:, :-2]) / 2
        with warnings.catch_warnings():
            warnings.simplefilter('ignore', RuntimeWarning)
            mx = np.nanmax(gx, axis=1)
        f = np.isfinite(mx)
        if f.sum() < 10:
            continue
        num += float((mx[f] > thr).sum()); den += int(f.sum())
    return num / den if den else None


def m6_hard(s, L, valid, sig=1.0):
    H, W = L.shape; sc = s.sc
    f0, f1 = feet_rows(s)
    y0 = int(max(0, f0 - 150 * sc)); y1 = int(min(H - 2, f1 + 120 * sc))
    g = T.gblur(L, sig * sc)
    gy = np.zeros_like(g); gy[1:-1] = np.abs(g[2:] - g[:-2]) / 2
    gy[T.dilate(~valid, int(3 * sig * sc) + 2)] = np.nan
    x0, x1 = int(150 * W / 1920), int(1770 * W / 1920)
    Z = gy[y0:y1, x0:x1]
    with warnings.catch_warnings():
        warnings.simplefilter('ignore', RuntimeWarning)
        colmax = np.nanmax(Z, axis=0); md = float(np.nanmedian(Z))
    f = np.isfinite(colmax)
    if f.sum() < 50 or not md:
        return dict(hard=None, rows=[y0, y1])
    return dict(hard=rnd(float(np.median(colmax[f]) / md), 2), p75=rnd(float(np.percentile(colmax[f], 75) / md), 2), median=rnd(md, 2), rows=[y0, y1])


def m7_tufts(s, L, valid, thr=8, sig=3.0):
    H, W = L.shape; sc = s.sc
    f0, f1 = feet_rows(s)
    y0 = int(max(0, f0 - 120 * sc)); y1 = int(min(H, f1 + 40 * sc))
    x0, x1 = int(100 * W / 1920), int(1820 * W / 1920)
    Hp = L - T.gblur(L, sig * sc)
    n = 0
    for sign in (1, -1):
        m = (sign * Hp > thr) & valid
        m[:y0] = False; m[y1:] = False; m[:, :x0] = False; m[:, x1:] = False
        for ya, yb, xa, xb, c in comps(m):
            h = yb - ya + 1; w = xb - xa + 1
            if h >= 8 * sc and w <= 8 * sc and h >= 1.5 * w:
                n += 1
    vz = float(valid[y0:y1, x0:x1].mean()) if y1 > y0 else 0
    return dict(count=n, per100=rnd(n / ((x1 - x0) * vz) * 100, 2) if vz > 0 else None, rows=[y0, y1])


def strips(s, L, valid):
    H = s.H; sh = max(8, int(round(60 * H / 1080.0)))
    lp = lap(T.gblur(L, 0.7)); me = ~T.dilate(~valid, 2)
    vals = []
    for y in range(0, H - sh + 1, sh):
        m = me[y:y + sh]; v = lp[y:y + sh][m]
        vals.append(float(v.var()) if v.size > 500 else np.nan)
    a = np.array(vals); f = np.isfinite(a)
    if not f.any():
        return dict(values=[])
    af = a[f]
    return dict(values=[rnd(v, 1) for v in a], empty=int((af < 2).sum()),
                ratioP=rnd(float(np.percentile(af, 90) / max(np.percentile(af, 10), 1.0)), 1),
                ratioFloor=rnd(float(af.max() / max(af.min(), 2.0)), 1), stripH=sh)


def edge_density(L, valid, rect):
    H, W = L.shape
    x0, y0, x1, y1 = T.clip_rect(rect, W, H)
    if x1 - x0 < 40 or y1 - y0 < 40:
        return None
    sub = L[y0:y1, x0:x1].astype(np.float32); ok = valid[y0:y1, x0:x1]
    if ok.sum() < 1000:
        return None
    return rnd(edge_count(sub, ok) / ok.sum() * 1e4, 1)


def chars(s, L):
    out = dict()
    H, W = L.shape
    if s.kind == 'ref':
        d = T.REFS[s.name]
        x, y, w, h = d['hero']
        body = float(np.median(L[y:y + h, x:x + w]))
        ring = T.rectmask(s.a.shape, [d['hero']], 60) & ~T.rectmask(s.a.shape, [d['hero']], 20) & ~s.excl
        win = float(np.median(L[ring])) if ring.sum() > 200 else None
        out.update(mask='本家の矩形 (体 = 主人公の矩形の中央値)', heroBody=rnd(body, 1), heroWindow=rnd(win, 1),
                   heroOverWindow=rnd(body / win, 2) if win else None)
        hw = short_edges(L, T.rectmask(s.a.shape, [d['hero']], 0))
        ew = short_edges(L, T.rectmask(s.a.shape, [d['chars'][0]], 0))
        out.update(heroSharp=rnd(float((hw <= 0).mean()), 2) if hw.size else None, heroEdgeWidth=float(np.median(hw) + 1) if hw.size else None,
                   enemySharp=rnd(float((ew <= 0).mean()), 2) if ew.size else None, enemyEdges=int(ew.size))
        # 足の真下 ÷ 左右 (近似: 主人公の矩形の真ん中 40% を足の幅)
        bot = int(d['heroFeet']); fx0, fx1 = int(x + 0.3 * w), int(x + 0.7 * w); fw = max(1, fx1 - fx0)
        out['heroContact'] = contact(L, np.zeros_like(L, bool), bot, fx0, fx1, fw)
        out['contactNote'] = '本家は主人公の矩形の真ん中 40% を足の幅とした近似'
        return out
    ub = s.unitBoxes or {}
    if 'player' not in ub:
        out['mask'] = 'キャラの板の記録が無い'
        return out
    umask = getattr(s, 'umask', None) or {}   # 型の分かるキャラ (unitsonly の型・借りた主人公の型)
    charm = s.charm
    uim = getattr(s, 'uiMask', None)          # UI ありの画だけの場面: layout.json の UI の矩形
    uix = T.dilate(uim, 2) if uim is not None else np.zeros((H, W), bool)

    def board(key):
        return board_mask(ub, key, H, W)
    hb, (x, y, w, h) = board('player')
    hero_exact = 'player' in umask
    ens = [k for k in ub if k.startswith('enemy')]
    enemy_exact = bool(ens) and all(k in umask for k in ens)
    out.update(heroExact=hero_exact, enemyExact=enemy_exact)
    if not hero_exact:
        hm = np.zeros((H, W), bool); hm[int(y + 0.25 * h):int(y + 0.75 * h), int(x + 0.3 * w):int(x + 0.7 * w)] = True
        body_m = hm
        others = charm & ~hb
        out['mask'] = '近似 (unitsonly が無い: 体 = 絵の矩形の真ん中・縁は板の矩形の中の全部の縁)'
        edge_region = hb
        ring_base = hb
    else:
        hm = umask['player']
        body_m = hm & ~T.dilate(~hm, 2)
        others = charm & ~hm
        don = getattr(s, 'donor', None)
        out['mask'] = 'unitsonly' if not don else 'unitsonly (%s から借りた・敵は%s)' % (don, '型' if enemy_exact else '近似')
        edge_region = T.dilate(hm, 1)
        ring_base = hm
    if not enemy_exact and ens:
        out['enemyMask'] = '近似 (敵の unitsonly が無い: 縁は板の矩形の中の全部の縁)'
    if body_m.sum() < 50:
        return out
    body = float(np.median(L[body_m]))
    ring = T.dilate(ring_base, int(round(60 * s.sc))) & ~T.dilate(ring_base, int(round(20 * s.sc))) & ~T.dilate(others, 4) & ~uix
    win = float(np.median(L[ring])) if ring.sum() > 200 else None
    out.update(heroBody=rnd(body, 1), heroWindow=rnd(win, 1), heroOverWindow=rnd(body / win, 2) if win else None)
    hw = short_edges(L, edge_region & ~T.dilate(others, 1) & ~uix)
    out.update(heroSharp=rnd(float((hw <= 0).mean()), 2) if hw.size else None, heroEdgeWidth=float(np.median(hw) + 1) if hw.size else None)
    pool = []
    per = {}
    for k in ens:
        eb, _ = board(k)
        if k in umask:
            em = umask[k]
            reg = T.dilate(em, 1) & ~T.dilate(charm & ~em, 1)
        else:
            reg = eb
        ew = short_edges(L, reg & ~uix)
        if ew.size:
            per[k] = rnd(float((ew <= 0).mean()), 2); pool.append(ew)
    if pool:
        allw = np.concatenate(pool)
        out.update(enemySharp=rnd(float((allw <= 0).mean()), 2), enemySharpPer=per, enemyEdges=int(allw.size))
    # 足の真下 ÷ 左右 (hd2d-char-metrics の contact と同じ)。型の分かるキャラだけ
    if hero_exact:
        out['heroContact'] = unit_contact(L, charm | uix, hm)
    ec = {}
    for k in ens:
        if k in umask:
            v = unit_contact(L, charm | uix, umask[k])
            if v is not None:
                ec[k] = v
    if ec:
        out['enemyContact'] = ec
    return out


def contact(L, ch, bot, fx0, fx1, fw):
    H, W = L.shape
    rows = slice(max(0, bot - 2), min(H, bot + 10))
    under = np.zeros((H, W), bool); under[rows, fx0:fx1] = True; under &= ~ch
    side = np.zeros((H, W), bool)
    side[rows, max(0, fx0 - int(1.2 * fw)):max(0, fx0 - int(0.4 * fw))] = True
    side[rows, min(W, fx1 + int(0.4 * fw)):min(W, fx1 + int(1.2 * fw))] = True
    side &= ~ch
    if under.sum() > 10 and side.sum() > 10:
        return rnd(float(np.median(L[under])) / max(1.0, float(np.median(L[side]))), 2)
    return None


def unit_contact(L, ch, m):
    if m.sum() < 50:
        return None
    ys, xs = np.nonzero(m)
    top, bot = int(ys.min()), int(ys.max()); hh = bot - top + 1
    low = m[max(0, bot - int(0.12 * hh)):bot + 1]
    fx = np.nonzero(low)[1]
    if not fx.size:
        return None
    fx0, fx1 = int(fx.min()), int(fx.max())
    return contact(L, ch, bot, fx0, fx1, max(1, fx1 - fx0))


# ---- 合否と表

def unjudged(r, key):
    """合否を付けない理由 (None = 付ける)。'近似' = キャラの型が近似・'UI あり' = UI ありの画だけで舞台を測った"""
    if not r or r.get('kind') == 'ref':
        return None
    ch = r.get('chars') or {}
    if key in HERO_KEYS and not ch.get('heroExact'):
        return '近似'
    if key in ENEMY_KEYS and not ch.get('enemyExact'):
        return '近似'
    if not key.startswith('chars.') and r.get('uiOnly'):
        return 'UI あり'
    return None


def judge(r, checks=CHECKS):
    out = []
    dev = r.get('dev', 'PC')
    for c in checks:
        rng = c.get(dev) or c.get('PC')
        v = T.get_key(r, c['key'])
        ok = None
        if c['cls'] != '参考' and rng and v is not None and (rng[0] is not None or rng[1] is not None) and not unjudged(r, c['key']):
            ok = (rng[0] is None or v >= rng[0]) and (rng[1] is None or v <= rng[1])
        out.append((c, v, rng, ok))
    return out


def fmt_cell(r, c, v, ok):
    """表の1マス: 値 + ○/× (合否なしの理由が近似か UI ありなら ≈)"""
    if v is None:
        return '—'
    if ok is not None:
        return fmt_v(v) + (' ○' if ok else ' ×')
    return fmt_v(v) + (' ≈' if (c['cls'] != '参考' and unjudged(r, c['key'])) else '')


def fmt_rng(rng):
    if not rng or (rng[0] is None and rng[1] is None):
        return '—'
    return T.fmt_rng(rng)


def fmt_v(v):
    if v is None:
        return '—'
    if isinstance(v, float):
        return ('%.3g' % v) if abs(v) < 10 else ('%.1f' % v if abs(v) < 1000 else '%.0f' % v)
    return str(v)


def text_report(r):
    lines = ['== %s%s (%s・%s)  足元 %s・芯の行 %s・sc %s・キャラ: %s' % (r['name'], '（UI あり）' if r.get('uiOnly') else '', r['kind'], r['dev'], r['feet'],
                                                                r['core']['peakRow'], r['sc'], r['chars'].get('mask', r['charMask']))]
    src = r.get('src') or {}
    if r.get('kind') != 'ref':
        lines.append('  画: 舞台 = %s・UI = %s・キャラの型 = %s' % (src.get('stage'), src_ui_text(r), src_units_text(r)))
    for n in r.get('notes', []):
        lines.append('  注: ' + n)
    for c, v, rng, ok in judge(r):
        mark = '' if ok is None else (' ○' if ok else ' ×')
        if ok is None and c['cls'] != '参考' and v is not None and unjudged(r, c['key']):
            mark = ' ≈ (%s = 合否なし)' % unjudged(r, c['key'])
        lines.append('  %s %-5s %-44s %-10s 目標 %-10s%s' % (CLS_MARK[c['cls']], c['id'], c['label'], fmt_v(v), fmt_rng(rng), mark))
    if r.get('ui'):
        for c in UI_CHECKS:
            lines.append('  %s %-5s %-44s %-10s (UI の画 %s)' % (CLS_MARK[c['cls']], c['id'], c['label'], fmt_v(T.get_key(r, c['key'])), r['ui'].get('img')))
    if r.get('diorama'):
        d = r['diorama']
        lines.append('  箱庭の門 (Diorama.Check): %s%s・材質 %s・三角形 %s・%s' % ('OK' if d.get('ok') else 'NG', '' if d.get('ok') else ' (%s)' % ' / '.join(d.get('failures') or []),
                                                                   d.get('materials'), d.get('triangles'), d.get('byKind')))
    lines.append('  (参考) peak31 %s・M1 の段 %s・帯の筋 %s・60行ごとの細かさ %s' % (
        r['guard'].get('peak31'), r['M1'].get('steps'), r['trunks'].get('widths'), r['strips'].get('values')))
    js = judge(r)
    ok = sum(1 for q in js if q[3]); ng = sum(1 for q in js if q[3] is False)
    lines.append('  目安・歯止め: ○ %d・× %d (参考は合否なし)' % (ok, ng))
    return '\n'.join(lines)


def col_label(r):
    lab = r.get('label', r['name'])
    return lab + ('（UI あり）' if r.get('uiOnly') and 'UI あり' not in lab else '')


def src_units_text(r):
    """キャラの型の出どころ (表の行・text_report)"""
    if r.get('kind') == 'ref':
        return '本家の矩形'
    ch = r.get('chars') or {}
    src = r.get('src') or {}
    if 'heroExact' not in ch:
        return '—'
    don = src.get('donor')
    if ch.get('heroExact') and ch.get('enemyExact'):
        return ('%s から借りた (主人公・敵)' % don) if don else (src.get('units') or 'unitsonly')
    if ch.get('heroExact'):
        return '主人公 = %s・敵 = 近似' % (don and ('%s から借りた' % don) or src.get('units') or 'unitsonly')
    if ch.get('enemyExact'):
        return '主人公 = 近似・敵 = %s' % (don and ('%s から借りた' % don) or 'unitsonly')
    return '近似 (unitsonly 無し)'


def src_ui_text(r):
    u = r.get('ui') or {}
    if not u:
        return '—'
    return '%s (%s)' % (u.get('img'), u.get('maskSrc'))


def md_table(results, title='三周目の物差し (scripts/hd2d-r3-targets.py)'):
    head = '| 物差し | 分類 | 目標 PC / PH | ' + ' | '.join(col_label(r) for r in results) + ' |'
    rows = ['# ' + title, '', '★ 目安 = 本家 ot16 が入り二周目が外れる物差し・◆ 歯止め = 本家も二周目も入る (壊さない)・— 参考 = 分けられなかった (合否なし)。'
            '合否は目で決める (裁定)。数字は「どの層が足りないか」の補助。',
            '舞台の物差しは UI なしの画 (<場面>-hideui-1.png か state に hideui=1 の画) で測り、UI ありの画は下の U1〜U4 (UI の物差し) だけに使う。'
            '列の見出しの「(UI あり)」= UI ありの画しか無い場面 (UI を layout.json の矩形で除いて測った。舞台の物差しは合否なし)。'
            '≈ = 合否を付けない (キャラの型が近似 = unitsonly も借りられる型も無い、か UI ありの画だけ)。', '', head, '|' + '---|' * (3 + len(results))]
    rows.append('| 舞台を測った画 | | | %s |' % ' | '.join((r.get('src') or {}).get('stage') or ('本家' if r.get('kind') == 'ref' else '—') for r in results))
    rows.append('| キャラの型 | | | %s |' % ' | '.join(src_units_text(r) for r in results))
    rows.append('| UI ありの画 (U1〜U4 だけ) | | | %s |' % ' | '.join(src_ui_text(r) for r in results))
    for c in CHECKS:
        cells = []
        for r in results:
            v = T.get_key(r, c['key'])
            j = [q for q in judge(r, [c])][0]
            cells.append(fmt_cell(r, c, v, j[3]))
        tg = '%s / %s' % (fmt_rng(c.get('PC')), fmt_rng(c.get('PH')))
        rows.append('| %s %s %s | %s | %s | %s |' % (CLS_MARK[c['cls']], c['id'], c['label'], c['cls'], tg, ' | '.join(cells)))
    tally = []
    for r in results:
        js = judge(r); tally.append('%d/%d' % (sum(1 for q in js if q[3]), sum(1 for q in js if q[3] is not None)))
    rows.append('| 目安・歯止めの合格 | | | %s |' % ' | '.join(tally))
    dio = [('OK' if (r.get('diorama') or {}).get('ok') else ('NG' if r.get('diorama') else '—')) for r in results]
    rows.append('| 箱庭の門 (Diorama.Check の result) | | OK | %s |' % ' | '.join(dio))
    if any(r.get('ui') for r in results):
        rows.append('| **UI の物差し (UI ありの画・参考)** | | | %s |' % ' | '.join('' for _ in results))
        for c in UI_CHECKS:
            rows.append('| %s %s %s | %s | — | %s |' % (CLS_MARK[c['cls']], c['id'], c['label'], c['cls'],
                                                      ' | '.join(fmt_v(T.get_key(r, c['key'])) for r in results)))
    return '\n'.join(rows) + '\n'


def calib_table(results):
    """分類の根拠: 本家 (ot16) が目標に入るか・二周目 (PC 5場面) が外れるか"""
    ref = next((r for r in results if r['kind'] == 'ref' and r['name'] == 'ot16'), None)
    r2 = [r for r in results if r.get('group') == 'r2' and r['dev'] == 'PC']
    lines = ['## 分類の根拠 (本家 ot16 が入り・二周目 PC が外れる物差しだけ目安)', '',
             '| 物差し | 目標 (PC) | 本家 ot16 | 二周目 PC (オーガ / 狼 / 4体 / 人形9体) | 本家が入る | 二周目が外れる | 分類 |', '|---|---|---|---|---|---|---|']
    for c in CHECKS:
        rng = c.get('PC')
        has = rng and (rng[0] is not None or rng[1] is not None)

        def inside(v):
            if v is None or not has:
                return None
            return (rng[0] is None or v >= rng[0]) and (rng[1] is None or v <= rng[1])
        rv = T.get_key(ref, c['key']) if ref else None
        vs = [T.get_key(r, c['key']) for r in r2]
        ri = inside(rv); ro = [inside(v) for v in vs]
        out_all = None if not has or any(q is None for q in ro) else all(q is False for q in ro)
        yn = lambda b: '—' if b is None else ('はい' if b else 'いいえ')
        lines.append('| %s %s | %s | %s | %s | %s | %s | %s |' % (c['id'], c['label'], fmt_rng(rng), fmt_v(rv), ' / '.join(fmt_v(v) for v in vs),
                                                             yn(ri), yn(out_all), c['cls']))
    return '\n'.join(lines) + '\n'


# ---- 画を読む

def board_mask(ub, key, H, W):
    """キャラの板 (boardRawPx・無ければ boardPx) の矩形の型と (x, y, w, h)"""
    bx = ub[key].get('boardRawPx') or ub[key].get('boardPx')
    x, y, w, h = [int(round(v)) for v in bx]
    m = np.zeros((H, W), bool); m[max(0, y):max(0, y + h), max(0, x):max(0, x + w)] = True
    return m, (x, y, w, h)


def ensure_masks(s):
    """型の分かるキャラ (s.umask = {key: 型}) を用意する。make_shot を通らない画 (シート・模型) は charMask が unitsonly なら全員"""
    if hasattr(s, 'umask') or s.kind == 'ref':
        return
    s.umask = {}
    ub = s.unitBoxes or {}
    if s.charMask.startswith('unitsonly'):
        for k in ub:
            if ub[k].get('boardRawPx') or ub[k].get('boardPx'):
                s.umask[k] = board_mask(ub, k, s.H, s.W)[0] & s.charm


def _lay(p):
    try:
        return json.load(open(p)) if p and os.path.exists(p) else None
    except Exception:  # noqa
        return None


def is_hideui(lay):
    return bool(lay) and 'hideui=1' in (lay.get('state') or '')


def scene_base(folder, name):
    """末尾の -hideui・-unitsonly・-uionly を外した場面の名前 (外した名前の画が何か1つあれば)"""
    for suf in SUFFIXES:
        if name.endswith(suf):
            b = name[:-len(suf)]
            if any(os.path.exists(os.path.join(folder, b + q)) for q in ('-1.png', '-hideui-1.png', '-unitsonly-1.png')):
                return b
    return name


def resolve(folder, name):
    """場面の画: stage = UI なしの画 (舞台の物差し)・ui = UI ありの画 (UI の物差し)・units = unitsonly。uiOnly = UI ありの画しか無い"""
    base = scene_base(folder, name)
    p = lambda suf: os.path.join(folder, base + suf)   # noqa: E731
    ex = lambda q: q if os.path.exists(q) else None    # noqa: E731
    stage = stage_lay = ui = ui_lay = None
    h = ex(p('-hideui-1.png'))
    if h:
        stage, stage_lay = h, ex(p('-hideui-1.layout.json'))
    one = ex(p('-1.png')); one_lay = ex(p('-1.layout.json'))
    if one:
        if is_hideui(_lay(one_lay)):
            if not stage:
                stage, stage_lay = one, one_lay
        else:
            ui, ui_lay = one, one_lay
    if not stage and not ui:
        raise SystemExit('画が無い: %s/%s-hideui-1.png か -1.png' % (folder, base))
    return dict(base=base, stage=stage or ui, stageLay=stage_lay if stage else ui_lay, ui=ui, uiLay=ui_lay,
                units=ex(p('-unitsonly-1.png')), uiOnly=stage is None)


def layout_ui_mask(lay, H, W):
    """layout.json の UI の矩形 (葉の node。キャラの絵・背景・全画面の入れ物・当たり判定は除く)"""
    m = np.zeros((H, W), bool)
    nodes = (lay or {}).get('nodes') or []
    paths = [n.get('path') or '' for n in nodes]
    for n, pth in zip(nodes, paths):
        if not pth.startswith('screen/') or '/sprite' in pth or pth.endswith('/bg') or 'catch' in pth:
            continue
        pre = pth + '/'
        if any(q.startswith(pre) for q in paths):
            continue
        x, y, w, h = n.get('px') or (0, 0, 0, 0)
        if w * h <= 0 or w * h > 0.25 * W * H:
            continue
        x0, y0, x1, y1 = T.clip_rect([x, y, w, h], W, H)
        m[y0:y1, x0:x1] = True
    return m


def _unit_sig(lay, key):
    """キャラ1体の目印 (同じキャラ・同じ板・同じ1ドットの px・同じ画面・同じ時刻なら同じ絵 = DET=1 の撮影)"""
    if not lay:
        return None
    ub = {u['key']: u for u in ((lay.get('stage') or {}).get('unitBoxes') or [])}
    u = ub.get(key)
    if not u:
        return None
    uid = next((x.get('id') for x in (lay.get('units') or []) if x.get('key') == key), None) or key
    return dict(id=uid, raw=u.get('boardRawPx') or u.get('boardPx'), board=u.get('boardPx'), ppd=u.get('pxPerDot'),
                screen=((lay.get('screen') or {}).get('w'), (lay.get('screen') or {}).get('h')), time=lay.get('time'))


def _same_sig(a, b):
    if not a or not b or a['id'] != b['id'] or a['screen'] != b['screen'] or a['ppd'] != b['ppd']:
        return False
    for k in ('raw', 'board'):
        if not a[k] or not b[k] or max(abs(p - q) for p, q in zip(a[k], b[k])) > 0.6:
            return False
    ta, tb = a.get('time'), b.get('time')
    return ta is None or tb is None or abs(ta - tb) <= 0.05


_DONORS = {}


def donors(folder):
    """フォルダの unitsonly の画 [(名前, png, layout)]"""
    if folder not in _DONORS:
        out = []
        for p in sorted(os.listdir(folder)) if os.path.isdir(folder) else []:
            if p.endswith('-unitsonly-1.png'):
                q = os.path.join(folder, p)
                out.append((p[:-len('-unitsonly-1.png')], q, _lay(q[:-4] + '.layout.json')))
        _DONORS[folder] = out
    return _DONORS[folder]


_SPLIT = {}


def donor_split(png, lay):
    """unitsonly の型をキャラごとに分ける {key: 型}。画素は板 (boardRawPx) に入るキャラのうち、絵の矩形 (boardPx) の中心がいちばん近い物へ"""
    if png in _SPLIT:
        return _SPLIT[png]
    u = T.load(png); H, W = u.shape[:2]
    ch = ~((u[..., 0] > 240) & (u[..., 1] < 20) & (u[..., 2] > 240))
    dub = {x['key']: x for x in ((lay or {}).get('stage', {}).get('unitBoxes') or [])}
    keys = [k for k in dub if dub[k].get('boardRawPx') or dub[k].get('boardPx')]
    ys, xs = np.nonzero(ch)
    best = np.full(len(ys), -1); bestd = np.full(len(ys), np.inf)
    for i, k in enumerate(keys):
        x, y, w, h = [int(round(v)) for v in (dub[k].get('boardRawPx') or dub[k].get('boardPx'))]
        inside = (xs >= x) & (xs < x + w) & (ys >= y) & (ys < y + h)
        bx, by, bw, bh = dub[k].get('boardPx') or (x, y, w, h)
        d = np.hypot(xs - (bx + bw / 2), ys - (by + bh / 2))
        sel = inside & (d < bestd)
        best[sel] = i; bestd[sel] = d[sel]
    out = {}
    for i, k in enumerate(keys):
        m = np.zeros((H, W), bool); sel = best == i
        m[ys[sel], xs[sel]] = True
        out[k] = m
    _SPLIT[png] = (out, (H, W))
    return _SPLIT[png]


def _ero(m):
    o = m.copy(); o[1:] &= m[:-1]; o[:-1] &= m[1:]; o[:, 1:] &= m[:, :-1]; o[:, :-1] &= m[:, 1:]
    return o


def aligned(L, hero):
    """借りた型が画に合うか: 型の輪郭 (内と外の1画素) の勾配が >20 の割合と、輪郭の勾配 ÷ 3〜6px 離れた所の勾配"""
    bd = (hero & ~_ero(hero)) | (T.dilate(hero, 1) & ~hero)
    e3 = hero
    for _ in range(3):
        e3 = _ero(e3)
    e6 = e3
    for _ in range(3):
        e6 = _ero(e6)
    near = (T.dilate(hero, 6) & ~T.dilate(hero, 3)) | (e3 & ~e6)
    gx = np.zeros_like(L); gy = np.zeros_like(L)
    gx[:, 1:-1] = np.abs(L[:, 2:] - L[:, :-2]); gy[1:-1] = np.abs(L[2:] - L[:-2])
    g = np.hypot(gx, gy)
    if bd.sum() < 100 or near.sum() < 100:
        return False, None, None
    strong = float((g[bd] > 20).mean()); ratio = float(g[bd].mean() / max(1e-6, g[near].mean()))
    return strong >= 0.5 and ratio >= 1.45, rnd(strong, 2), rnd(ratio, 2)


def borrow_units(s, folder):
    """キャラごとに、同じキャラ・同じ板・同じ時刻の unitsonly から型を借りる {key: (借りた先, 型)}。輪郭が画に合わない物は借りない"""
    out = {}
    L = None
    for key in (s.unitBoxes or {}):
        sig = _unit_sig(s.lay, key)
        if not sig:
            continue
        for nm, png, lay in donors(folder):
            if not _same_sig(sig, _unit_sig(lay, key)):
                continue
            parts, shape = donor_split(png, lay)
            if shape != (s.H, s.W) or key not in parts or parts[key].sum() < 300:
                continue
            if L is None:
                L = lum(s.a)
            ok, strong, ratio = aligned(L, parts[key])
            who = '主人公' if key == 'player' else key + ' '
            if ok:
                s.notes.append('%sの型は %s の unitsonly から借りた (輪郭の勾配 >20 の割合 %s・輪郭÷近く %s)' % (who, nm, strong, ratio))
                out[key] = (nm, parts[key])
                break
            s.notes.append('%s: %s の unitsonly は輪郭が合わない (%s・%s) = 借りない' % (who, nm, strong, ratio))
    return out


def make_shot(img, lay=None, units=None, folder=None, ui_only=False, name=None):
    """測る画1枚 (hd2d-r2-targets の Shot)。units が無ければ folder の unitsonly からキャラごとに型を借りる。ui_only = UI ありの画だけ (UI の矩形を除く)"""
    s = T.shot_from_files(img, lay, units)
    if name:
        s.name = name
    s.unitsSrc = os.path.basename(units) if units and os.path.exists(units) else None
    s.donor = None
    ensure_masks(s)
    if not s.umask and folder:
        got = borrow_units(s, folder)
        if got:
            ub = s.unitBoxes or {}
            rects = [ub[k]['boardPx'] for k in ub if k not in got and ub[k].get('boardPx')]
            known = np.zeros((s.H, s.W), bool)
            for _, m in got.values():
                known |= m
            s.charm = known | T.rectmask(s.a.shape, rects, 0)
            s.excl = T.dilate(known, 4) | T.rectmask(s.a.shape, rects, 4)
            names = sorted(set(nm for nm, _ in got.values()))
            s.charMask = 'unitsonly (%s から借りた: %s) + 他は板の矩形 +4px' % ('・'.join(names), '・'.join(got))
            s.umask = {k: m for k, (_, m) in got.items()}
            s.donor = '・'.join(n + '-unitsonly' for n in names)
            s.borrowed = sorted(got)
            if 'player' in got:
                ys, _ = np.nonzero(got['player'][1])
                if len(ys):
                    s.head = int(ys.min())
    s.uiOnly = bool(ui_only)
    if ui_only:
        um = layout_ui_mask(s.lay, s.H, s.W)
        s.uiMask = um
        s.excl = s.excl | T.dilate(um, 2)
        s.notes = [n for n in s.notes if not n.startswith('UI がある画')]
        s.notes.append('UI ありの画しか無い = UI を layout.json の矩形で除いて測った (舞台の物差しは合否なし)')
    return s


def box_count(m, r):
    """(2r+1)×(2r+1) の窓の中の True の数"""
    c = np.pad(m.astype(np.int32), ((r + 1, r), (r + 1, r))).cumsum(0).cumsum(1)
    k = 2 * r + 1
    return c[k:, k:] - c[:-k, k:] - c[k:, :-k] + c[:-k, :-k]


def ui_measure(ui_img, ui_lay, stage_shot, r):
    """UI の物差し (U1〜U4・参考)。UI = 双子の UI なしの画との差 (>12)、双子が無ければ layout.json の UI の矩形"""
    a = T.load(ui_img); H, W = a.shape[:2]
    lay = _lay(ui_lay) or {}
    twin = None
    if stage_shot is not None and stage_shot.path != ui_img and stage_shot.lay and stage_shot.a.shape == a.shape:
        st_u = (lay.get('state') or '').replace(';hideui=1', ''); st_s = (stage_shot.lay.get('state') or '').replace(';hideui=1', '')
        if st_u == st_s:
            twin = stage_shot
    if twin is not None:
        m = np.abs(a.astype(np.int16) - twin.a.astype(np.int16)).max(-1) > 12
        m &= box_count(m, 4) >= 12   # 1 コマ違いの月の塵の粒 (孤立した数画素) は UI でない
        src = '%s との差' % os.path.basename(twin.path)
    else:
        m = layout_ui_mask(lay, H, W)
        src = 'layout.json の UI の矩形'
    L = lum(a)
    top = 0
    tb = (lay.get('anchors') or {}).get('topbar')
    if tb:
        top = int(round(tb[1] + tb[3]))
    feet = int(round(r.get('feet') or H * 0.6))
    hand = np.zeros((H, W), bool)
    for hd in lay.get('hand') or []:
        x0, y0, x1, y1 = T.clip_rect(hd['px'], W, H); hand[y0:y1, x0:x1] = True
    n3 = (r.get('core') or {}).get('N3')
    out = dict(img=os.path.basename(ui_img), maskSrc=src, cover=rnd(float(m.mean())),
               pictureCover=rnd(float(m[top:feet].mean())) if feet > top else None, rows=[top, feet])
    mo = m & ~hand   # 手札の外 (紙は手札・確認の窓・メニューだけ = 舞台の上の札は夜色。2026-09-30 の裁定)
    if mo.sum() > 100:
        p95 = float(np.percentile(L[mo], 95))
        out.update(p95=rnd(p95, 1), p95OverPeak=rnd(p95 / n3, 2) if n3 else None)
        if n3:
            out['brightOutsideHand'] = rnd(float((mo & (L > n3)).sum()) / (H * W) * 1e4, 1)
    return out


def measure_scene(folder, scene, label=None, group=None):
    """場面1つ: 舞台は UI なしの画・UI の物差しは UI ありの画 (resolve の説明)"""
    q = resolve(folder, scene)
    s = make_shot(q['stage'], q['stageLay'], q['units'], folder=folder, ui_only=q['uiOnly'], name=q['base'])
    r = measure(s)
    r['src']['scene'] = q['base']
    if q['ui']:
        r['ui'] = ui_measure(q['ui'], q['uiLay'], None if q['uiOnly'] else s, r)
    if label:
        r['label'] = label
    if group:
        r['group'] = group
    return r


def all_scenes(folder):
    """フォルダの場面 (名前の末尾の -hideui・-unitsonly・-uionly を外して重ねない。unitsonly・uionly だけの場面は除く)"""
    out = []
    for p in sorted(os.listdir(folder)):
        if not p.endswith('-1.png'):
            continue
        b = p[:-6]
        if b.endswith('-unitsonly') or b.endswith('-uionly'):
            continue
        b = scene_base(folder, b)
        if b not in out:
            out.append(b)
    return out


def scene_list(folder, names):
    """渡された名前を場面にそろえ、同じ場面は1つに (順は最初に出た所)"""
    out = []
    for n in names:
        b = scene_base(folder, n)
        if b.endswith('-unitsonly') or b.endswith('-uionly'):
            continue
        if b not in out:
            out.append(b)
        elif b != n:
            print('注: %s は場面 %s と同じ列 (舞台は UI なしの画・UI の物差しは UI ありの画)' % (n, b))
    return out


def calibrate(r2dir, w5dir):
    res = []
    for k in ('ot16', 'ot7', 'ot11'):
        try:
            r = measure(T.shot_from_ref(k, REF_DIRS)); r['label'] = '本家 ' + k; r['group'] = 'ref'; res.append(r)
        except SystemExit as e:
            print('本家が無い:', e)
    for sc_, lab in (('PC-S-ogre', 'オーガ'), ('PC-S-wolf', '狼'), ('PC-S-quad', '4体'), ('PC-S-dolls', '人形9体'), ('PH-S-ogre', 'スマホ オーガ')):
        try:
            res.append(measure_scene(r2dir, sc_, '二周目 ' + lab, 'r2'))
        except SystemExit as e:
            print('二周目の画が無い:', e)
    if os.path.isdir(os.path.join(w5dir, 'hero62')):
        try:
            res.append(measure_scene(os.path.join(w5dir, 'hero62'), 'PC-S-ogre', 'W5 オーガ', 'w5'))
        except SystemExit as e:
            print('W5 の画が無い:', e)
    lay = os.path.join(r2dir, 'PC-S-ogre-hideui-1.layout.json'); un = os.path.join(r2dir, 'PC-S-ogre-unitsonly-1.png')
    for lab, p in MOCKS:
        if os.path.exists(p) and os.path.exists(lay):
            s = T.shot_from_files(p, lay, un if os.path.exists(un) else None); s.name = lab
            r = measure(s); r['label'] = lab; r['group'] = 'mock'; res.append(r)
    return res


def main():
    ap = argparse.ArgumentParser(description='HD-2D 見本 三周目の物差し (R12)')
    ap.add_argument('folder', nargs='?'); ap.add_argument('scenes', nargs='*')
    ap.add_argument('--image'); ap.add_argument('--layout'); ap.add_argument('--units')
    ap.add_argument('--ref', action='append', help='本家 (ot16 など。何回でも)')
    ap.add_argument('--ref-dir', action='append', default=[])
    ap.add_argument('--all', help='フォルダの場面を全部 (UI ありだけの場面は「(UI あり)」の列)')
    ap.add_argument('--calibrate', action='store_true', help='本家・二周目・W5・模型を測って分類の根拠の表を出す')
    ap.add_argument('--r2', default=DEFAULT_R2, help='二周目の最終の撮影 (r2-slice)')
    ap.add_argument('--w5', default=None, help='W5 の shots (hero62・slice の親)')
    ap.add_argument('--json'); ap.add_argument('--md')
    ap.add_argument('--quiet', action='store_true')
    a = ap.parse_args()
    global REF_DIRS
    REF_DIRS = a.ref_dir + REF_DIRS
    res = []
    if a.calibrate:
        res += calibrate(a.r2, a.w5 or default_w5())
    for k in a.ref or []:
        r = measure(T.shot_from_ref(k, REF_DIRS)); r['label'] = '本家 ' + k; res.append(r)
    if a.image:
        lay = a.layout or (a.image[:-4] + '.layout.json')
        uionly = bool(_lay(lay)) and not is_hideui(_lay(lay))
        s = make_shot(a.image, lay, a.units, folder=os.path.dirname(os.path.abspath(a.image)), ui_only=uionly)
        res.append(measure(s))
    if a.all:
        for sc_ in all_scenes(a.all):
            res.append(measure_scene(a.all, sc_))
    if a.folder:
        for sc_ in scene_list(a.folder, a.scenes):
            res.append(measure_scene(a.folder, sc_))
    if not res:
        ap.error('測る画が無い')
    if not a.quiet:
        for r in res:
            print(text_report(r))
    md = md_table(res)
    if a.calibrate:
        md += '\n' + calib_table(res)
    if a.md:
        os.makedirs(os.path.dirname(os.path.abspath(a.md)), exist_ok=True)
        open(a.md, 'w', encoding='utf-8').write(md)
    elif a.calibrate and not a.quiet:
        print('\n' + md)
    if a.json:
        os.makedirs(os.path.dirname(os.path.abspath(a.json)), exist_ok=True)
        json.dump(res, open(a.json, 'w', encoding='utf-8'), ensure_ascii=False, indent=1, default=lambda o: o.item() if hasattr(o, 'item') else str(o))


if __name__ == '__main__':
    main()
