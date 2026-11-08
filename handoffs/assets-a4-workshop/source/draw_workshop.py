#!/usr/bin/env python3
"""Native 400 x 224 pixel illustration. Integer geometry, fixed palette, no resampling."""
from PIL import Image, ImageDraw
from pathlib import Path
import hashlib, json
ROOT=Path(__file__).resolve().parents[1]
P={
'void':'#080d1c','night':'#10192b','deep':'#111d31','blue0':'#17283c','blue1':'#20364a','blue2':'#2a475b','blue3':'#3b6072','blue4':'#648895',
'ink':'#11131e','wood0':'#221e27','wood1':'#322630','wood2':'#463037','wood3':'#5e3f3c','wood4':'#795244','wood5':'#9a6a4a','wood6':'#bc8859',
'stone0':'#20232e','stone1':'#2f3440','stone2':'#414851','stone3':'#566069','stone4':'#778086','stone5':'#9a9a88',
'warm0':'#3d3033','warm1':'#58433b','warm2':'#72583f','warm3':'#91754e','warm4':'#b49965',
'iron0':'#16232c','iron1':'#263843','iron2':'#3c535c','iron3':'#5b747a','iron4':'#8ba0a0',
'brass0':'#42352b','brass1':'#695032','brass2':'#96703c','brass3':'#bf9750','brass4':'#dfbe70','brass5':'#f4d992',
'fire0':'#623138','fire1':'#954338','fire2':'#ce5b35','fire3':'#ed873c','fire4':'#ffb954','fire5':'#ffe39b',
'cyan0':'#14353c','cyan1':'#225a61','cyan2':'#378b8e','cyan3':'#74c4bb','cyan4':'#c2e5cc',
'purple':'#454056','moss0':'#253f40','moss1':'#3a5650'
}
im=Image.new('RGB',(400,224),P['void']); d=ImageDraw.Draw(im)
def C(c): return P.get(c,c)
def poly(pts,c,outline=None): d.polygon(pts,fill=C(c),outline=C(outline) if outline else None)
def rect(box,c,outline=None): d.rectangle(box,fill=C(c),outline=C(outline) if outline else None)
def line(pts,c,w=1): d.line(pts,fill=C(c),width=w)
def dot(x,y,c): im.putpixel((x,y),tuple(bytes.fromhex(C(c)[1:])))
def ellipse(box,c,outline=None): d.ellipse(box,fill=C(c),outline=C(outline) if outline else None)
def stone(pts,base='stone1',top='stone3'):
 poly(pts,base,'ink')
 if len(pts)>=5:
  a,b,c=pts[:3];end=pts[-1]
  mid={'stone1':'stone2','stone2':'stone3','blue1':'blue2','warm1':'warm2','warm2':'warm3'}.get(base,base)
  poly([(a[0]+1,a[1]+1),(b[0],b[1]+1),(c[0]-1,c[1]+1),(c[0]-3,c[1]+4),(b[0],b[1]+3),(a[0]+3,a[1]+3)],mid)
  r=pts[3];bot=pts[4]
  poly([(c[0]-1,c[1]+3),(r[0]-1,r[1]),(bot[0],bot[1]-1),(bot[0],bot[1]-3),(r[0]-4,r[1]-2),(c[0]-3,c[1]+4)],'stone0' if 'blue' not in base else 'blue0')
  cx=sum(p[0] for p in pts)//len(pts);cy=sum(p[1] for p in pts)//len(pts)
  line([(cx-4,cy),(cx-2,cy),(cx-1,cy-1)],mid)
  line([(cx+2,cy+4),(cx+5,cy+4)],mid)
  if cx%3==0: line([(c[0]-2,c[1]+1),(c[0]-4,c[1]+4),(c[0]-3,c[1]+6)],'stone0')
 line(pts[:3],top)

