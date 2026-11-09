#!/usr/bin/env python3
"""Validate every handoff file hash and all import dimensions/format."""
from pathlib import Path
from PIL import Image
import hashlib,json
root=Path(__file__).resolve().parent.parent
count=0
for line in (root/'SHA256SUMS').read_text().splitlines():
    expected,name=line.split('  ',1)
    p=root/name
    assert p.is_file(),p
    assert hashlib.sha256(p.read_bytes()).hexdigest()==expected,p
    count+=1
m=json.loads((root/'manifest.json').read_text())
for a in m['assets']:
    with Image.open(root/a['file']) as im:
        assert im.size==(400,224) and im.mode=='RGB' and im.format=='PNG',a['id']
print(f"Validated {count} hashes and {len(m['assets'])} import PNGs")
