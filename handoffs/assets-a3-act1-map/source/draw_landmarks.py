#!/usr/bin/env python3
"""A3 Act 1 map miniatures. Direct 96x96 integer-pixel drawing; no source resampling."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json, hashlib
import sys
OUT = Path(sys.argv[1]).resolve() if len(sys.argv)>1 else Path(__file__).resolve().parents[1]/'png'
OUT.mkdir(parents=True,exist_ok=True)
P = {
 'void':'#101a2b','edge':'#1b2940','soil':'#263b48','earth':'#354b51','stone0':'#273548','stone1':'#3d4d64','stone2':'#576981','stone3':'#7a8da0','stone4':'#a2b7bd',
 'wood0':'#252d38','wood1':'#48403d','wood2':'#6a5147','wood3':'#936c51','brass0':'#564c3a','brass1':'#89704b','brass2':'#b99a5c','brass3':'#ddc681',
 'moss0':'#203b3b','moss1':'#355b4c','moss2':'#57806b','moss3':'#86a18a','teal0':'#284e59','teal1':'#427981','teal2':'#70a6a5','teal3':'#add2c9',
 'plum0':'#332a46','plum1':'#52425f','plum2':'#775e7d','plum3':'#a081a2','rose0':'#563342','rose1':'#884858','rose2':'#b7767c','rose3':'#dca5a3'
}
def C(k): return P.get(k,k)
class Canvas:
 def __init__(self):
  self.im=Image.new('RGBA',(96,96)); self.d=ImageDraw.Draw(self.im)
 def p(self,pts,c): self.d.polygon(pts,fill=C(c))
 def l(self,pts,c,w=1): self.d.line(pts,fill=C(c),width=w)
 def r(self,box,c): self.d.rectangle(box,fill=C(c))
 def px(self,x,y,c): self.im.putpixel((x,y), (*ImageColorRGB(C(c)),255))
 def e(self,box,c): self.d.ellipse(box,fill=C(c))
 def save(self,name): self.im.save(OUT/name)
def ImageColorRGB(s): return tuple(int(s[i:i+2],16) for i in (1,3,5))
def base(a,variant=0):
 # No rectangular plate; stepped earth at the base of the miniature.
 a.p([(9,74),(16,68),(27,66),(38,64),(56,66),(70,67),(83,73),(90,80),(86,85),(74,89),(55,91),(31,90),(17,86),(8,80)],'void')
 a.p([(11,74),(20,69),(31,68),(43,65),(57,67),(71,69),(84,75),(87,80),(77,85),(59,88),(35,87),(20,83),(11,79)],'soil')
 a.p([(12,74),(22,70),(40,68),(57,70),(76,73),(82,78),(72,82),(53,84),(32,83),(16,79)],'earth')
 a.p([(16,79),(26,82),(46,84),(58,83),(72,84),(79,82),(80,84),(68,87),(51,89),(31,87),(19,83)],'stone0')
 a.l([(20,85),(30,87),(43,88)],'stone1'); a.l([(55,88),(65,87),(72,86)],'stone1')
 for x,y in [(14,77),(25,83),(36,70),(61,83),(78,78),(84,81),(31,87)]:
  a.r((x,y,x+2,y),'stone2'); a.r((x+1,y+1,x+2,y+1),'stone1')
def rock(a,x,y,w,h):
 # y bottom, coherent face planes, intentional one-pixel highlights.
 a.p([(x,y),(x-2,y-h//3),(x+2,y-h+3),(x+w//2,y-h),(x+w-2,y-h+3),(x+w+1,y-5),(x+w-2,y)],'edge')
 a.p([(x+1,y-2),(x,y-h//3),(x+3,y-h+4),(x+w//2,y-h+1),(x+w//2+1,y-h//2),(x+w//3,y-4)],'stone2')
 a.p([(x+w//2,y-h+1),(x+w-3,y-h+4),(x+w-1,y-6),(x+w//2+1,y-2),(x+w//3,y-4),(x+w//2+1,y-h//2)],'stone1')
 a.p([(x+1,y-2),(x+w//3,y-4),(x+w//2+1,y-2),(x+w-1,y-6),(x+w-2,y-1)],'stone0')
 a.l([(x+2,y-h//3),(x+4,y-h+5),(x+w//2,y-h+2)],'stone3')
 if w>15: a.l([(x+w//2+3,y-h+5),(x+w//2+1,y-h//2),(x+w//2+3,y-h//2+3)],'stone0')
def moss(a,x,y,s=1):
 a.p([(x-4*s,y),(x-2*s,y-2*s),(x+1*s,y-1*s),(x+3*s,y-3*s),(x+5*s,y),(x+2*s,y+2*s),(x-2*s,y+2*s)],'moss0')
 a.p([(x-3*s,y),(x-1*s,y-1*s),(x+1*s,y),(x+3*s,y-2*s),(x+4*s,y),(x+1*s,y+1*s)],'moss1')
 a.l([(x-2*s,y),(x-1*s,y),(x,y+1)],'moss2')
 a.p([(x,y),(x-2*s,y-4*s),(x,y-2*s),(x+1,y-6*s),(x+2,y-2*s),(x+4*s,y-4*s),(x+2*s,y+1)],'moss2')
 a.r((x,y-3*s,x,y-2*s),'moss3')
def rivet(a,x,y):
 a.r((x,y,x+2,y+2),'brass0'); a.r((x,y,x+1,y),'brass3');a.r((x,y+1,x,y+1),'brass1')
def timber(a,pts,axis='v'):
 a.p(pts,'wood0')
 if axis=='v':
  x=min(p[0] for p in pts); y=min(p[1] for p in pts); xmax=max(p[0] for p in pts); ymax=max(p[1] for p in pts)
  a.p([(x+1,y+1),(xmax-2,y+1),(xmax-2,ymax-2),(x+1,ymax-1)],'wood2')
  a.l([(x+2,y+2),(x+2,ymax-3)],'wood3')
  a.l([(xmax-2,y+4),(xmax-3,y+13),(xmax-2,ymax-3)],'wood1')
 else:
  x=min(p[0] for p in pts); y=min(p[1] for p in pts); xmax=max(p[0] for p in pts); ymax=max(p[1] for p in pts)
  a.p([(x+1,y+1),(xmax-1,y+1),(xmax-2,ymax-2),(x+1,ymax-1)],'wood2')
  a.l([(x+2,y+1),(xmax-3,y+1)],'wood3');a.l([(x+5,y+4),(x+15,y+3),(x+31,y+4),(xmax-4,y+3)],'wood1')
def gate():
 a=Canvas();base(a)
 # Ridge and pointed foreground rock outcrops give the entrance a non-flat silhouette.
 a.p([(13,69),(9,52),(14,31),(25,18),(37,14),(48,17),(63,12),(77,22),(85,41),(84,65),(77,77),(22,77)],'edge')
 a.p([(14,59),(15,33),(26,19),(37,16),(33,32),(24,47),(22,70)],'stone1')
 a.p([(17,39),(26,21),(32,19),(28,33),(24,41)],'stone2')
 a.l([(17,33),(26,22),(31,20)],'stone3')
 a.p([(32,25),(36,16),(46,18),(51,23),(61,14),(71,20),(76,32),(64,37),(43,32)],'stone1')
 a.p([(39,18),(46,20),(49,25),(40,26)],'stone2')
 a.p([(63,15),(70,21),(68,29),(60,31),(60,24)],'stone2')
 a.p([(73,23),(81,39),(83,58),(76,72),(70,64),(73,40)],'stone0')
 a.p([(76,31),(81,42),(80,58),(76,63),(76,45)],'stone1')
 # Deep opening. Never becomes absolute black.
 a.p([(29,31),(66,30),(70,68),(61,75),(32,74),(26,67)],'void')
 a.p([(33,33),(63,33),(64,66),(60,70),(36,70)],'#0c1423')
 # Rails and ties recede through threshold.
 for y,w in [(66,17),(70,22),(75,29),(81,39),(87,49)]:
  cx=48;a.p([(cx-w//2,y),(cx+w//2,y),(cx+w//2+2,y+2),(cx-w//2-2,y+2)],'wood1')
  a.l([(cx-w//2,y),(cx+w//2,y)],'wood2')
 a.p([(40,64),(43,64),(31,90),(28,90)],'stone1'); a.p([(55,64),(57,64),(70,90),(67,90)],'stone0')
 a.l([(41,64),(30,89)],'brass1');a.l([(42,64),(31,89)],'stone3')
 a.l([(56,64),(69,89)],'stone2');a.l([(55,64),(68,89)],'brass1')
 timber(a,[(25,28),(32,28),(34,76),(27,77)]); timber(a,[(64,27),(71,27),(69,76),(63,74)])
 timber(a,[(23,24),(73,24),(73,33),(24,33)],'h')
 a.p([(26,33),(32,33),(39,40),(37,43),(29,38)],'wood1'); a.l([(29,33),(37,40)],'wood3')
 a.p([(64,33),(69,33),(63,42),(59,43)],'wood1');a.l([(66,33),(61,40)],'wood3')
 for x,y in [(25,26),(66,26),(27,66),(64,65)]:rivet(a,x,y)
 # Small warm lamp remains a contextual object, not A7 UI lantern asset.
 a.l([(34,32),(34,42),(37,42)],'brass1');a.r((33,42,38,49),'brass0');a.r((34,43,37,47),'brass2');a.r((35,44,36,46),'brass3');a.l([(33,49),(38,49)],'wood0')
 rock(a,11,78,15,24); rock(a,72,78,14,24)
 moss(a,19,77);moss(a,75,78);moss(a,24,22);moss(a,60,18)
 a.save('map_gate_act1.png')
def brute():
 a=Canvas();base(a)
 # Low broad rock den, displaced entrance and heavy ribs of stone.
 a.p([(14,68),(12,52),(17,34),(29,24),(47,19),(64,24),(79,37),(83,56),(77,71),(62,76),(24,75)],'edge')
 a.p([(17,50),(21,35),(31,27),(46,22),(58,26),(57,37),(32,44),(23,61)],'stone2')
 a.p([(22,37),(31,28),(45,24),(48,28),(39,34),(31,33)],'stone3')
 a.p([(50,25),(62,27),(76,39),(79,56),(72,66),(66,41),(55,38)],'stone1')
 a.l([(60,28),(72,37),(75,44)],'stone2')
 a.p([(28,42),(40,35),(57,36),(67,47),(68,65),(61,72),(31,70),(23,62)],'void')
 a.p([(33,44),(42,39),(55,40),(61,48),(63,64),(56,67),(34,65)],'#0d1725')
 a.p([(26,65),(35,64),(51,67),(64,65),(62,73),(48,77),(28,73)],'stone0')
 # Brute's rough club leans over den; big asymmetric silhouette makes role readable.
 a.p([(67,75),(63,73),(78,32),(83,33),(83,39)],'wood0')
 a.p([(66,72),(65,70),(78,36),(80,36)],'wood3')
 a.p([(75,39),(70,32),(72,18),(78,11),(83,11),(88,19),(87,31),(83,41)],'wood0')
 a.p([(74,32),(75,20),(79,14),(82,13),(85,20),(83,34),(80,37)],'wood2')
 a.p([(74,26),(76,19),(79,15),(80,28),(78,35),(75,33)],'wood3')
 a.l([(83,18),(82,25),(83,31)],'wood1')
 a.p([(72,23),(86,20),(87,24),(73,27)],'stone0');a.l([(73,23),(85,21)],'brass1')
 a.p([(72,33),(84,30),(84,34),(75,37)],'stone0');a.l([(74,33),(83,31)],'brass1')
 # Bone trophies rendered in cool stone colors, not cream paper colors.
 a.p([(24,46),(21,40),(22,32),(26,28),(25,36),(27,41),(29,44)],'stone0')
 a.p([(24,42),(23,37),(24,32),(26,29),(25,36),(27,42)],'stone4')
 a.p([(59,44),(59,37),(63,32),(66,30),(64,36),(63,43),(61,47)],'stone0')
 a.p([(61,43),(61,38),(64,33),(65,32),(63,39),(63,43)],'stone3')
 a.l([(27,43),(36,48),(48,47),(59,43)],'brass0')
 for x,y in [(33,47),(41,48),(50,46)]:
  a.p([(x,y),(x+3,y),(x+2,y+6),(x,y+4)],'stone2');a.r((x,y,x+1,y+2),'stone4')
 # Broken logs and small debris, without large secondary silhouette.
 a.p([(17,69),(22,66),(39,77),(36,82),(32,83)],'wood0');a.p([(18,70),(22,68),(37,78),(35,80)],'wood2');a.l([(20,69),(35,78)],'wood3')
 a.e((30,76,38,83),'wood1');a.e((32,77,36,81),'wood3');a.l([(33,78),(35,80)],'wood1')
 rock(a,13,78,12,13);rock(a,58,81,12,10)
 moss(a,18,56);moss(a,58,26);moss(a,76,78)
 a.save('map_boss_enemy_brute.png')
def stone_pillar(a,x,y,h,rose=False):
 a.p([(x-4,y-h+3),(x,y-h),(x+5,y-h+3),(x+4,y),(x,y+3),(x-4,y)],'plum0' if rose else 'edge')
 a.p([(x-3,y-h+4),(x,y-h+2),(x,y+1),(x-3,y-1)],'plum2' if rose else 'stone2')
 a.p([(x,y-h+2),(x+4,y-h+4),(x+3,y-1),(x,y+1)],'plum1' if rose else 'stone1')
 a.l([(x-3,y-h+4),(x,y-h+1),(x+3,y-h+3)],'plum3' if rose else 'stone3')
def ritual():
 a=Canvas();base(a)
 # Broken rear arch turns the pawn into a ritual-site silhouette, not a building.
 stone_pillar(a,23,63,37,True);stone_pillar(a,73,62,35,True)
 a.p([(19,28),(23,20),(35,15),(42,14),(41,22),(32,23),(27,30)],'plum0')
 a.p([(22,27),(25,22),(35,18),(40,17),(39,20),(31,21),(26,28)],'plum2')
 a.l([(25,22),(35,17),(40,16)],'plum3')
 a.p([(60,15),(72,21),(77,29),(71,32),(67,25),(60,23)],'plum0')
 a.p([(62,18),(69,22),(74,28),(72,29),(67,24),(62,22)],'plum2')
 a.l([(63,18),(69,21),(74,27)],'plum3')
 a.p([(18,69),(46,58),(77,69),(76,76),(48,87),(19,77)],'plum0')
 a.p([(18,68),(46,57),(78,68),(48,81)],'plum1')
 a.l([(20,68),(46,59),(75,68)],'plum2');a.l([(20,77),(48,85),(75,75)],'plum2')
 # Foreshortened ring inscribed with restrained red facets.
 a.p([(28,69),(33,64),(47,62),(61,65),(67,69),(59,75),(45,77),(33,73)],'brass0')
 a.p([(31,69),(36,66),(47,64),(59,67),(63,69),(57,73),(45,75),(35,72)],'plum0')
 a.l([(30,69),(35,65),(46,63)],'brass2'); a.l([(53,64),(62,67),(65,69)],'brass1')
 a.l([(35,73),(44,76),(57,74)],'rose1')
 for pts in [[(31,67),(29,69),(32,70)],[(61,68),(65,69),(62,71)],[(45,74),(46,77),(49,75)]]: a.l(pts,'rose2')
 # Altar rear and top plane, with violet/red cloth and sharp brass filigree.
 a.p([(36,57),(48,52),(61,57),(61,69),(48,74),(36,69)],'plum0')
 a.p([(36,57),(48,53),(61,57),(48,62)],'stone2')
 a.l([(37,57),(48,54),(59,57)],'stone3')
 a.p([(37,59),(48,63),(48,71),(37,68)],'stone1')
 a.p([(48,63),(60,59),(60,68),(48,72)],'stone0')
 a.p([(44,56),(49,55),(55,58),(53,60),(53,72),(49,75),(46,73),(46,60)],'rose0')
 a.p([(46,57),(49,56),(52,58),(50,59),(50,72),(48,73),(47,70),(47,59)],'rose1')
 a.l([(46,60),(46,70),(49,74),(52,72)],'brass1')
 a.r((48,64,50,66),'brass2');a.r((49,63,49,68),'brass3')
 # Ritual crystal, not a character, and two candelabra at unequal depth.
 a.p([(44,49),(47,36),(51,40),(53,50),(49,55)],'rose0')
 a.p([(47,39),(49,41),(48,51),(46,50)],'rose3')
 a.p([(49,40),(52,50),(49,53),(48,51)],'rose1')
 for x,y,h in [(28,66,22),(69,69,27)]:
  a.p([(x-4,y),(x,y-2),(x+4,y),(x,y+2)],'brass0');a.l([(x,y-2),(x,y-h)],'brass1',2)
  a.l([(x-4,y-h+6),(x-4,y-h+3),(x,y-h+5),(x+4,y-h+3),(x+4,y-h+6)],'brass1')
  a.l([(x,y-h),(x,y-h+6)],'brass2')
  a.r((x-1,y-h-4,x+1,y-h),'rose2'); a.r((x,y-h-5,x,y-h-2),'rose3')
  a.r((x-4,y-h+1,x-4,y-h+3),'rose3'); a.r((x+4,y-h+1,x+4,y-h+3),'rose2')
 rock(a,12,80,12,9);rock(a,77,79,9,9);moss(a,16,73);moss(a,75,82)
 a.save('map_boss_enc_kin_ritual.png')
def branch(a,pts,w=3):
 a.l(pts,'stone0',w+2);a.l([(x-1,y-1) for x,y in pts],'plum1',w);a.l([(x-1,y-1) for x,y in pts],'plum3',1)
def crystal(a,x,y,h,w=7):
 a.p([(x-w//2,y-2),(x-w//2-1,y-h//3),(x-1,y-h),(x+2,y-h+2),(x+w//2+1,y-h//3),(x+w//2,y-1),(x,y+2)],'teal0')
 a.p([(x-w//2,y-2),(x-w//2,y-h//3),(x-1,y-h+2),(x,y-2)],'teal2')
 a.p([(x,y-2),(x-1,y-h+2),(x+1,y-h+3),(x+w//2,y-h//3),(x+w//2-1,y-1)],'teal1')
 a.l([(x-w//2,y-h//3),(x-1,y-h+2)],'teal3');a.l([(x-1,y-h+2),(x,y-2)],'stone4')
def stag():
 a=Canvas();base(a)
 # Two rooted antler trees, one behind the other; generous negative space through centre.
 branch(a,[(28,72),(25,57),(26,42),(21,26),(17,20)],5)
 branch(a,[(26,46),(15,39),(11,27),(13,19)],3)
 branch(a,[(22,30),(29,24),(31,13),(28,8)],3)
 branch(a,[(29,23),(39,18),(41,10)],2)
 branch(a,[(16,39),(9,39),(6,33)],2)
 branch(a,[(22,27),(15,25),(13,21)],2)
 branch(a,[(31,16),(35,13),(36,7)],1)
 branch(a,[(69,70),(72,56),(68,43),(73,30),(77,22)],5)
 branch(a,[(70,48),(82,40),(85,29),(84,22)],3)
 branch(a,[(73,31),(66,24),(64,13),(66,8)],3)
 branch(a,[(66,23),(57,18),(55,12)],2)
 branch(a,[(81,40),(88,38),(90,33)],2)
 branch(a,[(76,25),(83,21),(84,17)],2)
 branch(a,[(63,15),(59,12),(58,7)],1)
 # Small pink ends tie to the actual haze-stag antlers.
 for x,y in [(28,8),(41,10),(36,7),(66,8),(55,12),(58,7)]:a.r((x-1,y,x,y+1),'rose3')
 a.p([(19,73),(26,63),(30,67),(33,77),(28,82),(16,79)],'stone0')
 a.p([(21,72),(26,66),(28,69),(27,76),(20,78)],'stone2')
 a.l([(21,72),(26,67)],'stone3')
 a.p([(65,69),(69,62),(74,67),(80,79),(71,83),(63,77)],'stone0')
 a.p([(67,70),(69,66),(72,68),(73,77),(68,79)],'stone2')
 # Mossy stone spring and cold crystal outcrop.
 a.e((28,65,68,83),'stone0');a.e((31,67,64,79),'teal0');a.e((34,68,61,76),'teal1')
 a.l([(34,70),(39,68),(49,68)],'teal2');a.l([(46,77),(55,76),(60,74)],'teal2')
 rock(a,35,72,14,17);rock(a,52,77,11,12)
 crystal(a,45,70,24,9);crystal(a,52,73,17,7);crystal(a,37,75,12,6)
 # Deliberately opaque, sparse pixel wisps kept inside the footprint.
 a.l([(17,82),(26,82),(28,83),(38,83)],'teal1')
 a.l([(21,80),(29,80),(31,81),(40,81)],'teal2')
 a.l([(55,81),(64,81),(67,80),(80,80)],'teal1')
 a.l([(58,79),(66,79),(69,78),(76,78)],'teal2')
 moss(a,15,77);moss(a,76,84);moss(a,60,74)
 for x,y in [(20,68),(29,80),(61,68)]:
  a.l([(x,y),(x-1,y-5)],'moss3');a.l([(x,y),(x+2,y-4)],'teal2')
 a.save('map_boss_enemy_haze_stag.png')
if __name__=='__main__':
 for f in (gate,brute,ritual,stag):f()
 files=['map_gate_act1.png','map_boss_enemy_brute.png','map_boss_enc_kin_ritual.png','map_boss_enemy_haze_stag.png']
 # Preview only: native art is never obtained by reducing a larger bitmap.
 sheet=Image.new('RGB',(400,236),'#101c2c');d=ImageDraw.Draw(sheet)
 for i,f in enumerate(files):
  im=Image.open(OUT/f);x=i*100+2;sheet.paste(im,(x,17),im);d.text((x,2),['GATE','BRUTE','RITUAL','HAZE STAG'][i],fill='#bca26b')
  light=Image.new('RGB',(96,96),'#91a1af');light.paste(im,(0,0),im);sheet.paste(light,(x,129))
 sheet.save(OUT/'landmarks_native.png');sheet.resize((1200,708),Image.Resampling.NEAREST).save(OUT/'landmarks_3x.png')
 (OUT/'palette.json').write_text(json.dumps(P,indent=2)+'\n')
 print('Rendered',len(files),'native 96x96 RGBA landmarks')
