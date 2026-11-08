#!/usr/bin/env python3
"""Original C2 act3 native pixel drawings. Integer geometry, no source resampling.
Run: python draw_act3.py [output-directory]. Requires Pillow.
"""
from pathlib import Path
from PIL import Image, ImageDraw
import sys, json, hashlib
P={'void':'#101a2b','edge':'#1b2940','soil':'#263b48','earth':'#354b51','s0':'#273548','s1':'#3d4d64','s2':'#576981','s3':'#7a8da0','s4':'#a2b7bd','b0':'#564c3a','b1':'#89704b','b2':'#b99a5c','b3':'#ddc681','t0':'#284e59','t1':'#427981','t2':'#70a6a5','t3':'#add2c9','p0':'#332a46','p1':'#52425f','p2':'#775e7d','r0':'#563342','r1':'#884858','r2':'#b7767c','f0':'#693f39','f1':'#a3583e','f2':'#ce8753','f3':'#e2b66d'}
class C:
 def __init__(self,w=96,h=96): self.im=Image.new('RGBA',(w,h));self.d=ImageDraw.Draw(self.im);self.w=w;self.h=h
 def p(self,xy,c):self.d.polygon(xy,fill=P.get(c,c))
 def l(self,xy,c,w=1):self.d.line(xy,fill=P.get(c,c),width=w)
 def r(self,xy,c):self.d.rectangle(xy,fill=P.get(c,c))
 def e(self,xy,c):self.d.ellipse(xy,fill=P.get(c,c))
 def save(self,o,n):self.im.save(o/(n+'.png'))
def base(a):
 w,h=a.w,a.h;y=h-5;x=w//2
 a.p([(5,y-13),(11,y-21),(x-16,y-26),(x+17,y-25),(w-14,y-19),(w-5,y-10),(w-10,y-5),(x+18,y-1),(x,y),(x-21,y-2),(13,y-6)],'void')
 a.p([(7,y-14),(14,y-20),(x-15,y-24),(x+15,y-23),(w-16,y-18),(w-8,y-11),(w-15,y-7),(x+16,y-4),(x-9,y-3),(16,y-8)],'soil')
 a.p([(12,y-16),(x-13,y-21),(x+11,y-20),(w-18,y-15),(w-13,y-11),(x+12,y-8),(x-11,y-7),(18,y-12)],'earth')
 a.l([(14,y-7),(x-18,y-3),(x,y-1),(x+18,y-3)],'s1');a.l([(x+22,y-4),(w-12,y-7)],'s0')
 for xx,yy in [(16,y-10),(23,y-15),(w-18,y-11),(x-7,y-6)]:
  a.p([(xx,yy),(xx+5,yy-1),(xx+8,yy+1),(xx+2,yy+2)],'s1');a.l([(xx,yy),(xx+4,yy-1)],'s2')
def block(a,x,y,w,h,d=5,t='s2',f='s1',s='s0'):
 # Ground boundary at y, x is front left. Upper-left key.
 a.p([(x,y-h),(x+d,y-h-d),(x+w+d,y-h-d),(x+w,y-h)],t)
 a.p([(x,y-h),(x+w,y-h),(x+w,y),(x,y)],f)
 a.p([(x+w,y-h),(x+w+d,y-h-d),(x+w+d,y-d),(x+w,y)],s)
 a.l([(x,y-h),(x+w-1,y-h)],'s3');a.l([(x,y-h+1),(x,y-1)],'s2')
 if w>13:a.l([(x+3,y-2),(x+w-4,y-2)],s)
def stone(a,x,y,w=10,h=7):block(a,x,y,w,h,3)
def col(a,x,y,height=38,width=12):
 block(a,x-3,y,width+6,4,4)
 for i,yy in enumerate(range(y-5,y-height,-8)):
  block(a,x,yy,width,min(7,yy-(y-height)),3, 's2' if i%3 else 's3','s1','s0')
  if i%2:a.l([(x+3,yy-5),(x+5,yy-3),(x+4,yy-1)],'s0')
 block(a,x-3,y-height,width+6,4,4)
def brass(a,x,y):a.r((x,y,x+2,y+2),'b0');a.r((x,y,x+1,y),'b2')
def steps(a,x,y,w,n=5):
 for i in range(n):
  ww=w-i*5;xx=x+i*2;yy=y-i*4
  block(a,xx,yy,ww,3,4,'s2','s0','edge')
