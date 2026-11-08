#!/usr/bin/env python3
"""Validate A6 assets; does not change artwork."""
from pathlib import Path
from PIL import Image
import hashlib, json, tempfile, shutil, subprocess, sys

ROOT=Path(__file__).resolve().parents[1]
manifest=json.loads((ROOT/'manifest.json').read_text())
palette={tuple(bytes.fromhex(v[1:])) for v in json.loads((ROOT/'palette.json').read_text()).values()}
checks=[]; detail=[]
def test(name,condition):
    checks.append({'name':name,'pass':bool(condition)})
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()

files=sorted((ROOT/'png').glob('*.png'))
test('exactly_14_pngs',len(files)==14)
test('unique_14_ids',len({a['id'] for a in manifest['assets']})==14)
test('manifest_files_match_disk', {a['file'] for a in manifest['assets']}=={p.relative_to(ROOT).as_posix() for p in files})
atlas=Image.open(ROOT/'status_atlas.png')
test('atlas_size_mode',atlas.size==(224,64) and atlas.mode=='RGBA')
for a in manifest['assets']:
    p=ROOT/a['file']; im=Image.open(p); px=list(im.getdata()); alpha=im.getchannel('A'); vals=set(alpha.getdata()); box=alpha.getbbox()
    test(a['id']+'_32x32_rgba',im.size==(32,32) and im.mode=='RGBA')
    test(a['id']+'_binary_alpha',vals=={0,255})
    test(a['id']+'_transparent_border',box[0]>=1 and box[1]>=1 and box[2]<=31 and box[3]<=31)
    test(a['id']+'_visible',sum(v==255 for v in alpha.getdata())>=40)
    test(a['id']+'_palette',all(tuple(v[:3]) in palette for v in px if v[3]))
    test(a['id']+'_sha',sha(p)==a['sha256'])
    x,y,w,h=a['atlas_rect']; test(a['id']+'_atlas_equal',atlas.crop((x,y,x+w,y+h)).tobytes()==im.tobytes())
    detail.append({'id':a['id'],'size':list(im.size),'mode':im.mode,'alpha_values':sorted(vals),'opaque_pixels':sum(v==255 for v in alpha.getdata()),'bbox':list(box),'visible_colors':len({tuple(v[:3]) for v in px if v[3]}),'sha256':sha(p)})
test('all_images_unique',len({a['sha256'] for a in manifest['assets']})==14)
with tempfile.TemporaryDirectory(prefix='a6-reproduce-') as td:
    copy=Path(td)/'assets'; shutil.copytree(ROOT,copy)
    for p in (copy/'png').glob('*.png'): p.unlink()
    subprocess.run([sys.executable,str(copy/'source/draw_icons.py')],check=True)
    test('reproduction_14_byte_exact',all(sha(copy/a['file'])==a['sha256'] for a in manifest['assets']))
    test('reproduction_atlas_byte_exact',sha(copy/'status_atlas.png')==sha(ROOT/'status_atlas.png'))
report={'result':'pass' if all(c['pass'] for c in checks) else 'fail','checks_passed':sum(c['pass'] for c in checks),'checks_total':len(checks),'checks':checks,'images':detail,'runtime_integration_tested':False,'visual_qa':'See qa/visual_review.md; technical checks do not prove semantic recognition.'}
(ROOT/'qa/verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({k:report[k] for k in ('result','checks_passed','checks_total')},ensure_ascii=False))
if report['result']!='pass': raise SystemExit(1)
