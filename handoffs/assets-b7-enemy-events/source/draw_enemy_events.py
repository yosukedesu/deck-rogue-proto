#!/usr/bin/env python3
"""B7 original integer-pixel effects, drawn at final 64x64 resolution. No game art copied."""
from PIL import Image,ImageDraw,ImageFont,ImageColor
from pathlib import Path
import json,math,hashlib
R=Path(__file__).resolve().parents[1]
P={k:ImageColor.getrgb(v)+(255,) for k,v in json.loads((R/'palette.json').read_text()).items()}
SPECS=[
 ('death','崩れて消える',[55,65,75,80,85,85,80,75]),
 ('steal','盗み',[45,50,55,65,65,60,55,45]),
 ('flee','逃走',[35,40,45,45,50,55,55,55]),
 ('split','分裂',[50,55,65,70,75,70,65,50]),
 ('hatch','孵化',[60,60,70,70,75,70,65,50]),
 ('transform','残機の変身',[60,65,70,80,80,80,70,55]),
 ('rally','応援',[45,50,55,65,65,60,55,45]),
 ('heal','回復',[50,55,60,70,70,65,60,50]),
 ('mill','山札喰い',[50,55,60,70,70,65,60,50]),
 ('enrage','激昂',[45,50,55,65,70,65,60,50])]

def line(im,pts,c):ImageDraw.Draw(im).line(pts,fill=P[c],width=1)
def dot(im,x,y,c):
 if 0<=x<64 and 0<=y<64:im.putpixel((x,y),P[c])
def poly(im,pts,c):ImageDraw.Draw(im).polygon(pts,fill=P[c])
def spark(im,x,y,c,r=1):line(im,[(x-r,y),(x+r,y)],c);line(im,[(x,y-r),(x,y+r)],c)
def ring(im,cx,cy,rx,ry,c,a=0,b=math.tau):
 n=max(12,round((b-a)*max(rx,ry)*2));line(im,[(round(cx+rx*math.cos(a+(b-a)*j/n)),round(cy+ry*math.sin(a+(b-a)*j/n))) for j in range(n+1)],c)