def arch(a,x,y,w,h,broken=False):
 # Ring of stone polygons; interior stays transparent.
 col(a,x,y,h-12,11);col(a,x+w-11,y,h-12,11)
 yy=y-h
 pts=[([(x-2,yy+10),(x+1,yy+2),(x+10,yy-2),(x+15,yy+4),(x+9,yy+10)],'s2'), ([(x+11,yy-2),(x+21,yy-6),(x+26,yy-5),(x+25,yy+3),(x+16,yy+4)],'s3'), ([(x+27,yy-5),(x+w-17,yy-5),(x+w-12,yy+2),(x+27,yy+3)],'s2'), ([(x+w-16,yy-5),(x+w-5,yy),(x+w+3,yy+8),(x+w-8,yy+12),(x+w-13,yy+3)],'s1')]
 for i,(p,c) in enumerate(pts):
  if broken and i==2:continue
  a.p(p,'edge');a.p([(xx,yy+1) for xx,yy in p],c);a.l(p[:2],'s4' if i==1 else 's3')
 if broken:a.l([(x+23,yy-4),(x+24,yy),(x+21,yy+3)],'s0')
def ruin_tower():
 a=C(96,128);base(a)
 # Octagonal outer walls, broken crown and a dark central chamber.
 a.p([(25,104),(22,40),(26,30),(31,33),(36,25),(39,30),(49,25),(55,31),(61,28),(71,37),(72,96),(59,108),(40,112)],'edge')
 a.p([(25,99),(25,42),(36,34),(54,32),(65,39),(66,96),(54,105),(39,108)],'s1')
 a.p([(25,42),(36,34),(37,99),(39,108),(25,100)],'s2')
 a.p([(54,33),(65,39),(66,96),(54,105)],'s0')
 for y in [47,59,72,85,98]:
  a.l([(26,y),(36,y-5),(54,y-6),(65,y)],'edge')
  a.l([(27,y+1),(35,y-3)],'s3');a.l([(38,y-4),(51,y-5)],'s2')
 for x,y in [(29,52),(43,44),(49,66),(40,79),(58,92)]:a.l([(x,y-5),(x,y+5)],'s0')
 a.p([(26,38),(30,34),(33,37),(38,29),(42,34),(49,29),(54,34),(60,32),(67,39),(62,45),(51,39),(38,40),(29,46)],'s3')
 a.p([(34,40),(41,36),(51,35),(59,40),(53,45),(40,46)],'void')
 a.l([(34,39),(41,35),(50,34)],'s4')
 # Narrow broken window: not a face.
 a.p([(42,55),(47,51),(52,53),(52,68),(42,69)],'void');a.l([(43,56),(47,53),(50,54)],'s3');a.r((47,55,48,66),'t0')
 a.p([(41,96),(41,82),(47,77),(54,79),(54,99),(49,103)],'void');a.l([(40,95),(40,82),(46,77)],'s2')
 steps(a,34,111,33,3)
 stone(a,14,111,12,7);stone(a,65,110,16,9);stone(a,73,116,9,5)
 return a
def plaza():
 a=C();base(a)
 # Broken concentric pavement, a dry fountain bowl, no character statue.
 a.p([(16,65),(31,55),(62,55),(81,65),(76,77),(58,85),(31,82),(16,73)],'s0')
 a.p([(19,65),(33,57),(61,57),(77,65),(73,75),(57,81),(32,79),(19,72)],'s2')
 a.p([(25,65),(35,60),(59,60),(70,65),(67,73),(56,77),(35,75),(26,71)],'s1')
 for p in [[(20,65),(29,66)],[(35,57),(37,63)],[(59,58),(56,63)],[(75,66),(65,68)],[(57,80),(55,74)],[(31,78),(36,72)]]:a.l(p,'s0')
 a.e((30,48,64,68),'edge');a.e((31,45,64,62),'s2');a.e((35,48,60,58),'void');a.l([(35,49),(44,46),(55,47)],'s3')
 a.p([(36,53),(46,50),(57,53),(53,56),(42,56)],'t0')
 block(a,43,53,8,12,3,'s3','s1','s0');a.p([(42,40),(48,36),(54,40),(49,44)],'s3')
 a.l([(43,40),(48,37)],'s4');a.l([(48,44),(48,49)],'t1')
 stone(a,13,76,10,5);stone(a,69,81,12,5)
 return a
def broken_arch():
 a=C();base(a);steps(a,21,82,51,3);arch(a,25,74,42,43,True)
 stone(a,12,81,14,9);stone(a,64,82,12,6);stone(a,57,86,8,4)
 # Fine ancient brass inlay, worn not a UI outline.
 a.l([(30,62),(30,47)],'b1');a.l([(58,60),(58,50)],'b0')
 return a
