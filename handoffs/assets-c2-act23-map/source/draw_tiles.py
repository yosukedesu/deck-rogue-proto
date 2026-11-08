#!/usr/bin/env python3
"""C2: six original 64 px map tiles, authored at native integer coordinates.

No input images, resizing, palette remapping, antialiasing, or image editing.
Periodic coordinate drawing and a common 3 px boundary establish mixed-tile
compatibility. All marks use a small authored palette and low contrast.
"""
from pathlib import Path
from PIL import Image, ImageDraw
import random, sys

OUT=Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).resolve().parents[1]/'png'
OUT.mkdir(parents=True,exist_ok=True)
PAL={
 2:['#2d3539','#30383c','#333b3f','#363e42','#394245','#3e4749','#454d4c'],
 3:['#2b3947','#2e3c4a','#303e4c','#33414f','#374652','#3c4b57','#46545e'],
}
def rgb(s): return tuple(int(s[i:i+2],16) for i in (1,3,5))+(255,)
def tile(act,variant,path=False):
 p=[rgb(x) for x in PAL[act]]
 im=Image.new('RGBA',(64,64),p[2 if not path else 3]);d=ImageDraw.Draw(im)
 def poly(points,idx,periodic=False):
  for ox in ([-63,0,63] if periodic else [0]):
   for oy in ([-63,0,63] if periodic else [0]):
    d.polygon([(x+ox,y+oy) for x,y in points],fill=p[idx])
 def line(points,idx,periodic=False):
  for ox in ([-63,0,63] if periodic else [0]):
   for oy in ([-63,0,63] if periodic else [0]):
    d.line([(x+ox,y+oy) for x,y in points],fill=p[idx],width=1)
 # Shared low-frequency boundary shape, not a blank framing strip.
 poly([(-8,5),(2,3),(12,7),(17,13),(11,18),(-4,17)],3,True)
 poly([(43,-6),(57,-3),(68,4),(61,11),(50,9),(42,3)],1 if not path else 2,True)
 poly([(56,30),(68,25),(73,38),(66,46),(57,43),(53,37)],3,True)
 poly([(17,56),(26,52),(36,57),(38,68),(24,70),(15,64)],3,True)
 boundary=im.copy()
 if act==2:
  # Compressed earth: large oblique patches, short buried grit, no noise fill.
  maps=[[
   ([(10,22),(19,17),(30,19),(34,25),(26,30),(14,29)],3),
   ([(35,13),(47,14),(53,20),(49,27),(39,28),(33,23)],1),
   ([(12,37),(24,32),(34,38),(31,45),(20,49),(9,44)],1),
   ([(33,40),(44,33),(54,37),(53,49),(41,52),(34,47)],3),
  ],[
   ([(12,14),(21,10),(30,15),(27,23),(17,27),(10,22)],1),
   ([(35,25),(45,18),(53,22),(55,31),(45,37),(34,34)],3),
   ([(16,36),(26,29),(35,35),(33,47),(24,53),(16,48)],3),
   ([(38,46),(49,40),(56,46),(52,55),(41,54)],1),
  ]]
  for pts,col in maps[variant]:poly(pts,min(col+(1 if path else 0),5))
  # Hand-placed and short: these read as strata, not equal-spaced pixels.
  marks=[[(16,25),(21,24),(25,25)],[(39,19),(44,19)],[(20,43),(24,42)],[(41,47),(46,45)]] if variant==0 else [[(16,19),(21,17)],[(42,29),(47,28),(50,29)],[(22,40),(27,39)],[(44,50),(48,49)]]
  for pts in marks:line(pts,4 if path else 3)
  # Tiny connected gravel fragments, not white speckles.
  gravel=[(30,13),(12,51),(49,32),(33,54)] if not variant else [(32,21),(13,33),(38,51),(51,13)]
  for x,y in gravel:
   poly([(x,y),(x+3,y-1),(x+5,y+1),(x+3,y+2),(x,y+1)],2 if path else 1)
   line([(x+1,y),(x+3,y)],4)
  if path:
   poly([(16,19),(28,16),(36,20),(33,27),(21,29),(14,25)],3)
   poly([(31,38),(41,33),(49,37),(46,45),(35,46)],3)
   line([(24,42),(30,41),(35,42)],4)
 else:
  # Ancient paving: broad broken plates with sand-filled discontinuous joints.
  plates=[
   [(5,7),(20,6),(23,13),(20,21),(6,23),(4,16)],
   [(25,7),(39,6),(42,15),(39,23),(25,22)],
   [(44,12),(58,13),(60,27),(47,29),(42,23)],
   [(8,26),(24,25),(30,29),(28,40),(8,42),(6,34)],
   [(32,27),(43,26),(46,34),(43,43),(32,42)],
   [(47,33),(57,32),(60,44),(55,53),(47,51)],
   [(8,46),(23,44),(28,51),(25,58),(10,57)],
   [(31,47),(43,46),(45,55),(40,59),(30,57)],
  ]
  cols=[3,2,3,3,2,3,2,3] if variant==0 else [2,3,3,2,3,2,3,2]
  for n,(pts,col) in enumerate(zip(plates,cols)):
   poly(pts,min(col+(1 if path else 0),4))
   x,y=pts[0];line([(x+2,y),(x+6,y)],4 if not path else 5)
   # Only fragments of the joints remain dark: no horizontal full-row bars.
   if n%3==variant:line([pts[-2],pts[-1]],1 if not path else 2)
  fractures=[[(16,10),(15,13),(18,16)],[(49,17),(52,21),(50,24)],[(15,31),(18,33),(17,37)],[(35,51),(37,54)]]
  if variant: fractures=[[(29,12),(31,15),(30,18)],[(36,33),(39,35)],[(13,49),(16,52),(15,55)]]
  for pts in fractures:line(pts,2 if not path else 3)
  # Dust half-buries joints and separates chipped stone from a tiled checker.
  poly([(20,21),(27,22),(29,27),(23,29),(17,27)],3)
  poly([(39,39),(47,40),(48,46),(42,49),(37,45)],3)
 # Same coordinate-authored boundary on every family member. It is part of the
 # drawing contract, not post-processing of an existing image or a resample.
 for y in range(64):
  for x in range(64):
   if min(x,y,63-x,63-y)<3: im.putpixel((x,y),boundary.getpixel((x,y)))
 return im

def main():
 for a in (2,3):
  for v,k in enumerate(('ground_a','ground_b')):tile(a,v).save(OUT/f'act{a}_{k}.png')
  tile(a,0,True).save(OUT/f'act{a}_path.png')
if __name__=='__main__':main()
