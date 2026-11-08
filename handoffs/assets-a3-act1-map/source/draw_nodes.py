#!/usr/bin/env python3
"""Native 48x48 hand-authored integer-coordinate pixel drawings.
No input image, resampling, filter, or image generation is used for final assets.
All contours, facets and highlight clusters are placed directly on the final grid.
Only the review sheets use integer nearest-neighbor display enlargement.
"""
from pathlib import Path
import json, hashlib
from PIL import Image,ImageDraw
import sys
OUT=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else Path(__file__).resolve().parents[1]/'png'
OUT.mkdir(parents=True,exist_ok=True)
ROOT=OUT
# Night glass / aged brass. Highlights are gold or cool steel, never paper ivory.
P={
 'void':'#101a28','night':'#172438','navy':'#22364b','slate':'#334b61','blue':'#4d687d','steel':'#7b98a7','ice':'#a8c4cb',
 'umber':'#302b2b','bronze':'#584236','brassdark':'#735538','brass':'#9b743f','gold':'#c89c55','glint':'#e5be72',
 'copperdark':'#643f38','copper':'#995840','rust':'#bd7550',
 'tealdark':'#17414b','teal':'#287080','aqua':'#57a5af','foam':'#9acdd0',
 'plumdark':'#312c46','plum':'#4f405c','violet':'#74607b',
 'emberdark':'#713943','ember':'#ad4d3c','orange':'#dd713e','flame':'#efa54e','hot':'#f5cf72',
 'wooddark':'#332e30','wood':'#574139','woodlit':'#805744','woodglint':'#a57550',
}
class C:
 def __init__(self): self.im=Image.new('RGBA',(48,48),(0,0,0,0)); self.d=ImageDraw.Draw(self.im)
 def poly(self,pts,col): self.d.polygon(pts, fill=P.get(col,col))
 def line(self,pts,col,w=1): self.d.line(pts, fill=P.get(col,col),width=w)
 def rect(self,xy,col): self.d.rectangle(xy,fill=P.get(col,col))
 def px(self,x,y,col): self.d.point((x,y),fill=P.get(col,col))
 def save(self,name): self.im.save(OUT/(name+'.png'),optimize=False); return self.im

def battle():
 c=C()
 # Irregular octagonal steel medallion, a thin gold edge rather than an ink ring.
 c.poly([(17,5),(30,5),(39,13),(43,22),(41,32),(32,41),(21,43),(10,38),(5,29),(5,18),(11,10)],'navy')
 c.poly([(17,6),(29,6),(38,14),(41,22),(39,31),(31,39),(21,41),(11,36),(7,28),(7,18),(12,12)],'bronze')
 c.line([(17,6),(29,6),(37,13),(40,20)],'gold')
 c.line([(16,8),(10,14),(8,21),(8,27)],'glint')
 c.line([(11,34),(20,39),(29,38),(37,31)],'brass')
 c.line([(40,24),(38,31),(30,40),(22,42)],'brassdark')
 c.poly([(17,10),(28,10),(35,16),(38,24),(35,32),(28,36),(19,36),(12,31),(10,23),(13,15)],'tealdark')
 c.poly([(18,12),(29,12),(35,18),(35,29),(28,34),(19,34),(13,29),(13,19)],'night')
 # Horned bestial mask and brow, not a human skull.
 c.poly([(12,14),(18,17),(23,14),(29,17),(35,13),(33,22),(31,28),(28,32),(25,36),(21,35),(18,31),(15,27),(14,21)],'slate')
 c.poly([(12,14),(17,17),(17,22),(15,21)],'steel')
 c.poly([(35,13),(32,22),(29,22),(30,17)],'blue')
 c.poly([(18,18),(23,16),(28,18),(27,21),(23,20),(19,22)],'blue')
 c.line([(18,18),(22,16),(26,17)],'steel')
 c.poly([(16,22),(21,23),(20,26),(17,25)],'void')
 c.poly([(26,23),(31,21),(30,25),(27,26)],'void')
 c.line([(17,23),(20,24)],'aqua'); c.line([(27,24),(30,22)],'aqua')
 c.px(18,23,'foam'); c.px(29,23,'foam')
 c.poly([(23,22),(25,26),(24,28),(21,27)],'blue')
 c.px(23,24,'steel'); c.line([(20,29),(24,30),(28,28)],'void')
 c.poly([(17,28),(20,29),(19,33),(17,31)],'gold')
 c.poly([(27,29),(30,27),(29,32),(27,34)],'brass')
 c.px(17,28,'glint'); c.px(29,28,'gold')
 c.line([(21,33),(23,34),(25,33)],'blue')
 for x,y in [(17,8),(38,22),(23,39),(9,26)]: c.px(x,y,'glint')
 return c.save('node_battle')

