#!/usr/bin/env python3
from pathlib import Path
from PIL import Image,ImageColor,ImageSequence
import json,hashlib,subprocess,sys
R=Path(__file__).resolve().parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def run():
    m=json.loads((R/'manifest.json').read_text());pal={ImageColor.getrgb(v)+(255,) for v in json.loads((R/'palette.json').read_text()).values()};items=[]
    for a in m['animations']+m['ui_assets']:
        p=R/a['path'];im=Image.open(p);assert im.mode=='RGBA';assert list(im.size)==a['size_px'];assert sha(p)==a['sha256']
        cs=set(im.getdata());assert {c[3] for c in cs}<={0,255};assert all(c in pal for c in cs if c[3]);assert all(c==(0,0,0,0) for c in cs if not c[3]);items.append({'path':a['path'],'dimensions':im.size,'color_count_including_transparent':len(cs),'sha256':sha(p)})
    a=m['animations'][0];im=Image.open(R/a['path']);seen=set();total=0;boxes=[]
    for f in a['frames']:
        i=f['index'];x,y,w,h=f['rect_px'];assert (x,y,w,h)==(i*64,0,64,64);assert f['start_ms']==total;total+=f['duration_ms'];cell=im.crop((x,y,x+w,y+h));bb=cell.getbbox();assert (list(bb) if bb else None)==f['bbox_local_px'];assert cell.crop((15,15,49,49)).getbbox() is None
        assert cell.tobytes() not in seen;seen.add(cell.tobytes());boxes.append(bb)
        if bb:assert min(bb[0],bb[1],64-bb[2],64-bb[3])>=6
    assert total==550;assert boxes[-1] is None;assert a['frames'][-1]['icon_offset_px']==[0,0] and a['frames'][-1]['icon_warm_mix_weight']==0
    for p in m['ui_assets']:
        plate=Image.open(R/p['path']);assert set(plate.crop((4,3,20,11)).getdata())=={ImageColor.getrgb('#17283f')+(255,)}
    timing=json.loads((R/'previews/playback_timing.json').read_text());apng=Image.open(R/'previews/relics_playback.png');decoded=[]
    for f in ImageSequence.Iterator(apng):decoded.append(round(f.info['duration']));assert f.size==(320,180)
    assert decoded==[f['duration_ms'] for f in timing['frames']];assert sum(decoded)==timing['total_ms'];assert len(decoded)==10
    assert [f['production_frame_index'] for f in timing['frames'][1:9]]==list(range(8));assert [f['duration_ms'] for f in timing['frames'][1:9]]==[f['duration_ms'] for f in a['frames']]
    generated=list((R/'Art').rglob('*.png'))+list((R/'previews').glob('*'))+[R/'manifest.json']
    before={str(p.relative_to(R)):sha(p) for p in generated}
    subprocess.run([sys.executable,str(R/'source/draw_relics.py')],check=True);subprocess.run([sys.executable,str(R/'source/build_manifest.py')],check=True)
    assert before=={str(p.relative_to(R)):sha(p) for p in generated}
    result={'result':'pass','production_assets':items,'checks':{'rgba8':True,'binary_alpha':True,'transparent_rgb_zero':True,'declared_palette_only':True,'ring_8_distinct_frames':True,'min_margin_px':6,'icon_34x34_aperture_clear':True,'last_frame_transparent':True,'counter_text_area_blank':True,'timing_550_ms':True,'icon_restored_at_end':True,'apng_all_10_frames_decode_and_durations_match':True,'rebuild_byte_identical':True},'limitations':['No game integration or runtime playback verified.','Visual review is separate qa/visual_review.md.','Source fixtures and original game icons are not distributed.']}
    (R/'qa/verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps(result,ensure_ascii=False))
if __name__=='__main__':run()
