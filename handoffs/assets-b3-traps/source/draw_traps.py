#!/usr/bin/env python3
"""Original native 64px trap VFX. No source images, resizing, filters or RNG."""
from PIL import Image,ImageDraw,ImageFont,ImageColor
from pathlib import Path
import math,json
R=Path(__file__).resolve().parents[1]
P={k:ImageColor.getrgb(v)+(255,) for k,v in json.loads((R/'palette.json').read_text()).items()}
KEYS=['place','trigger','expire','destroy']
TIMES={'place':[65,65,70,90,85,70,75,60],'trigger':[60,60,70,85,80,70,75,60],'expire':[75,80,85,90,90,85,85,60],'destroy':[60,65,70,75,80,80,85,60]}
LABELS=['SET / MESH','TRIGGER / TURN','EXPIRE / RELEASE','DESTROY / BREAK']
BG='#0c1525';BG2='#17283f'
def blank():return Image.new('RGBA',(64,64),(0,0,0,0))
def line(im,points,c,w=1):ImageDraw.Draw(im).line(points,fill=P[c],width=w)
def pixel(im,x,y,c):
 if 0<=x<64 and 0<=y<64:im.putpixel((int(x),int(y)),P[c])
def polar(c,r,a):return (round(c[0]+r*math.cos(a)),round(c[1]+r*math.sin(a)))
def arc(im,c,r,a,b,color):
 pts=[polar(c,r,a+(b-a)*i/max(1,round(abs(b-a)*r*2))) for i in range(max(1,round(abs(b-a)*r*2))+1)]
 line(im,pts,color)
def gear(cx,cy,r,teeth,angle,bright=0):
 im=blank(); root=r-2; inner=max(2,r-5); hub=2 if r>8 else 1
 for y in range(cy-r-1,cy+r+2):
  for x in range(cx-r-1,cx+r+2):
   dx=x-cx;dy=y-cy;d=math.hypot(dx,dy);a=math.atan2(dy,dx)-angle
   phase=((a/(2*math.pi)*teeth+0.5)%1)-0.5
   outer=r if abs(phase)<.24 else root
   spoke=abs(math.sin(a*2))<(.18 if r>8 else .25)
   if d<=outer and (d>=inner or d<=hub or spoke):
    if d>=outer-.9 or (inner<=d<inner+.7):co='teal0'
    else:co='teal1'
    if dx+dy<-3 and d>inner:co='teal2' if bright else 'teal1'
    if bright and d>inner+1 and abs(phase)<.10:co='teal2'
    if d<=hub:co='brass2' if bright else 'brass1'
    if spoke and d<inner:co='brass1' if not bright else 'brass2'
    pixel(im,x,y,co)
 # asymmetric brass marker on one tooth makes actual rotation legible
 mx,my=polar((cx,cy),r-1,angle);pixel(im,mx,my,'brass3' if bright else 'brass2')
 return im

def pair(im,big=(26,34),small=(43,27),angle=0,bright=0):
 im.alpha_composite(gear(*big,11,8,angle,bright));im.alpha_composite(gear(*small,7,6,-angle*8/6+.3,bright))

def glint(im,c,color='teal2',r=2):
 x,y=c;line(im,[(x-r,y),(x+r,y)],color);line(im,[(x,y-r),(x,y+r)],color)

