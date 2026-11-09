"""Offline native-resolution alpha composites; not a game/GPU capture."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import numpy as np, json, subprocess
ROOT=Path(__file__).resolve().parents[1]; QA=ROOT/'qa'
W,H=960,270
SPEED=[.04,.12,.21]; OPACITY={2:[1,.65,.18],3:[1,.55,.82]}
LAYERS={a:[Image.open(ROOT/f'assets/act{a}/far_{i}.png').convert('RGBA') for i in range(3)] for a in (2,3)}
FONT=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',14)
def shifted(im,delta):
    # wrap through 3 repeats, then Lanczos pixel phase in premultiplied-alpha space
    n=delta%W; big=Image.new('RGBA',(W*3,H))
    for x in range(3):big.paste(im,(x*W,0))
    return big.convert('RGBa').resize((W,H),Image.Resampling.LANCZOS,box=(W+n,0,W*2+n,H)).convert('RGBA')
def compose(act,cam=0,ground=False,omit=None):
    out=Image.new('RGBA',(W,H),(7,16,26,255))
    for i,im in enumerate(LAYERS[act]):
        if i==omit:continue
        s=shifted(im,cam*SPEED[i])
        s.putalpha(s.getchannel('A').point(lambda x:round(x*OPACITY[act][i])))
        out=Image.alpha_composite(out,s)
    if ground:
        # Uniform diagnostic occlusion, not production terrain or a new asset.
        ImageDraw.Draw(out).rectangle((0,248,W,H),fill=(13,22,28,255))
    return out.convert('RGB')
def tile(rows, path):
    sheet=Image.new('RGB',(W,len(rows)*(H+26)),(12,20,30));d=ImageDraw.Draw(sheet)
    for j,(label,im) in enumerate(rows):
        y=j*(H+26);d.text((8,y+4),label,font=FONT,fill=(196,177,133));sheet.paste(im,(0,y+26))
    sheet.save(QA/path)
rows=[]
checker=np.indices((H,W)).sum(axis=0) # replace with 16px checker
checker=((np.indices((H,W))[0]//16+np.indices((H,W))[1]//16)%2)
c=np.zeros((H,W,4),dtype=np.uint8);c[:,:,:3]=np.where(checker[:,:,None],np.array([30,44,60]),np.array([44,58,74]));c[:,:,3]=255
for a in (2,3):
    tile([(f'ACT {a} | composite / no terrain',compose(a)),(f'ACT {a} | diagnostic terrain occludes y >= 248',compose(a,ground=True))],f'act{a}_composite.png')
    for i,im in enumerate(LAYERS[a]):
        rows.append((f'ACT {a} / FAR {i} | 960 x 270 | opacity 1 | checker = transparency',Image.alpha_composite(Image.fromarray(c),im).convert('RGB')))
    tile([(f'ACT {a} | camera X = {p} | y >= 248 occluded',compose(a,p,True)) for p in [-4800,-960,-1,0,960,4571.428571,8000]],f'act{a}_motion_samples.png')
    tile([(f'ACT {a} FAR {i} | join at center x=480 / native scale',Image.alpha_composite(Image.fromarray(c),shifted(im,480)).convert('RGB')) for i,im in enumerate(LAYERS[a])],f'act{a}_seam_native.png')
    # 240 frames, 12 seconds: continuous 240 camera pixels/sec, near layer 50.4px/sec.
    frames=[compose(a,3600+n*12,True) for n in range(240)]
    subprocess.run(['ffmpeg','-y','-v','error','-f','rawvideo','-pixel_format','rgb24','-video_size','960x270','-framerate','20','-i','pipe:0','-c:v','libx264','-crf','17','-pix_fmt','yuv420p','-movflags','+faststart',str(QA/f'act{a}_parallax.mp4')],input=b''.join(x.tobytes() for x in frames),check=True)
    (QA/f'act{a}_motion_frames').mkdir(exist_ok=True)
    for k in [0,1,79,80,81,159,160,161,239]:frames[k].save(QA/f'act{a}_motion_frames/frame_{k:03}.png')
    diffs=[float(np.abs(np.array(frames[n]).astype(float)-np.array(frames[n-1]).astype(float)).mean()) for n in range(1,len(frames))]
    metrics={'renderer':'Pillow CPU / sRGB alpha composite, not game/GPU','frames':len(frames),'fps':20,'duration_seconds':12,'camera_range':[3600,3600+239*12],'camera_pixels_per_second':240,'layer_pixels_per_second':[x*240 for x in SPEED],'opacity':OPACITY[a], 'ground_diagnostic_occlusion_y':248,'consecutive_frame_mae_mean':float(np.mean(diffs)),'consecutive_frame_mae_max':float(max(diffs)), 'all_frames_dimensions':all(x.size==(W,H) for x in frames),'video_end_note':'Linear test pans restart at the end; endpoint jump is not an asset seam.'}
    # Each layer 1px before/after its own periodic phase. Out-of-bounds none.
    checks=[]
    for i,im in enumerate(LAYERS[a]):
        x=np.array(shifted(im,0));y=np.array(shifted(im,W))
        checks.append({'layer':i,'period_px':W,'frame_at_0_equals_at_960':bool(np.array_equal(x,y)),'offset_959_to_0_is_one_pixel_roll':bool(np.array_equal(np.roll(np.array(shifted(im,959)),-1,axis=1),x))})
    metrics['wrap_tests']=checks
    full=np.array(compose(a)).astype(float)
    if a==2:
        nofog=np.array(compose(a,omit=2)).astype(float)
        delta=np.abs(full-nofog);metrics['fog_composite_contribution']={'max_channel_delta':float(delta.max()),'nonzero_pixels':int(np.count_nonzero(np.max(delta,axis=2)>0)),'mean_channel_delta':float(delta.mean())}
        tile([('ACT 2 | fog OFF',compose(a,omit=2)),('ACT 2 | fog ON (source alpha x 0.18)',compose(a))],'act2_fog_ab.png')
    (QA/f'act{a}_motion_measurements.json').write_text(json.dumps(metrics,indent=2)+'\n')
tile(rows,'layers_native.png')
print('QA rendered: composites, six layer previews, seam rolls, 2 x 240 frames, wrap measurements.')
