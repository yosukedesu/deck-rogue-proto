# build.py — 「伏せ」の言い換え比較シート・第3版 (2026-09-10)。
#
# 経緯:
#   第1版「狩人の仕掛け（獣道に罠を張る）」→ ユーザー却下「緑のこのはだけができるものじゃないから」
#   第2版「夜/月/息/隠し手（色中立の抽象）」→ ユーザー却下「案はすべてイメージしにくい。罠を設置とかはわかりやすい」
#   第3版（これ）= **物として目に見える** × **色に依存しない**。
#   前提の訂正: 「罠」自体は色をまたいでいる（囁きの罠=青）。却下されたのは語ではなく「狩人が獣道に」という物語の枠。
#   よって物の具体性は残し、置く場所を「足元／敵とのあいだの地面」にして、誰の職能でもなくする。
#
# 見せ方: 各案を **3コマの絵** で出す（①置く ②敵が動く ③作動する）。言葉の表より先に絵で分かること。
# python3 build.py → Main.dc.html + canvas.json → seed-canvas.mjs で公開。
import json, os, html

OUT = os.path.dirname(os.path.abspath(__file__))
INK = '#3b2f2f'; INK_MID = '#574b48'; PAPER = '#f4ecd6'; PAPER2 = '#eadfc4'
HONEY = '#e0b25a'; MOSS = '#8fae7b'; SKY = '#7fa7c9'; ROSE = '#d97b7b'

HEAD = '''<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <script src="./support.js"></script>
</head>
<body>
<x-dc>
<helmet>
  <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Kaisei+Decol:wght@400;700&amp;family=Klee+One:wght@400;600&amp;display=swap">
  <style>
    body { margin: 0; background: #1a1c33; font-family: "Klee One", "Hiragino Sans", sans-serif; color: #3b2f2f; -webkit-font-smoothing: antialiased; }
    .abs { position: absolute; }
    .deco { font-family: "Kaisei Decol", "Hiragino Mincho ProN", serif; font-weight: 700; }
    .light { color: #f4ecd6; }
    .paper { background: #f4ecd6; color: #3b2f2f; border-radius: 16px 20px 14px 18px / 18px 14px 20px 16px; box-shadow: 0 0 0 1.5px #3b2f2f, 0 0 0 4px #f4ecd6, 0 0 0 5.5px rgba(59,47,47,0.5), 0 10px 22px rgba(0,0,0,0.35); }
    .note { font-size: 13px; line-height: 20px; color: #f4ecd6; text-shadow: 0 1px 0 rgba(0,0,0,0.6); }
    .tag { display: inline-block; padding: 2px 10px; border-radius: 8px; font-size: 12px; box-shadow: 0 0 0 1.5px #3b2f2f; background: #f4ecd6; }
    .cap { font-size: 12px; color: #574b48; margin-top: 5px; line-height: 16px; }
    .step { font-family: "Kaisei Decol", serif; font-weight: 700; font-size: 13px; color: #3b2f2f; }
    table.v { border-collapse: collapse; width: 100%; font-size: 13px; }
    table.v th, table.v td { border-bottom: 1px solid rgba(59,47,47,0.22); padding: 7px 9px; vertical-align: top; text-align: left; }
    table.v th { font-family: "Kaisei Decol", serif; font-size: 15px; background: rgba(59,47,47,0.07); }
    table.v td.cur { color: #574b48; background: rgba(59,47,47,0.04); width: 112px; font-size: 12px; }
    .big { font-family: "Kaisei Decol", serif; font-weight: 700; font-size: 16px; }
  </style>
</helmet>
'''
TAIL = '</x-dc>\n</body>\n</html>\n'


def esc(s):
    return html.escape(str(s or ''))


# ---------- 3コマの絵 ----------
W, H = 250, 165