def chip(im,x,y,c,orient=1):poly(im,[(x,y),(x+3*orient,y-2),(x+4*orient,y+1),(x+1*orient,y+2)],c)
def frame(name,f):
 im=Image.new('RGBA',(64,64))
 if f==7:return im
 c=['brass0','brass1','brass2','brass3','brass2','brass1','brass0'][f]
 if name=='death':
  pts=[(21,12),(33,10),(42,17),(15,26),(29,24),(45,29),(20,37),(36,35),(47,43),(26,46),(38,48),(17,46)]
  for j,(x,y) in enumerate(pts):
   age=f-j//3
   if 0<=age<=3:
    x+=(-1 if j%2 else 1)*age*2;y+=age*3
    if age<2:chip(im,x,y,'steel2' if age==0 else 'steel1',1 if j%2 else -1)
    else:dot(im,x,y,'steel0');dot(im,x+2,y-2,'brass0')
  if f>3:
   for x in [18,27,38,47]:dot(im,x,54+(f%2),'steel1' if f<6 else 'steel0')
 elif name=='steal':
  y=[34,30,26,23,26,30,33][f];x=35
  if f<6:
   poly(im,[(x-3,y-8),(x-5,y-12),(x+5,y-12),(x+3,y-8),(x+7,y-3),(x+7,y+4),(x+4,y+7),(x-4,y+7),(x-7,y+4),(x-7,y-3)],'brass0')
   poly(im,[(x-2,y-7),(x+2,y-7),(x+5,y-2),(x+4,y+4),(x+1,y+5),(x-4,y+3),(x-4,y-2)],'brass1')
   line(im,[(x-4,y-7),(x+4,y-7)],'brass3');line(im,[(x-2,y-3),(x-3,y+2),(x-1,y+4)],'brass2')
   ring(im,x+1,y,2,2,'brass3')
  if f<5:
   xx=[13,17,23,29,34][f];yy=[23,18,15,15,18][f]
   ring(im,xx,yy,2,3,'brass2');dot(im,xx,yy,'brass3')
   line(im,[(xx-5,yy+3),(xx-3,yy+2)],'brass0')
  if 2<=f<=5:spark(im,48,20+f,c,2 if f==3 else 1)
  if f==6:dot(im,36,23,'brass1');dot(im,43,29,'brass0')
 elif name=='flee':
  for j in range(5):
   age=f-j//2
   if 0<=age<=4:
    x=39-j*5-age*3;y=48+(j%2)*3-age
    rx=max(1,[2,4,5,4,2][age]);ry=max(1,rx-1)
    ring(im,x,y,rx,ry,'brass1' if age<3 else 'brass0',math.pi*.9,math.tau*1.1)
    if age<3:line(im,[(x-rx+1,y+2),(x+rx,y+2)],'steel1')
  if f<5:
   line(im,[(28+f*2,42),(40+f*2,42)],'steel2' if f in (1,2) else 'steel0')
   line(im,[(36+f,38),(44+f*2,38)],'brass1')
 elif name=='split':
  dx=[0,2,5,9,13,17,20][f]
  for sign in [-1,1]:
   x=32+sign*dx;rx=[3,4,6,7,8,8,7][f]
   ring(im,x,35,rx,14,'teal2' if f==3 else 'teal1',-1.2 if sign>0 else 1.95,1.2 if sign>0 else 4.3)
   if f<5:
    line(im,[(x,25),(x+sign*2,28),(x,33)],'steel3' if f==2 else 'teal1')
   if f>=2:dot(im,x,52,'teal1')
  if f<=3:line(im,[(31,18),(34,24),(29,31),(35,37),(31,45)],'brass2' if f in (1,2) else 'brass0')
  if f in (2,3,4):spark(im,32,34,'steel3',2)
 elif name=='hatch':
  if f<=2:
   line(im,[(32,17),(29,24),(34,29),(29,35),(32,43)],'brass2' if f else 'brass0')
   if f>0:line(im,[(31,28),(24,27),(20,30)],'steel2');line(im,[(31,35),(39,37),(43,34)],'steel2')
  if f>=2:
   a=f-2
   for sign in [-1,1]:
    x=32+sign*(8+a*3);y=39+a*2
    poly(im,[(x-sign*5,y-8),(x-sign*1,y-5),(x+sign*1,y-9),(x+sign*5,y-4),(x+sign*4,y+2),(x+sign*1,y+4),(x-sign*3,y+2)],'steel1' if a<3 else 'steel0')
    line(im,[(x-sign*5,y-8),(x-sign*1,y-5),(x+sign*1,y-9),(x+sign*5,y-4)],'steel3' if a<2 else 'steel2')
   if f<6:
    for x,y in [(23-a*3,23+a),(41+a*3,25+a),(29,14+a*2)]:chip(im,x,y,'brass2' if f<4 else 'brass0')
  if f in (2,3,4):spark(im,32,28,'teal2',3 if f==3 else 2)
 elif name=='transform':
  rx=[19,16,12,5,12,20,25][f];ry=[21,23,24,25,24,22,19][f]
  for a,b in [(0.2,2.7),(3.35,5.9)]:ring(im,32,30,rx,ry,'violet2' if f==3 else 'violet1',a,b)
  if f<=4:
   for j in range(4):
    ang=j*math.pi/2+f*.5;x=round(32+rx*math.cos(ang));y=round(30+ry*math.sin(ang));spark(im,x,y,'brass2',1)
  if f in (2,3,4):
   poly(im,[(32,12),(35,29),(32,46),(29,29)],'violet1');line(im,[(32,16),(32,42)],'steel3')
  if f>=4:
   spark(im,14,19+f,'violet2',1);spark(im,49,45-f,'violet1',1)
 elif name=='rally':
  rx=[7,10,14,18,22,25,28][f]
  ring(im,32,28,rx,max(3,rx//2),'brass2' if f in (2,3) else 'brass1',0.3,2.85)
  ring(im,32,28,rx,max(3,rx//2),'brass2' if f in (2,3) else 'brass1',3.45,5.95)
  if f<=4:
   y=27+(1 if f in (1,3) else 0)
   poly(im,[(26,y),(38,y),(38,y+8),(35,y+10),(29,y+10),(26,y+8)],'brass0')
   ring(im,32,y,6,3,'brass3');line(im,[(26,y+1),(38,y+1)],'steel2')
   line(im,[(27,y+4),(30,y+8),(33,y+4),(36,y+8)],'brass2')
   line(im,[(23,17+f),(29,23)],'brass2');line(im,[(41,17+f),(35,23)],'brass2')
  if f in (2,3,4):spark(im,32,13,'brass3',2)
 elif name=='heal':
  h=[3,6,9,10,8,5,2][f];cy=36-f*2
  # Clear restoration plus sign and rising droplets, no medical-red cross.
  poly(im,[(30,cy-h),(34,cy-h),(34,cy-2),(32+h,cy-2),(32+h,cy+2),(34,cy+2),(34,cy+h),(30,cy+h),(30,cy+2),(32-h,cy+2),(32-h,cy-2),(30,cy-2)],'teal1' if f<5 else 'teal0')
  line(im,[(32,cy-h+1),(32,cy+h-1)],'teal2');line(im,[(33-h,cy),(31+h,cy)],'teal2')
  for j in range(3):
   x=[18,46,23][j];y=49-f*3-j*6
   spark(im,x,y,'steel3' if f in (2,3) else 'teal1',1)
  if f<=4:ring(im,32,52,12+f*2,3,'teal1',.2,2.95)
 elif name=='mill':
  # The bitten card is blue glass with a brass rim, never cream parchment.
  cx=[18,20,23,26,29,32,34][f];y=22
  if f<=5:
   card=Image.new('RGBA',(64,64));ImageDraw.Draw(card).rectangle((cx-6,y,cx+6,y+19),fill=P['deep'],outline=P['brass1'])
   line(card,[(cx-3,y+5),(cx+3,y+5),(cx+3,y+12),(cx-3,y+12),(cx-3,y+5)],'steel1')
   bite=max(0,f-1)*3
   # Remove the right edge in a progressively deeper stepped bite.
   if bite:
    ImageDraw.Draw(card).polygon([(cx+7-bite,y+3),(cx+3-bite,y+6),(cx+7-bite,y+9),(cx+3-bite,y+12),(cx+7-bite,y+16),(cx+8,y+16),(cx+8,y+3)],fill=(0,0,0,0))
   im.alpha_composite(card)
  if f<=5:
   spread=[10,9,7,4,2,1][f]
   line(im,[(42,20),(38,24),(34,24+spread),(38,25+spread),(41,23+spread)],'violet2')
   line(im,[(42,43),(38,39),(34,39-spread),(38,38-spread),(41,40-spread)],'violet1')
  if f>=2:
   for j in range(4):
    x=30+j*3+(f-2);y=43+j%2*4+(f-2)
    dot(im,x,y,'brass1' if f<5 else 'brass0')
 elif name=='enrage':
  for j,x in enumerate([13,20,44,51]):
   h=[4,8,13,22,25,17,8][f]-(j%2)*3;y=48+(j%2)*3
   if h>0:
    poly(im,[(x-3,y),(x-3,y-h//2),(x-1,y-h+3),(x+1,y-h),(x+1,y-h//2),(x+3,y-4),(x+2,y)],'red0')
    line(im,[(x,y-1),(x-1,y-h//2),(x+1,y-h+2)],'red2' if f==3 else 'red1')
  if 1<=f<=5:
   ring(im,32,53,15+f,3,'red1',.1,3.0)
   spark(im,26,13-f,'red2',1);spark(im,39,17-f,'red1',1)
  if f in (2,3):line(im,[(26,22),(29,24)],'red2');line(im,[(38,22),(35,24)],'red2')
 if f==6 and name in ('split','transform','rally'):
  # Last visible frame breaks into darker fragments before transparent terminator.
  fade={'split':'teal0','transform':'violet0','rally':'brass0'}[name]
  for y in range(64):
   for x in range(64):
    if im.getpixel((x,y))[3]:im.putpixel((x,y),P[fade] if (x+2*y)%9>=3 else (0,0,0,0))
 return im

def dummy():
 im=Image.new('RGBA',(64,64));poly(im,[(32,13),(39,20),(38,29),(43,47),(39,52),(25,52),(21,47),(26,29),(25,20)],'steel0');line(im,[(29,20),(35,20),(36,27),(28,27),(29,20)],'steel2');return im

def render_preview(name,f):
 # Explanatory neutral token, not production enemy art.
 im=Image.new('RGBA',(64,64),'#0c1525');token=dummy()
 if name=='death':
  cut=[0,13,21,29,38,46,53,64][f];ImageDraw.Draw(token).rectangle((0,0,63,cut),fill=(0,0,0,0))
 elif name=='flee':
  off=[0,2,7,14,23,34,44,64][f];shifted=Image.new('RGBA',(64,64));shifted.alpha_composite(token,(off,0));token=shifted
 elif name=='transform' and f in (2,3,4):
  # Preview-only masked old/new silhouette swap, no actual game sprite.
  if f==3:token=Image.new('RGBA',(64,64))
  else:token=token.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
 im.alpha_composite(token);im.alpha_composite(frame(name,f));return im

def build():
 for d in ['Art/fx','previews','qa']:(R/d).mkdir(exist_ok=True,parents=True)
 font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',12)
 sheet=Image.new('RGB',(120+8*132,10*150+35),'#0c1525');ds=ImageDraw.Draw(sheet)
 ds.text((8,10),'B7 ENEMY EVENTS | native 64 x 64 | 8 frames | 2x NEAREST',font=font,fill='#cfaa63')
 native=Image.new('RGB',(8*68+128,10*80+30),'#0c1525');dn=ImageDraw.Draw(native)
 dn.text((8,6),'B7 | 1x pixels | two night backgrounds | no smoothing',font=font,fill='#cfaa63')
 for row,(name,label,times) in enumerate(SPECS):
  frames=[frame(name,i) for i in range(8)]
  strip=Image.new('RGBA',(512,64))
  for i,fr in enumerate(frames):strip.paste(fr,(64*i,0))
  strip.save(R/f'Art/fx/enemy_{name}_8f.png')
  y=35+150*row;ds.text((7,y+40),name,font=font,fill='#bad2d9')
  dn.text((4,30+80*row+24),name,font=font,fill='#bad2d9')
  for f,fr in enumerate(frames):
   bg=Image.new('RGBA',(64,64),'#0c1525' if f%2==0 else '#17283f');bg.alpha_composite(fr)
   sheet.paste(bg.resize((128,128),Image.Resampling.NEAREST).convert('RGB'),(120+132*f,y))
   ds.text((120+132*f,y+130),f'{f}: {times[f]} ms',font=font,fill='#819eaf')
   native.paste(bg.convert('RGB'),(128+68*f,30+80*row))
  playback=[]
  for f in range(8):
   p=Image.new('RGB',(360,206),'#0c1525');pd=ImageDraw.Draw(p);pd.text((8,8),f'{name} | frame {f} | {times[f]} ms',font=font,fill='#cfaa63')
   bg=Image.new('RGBA',(64,64),'#17283f');bg.alpha_composite(frames[f]);p.paste(bg.resize((128,128),Image.Resampling.NEAREST),(8,40));p.paste(bg,(151,40));p.paste(render_preview(name,f),(235,40));pd.text((8,176),'2x effect     1x effect     neutral dummy',font=font,fill='#819eaf');playback.append(p)
  # Use one consistent preview-only pause rather than changing production timing.
  pause=playback[7].copy();ImageDraw.Draw(pause).text((230,145),'800 ms pause',font=font,fill='#819eaf')
  playback[0].save(R/f'previews/enemy_{name}_playback.png',save_all=True,append_images=playback[1:]+[pause],duration=times+[800],loop=0,disposal=0,blend=0)
 sheet.save(R/'previews/enemy_events_contactsheet.png');native.save(R/'previews/enemy_events_native.png')
 (R/'previews/playback_timing.json').write_text(json.dumps({n:{'production_ms':t,'pause_ms':800,'dummy_is_not_game_art':True} for n,l,t in SPECS},indent=2)+'\n')
if __name__=='__main__':build()
