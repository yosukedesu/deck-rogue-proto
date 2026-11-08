#!/usr/bin/env python3
"""C2 Act 2: nine native-resolution pixel miniatures. Pillow integer geometry only.
No image resampling or image-source conversion. Optional argv[1] output directory.
Palette follows A3 handoff: night glass, slate, aged timber, thin brass highlights.
"""
from PIL import Image, ImageDraw
from pathlib import Path
import sys, json, hashlib
P={'void':'#101a2b','edge':'#1b2940','soil':'#263b48','earth':'#354b51','stone0':'#273548','stone1':'#3d4d64','stone2':'#576981','stone3':'#7a8da0','stone4':'#a2b7bd','wood0':'#252d38','wood1':'#48403d','wood2':'#6a5147','wood3':'#936c51','brass0':'#564c3a','brass1':'#89704b','brass2':'#b99a5c','brass3':'#ddc681','moss0':'#203b3b','moss1':'#355b4c','moss2':'#57806b','teal0':'#284e59','teal1':'#427981','teal2':'#70a6a5','teal3':'#add2c9'}
class C:
 def __init__(self,w=96,h=96):self.im=Image.new('RGBA',(w,h));self.d=ImageDraw.Draw(self.im)
 def p(self,pts,col):self.d.polygon(pts,fill=P.get(col,col))
 def l(self,pts,col,w=1):self.d.line(pts,fill=P.get(col,col),width=w)
 def r(self,box,col):self.d.rectangle(box,fill=P.get(col,col))
 def e(self,box,col):self.d.ellipse(box,fill=P.get(col,col))
def ground(c,y=91):
 c.p([(7,y-12),(16,y-20),(32,y-24),(55,y-23),(75,y-19),(88,y-12),(86,y-6),(71,y-2),(53,y),(28,y-1),(14,y-5)],'void')
 c.p([(9,y-13),(19,y-19),(34,y-22),(54,y-21),(75,y-17),(85,y-12),(82,y-7),(65,y-4),(38,y-4),(18,y-7)],'soil')
 c.p([(17,y-15),(27,y-19),(51,y-19),(71,y-14),(76,y-10),(57,y-7),(34,y-8),(18,y-11)],'earth')
 c.l([(14,y-7),(26,y-4),(43,y-2),(58,y-3)],'stone1');c.l([(68,y-4),(79,y-7)],'stone2')
 for x,yy in [(17,y-12),(26,y-7),(66,y-9),(79,y-13)]:c.r((x,yy,x+3,yy+1),'stone1')