def bolt(x,y): rect((x,y,x+2,y+2),'iron0');dot(x,y,'brass3')
def ring(x,y,r,c='brass2',inner='wood1'):
 ellipse((x-r,y-r,x+r,y+r),c,'ink');ellipse((x-r+2,y-r+2,x+r-2,y+r-2),inner)
def gear(x,y,r=9,face='brass2',rim='brass4'):
 # hand-placed cardinal/intercardinal teeth and a crisp annular wheel
 for dx,dy in [(0,-r), (0,r),(-r,0),(r,0),(-r+3,-r+3),(r-3,-r+3),(-r+3,r-3),(r-3,r-3)]:
  rect((x+dx-2,y+dy-2,x+dx+2,y+dy+2),'ink')
  rect((x+dx-1,y+dy-1,x+dx+1,y+dy+1),face)
 ellipse((x-r+2,y-r+2,x+r-2,y+r-2),face,'ink')
 line([(x-r+4,y-3),(x-r+2,y),(x-r+3,y+4)],rim)
 ellipse((x-3,y-3,x+3,y+3),'ink');ellipse((x-1,y-1,x+1,y+1),'iron2')
def bottle(x,y,w=7,h=13,c='cyan1'):
 rect((x+2,y,x+w-2,y+2),'brass1','ink');rect((x+2,y+3,x+w-2,y+5),c)
 poly([(x+2,y+4),(x,y+7),(x,y+h),(x+w,y+h),(x+w,y+7),(x+w-2,y+4)],c,'ink')
 line([(x+2,y+7),(x+1,y+9),(x+1,y+h-2)],'cyan3' if c=='cyan1' else 'brass3')
 line([(x+2,y+h-1),(x+w-2,y+h-1)],'cyan0' if c=='cyan1' else 'wood3')
def wood_board(box,base='wood2',highlight='wood4'):
 x0,y0,x1,y1=box;rect(box,base,'ink');line([(x0+1,y0+1),(x1-1,y0+1)],highlight)

def crate(x,y,w=30,h=26):
 poly([(x,y+4),(x+w-4,y),(x+w,y+3),(x+w,y+h-3),(x+4,y+h),(x,y+h-3)],'wood1','ink')
 rect((x+2,y+6,x+w-4,y+h-3),'wood3','ink')
 for xx in range(x+9,x+w-4,7): line([(xx,y+7),(xx,y+h-4)],'wood1')
 line([(x+3,y+6),(x+w-5,y+6)],'wood5');line([(x+3,y+7),(x+3,y+h-4)],'wood4')
 rect((x+1,y+10,x+w-3,y+13),'wood2','ink');line([(x+2,y+10),(x+w-4,y+10)],'wood5')
 rect((x+1,y+h-7,x+w-3,y+h-4),'wood2','ink');line([(x+2,y+h-7),(x+w-4,y+h-7)],'wood4')
 for xx in (x+3,x+w-6): bolt(xx,y+11);bolt(xx,y+h-6)

def flag(pts,base,hi):
 poly(pts,base,'stone0')
 a,b,c,e=pts
 poly([(e[0]+1,e[1]-1),(c[0]-1,c[1]-1),(c[0]-3,c[1]-3),(e[0]+3,e[1]-3)],'stone1' if base.startswith('stone') else 'warm0')
 line([a,b],hi);line([(a[0]+1,a[1]+1),(e[0]+1,e[1]-2)],hi)
 line([(b[0],b[1]+1),(c[0]-1,c[1]-2)],'stone0')