def scene(obj_svg='', hero_x=0.16, beast_x=0.74, beast_step=0.0, flash=False, beast_dim=False):
    """夜の地面・リーダー・魔物。obj_svg は地面に置かれた物"""
    bx = beast_x - beast_step
    s = (f'<rect width="{W}" height="{H}" fill="#20233a"/>'
         f'<ellipse cx="{W*0.5}" cy="{H*0.80}" rx="{W*0.58}" ry="{H*0.22}" fill="#5a4636" opacity="0.85"/>'
         f'<ellipse cx="{W*0.5}" cy="{H*0.80}" rx="{W*0.36}" ry="{H*0.13}" fill="#6b5340" opacity="0.9"/>')
    s += f'<circle cx="{W*0.90}" cy="{H*0.14}" r="10" fill="#f4ecd6" opacity="0.9"/><circle cx="{W*0.90}" cy="{H*0.14}" r="20" fill="#f4ecd6" opacity="0.10"/>'
    if flash:
        s += f'<ellipse cx="{W*0.5}" cy="{H*0.70}" rx="{W*0.5}" ry="{H*0.32}" fill="#ffe9a8" opacity="0.20"/>'
    # リーダー（色を持たない中立の姿）
    hx = W * hero_x
    s += (f'<g transform="translate({hx},{H*0.34})"><circle cx="16" cy="13" r="11" fill="#e9c9a8"/>'
          f'<path d="M5 11 a11 11 0 0 1 22 0 z" fill="#4a3a5a"/>'
          f'<rect x="5" y="24" width="22" height="28" rx="6" fill="#3d5f6e"/>'
          f'<rect x="0" y="25" width="5" height="24" fill="#3b2f2f"/><rect x="27" y="25" width="5" height="24" fill="#3b2f2f"/></g>')
    # 魔物（月光で淡く光る）
    op = 0.45 if beast_dim else 1.0
    s += (f'<g transform="translate({W*bx},{H*0.32})" opacity="{op}">'
          f'<ellipse cx="24" cy="30" rx="26" ry="24" fill="#dfe9ee" opacity="0.14"/>'
          f'<ellipse cx="24" cy="30" rx="22" ry="19" fill="#b9c3c9"/>'
          f'<circle cx="24" cy="13" r="12" fill="#c7cfd4"/>'
          f'<circle cx="19" cy="12" r="2" fill="#2a2a33"/><circle cx="29" cy="12" r="2" fill="#2a2a33"/></g>')
    return f'<svg width="{W}" height="{H}" viewBox="0 0 {W} {H}" style="display:block;border-radius:9px">{s}{obj_svg}</svg>'


CX, CY = W * 0.47, H * 0.76


def art_shikake(step):
    """仕掛け: 地面に置いた小さな装置（板と留め金）"""
    base = (f'<g transform="translate({CX-16},{CY-22})">'
            f'<rect x="0" y="10" width="32" height="12" rx="2" fill="#8a6a3c" stroke="#3b2f2f" stroke-width="1.2"/>'
            f'<rect x="4" y="2" width="24" height="9" rx="2" fill="#c9a982" stroke="#3b2f2f" stroke-width="1.2"/>'
            f'<line x1="16" y1="2" x2="16" y2="-6" stroke="#3b2f2f" stroke-width="1.4"/></g>')
    if step == 0:
        return base + f'<circle cx="{CX}" cy="{CY-34}" r="10" fill="#e0b25a" opacity="0.18"/>'
    if step == 1:
        return base
    # 作動: 板が跳ね上がる
    return (f'<g transform="translate({CX-16},{CY-22})">'
            f'<rect x="0" y="10" width="32" height="12" rx="2" fill="#8a6a3c" stroke="#3b2f2f" stroke-width="1.2"/>'
            f'<g transform="rotate(-55 4 10)"><rect x="4" y="2" width="24" height="9" rx="2" fill="#f0d58a" stroke="#3b2f2f" stroke-width="1.2"/></g></g>'
            f'<path d="M{CX+6} {CY-30} L{CX+38} {CY-44}" stroke="#ffe9a8" stroke-width="3" stroke-linecap="round"/>')


