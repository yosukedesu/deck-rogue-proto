#!/usr/bin/env python3
"""Verify the authored native file and reproduce its exact PNG bytes."""
import hashlib,json,subprocess,sys
from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parents[1]
asset=root/'Art/cards/blue_mode_tide_crossroad.png'
img=Image.open(asset)
assert img.size==(80,48),img.size
assert img.mode=='RGB',img.mode
source=(root/'source/draw_tide_crossroad.py').read_text()
assert all(s not in source for s in ['Image.open','.resize(','.resample(','.filter('])
before=asset.read_bytes()
subprocess.run([sys.executable,str(root/'source/draw_tide_crossroad.py')],check=True)
assert before==asset.read_bytes(),'rerender not byte-identical'
manifest=json.loads((root/'manifest.json').read_text())
assert manifest['output']['sha256']==hashlib.sha256(before).hexdigest()
assert manifest['output']['colors']==len(img.getcolors(1<<24))
print('PASS: 80x48 RGB, no source image/resampling, palette and checksum, deterministic render')
