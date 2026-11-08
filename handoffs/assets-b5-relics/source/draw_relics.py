#!/usr/bin/env python3
"""Original native-grid B5 UI assets. No input artwork, image model or downsampling."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageColor
import json, math
R=Path(__file__).resolve().parents[1]
P={k:ImageColor.getrgb(v)+(255,) for k,v in json.loads((R/'palette.json').read_text()).items()}
TIMES=[60,55,65,80,80,75,70,65]
OFFSETS=[(0,0),(-1,0),(1,0),(-1,0),(1,0),(0,0),(0,0),(0,0)]
FLASH=[0,.12,.26,.16,.08,0,0,0]
BG='#0c1525'; BG2='#17283f'
def line(im,points,c): ImageDraw.Draw(im).line(points,fill=P[c],width=1)
def pt(r,a): return (round(31.5+r*math.cos(a)),round(31.5+r*math.sin(a)))
def arc(im,r,a,b,c):
    n=max(2,round((b-a)*r*3))
    line(im,[pt(r,a+(b-a)*j/n) for j in range(n+1)],c)
def spark(im,x,y,c):
    line(im,[(x-1,y),(x+1,y)],c); line(im,[(x,y-1),(x,y+1)],c)
def ring(f):
    im=Image.new('RGBA',(64,64))
    if f==7:return im
    radius=[21,22,23,24,24,24,24][f]
    spans=[.28,.54,.76,.92,.74,.46,.18]
    colors=['brass0','brass1','brass2','brass3','brass2','brass1','brass0']
    # Four open quarters: circular rim around the icon, never a target reticle.
    for j in range(4):
        a=-math.pi/2+j*math.pi/2
        arc(im,radius,a-spans[f]/2,a+spans[f]/2,colors[f])
        if 1<=f<=4:
            arc(im,radius-2,a-spans[f]/3,a+spans[f]/3,'teal1' if f in (2,3) else 'teal0')
        # A tiny brass clasp holds each arc; no arrows or directional damage marks.
        if f in (1,2,3,4):
            x,y=pt(radius,a); x2,y2=pt(radius+1,a)
            line(im,[(x,y),(x2,y2)],colors[f])
    if f in (2,3,4,5):
        for j in range(4):
            a=math.pi/4+j*math.pi/2
            x,y=pt(26,a)
            if f in (2,3):spark(im,x,y,'brass2' if f==2 else 'brass3')
            else:im.putpixel((x,y),P['brass1' if f==4 else 'brass0'])
    # Protect a 34x34 aperture: a 32x32 relic can shake one pixel either way.
    ImageDraw.Draw(im).rectangle((15,15,48,48),fill=(0,0,0,0))
    return im
def plate(active=False):
    im=Image.new('RGBA',(24,14));d=ImageDraw.Draw(im)
    pts=[(3,1),(20,1),(22,3),(22,10),(20,12),(3,12),(1,10),(1,3)]
    d.polygon(pts,fill=P['deep'])
    d.line(pts+[pts[0]],fill=P['brass2' if active else 'brass0'],width=1)
    d.line([(3,1),(20,1),(21,2)],fill=P['brass3' if active else 'brass1'],width=1)
    d.line([(2,3),(2,9)],fill=P['brass1' if active else 'edge'],width=1)
    d.line([(4,11),(19,11)],fill=P['edge'],width=1)
    return im
DIGITS={'0':['111','101','101','101','111'],'1':['010','110','010','010','111'],'2':['111','001','111','100','111'],'3':['111','001','111','001','111'],'7':['111','001','010','010','010'],'9':['111','101','111','001','111'],'+':['000','010','111','010','000']}
def number(im,txt,xy,color='steel3'):
    x,y=xy
    for ch in txt:
        for yy,row in enumerate(DIGITS[ch]):
            for xx,v in enumerate(row):
                if v=='1':im.putpixel((x+xx,y+yy),P[color])
        x+=4
def dummy():
    """Original preview-only relic, not a replacement for any game icon."""
    im=Image.new('RGBA',(32,32));d=ImageDraw.Draw(im)
    d.polygon([(16,3),(23,9),(25,21),(16,28),(7,21),(9,9)],fill=P['steel0'])
    d.line([(16,3),(23,9),(25,21),(16,28),(7,21),(9,9),(16,3)],fill=P['brass1'])
    d.line([(16,6),(16,24)],fill=P['brass2'])
    d.polygon([(16,8),(21,15),(16,22),(11,15)],fill=P['teal1'])
    d.line([(16,8),(11,15),(16,22)],fill=P['teal2'])
    return im
def tint(im,weight):
    im=im.copy()
    if weight:
        for y in range(im.height):
            for x in range(im.width):
                c=im.getpixel((x,y))
                if c[3]:im.putpixel((x,y),tuple(round(c[k]*(1-weight)+P['brass3'][k]*weight) for k in range(3))+(c[3],))
    return im
def composed(f,bg=BG,show_icon=True,with_number=True):
    im=Image.new('RGBA',(64,64),bg)
    im.alpha_composite(ring(f))
    if show_icon:
        dx,dy=OFFSETS[f];im.alpha_composite(tint(dummy(),FLASH[f]),(16+dx,16+dy))
        if with_number:
            b=plate(f in (2,3));number(b,'3',(10,4));im.alpha_composite(b,(32,44))
    return im
def make():
    for p in ['Art/fx','Art/ui','previews']:(R/p).mkdir(parents=True,exist_ok=True)
    sheet=Image.new('RGBA',(512,64))
    for f in range(8):sheet.alpha_composite(ring(f),(f*64,0))
    sheet.save(R/'Art/fx/relic_ring_8f.png')
    plate().save(R/'Art/ui/relic_counter_plate.png')
    plate(True).save(R/'Art/ui/relic_counter_plate_active.png')
    font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',12)
    small=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',10)
    contact=Image.new('RGBA',(1088,404),BG);d=ImageDraw.Draw(contact)
    d.text((16,12),'B5 / RELIC ACTIVATION  |  8 frames / 550 ms  |  Native pixel artwork / preview 2x',font=font,fill=P['steel3'])
    d.text((16,35),'Top: transparent overlay on night board. Bottom: new dummy relic + stationary counter. No real UI integration.',font=small,fill=P['steel2'])
    for f in range(8):
        x=12+f*134
        d.text((x,58),f'{f}  /  {TIMES[f]} ms',font=font,fill=P['steel3'])
        contact.alpha_composite(composed(f,show_icon=False).resize((128,128),Image.Resampling.NEAREST),(x,78))
        contact.alpha_composite(composed(f).resize((128,128),Image.Resampling.NEAREST),(x,214))
    d.text((16,354),'Counter plates (24x14): blank production plates, runtime digits shown separately below.',font=font,fill=P['steel3'])
    for j,(label,active,txt) in enumerate([('base',False,''),('active',True,''),('3',False,'3'),('99',False,'99'),('99+',False,'99+')]):
        x=16+j*150;b=plate(active)
        if txt:number(b,txt,(12-(len(txt)*4-1)//2,4))
        contact.alpha_composite(b.resize((48,28),Image.Resampling.NEAREST),(x,375));d.text((x+55,381),label,font=small,fill=P['steel2'])
    contact.save(R/'previews/relics_contactsheet.png')
    native=Image.new('RGBA',(576,270),BG);d=ImageDraw.Draw(native)
    d.text((10,9),'Native 1x / 32px dummy relic / no scaling',font=font,fill=P['steel3'])
    for row,bg in enumerate([BG,BG2]):
        for f in range(8):native.alpha_composite(composed(f,bg),(20+f*68,32+row*70))
    d.text((10,177),'Dense 42px pitch: middle icon activates; numbers stay fixed.',font=small,fill=P['steel2'])
    # Neighbour icons at existing desktop row pitch. Rings sit behind all icons.
    strip=Image.new('RGBA',(250,64),BG);strip.alpha_composite(ring(3),(93,0))
    for x in [25,67,109,151,193]:
        strip.alpha_composite(dummy(),(x,16))
        b=plate(x==109);number(b,'3',(10,4));strip.alpha_composite(b,(x+16,44))
    native.alpha_composite(strip,(10,195));native.save(R/'previews/relics_native.png')
    frames=[];dur=[];timeline=[]
    order=[(7,500)]+list(enumerate(TIMES))+[(7,900)]
    for f,t in order:
        panel=Image.new('RGBA',(320,180),BG);d=ImageDraw.Draw(panel)
        d.text((12,10),f'B5 / index {f} / {t} ms',font=font,fill=P['steel3'])
        panel.alpha_composite(composed(f).resize((128,128),Image.Resampling.NEAREST),(18,36))
        panel.alpha_composite(composed(f),(195,65))
        d.text((182,136),'native 1x',font=small,fill=P['steel2'])
        frames.append(panel);dur.append(t);timeline.append({'production_frame_index':f,'duration_ms':t,'preview_hold':len(timeline) in (0,9)})
    frames[0].save(R/'previews/relics_playback.png',save_all=True,append_images=frames[1:],duration=dur,loop=0,disposal=0,blend=0)
    (R/'previews/playback_timing.json').write_text(json.dumps({'frames':timeline,'total_ms':sum(dur),'production_ms':sum(TIMES)},indent=2)+'\n')
if __name__=='__main__':make()
