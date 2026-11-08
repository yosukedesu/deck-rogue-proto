#!/usr/bin/env python3
"""B1: original native-grid pixel VFX. Pillow 12.x; no source image/resampling.

Artwork is placed at integer coordinates on a 64x64 transparent canvas.
Curves are rasterized directly on that native canvas. NEAREST is preview-only.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import math, json

ROOT = Path(__file__).resolve().parents[1]
P = {
 'edge':'#24344d','deep':'#17283f','steel0':'#344a66','steel1':'#55718c',
 'steel2':'#819eaf','steel3':'#bad2d9','brass0':'#675236','brass1':'#a88147',
 'brass2':'#cfaa63','brass3':'#dfc77f','teal0':'#2d5a67','teal1':'#528c94',
 'teal2':'#9fcaca','red0':'#663746','red1':'#a64f5f','red2':'#dc8290',
 'violet0':'#4c416e','violet1':'#8574a5','violet2':'#b7a8ce',
 'ember0':'#7e4142','ember1':'#c36843','ember2':'#e9a85b',
}
DUR=[65,65,70,80,85,85,100,70] # 620ms; frame 7 explicitly clears effect
KEYS=['enemy_weak','vulnerable','frail','restrain','mist','slow','wound','scald','confusion']
JA=['威圧','脆弱','虚弱','拘束','霞み','重り','負傷','火傷','混乱']
LABEL=['INTIMIDATE','VULNERABLE','FRAIL','RESTRAIN','MIST','WEIGHT','WOUND / CARD','SCALD / CARD','CONFUSION']
fontpath='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
font=ImageFont.truetype(fontpath,12)
small=ImageFont.truetype(fontpath,10)

def color(c): return P.get(c,c)
def new():
 global im,d
 im=Image.new('RGBA',(64,64)); d=ImageDraw.Draw(im)
def line(p,c,w=1): d.line(p,fill=color(c),width=w)
def rect(b,c): d.rectangle(b,fill=color(c))
def poly(p,c): d.polygon(p,fill=color(c))
def dot(x,y,c): d.point((x,y),fill=color(c))
def ell(b,c=None,o=None,w=1): d.ellipse(b,fill=color(c) if c else None,outline=color(o) if o else None,width=w)
def arc(cx,cy,rx,ry,start,end,c,w=1):
 n=max(2,int(abs(end-start)/4))
 pts=[(round(cx+rx*math.cos(math.radians(start+(end-start)*i/n))),round(cy+ry*math.sin(math.radians(start+(end-start)*i/n)))) for i in range(n+1)]
 line(pts,c,w)
def chevron(x,y,span,c):
 line([(x-span,y),(x,y+span//2),(x+span,y)],'violet0',3)
 line([(x-span,y-1),(x,y+span//2-1),(x+span,y-1)],c)
def shard(x,y,c,flip=1): poly([(x,y),(x+2*flip,y+1),(x+3*flip,y+4),(x,y+3)],c)
def path_part(path,t,c,w=1):
 # Reveal along polyline's actual length, not index fractions.
 lengths=[math.dist(a,b) for a,b in zip(path,path[1:])]; total=sum(lengths)*max(0,min(1,t)); out=[path[0]]
 for a,b,L in zip(path,path[1:],lengths):
  if total>=L: out.append(b);total-=L
  else:
   if total>0: out.append((round(a[0]+(b[0]-a[0])*total/L),round(a[1]+(b[1]-a[1])*total/L)))
   break
 if len(out)>1: line(out,c,w)

def enemy_weak(f):
 rx=[11,18,24,26,23,20,17][f]; ry=[4,6,8,9,7,6,5][f]
 y=[41,43,46,48,49,50,51][f]
 bright='violet2' if f in (2,3) else 'violet1'
 # Broken rings press inward. Front/back arcs allow the target to stay readable.
 arc(32,y,rx,ry,195,340,'violet0',3);arc(32,y,rx,ry,15,160,'violet0',3)
 arc(32,y,rx,ry,210,330,bright);arc(32,y,rx,ry,25,145,bright)
 if f<5:
  yy=[11,14,17,21,25][f]; span=[4,6,7,7,5][f]
  chevron(32,yy,span,bright);chevron(32,yy+10,span,'brass2' if f==3 else 'violet1')
 if f in (2,3,4):
  for x in (12,52): line([(x,29+f),(x+(-2 if x<32 else 2),34+f)],'violet1')

def vulnerable(f):
 main=[(32,9),(28,20),(34,26),(29,35),(36,42),(31,54)]
 left=[(31,26),(23,29),(20,36),(13,40)]
 right=[(30,35),(42,32),(48,23)]
 p=[.12,.30,.58,.85,1,1,.9][f]
 base='red1' if f<5 else 'red0'; hi='red2' if f<5 else 'red1'
 path_part(main,p,base,3);path_part(main,p,hi)
 if f>=2:
  path_part(left,min(1,(f-1)/2),base,2);path_part(right,min(1,(f-1)/2),base,2)
  path_part([(32,27),(24,30),(21,37)],min(1,(f-1)/2),hi)
 if f in (3,4,5):
  # Sharp opening edges, unlike a centered spell explosion.
  offset=(f-3)*2
  shard(18-offset,16+offset,'steel2');shard(45+offset,46+offset,'red1',-1)
  line([(9,29),(13,29),(15,27)],'brass2' if f<5 else 'brass1')
  line([(48,39),(51,37),(55,37)],'brass2' if f<5 else 'brass1')

def frail(f):
 y=[-3,-1,0,0,1,3,5][f]; s=[0,0,0,1,2,3,4][f]
 left=[(18-s,15+y),(31-s,11+y),(31-s,23+y),(27-s,28+y),(31-s,33+y),(26-s,39+y),(30-s,49+y),(21-s,40+y),(17-s,28+y)]
 right=[(34+s,12+y),(46+s,15+y),(46+s,29+y),(42+s,41+y),(34+s,48+y),(31+s,40+y),(35+s,33+y),(31+s,28+y),(35+s,22+y)]
 base='steel0' if f<6 else 'edge';hi='steel2' if f in (1,2,3) else 'steel1'
 poly(left,base);poly(right,base)
 line([(18-s,15+y),(31-s,11+y)],'brass2' if f<5 else 'brass1')
 line([(34+s,12+y),(46+s,15+y),(46+s,29+y),(42+s,41+y),(34+s,48+y)],'brass1')
 line([(20-s,18+y),(20-s,28+y),(23-s,36+y)],hi)
 line([(31-s,12+y),(30-s,23+y),(26-s,28+y),(30-s,33+y),(25-s,39+y)],'steel3' if f in (2,3) else 'steel1')
 if f>=3:
  shard(13-(f-3),37+(f-3)*3,'steel2' if f<5 else 'steel1')
  shard(49+(f-3),32+(f-3)*4,'brass1',-1)

def rope(points,hi=True):
 line(points,'brass0',3);line(points,'brass2' if hi else 'brass1')
 # Intermittent twisted fibres, not smooth vector outlines.
 for x,y in points[1::2]: dot(x,y,'brass3' if hi else 'brass2')
def restrain(f):
 rx=[24,22,20,18,16,15,14][f];cy=[35,35,36,37,39,41,43][f]
 extent=[80,150,240,330,360,360,360][f]
 pts=[(round(32+rx*math.cos(math.radians(a))),round(cy+8*math.sin(math.radians(a)))) for a in range(150,150+extent+1,10)]
 rope(pts,f<5)
 if f>0:
  # Second coil closes in the opposite direction.
  pts=[(round(32+rx*math.cos(math.radians(a))),round(cy-11+7*math.sin(math.radians(a)))) for a in range(25,25-max(0,extent-50)-1,-10)]
  rope(pts,f<5)
 if f>=3:
  dy=cy-35
  rope([(26,39+dy),(29,36+dy),(35,40+dy),(34,44+dy),(30,45+dy),(27,42+dy),(30,38+dy),(36,36+dy),(39,39+dy)],f<5)
  rope([(30,43+dy),(25,50+dy),(23,51+dy)],False)
  rope([(35,43+dy),(39,48+dy),(41,47+dy)],False)

def mist(f):
 # Independently drifting ribbons: no filled fog rectangle over the target.
 amount=[.35,.55,.8,1,1,.75,.45][f]
 for idx,(base,shift) in enumerate([(24,0),(34,6),(44,-4)]):
  x=[-4,-1,1,3,5,8,10][f]*(1 if idx%2==0 else -1)+shift
  L=round(19*amount);cx=32+x//2
  pts=[(cx-L,base+2),(cx-L+5,base-1),(cx-7,base-1),(cx-1,base+2),(cx+6,base+2),(cx+11,base),(cx+L,base+1)]
  line(pts,'teal0' if idx!=1 else 'steel0',4)
  line([(a,b-1) for a,b in pts],'teal1' if idx!=1 else 'steel2',2)
  line(pts[1:4],'teal2' if f in (2,3,4) else 'teal1')
 if f in (2,3,4,5):
  rect((12+(f-2)*3,51,17+(f-2)*3,52),'steel1')
  line([(42-(f-2)*2,17),(46-(f-2)*2,17)],'teal1')

def weight(cx,y,small=False,fade=False):
 if small:
  ell((cx-3,y-9,cx+3,y-3),None,'brass1');
  poly([(cx-5,y-3),(cx+5,y-3),(cx+8,y+9),(cx-8,y+9)],'steel0')
  poly([(cx-4,y-2),(cx,y-2),(cx-1,y+7),(cx-6,y+7)],'steel2')
  line([(cx-5,y-3),(cx+5,y-3)],'brass2');line([(cx-7,y+9),(cx+7,y+9)],'brass1')
 else:
  ell((cx-5,y-10,cx+5,y-1),None,'brass1',2)
  poly([(cx-9,y-2),(cx+9,y-2),(cx+15,y+20),(cx+11,y+23),(cx-11,y+23),(cx-15,y+20)],'steel0' if not fade else 'edge')
  poly([(cx-8,y),(cx-1,y),(cx-4,y+20),(cx-11,y+20)],'steel2' if not fade else 'steel1')
  line([(cx-9,y-2),(cx+9,y-2)],'brass2' if not fade else 'brass1')
  line([(cx-13,y+21),(cx+12,y+21)],'brass1')
  rect((cx-1,y+5,cx+2,y+14),'steel1')

def slow(f):
 y=[13,16,21,28,29,28,28][f];weight(32,y,fade=f==6)
 if f<3:
  line([(21,7+f*4),(21,11+f*4)],'steel1');line([(44,6+f*3),(44,11+f*3)],'brass1')
 if f in (3,4,5,6):
  rx=[17,22,26,27][f-3]
  arc(32,54,rx,4,15,165,'brass2' if f==3 else 'steel1')
  arc(32,53,rx,4,205,335,'steel1')
  if f<6:
   shard(12-(f-3)*2,47-(f-3),'steel2');shard(51+(f-3)*2,48-(f-3),'brass1',-1)

def card_edges(c='steel1'):
 # Only detached corner strokes; actual card face supplied by engine.
 line([(13,15),(13,10),(19,10)],c);line([(44,10),(50,10),(50,15)],c)
 line([(13,47),(13,53),(19,53)],c);line([(44,53),(50,53),(50,47)],c)

def wound(f):
 # Red slit tears through a card overlay, never a blood spray.
 path=[(43,9),(35,20),(38,26),(28,34),(30,39),(20,54)]
 t=[.12,.32,.58,.82,1,1,1][f]
 if f in (1,2,3,4):card_edges('steel1')
 path_part(path,t,'red0',5);path_part(path,t,'red1',3)
 path_part([(x-1,y) for x,y in path],t,'red2' if f<5 else 'red1')
 if f>=3:
  path_part([(29,34),(21,32),(16,36)],min(1,(f-2)/2),'red1',2)
  path_part([(36,22),(45,24),(48,20)],min(1,(f-2)/2),'red1',2)
 if f in (3,4,5):
  shard(45+(f-3)*2,15+(f-3)*2,'steel2');shard(17-(f-3)*2,44+(f-3)*3,'red1')

def flame(x,y,h,bright=True):
 poly([(x-4,y),(x-4,y-4),(x-2,y-6),(x-1,y-h),(x+2,y-h+4),(x+2,y-5),(x+4,y-7),(x+5,y-2),(x+3,y+1),(x-2,y+2)],'ember0')
 poly([(x-2,y),(x-1,y-h+4),(x+1,y-4),(x+3,y-3),(x+2,y+1)],'ember1')
 if bright:line([(x,y),(x,y-4),(x+1,y-5)],'ember2')

def scald(f):
 # Burn along the lower and side edges of the card, localized heat, no white flash.
 if f<6: card_edges('steel1' if f<4 else 'steel0')
 l=[25,21,17,14,14,16,20][f];r=[38,42,47,49,49,47,43][f]
 pts=[(l,48),(l+4,50),(l+7,48),(l+12,51),(r-4,49),(r,50),(r,54),(l,54)]
 poly(pts,'edge');line(pts[:6],'ember1' if f<5 else 'ember0',2)
 if f<6:
  heights=[6,10,16,19,15,8][f]
  flame(25,49,heights);flame(40,51,max(5,heights-4),f<5)
  if f>=2:flame(47,40,heights-3,True)
 if f in (2,3,4,5):
  for x,y in [(23-f,29-f),(38+f,23-f),(50,30-f*2)]:
   rect((x,y,x+1,y+1),'ember2' if f<5 else 'ember1')
 if f==6:line([(22,51),(30,52),(35,51),(43,53)],'ember0')

def confusion(f):
 # Reveal a counterclockwise spiral, then unwind; no rotating whole image.
 angle=[-80,-48,-16,16,44,70,92][f];fraction=[.28,.5,.75,1,1,.78,.5][f]
 pts=[]
 for i in range(81):
  t=i/80
  if t>fraction:break
  a=math.radians(angle+610*t);r=22-18*t
  pts.append((round(32+r*math.cos(a)),round(30+r*.84*math.sin(a))))
 line(pts,'violet0',4);line(pts,'violet1',2)
 if len(pts)>8:line(pts[:len(pts)//3],'violet2' if f<5 else 'violet1')
 if len(pts)>20:line(pts[-12:],'teal2' if f in (2,3,4) else 'teal1')
 for j,(cx,cy) in enumerate([(12+f,48-f),(49-f,12+f)]):
  if f<6:poly([(cx,cy-2),(cx+2,cy),(cx,cy+2),(cx-2,cy)],'brass2' if f<4 else 'brass1')

FUNCS=[enemy_weak,vulnerable,frail,restrain,mist,slow,wound,scald,confusion]

def mark(key):
 new();x=32;y=49
 if key=='enemy_weak':
  arc(x,y,18,6,195,345,'violet1');arc(x,y,18,6,15,165,'violet1')
  chevron(x,43,5,'violet2');chevron(x,49,4,'brass2')
 elif key=='vulnerable':
  line([(31,39),(28,44),(33,48),(29,53),(32,58)],'red0',3)
  line([(31,39),(28,44),(33,48),(29,53),(32,58)],'red2')
  line([(31,47),(22,48),(19,53)],'red1');line([(32,50),(40,47),(44,48)],'red1')
 elif key=='frail':
  poly([(23,41),(31,39),(30,46),(27,49),(30,52),(28,58),(24,53)],'steel0')
  poly([(34,40),(41,42),(40,51),(35,58),(32,53),(34,49),(31,46)],'steel0')
  line([(23,41),(31,39)],'brass2');line([(34,40),(41,42),(40,51),(35,58)],'brass1')
  line([(30,40),(29,46),(26,49),(29,52)],'steel2')
 elif key=='restrain':
  rope([(20,48),(24,44),(40,44),(44,48),(40,53),(24,53),(20,48)],False)
  rope([(28,48),(31,45),(35,48),(35,52),(32,54),(28,51),(28,48)],True)
  rope([(31,52),(27,58)],False);rope([(34,52),(39,57)],False)
 elif key=='mist':
  for dx,dy in [(0,0),(4,7)]:
   p=[(17+dx,45+dy),(23+dx,43+dy),(29+dx,46+dy),(37+dx,46+dy),(43+dx,44+dy)]
   line(p,'teal0',3);line([(a,b-1) for a,b in p],'teal1');line(p[1:4],'teal2')
 elif key=='slow':weight(32,48,small=True)
 elif key=='wound':
  line([(40,18),(32,28),(35,33),(27,38),(29,42),(24,48)],'red0',4)
  line([(40,18),(32,28),(35,33),(27,38),(29,42),(24,48)],'red1',2)
  line([(39,18),(31,28),(34,33),(26,38)],'red2')
 elif key=='scald':
  poly([(15,48),(22,50),(28,48),(35,51),(42,49),(49,51),(49,54),(15,54)],'edge')
  line([(16,49),(22,51),(28,49),(35,52),(42,50),(48,52)],'ember0',2)
  line([(21,50),(24,51),(27,50)],'ember1');line([(39,51),(42,50),(45,51)],'ember1');dot(25,51,'ember2')
 elif key=='confusion':
  pts=[]
  for i in range(50):
   t=i/49;a=math.radians(-40+540*t);r=12-9*t
   pts.append((round(32+r*math.cos(a)),round(48+r*.75*math.sin(a))))
  line(pts,'violet0',3);line(pts,'violet1');line(pts[8:19],'violet2');line(pts[-8:],'teal2')
 return im.copy()

def preview_base(size): return Image.new('RGBA',size,'#101b2b')
def text(draw,xy,s,fill='#aebfcf',f=font):draw.text(xy,s,font=f,fill=fill)
def bg_card(canvas,xy,scale=1):
 dr=ImageDraw.Draw(canvas);x,y=xy
 dr.rectangle((x+12*scale,y+8*scale,x+51*scale,y+55*scale),fill='#1b2b40',outline='#675236',width=scale)
 dr.line([(x+19*scale,y+18*scale),(x+44*scale,y+18*scale)],fill='#344a66',width=scale)
 dr.line([(x+19*scale,y+21*scale),(x+37*scale,y+21*scale)],fill='#344a66',width=scale)
def guide(canvas,xy,scale=1):
 dr=ImageDraw.Draw(canvas);x,y=xy
 # Clearly labelled generic anchor guide, never a new character design.
 dr.line([(x+32*scale,y+47*scale),(x+32*scale,y+56*scale)],fill='#27374b',width=scale)
 dr.line([(x+25*scale,y+52*scale),(x+39*scale,y+52*scale)],fill='#27374b',width=scale)

ASSETS={};MARKS={}
for key,func in zip(KEYS,FUNCS):
 frames=[]
 for f in range(8):
  new()
  if f<7:func(f)
  frames.append(im.copy())
 sheet=Image.new('RGBA',(512,64))
 for f,frame in enumerate(frames):sheet.paste(frame,(f*64,0))
 (ROOT/'Art/fx').mkdir(parents=True,exist_ok=True)
 sheet.save(ROOT/f'Art/fx/status_{key}_8f.png',optimize=True)
 m=mark(key);m.save(ROOT/f'Art/fx/status_{key}_mark.png',optimize=True)
 ASSETS[key]=frames;MARKS[key]=m
(ROOT/'palette.json').write_text(json.dumps(P,indent=2)+'\n')
(ROOT/'previews').mkdir(exist_ok=True)

# Native 1x reference: one row per effect + persistent mark. All effects at 1:1.
sheet=preview_base((760,9*90+75));dr=ImageDraw.Draw(sheet)
text(dr,(12,10),'B1 / STATUS APPLICATION     64 x 64 native pixels / 8 frames + mark',f=font)
for f in range(8):text(dr,(140+f*66,36),f'{f} / {DUR[f]}ms',f=small)
text(dr,(681,36),'MARK',f=small)
for j,key in enumerate(KEYS):
 y=60+j*90;text(dr,(10,y+15),LABEL[j],f=small)
 text(dr,(10,y+33),'card overlay' if key in ('wound','scald') else 'anchor (32,52)',f=small)
 for f,frame in enumerate(ASSETS[key]):
  x=138+f*66
  if key in ('wound','scald'):bg_card(sheet,(x,y))
  else:guide(sheet,(x,y))
  sheet.alpha_composite(frame,(x,y))
 if key in ('wound','scald'):bg_card(sheet,(680,y))
 else:guide(sheet,(680,y))
 sheet.alpha_composite(MARKS[key],(680,y))
 dr.line([(10,y+76),(747,y+76)],fill='#26364a')
text(dr,(12,sheet.height-14),'Preview only: dark cards and anchor guides are not baked into PNG assets.',f=small)
sheet.convert('RGB').save(ROOT/'previews/contactsheet_native.png',optimize=True)

# Three-background test, 2x peak + native mark, without resampling artwork.
comp=preview_base((1320,765));dr=ImageDraw.Draw(comp)
text(dr,(14,10),'B1 / READABILITY    peak 2x nearest / mark 1x    dark, midtone, light backgrounds')
for j,key in enumerate(KEYS):
 x=10+(j%3)*438;y=44+(j//3)*238
 text(dr,(x,y),LABEL[j])
 for k,bgc in enumerate(['#101b2b','#536477','#d3d9df']):
  tile=Image.new('RGBA',(142,205),bgc)
  peak=ASSETS[key][3].resize((128,128),Image.Resampling.NEAREST)
  tile.alpha_composite(peak,(7,4))
  tile.alpha_composite(MARKS[key],(39,135))
  comp.alpha_composite(tile,(x+k*144,y+21))
comp.convert('RGB').save(ROOT/'previews/readability_2x.png',optimize=True)

# 3x3 visual review animation: apply 620ms, mark 900ms, reset 220ms.
an=[];times=DUR+[900,220]
for step in range(10):
 canvas=preview_base((624,696));dr=ImageDraw.Draw(canvas)
 for j,key in enumerate(KEYS):
  x=(j%3)*208;y=(j//3)*232
  text(dr,(x+8,y+5),LABEL[j],f=small)
  text(dr,(x+8,y+21),f'APPLY {step}/7' if step<7 else ('CLEAR + MARK' if step==7 else ('PERSISTENT MARK' if step==8 else 'RESET')),f=small)
  tile=Image.new('RGBA',(64,64),'#101b2b')
  if key in ('wound','scald'):bg_card(tile,(0,0))
  else:guide(tile,(0,0))
  if 7<=step<=8:tile.alpha_composite(MARKS[key])
  if step<8:tile.alpha_composite(ASSETS[key][step])
  canvas.alpha_composite(tile.resize((192,192),Image.Resampling.NEAREST),(x+8,y+32))
 an.append(canvas.convert('RGB'))
an[0].save(ROOT/'previews/status_application_preview.png',save_all=True,append_images=an[1:],duration=times,loop=0,disposal=0,blend=0,optimize=True)
print('Generated 18 native PNG assets and 3 review previews.')
