#!/usr/bin/env python3
from pathlib import Path
from PIL import Image, ImageDraw
R=Path(__file__).resolve().parents[1]
(R/'preview').mkdir(exist_ok=True)
im=Image.open(R/'png/bd_workshop.png').convert('RGB')
im.resize((1200,672),Image.Resampling.NEAREST).save(R/'preview/workshop_preview_3x.png')
# References at native size, not enlarged to disguise their original dimensions.
sheet=Image.new('RGB',(816,512),'#080d1c'); d=ImageDraw.Draw(sheet)
entries=[('references/workshop.png',(8,30),'REFERENCE: workshop 240x132 (native)'),('png/bd_workshop.png',(408,30),'A4: workshop 400x224 (native)'),('references/bd_shop.png',(8,282),'REFERENCE: shop 400x224 (native)'),('references/bd_campfire.png',(408,282),'REFERENCE: campfire 400x224 (native)')]
for name,(x,y),label in entries:
 d.text((x,y-19),label,fill='#dfbe70')
 tile=Image.open(R/name).convert('RGB')
 if tile.width<400: sheet.paste(tile,(x+(400-tile.width)//2,y+(224-tile.height)//2))
 else:sheet.paste(tile,(x,y))
sheet.save(R/'qa/comparison_facilities.png')
