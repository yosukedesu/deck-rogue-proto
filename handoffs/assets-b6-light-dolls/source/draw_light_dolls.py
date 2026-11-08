#!/usr/bin/env python3
"""Original B6 integer-grid effect art. No source art, image model, or downsampling."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageColor
import json, math
R=Path(__file__).resolve().parents[1]
P={k:ImageColor.getrgb(v)+(255,) for k,v in json.loads((R/'palette.json').read_text()).items()}
NAMES=['light_gain','light_command','light_release','doll_ignite','doll_step','doll_dissolve']
TIMES=[[40,45,45,45,45,45,55,40],[45,50,55,65,65,60,55,45],[35,35,40,45,45,45,45,40],[45,50,55,65,65,60,60,50],[35,30,30,40,40,35,35,35],[55,60,70,80,80,80,70,65]]
OFFSETS=[(0,0),(-1,0),(4,-1),(8,-2),(6,-1),(3,0),(1,0),(0,0)]
LIGHT=[0,.15,.35,.50,.35,.15,.05,0]
BG='#0c1525'; BG2='#17283f'
def line(im,pts,c):ImageDraw.Draw(im).line(pts,fill=P[c],width=1)
def dot(im,x,y,c):
    if 0<=x<64 and 0<=y<64:im.putpixel((x,y),P[c])
def spark(im,x,y,c,r=1):
    line(im,[(x-r,y),(x+r,y)],c);line(im,[(x,y-r),(x,y+r)],c)
def ellipse(im,cx,cy,rx,ry,c,start=0,end=2*math.pi):
    n=max(12,round((end-start)*max(rx,ry)*2))
    line(im,[(round(cx+rx*math.cos(start+(end-start)*j/n)),round(cy+ry*math.sin(start+(end-start)*j/n))) for j in range(n+1)],c)
def flame(im,x,y,h,c):
    # Sparse stepped diamond lick, not a smooth anti-aliased glow.
    ImageDraw.Draw(im).polygon([(x,y-h),(x+2,y-2),(x+1,y+1),(x-1,y+2),(x-2,y-1)],fill=P[c])
    line(im,[(x,y-h+3),(x,y)],'brass3')
def effect(name,f):
    im=Image.new('RGBA',(64,64));d=ImageDraw.Draw(im)
    if f==7:return im
    c=['brass0','brass1','brass2','brass3','brass2','brass1','brass0'][f]
    if name=='light_gain':
        # Compact travelling cluster. World-space route is not baked into texture.
        x=[27,28,29,30,31,32,32][f]
        spark(im,x,32,c,1 if f!=3 else 2)
        if f<6:
            dot(im,x-3,33,'brass1');dot(im,x-5,34,'brass0')
            for j,(sx,sy) in enumerate([(-9,-6),(-6,8),(4,-9)]):
                k=1-f/8;xx=round(x+sx*k);yy=round(32+sy*k)
                if f in (2,3,4):spark(im,xx,yy,'brass2',1)
                else:dot(im,xx,yy,'brass1')
            if f in (2,3,4):dot(im,x,32,'steel3')
    elif name=='light_command':
        # Open crown around the upper body: central face/body remains clear.
        rx=[15,17,19,21,23,25,26][f];ry=[20,21,22,23,24,24,24][f]
        for a,b in [(3.45,4.30),(5.12,5.98),(.35,1.10),(2.04,2.80)]:ellipse(im,32,28,rx,ry,c,a,b)
        if f in (1,2,3,4,5):
            for x,y in [(32,5),(9,26),(55,26)]:
                spark(im,x,y,'brass3' if f==3 else 'brass2',2 if f in (2,3) else 1)
        if 2<=f<=4:
            line(im,[(25,8),(28,6)],'teal1');line(im,[(36,6),(39,8)],'teal1')
    elif name=='light_release':
        # Right-oriented light lance, travelling head fixed near the anchor.
        tail=[8,14,20,24,20,13,6][f];head=39
        d.polygon([(head,32),(31,29),(head-tail,31),(head-tail-3,32),(head-tail,33),(31,35)],fill=P[c])
        line(im,[(head-tail+2,32),(head-2,32)],'steel3' if f in (2,3,4) else 'brass2')
        if f<6:
            line(im,[(head-tail-6,29),(head-tail-2,29)],'brass1')
            line(im,[(head-tail-3,36),(head-tail+1,35)],'brass0')
            dot(im,head-tail-8,33,'brass1')
        if f in (2,3):spark(im,37,32,'steel3',2)
    elif name=='doll_ignite':
        # Feet ring and twin wick-like side flames, no doll body in production.
        rx=[5,9,14,19,22,24,24][f]
        ellipse(im,32,52,rx,max(2,rx//5),c,0,math.pi*.87)
        ellipse(im,32,52,rx,max(2,rx//5),c,math.pi*1.12,math.pi*1.92)
        if f<6:
            h=[3,6,10,13,9,5][f]
            flame(im,21,47,h,c);flame(im,43,47,h,c)
        if 2<=f<=5:
            for x,y in [(16,31-f),(48,35-f),(23,21-f)]:dot(im,x,y,'brass2' if f<4 else 'brass1')
    elif name=='doll_step':
        # Horizontal kick-up is tied to the launch feet, not the sprite's centre.
        if f<6:
            for j in range(3):
                x=17-j*4+f;y=51+j*2
                n=[2,5,10,11,7,3][f]-j*2
                if n>0:line(im,[(x,y),(x+n,y)],c)
            if f>=1:dot(im,13+f,49,'brass1')
        if 2<=f<=5:
            line(im,[(40,48),(44,50),(48,50)],'brass2' if f==3 else 'brass1')
            dot(im,46,47,'steel2')
        if f==6:dot(im,18,54,'brass0');dot(im,43,51,'brass0')
    elif name=='doll_dissolve':
        # Drifting ash crosses top to bottom. Body disappearance is a separate mask.
        pts=[(20,14),(29,10),(39,18),(45,23),(18,27),(33,25),(25,34),(41,35),(29,42),(45,44),(20,44),(37,48)]
        for j,(x,y) in enumerate(pts):
            born=j//3
            age=f-born
            if 0<=age<=3:
                xx=x+([-1,1,0][j%3])*age;yy=y+age*3
                if age==0 and f<=3:spark(im,xx,yy,'brass2',1)
                elif age<3:line(im,[(xx,yy),(xx+1,yy)],'steel1' if j%3 else 'brass1')
                else:dot(im,xx,yy,'steel0')
        if f==6:
            for x in [23,31,40]:dot(im,x,55,'steel0')
    return im

def dummy(size=32):
    # Preview-only plinth-shaped generic token. Not any in-game doll.
    im=Image.new('RGBA',(size,size));d=ImageDraw.Draw(im);m=size//2
    d.polygon([(m,2),(m+5,7),(m+4,12),(m-4,12),(m-5,7)],fill=P['steel2'])
    d.polygon([(m-4,13),(m+4,13),(m+7,size-8),(m+5,size-3),(m-5,size-3),(m-7,size-8)],fill=P['teal0'])
    d.line([(m-4,14),(m-3,size-8),(m+4,size-8)],fill=P['brass1'])
    d.rectangle((m-5,size-3,m-2,size-1),fill=P['steel1']);d.rectangle((m+2,size-3,m+5,size-1),fill=P['steel1'])
    return im

def body(name,f,size=32):
    im=dummy(size);w=LIGHT[f] if name in ('light_command','doll_ignite') else 0
    for y in range(size):
        for x in range(size):
            px=im.getpixel((x,y))
            if name=='doll_dissolve' and y < round(size*[0,.08,.23,.41,.61,.79,.94,1][f]):im.putpixel((x,y),(0,0,0,0));continue
            if px[3] and w:im.putpixel((x,y),tuple(round(px[k]*(1-w)+P['brass3'][k]*w) for k in range(3))+(255,))
    return im

def composed(name,f,bg=BG,size=32):
    im=Image.new('RGBA',(64,64),bg)
    if name not in ('light_gain','light_release'):
        dx,dy=OFFSETS[f] if name=='doll_step' else (0,round(3*f/7) if name=='doll_dissolve' else 0)
        im.alpha_composite(body(name,f,size),(32-size//2+dx,52-size+dy))
    im.alpha_composite(effect(name,f));return im

def make():
    for path in ['Art/fx','previews']:(R/path).mkdir(parents=True,exist_ok=True)
    for name in NAMES:
        atlas=Image.new('RGBA',(512,64))
        for f in range(8):atlas.alpha_composite(effect(name,f),(64*f,0))
        atlas.save(R/f'Art/fx/{name}_8f.png')
    font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',12)
    small=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',10)
    contact=Image.new('RGBA',(1088,1118),BG);d=ImageDraw.Draw(contact)
    d.text((12,10),'B6 / LIGHT + DOLLS / 6 x 8 frames / 64px cells / Native grid art (preview 2x)',font=font,fill=P['steel3'])
    d.text((12,30),'Overlays only. Dummy tokens appear in composition previews, NOT in production textures. Last frame transparent.',font=small,fill=P['steel2'])
    native=Image.new('RGBA',(584,1010),BG);dn=ImageDraw.Draw(native)
    dn.text((12,10),'B6 / native 1x / dark and mid-night boards',font=font,fill=P['steel3'])
    for row,name in enumerate(NAMES):
        y=60+row*175;d.text((12,y),f'{name} / {sum(TIMES[row])} ms / overlay only',font=font,fill=P['steel3'])
        for f in range(8):
            x=12+f*134
            frame=Image.new('RGBA',(64,64),BG);frame.alpha_composite(effect(name,f))
            contact.alpha_composite(frame.resize((128,128),Image.Resampling.NEAREST),(x,y+22))
            d.text((x,y+151),f'{f}: {TIMES[row][f]} ms',font=small,fill=P['steel2'])
        yn=40+row*160;dn.text((12,yn),name,font=font,fill=P['steel3'])
        for f in range(8):
            native.alpha_composite(composed(name,f,BG),(12+f*70,yn+20))
            native.alpha_composite(composed(name,f,BG2),(12+f*70,yn+86))
    contact.save(R/'previews/light_dolls_contactsheet.png');native.save(R/'previews/light_dolls_native.png')
    # One animation per effect: exact atlas samples, original durations, 1x + 2x view.
    for row,name in enumerate(NAMES):
        fs=[]
        for f in range(8):
            canvas=Image.new('RGBA',(330,204),BG);dc=ImageDraw.Draw(canvas)
            dc.text((8,8),f'{name} / {f} / {TIMES[row][f]} ms',font=font,fill=P['steel3'])
            canvas.alpha_composite(composed(name,f).resize((128,128),Image.Resampling.NEAREST),(8,34))
            canvas.alpha_composite(composed(name,f),(152,66))
            if name not in ('light_gain','light_release'):canvas.alpha_composite(composed(name,f,BG2,48),(244,66))
            dc.text((8,174),'2x / 1x / 48px dummy (if local)',font=small,fill=P['steel2'])
            fs.append(canvas)
        fs[0].save(R/f'previews/{name}_playback.png',save_all=True,append_images=fs[1:]+[fs[0]],duration=TIMES[row]+[800],loop=0,disposal=0,blend=0)
    (R/'previews/playback_timing.json').write_text(json.dumps({n:{'durations_ms':TIMES[i]+[800],'last_preview_frame':'800ms restart hold, not production'} for i,n in enumerate(NAMES)},indent=2)+'\n')
if __name__=='__main__':make()
