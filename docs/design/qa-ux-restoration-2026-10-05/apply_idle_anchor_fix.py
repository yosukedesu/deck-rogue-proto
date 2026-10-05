"""Reproduce the minimal fix from the unchanged original sprite export (Pillow only).
Usage: python apply_idle_anchor_fix.py ORIGINAL_EXPORT_DIR NEW_OUTPUT_DIR
Never writes to the source directory. Refuses unexpected/already-fixed inputs.
"""
from pathlib import Path
import argparse, hashlib
from PIL import Image

OLD = {
    'leader_blue_idle_0.png': '250a0ba8eaccfac6acf043fc93fe8ecd9c5d37b0443212d34fce993a006a425c',
    'leader_blue.png': '250a0ba8eaccfac6acf043fc93fe8ecd9c5d37b0443212d34fce993a006a425c',
    'leader_blue_atlas.png': 'd788f8428a34067ca92603ae9ce16628ba0ce5f5611ec481f514a45aeaa0ac8f',
}
NEW = {
    'leader_blue_idle_0.png': '0c5314f5e89b28666ec4648d843fa7c6b2ef80c9c6ebff3378892d4b1359d8ad',
    'leader_blue.png': '0c5314f5e89b28666ec4648d843fa7c6b2ef80c9c6ebff3378892d4b1359d8ad',
    'leader_blue_atlas.png': 'f5e35068bb27209b2aa14ca49f2104540d6a19f6c7bf35285ccb2a791ad668ff',
}
p = argparse.ArgumentParser(description=__doc__)
p.add_argument('input_dir', type=Path)
p.add_argument('output_dir', type=Path)
a = p.parse_args()
if a.input_dir.resolve() == a.output_dir.resolve():
    raise SystemExit('Source and output directories must differ.')
for name, expected in OLD.items():
    if hashlib.sha256((a.input_dir/name).read_bytes()).hexdigest() != expected:
        raise SystemExit(f'Unexpected or already-fixed source: {name}')
a.output_dir.mkdir(parents=True, exist_ok=True)
original = Image.open(a.input_dir/'leader_blue_idle_0.png').convert('RGBA')
assert original.size == (128,84)
assert original.getchannel('A').crop((0,0,8,84)).getbbox() is None
fixed = Image.new('RGBA', original.size, original.getpixel((0,0)))
fixed.paste(original.crop((8,0,128,84)), (0,0))
for name in ['leader_blue_idle_0.png', 'leader_blue.png']:
    fixed.save(a.output_dir/name)
atlas = Image.open(a.input_dir/'leader_blue_atlas.png').convert('RGBA')
assert atlas.size == (512,336)
cell = Image.new('RGBA', fixed.size)
cell.alpha_composite(fixed)
atlas.paste(cell, (0,0))
atlas.save(a.output_dir/'leader_blue_atlas.png')
for name in NEW:
    expected = Image.open(Path(__file__).parent/name).convert('RGBA') if (Path(__file__).parent/name).exists() else None
    output = Image.open(a.output_dir/name).convert('RGBA')
    if expected is not None:
        assert expected.size == output.size and expected.tobytes() == output.tobytes(), name
    print(name, hashlib.sha256((a.output_dir/name).read_bytes()).hexdigest())
print('Done: idle_0 translated -8 px on X only; default sprite and first atlas cell synchronized.')
