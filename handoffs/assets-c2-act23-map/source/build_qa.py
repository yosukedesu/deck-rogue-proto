#!/usr/bin/env python3
from pathlib import Path
from PIL import Image,ImageDraw
import json,math
R=Path(__file__).resolve().parents[1];Q=R/'qa';Q.mkdir(exist_ok=True)
M=json.loads((R/'manifest.json').read_text());BG='#101a2b';INK='#b5c7cf';BRASS='#b99a5c'
for act in (2,3):
 assets=[a for a in M['assets'] if a.get('act')==act]
 sheet=Image.new('RGBA',(640,580),BG);d=ImageDraw.Draw(sheet);d.text((12,8),f'ACT {act} | NATIVE PIXEL MAP PARTS | 12 NEW',fill=INK)
 for i,a in enumerate(assets):
  x=(i%4)*160;y=34+(i//4)*180;im=Image.open(R/a['file'])
  if a['kind']=='tile':
   for yy in range(2):
    for xx in range(2):sheet.alpha_composite(im,(x+16+xx*64,y+yy*64))
  else:sheet.alpha_composite(im,(x+(160-im.width)//2,y+128-im.height))
  d.text((x+6,y+134),a['id'].removeprefix(f'act{act}_')[:24],fill=BRASS)
  d.text((x+6,y+148),f'{im.width}x{im.height} / '+('2x2 repeat' if a['kind']=='tile' else 'RGBA'),fill=INK)
 d.text((12,565),'Artifact preview only. Game import / placement not tested.',fill=INK)
 sheet.convert('RGB').save(Q/f'act{act}_contactsheet_native.png');sheet.resize((1280,1160),Image.Resampling.NEAREST).convert('RGB').save(Q/f'act{act}_contactsheet_2x.png')
 # Readability on a light inspection plate; does not represent the game theme.
 light=Image.new('RGBA',(640,580),'#a2b7bd');ld=ImageDraw.Draw(light)
 for i,a in enumerate(assets):
  if a['kind']=='tile':continue
  x=(i%4)*160;y=34+(i//4)*180;im=Image.open(R/a['file']);light.alpha_composite(im,(x+(160-im.width)//2,y+128-im.height));ld.text((x+5,y+140),a['id'][:23],fill='#263b48')
 light.convert('RGB').save(Q/f'act{act}_light_background.png')
 # Alternating A/B tiles ensure boundary inspection is not self-repeat only.
 repeat=Image.new('RGBA',(384,256),BG)
 for y in range(4):
  for x in range(6):repeat.alpha_composite(Image.open(R/'png'/f'act{act}_ground_{"a" if (x+2*y+y//2)%3 else "b"}.png'),(x*64,y*64))
 repeat.convert('RGB').save(Q/f'act{act}_mixed_ground.png')
 # Assembly study: dynamic route concepts only; not a Unity screenshot.
 w,h=680,940;im=Image.new('RGBA',(w,h),BG)
 for yy in range(0,h,64):
  for xx in range(0,w,64):im.alpha_composite(Image.open(R/'png'/f'act{act}_ground_{"a" if (xx//64+yy//64)%3 else "b"}.png'),(xx,yy))
 dr=ImageDraw.Draw(im);nodes=[(340,750,'battle'),(170,610,'shop'),(480,610,'unknown'),(250,470,'campfire'),(440,350,'elite'),(340,220,'boss')]
 edges=[(0,1),(0,2),(1,3),(2,3),(3,4),(4,5)]
 for aa,bb in edges:
  p=nodes[aa][:2];q=nodes[bb][:2];dr.line([p,q],fill='#192533',width=19);dr.line([p,q],fill='#59606a',width=13);dr.line([p,q],fill='#35404b',width=11)
  length=math.dist(p,q)
  for t in range(0,int(length),13):
   f=t/length;x=round(p[0]+(q[0]-p[0])*f);y=round(p[1]+(q[1]-p[1])*f);dr.rectangle((x-1,y-1,x+1,y+1),fill='#b99a5c' if aa<2 else '#427981')
 def paste_at(name,x,y):
  a=next(a for a in M['assets'] if a['id']==name);sp=Image.open(R/a['file']);anchor=a['anchorPixels'];im.alpha_composite(sp,(x-anchor[0],y-anchor[1]))
 props=[a['id'] for a in assets if a['kind']=='prop']
 for name,pt in zip(props,[(90,330),(580,440),(100,650),(565,765),(95,840)]):paste_at(name,*pt)
 paste_at(f'act{act}_start',340,890)
 dest=next(a['id'] for a in assets if a.get('encounterId'));paste_at(dest,340,155)
 for x,y,name in nodes:
  dr.ellipse((x-32,y-32,x+32,y+32),fill='#172434',outline='#89704b',width=1);paste_at('node_'+name,x,y)
 dr.rectangle((0,0,w,58),fill='#101a2b');dr.text((16,12),f'ACT {act} | ASSEMBLY STUDY, NOT GAME CAPTURE',fill=INK);dr.text((16,30),'Route lines and node rings are preview-only. No characters.',fill=INK)
 im.convert('RGB').save(Q/f'act{act}_assembly_study.png')
nodes=[a for a in M['assets'] if a.get('reused')];sheet=Image.new('RGBA',(640,106),BG);d=ImageDraw.Draw(sheet)
for i,a in enumerate(nodes):
 im=Image.open(R/a['file']);sheet.alpha_composite(im,(i*80+16,12));d.text((i*80+3,68),a['id'][5:],fill=INK)
d.text((12,90),'8 COMMON A3 NODES, BYTE-IDENTICAL, NOT NEW DRAWINGS',fill=BRASS);sheet.convert('RGB').save(Q/'common_nodes_native.png')
print('QA sheets prepared')
