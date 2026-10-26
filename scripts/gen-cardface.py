#!/usr/bin/env python3
# scripts/gen-cardface.py — 札の面「W09」(2026-10-26 ユーザー裁定: Godot の面に合わせる) の層を PNG に描く。
# 形は dot の Godot 版 SourceCardFace.SelectedFrame の SVG（630×880 の設計座標）と同じ。
#   cardface_frame_<c|u|r>.png … 夜色の板・レア度の線と角飾り・コスト枠・本文の板・宝石の窪みと粒。色の帯の場所は穴 (透明)
#   cardface_type_<physical|spell|reaction|permanent|other>.png … タイプの形 (y 488〜816 を切り出し)
#   cardface_band.png … 色の帯の上に重ねる光沢と内側の線 (9スライス・border 16・設計の画素)
# 出力先 unity/Assets/Resources/Art/ui/。ArtImporter が cardface_ を Bilinear＋ミップで取り込む。
# 使い方: python3 scripts/gen-cardface.py  (要 cairosvg・Pillow)
import io, os, sys
import cairosvg
from PIL import Image, ImageDraw
OUT = os.path.join(os.path.dirname(__file__), "..", "unity", "Assets", "Resources", "Art", "ui")
P = dict(shell="#223442", body="#2c3d4b", edge="#b9c8c6", line="#788c97", well="#20313d", shadow="#172733")
ACC = ["#a8b8b9", "#56b69a", "#ddbd70"]; SHADE = ["#576e70", "#28775e", "#a0782a"]; DOT = ["#bcc8c9", "#77c6bd", "#d9b363"]
BANDS = [(169, 29, 438, 118), (24, 150, 582, 349), (25, 806, 581, 34)]
TYPE_Y0, TYPE_H = 488, 328

def svg_doc(body):
    return ("<svg xmlns='http://www.w3.org/2000/svg' width='630' height='880' viewBox='0 0 630 880'>" + body + "</svg>").encode()

def frame_svg(rank):
    accent, shade, dot = ACC[rank], SHADE[rank], DOT[rank]
    s = [f"<rect width='630' height='880' rx='3' fill='{P['shell']}'/>"]
    s.append(f"<g fill='none' stroke='{accent}' stroke-width='4'><rect x='4' y='12' width='622' height='856'/><rect x='12' y='4' width='606' height='872'/></g><rect x='17' y='17' width='596' height='846' fill='none' stroke='{shade}'/>")
    for side in (0, 1):
        for top in (0, 1):
            x = 29 if side == 0 else 601; sx = 1 if side == 0 else -1; y = 25 if top == 0 else 855; sy = 1 if top == 0 else -1
            if rank == 0: s.append(f"<path d='M{x} {y+sy*17}V{y}H{x+sx*17}' fill='none' stroke='{accent}' stroke-width='3'/>")
            elif rank == 1: s.append(f"<path d='M{x} {y+sy*23}V{y}H{x+sx*23} M{x+sx*7} {y+sy*23}V{y+sy*7}H{x+sx*23}' fill='none' stroke='{accent}' stroke-width='3'/>")
            else: s.append(f"<path d='M{x} {y+sy*26}V{y+sy*8}L{x+sx*8} {y}H{x+sx*26}' fill='none' stroke='{accent}' stroke-width='5'/><path d='M{x+sx*9} {y+sy*9-6}l6 6-6 6-6-6z' fill='{accent}'/>")
    # コスト枠 (レア度の色)・本文の板・宝石の窪み・粒
    s.append(f"<path d='M32 22H154V27H159V141H154V146H32V141H27V27H32Z' fill='{accent}'/><path d='M35 26H151V31H155V137H151V142H35V137H31V31H35Z' fill='{P['shell']}'/>")
    s.append(f"<rect x='25' y='500' width='580' height='305' fill='{P['shell']}'/><rect x='25' y='574' width='580' height='231' fill='{P['body']}'/>")
    return "".join(s)

def wells_svg(rank):
    # 宝石の窪みと粒 (帯の上に乗る＝穴を開けた後に重ねる)
    dot = DOT[rank]
    s = [f"<rect x='263' y='808' width='104' height='30' rx='7' fill='{P['well']}'/><rect x='526' y='119' width='75' height='24' rx='5' fill='{P['well']}'/>"]
    for i in range(rank + 1):
        off = i - rank / 2
        s.append(f"<circle cx='{315+off*28}' cy='823' r='7' fill='{dot}'/><circle cx='{564+off*22}' cy='131' r='6' fill='{dot}'/>")
    return "".join(s)

