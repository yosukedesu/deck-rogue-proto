#!/usr/bin/env python3
"""B2 original native 64x96 integer-grid pixel VFX. Pillow. No generated/source image.
All production pixels are authored on the final grid. Nearest transforms only
animate an ORIGINAL generic proxy in previews; proxy never enters production.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json, math, hashlib
R=Path(__file__).resolve().parents[1]
P=json.loads((R/'palette.json').read_text())
W,H=64,96
KEYS=['exhaust','discard','draw','impulse_expire','kindle']
JA=['消滅','捨てる','ドロー','衝動の失効','くべる']
LABELS=['EXHAUST / burn away','DISCARD / flutter out','DRAW / light in','IMPULSE / time runs out','KINDLE / feed the fire']
TIMES={'exhaust':[65,65,75,80,85,85,95,70], 'discard':[55,60,65,65,70,75,80,70], 'draw':[45,50,55,65,75,75,85,70], 'impulse_expire':[70,70,75,80,85,85,95,70], 'kindle':[55,55,65,75,85,85,90,70]}
# px offset, scale x, scale y, clockwise degrees, alpha. Applied to original card,
# never to the overlay, which stays at the start/end anchor. No game code changes.
MOTION={
'exhaust':[[0,0,1,1,0,1]]*7+[[0,0,1,1,0,0]],
'discard':[[0,0,1,1,0,1],[2,-2,.92,.98,4,1],[5,-3,.68,.94,10,1],[9,0,.42,.88,15,.95],[13,5,.55,.82,22,.8],[17,12,.34,.74,30,.5],[21,20,.22,.64,38,.2],[26,28,.16,.5,45,0]],
'draw':[[-17,21,.68,.78,-12,.15],[-12,15,.76,.86,-9,.4],[-7,9,.85,.93,-6,.7],[-3,4,.93,.97,-3,.9],[0,0,1,1,0,1],[0,-1,1,1,0,1],[0,0,1,1,0,1],[0,0,1,1,0,1]],
'impulse_expire':[[0,0,1,1,0,1],[0,0,1,1,0,.94],[0,0,1,1,0,.85],[0,1,.98,.98,0,.7],[2,4,.95,.95,3,.55],[5,10,.88,.9,6,.35],[9,18,.78,.8,9,.15],[14,28,.65,.7,12,0]],
'kindle':[[0,0,1,1,0,1],[1,1,.97,.96,3,1],[3,5,.85,.84,10,1],[6,11,.65,.64,18,.9],[9,19,.4,.4,26,.6],[11,24,.18,.18,34,.2],[12,27,.1,.1,40,0],[12,27,.1,.1,40,0]],
}
def c(s): return P.get(s,s)
def new():
 global im,d
 im=Image.new('RGBA',(W,H));d=ImageDraw.Draw(im)
def line(p,col,w=1):d.line(p,fill=c(col),width=w)
def poly(p,col):d.polygon(p,fill=c(col))
def rect(b,col):d.rectangle(b,fill=c(col))
def dot(x,y,col):d.point((x,y),fill=c(col))
def star(x,y,col='brass2',r=2):
 line([(x-r,y),(x+r,y)],col);line([(x,y-r),(x,y+r)],col);dot(x,y,'brass3' if col.startswith('brass') else 'steel3')
def flame(x,y,h,w=3,bright=True):
 poly([(x-w,y),(x-w,y-3),(x-2,y-h//2),(x,y-h),(x+2,y-h+4),(x+1,y-5),(x+w,y-7),(x+w+1,y-2),(x+2,y+2),(x-2,y+2)],'ember0')
 poly([(x-2,y),(x,y-h+4),(x+1,y-5),(x+w-1,y-3),(x+1,y+1)],'ember1')
 if bright:line([(x,y),(x,y-3),(x+1,y-5)],'ember2')
def curve(points,col,w=1):line(points,col,w)
def exhaust(f):
 if f<=5:
  ys=[77,70,60,48,36,24];yy=ys[f]
  # Burn front crawls UP the card; active tongues sit at the surviving edge.
  xs=([17,44] if f==0 else [15,23,32,41,49])
  for i,x in enumerate(xs):flame(x,yy+2,8+(i*3+f*2)%8,3,f in (1,2,3,4))
  if f>0:
   line([(12,yy),(17,yy-2),(23,yy),(28,yy-3),(34,yy-1),(40,yy+1),(46,yy-2),(51,yy)],'ember1')
  for i,(xx,dy) in enumerate([(17,13),(27,20),(42,16),(48,26)]):
   x=xx+((f+i)%3-1)*2;y=max(3,yy-dy-f)
   if f>0 or i%2==0:line([(x,y),(x,y+2)],'ember2' if (i+f)%2 else 'ember1')
 else:
  for i,(x,y) in enumerate([(13,23),(22,12),(36,6),(45,16),(51,9),(31,26)]):
   dot(x,y,'ember1' if i%2 else 'steel1');
   if i%2:dot(x+1,y+1,'ember0')
def discard(f):
 # Folded, cool-coloured flecks and an arcing route. No burning/smoke.
 paths=[[(8,39),(7,47),(10,55),(17,61),(26,64)],[(7,46),(9,55),(16,63),(25,68),(35,70)],[(9,53),(15,63),(24,70),(36,74),(47,75)],[(16,64),(25,72),(36,78),(48,81)],[(26,74),(36,80),(47,85),(56,87)],[(38,82),(46,87),(55,90)],[(48,89),(55,92)]]
 p=paths[f];line(p,'steel0',2);line(p[1:],'steel2' if f<4 else 'steel1')
 if f in (1,2,3,4):
  x=[13,19,29,41][f-1];y=[55,64,72,79][f-1]
  poly([(x,y),(x+6,y-2),(x+8,y+1),(x+3,y+4)],'steel1');line([(x,y),(x+3,y+1),(x+6,y-2)],'steel3');line([(x+3,y+1),(x+3,y+4)],'brass1')
 if f>=2:
  x=[10,16,26,38,48][f-2];y=[69,76,82,87,90][f-2]
  line([(x,y),(x+3,y+1),(x+5,y-1)],'brass2' if f<5 else 'brass1')
 if f<4:line([(7+f*2,26+f*3),(5+f*2,32+f*3),(6+f*2,37+f*3)],'steel1')
def draw_in(f):
 # Cool incoming streak, opposite direction to discard. A punctual corner lock.
 paths=[[(3,89),(8,82),(14,75)],[(4,88),(13,76),(23,64)],[(8,82),(19,67),(30,51)],[(16,72),(28,55),(37,41)],[(26,59),(36,44),(44,30)],[(40,37),(47,26),(52,18)],[(48,24),(52,18)]]
 line(paths[f],'teal0',3);line(paths[f],'teal2' if f in (2,3,4) else 'teal1')
 if f<5:
  line([(x+5,y+3) for x,y in paths[f]],'steel1');line([(x-2,y-6) for x,y in paths[f]],'steel0')
 if f in (3,4,5):
  s=[4,6,4][f-3];star(50,18,'teal1',s);star(12,77,'brass2',2)
 if f in (4,5,6):
  col='brass2' if f==4 else 'brass1';n=[7,5,3][f-4]
  line([(11,19+n),(11,17),(11+n,17)],col);line([(52-n,17),(52,17),(52,19+n)],col)
  line([(11,76-n),(11,78),(11+n,78)],col);line([(52-n,78),(52,78),(52,76-n)],col)
def hourglass(x,y,f):
 col='brass2' if f<3 else 'brass1'
 if f<4:
  line([(x-5,y-8),(x+5,y-8)],col);line([(x-5,y+8),(x+5,y+8)],col)
  line([(x-4,y-7),(x-4,y-4),(x,y),(x-4,y+4),(x-4,y+7)],'brass1');line([(x+4,y-7),(x+4,y-4),(x,y),(x+4,y+4),(x+4,y+7)],'brass1')
  if f<3:
   top=[4,2,0][f]
   if top:poly([(x-3,y-5),(x+3,y-5),(x,y-5+top)],'brass3')
   line([(x,y),(x,y+3)],'brass2')
   poly([(x-3,y+6),(x+3,y+6),(x,y+3-f)],'brass2')
  else:
   line([(x-3,y+6),(x+3,y+6)],'brass1');line([(x-7,y-4),(x+7,y+5)],'steel2')
 else:
  off=f-3
  line([(x-5-off,y-8+off),(x-1-off,y-8+off)],'brass0');line([(x+1+off,y+8+off),(x+5+off,y+8+off)],'steel1')
  if f<6:line([(x+3,y+4),(x+4,y+5)],'brass1')
def impulse_expire(f):
 hourglass(47,18,f)
 # Small amber time seal expires; slate routes downward to DISCARD. No flames.
 if f in (1,2,3):
  y=[73,67,61][f-1];line([(15,y),(25,y)],'steel1');line([(33,y),(48,y)],'steel0')
 if f>=3:
  yy=[72,77,83,89][f-3]
  line([(21,yy-2),(29,yy),(36,yy)],'steel1');line([(38,yy+2),(45,yy+3),(50,yy+5)],'brass0')
 if f==0:dot(47,29,'brass3')
def kindle(f):
 # Recipient fire is BELOW the card. Card moves INTO it; unlike upward exhaust.
 peaks=[5,7,11,17,24,17,7]
 for i,(x,y) in enumerate([(34,84),(42,86),(49,84)]):
  flame(x,y,peaks[f]+(i%2)*5,3,True)
 if f<5:
  for i,(x,y) in enumerate([(14,35),(18,45),(25,53)]):
   yy=y+f*5;xx=x+f*2
   line([(xx,yy),(xx+2,yy+4)],'brass1' if f<2 else 'ember1')
 if f in (3,4,5):
  # Outward sparks only at the fuel landing moment.
  k=f-3
  line([(27-k*3,72-k*4),(25-k*3,68-k*4)],'ember2' if f<5 else 'ember1')
  line([(52+k*2,71-k*3),(54+k*2,67-k*3)],'ember2' if f<5 else 'ember1')
 if f==6:
  dot(35,70,'ember1');dot(48,73,'ember2')
FUNCS=[exhaust,discard,draw_in,impulse_expire,kindle]
def burn_mask(f):
 mask=Image.new('RGBA',(W,H));md=ImageDraw.Draw(mask)
 if f==7 or f==6:return mask
 cutoff=[78,70,60,48,36,24][f]
 for x in range(12,52):
  yy=cutoff if f==0 else cutoff+((x//3+f)%5-2)
  if yy>18:md.line([(x,18),(x,min(77,yy-1))],fill=(255,255,255,255))
 return mask

def proxy():
 a=Image.new('RGBA',(40,60));q=ImageDraw.Draw(a)
 q.rectangle((0,0,39,59),fill=P['deep'],outline=P['brass0'])
 q.line([(2,9),(2,2),(10,2)],fill=P['brass2']);q.line([(29,2),(37,2),(37,9)],fill=P['brass2'])
 q.line([(2,50),(2,57),(10,57)],fill=P['brass1']);q.line([(29,57),(37,57),(37,50)],fill=P['brass1'])
 q.rectangle((5,8,34,43),fill=P['edge'])
 q.polygon([(20,14),(29,25),(20,37),(11,25)],fill=P['steel0'],outline=P['steel2'])
 q.line([(20,18),(24,25),(20,32),(16,25),(20,18)],fill=P['teal1'])
 q.line([(9,49),(30,49)],fill=P['steel1']);q.line([(13,52),(26,52)],fill=P['steel0'])
 return a
CARD=proxy()
def composite(key,f,overlays,base='#0c1525',show_card=True):
 bg=Image.new('RGBA',(W,H),base)
 if show_card:
  dx,dy,sx,sy,ang,alpha=MOTION[key][f]
  if alpha>0:
   card=CARD.copy()
   if key=='exhaust':
    card.putalpha(burn_mask(f).getchannel('A').crop((12,18,52,78)))
   else:
    card=card.resize((max(1,round(40*sx)),max(1,round(60*sy))),Image.Resampling.NEAREST)
    card=card.rotate(-ang,resample=Image.Resampling.NEAREST,expand=True)
    card.putalpha(card.getchannel('A').point(lambda a:round(a*alpha)))
   bg.alpha_composite(card,(round(32+dx-card.width/2),round(48+dy-card.height/2)))
 bg.alpha_composite(overlays[key][f]);return bg

def generate():
 (R/'Art/fx').mkdir(parents=True,exist_ok=True);(R/'previews').mkdir(exist_ok=True)
 out={}
 for key,fn in zip(KEYS,FUNCS):
  frames=[]
  for f in range(8):
   new()
   if f<7:fn(f)
   frames.append(im)
  atlas=Image.new('RGBA',(512,96))
  for i,a in enumerate(frames):atlas.paste(a,(i*64,0))
  atlas.save(R/f'Art/fx/card_{key}_8f.png',optimize=False);out[key]=frames
 mask=Image.new('RGBA',(512,96))
 for f in range(8):mask.paste(burn_mask(f),(f*64,0))
 mask.save(R/'Art/fx/card_exhaust_keep_mask_8f.png',optimize=False)
 # Eight frames per row, with a separate baseline/landing proxy column.
 font='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
 title=ImageFont.truetype(font,19);label=ImageFont.truetype(font,13);tiny=ImageFont.truetype(font,11)
 sheet=Image.new('RGB',(1232,1130),'#0c1525');sd=ImageDraw.Draw(sheet)
 sd.text((22,15),'B2  /  CARD FLOW  /  NATIVE 64 x 96  /  8 FRAMES',font=title,fill=P['steel3'])
 sd.text((22,44),'2x nearest preview. Generic dark-glass card is preview-only. Atlases contain transparent VFX only.',font=label,fill=P['steel2'])
 sd.text((22,64),'Frame 7 clears VFX. Exhaust uses supplied alpha keep-mask. Actual game integration has not been tested.',font=tiny,fill=P['brass2'])
 for row,key in enumerate(KEYS):
  y=92+row*205
  sd.text((14,y+7),LABELS[row],font=label,fill=P['brass2'])
  sd.text((14,y+28),str(sum(TIMES[key]))+' ms / one-shot',font=tiny,fill=P['steel2'])
  sd.text((14,y+48),'TO: '+{'exhaust':'EXHAUST','discard':'DISCARD','draw':'HAND','impulse_expire':'DISCARD','kindle':'EXHAUST'}[key],font=tiny,fill=P['steel2'])
  for f in range(8):
   x=190+f*129;frame=composite(key,f,out).resize((128,192),Image.Resampling.NEAREST)
   sheet.paste(frame,(x,y));sd.text((x+5,y+176),f'{f} / {TIMES[key][f]}ms',font=tiny,fill=P['steel3'])
  sd.line((14,y+198,1220,y+198),fill=P['edge'])
 sheet.save(R/'previews/card_flow_contactsheet.png')
 # Native actual-size frames on two backgrounds for transparency/contrast QA.
 native=Image.new('RGB',(8*64+176,5*108+48),'#0c1525');nd=ImageDraw.Draw(native)
 nd.text((10,9),'1x native overlay / alternate night-glass backgrounds',font=label,fill=P['steel3'])
 for row,key in enumerate(KEYS):
  y=36+row*108;nd.text((8,y+8),key,font=tiny,fill=P['brass2'])
  for f in range(8):native.paste(composite(key,f,out,'#17283f' if f%2 else '#0c1525',False),(176+64*f,y))
 native.save(R/'previews/card_flow_native.png')
 # Sequential exact-timing review: avoids unsupported 5 ms interleaved APNG
 # frames that many viewers clamp. Each effect keeps its own >=45 ms duration.
 frames=[];durations=[];sequence=[]
 for active,key_active in enumerate(KEYS):
  for stage in ['ready']+list(range(8))+['hold']:
   a=Image.new('RGB',(772,284),'#0c1525');q=ImageDraw.Draw(a)
   q.text((14,11),'B2 / 5 CARD FLOWS / EXACT TIMING / '+key_active.upper(),font=label,fill=P['steel3'])
   for i,key in enumerate(KEYS):
    x=13+i*153;y=65
    q.text((x,39),key,font=tiny,fill=P['brass2'] if i==active else P['steel1'])
    baseline=(i>active or (i==active and stage=='ready'))
    if baseline:
     cell=Image.new('RGBA',(64,96),'#0c1525')
     if key!='draw':cell.alpha_composite(CARD,(12,18))
    else:
     f=7 if i<active or stage=='hold' else stage
     cell=composite(key,f,out)
    a.paste(cell.resize((128,192),Image.Resampling.NEAREST),(x,y))
    status=('DONE' if i<active else 'NEXT') if i!=active else (str(stage).upper() if isinstance(stage,str) else f'FRAME {stage} / {TIMES[key][stage]} ms')
    q.text((x,262),status,font=tiny,fill=P['steel2'])
   dt=350 if stage=='ready' else 700 if stage=='hold' else TIMES[key_active][stage]
   frames.append(a);durations.append(dt);sequence.append({'effect':key_active,'stage':stage,'duration_ms':dt})
 frames[0].save(R/'previews/card_flow_playback.png',save_all=True,append_images=frames[1:],duration=durations,loop=0,disposal=0,blend=0,optimize=False)
 (R/'previews/playback_timing.json').write_text(json.dumps({'preview_only':True,'format':'APNG','size':[772,284],'durations_ms':durations,'sequence':sequence,'frame_count':len(frames),'loop':0},indent=2)+'\n')
 print('Wrote 5 native atlases + 1 alpha keep-mask; 3 preview PNGs')
 return out
if __name__=='__main__':generate()