def rock(c,x,y,w=12,h=13):
 c.p([(x,y),(x-2,y-5),(x+1,y-h+3),(x+w//2,y-h),(x+w-1,y-h+3),(x+w+1,y-3),(x+w-2,y+1)],'stone0')
 c.p([(x,y-5),(x+2,y-h+4),(x+w//2,y-h+1),(x+w//2+1,y-5),(x+3,y-2)],'stone2')
 c.p([(x+w//2,y-h+1),(x+w-2,y-h+4),(x+w,y-3),(x+w//2+1,y-1),(x+w//2+1,y-5)],'stone1')
 c.l([(x+1,y-h+5),(x+w//2,y-h+2)],'stone3')
def timber(c,x,y,w,h):
 c.r((x,y,x+w,y+h),'wood0');c.r((x+1,y+1,x+w-2,y+h-1),'wood2')
 c.l([(x+2,y+2),(x+2,y+h-2)],'wood3');c.l([(x+w-2,y+4),(x+w-3,y+h//2),(x+w-2,y+h-3)],'wood1')
 for yy in [y+4,y+h-5]:
  c.r((x,yy,x+w,yy+2),'stone0');c.r((x+1,yy,x+w-1,yy),'brass1');c.r((x+2,yy+1,x+2,yy+1),'brass2')
def beam(c,pts):
 c.p(pts,'wood0'); a,b,d,e=pts
 c.l([(a[0]+1,a[1]+1),(b[0]-1,b[1]+1)],'wood3');c.l([(e[0]+2,e[1]-2),(d[0]-2,d[1]-2)],'wood2',3)
def lamp(c,x,y):
 c.l([(x,y-5),(x,y)],'brass1');c.r((x-3,y,x+3,y+8),'brass0');c.r((x-2,y+1,x+2,y+6),'brass2');c.r((x-1,y+2,x,y+5),'brass3');c.r((x-3,y+8,x+3,y+9),'stone0')
def wheel(c,x,y,r=10):
 c.e((x-r,y-r,x+r,y+r),'wood0');c.e((x-r+1,y-r+1,x+r-1,y+r-1),'brass1');c.e((x-r+3,y-r+3,x+r-3,y+r-3),'wood0')
 for dx,dy in [(r-3,0),(0,r-3),(r-5,r-5),(r-5,-r+5)]:c.l([(x-dx,y-dy),(x+dx,y+dy)],'brass1',2)
 c.e((x-2,y-2,x+2,y+2),'brass2');c.l([(x-r+3,y-r+4),(x-r+5,y-r+2),(x-2,y-r+1)],'brass3')
def drum(c,x,y,w=24,h=20):
 c.p([(x,y),(x+w,y+4),(x+w,y+h),(x,y+h-4)],'wood0')
 for xx in range(x+3,x+w-1,3):c.l([(xx,y+2+(xx-x)//6),(xx,y+h-3+(xx-x)//6)],'brass1',2)
 c.l([(x+2,y+1),(x+w-2,y+5)],'brass2');c.e((x-4,y-2,x+5,y+h-1),'stone0');c.e((x-2,y,x+3,y+h-3),'brass1');c.l([(x-1,y+3),(x-1,y+h-6)],'brass3')
 c.e((x+w-4,y+2,x+w+5,y+h+3),'wood0');c.e((x+w-2,y+4,x+w+3,y+h+1),'brass1');c.e((x+w,y+9,x+w+2,y+h-4),'wood0')
def support():
 c=C();ground(c);rock(c,10,83,13,13);rock(c,72,85,13,12)
 timber(c,20,25,8,58);timber(c,68,20,8,59)
 beam(c,[(16,19),(78,13),(80,25),(18,31)])
 c.l([(20,22),(75,17)],'wood2',3);c.l([(23,20),(48,18),(67,16)],'wood3')
 c.p([(28,34),(40,29),(29,49)],'wood0');c.l([(28,34),(37,30),(29,45)],'wood2',3)
 c.p([(67,30),(55,30),(67,44)],'wood0');c.l([(65,32),(59,31),(66,40)],'wood2',3)
 c.l([(45,27),(45,38)],'brass0');lamp(c,45,41)
 c.r((23,84,29,88),'stone0');c.r((66,81,75,86),'stone0')
 return c

def rail_straight():
 c=C(96,48)
 # Perspective in the map plane: rails run left-to-right, not off-canvas.
 for x,y in [(12,19),(24,19),(37,18),(51,17),(65,16),(79,15)]:
  c.p([(x,y),(x+6,y-1),(x+11,y+23),(x+5,y+24)],'wood0');c.p([(x+1,y+1),(x+5,y),(x+9,y+21),(x+6,y+22)],'wood2');c.l([(x+2,y+2),(x+7,y+20)],'wood3')
 for yy in [21,35]:
  c.l([(8,yy+4),(88,yy-2)],'stone0',4);c.l([(8,yy+2),(88,yy-4)],'stone2',2);c.l([(9,yy+1),(85,yy-5)],'stone3')
 for x in [18,44,72]:c.r((x,28-x//14,x+2,29-x//14),'brass1')
 # A small broken tie reaches the declared footprint boundary, without a painted plate.
 c.p([(17,39),(22,38),(24,43),(19,43)],'wood1');c.l([(18,39),(22,39)],'wood3')
 return c

def rail_curve():
 c=C()
 ties=[[(9,80),(7,84),(27,90),(29,86)],[(19,63),(16,67),(36,80),(39,76)],[(31,50),(28,54),(47,68),(50,64)],[(45,39),(41,42),(60,57),(64,54)],[(61,30),(57,32),(72,49),(76,47)],[(77,25),(73,26),(85,44),(89,43)]]
 for p in ties:c.p(p,'wood0');c.l([p[0],p[3]],'wood3',2);c.l([p[1],p[2]],'wood2',2)
 for pts in [[(10,85),(20,69),(31,56),(43,45),(57,37),(72,31),(88,29)],[(24,90),(33,76),(44,63),(55,53),(66,46),(79,42),(90,41)]]:
  c.l(pts,'stone0',4);c.l([(x,y-1) for x,y in pts],'stone2',2);c.l([(x,y-2) for x,y in pts],'stone3')
 return c

def shaft():
 c=C();ground(c)
 c.p([(17,53),(43,40),(78,49),(82,70),(57,84),(24,72)],'stone0')
 c.p([(20,53),(44,43),(76,51),(78,67),(56,79),(26,69)],'wood2')
 c.p([(28,55),(45,48),(69,54),(70,66),(54,74),(33,66)],'void')
 c.p([(31,56),(45,51),(66,56),(65,63),(53,69),(36,63)],'#0b1320')
 c.l([(21,54),(43,44),(74,52)],'wood3');c.l([(27,70),(55,80),(78,67)],'wood1',3)
 # Two narrow guard rails, rear side is tall and leaves the hole readable.
 for x,y in [(22,52),(43,43),(75,51)]:timber(c,x-2,y-20,4,21)
 c.l([(22,35),(43,26),(75,34)],'wood0',5);c.l([(22,33),(43,24),(75,32)],'wood3',2)
 c.l([(52,52),(56,68)],'stone2');c.l([(56,52),(60,66)],'stone1')
 for y in [55,59,63]:c.l([(53+(y-55)//3,y),(57+(y-55)//3,y)],'brass1')
 lamp(c,30,44);rock(c,10,81,11,8);rock(c,75,81,11,10)
 return c

def winch():
 c=C(96,64);ground(c,59)
 c.p([(17,43),(54,36),(77,46),(40,55)],'wood0')
 for y in range(40,53,4):c.l([(21,y+3),(57,y-4),(73,y+4)],'wood2',2)
 timber(c,23,18,6,29);timber(c,58,15,6,28)
 drum(c,31,20,25,19);wheel(c,24,34,12)
 c.l([(57,41),(69,46),(73,43)],'stone1',3);c.r((70,38,73,44),'wood3')
 c.l([(45,43),(49,48),(59,50)],'brass1');c.l([(44,43),(48,49),(58,51)],'wood0')
 rock(c,72,55,10,8)
 return c

def start():
 c=C();ground(c)
 # A stone descent and wooden lift landing, clearly an entrance not a forest gate.
 c.p([(16,46),(38,34),(73,43),(83,73),(59,88),(22,74)],'stone0')
 c.p([(21,49),(40,39),(69,46),(73,67),(55,80),(27,69)],'void')
 for i in range(6):
  x=31+i*2;y=50+i*5
  c.p([(x,y),(x+22,y-7),(x+28,y-4),(x+6,y+4)],'stone1');c.l([(x,y),(x+22,y-7),(x+27,y-5)],'stone3')
 timber(c,17,22,7,51);timber(c,67,15,7,51)
 beam(c,[(13,17),(74,10),(78,20),(15,29)])
 c.l([(20,19),(69,13)],'wood2',3);c.l([(21,18),(67,13)],'wood3')
 wheel(c,46,26,8);c.l([(42,33),(42,49)],'stone2');c.l([(50,32),(50,47)],'stone1')
 lamp(c,26,37);c.p([(70,61),(80,58),(86,64),(76,69)],'wood2');c.l([(72,61),(79,60)],'wood3')
 rock(c,9,82,14,18);rock(c,74,85,12,15)
 return c

def turtle():
 c=C();ground(c)
 # A broad low hollow, shell-like geology without depicting the boss itself.
 c.p([(11,71),(12,50),(22,35),(40,26),(63,29),(79,43),(85,65),(79,77),(22,81)],'stone0')
 c.p([(13,59),(16,47),(27,36),(40,29),(51,31),(43,44),(26,52),(21,69)],'stone2')
 c.p([(43,29),(63,32),(75,43),(80,55),(64,51),(55,43)],'stone1')
 c.l([(17,48),(28,37),(40,31),(49,32)],'stone3')
 c.p([(24,64),(29,51),(42,44),(57,46),(70,56),(72,72),(62,79),(33,78)],'void')
 c.p([(31,66),(39,58),(52,55),(63,60),(67,68),(61,72),(41,73)],'#0c1423')
 c.p([(25,77),(38,72),(55,73),(67,77),(62,82),(42,84),(29,81)],'wood1')
 for pts in [[(29,78),(43,75),(52,77)],[(39,81),(54,79),(62,79)],[(31,80),(37,80)]]:c.l(pts,'wood2')
 rock(c,12,82,15,13);rock(c,70,83,15,14)
 for x,y in [(20,45),(28,40),(70,44),(78,63)]:c.p([(x,y),(x+5,y-2),(x+8,y+1),(x+3,y+3)],'moss1');c.l([(x+2,y),(x+5,y-1)],'moss2')
 c.p([(57,84),(68,81),(76,84),(69,87),(59,87)],'teal0');c.l([(61,84),(68,83),(71,84)],'teal1')
 return c

def crab():
 c=C();ground(c)
 c.p([(17,64),(35,54),(61,54),(80,66),(72,82),(49,88),(23,80)],'stone0')
 c.p([(21,66),(36,57),(60,57),(76,67),(69,79),(48,84),(26,77)],'teal0')
 c.p([(29,68),(40,62),(58,62),(66,69),(61,77),(43,80),(30,75)],'teal1')
 c.l([(27,69),(39,63),(50,64)],'teal2');c.l([(45,79),(60,76)],'teal2')
 # Mirrored pumping housings / paired arms represented by architecture, not creatures.
 for x,y in [(17,33),(62,28)]:
  c.p([(x,y+8),(x+5,y),(x+18,y+2),(x+21,y+27),(x+14,y+35),(x,y+30)],'stone0')
  c.p([(x+2,y+8),(x+6,y+2),(x+15,y+4),(x+15,y+29),(x+2,y+27)],'stone2')
  c.p([(x+16,y+4),(x+19,y+8),(x+20,y+26),(x+16,y+30)],'stone1')
  c.r((x+5,y+11,x+12,y+21),'void');c.l([(x+4,y+8),(x+13,y+10)],'brass2')
  c.l([(x+4,y+25),(x+12,y+27)],'brass1',2)
  wheel(c,x+9,y+15,5)
 c.l([(33,44),(42,45),(43,63)],'wood0',6);c.l([(34,42),(43,44),(44,63)],'brass1',3)
 c.l([(65,43),(57,47),(55,66)],'wood0',6);c.l([(64,41),(56,46),(54,64)],'brass1',3)
 c.l([(36,72),(43,73)],'teal3');c.l([(57,71),(62,70)],'teal2')
 rock(c,10,79,12,11);rock(c,75,83,12,13)
 return c

def warden():
 c=C();ground(c)
 c.p([(20,70),(21,38),(31,23),(64,20),(78,35),(79,70),(61,82),(32,81)],'stone0')
 c.p([(23,39),(32,26),(39,25),(37,64),(28,74)],'stone1')
 c.p([(64,23),(74,36),(76,68),(68,74),(65,42)],'stone1')
 timber(c,24,25,7,49);timber(c,66,19,7,52)
 beam(c,[(20,20),(74,13),(78,24),(22,32)])
 c.l([(26,22),(68,17)],'wood3');drum(c,36,33,23,23)
 wheel(c,34,47,13)
 for x in [45,57]:
  c.l([(x,59),(x,79)],'stone0',3)
  for y in range(60,80,4):c.r((x-1,y,x+1,y+2),'brass1');c.r((x,y,x,y+1),'brass2')
 c.p([(33,75),(61,72),(69,78),(42,84)],'wood0')
 for x in [38,44,50,56,62]:c.l([(x,71),(x+2,80)],'stone2',2)
 c.l([(34,73),(64,70)],'stone3');c.l([(39,81),(67,77)],'stone1')
 lamp(c,76,40);rock(c,10,83,13,13);rock(c,73,85,12,12)
 return c
DRAW={'act2_support':support,'act2_rail_straight':rail_straight,'act2_rail_curve':rail_curve,'act2_shaft':shaft,'act2_winch':winch,'act2_start':start,'act2_boss_enemy_turtle':turtle,'act2_boss_enc_kaiser_crab':crab,'act2_boss_enemy_winch_warden':warden}
def main():
 out=Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).resolve().parents[1]/'png';out.mkdir(parents=True,exist_ok=True)
 for name,fun in DRAW.items():fun().im.save(out/(name+'.png'))
if __name__=='__main__':main()
