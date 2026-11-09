"""Reproduce C1 target PNGs only. Pillow 11+, numpy. Source bytes stay unchanged."""
from pathlib import Path
from PIL import Image
import numpy as np, json, hashlib
ROOT=Path(__file__).resolve().parents[1]
SIZE=(960,270)
# One common source-camera crop: x scale and y scale exactly match (960/2172).
BOX=(0.,48.,2172.,658.875)

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def seam(a):
    # Only the outer 16px strips; sub-8/255 correction. Center 928px untouched.
    a=a.copy().astype(np.float64)
    edge=(a[:,0,:3]+a[:,-1,:3])/2
    for k in range(16):
        w=(1+np.cos(np.pi*k/15))/2
        a[:,k,:3] += w*(edge-a[:,0,:3]) if k else 0
        a[:,-1-k,:3] += w*(edge-a[:,-1,:3]) if k else 0
    # Explicit shared endpoint to eliminate rounding asymmetry.
    a[:,0,:3]=edge; a[:,-1,:3]=edge
    return np.clip(np.round(a),0,255).astype(np.uint8)

def resample(im):
    # RGBa is Pillow premultiplied alpha; prevents hidden RGB from making halos.
    return im.convert('RGBA').convert('RGBa').resize(SIZE,Image.Resampling.LANCZOS,box=BOX).convert('RGBA')

results=[]
for act in (2,3):
  for i in range(3):
    src=ROOT/f'source/raw/act{act}/far_{i}_source.png'
    if act==2 and i==2: src=ROOT/'source/raw/act2/far_2_source_v2.png'
    dst=ROOT/f'assets/act{act}/far_{i}.png'
    im=Image.open(src); a=np.array(resample(im)); before=a.copy()
    if i==0: a=seam(a)
    else: a[a[:,:,3]==0,:3]=0
    Image.fromarray(a,'RGBA').save(dst,optimize=True)
    d=np.abs(a.astype(int)-before.astype(int))
    pm=a.astype(float); pm[:,:,:3]*=pm[:,:,3:4]/255
    edge=np.abs(pm[:,0]-pm[:,-1])
    nz=np.argwhere(a[:,:,3]>4)
    results.append(dict(id=f'act{act}_far_{i}',file=dst.relative_to(ROOT).as_posix(),source=src.relative_to(ROOT).as_posix(),source_sha256=sha(src),sha256=sha(dst),source_size=list(im.size),size=list(SIZE),mode='RGBA',crop_box_source=list(BOX),scale=960/2172,opaque=bool(np.all(a[:,:,3]==255)),alpha_nonzero_fraction=float(np.mean(a[:,:,3]>0)),alpha_partial_fraction=float(np.mean((a[:,:,3]>0)&(a[:,:,3]<255))),left_right_premultiplied_max=float(edge.max()),left_right_rgba_max=int(np.abs(a[:,0].astype(int)-a[:,-1].astype(int)).max()),post_resize_edit_rgb_max=int(d[:,:,:3].max()),post_resize_edit_rgb_mae=float(d[:,:,:3].mean()),visible_bbox_alpha_gt4=[int(nz[:,1].min()),int(nz[:,0].min()),int(nz[:,1].max()+1),int(nz[:,0].max()+1)]))
(ROOT/'qa/build_audit.json').write_text(json.dumps({'resampler':'Pillow RGBa Lanczos, source box in continuous coordinates','pillow_version':Image.__version__,'assets':results},indent=2)+'\n')
print(json.dumps(results,indent=2))
