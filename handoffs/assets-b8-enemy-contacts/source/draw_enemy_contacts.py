#!/usr/bin/env python3
"""Original B8 contact-only pixel geometry. Final native resolution, no resized artwork."""
from PIL import Image,ImageDraw,ImageFont,ImageColor
from pathlib import Path
import json,math,hashlib
R=Path(__file__).resolve().parents[1]
P={k:ImageColor.getrgb(v)+(255,) for k,v in json.loads((R/'palette.json').read_text()).items()}
P.update({'acid0':(48,77,58,255),'acid1':(92,135,76,255),'acid2':(164,199,109,255),'ice1':(87,143,177,255),'ice2':(157,206,220,255)})
SPECS=[('acid','酸',[30,35,40,45,50,55,60,35]),('poison','毒',[35,40,45,50,55,60,65,40]),('curse','呪い',[35,40,45,50,55,55,60,40]),('roar','吠え',[25,30,35,40,45,50,55,40]),('bell','鈴',[30,35,40,45,50,55,60,45]),('song','歌',[35,40,45,50,55,60,65,40]),('cold','冷気',[30,35,40,45,50,55,60,45]),('lightning','雷',[20,25,30,35,40,45,55,50])]
def line(im,pts,c,w=1):ImageDraw.Draw(im).line(pts,fill=P[c],width=w)
def poly(im,pts,c):ImageDraw.Draw(im).polygon(pts,fill=P[c])
def dot(im,x,y,c):
 if 0<=x<64 and 0<=y<64:im.putpixel((x,y),P[c])
def arc(im,x,y,rx,ry,a,b,c):
 n=max(5,round(abs(b-a)*max(rx,ry)*2));line(im,[(round(x+rx*math.cos(a+(b-a)*i/n)),round(y+ry*math.sin(a+(b-a)*i/n))) for i in range(n+1)],c)