def elite():
 c=C()
 # Wide horns silhouette, separate from round regular-battle mask.
 c.poly([(6,5),(7,13),(12,17),(18,18),(19,22),(13,21),(7,18),(4,13),(4,7)],'bronze')
 c.poly([(6,5),(6,11),(8,15),(13,18),(18,18),(18,20),(12,19),(7,16),(5,12),(5,7)],'gold')
 c.line([(6,6),(7,11),(9,14),(13,16)],'glint')
 c.poly([(42,5),(44,11),(42,17),(38,20),(31,22),(30,18),(36,17),(40,13)],'brassdark')
 c.line([(42,6),(43,11),(41,16),(37,19),(32,20)],'gold')
 c.line([(40,9),(40,13),(37,16)],'bronze')
 for pts in [[(8,16),(10,14)],[(12,19),(13,16)],[(36,19),(35,17)],[(41,15),(39,13)]]: c.line(pts,'bronze')
 # Skull in patinated grey metal, not cream bone.
 c.poly([(20,13),(27,13),(32,18),(33,26),(29,31),(28,40),(25,44),(21,43),(19,38),(19,32),(15,27),(15,20)],'navy')
 c.poly([(20,14),(26,14),(31,19),(31,26),(27,31),(27,39),(24,42),(21,40),(21,31),(17,26),(17,20)],'blue')
 c.poly([(20,15),(25,15),(28,18),(28,22),(24,23),(20,21),(18,24),(18,20)],'steel')
 c.line([(20,15),(24,14),(27,16)],'ice')
 c.poly([(18,24),(21,25),(21,29),(18,27)],'void')
 c.poly([(25,25),(30,23),(29,28),(26,29)],'night')
 c.line([(18,24),(21,25)],'bronze'); c.line([(26,25),(29,24)],'brass')
 c.poly([(22,24),(24,24),(25,32),(24,37),(22,34)],'steel')
 c.line([(22,25),(22,32)],'ice')
 c.poly([(22,36),(24,37),(25,35),(25,39),(23,41),(22,39)],'night')
 c.px(21,35,'gold');c.px(26,34,'brass')
 c.line([(20,19),(22,20),(21,22)],'blue'); c.line([(26,18),(25,21)],'blue')
 c.line([(17,29),(19,31)],'gold'); c.line([(28,30),(30,28)],'brass')
 return c.save('node_elite')

def unknown():
 c=C()
 # Glass question mark in a narrow brass setting. Hard stepped outline and bevel.
 outline=[(17,5),(29,5),(35,8),(39,14),(39,20),(36,25),(29,29),(27,32),(27,34),(18,34),(18,29),(22,24),(28,21),(30,18),(29,15),(25,13),(21,14),(19,18),(10,18),(10,13),(13,8)]
 c.poly(outline,'bronze')
 inner=[(17,7),(29,7),(34,10),(37,14),(37,20),(34,24),(27,28),(25,31),(25,33),(20,33),(20,29),(24,24),(30,21),(32,18),(31,14),(26,11),(20,12),(17,16),(12,16),(12,13),(15,9)]
 c.poly(inner,'teal')
 c.poly([(17,7),(29,7),(34,10),(34,13),(28,10),(21,10),(17,12),(15,16),(12,16),(12,13)],'aqua')
 c.line([(18,7),(28,7),(33,9)],'foam')
 c.poly([(35,12),(37,15),(37,20),(34,24),(27,28),(25,31),(25,33),(23,33),(23,29),(26,26),(32,22),(35,18)],'tealdark')
 c.line([(20,29),(24,24),(30,21)],'aqua')
 c.line([(11,12),(14,8),(17,6)],'gold'); c.line([(35,9),(38,14),(38,19)],'brass')
 c.line([(20,34),(27,34)],'brassdark')
 c.poly([(18,38),(26,37),(28,39),(27,44),(19,44),(17,42)],'bronze')
 c.poly([(19,38),(26,38),(26,42),(19,43),(18,41)],'teal')
 c.line([(19,38),(25,38)],'foam'); c.line([(18,40),(19,42)],'aqua')
 c.line([(20,44),(27,43)],'brassdark')
 return c.save('node_unknown')