def art_ito(step):
    """糸: 二点に張った細い糸。発動＝手元に引く"""
    y = CY - 20
    if step == 2:
        return (f'<path d="M{W*0.22} {y} Q {CX} {y-26} {W*0.72} {y-6}" stroke="#ffe9a8" stroke-width="2.4" fill="none"/>'
                f'<circle cx="{W*0.22}" cy="{y}" r="3" fill="#c9a982"/>'
                f'<path d="M{W*0.60} {y-16} l 10 -12 M{W*0.66} {y-12} l 12 -6" stroke="#ffe9a8" stroke-width="2" stroke-linecap="round"/>')
    
    col = '#e8dfc8' if step == 0 else '#8e8776'
    s = (f'<line x1="{W*0.22}" y1="{y}" x2="{W*0.74}" y2="{y-8}" stroke="{col}" stroke-width="1.6"/>'
         f'<circle cx="{W*0.22}" cy="{y}" r="3" fill="#c9a982" stroke="#3b2f2f" stroke-width="1"/>'
         f'<circle cx="{W*0.74}" cy="{y-8}" r="3" fill="#c9a982" stroke="#3b2f2f" stroke-width="1"/>')
    if step == 0:
        s += ''.join(f'<circle cx="{W*0.30+i*22}" cy="{y-4-(i%2)*5}" r="1.6" fill="#e0b25a" opacity="0.9"/>' for i in range(5))
    return s


def art_tomoshibi(step):
    """灯: 地面に置いた小さな明かり。発動＝ぱっと燃え上がる"""
    body = (f'<g transform="translate({CX-9},{CY-20})">'
            f'<rect x="0" y="8" width="18" height="12" rx="3" fill="#8a6a3c" stroke="#3b2f2f" stroke-width="1.2"/>'
            f'<rect x="3" y="2" width="12" height="8" rx="2" fill="#f4ecd6" stroke="#3b2f2f" stroke-width="1"/></g>')
    if step == 0:
        return (body + f'<circle cx="{CX}" cy="{CY-14}" r="16" fill="#ffd27a" opacity="0.28"/>'
                f'<circle cx="{CX}" cy="{CY-14}" r="28" fill="#ffd27a" opacity="0.10"/>')
    if step == 1:
        return body + f'<circle cx="{CX}" cy="{CY-14}" r="13" fill="#ffd27a" opacity="0.18"/>'
    return (body + f'<circle cx="{CX}" cy="{CY-16}" r="34" fill="#ffe9a8" opacity="0.34"/>'
            f'<circle cx="{CX}" cy="{CY-16}" r="18" fill="#fff3c4" opacity="0.7"/>'
            f'<path d="M{CX} {CY-40} l -7 14 l 7 -4 l 7 4 z" fill="#fff3c4"/>')


def art_kui(step):
    """杭と包み: 地面に打った杭に吊るした布包み"""
    stake = (f'<rect x="{CX-2}" y="{CY-30}" width="4" height="30" fill="#8a6a3c" stroke="#3b2f2f" stroke-width="1"/>')
    if step == 2:
        return (stake + f'<path d="M{CX+2} {CY-28} L{CX+34} {CY-40}" stroke="#ffe9a8" stroke-width="3" stroke-linecap="round"/>'
                + ''.join(f'<circle cx="{CX+8+i*7}" cy="{CY-30-i*3}" r="2" fill="#f0d58a"/>' for i in range(4)))
    s = stake + (f'<line x1="{CX}" y1="{CY-28}" x2="{CX+12}" y2="{CY-20}" stroke="#c9a982" stroke-width="1.2"/>'
                 f'<ellipse cx="{CX+14}" cy="{CY-16}" rx="8" ry="9" fill="#e8dfc8" stroke="#3b2f2f" stroke-width="1.2"/>')
    if step == 0:
        s += f'<ellipse cx="{CX+10}" cy="{CY-22}" rx="22" ry="16" fill="#cfe4ff" opacity="0.18"/>'
    return s