def aqueduct():
 a=C(128,64);base(a)
 # Three open arches supporting a water channel.
 for x in [15,48,81]:
  block(a,x,47,8,17,4);block(a,x+22,47,8,17,4)
  a.p([(x+6,31),(x+10,24),(x+22,24),(x+26,31),(x+21,33),(x+19,29),(x+13,29),(x+11,34)],'s1')
  a.l([(x+8,29),(x+11,25),(x+20,25)],'s3')
 a.p([(12,22),(21,15),(111,15),(115,20),(108,28),(12,28)],'s0')
 a.p([(14,21),(23,16),(109,16),(105,21)],'s2')
 a.p([(20,21),(26,18),(105,18),(101,22)],'t0');a.l([(27,19),(62,19),(65,20),(97,20)],'t1')
 a.l([(14,23),(104,23)],'s3')
 for x in [32,56,79,101]:a.l([(x,24),(x,28)],'edge')
 stone(a,99,53,13,5);stone(a,15,52,9,4)
 return a
def rubble():
 a=C(96,64);base(a)
 # Fallen fluted column resting diagonally among masonry.
 a.p([(23,34),(49,24),(70,29),(73,39),(48,49),(26,43)],'edge')
 a.p([(25,34),(49,27),(67,30),(47,38)],'s3')
 a.p([(25,35),(47,39),(68,31),(70,38),(48,46),(27,41)],'s1')
 for d in [0,7,14,21]:
  a.l([(30+d,34-d//4),(32+d,41-d//4)],'s0')
 a.l([(30,35),(47,40),(64,34)],'s2')
 stone(a,13,43,12,11);stone(a,66,46,14,8);stone(a,49,54,12,6);stone(a,22,51,9,4)
 return a
def start():
 a=C();base(a)
 steps(a,24,85,49,7)
 # A low pair of portal remnants flanks the descending stair.
 col(a,17,75,39,12);col(a,67,75,32,11)
 a.p([(13,31),(18,24),(29,24),(34,29),(28,34),(13,34)],'s1');a.l([(14,30),(19,25),(28,25)],'s3')
 a.p([(64,40),(66,34),(72,36),(77,32),(83,38),(81,42)],'s2')
 a.p([(35,45),(39,40),(60,41),(65,47),(60,54),(38,53)],'void')
 for y in range(47,58,3):a.l([(39,y),(60,y)],'s0')
 a.l([(21,43),(21,61)],'b1');a.l([(70,49),(70,59)],'b0')
 # Two small cold vein lanterns, no extra glow halo.
 for x,y in [(19,61),(73,66)]:
  block(a,x,y,5,8,2,'b1','b0','edge');a.r((x+1,y-6,x+3,y-3),'t2');a.r((x+1,y-6,x+2,y-5),'t3')
 stone(a,14,82,9,5);stone(a,72,85,10,5)
 return a
def warden():
 a=C();base(a);steps(a,26,85,44,3)
 # Heavy gatehouse, bell-tower crown; closed door distinct from open start.
 col(a,17,76,41,14);col(a,64,76,41,14)
 block(a,18,36,59,9,4,'s3','s1','s0')
 a.p([(33,74),(33,44),(41,38),(56,38),(63,44),(63,74)],'void')
 a.p([(35,73),(35,45),(42,40),(47,40),(47,73)],'s0');a.p([(49,40),(56,40),(61,45),(61,73),(49,73)],'edge')
 for x in [38,44,53,59]:a.l([(x,47),(x,71)],'b0')
 a.r((36,54,60,56),'b1');a.r((36,65,60,66),'b0');brass(a,46,58)
 # Raised bell canopy.
 block(a,35,28,27,4,3);a.r((38,14,40,26),'s1');a.r((59,14,61,26),'s0')
 a.p([(32,14),(44,7),(58,8),(66,14),(63,17),(35,17)],'s1');a.l([(33,14),(44,8),(56,9)],'s3')
 a.p([(47,17),(52,17),(55,21),(55,24),(58,26),(43,26),(46,23),(46,20)],'b1')
 a.l([(47,19),(48,17),(51,17)],'b3');a.r((49,27,51,28),'b0')
 a.l([(43,25),(57,25)],'b2')
 stone(a,11,83,10,6);stone(a,74,82,10,5)
 return a
def chimera():
 a=C();base(a)
 # Ancient experimental vat; cracked glass, three connected pipes, no occupant.
 block(a,24,79,46,8,6,'s2','s0','edge')
 a.e((29,60,68,77),'edge');a.e((30,58,68,72),'b0')
 a.p([(32,60),(32,33),(39,25),(57,25),(65,33),(65,60),(58,67),(40,67)],'edge')
 a.p([(35,59),(35,34),(41,28),(48,28),(48,64),(41,64)],'t1')
 a.p([(49,28),(57,28),(62,34),(62,59),(57,64),(49,64)],'t0')
 a.l([(37,35),(42,30),(45,30)],'t3');a.l([(37,36),(37,48)],'t2')
 a.p([(48,28),(46,39),(52,46),(48,55),(54,64),(50,53),(55,45),(49,38),(52,28)],'void')
 a.l([(48,29),(47,39),(53,46)],'t2')
 # Circular upper collar, open broken top and retaining bands.
 a.e((29,22,68,35),'b0');a.e((33,24,64,31),'void');a.l([(33,24),(42,22),(57,23)],'b2')
 a.l([(32,57),(40,61),(56,61),(66,56)],'b1',2)
 for x,y,h in [(19,68,28),(72,63,21),(77,75,15)]:
  a.l([(x,y),(x,y-h),(x+5,y-h-3),(x+10,y-h-3)],'edge',5)
  a.l([(x,y-1),(x,y-h),(x+5,y-h-3),(x+10,y-h-3)],'b0',3)
  a.l([(x-1,y-3),(x-1,y-h),(x+5,y-h-4)],'b1')
  for yy in [y-4,y-11]:a.r((x-2,yy,x+2,yy+1),'b1')
 block(a,10,80,12,8,3);a.r((13,74,17,76),'t1')
 stone(a,69,86,12,5)
 return a
def furnace():
 a=C();base(a);steps(a,25,84,44,3)
 # Squat furnace with a warm recessed mouth; ring of pipes hugs the housing.
 a.p([(27,72),(24,39),(30,26),(41,20),(57,21),(69,29),(74,43),(70,72),(58,78),(39,77)],'edge')
 a.p([(27,40),(32,29),(42,24),(55,25),(64,31),(67,43),(65,69),(57,74),(39,73),(29,67)],'s1')
 a.p([(55,25),(64,31),(67,43),(65,69),(57,74),(57,43)],'s0')
 a.l([(29,40),(34,29),(42,25),(52,25)],'s3')
 for y in [38,48,62]:
  a.l([(29,y),(40,y-3),(57,y-2),(67,y+2)],'edge')
 a.p([(38,68),(37,51),(42,43),(53,43),(59,50),(59,67),(52,72),(43,72)],'void')
 a.p([(40,65),(40,52),(44,47),(51,47),(56,52),(56,65),(51,68),(44,68)],'f0')
 a.p([(42,64),(43,56),(47,58),(49,51),(53,57),(54,64),(50,66)],'f1')
 a.p([(44,63),(46,58),(48,60),(49,56),(52,63),(49,65)],'f2');a.r((47,61,49,63),'f3')
 for x in [43,50,56]:a.l([(x,50),(x,67)],'edge')
 a.l([(39,67),(57,67)],'s0')
 # Side loop pipes and two uneven chimneys.
 for xx,yy in [(19,46),(72,46)]:
  a.l([(xx+3,yy+21),(xx,yy+15),(xx,yy-3),(xx+4,yy-9),(xx+9,yy-9)],'edge',5)
  a.l([(xx+3,yy+20),(xx,yy+15),(xx,yy-3),(xx+4,yy-9),(xx+9,yy-9)],'b0',3)
  a.l([(xx-1,yy+14),(xx-1,yy-3),(xx+3,yy-9)],'b1')
 block(a,33,28,9,17,3,'s2','s1','s0');a.e((32,7,46,13),'edge');a.l([(34,8),(42,8)],'s3')
 block(a,56,30,8,13,3,'s2','s0','edge');a.e((55,14,68,19),'edge');a.l([(57,15),(64,15)],'s2')
 stone(a,13,84,12,6);stone(a,72,84,10,5)
 return a
FUNCS={'act3_tower_ruin':ruin_tower,'act3_plaza':plaza,'act3_arch':broken_arch,'act3_aqueduct':aqueduct,'act3_rubble':rubble,'act3_start':start,'act3_boss_enemy_warden':warden,'act3_boss_enemy_chimera_1':chimera,'act3_boss_enemy_ember_furnace':furnace}
def main():
 out=Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).resolve().parents[1]/'png';out.mkdir(parents=True,exist_ok=True)
 for n,fn in FUNCS.items():fn().save(out,n)
if __name__=='__main__':main()
