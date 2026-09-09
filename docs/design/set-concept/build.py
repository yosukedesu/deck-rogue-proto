# build.py — 「伏せ」の言い換え比較シート (2026-09-10 ユーザー「伏せって概念を戦闘世界観に合わせた形にしたいな」)。
# packages.json (4案: 切り口ごとの語彙パッケージ+検査結果) を読み、1枚のボードに「現在の語彙 → 各案」の対照表と、
# 舞台での見え方 (簡易の絵) と実際の画面文を並べる。python3 build.py → Main.dc.html + canvas.json → seed-canvas.mjs で公開。
import json, os, html
OUT = os.path.dirname(os.path.abspath(__file__))
INK = '#3b2f2f'; INK_MID = '#574b48'; PAPER = '#f4ecd6'; PAPER2 = '#eadfc4'; NIGHT = '#1a1c33'
HONEY = '#e0b25a'; MOSS = '#8fae7b'; SKY = '#7fa7c9'; PLUM = '#a98cc4'; TEAL = '#7ab8b0'; SAND = '#c9a982'

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
    .hand { font-family: "Klee One", sans-serif; font-weight: 600; }
    .light { color: #f4ecd6; }
    .paper { background: #f4ecd6; color: #3b2f2f; border-radius: 16px 20px 14px 18px / 18px 14px 20px 16px; box-shadow: 0 0 0 1.5px #3b2f2f, 0 0 0 4px #f4ecd6, 0 0 0 5.5px rgba(59,47,47,0.5), 0 10px 22px rgba(0,0,0,0.35); }
    .note { font-size: 13px; line-height: 20px; color: #f4ecd6; text-shadow: 0 1px 0 rgba(0,0,0,0.6); }
    table.vocab { border-collapse: collapse; width: 100%; font-size: 13px; }
    table.vocab th, table.vocab td { border-bottom: 1px solid rgba(59,47,47,0.25); padding: 6px 8px; vertical-align: top; text-align: left; }
    table.vocab th { font-family: "Kaisei Decol", serif; font-size: 14px; background: rgba(59,47,47,0.06); }
    table.vocab td.cur { color: #574b48; background: rgba(59,47,47,0.04); width: 120px; }
    .tag { display: inline-block; padding: 1px 8px; border-radius: 8px; font-size: 12px; margin-left: 6px; box-shadow: 0 0 0 1.5px #3b2f2f; background: #f4ecd6; }
    .kw { font-family: "Kaisei Decol", serif; font-weight: 700; }
    .px { image-rendering: pixelated; }
  </style>
</helmet>
'''
TAIL = '''</x-dc>
</body>
</html>
'''

def esc(s): return html.escape(str(s or ''))

# 舞台の簡易の絵 (SVG)。切り口ごとに「置かれた札」を別の物として描く
def glyph(key, w=300, h=150):
    ground = f'<rect x="0" y="0" width="{w}" height="{h}" fill="#20233a"/><ellipse cx="{w*0.5}" cy="{h*0.78}" rx="{w*0.55}" ry="{h*0.22}" fill="#5a4636" opacity="0.85"/><ellipse cx="{w*0.5}" cy="{h*0.78}" rx="{w*0.35}" ry="{h*0.14}" fill="#6b5340" opacity="0.9"/>'
    hunter = f'<g transform="translate({w*0.14},{h*0.36})"><circle cx="18" cy="14" r="12" fill="#e9c9a8"/><rect x="6" y="26" width="24" height="30" rx="6" fill="#2f5a3a"/><rect x="0" y="27" width="6" height="26" fill="#3b2f2f"/><rect x="30" y="27" width="6" height="26" fill="#3b2f2f"/><circle cx="18" cy="12" r="12" fill="#5a3a22" opacity="0.85"/><rect x="9" y="8" width="18" height="9" rx="3" fill="#e9c9a8"/></g>'
    beast = f'<g transform="translate({w*0.72},{h*0.34})"><ellipse cx="26" cy="30" rx="26" ry="22" fill="#b9c3c9" opacity="0.9"/><circle cx="26" cy="12" r="14" fill="#c7cfd4"/><circle cx="21" cy="11" r="2" fill="#2a2a33"/><circle cx="31" cy="11" r="2" fill="#2a2a33"/></g>'
    moon = f'<circle cx="{w*0.88}" cy="{h*0.16}" r="12" fill="#f4ecd6" opacity="0.9"/><circle cx="{w*0.88}" cy="{h*0.16}" r="22" fill="#f4ecd6" opacity="0.12"/>'
    cx, cy = w*0.46, h*0.74
    if key == 'current':
        obj = f'<rect x="{cx-16}" y="{cy-40}" width="32" height="46" rx="4" fill="#2b2d4d" stroke="{PAPER}" stroke-width="1.5"/><text x="{cx}" y="{cy-12}" text-anchor="middle" font-size="16" fill="{PAPER}" font-family="serif">?</text>'
    elif key == 'trap':
        obj = f'<ellipse cx="{cx}" cy="{cy}" rx="26" ry="9" fill="none" stroke="#c9a982" stroke-width="3" stroke-dasharray="6 4"/><path d="M{cx-26} {cy} q -8 -22 4 -34" fill="none" stroke="#8a6a3c" stroke-width="3"/><circle cx="{cx-22}" cy="{cy-34}" r="3" fill="#e0b25a"/><path d="M{cx-14} {cy-4} l 6 -8 l 6 8 l 6 -8 l 6 8" fill="none" stroke="#c9a982" stroke-width="2"/>'
    elif key == 'charm':
        obj = f'<g transform="translate({cx-12},{cy-52})"><rect x="0" y="0" width="24" height="44" fill="{PAPER}" stroke="{INK}" stroke-width="1.5"/><line x1="12" y1="6" x2="12" y2="36" stroke="#a33a30" stroke-width="2"/><circle cx="12" cy="20" r="5" fill="none" stroke="#a33a30" stroke-width="1.5"/></g><line x1="{cx}" y1="{cy-52}" x2="{cx}" y2="{cy-70}" stroke="#c9a982" stroke-width="1.5"/><circle cx="{cx}" cy="{cy-52}" r="14" fill="#f4ecd6" opacity="0.12"/>'
    elif key == 'ambush':
        obj = f'<ellipse cx="{cx}" cy="{cy-6}" rx="30" ry="14" fill="#0e1020" opacity="0.9"/><path d="M{cx-30} {cy-6} q 6 -18 12 -4 q 6 -22 12 -6 q 6 -18 12 -2 q 4 -12 8 -2" fill="#14331f" stroke="#1f4a2b" stroke-width="1"/><circle cx="{cx-6}" cy="{cy-10}" r="2" fill="#e0b25a"/><circle cx="{cx+6}" cy="{cy-10}" r="2" fill="#e0b25a"/>'
    else:  # stance
        obj = f'<g transform="translate({w*0.14},{h*0.36})"><rect x="26" y="20" width="34" height="5" rx="2" fill="#8a6a3c" transform="rotate(-35 26 20)"/><rect x="52" y="-2" width="14" height="12" rx="2" fill="#9aa8b5" transform="rotate(-35 26 20)"/></g><path d="M{cx-34} {cy+2} q 34 -14 68 0" fill="none" stroke="#e0b25a" stroke-width="2" stroke-dasharray="4 4" opacity="0.9"/><circle cx="{cx}" cy="{cy-4}" r="3" fill="#e0b25a"/>'
    return f'<svg width="{w}" height="{h}" viewBox="0 0 {w} {h}" style="display:block;border-radius:10px">{ground}{moon}{hunter}{beast}{obj}</svg>'

ROWS = [
  ('伏せ札', 'noun_card'), ('伏せ場', 'noun_zone'), ('伏せ枠', 'noun_slot'), ('伏せる', 'verb_set'),
  ('発動', 'verb_fire'), ('温存', 'verb_hold'), ('回収（1E）', 'verb_retrieve'), ('伏せ破壊', 'enemy_destroy'),
  ('伏せ警戒・分岐', 'enemy_react'), ('鮮度（気配）', 'freshness'), ('ブラフ', 'bluff'),
  ('タイプ名「リアクション」', 'card_type_name'), ('窓の名', 'window_names'), ('がらくた', 'junk'),
]

def main_board(pkgs):
    W = 1760
    n = len(pkgs)
    colw = int((W - 80 - 160) / max(1, n))
    s = f'<div style="position: relative; width: {W}px; background: #1a1c33; padding-bottom: 40px">'
    s += '<div class="abs" style="inset: 0; background: linear-gradient(180deg, #26294a 0%, #12142a 100%)"></div>'
    s += '<div style="position: relative; padding: 26px 40px 0 40px">'
    s += '<div class="deco light" style="font-size: 30px; letter-spacing: 0.06em; text-shadow: 0 2px 0 rgba(0,0,0,0.5)">「伏せ」を世界の言葉に — 4つの切り口</div>'
    s += '<div class="note" style="margin-top: 8px; width: 1500px">機構は変えない（事前払い・実値を見てから発動/温存・回収1E・敵は新しく置いた札だけに反応・壊す敵がいる）。変えるのは言葉と、舞台に置かれた札が何として見えるか。緑の茨も青の対抗呪文も白の聖印も黒の呪詛も、同じ動詞で「置ける」ことが条件。各案は別々の担当が作り、検査官が既存カード名との衝突・色の整合・機構の説明可能性・トーンを検査した後の姿。</div>'
    # 舞台の見え方 (現在 + 各案)
    s += '<div style="display: flex; gap: 20px; margin-top: 22px; align-items: flex-start">'
    s += '<div style="width: 300px"><div class="note" style="font-size: 14px; margin-bottom: 6px">現在: カードの裏「?」（実機）</div><img class="px" src="current_zone.jpg" width="300" height="143" style="display:block;border-radius:10px;object-fit:cover"><div class="note" style="font-size: 12px; margin-top: 6px; opacity: 0.85">リーダーの右のポケットに札の裏を置く。世界の物ではなく「カード」がそのまま舞台に出ている。</div></div>'
    for p in pkgs:
        pk = p['package']
        s += f'<div style="width: 300px"><div class="note" style="font-size: 14px; margin-bottom: 6px"><span class="deco" style="font-size: 16px">{esc(p["title"])}</span> <span class="tag" style="background: {HONEY if p["verdict"]=="通る" else PAPER}">{esc(p["verdict"])}</span></div>{glyph(p["key"])}<div class="note" style="font-size: 12px; margin-top: 6px; opacity: 0.9; line-height: 17px">{esc(pk.get("stage_visual",""))}</div></div>'
    s += '</div>'
    # 対照表 (紙)
    s += '<div class="paper" style="position: relative; margin-top: 30px; padding: 18px 22px">'
    s += '<table class="vocab"><tr><th style="width:120px">現在</th>' + ''.join(f'<th>{esc(p["title"])}<div style="font-weight: 400; font-size: 12px; color: {INK_MID}; margin-top: 2px">{esc(p["package"].get("concept",""))}</div></th>' for p in pkgs) + '</tr>'
    for label, key in ROWS:
        s += f'<tr><td class="cur">{esc(label)}</td>' + ''.join(f'<td>{esc(p["package"].get(key,""))}</td>' for p in pkgs) + '</tr>'
    s += '</table></div>'
    # 実際の画面文 + 検査官の1行 + 懸念
    s += '<div style="display: flex; gap: 20px; margin-top: 24px; align-items: flex-start">'
    for p in pkgs:
        pk = p['package']
        lines = ''.join(f'<div style="padding: 6px 10px; margin: 6px 0; background: rgba(59,47,47,0.06); border-radius: 8px; font-size: 13px; line-height: 18px">{esc(l)}</div>' for l in pk.get('sample_lines', []))
        s += f'<div class="paper" style="position: relative; width: {colw - 44}px; padding: 14px 18px"><div class="deco" style="font-size: 16px">{esc(p["title"])} — 画面に出る文</div>{lines}<div style="font-size: 12px; color: {INK_MID}; margin-top: 10px; line-height: 17px"><span class="kw">検査官:</span> {esc(p.get("one_line",""))}</div><div style="font-size: 12px; color: {INK_MID}; margin-top: 6px; line-height: 17px"><span class="kw">懸念:</span> {esc(pk.get("concerns",""))}</div><div style="font-size: 12px; color: {INK_MID}; margin-top: 6px; line-height: 17px"><span class="kw">レリック・札の名:</span> {esc(pk.get("relic_names",""))} ／ {esc(pk.get("card_names",""))}</div></div>'
    s += '</div></div></div>'
    return s

def write(name, body):
    with open(os.path.join(OUT, name), 'w', encoding='utf-8') as f:
        f.write(HEAD + body + TAIL)

pkgs = json.load(open(os.path.join(OUT, 'packages.json'), encoding='utf-8'))
write('Main.dc.html', main_board(pkgs))
json.dump({
    'artboards': [{'file': 'Main.dc.html', 'title': '「伏せ」の言い換え 4案', 'x': 0, 'y': 0, 'w': 1760, 'h': 1900, 'expand': 'fill'}],
    'annotations': [{'id': 'brief', 'x': 0, 'y': -160, 'w': 700, 'text': '「伏せ」の概念を戦闘世界観に合わせる（2026-09-10）。上段＝舞台で札が何として見えるか。中段＝現在の語彙→各案の対照表。下段＝実際の画面文と検査官の評。機構は不変。'}],
    'launch': {'view': 'canvas'},
}, open(os.path.join(OUT, 'canvas.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
print('written set-concept sheet for', len(pkgs), 'packages')