def chip(im,x,y,r,c):poly(im,[(x,y-r),(x+r,y),(x,y+r),(x-r,y)],c)
def frame(name,f):
 im=Image.new('RGBA',(64,64))
 if f==7:return im
 bright=f in (0,1,2)
 if name=='acid':
  # Wet, connected crown ruptures then falls. Never a poison cloud.
  if f<6:
   spread=[0,3,6,8,9,7][f];y=32+f
   poly(im,[(29,y-5),(34,y-9),(38+spread,y-12),(37+spread,y-4),(44+spread,y-1),(40+spread,y+3),(45+spread,y+7),(35,y+4),(30,y+6),(27,y+1)],'acid0')
   poly(im,[(30,y-3),(34,y-6),(36+spread,y-7),(35+spread,y-1),(41+spread,y+1),(35,y+2),(30,y+3)],'acid1')
   line(im,[(30,y-2),(33,y-4),(36+spread,y-5)],'acid2')
   line(im,[(31,y+1),(35,y+2),(39+spread,y+2)],'teal2' if f<2 else 'acid1')
   if f>=4:
    for yy in range(64):
     for xx in range(64):
      if im.getpixel((xx,yy))[3] and ((xx+yy*2)%(7 if f==4 else 4)<(2 if f==4 else 2)):
       im.putpixel((xx,yy),(0,0,0,0))
      elif f==5 and im.getpixel((xx,yy))[3]:im.putpixel((xx,yy),P['acid0'])
  for j,(dx,dy) in enumerate([(12,-14),(21,-4),(18,10),(7,17)]):
   x=32+round(dx*(.45+.085*f));y=32+round(dy*(.4+.09*f))+round(f*f*.28)
   if f>j//2 and y<59:chip(im,x,y,max(1,3-f//2),'acid2' if bright else 'acid1' if f<5 else 'acid0')
  if f>=4:arc(im,39,51+f,5+f,2,0,math.pi,'acid1' if f==4 else 'acid0')
 elif name=='poison':
  # Hollow vapor curls rise. Separated beads avoid acid's liquid crown.
  for j in range(3):
   x=31+j*7-f//2;y=37-j*6-f*2;r=[3,5,6,6,5,4,2][f]
   arc(im,x,y,r,r+1,.25+j*.3,5.4+j*.15,'violet1' if f<4 else 'violet0')
   arc(im,x-1,y-1,max(1,r-2),max(1,r-1),3.1,5.8,'teal2' if f<3 else 'teal1' if f<5 else 'teal0')
  if f<4:
   line(im,[(28,38+f),(27,32),(29,29),(33,30),(34,34)],'violet2' if bright else 'violet1')
  for j in range(4):dot(im,23+j*7+(f%2),43-j*5-f*2,'teal1' if f<5 else 'violet0')
  if f<6:line(im,[(30,40-f),(28,35-f*2),(30,30-f*2),(33,26-f*2),(32,22-f*2)],'teal1' if f<3 else 'teal0')
 elif name=='curse':
  # Inward hooked facets, asymmetric broken seal. No skull/no status UI duplicate.
  r=[19,17,15,13,12,13,15][f]
  for j,a in enumerate([-.85,1.05,3.15]):
   x=round(33+r*math.cos(a));y=round(32+r*math.sin(a))
   tx=round(33+(r-7)*math.cos(a+.4));ty=round(32+(r-7)*math.sin(a+.4))
   line(im,[(x-round((4+j)*math.sin(a)),y+round((4+j)*math.cos(a))),(x,y),(tx,ty)],'violet2' if f<2 else 'violet1' if f<5 else 'violet0',2 if f<3 else 1)
   if f<4:dot(im,x,y,'brass2')
  if f<5:
   poly(im,[(31,26),(35,29),(37,34),(33,38),(29,34)],'violet0')
   line(im,[(32,27),(34,31),(32,35)],'violet2' if bright else 'violet1')
  if f>=4:
   for j in range(5):dot(im,24+j*4,22+((j*7+f*2)%22),'violet1' if f==4 else 'violet0')
 elif name=='roar':
  # Three open pressure blades point toward receiver, never a circular buff aura.
  for j in range(3):
   x=31+f*2+j*6;h=8+j*4+f
   if x<59 and f<6:
    line(im,[(x+3,32-h),(x,32-h+4),(x-3,32),(x,32+h-4),(x+3,32+h)],'steel3' if j==0 and f<2 else 'steel2' if f<3 else 'steel1' if f<5 else 'steel0',2 if f<2 else 1)
  if f<3:
   poly(im,[(29,29),(35,30),(39,32),(34,34),(29,35),(32,32)],'steel1')
   line(im,[(29,32),(34,32)],'brass2')
  if f>=4:
   for j in range(3):line(im,[(43+j*5,21+j*9),(45+j*5,22+j*9)],'steel0')
 elif name=='bell':
  # Two geometric resonant loops with a tiny strike, no literal bell at target.
  r=[5,8,11,14,17,20,23][f]
  for k in range(2):
   rr=r-k*6
   if rr<2:continue
   pts=[(round(33+rr*math.cos(i*math.tau/12)),round(32+rr*.82*math.sin(i*math.tau/12))) for i in range(13)]
   for j in range(12):
    if f<4 or j%3!=1:line(im,pts[j:j+2],'brass3' if k and bright else 'brass2' if f<3 else 'brass1' if f<5 else 'brass0')
  if f<3:
   chip(im,32,32,3-f,'brass3');line(im,[(32,24-f),(32,22-f)],'steel3')
  if f>=4:dot(im,32,12,'brass1' if f<6 else 'brass0');dot(im,51,33,'brass1' if f<6 else 'brass0')
 elif name=='song':
  # Two linked musical wave ribbons, unequal and phased; no UI musical note glyph.
  for j in range(2):
   pts=[]
   for x in range(25+f,53+f//2):
    xx=(x-25-f)/27;env=math.sin(math.pi*xx)
    y=32+(j*9-5)+round(math.sin(xx*math.tau+f*.34+j*.9)*(4+f*.55)*env)
    pts.append((x,y))
   line(im,pts,'violet2' if j==0 and f<3 else ('teal2' if j else 'violet1') if f<5 else ('teal1' if j else 'violet0'))
   if f==5:
    for px,py in pts:
     if px%8<3:im.putpixel((px,py),(0,0,0,0))
  if f<4:arc(im,31,32,4+f,8+f,2.3,4.0,'brass2' if f<2 else 'brass1')
  for j in range(3):dot(im,36+j*7+f//2,20+(j%2)*23-f//2,'teal1' if f<5 else 'teal0')
 elif name=='cold':
  # Faceted ice pierces outward, then chips separate. Never a snowflake icon.
  for j,(dx,dy) in enumerate([(22,-10),(14,-22),(20,9),(6,18)]):
   gain=[.28,.55,.82,1,1.08,1.14,1.18][f];x=32+round(dx*gain);y=32+round(dy*gain)
   root=(32+round(dx*max(0,(f-3)*.19)),32+round(dy*max(0,(f-3)*.19)))
   if f<6:
    poly(im,[root,(x-3,y+3),(x,y),(x+1,y+5)],'ice1' if f<4 else 'steel1')
    line(im,[root,(x,y)],'ice2' if bright else 'steel2' if f<5 else 'steel1')
   elif j<3:chip(im,x,y+2,1,'steel0')
  if f<3:poly(im,[(28,31),(31,27),(35,29),(36,33),(32,36)],'ice2')
  if f>=3:
   arc(im,38,43,8+f,3,0.2,2.8,'steel1' if f<5 else 'steel0')
   dot(im,44,18+f,'ice1' if f<5 else 'steel0')
  if f==5:
   for yy in range(64):
    for xx in range(64):
     if im.getpixel((xx,yy))[3] and (xx+yy)%7<3:im.putpixel((xx,yy),(0,0,0,0))
 elif name=='lightning':
  # One lightning impulse, no repeated full-white strobe, no gap-spanning beam.
  paths=[[(29,34),(35,26),(32,24),(42,12)],[(32,32),(41,33),(38,38),(52,42)],[(35,29),(46,24),(44,21),(55,18)]]
  for j,pts in enumerate(paths):
   if f<5:
    start=0 if f<3 else 1
    shifted=[(x+(f//3),y+(f//4)) for x,y in pts[start:]]
    line(im,shifted,'violet0' if f>=3 else 'ice1',3 if f<2 else 1)
    if f<3:line(im,shifted,'ice2' if f<2 else 'steel2')
   else:
    x,y=pts[-1];line(im,[(x-1,y),(x,y-2)],'steel1' if f==5 else 'steel0')
  if f<2:
   chip(im,32,32,3-f,'ice2');line(im,[(28,29),(31,31)],'brass2')
  if f>=2:dot(im,46,49+f,'brass1' if f<4 else 'brass0')
 if f==6:
  tail={'acid':'acid0','poison':'violet0','curse':'violet0','roar':'steel0','bell':'brass0','song':'teal0','cold':'steel0','lightning':'steel0'}[name]
  for y in range(64):
   for x in range(64):
    if im.getpixel((x,y))[3]:im.putpixel((x,y),P[tail] if (x+2*y)%5!=0 else (0,0,0,0))
 return im

def mesh_for(fr,idx):
 # Maximal horizontal occupied runs: silhouette-following indexed planar geometry.
 # Material carries palette and hard alpha, no huge transparent billboard.
 vertices=[];uv=[];indices=[]
 for y in range(64):
  x=0
  while x<64:
   if fr.getpixel((x,y))[3]==0:x+=1;continue
   start=x
   while x<64 and fr.getpixel((x,y))[3]>0:x+=1
   n=len(vertices)
   for px,py in [(start,y),(x,y),(x,y+1),(start,y+1)]:
    vertices.append([round((px-32)/64,6),round((32-py)/64,6),0.0]);uv.append([round((idx*64+px)/512,9),round(py/64,9)])
   indices.extend([n,n+1,n+2,n,n+2,n+3])
 return {'vertices':vertices,'uv':uv,'indices':indices}

def dummy():
 im=Image.new('RGBA',(64,64));poly(im,[(22,14),(28,19),(27,30),(31,49),(27,54),(14,54),(11,49),(16,30),(15,19)],'steel0');line(im,[(19,20),(24,20),(24,27),(18,27)],'steel2');return im
def build():
 for d in ['Art/fx','Geometry','previews','qa','godot']:(R/d).mkdir(exist_ok=True,parents=True)
 (R/'palette.json').write_text(json.dumps({k:'#%02x%02x%02x'%v[:3] for k,v in P.items()},indent=2)+'\n')
 font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',12)
 sheet=Image.new('RGB',(116+8*132,8*150+35),'#0c1525');ds=ImageDraw.Draw(sheet)
 ds.text((8,10),'B8 ENEMY CONTACTS | 64 x 64 native | source on right | 2x NEAREST',font=font,fill='#cfaa63')
 native=Image.new('RGB',(8*68+116,8*80+30),'#0c1525');dn=ImageDraw.Draw(native);dn.text((8,6),'B8 | native pixels | alternating night-glass backgrounds',font=font,fill='#cfaa63')
 for row,(name,label,times) in enumerate(SPECS):
  frames=[frame(name,i) for i in range(8)];strip=Image.new('RGBA',(512,64));meshes=[]
  for i,fr in enumerate(frames):strip.paste(fr,(64*i,0));meshes.append(mesh_for(fr,i))
  strip.save(R/f'Art/fx/enemy_{name}_contact_8f.png')
  data={'schema':'deckrogue.contact-mesh.v1','effect':name,'coordinate_system':'receiver-relative; x toward attacker; y up; z=0; units are receiver height','anchor_px_top_left':[32,32],'native_grid':64,'mesh_width_receiver_heights':1.0,'duration_ms':times,'frames':meshes}
  (R/f'Geometry/enemy_{name}_contact.json').write_text(json.dumps(data,separators=(',',':'))+'\n')
  y=35+150*row;ds.text((7,y+40),name,font=font,fill='#bad2d9');dn.text((4,30+80*row+24),name,font=font,fill='#bad2d9')
  playback=[]
  for f,fr in enumerate(frames):
   bg=Image.new('RGBA',(64,64),'#0c1525' if f%2==0 else '#17283f');bg.alpha_composite(fr)
   sheet.paste(bg.resize((128,128),Image.Resampling.NEAREST).convert('RGB'),(116+132*f,y));ds.text((116+132*f,y+130),f'{f}: {times[f]} ms',font=font,fill='#819eaf');native.paste(bg,(116+68*f,30+80*row))
   p=Image.new('RGB',(360,206),'#0c1525');pd=ImageDraw.Draw(p);pd.text((8,8),f'{name} | contact + frame {f} | {times[f]} ms',font=font,fill='#cfaa63');p.paste(bg.resize((128,128),Image.Resampling.NEAREST),(8,40));p.paste(bg,(151,40))
   comp=Image.new('RGBA',(64,64),'#17283f');comp.alpha_composite(dummy());comp.alpha_composite(fr);p.paste(comp,(235,40));pd.text((8,176),'2x effect     1x effect     neutral dummy',font=font,fill='#819eaf');playback.append(p)
  pause=playback[7].copy();ImageDraw.Draw(pause).text((230,145),'800 ms pause',font=font,fill='#819eaf')
  playback[0].save(R/f'previews/enemy_{name}_contact_playback.png',save_all=True,append_images=playback[1:]+[pause],duration=times+[800],loop=0,disposal=0,blend=0)
 sheet.save(R/'previews/enemy_contacts_contactsheet.png');native.save(R/'previews/enemy_contacts_native.png')
 (R/'previews/playback_timing.json').write_text(json.dumps({n:{'production_ms':t,'pause_ms':800,'dummy_is_not_game_art':True} for n,l,t in SPECS},indent=2)+'\n')
if __name__=='__main__':build()
