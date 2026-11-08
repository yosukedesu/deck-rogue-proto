#!/usr/bin/env python3
from pathlib import Path
from PIL import Image, __version__
import ast, hashlib, json, tempfile, shutil, subprocess, sys
R=Path(__file__).resolve().parents[1]
checks=[]
def check(name,ok,details=None):
 checks.append({'name':name,'pass':bool(ok),'details':details})
 if not ok: raise AssertionError(name)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
m=json.loads((R/'manifest.json').read_text());a=m['assets'][0]
p=R/a['file'];im=Image.open(p);colors=set(im.getdata())
check('target_dimensions',im.size==(400,224))
check('mode_and_opacity',im.mode=='RGB')
check('48_used_colors',len(colors)==48,len(colors))
check('asset_hash_matches_manifest',sha(p)==a['sha256'])
check('asset_bytes_match_manifest',p.stat().st_size==a['bytes'])
src=(R/'source/draw_workshop.py').read_text();tree=ast.parse(src)
check('source_syntax',True)
check('no_resize_blur_quantize_in_asset_source',not any(s in src for s in ['.resize(','.filter(','.quantize(','.transform(']))
check('no_reference_load_in_asset_source','Image.open(' not in src)
# Literal palette colors are used directly, no accidental blend colors.
node=next(n.value for n in tree.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='P' for t in n.targets))
pal=ast.literal_eval(node)
rgb={tuple(bytes.fromhex(v[1:])) for v in pal.values()}
check('all_pixels_from_fixed_palette',colors<=rgb)
listed=json.loads((R/'source/palette.json').read_text())
check('palette_file_matches_pixels',set(listed['colors'])=={'#%02x%02x%02x'%c for c in colors})
with tempfile.TemporaryDirectory() as t:
 root=Path(t);(root/'source').mkdir();shutil.copy2(R/'source/draw_workshop.py',root/'source/draw_workshop.py')
 subprocess.run([sys.executable,str(root/'source/draw_workshop.py')],check=True,capture_output=True)
 re=root/'png/bd_workshop.png'
 check('rebuild_pixel_exact',Image.open(re).tobytes()==im.tobytes())
 check('rebuild_png_byte_exact',re.read_bytes()==p.read_bytes())
prev=Image.open(R/'preview/workshop_preview_3x.png')
check('preview_dimensions',prev.size==(1200,672))
check('preview_exact_nearest_neighbor',prev.tobytes()==im.resize((1200,672),Image.Resampling.NEAREST).tobytes())
check('comparison_dimensions',Image.open(R/'qa/comparison_facilities.png').size==(816,512))
for r in m['references']:check('reference_unchanged_'+Path(r['file']).name,sha(R/r['file'])==r['sha256'])
check('expected_asset_count',len(list((R/'png').glob('*.png')))==1)
result={'status':'pass','passed':len(checks),'checks':checks,'runtimeVerified':False,'pillowVersion':__version__}
(R/'qa/verification.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({'status':'pass','checks':len(checks),'runtimeVerified':False}))