# Night outside the stone shelter: deliberate broad masses, not texture noise.
poly([(0,47),(31,26),(62,31),(76,71),(57,154),(13,200),(0,199)],'night')
poly([(340,36),(374,18),(399,46),(399,193),(368,205),(342,160)],'deep')
poly([(0,224),(0,208),(25,195),(55,186),(352,184),(399,205),(399,224)],'night')
# Small forest opening at left, behind the room.
poly([(18,166),(30,143),(25,143),(36,121),(31,122),(42,91),(51,121),(47,120),(58,142),(52,141),(64,163)],'blue0')
poly([(2,175),(14,152),(10,152),(24,119),(30,152),(26,152),(40,176)],'deep')
# Architectural outer silhouette and floor.
poly([(48,32),(73,21),(329,24),(352,38),(365,164),(385,205),(368,215),(34,215),(14,207),(37,166)],'ink')
poly([(53,41),(337,39),(345,165),(47,170)],'wood0')
# Rear timber plank wall; restrained wood-grain segments.
for y0,y1 in [(42,58),(60,76),(78,94),(96,112),(114,130),(132,150),(152,163)]:
 wood_board((57,y0,338,y1),'wood1','wood3')
 for x,n in [(68,27),(110,16),(158,29),(205,35),(257,20),(304,23)]:
  yy=y0+5+(x//10)%3;line([(x,yy),(x+n,yy)],'wood2')
 for x in [95,181,270]:
  if (y0//18+x)%2: line([(x,y0+1),(x,y1-1)],'wood0')
# Selected timber knots and offset grain clusters follow the board lengths.
for x,y,n in [(78,45,18),(160,46,38),(204,48,25),(283,45,22),(81,65,16),(215,68,23),(278,84,17),(89,86,21),(145,83,15),(198,89,23),(105,107,18),(185,119,27),(277,122,25),(294,139,21)]:
 line([(x,y),(x+n,y)],'wood3');line([(x+3,y+2),(x+n-7,y+2)],'wood2')
for x,y in [(112,48),(217,82),(240,119),(92,150),(315,64)]:
 line([(x-4,y),(x-2,y-1),(x+3,y-1),(x+5,y),(x+3,y+1),(x-2,y+1),(x-4,y)],'wood0')
 line([(x-8,y+2),(x-3,y+2),(x+2,y+3),(x+9,y+2)],'wood3')
# Wall posts.
for x in [62,129,263,334]:
 rect((x,39,x+8,163),'wood1','ink');line([(x+1,41),(x+1,160)],'wood4');line([(x+5,47),(x+5,153)],'wood2')
 for y in [47,145]:bolt(x+3,y)
# Cool floor, warm tiles under furnace. Each slab is individually laid in perspective.
poly([(49,157),(341,157),(380,207),(363,216),(33,216),(18,207)],'stone0')
rows=[(159,169,44,348,38),(171,184,35,358,47),(186,201,25,371,57),(203,214,30,366,65)]
for row,(yt,yb,xs,xe,step) in enumerate(rows):
 for j,x in enumerate(range(xs,xe,step)):
  right=min(x+step-2,xe)
  warm=x>248
  base=(['stone1','stone2','stone1','stone2'][j%4] if not warm else ['warm1','warm2','warm1','warm2'][j%4])
  hi='stone3' if not warm else 'warm3'
  pts=[(x+6,yt),(right-3,yt),(right+4,yb-2),(x,yb)]
  flag(pts,base,hi)
  if (j+row)%3==0:line([(x+step//2,yt+2),(x+step//2-2,yt+4),(x+step//2+3,yt+5)],'stone0')
  if (j+row)%3==1:line([(x+10,yb-3),(x+19,yb-3)],'stone3' if not warm else 'warm3')
# Quiet foreground vignette silhouette around floor, no gradient or resampling.
poly([(0,206),(16,202),(24,210),(43,216),(365,216),(382,206),(399,205),(399,223),(0,223)],'void')
line([(43,217),(97,217),(103,219),(303,219),(311,217),(364,217)],'night')
# Roof heavy beams and mounting ties.
poly([(47,31),(64,23),(338,27),(351,34),(344,44),(52,43)],'wood2','ink')
line([(54,32),(333,31),(345,35)],'wood5');line([(55,35),(330,35)],'wood3');line([(59,39),(145,39)],'wood4')
for x in [81,147,250,320]:
 poly([(x,27),(x+6,27),(x+7,42),(x+1,42)],'iron1','ink');line([(x+1,29),(x+1,39)],'iron3');bolt(x+3,31)
# Night-glass window, fractured ruin outside visible through it.
rect((76,48,123,99),'ink');rect((78,50,121,97),'brass1');rect((81,53,118,94),'blue0')
poly([(82,54),(117,54),(117,76),(82,88)],'blue1')
poly([(84,64),(88,56),(90,65),(94,71),(91,70),(98,83),(83,88)],'deep')
poly([(103,73),(109,59),(114,73),(111,72),(118,86),(118,94),(98,94)],'night')
line([(83,55),(100,55)],'blue3');line([(84,57),(95,57)],'blue2')
rect((98,52,100,95),'brass0');line([(98,53),(98,94)],'brass3')
rect((81,73,118,75),'brass0');line([(81,73),(117,73)],'brass2')
line([(79,51),(120,51),(120,96)],'brass3');line([(81,97),(121,97)],'brass2')
rect((75,98,125,102),'wood1','ink');line([(77,99),(122,99)],'wood5')
# Window cool light caught on the work surface rather than glowing bloom.
poly([(79,105),(119,105),(146,129),(68,129)],'wood2')
# Hanging brass rail, orderly hand tools.
rect((144,52,253,56),'ink');rect((145,53,251,54),'brass3');line([(147,55),(250,55)],'brass1')
for x in [145,250]:bolt(x,52)
for x in [153,169,188,207,227,242]:
 line([(x,56),(x,61),(x+2,63),(x+4,61),(x+4,58)],'ink');line([(x+1,57),(x+1,61),(x+3,62)],'brass3')
# Wrench.
line([(155,65),(155,84)],'ink',5);line([(155,65),(155,84)],'iron2',3)
poly([(151,67),(150,63),(152,59),(153,64),(157,64),(158,59),(160,62),(159,67),(157,69),(153,69)],'iron2','ink')
line([(151,64),(152,66),(154,67)],'iron4');ellipse((153,82,157,87),'iron2','ink');dot(155,84,'ink')
# Hammer.
line([(171,66),(171,88)],'ink',5);line([(171,66),(171,87)],'wood4',3);line([(170,69),(170,84)],'wood6')
poly([(164,65),(166,62),(178,62),(180,65),(178,70),(165,70)],'iron2','ink');line([(166,63),(177,63)],'iron4')
# Long tongs, two unequal legs.
line([(189,64),(188,76),(185,86)],'ink',4);line([(190,64),(192,76),(195,86)],'ink',4)
line([(189,65),(189,74),(186,85)],'iron3');line([(191,65),(191,75),(194,85)],'iron2');dot(190,73,'brass3')
# Gear keys.
line([(209,65),(209,84)],'ink',4);line([(208,65),(208,83)],'brass3',2);ring(209,66,4,'brass3');rect((209,80,213,83),'brass2','ink')
# File.
rect((227,64,230,73),'wood5','ink');poly([(227,72),(230,72),(230,91),(226,91)],'iron2','ink')
for yy in range(76,90,3):line([(227,yy),(229,yy+1)],'iron3')
# Small hand saw.
poly([(242,65),(247,65),(249,68),(247,75),(243,75),(241,71)],'wood4','ink');rect((244,68,246,71),'wood0')
poly([(242,75),(247,75),(242,92),(239,92)],'iron2','ink')
for yy in [78,82,86,90]:dot(243-(yy-78)//4,yy,'iron4')
# Shelf, bottles, small mechanical parts.
rect((143,109,255,114),'wood1','ink');line([(144,110),(253,110)],'wood5')
poly([(148,114),(157,114),(151,123)],'wood2','ink');poly([(244,114),(253,114),(249,123)],'wood2','ink')
bottle(147,94,7,14);bottle(158,98,6,10,'wood4');bottle(169,92,8,16)
rect((184,100,201,108),'iron1','ink');line([(185,101),(199,101)],'iron3')
gear(191,101,5,'brass2','brass4')
for x,y in [(210,103),(216,101),(222,102)]:rect((x,y,x+4,108),'brass1','ink');line([(x+1,y+1),(x+3,y+1)],'brass3')
rect((235,97,248,108),'wood2','ink');line([(236,98),(247,98)],'wood5');bolt(238,101);bolt(243,101)
# Forge chimney & hood, old hand-laid stonework.
poly([(283,37),(315,37),(321,81),(277,81)],'stone0','ink')
for yy,left,right in [(39,284,314),(49,283,316),(60,281,317),(71,279,319)]:
 rect((left,yy,right,yy+8),'stone1','ink');line([(left+1,yy+1),(right-1,yy+1)],'stone3')
 line([(left+13+(yy%3)*2,yy+1),(left+13+(yy%3)*2,yy+7)],'stone0')
# Copper flue along right.
line([(323,47),(331,47),(335,52),(335,96)],'ink',7);line([(323,47),(330,47),(334,52),(334,96)],'brass1',5)
line([(323,45),(331,45),(336,51),(336,94)],'brass3')
for yy in [62,81]:rect((331,yy,337,yy+3),'iron1','ink');line([(332,yy+1),(336,yy+1)],'iron3')
# Forge body silhouette and arched opening.
poly([(279,74),(317,74),(329,85),(331,157),(269,157),(269,88)],'stone0','ink')
poly([(280,84),(285,78),(311,78),(319,84),(324,98),(324,143),(277,143),(277,98)],'warm1','ink')
poly([(284,91),(292,84),(307,84),(315,91),(319,102),(319,142),(282,142),(282,103)],'fire0','ink')
poly([(291,97),(298,92),(308,96),(315,107),(315,139),(286,139),(286,108)],'wood0')
# Arched voussoirs explicitly placed, warm inner edge.
for pts in [ [(276,88),(282,78),(289,82),(283,94)],[(283,78),(292,73),(297,82),(289,86)],[(293,74),(305,74),(306,83),(297,84)],[(306,75),(316,79),(311,88),(306,84)],[(317,81),(324,91),(317,98),(312,89)],[(324,93),(329,103),(320,106),(317,100)] ]:
 stone(pts,'stone2','stone4')
for yy in [103,115,128,141]:
 stone([(270,yy),(280,yy-1),(280,yy+11),(269,yy+12)],'stone2','stone4')
 stone([(320,yy),(329,yy-1),(330,yy+12),(320,yy+12)],'warm2','warm4')
 line([(280,yy),(280,yy+10)],'fire3')
 line([(319,yy),(319,yy+10)],'fire3')
# Flame is crisp tiers and separated tongues, not alpha glow.
poly([(286,137),(287,128),(292,119),(291,110),(297,117),(302,102),(303,115),(309,109),(307,122),(313,126),(314,138)],'fire2')
poly([(289,138),(291,129),(296,124),(297,116),(301,124),(306,116),(305,128),(311,132),(309,139)],'fire3')
poly([(293,138),(295,130),(299,127),(300,119),(302,130),(307,134),(305,139)],'fire4')
poly([(297,137),(299,132),(302,133),(304,138)],'fire5')
for x,y in [(296,105),(310,117),(288,119)]:rect((x,y,x,y+2),'fire3')
# Coals/grate.
rect((282,140,319,145),'iron0','ink')
for xx in range(285,318,5):rect((xx,140,xx+2,142),'fire2');dot(xx+1,140,'fire4')
line([(283,144),(318,144)],'iron3')
poly([(266,149),(331,149),(334,157),(266,159)],'iron1','ink');line([(269,150),(329,150)],'iron4')
rect((272,160,328,168),'stone1','ink');line([(273,161),(326,161)],'warm3')
# Firewood below forge, not modern powered machinery.
for x,y in [(278,158),(290,158),(301,158),(310,158)]:
 line([(x,y),(x+6,y+4)],'wood4',4);dot(x,y,'wood6')
# Bench front: deep shadow below legs.
ellipse((58,163,244,188),'stone0')
for x in [77,215]:
 poly([(x,140),(x+10,140),(x+8,181),(x-2,181)],'wood2','ink');line([(x+1,143),(x,179)],'wood5')
 rect((x-2,172,x+8,175),'iron1','ink');bolt(x+1,173)
# Low shelf and front plank.
poly([(79,163),(222,162),(225,168),(77,170)],'wood2','ink');line([(79,164),(220,163)],'wood4')
rect((77,141,225,154),'wood2','ink');line([(79,142),(224,142)],'wood5')
line([(89,149),(113,149)],'wood3');line([(168,147),(199,147)],'wood4')
# Drawer.
rect((123,144,174,153),'wood1','ink');line([(125,145),(172,145)],'wood4');ring(149,149,2,'brass3')
# Worktop dimensional lip.
poly([(71,129),(226,127),(239,134),(235,141),(66,142),(66,136)],'wood3','ink')
line([(73,130),(224,129),(235,134)],'wood6');line([(67,137),(234,136)],'wood5')
line([(79,133),(118,132)],'wood4');line([(180,132),(218,131)],'wood5');line([(73,140),(128,140)],'wood2')
# Blueprint is an engraved dark steel tablet, deliberately not paper.
poly([(106,127),(132,126),(141,132),(112,134)],'iron1','ink')
line([(110,128),(129,128),(135,131),(115,132),(110,128)],'iron3')
line([(116,129),(121,131),(127,129)],'cyan2');dot(123,130,'cyan3')
# Brass mechanism / card-combination apparatus resting on bench.
poly([(148,119),(172,118),(183,127),(181,133),(145,134),(139,128)],'iron1','ink')
line([(147,120),(170,119),(180,127)],'iron3');line([(144,131),(179,130)],'brass2')
gear(153,126,7);gear(170,126,5,'brass1','brass3')
poly([(158,121),(158,112),(161,109),(167,109),(170,113),(170,121)],'brass1','ink')
ellipse((158,109,169,120),'cyan0','ink');ellipse((160,111,167,118),'cyan2');line([(162,111),(165,111)],'cyan4');dot(161,112,'cyan4');dot(167,117,'cyan1')
line([(159,118),(162,122),(167,122),(170,118)],'brass3')
# Fine bench scratches and chipped top edge.
for x,y in [(78,139),(103,136),(177,137),(211,138)]:
 line([(x,y),(x+6,y)],'wood6');dot(x+8,y,'wood4')
line([(89,138),(94,138),(95,139),(103,139)],'wood2')
line([(192,134),(209,133)],'wood2')
# Loose tools and ore on top.
line([(186,128),(206,126)],'ink',4);line([(187,127),(206,125)],'wood5',2)
poly([(202,122),(208,120),(212,125),(207,129),(203,127)],'iron2','ink');line([(204,122),(208,121)],'iron4')
poly([(84,123),(89,116),(95,118),(97,125),(91,128),(84,127)],'blue2','ink');line([(88,118),(89,123),(95,124)],'blue4')
poly([(94,126),(99,121),(104,123),(106,128),(98,130)],'blue1','ink');line([(99,122),(101,126)],'cyan2')
bottle(73,110,7,18)
# Bench vise at right, sturdy dark metal.
rect((222,129,229,148),'iron1','ink');line([(223,130),(223,145)],'iron3')
rect((215,125,231,131),'iron1','ink');rect((216,122,221,127),'iron2','ink');rect((225,121,231,126),'iron2','ink')
line([(217,123),(220,123)],'iron4');line([(226,122),(230,122)],'iron4')
line([(228,138),(237,138)],'iron3',2);line([(237,133),(237,144)],'ink',3);line([(237,134),(237,143)],'brass3')
# Supplies under workbench, distinct silhouettes.
crate(89,154,27,23)
rect((128,158,149,165),'iron0','ink');line([(130,159),(147,159)],'iron3');line([(134,158),(134,164)],'iron2')
rect((155,157,175,165),'wood1','ink');line([(157,158),(172,158)],'wood4')
for xx in [159,165,171]:rect((xx,154,xx+2,158),'brass2','ink')
# Anvil with recognizable horn, waist, hardy hole, steel planes.
ellipse((232,178,276,194),'stone0')
poly([(238,163),(261,161),(269,169),(269,186),(259,192),(236,188),(233,170)],'wood2','ink')
ellipse((235,159,267,173),'wood3','ink');ellipse((239,161,263,170),'wood4')
line([(244,162),(244,169),(260,169)],'wood5');line([(240,174),(240,185)],'wood4');line([(249,176),(250,188)],'wood1');line([(262,174),(262,185)],'wood1')
rect((233,179,269,182),'iron1','ink');line([(234,180),(267,180)],'iron3');bolt(238,180);bolt(263,180)
poly([(230,145),(260,144),(271,148),(280,148),(275,153),(262,155),(259,160),(263,164),(263,167),(237,167),(237,164),(242,160),(240,154),(230,153),(223,148)],'iron1','ink')
poly([(226,147),(232,145),(260,145),(269,149),(278,149),(272,152),(239,152)],'iron3')
line([(231,146),(258,146),(266,149)],'iron4');line([(242,156),(257,156)],'iron2');line([(240,165),(261,165)],'iron3')
rect((249,146,252,148),'iron0');line([(259,152),(260,154)],'brass2')
# Tongs leaning against stump.
line([(276,162),(274,173),(277,186)],'ink',4);line([(280,162),(276,173),(281,184)],'ink',4)
line([(276,163),(275,172),(278,185)],'iron3');line([(280,163),(277,173),(281,184)],'iron2');dot(276,173,'brass3')
# Coal bucket and shovel near forge.
poly([(323,175),(337,175),(335,189),(325,189)],'iron1','ink');ellipse((322,172,338,178),'iron0','ink')
line([(325,174),(325,168),(328,165),(333,165),(336,169),(336,174)],'iron2')
for pts in [[(324,175),(328,171),(331,175)],[(329,176),(333,172),(336,175)],[(326,177),(330,174),(332,178)]]:poly(pts,'stone2','ink')
line([(325,180),(326,187)],'iron3');line([(332,181),(333,186)],'iron2')
line([(346,138),(342,177)],'ink',4);line([(346,139),(342,176)],'wood5',2)
poly([(338,174),(347,175),(345,184),(340,188),(337,182)],'iron1','ink');line([(339,176),(339,181),(342,185)],'iron3')
# Foreground crate and a spare gear give recognizable workshop use.
crate(57,172,36,28);gear(80,175,10,'brass1','brass3')
poly([(59,171),(59,164),(65,161),(71,166),(73,173)],'stone2','ink');line([(62,164),(65,167),(66,172)],'blue4')
poly([(64,172),(70,164),(76,167),(77,174)],'blue1','ink');line([(70,165),(71,171)],'cyan2')
# Vertical stone entry piers framing the old workshop, all stones hand-defined.
left_stones=[[(37,41),(48,30),(57,33),(58,49),(48,57),(36,54)],[(31,57),(44,52),(53,59),(50,76),(37,80),(28,70)],[(30,80),(44,77),(53,84),(50,100),(34,103),(25,94)],[(26,105),(39,100),(51,108),(48,122),(32,128),(23,120)],[(24,129),(37,125),(48,132),(45,150),(29,154),(20,146)],[(22,157),(34,150),(46,157),(45,174),(30,180),(18,172)],[(20,182),(32,176),(45,184),(44,200),(30,207),(14,198)]]
for i,pts in enumerate(left_stones):
 stone(pts,'blue1' if i<4 else 'stone1','blue3' if i<4 else 'stone3')
 if i%2==0:line([(pts[0][0]+6,pts[0][1]+6),(pts[0][0]+10,pts[0][1]+5)],'blue2')
right_stones=[[(344,34),(356,28),(367,37),(368,54),(355,60),(344,51)],[(348,60),(361,55),(372,62),(372,79),(358,83),(347,76)],[(349,85),(364,80),(376,90),(374,106),(359,109),(348,101)],[(351,111),(365,107),(377,116),(375,132),(359,136),(349,128)],[(351,137),(365,134),(378,142),(378,159),(361,165),(350,156)],[(353,167),(365,160),(380,169),(382,186),(365,192),(353,184)],[(357,194),(371,188),(385,196),(383,208),(368,214),(355,205)]]
for i,pts in enumerate(right_stones):
 stone(pts,'stone1' if i<3 else 'warm1','stone3' if i<3 else 'warm3')
 if i%2:line([(pts[0][0]+5,pts[0][1]+3),(pts[0][0]+9,pts[0][1]+9)],'stone0')
# Uneven lintel rocks.
for pts in [[(50,26),(67,16),(84,18),(89,28),(75,33),(59,32)],[(86,19),(108,14),(130,18),(129,26),(112,31),(91,27)],[(279,18),(302,14),(319,20),(315,29),(297,29),(282,27)],[(318,21),(337,19),(352,26),(349,35),(331,32)]]:
 stone(pts,'stone1','stone3')
# Roof moss: sparse cool clusters, never noise over the subject.
for pts in [[(58,24),(68,18),(75,19),(72,23),(64,24),(62,29)],[(104,17),(115,14),(123,17),(118,19),(109,19)],[(335,22),(342,24),(345,30),(339,29)],[(22,151),(29,152),(31,156),(26,158),(22,157)]]:
 poly(pts,'moss0');line(pts[:2],'moss1')
# Hanging lantern attached left pier. Warm accents limited to the local pool.
line([(52,41),(52,59),(57,59)],'ink',3);line([(52,42),(52,58),(56,58)],'brass2')
ring(58,62,3,'brass2','wood0')
poly([(52,66),(64,66),(67,71),(65,86),(51,86),(49,71)],'ink')
poly([(52,70),(63,70),(63,82),(53,83)],'fire3')
poly([(55,71),(60,71),(60,82),(55,81)],'fire4');rect((57,72,59,79),'fire5')
poly([(51,66),(64,66),(66,70),(50,70)],'brass2','ink');line([(53,67),(62,67)],'brass4')
rect((50,83,65,87),'brass1','ink');line([(52,84),(63,84)],'brass3')
line([(52,70),(52,82)],'brass1');line([(63,70),(63,82)],'brass1');line([(57,70),(57,82)],'brass1')
# Accent hits on closest masonry, nothing is blurred.
line([(46,82),(49,87),(48,94)],'warm3');line([(45,62),(49,65),(48,71)],'warm4')
# Few grounded chips, clearly clustered at foreground corners.
for pts,c in [([(102,190),(108,188),(112,191),(109,193),(103,193)],'stone3'), ([(116,202),(121,200),(125,202),(122,204)],'stone2'), ([(297,195),(302,193),(307,195),(305,198),(298,198)],'warm2'), ([(313,205),(317,204),(321,207),(315,208)],'stone2')]:
 poly(pts,c,'stone0');line(pts[:2],'stone4' if c!='warm2' else 'warm3')
# Small chips and shallow bevels on exposed paving, never indiscriminate noise.
for x,y in [(141,181),(192,182),(145,195),(207,193),(275,206),(336,196),(63,207),(174,210)]:
 line([(x,y),(x+6,y),(x+7,y-1)],'stone3');line([(x+1,y+1),(x+4,y+1)],'stone0')
for x,y in [(312,173),(299,181),(347,187)]:
 line([(x,y),(x+5,y)],'warm4');line([(x+2,y+1),(x+7,y+1)],'warm1')
# Final export: the image is authored exactly at target dimensions.
(ROOT/'png').mkdir(exist_ok=True)
im.save(ROOT/'png/bd_workshop.png',optimize=False)
if __name__=='__main__': print(ROOT/'png/bd_workshop.png')