def campfire():
 c=C()
 # Tapered crossed wood, no disk underlay.
 c.poly([(6,32),(10,29),(39,37),(42,41),(37,44),(9,36)],'wooddark')
 c.poly([(8,32),(11,31),(38,38),(39,40),(35,42),(10,35)],'wood')
 c.line([(10,31),(36,38)],'woodlit'); c.line([(12,34),(31,40)],'wooddark')
 c.poly([(8,40),(11,35),(35,29),(40,31),(40,35),(13,44)],'wooddark')
 c.poly([(10,40),(13,36),(35,30),(38,31),(37,34),(13,42)],'woodlit')
 c.line([(13,36),(35,30)],'woodglint'); c.line([(14,40),(35,33)],'wood')
 c.poly([(36,30),(40,31),(40,35),(36,36),(34,33)],'wood')
 c.line([(37,31),(39,33),(37,35)],'woodglint')
 c.poly([(10,38),(13,40),(12,43),(9,42),(8,40)],'wood')
 c.px(10,40,'woodglint')
 # Asymmetrical dancing flame: golden outer edge never cream.
 c.poly([(23,4),(26,9),(26,14),(32,11),(32,18),(36,23),(35,29),(31,34),(24,37),(17,35),(12,30),(12,25),(16,20),(17,13),(21,16),(23,11)],'emberdark')
 c.poly([(23,5),(25,10),(24,17),(30,14),(29,19),(34,25),(33,30),(29,34),(23,35),(17,33),(14,29),(15,24),(19,20),(19,16),(22,19),(25,17)],'orange')
 c.poly([(22,14),(23,20),(28,19),(27,23),(30,27),(29,31),(25,34),(20,33),(17,29),(18,25),(21,22)],'flame')
 c.poly([(23,24),(24,28),(26,27),(27,31),(24,34),(20,32),(20,29)],'hot')
 c.line([(14,25),(14,29),(18,33)],'ember')
 c.px(31,24,'flame'); c.px(24,10,'flame')
 c.rect((8,20,9,21),'ember');c.px(36,16,'orange');c.px(18,7,'ember')
 c.line([(18,36),(29,36)],'ember'); c.px(29,38,'orange'); c.px(17,38,'ember')
 return c.save('node_campfire')

def shop():
 c=C()
 # Balance scale: central brass shaft and hanging bowls, no enclosing sign.
 c.poly([(20,40),(28,40),(34,43),(34,45),(14,45),(14,43)],'bronze')
 c.poly([(21,40),(27,40),(31,42),(17,42)],'brass')
 c.line([(17,43),(31,43)],'gold'); c.line([(15,45),(33,45)],'brassdark')
 c.rect((22,13,25,39),'brassdark');c.rect((22,14,23,39),'gold');c.line([(24,18),(24,38)],'brass')
 c.poly([(20,38),(27,38),(29,40),(19,40)],'bronze'); c.line([(20,38),(26,38)],'gold')
 c.poly([(21,8),(24,4),(27,8),(25,12),(23,13)],'brass')
 c.line([(24,5),(22,8),(23,10)],'glint');c.px(25,9,'brassdark')
 c.rect((7,12,40,14),'bronze');c.line([(7,11),(40,11)],'gold'); c.line([(8,12),(39,12)],'brass')
 c.rect((7,10,8,15),'brass');c.px(7,10,'glint'); c.rect((39,10,40,15),'brassdark'); c.px(39,10,'gold')
 c.rect((21,11,26,15),'brass'); c.line([(22,11),(25,11)],'glint'); c.px(23,13,'teal')
 # Left bowl hangs a little lower, side chains are 1px.
 c.line([(10,15),(6,29)],'gold');c.line([(10,15),(15,29)],'brass')
 c.line([(36,15),(31,27)],'gold');c.line([(36,15),(41,27)],'brassdark')
 c.poly([(3,29),(18,29),(16,33),(12,35),(8,35),(4,32)],'bronze')
 c.line([(3,29),(18,29)],'glint');c.poly([(5,30),(16,30),(14,32),(8,32)],'gold')
 c.line([(8,34),(13,34)],'brass')
 c.poly([(28,27),(44,27),(42,31),(38,33),(34,33),(30,30)],'bronze')
 c.line([(28,27),(44,27)],'gold');c.poly([(30,28),(42,28),(40,30),(33,30)],'brass')
 c.line([(33,28),(39,28)],'glint');c.line([(35,32),(39,32)],'brassdark')
 # Small specular marks in dark plates
 c.px(5,30,'gold');c.px(30,28,'gold');c.px(22,21,'glint');c.px(23,34,'glint')
 return c.save('node_shop')