def art_karakuri(step):
    """カラクリ: 足元に据えた小さな木の匣（歯車と発条）。発動＝蓋がはじけて中身が飛び出す"""
    bx, by = CX - 17, CY - 26
    box = (f'<g transform="translate({bx},{by})">'
           f'<rect x="0" y="8" width="34" height="18" rx="3" fill="#a8763f" stroke="#3b2f2f" stroke-width="1.4"/>'
           f'<rect x="3" y="12" width="28" height="10" rx="2" fill="#8a6a3c"/>'
           f'<circle cx="10" cy="17" r="4.2" fill="none" stroke="#e0b25a" stroke-width="1.6"/>'
           f'<circle cx="10" cy="17" r="1.4" fill="#e0b25a"/>'
           f'<circle cx="22" cy="19" r="3" fill="none" stroke="#c9a982" stroke-width="1.3"/>')
    if step == 0:
        # 蓋を閉じ、ぜんまいの取っ手を回している
        return (box + f'<rect x="0" y="3" width="34" height="6" rx="2" fill="#c9a982" stroke="#3b2f2f" stroke-width="1.3"/>'
                f'<path d="M34 14 q 8 -2 6 -8" fill="none" stroke="#3b2f2f" stroke-width="1.4"/>'
                f'<circle cx="40" cy="5" r="2.6" fill="#c9a982" stroke="#3b2f2f" stroke-width="1.1"/></g>'
                f'<circle cx="{CX}" cy="{CY-40}" r="11" fill="#e0b25a" opacity="0.20"/>')
    if step == 1:
        # 閉じたまま。歯車がひとつ噛んでいる印
        return (box + f'<rect x="0" y="3" width="34" height="6" rx="2" fill="#c9a982" stroke="#3b2f2f" stroke-width="1.3"/></g>'
                f'<circle cx="{CX-7}" cy="{CY-9}" r="1.5" fill="#e0b25a" opacity="0.85"/>')
    # 作動: 蓋がはじけ、中身が敵へ飛ぶ
    return (box + '</g>'
            f'<g transform="rotate(-52 {bx} {by+4})"><rect x="{bx}" y="{by+1}" width="34" height="6" rx="2" fill="#f0d58a" stroke="#3b2f2f" stroke-width="1.3"/></g>'
            f'<path d="M{CX+4} {CY-30} L{CX+40} {CY-46}" stroke="#ffe9a8" stroke-width="3.2" stroke-linecap="round"/>'
            + ''.join(f'<circle cx="{CX+10+i*9}" cy="{CY-34-i*4}" r="2.1" fill="#f0d58a"/>' for i in range(4)))


ART = {'shikake': art_shikake, 'ito': art_ito, 'tomoshibi': art_tomoshibi, 'kui': art_kui, 'karakuri': art_karakuri}


def strip(key, steps):
    """3コマ: ①置く ②敵が動く（物はそのまま）③作動"""
    fn = ART[key]
    out = '<div style="display: flex; gap: 8px">'
    cfg = [dict(step=0, beast_step=0.0), dict(step=1, beast_step=0.10), dict(step=2, beast_step=0.10, flash=True, beast_dim=True)]
    for i, c in enumerate(cfg):
        svg = scene(fn(c['step']), beast_step=c['beast_step'], flash=c.get('flash', False), beast_dim=c.get('beast_dim', False))
        out += f'<div style="width: {W}px"><div class="step">{esc(steps[i][0])}</div>{svg}<div class="cap">{esc(steps[i][1])}</div></div>'
    return out + '</div>'


ROWS = [
    ('伏せ札', 'noun_card'), ('伏せ場', 'noun_zone'), ('伏せ枠', 'noun_slot'), ('伏せる', 'verb_set'),
    ('発動', 'verb_fire'), ('温存', 'verb_hold'), ('回収（1E）', 'verb_retrieve'),
    ('伏せ破壊', 'enemy_destroy'), ('鮮度（新しい札だけ敵が反応）', 'freshness'),
    ('タイプ名', 'card_type_name'),
]