def type_svg(t):
    if t == "physical": return f"<path d='M55 504H575L599 528V548L575 572H55L31 548V528Z' fill='{P['shell']}' stroke='{P['edge']}' stroke-width='13'/><path d='M34 573V765L68 801H562L596 765V573' fill='none' stroke='{P['line']}' stroke-width='7'/>"
    if t == "spell": return f"<path d='M89 573V548L150 526 238 509H392L480 526 541 548V573' fill='none' stroke='{P['edge']}' stroke-width='27' stroke-linejoin='round'/><path d='M34 575V773L46 793 66 801H564L584 793 596 773V575' fill='none' stroke='{P['line']}' stroke-width='7' stroke-linejoin='round'/>"
    if t == "reaction": return f"<path d='M31 506H567L599 536H66Z' fill='{P['shell']}' stroke='{P['edge']}' stroke-width='15'/><path d='M66 536H599L564 571H31Z' fill='{P['shell']}' stroke='{P['line']}' stroke-width='15'/><path d='M120 518H542L515 573H95Z' fill='{P['shell']}'/><path d='M34 576V801H596V576' fill='none' stroke='{P['line']}' stroke-width='7'/>"
    if t == "permanent": return f"<path d='M38 522V799H592V522' fill='none' stroke='{P['edge']}' stroke-width='14'/><path d='M31 514H599' stroke='{P['line']}' stroke-width='10'/><rect x='165' y='508' width='300' height='64' fill='{P['shell']}' stroke='{P['edge']}' stroke-width='7'/>"
    return f"<rect x='31' y='508' width='568' height='64' fill='{P['shell']}' stroke='{P['line']}' stroke-width='7'/><path d='M34 576V801H596V576' fill='none' stroke='{P['line']}' stroke-width='7'/>"

def render(body):
    png = cairosvg.svg2png(bytestring=svg_doc(body), output_width=630, output_height=880)
    return Image.open(io.BytesIO(png)).convert("RGBA")

def main():
    os.makedirs(OUT, exist_ok=True)
    for rank, key in enumerate("cur"):
        im = render(frame_svg(rank))
        # 色の帯の場所を穴にする (Unity 側で色つきの Image を下に敷く)
        for (x, y, w, h) in BANDS:
            im.paste((0, 0, 0, 0), (x, y, x + w, y + h))
        over = render(wells_svg(rank)); im.alpha_composite(over)
        im.save(os.path.join(OUT, f"cardface_frame_{key}.png"))
    for t in ("physical", "spell", "reaction", "permanent", "other"):
        im = render(type_svg(t)).crop((0, TYPE_Y0, 630, TYPE_Y0 + TYPE_H))
        im.save(os.path.join(OUT, f"cardface_type_{t}.png"))
    # 帯の光沢: 縦の明るみ (上 30% が最も明るい .075・65% で 0・下端に影 .10) と内側の線 (3.5 内・2px・.25)・外周の影 (1px・.22)
    n = 120; band = Image.new("RGBA", (n, n), (0, 0, 0, 0)); px = band.load()
    def sheen(t):   # t: 0..1 (上→下)
        if t < 0.3: a = 0.006 + (0.075 - 0.006) * (t / 0.3); return (255, 247, 224, a)
        if t < 0.65: a = 0.075 * (1 - (t - 0.3) / 0.35); return (255, 247, 224, a)
        a = 0.10 * ((t - 0.65) / 0.35); return (0x17, 0x27, 0x33, a)
    for y in range(n):
        for x in range(n):
            r, g, b, a = sheen((y + 0.5) / n)
            d = min(x, y, n - 1 - x, n - 1 - y)   # 外周からの距離 (画素)
            if d < 1: r, g, b, a = 0x17, 0x27, 0x33, 0.22 + a * 0.5
            elif 2.5 <= d < 4.5: r, g, b, a = 255, 247, 224, 0.25 + a * 0.5
            px[x, y] = (r, g, b, int(round(min(1.0, a) * 255)))
    band.save(os.path.join(OUT, "cardface_band.png"))
    for f in sorted(os.listdir(OUT)):
        if f.startswith("cardface_"): print(f, Image.open(os.path.join(OUT, f)).size)

if __name__ == "__main__":
    main()