def workshop():
 c=C()
 # Stone foot and stool, thin copper rims.
 c.poly([(14,35),(32,35),(39,40),(25,45),(7,40)],'night')
 c.poly([(14,35),(31,35),(36,39),(25,42),(10,39)],'slate')
 c.line([(14,35),(30,35),(35,38)],'blue')
 c.poly([(10,39),(25,42),(36,39),(36,42),(25,46),(10,42)],'navy')
 c.line([(11,39),(24,43),(35,40)],'brass')
 c.line([(14,42),(23,45)],'bronze')
 # Anvil top horn and body, sloped facets and visible metal wear.
 c.poly([(5,21),(15,16),(32,16),(36,21),(33,25),(29,26),(27,32),(31,35),(14,36),(17,31),(17,27),(11,26)],'navy')
 c.poly([(5,21),(15,17),(31,17),(34,20),(14,23)],'steel')
 c.line([(6,21),(15,17),(30,17)],'ice')
 c.poly([(14,23),(34,20),(34,24),(28,26),(25,31),(18,31),(19,26)],'slate')
 c.poly([(5,22),(14,24),(18,26),(16,28),(11,27)],'blue')
 c.line([(14,24),(31,21)],'blue')
 c.poly([(19,28),(25,27),(24,32),(28,34),(16,35),(20,31)],'blue')
 c.line([(17,35),(28,34)],'steel'); c.line([(19,28),(19,31)],'steel')
 c.px(22,20,'ice');c.line([(26,18),(28,18)],'ice')
 # Raised hammer, unmistakable diagonal tool, not part of anvil.
 c.poly([(29,8),(33,7),(42,20),(39,23)],'wooddark')
 c.poly([(30,8),(32,8),(41,20),(39,21)],'woodlit')
 c.line([(31,9),(38,19)],'gold')
 c.poly([(24,5),(31,3),(36,7),(34,12),(27,15),(22,11)],'navy')
 c.poly([(24,5),(30,4),(34,7),(28,10),(23,9)],'steel')
 c.line([(24,5),(30,4),(33,6)],'ice')
 c.poly([(23,9),(28,10),(27,14),(23,11)],'blue')
 c.poly([(28,10),(34,7),(33,11),(27,14)],'slate')
 c.line([(28,10),(31,9)],'brass')
 return c.save('node_workshop')

def treasure():
 c=C()
 # Three quarter jewel chest. Rounded lid, gold straps, readable central latch.
 c.poly([(7,18),(10,11),(17,7),(31,8),(40,14),(42,31),(37,38),(18,43),(7,36)],'plumdark')
 c.poly([(9,18),(12,12),(18,9),(30,10),(37,14),(39,21),(18,26)],'plum')
 c.poly([(11,15),(17,11),(30,12),(35,15),(16,20)],'violet')
 c.poly([(9,18),(16,21),(18,26),(8,23)],'plumdark')
 c.line([(11,13),(17,9),(30,10),(36,13)],'copper')
 c.line([(16,14),(31,14)],'plum')
 # lid metal straps follow the arch.
 c.poly([(14,11),(17,9),(21,10),(18,13),(16,18),(18,25),(15,24),(13,19)],'brassdark')
 c.line([(16,11),(18,10),(16,14),(15,18),(17,24)],'gold')
 c.poly([(29,10),(32,11),(36,15),(38,21),(35,22),(34,16)],'brass')
 c.line([(30,11),(33,13),(35,17),(36,21)],'glint')
 # box body, side darker and cool bounced light.
 c.poly([(8,25),(18,28),(40,23),(40,32),(36,36),(18,41),(8,35)],'plum')
 c.poly([(18,28),(39,24),(39,32),(35,35),(18,40)],'wood')
 c.poly([(9,26),(16,29),(16,39),(9,34)],'plumdark')
 c.line([(10,29),(15,31)],'violet');c.line([(10,33),(15,35)],'plum')
 c.line([(20,31),(36,27)],'woodlit'); c.line([(20,35),(35,31)],'wooddark')
 c.line([(20,38),(35,34)],'woodlit')
 c.poly([(17,27),(20,27),(20,40),(17,41)],'brassdark')
 c.line([(18,28),(18,39)],'gold')
 c.poly([(35,25),(38,24),(38,34),(35,36)],'brassdark')
 c.line([(36,26),(36,34)],'gold')
 c.line([(8,24),(18,28),(40,23)],'gold')
 c.line([(9,35),(18,41),(36,36)],'bronze')
 c.line([(18,42),(36,37)],'brassdark')
 # blue-glass lock in aged brass housing.
 c.poly([(25,25),(29,24),(31,27),(30,32),(26,33),(24,30)],'bronze')
 c.poly([(26,26),(29,25),(29,30),(26,31)],'gold')
 c.rect((27,27,28,29),'teal'); c.px(27,27,'foam')
 # feet and polished rivets
 c.rect((10,36,13,38),'bronze');c.rect((32,37,35,39),'bronze')
 for x,y in [(18,29),(18,37),(36,28),(36,32),(16,16)]:c.px(x,y,'glint')
 return c.save('node_treasure')