def frame(key,f):
 im=blank()
 if f==7:return im
 if key=='place':
  bx=[15,18,22,25,26,26,26][f];sx=[52,50,47,44,43,43,43][f]
  by=[38,37,36,35,34,34,34][f];sy=[22,23,24,26,27,27,27][f]
  if f<5:
   pair(im,(bx,by),(sx,sy),[0,.12,.24,.36,.48][f],f==4)
  elif f==5:
   pair(im,angle=.48);arc(im,(33,31),22,-.1,.9,'teal1');arc(im,(33,31),22,2.5,3.4,'brass1')
  else:
   # seated mechanism is now the existing UI token; let only the pulse fade
   arc(im,(33,31),23,.2,.7,'teal0');arc(im,(33,31),23,2.7,3.2,'teal0');pixel(im,33,10,'teal1')
  if f in [2,3]:
   line(im,[(8+f*2,39),(12+f*2,38)],'teal0');line(im,[(52-f,18),(55-f,17)],'teal0')
  if f==4:glint(im,(35,29));pixel(im,37,24,'brass2');pixel(im,39,32,'brass2')
 elif key=='trigger':
  if f<=4:pair(im,angle=.48+[0,.18,.5,.9,1.3][f],bright=f in [2,3])
  if f>=1:
   rr=[0,20,22,24,25,26,27][f]
   col='teal2' if f in [2,3] else ('teal1' if f<5 else 'teal0')
   arc(im,(32,32),rr,-1.9+f*.18,-.9+f*.18,col)
   arc(im,(32,32),rr,1.2+f*.18,2.2+f*.18,col)
  if f in [2,3,4]:
   for a,l in [(-.5,5),(1.1,4),(2.65,3),(-1.85,3)]:
    r=[0,0,19,23,24][f];line(im,[polar((32,32),r,a),polar((32,32),r+l,a)],'brass2' if f==2 else 'teal1')
   if f==2:glint(im,(35,29),'teal2',3)
  if f==5:glint(im,(49,16),'teal1',1);glint(im,(15,48),'brass1',1)
  if f==6:pixel(im,54,19,'teal0');pixel(im,10,46,'teal0')
 elif key=='expire':
  if f<4:
   pair(im,(26-f*2,34+f),(43+f,27-f),.48-f*.22)
  if f>=2:
   # unbroken hoops unwind into quiet, descending single-pixel ribbons
   rr=[0,0,12,14,16,18,20][f];end=[0,0,4.8,4,3.3,2.3,1.4][f]
   arc(im,(26,35+min(f,4)),rr,-.5,end,'teal1' if f<4 else 'teal0')
   arc(im,(42,27+f),max(4,rr-5),2.6,2.6+max(.45,end*.6),'brass1' if f<4 else 'brass0')
  if f in [4,5]:
   line(im,[(17-f,41+f),(14-f,44+f),(14-f,47+f)],'teal0')
  if f==6:pixel(im,24,57,'teal0');pixel(im,41,48,'brass0')
 elif key=='destroy':
  base=blank();pair(base,angle=.48,bright=f==1)
  if f==0:
   im=base;line(im,[(27,19),(24,28),(31,35),(25,43),(29,47)],'red1')
  else:
   # Three separated gear sectors. No full-ring rotation or gentle release.
   for y in range(64):
    for x in range(64):
     px=base.getpixel((x,y))
     if not px[3]:continue
     side=0 if y<30 else (1 if x<32 else 2)
     # carve a jagged rupture before fragments move
     if abs(x-(26 if y<31 else 29 if y<38 else 25))<2:continue
     vx,vy=[(-2,-2),(-2,1),(2,2)][side];step=[0,1,2,3,4,5,6][f]
     nx=x+vx*step;ny=y+vy*step
     if f>=4 and (x*3+y*5)%7<f-3:continue
     if f==6 and (x+y)%4:continue
     if f>=4:px=P['teal0'] if side!=2 else P['steel0']
     if 2<=nx<62 and 2<=ny<62:im.putpixel((nx,ny),px)
   if f in [1,2]:
    line(im,[(27,20),(25,28),(30,34),(26,43)],'brass3' if f==1 else 'red1')
   if f<=4:
    for a in [-2.6,-1.1,.5,1.7]:
     rr=7+f*4;line(im,[polar((31,31),rr,a),polar((31,31),rr+3,a)],'brass2' if f<3 else 'red0')
 return im

def frames():return {k:[frame(k,f) for f in range(8)] for k in KEYS}
def font(n):
 try:return ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',n)
 except OSError:return ImageFont.load_default()
def slot(bg=BG):
 im=Image.new('RGBA',(64,64),bg);d=ImageDraw.Draw(im)
 d.rounded_rectangle((7,8,56,55),radius=3,fill='#122034',outline='#675236',width=1)
 # Generic preview-only trap marker; no game card / leader art.
 d.rectangle((11,12,52,45),outline='#24344d',width=1);d.line((14,50,49,50),fill='#344a66')
 for x in [24,31,38]:d.rectangle((x,48,x+2,52),fill='#55718c')
 return im