def board(pkgs):
    BW = 1760
    s = f'<div style="position: relative; width: {BW}px; background: #1a1c33; padding-bottom: 44px">'
    s += '<div class="abs" style="inset: 0; background: linear-gradient(180deg, #26294a 0%, #12142a 100%)"></div>'
    s += '<div style="position: relative; padding: 26px 40px 0 40px">'
    s += '<div class="deco light" style="font-size: 30px; letter-spacing: 0.06em; text-shadow: 0 2px 0 rgba(0,0,0,0.5)">「伏せ」を世界の言葉に — 第3版（目に見える物）</div>'
    s += ('<div class="note" style="margin-top: 8px; width: 1580px">'
          '第1版（狩人の仕掛け）は<b>緑の職能に見える</b>ため却下。第2版（夜・月・間・懐）は<b>イメージしにくい</b>ため却下。'
          'そこで第3版は <b>物として目に見える × どの色の道具でもない</b> に絞った。'
          '前提の訂正: 「罠」自体は色をまたいでいる（<b>囁きの罠は青</b>）。却下されたのは語ではなく「狩人が獣道に」という<b>物語の枠</b>だったので、'
          '物の具体性は残し、置く場所を<b>足元／敵とのあいだの地面</b>にして誰の職能でもなくする。'
          '下の「物」はすべて実データで未使用（からくり・仕掛・糸・灯・杭は名前に一度も出てこない）。3コマで因果が読めることを最優先にした。'
          '<b>カラクリ</b>はユーザーの提案（2026-09-10）。塔は幕2＝工房の名残・歯車、幕3＝作られたもの、と元から作り物の場所なので相性がよい。</div>')

    for p in pkgs:
        pk = p['package']
        s += '<div class="paper" style="position: relative; margin-top: 26px; padding: 18px 22px">'
        s += (f'<div style="display: flex; align-items: baseline; gap: 12px"><span class="deco" style="font-size: 22px">{esc(p["title"])}</span>'
              f'<span class="tag">{esc(pk.get("one_liner",""))}</span></div>')
        s += f'<div style="font-size: 13px; color: {INK_MID}; margin: 6px 0 12px; line-height: 19px">{esc(pk.get("concept",""))}</div>'
        s += '<div style="display: flex; gap: 26px; align-items: flex-start">'
        s += f'<div>{strip(p["key"], pk["steps"])}</div>'
        # 語彙の小表
        s += '<div style="flex: 1"><table class="v">'
        for label, key in ROWS:
            s += f'<tr><td class="cur">{esc(label)}</td><td><span class="big">{esc(pk.get(key,""))}</span></td></tr>'
        s += '</table></div>'
        s += '</div>'
        # 画面文と懸念
        lines = ''.join(f'<div style="padding: 5px 10px; margin: 4px 0; background: rgba(59,47,47,0.06); border-radius: 8px; font-size: 12.5px; line-height: 18px">{esc(l)}</div>' for l in pk.get('sample_lines', []))
        s += (f'<div style="display: flex; gap: 20px; margin-top: 12px">'
              f'<div style="flex: 1"><div style="font-size: 12px; color: {INK_MID}; margin-bottom: 3px">画面に出る文</div>{lines}</div>'
              f'<div style="flex: 1"><div style="font-size: 12px; color: {INK_MID}; margin-bottom: 3px">4色で読めるか</div>'
              + ''.join(f'<div style="font-size: 12.5px; line-height: 19px">{esc(t)}</div>' for t in pk.get('four_color', []))
              + f'<div style="font-size: 12px; color: {INK_MID}; margin-top: 8px; line-height: 17px"><b>懸念:</b> {esc(pk.get("concerns",""))}</div></div></div>')
        s += '</div>'

    s += '</div></div>'
    return s


def write(name, body):
    with open(os.path.join(OUT, name), 'w', encoding='utf-8') as f:
        f.write(HEAD + body + TAIL)


pkgs = json.load(open(os.path.join(OUT, 'packages3.json'), encoding='utf-8'))
write('Main.dc.html', board(pkgs))
json.dump({
    'artboards': [{'file': 'Main.dc.html', 'title': '「伏せ」の言い換え（目に見える物・4案）', 'x': 0, 'y': 0, 'w': 1760, 'h': 2600, 'expand': 'fill'}],
    'annotations': [{'id': 'brief', 'x': 0, 'y': -150, 'w': 720,
                     'text': '第3版（2026-09-10）。第1版=緑の職能に見える／第2版=イメージしにくい、の2回の却下を受け、'
                             '「目に見える物 × どの色の道具でもない」に絞った。各案を3コマ（置く→敵が動く→作動）で見せる。'}],
    'launch': {'view': 'canvas'},
}, open(os.path.join(OUT, 'canvas.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
print('written v3 sheet for', len(pkgs), 'packages')