def boss():
 c=C()
 # Crown rises above a three-quarter night-stone altar.
 c.poly([(13,30),(29,27),(39,33),(36,40),(22,45),(9,38)],'night')
 c.poly([(13,30),(28,28),(36,32),(22,37),(10,34)],'blue')
 c.line([(14,30),(28,28),(34,31)],'steel')
 c.poly([(10,34),(22,37),(22,44),(11,38)],'slate')
 c.poly([(22,37),(36,32),(36,39),(22,44)],'navy')
 c.line([(11,35),(21,38)],'brassdark');c.line([(23,38),(34,34)],'brass')
 c.line([(13,38),(18,40)],'blue');c.line([(31,38),(34,37)],'slate')
 # Points and band: slender glowing brass, dark ruby glass.
 c.poly([(11,13),(16,17),(21,11),(24,4),(27,12),(32,17),(37,12),(35,29),(29,33),(18,33),(13,28)],'bronze')
 c.poly([(12,14),(17,20),(22,13),(24,7),(26,14),(31,21),(36,14),(33,27),(28,30),(19,30),(15,26)],'brass')
 c.poly([(13,15),(16,24),(19,27),(22,15),(24,9),(25,14),(22,28),(19,30),(15,26)],'gold')
 c.poly([(27,16),(31,22),(35,17),(33,26),(29,29)],'brassdark')
 c.line([(12,14),(16,19),(21,12),(24,5)],'glint')
 c.line([(26,12),(31,19),(36,14)],'gold')
 c.line([(14,27),(19,31),(28,31),(34,27)],'gold')
 c.line([(18,33),(28,33),(34,29)],'brassdark')
 c.poly([(22,22),(25,20),(28,24),(25,28),(22,26)],'copperdark')
 c.poly([(24,21),(26,23),(25,26),(23,25)],'ember')
 c.px(24,22,'rust')
 c.px(16,26,'glint');c.px(31,26,'gold');c.px(24,5,'glint')
 return c.save('node_boss')

NAMES=['node_battle','node_elite','node_unknown','node_campfire','node_shop','node_workshop','node_treasure','node_boss']
if __name__=='__main__':
 imgs=[battle(),elite(),unknown(),campfire(),shop(),workshop(),treasure(),boss()]
 for scale in [1,4]:
  cw=64*scale; ch=68*scale
  sheet=Image.new('RGB',(cw*4,ch*2),'#101a28'); d=ImageDraw.Draw(sheet)
  for i,(name,im) in enumerate(zip(NAMES,imgs)):
   x=(i%4)*cw+8*scale;y=(i//4)*ch+4*scale
   if scale==1: d.text((x,y+50),name.removeprefix('node_'),fill='#b7bfc9')
   else: d.text((x,y+50*scale),name,fill='#b7bfc9')
   sheet.paste(im.resize((48*scale,48*scale),Image.Resampling.NEAREST),(x,y),im.resize((48*scale,48*scale),Image.Resampling.NEAREST))
  sheet.save(ROOT/f'nodes_night_{scale}x.png')
 # Cross theme preview tests native transparency on pale steel and night glass.
 sheet=Image.new('RGB',(8*64,2*64),'#101a28')
 for row,bg in enumerate(['#101a28','#aab7c4']):
  ImageDraw.Draw(sheet).rectangle((0,row*64,511,row*64+63),fill=bg)
  for col,im in enumerate(imgs): sheet.paste(im,(col*64+8,row*64+8),im)
 sheet.save(ROOT/'nodes_background_test.png')
 qa=[]
 for name,im in zip(NAMES,imgs):
  f=OUT/(name+'.png'); alpha=im.getchannel('A'); bb=alpha.getbbox(); colors=set(im.get_flattened_data()); visible={v[:3] for v in colors if v[3]}
  qa.append({'file':f.name,'size':list(im.size),'mode':im.mode,'alphaValues':sorted(set(alpha.get_flattened_data())),'bbox':list(bb),'marginsLTRB':[bb[0],bb[1],48-bb[2],48-bb[3]],'opaqueColors':len(visible),'sha256':hashlib.sha256(f.read_bytes()).hexdigest()})
 (ROOT/'qa.json').write_text(json.dumps({'method':'Direct integer-coordinate drawing on final 48x48 canvas; no source raster resampling','palette':P,'assets':qa},indent=2))
