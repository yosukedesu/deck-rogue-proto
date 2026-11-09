#!/usr/bin/env python3
"""Reproduce only the import PNGs from preserved originals. Run from anywhere."""
from pathlib import Path
from PIL import Image
import hashlib, json
root=Path(__file__).resolve().parent.parent
manifest=json.loads((root/'manifest.json').read_text())
for asset in manifest['assets']:
    source=root/asset['sourceFile']
    assert hashlib.sha256(source.read_bytes()).hexdigest()==asset['source']['sha256'],source
    with Image.open(source) as im:
        out=root/asset['file']
        out.parent.mkdir(parents=True,exist_ok=True)
        im.convert('RGB').resize((400,224),Image.Resampling.BOX).save(out,optimize=True)
    assert hashlib.sha256(out.read_bytes()).hexdigest()==asset['sha256'],(out,'Use requirements.txt Pillow version for byte-identical files')
print('All import PNGs reproduced and SHA-256 matched')