def composite(key,f,fs,bg=BG):
 im=slot(bg)
 if key in ['expire','destroy'] and f>=4:im=Image.new('RGBA',(64,64),bg)
 im.alpha_composite(fs[key][f]);return im

def main():
 for p in ['Art/fx','previews']:(R/p).mkdir(parents=True,exist_ok=True)
 fs=frames()
 for k in KEYS:
  sheet=Image.new('RGBA',(512,64));
  for i,im in enumerate(fs[k]):sheet.paste(im,(i*64,0))
  sheet.save(R/f'Art/fx/trap_{k}_8f.png',optimize=False)
 # Contact sheet: raw overlay on dark, labels outside cells.
 cs=Image.new('RGB',(1112,700),BG);d=ImageDraw.Draw(cs)
 d.text((16,9),'B3 | TRAP VFX | native 64x64 | 8 frames | 2x NEAREST',font=font(19),fill='#bad2d9')
 for r,k in enumerate(KEYS):
  y=45+r*161;d.text((16,y),LABELS[r]+f'  /  {sum(TIMES[k])} ms',font=font(14),fill='#cfaa63')
  for f in range(8):
   tile=Image.new('RGBA',(64,64),BG2 if f%2 else BG);tile.alpha_composite(fs[k][f])
   x=16+f*137;cs.paste(tile.convert('RGB').resize((128,128),Image.Resampling.NEAREST),(x,y+22));d.text((x+3,y+24),f'{f} / {TIMES[k][f]}ms',font=font(10),fill='#819eaf')
 cs.save(R/'previews/traps_contactsheet.png')
 # Native full sequence on two dark backgrounds. Never upscale this output.
 native=Image.new('RGB',(568,688),BG);d=ImageDraw.Draw(native)
 d.text((12,8),'Native 1:1 / dark panels',font=font(15),fill='#bad2d9')
 for row,k in enumerate(KEYS):
  y=35+row*160;d.text((12,y),LABELS[row],font=font(12),fill='#cfaa63')
  for b,bg in enumerate([BG,BG2]):
   for f in range(8):
    tile=Image.new('RGBA',(64,64),bg);tile.alpha_composite(fs[k][f]);native.paste(tile.convert('RGB'),(12+f*68,y+20+b*66))
 native.save(R/'previews/traps_native.png')
 # Sequential playback avoids timing distortion from merging many phase boundaries.
 playback=[];entries=[];durations=[]
 for active,k in enumerate(KEYS):
  for stage,dt in [('ready',350)]+list(enumerate(TIMES[k]))+[('hold',700)]:
   im=Image.new('RGB',(650,236),BG);d=ImageDraw.Draw(im)
   d.text((13,10),'B3 / sequential timing / 2x NEAREST / preview-only slot',font=font(15),fill='#bad2d9')
   for j,key in enumerate(KEYS):
    x=13+j*159;d.text((x,39),['SET','TRIGGER','EXPIRE','DESTROY'][j],font=font(12),fill='#cfaa63' if active==j else '#55718c')
    if j>active or (j==active and stage=='ready'):cell=slot()
    else:cell=composite(key,7 if j<active or stage=='hold' else stage,fs)
    im.paste(cell.convert('RGB').resize((128,128),Image.Resampling.NEAREST),(x,64))
   d.text((13,207),f'{LABELS[active]} / frame {stage} / {dt} ms',font=font(12),fill='#819eaf')
   playback.append(im);durations.append(dt);entries.append({'effect':k,'stage':stage,'duration_ms':dt})
 playback[0].save(R/'previews/traps_playback.png',save_all=True,append_images=playback[1:],duration=durations,loop=0,disposal=0,blend=0)
 (R/'previews/playback_timing.json').write_text(json.dumps({'frame_count':len(entries),'durations_ms':durations,'sequence':entries},indent=2)+'\n')
if __name__=='__main__':main()
