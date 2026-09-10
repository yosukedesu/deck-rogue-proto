# build.py — 「伏せ」の言い換え比較シート (2026-09-10 ユーザー「伏せって概念を戦闘世界観に合わせた形にしたいな」)。
# 第2版: ユーザー却下「いや緑のこのはだけができるものじゃないから」を受け、色中立な枠だけで作り直した4案を比較する。
# packages.json（各案: 語彙パッケージ + 検査結果 + 4色テスト）を読み、
#   上段=舞台での見え方（左端は実機のスクショ）／中段=語彙の対照表／下段=4色テストと画面文
# を1枚のボードに並べる。python3 build.py → Main.dc.html + canvas.json → seed-canvas.mjs で公開。
import json, os, html

OUT = os.path.dirname(os.path.abspath(__file__))
INK = '#3b2f2f'; INK_MID = '#574b48'; PAPER = '#f4ecd6'; PAPER2 = '#eadfc4'; NIGHT = '#1a1c33'
HONEY = '#e0b25a'; MOSS = '#8fae7b'; SKY = '#7fa7c9'; PLUM = '#a98cc4'; TEAL = '#7ab8b0'
COLOR_INK = {'緑': '#2f6e40', '青': '#2f5a7a', '白': '#7a5a1a', '黒': '#5a3d78'}
OK_BG = {'自然': '#cfeacc', 'やや無理': '#f6e3b0', '無理': '#f0c9c2'}

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
    table.vocab { border-collapse: collapse; width: 100%; font-size: 13px; }
    table.vocab th, table.vocab td { border-bottom: 1px solid rgba(59,47,47,0.22); padding: 7px 9px; vertical-align: top; text-align: left; }
    table.vocab th { font-family: "Kaisei Decol", serif; font-size: 15px; background: rgba(59,47,47,0.07); }
    table.vocab td.cur { color: #574b48; background: rgba(59,47,47,0.04); width: 118px; font-size: 12px; }
    .tag { display: inline-block; padding: 1px 9px; border-radius: 8px; font-size: 12px; box-shadow: 0 0 0 1.5px #3b2f2f; background: #f4ecd6; }
    .kw { font-family: "Kaisei Decol", serif; font-weight: 700; }
    .px { image-rendering: pixelated; }
    .big { font-family: "Kaisei Decol", serif; font-weight: 700; font-size: 17px; }
  </style>
</helmet>
'''
TAIL = '''</x-dc>
</body>
</html>
'''


def esc(s):
    return html.escape(str(s or ''))


def short(s, n=999):
    """語彙セルは先頭の語だけを大きく、補足を小さく出す"""
    s = str(s or '')
    for sep in ('（', '。', '——', '(', '／'):
        if sep in s:
            head, rest = s.split(sep, 1)
            if 0 < len(head) <= 14:
                return head, (sep if sep in '（(' else '') + rest
    return s, ''


def cell(v):
    head, rest = short(v)
    out = f'<span class="big">{esc(head)}</span>'
    if rest:
        out += f'<div style="font-size: 11.5px; color: {INK_MID}; line-height: 16px; margin-top: 2px">{esc(rest[:150])}</div>'
    return out


def glyph(key, w=300, h=150):
    """舞台の簡易の絵。案ごとに「置かれた一手」を別の物として描く（色に依存しない物であること）"""
    g = (f'<rect width="{w}" height="{h}" fill="#20233a"/>'
         f'<ellipse cx="{w*0.5}" cy="{h*0.78}" rx="{w*0.55}" ry="{h*0.22}" fill="#5a4636" opacity="0.85"/>'
         f'<ellipse cx="{w*0.5}" cy="{h*0.78}" rx="{w*0.34}" ry="{h*0.13}" fill="#6b5340" opacity="0.9"/>')
    moon = f'<circle cx="{w*0.88}" cy="{h*0.15}" r="12" fill="#f4ecd6" opacity="0.92"/><circle cx="{w*0.88}" cy="{h*0.15}" r="24" fill="#f4ecd6" opacity="0.10"/>'
    hero = (f'<g transform="translate({w*0.14},{h*0.36})"><circle cx="18" cy="14" r="12" fill="#e9c9a8"/>'
            f'<rect x="6" y="26" width="24" height="30" rx="6" fill="#3d5f6e"/>'
            f'<rect x="0" y="27" width="6" height="26" fill="#3b2f2f"/><rect x="30" y="27" width="6" height="26" fill="#3b2f2f"/>'
            f'<path d="M6 12 a12 12 0 0 1 24 0 z" fill="#4a3a5a"/></g>')
    beast = (f'<g transform="translate({w*0.72},{h*0.34})"><ellipse cx="26" cy="30" rx="26" ry="22" fill="#b9c3c9" opacity="0.9"/>'
             f'<circle cx="26" cy="12" r="14" fill="#c7cfd4"/><circle cx="21" cy="11" r="2" fill="#2a2a33"/><circle cx="31" cy="11" r="2" fill="#2a2a33"/>'
             f'<ellipse cx="26" cy="22" rx="30" ry="26" fill="#dfe9ee" opacity="0.14"/></g>')
    cx, cy = w * 0.46, h * 0.74
    if key == 'dark':      # 夜に隠す: 足元に落ちた濃い影のくぼみ。中で何かが小さく光る
        obj = (f'<ellipse cx="{cx}" cy="{cy}" rx="30" ry="12" fill="#0b0d18"/>'
               f'<ellipse cx="{cx}" cy="{cy}" rx="22" ry="8" fill="#05060d"/>'
               f'<circle cx="{cx}" cy="{cy-2}" r="2.5" fill="#9fd8ff" opacity="0.9"/>'
               f'<ellipse cx="{cx}" cy="{cy}" rx="34" ry="15" fill="none" stroke="#9fd8ff" stroke-width="1" opacity="0.35" stroke-dasharray="3 5"/>')
    elif key == 'moon':    # 月に預ける: 月光の細い柱が地面に落ち、その中に薄い板が浮く
        obj = (f'<path d="M{w*0.88} {h*0.15} L{cx-16} {cy} L{cx+16} {cy} Z" fill="#f4ecd6" opacity="0.13"/>'
               f'<rect x="{cx-11}" y="{cy-34}" width="22" height="30" rx="2" fill="#e8eef4" opacity="0.9" stroke="#f4ecd6" stroke-width="1"/>'
               f'<ellipse cx="{cx}" cy="{cy}" rx="20" ry="7" fill="#f4ecd6" opacity="0.25"/>')
    elif key == 'breath':  # 息を置く: 足元の淡い印と、集まった光の粒
        obj = (f'<circle cx="{cx}" cy="{cy}" r="24" fill="none" stroke="#e0b25a" stroke-width="1.6" opacity="0.75" stroke-dasharray="5 5"/>'
               f'<circle cx="{cx}" cy="{cy}" r="13" fill="none" stroke="#e0b25a" stroke-width="1.2" opacity="0.55"/>'
               + ''.join(f'<circle cx="{cx-18+i*9}" cy="{cy-16-(i%3)*7}" r="1.8" fill="#e0b25a" opacity="0.9"/>' for i in range(5)))
    else:                  # 隠し手: 手元に伏せた無地の板（世界の物＝木札）
        obj = (f'<g transform="rotate(-6 {cx} {cy})"><rect x="{cx-15}" y="{cy-36}" width="30" height="40" rx="3" fill="#c9a982" stroke="#3b2f2f" stroke-width="1.5"/>'
               f'<rect x="{cx-11}" y="{cy-32}" width="22" height="32" rx="2" fill="none" stroke="#8a6a3c" stroke-width="1"/></g>'
               f'<ellipse cx="{cx}" cy="{cy+2}" rx="18" ry="6" fill="#000" opacity="0.35"/>')
    return f'<svg width="{w}" height="{h}" viewBox="0 0 {w} {h}" style="display:block;border-radius:10px">{g}{moon}{hero}{beast}{obj}</svg>'


ROWS = [
    ('伏せ札', 'noun_card'), ('伏せ場', 'noun_zone'), ('伏せ枠', 'noun_slot'), ('伏せる', 'verb_set'),
    ('発動', 'verb_fire'), ('温存', 'verb_hold'), ('回収（1E）', 'verb_retrieve'),
    ('伏せ破壊', 'enemy_destroy'), ('タイプ名', 'card_type_name'),
    ('鮮度（新しい札だけ敵が反応）', 'freshness'), ('ブラフ', 'bluff'),
]


def board(pkgs):
    W = 1760
    n = max(1, len(pkgs))
    colw = int((W - 80 - (n - 1) * 20) / n)
    s = f'<div style="position: relative; width: {W}px; background: #1a1c33; padding-bottom: 44px">'
    s += '<div class="abs" style="inset: 0; background: linear-gradient(180deg, #26294a 0%, #12142a 100%)"></div>'
    s += '<div style="position: relative; padding: 26px 40px 0 40px">'
    s += '<div class="deco light" style="font-size: 30px; letter-spacing: 0.06em; text-shadow: 0 2px 0 rgba(0,0,0,0.5)">「伏せ」を世界の言葉に — 第2版（色に依存しない枠）</div>'
    s += ('<div class="note" style="margin-top: 8px; width: 1560px">第1版の「狩人の仕掛け（獣道に罠を張る）」は<b>緑のこのはの職能に見える</b>ため却下。'
          '伏せは15人のリーダー全員・4色（緑青白黒／赤は設計上リアクションを持たない）に共通のシステム機構なので、'
          '語彙は<b>全員が共有する世界（永い夜・月・光の粒）か、行為そのものの抽象</b>からしか引かない。機構は不変で、変えるのは言葉と、舞台に置かれた一手が何として見えるか。'
          '合格条件は<b>緑の茨・青の対抗呪文・白の聖印・黒の呪詛が同じ動詞で置けること</b>（下段の4色テスト）。</div>')

    # 上段: 舞台での見え方
    s += '<div style="display: flex; gap: 20px; margin-top: 22px; align-items: flex-start">'
    s += ('<div style="width: 300px"><div class="note" style="font-size: 14px; margin-bottom: 6px">現在（実機）: カードの裏「?」</div>'
          '<img class="px" src="current_zone.jpg" width="300" height="143" style="display:block;border-radius:10px;object-fit:cover">'
          f'<div class="note" style="font-size: 12px; margin-top: 6px; opacity: 0.85">リーダーの右のポケットに札の裏を置く。世界の物ではなく「カード」がそのまま舞台に出ている。</div></div>')
    for p in pkgs:
        vb = HONEY if p.get('verdict') == '通る' else PAPER
        s += (f'<div style="width: 300px"><div class="note" style="font-size: 14px; margin-bottom: 6px">'
              f'<span class="deco" style="font-size: 17px">{esc(p["title"])}</span> <span class="tag" style="background: {vb}">{esc(p.get("verdict",""))}</span></div>'
              f'{glyph(p["key"])}'
              f'<div class="note" style="font-size: 12px; margin-top: 6px; opacity: 0.9; line-height: 17px">{esc(str(p["package"].get("stage_visual",""))[:230])}</div></div>')
    s += '</div>'

    # 中段: 語彙の対照表
    s += '<div class="paper" style="position: relative; margin-top: 28px; padding: 18px 22px">'
    s += '<table class="vocab"><tr><th style="width:118px">現在</th>'
    for p in pkgs:
        s += (f'<th>{esc(p["title"])}'
              f'<div style="font-weight: 400; font-size: 11.5px; color: {INK_MID}; margin-top: 3px; line-height: 16px">{esc(str(p["package"].get("concept",""))[:170])}</div></th>')
    s += '</tr>'
    for label, key in ROWS:
        s += f'<tr><td class="cur">{esc(label)}</td>' + ''.join(f'<td>{cell(p["package"].get(key,""))}</td>' for p in pkgs) + '</tr>'
    s += '</table></div>'

    # 下段: 4色テスト + 画面文 + 検査
    s += '<div style="display: flex; gap: 20px; margin-top: 24px; align-items: flex-start">'
    for p in pkgs:
        pk = p['package']
        rows = ''
        for t in pk.get('five_color_test', []):
            c = t.get('color', '')
            ink = COLOR_INK.get(c[0] if c else '', INK)
            bg = OK_BG.get(t.get('natural', ''), '#eee')
            rows += (f'<div style="display: flex; gap: 8px; align-items: flex-start; margin: 5px 0">'
                     f'<span style="flex: none; width: 22px; height: 22px; border-radius: 50%; background: {ink}; color: #f4ecd6; font-size: 12px; display: flex; align-items: center; justify-content: center">{esc(c[0] if c else "?")}</span>'
                     f'<span style="font-size: 12.5px; line-height: 18px; flex: 1">{esc(t.get("sentence",""))}</span>'
                     f'<span class="tag" style="flex: none; background: {bg}; font-size: 11px">{esc(t.get("natural",""))}</span></div>')
        lines = ''.join(
            f'<div style="padding: 6px 10px; margin: 5px 0; background: rgba(59,47,47,0.06); border-radius: 8px; font-size: 12.5px; line-height: 18px">{esc(l)}</div>'
            for l in pk.get('sample_lines', []))
        s += (f'<div class="paper" style="position: relative; width: {colw - 44}px; padding: 15px 18px">'
              f'<div class="deco" style="font-size: 17px">{esc(p["title"])}</div>'
              f'<div style="font-size: 12px; color: {INK_MID}; margin: 4px 0 8px">4色テスト（同じ動詞で置けるか）</div>{rows}'
              f'<div style="font-size: 12px; color: {INK_MID}; margin: 12px 0 4px">画面に出る文</div>{lines}'
              f'<div style="font-size: 12px; color: {INK_MID}; margin-top: 10px; line-height: 17px"><span class="kw">評:</span> {esc(p.get("one_line",""))}</div>'
              f'<div style="font-size: 12px; color: {INK_MID}; margin-top: 5px; line-height: 17px"><span class="kw">色中立:</span> {esc(p.get("color_neutral",""))}</div>'
              f'<div style="font-size: 12px; color: {INK_MID}; margin-top: 5px; line-height: 17px"><span class="kw">衝突:</span> {esc(p.get("conflicts",""))}</div>'
              f'<div style="font-size: 12px; color: {INK_MID}; margin-top: 5px; line-height: 17px"><span class="kw">レリック:</span> {esc(str(pk.get("relic_names",""))[:260])}</div>'
              f'<div style="font-size: 12px; color: {INK_MID}; margin-top: 5px; line-height: 17px"><span class="kw">懸念:</span> {esc(str(pk.get("concerns",""))[:300])}</div>'
              f'</div>')
    s += '</div></div></div>'
    return s


def write(name, body):
    with open(os.path.join(OUT, name), 'w', encoding='utf-8') as f:
        f.write(HEAD + body + TAIL)


pkgs = json.load(open(os.path.join(OUT, 'packages.json'), encoding='utf-8'))
write('Main.dc.html', board(pkgs))
json.dump({
    'artboards': [{'file': 'Main.dc.html', 'title': '「伏せ」の言い換え（色中立の4案）', 'x': 0, 'y': 0, 'w': 1760, 'h': 1980, 'expand': 'fill'}],
    'annotations': [{'id': 'brief', 'x': 0, 'y': -170, 'w': 720,
                     'text': '「伏せ」を戦闘世界観の言葉に（2026-09-10・第2版）。第1版の狩人案は「緑のこのはだけのものに見える」で却下。'
                             '伏せは15リーダー・4色に共通の機構なので、語彙は共有の世界（永い夜・月）か行為の抽象からのみ引く。'
                             '上段=舞台での見え方（左端は実機）／中段=語彙の対照／下段=4色テストと画面文。機構は不変。'}],
    'launch': {'view': 'canvas'},
}, open(os.path.join(OUT, 'canvas.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
print('written v2 sheet for', len(pkgs), 'packages')
