#!/usr/bin/env python3
"""A8 original pixel artwork. Draw directly at delivery resolution; no input images.

Pillow is used as an integer pixel plotting surface, not to resample large art.
Only the separately labelled previews are nearest-neighbour enlarged.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageOps
import json, hashlib

ROOT = Path(__file__).resolve().parents[1]
P = {
    'glass0':'#17283f', 'glass1':'#24344d', 'glass2':'#344a66',
    'steel1':'#55718c', 'steel2':'#819eaf', 'steel3':'#bad2d9',
    'brass0':'#675236', 'brass1':'#a88147', 'brass2':'#cfaa63', 'brass3':'#dfc77f',
    'teal1':'#528c94', 'teal2':'#9fcaca',
    'rose0':'#663746', 'rose1':'#a64f5f', 'rose2':'#dc8290',
    'ember0':'#7e4142', 'ember1':'#c36843', 'ember2':'#e9a85b',
}
COL = {k: tuple(bytes.fromhex(v[1:]))+(255,) for k,v in P.items()}

def canvas(size): return Image.new('RGBA', size, (0,0,0,0))
def line(im, pts, color): ImageDraw.Draw(im).line(pts, fill=COL[color], width=1)
def poly(im, pts, fill, outline=None):
    ImageDraw.Draw(im).polygon(pts, fill=COL[fill] if fill else None, outline=COL[outline] if outline else None)
def dot(im, x, y, color): im.putpixel((x,y), COL[color])
def diamond(im, x, y, rx, ry, fill, outline=None):
    poly(im,[(x,y-ry),(x+rx,y),(x,y+ry),(x-rx,y)],fill,outline)
def save(im,path):
    target=ROOT/path; target.parent.mkdir(parents=True,exist_ok=True)
    im.save(target, optimize=False)
    return target

def cap():
    im=canvas((64,48))
    # A suspended piece of dark glass in an inward-opening brass hinge.
    poly(im,[(19,6),(28,15),(28,32),(19,41),(10,32),(10,15)],'glass0')
    poly(im,[(19,9),(25,16),(25,30),(19,37),(14,30),(14,16)],'glass1')
    line(im,[(19,11),(15,16),(15,29)],'steel1')
    line(im,[(19,37),(25,31),(25,24)],'steel1')
    # Rails do not reach the edge, leaving a two-pixel transparent bleed gutter.
    line(im,[(19,4),(27,4),(33,10),(47,10),(53,4),(61,4)],'brass1')
    line(im,[(19,43),(27,43),(33,37),(47,37),(53,43),(61,43)],'brass1')
    line(im,[(19,4),(5,18),(5,29),(19,43)],'brass2')
    line(im,[(19,8),(9,18),(9,29),(19,39)],'brass0')
    line(im,[(28,15),(35,22),(51,22),(56,17),(61,17)],'brass2')
    line(im,[(28,32),(35,25),(51,25),(56,30),(61,30)],'brass1')
    line(im,[(37,16),(43,16),(47,12)],'brass0')
    line(im,[(37,31),(43,31),(47,35)],'brass0')
    diamond(im,19,23,5,8,'glass2','brass2')
    diamond(im,19,23,2,4,'teal1')
    dot(im,19,20,'teal2'); dot(im,19,4,'brass3'); dot(im,5,23,'brass3')
    diamond(im,58,23,2,2,'brass2'); dot(im,58,22,'brass3')
    return im

def win():
    im=canvas((96,96))
    # Two slender laurel branches; paired leaves are pixel silhouettes, not blobs.
    left=canvas((96,96))
    line(left,[(45,83),(35,78),(26,68),(19,57),(15,43),(17,29),(25,18)],'brass1')
    leaves=[([(32,74),(27,65),(21,63),(22,70)],[(34,75),(36,66),(32,60),(30,67)]),
            ([(23,64),(17,57),(10,56),(14,63)],[(24,63),(29,55),(27,49),(23,54)]),
            ([(18,53),(11,47),(7,40),(14,43)],[(19,52),(25,44),(24,38),(19,42)]),
            ([(16,40),(10,34),(12,27),(16,33)],[(18,39),(25,32),(24,26),(20,30)]),
            ([(20,27),(18,20),(22,13),(23,20)],[(23,24),(29,20),(31,13),(26,17)])]
    for outer,inner in leaves:
        poly(left,outer,'brass1'); poly(left,inner,'brass2')
        line(left,[outer[0],outer[2]],'brass2')
        line(left,[inner[0],inner[2]],'brass3')
    im.alpha_composite(left); im.alpha_composite(ImageOps.mirror(left))
    # Faceted night-glass disc, outlined with a fine broken brass gear.
    poly(im,[(40,25),(55,25),(66,36),(66,53),(55,64),(40,64),(29,53),(29,36)],'glass0')
    poly(im,[(40,28),(55,28),(63,37),(63,52),(54,61),(40,61),(32,52),(32,37)],'glass1')
    line(im,[(35,36),(41,30),(53,30)],'steel1')
    line(im,[(62,44),(62,51),(54,59)],'steel1')
    # Four small mechanical teeth, separate from the laurel.
    for pts in [[(42,25),(42,22),(53,22),(53,25)],[(66,39),(69,39),(69,50),(66,50)],[(53,64),(53,67),(42,67),(42,64)],[(29,50),(26,50),(26,39),(29,39)]]:
        line(im,pts,'brass1')
    line(im,[(40,25),(55,25),(66,36),(66,53),(55,64),(40,64),(29,53),(29,36),(40,25)],'brass2')
    # A compass-star is the primary victory silhouette.
    pts=[(48,29),(52,39),(61,44),(52,48),(48,59),(43,48),(34,44),(43,39)]
    poly(im,pts,'brass2')
    poly(im,[(48,29),(48,44),(34,44),(43,39)],'brass3')
    poly(im,[(48,44),(52,48),(48,59)],'brass1')
    diamond(im,48,44,3,4,'glass0'); dot(im,48,42,'teal2')
    # The upward and lower finials leave the title area free of letters.
    diamond(im,48,13,3,5,'brass2'); dot(im,48,9,'brass3')
    line(im,[(43,17),(48,20),(53,17)],'brass0')
    line(im,[(31,79),(39,82),(44,82)],'brass2')
    line(im,[(64,79),(56,82),(52,82)],'brass2')
    diamond(im,48,83,3,5,'brass1'); dot(im,48,80,'brass3')
    return im

def lose():
    im=canvas((96,96))
    # Defeat is fractured night glass, not the superseded ink/paper treatment.
    # Main silhouette opens at the top and visibly sags toward the lower right.
    poly(im,[(26,20),(43,14),(42,29),(36,36),(43,46),(32,53),(20,47),(16,33)],'glass1')
    poly(im,[(26,20),(43,14),(32,29),(20,47),(16,33)],'glass2')
    line(im,[(26,20),(43,14),(42,29),(36,36),(43,46)],'steel2')
    line(im,[(26,20),(16,33),(20,47)],'steel1')
    line(im,[(29,22),(23,31),(22,36)],'steel3')
    poly(im,[(53,17),(69,23),(79,39),(73,53),(58,49),(52,39),(57,30)],'glass0')
    poly(im,[(69,23),(79,39),(73,53),(69,40)],'glass2')
    line(im,[(53,17),(69,23),(79,39),(73,53)],'steel1')
    line(im,[(53,17),(57,30),(52,39),(58,49)],'steel2')
    line(im,[(57,20),(62,22)],'steel3')
    # Cracked lower arc carries one interrupted brass memory of the victory seal.
    poly(im,[(19,57),(32,60),(45,53),(44,65),(39,70),(44,78),(29,76),(18,68)],'glass1')
    poly(im,[(56,57),(68,61),(76,57),(78,69),(69,81),(56,78),(51,68)],'glass2')
    line(im,[(19,57),(32,60),(45,53),(44,65),(39,70)],'steel1')
    line(im,[(56,57),(51,68),(56,78),(69,81),(78,69)],'steel2')
    line(im,[(22,65),(30,72),(37,72)],'brass0')
    line(im,[(59,74),(67,76),(73,68)],'brass1')
    line(im,[(42,31),(40,36),(47,46),(44,52)],'rose1')
    line(im,[(51,50),(49,57),(53,62)],'rose2')
    # Discrete falling shards, never a solid black blot or text background.
    poly(im,[(43,78),(48,82),(45,90),(41,85)],'steel1')
    line(im,[(43,78),(48,82)],'steel2')
    poly(im,[(60,85),(65,87),(62,92)],'glass2')
    dot(im,61,86,'steel1'); poly(im,[(83,48),(87,50),(84,54)],'steel1')
    dot(im,11,54,'steel1'); dot(im,17,77,'glass2'); dot(im,76,16,'steel1')
    return im

def petals():
    # One rotating petal per cell. Spawn/motion belongs to the consuming engine.
    shapes=[[(10,11),(17,9),(22,13),(23,17),(17,22),(12,20),(10,15)],
            [(12,10),(18,11),(22,16),(20,21),(14,22),(12,18)],
            [(15,9),(18,11),(19,18),(16,23),(14,20),(14,13)],
            [(17,10),(20,12),(18,20),(14,22),(12,19),(14,13)],
            [(20,10),(22,15),(20,21),(14,23),(10,19),(11,14),(15,10)],
            [(19,10),(21,16),(18,22),(13,21),(11,16),(14,12)],
            [(17,9),(19,13),(18,21),(15,23),(14,17),(15,11)],
            [(13,10),(17,11),(21,17),(20,21),(16,22),(11,16)]]
    frames=[]
    for i,pts in enumerate(shapes):
        im=canvas((32,32)); poly(im,pts,'rose1','rose0')
        if i in (0,4): poly(im,[pts[0],pts[1],(16,16),pts[-1]],'rose2')
        elif i in (1,5,7): line(im,[pts[0],(16,15),(17,19)],'rose2')
        else: line(im,[(16,12),(16,18)],'rose2')
        line(im,[(16,16),(17,20)],'rose0')
        dot(im,16,12,'rose2'); frames.append(im)
    return frames

def embers():
    frames=[]
    for i in range(8):
        im=canvas((32,32))
        shapes=[[(16,8),(18,14),(20,18),(16,22),(11,18),(15,13)],
                [(18,8),(18,14),(20,17),(17,22),(13,20),(13,16)],
                [(18,10),(20,15),(18,21),(14,22),(13,18)],
                [(18,12),(19,16),(17,22),(14,20),(14,17)],
                [(17,13),(19,17),(17,21),(14,19)],
                [(17,15),(18,18),(16,21),(14,19)],
                [(16,17),(18,19),(16,21),(15,19)],
                [(16,18),(17,19),(16,20),(15,19)]]
        poly(im,shapes[i],'ember0')
        if i<5:
            poly(im,[(16,13+i),(18,17),(16,20),(14,18)],'ember1')
            line(im,[(16,16+i//2),(16,19)],'ember2')
        elif i<7: dot(im,16,19,'ember1')
        frames.append(im)
    return frames

def strip(frames):
    im=canvas((len(frames)*32,32))
    for i,f in enumerate(frames): im.paste(f,(32*i,0))
    return im

def font(n):
    return ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',n)

def preview(main,pf,ef):
    bg='#101b2b'; panel='#202f42'; text='#a8bdc9'
    im=Image.new('RGB',(1100,900),bg); d=ImageDraw.Draw(im)
    d.text((28,20),'A8  /  NIGHT GLASS & BRASS',font=font(24),fill='#cfaa63')
    d.text((28,56),'Native pixel assets. Transparent PNGs. Display mockups only, not game screenshots.',font=font(14),fill=text)
    # Composition shows how the caps flank text without adding a solid frame.
    d.rectangle((28,92,1071,191), fill=panel)
    for key,x in [('banner_end_l',40),('banner_end_r',932)]:
        a=main[key].resize((128,96),Image.Resampling.NEAREST); im.paste(a,(x,94),a)
    d.text((401,124),'BOSS ENTRANCE',font=font(25),fill='#819eaf')
    d.text((28,203),'Caps 64 x 48 each  /  shown 2x  /  title is UI text, not baked artwork',font=font(14),fill=text)
    for col,(key,label) in enumerate([('win_emblem','VICTORY / intact star and laurel'),('lose_ink','DEFEAT / broken night-glass seal')]):
        x=28+col*544
        d.rectangle((x,244,x+515,562), fill=panel)
        a=main[key].resize((288,288),Image.Resampling.NEAREST); im.paste(a,(x+14,258),a)
        a=main[key]; im.paste(a,(x+365,332),a)
        d.text((x+361,442),'1x native',font=font(13),fill=text)
        d.text((x,575),label+'  /  96 x 96',font=font(14),fill=text)
    for row,(fs,label) in enumerate([(pf,'PETAL  /  8 unique views, seamless loop'),(ef,'EMBER  /  8 views, one shot then hide')]):
        y=627+row*120; d.text((28,y),label,font=font(14),fill=text)
        for i,f in enumerate(fs):
            x=30+i*132; d.rectangle((x,y+27,x+91,y+99),fill=panel)
            a=f.resize((64,64),Image.Resampling.NEAREST); im.paste(a,(x+14,y+31),a)
            d.text((x+98,y+52),str(i),font=font(12),fill=text)
    save(im,'previews/contactsheet.png')
    # Primary deliverables on three dark panels, always at native 1x.
    qa=Image.new('RGB',(880,440),bg); q=ImageDraw.Draw(qa)
    q.text((20,15),'1x NATIVE / DARK BACKGROUND CHECK',font=font(18),fill=text)
    for y,b in [(65,'#0f1120'),(188,'#20233a'),(311,'#2c3d4b')]:
        q.rectangle((16,y,863,y+112),fill=b); q.text((28,y+13),b,font=font(14),fill=text)
        xx=148
        for k in main:
            a=main[k]; qa.paste(a,(xx,y+(112-a.height)//2),a); xx+=155
        qa.paste(pf[0],(775,y+18),pf[0]); qa.paste(ef[0],(818,y+64),ef[0])
    save(qa,'previews/native_dark_backgrounds.png')
    anim=[]; durations=[]
    # Preview honours the manifest timings, with a quiet gap after the ember.
    ends=[70,140,220,300,390,480,580,680]
    events=sorted(set(list(range(0,1441,90))+ends+[1440]))
    for t,following in zip(events,events[1:]):
        a=Image.new('RGBA',(480,176),bg); dd=ImageDraw.Draw(a)
        dd.text((22,14),'PETAL: LOOP',font=font(15),fill=text)
        dd.text((263,14),'EMBER: ONCE',font=font(15),fill=text)
        ei=next((i for i,end in enumerate(ends) if t<end),None)
        for xx,f in [(55,pf[(t//90)%8]),(300,ef[ei] if ei is not None else canvas((32,32)))]:
            f=f.resize((96,96),Image.Resampling.NEAREST); a.alpha_composite(f,(xx,57))
        anim.append(a); durations.append(following-t)
    anim[0].save(ROOT/'previews/particles.apng',save_all=True,append_images=anim[1:],duration=durations,loop=0,disposal=0,blend=0)

def main():
    for p in ['Art/ui','Art/fx','previews','qa']: (ROOT/p).mkdir(parents=True,exist_ok=True)
    left=cap(); main={'banner_end_l':left,'banner_end_r':ImageOps.mirror(left),'win_emblem':win(),'lose_ink':lose()}
    for k,v in main.items(): save(v,'Art/ui/'+k+'.png')
    pf,ef=petals(),embers()
    save(strip(pf),'Art/fx/petal_8f.png'); save(strip(ef),'Art/fx/ember_8f.png')
    (ROOT/'palette.json').write_text(json.dumps(P,ensure_ascii=False,indent=2)+'\n')
    preview(main,pf,ef)
    assets=[]
    for k,v in main.items():
        path='Art/ui/'+k+'.png'
        assets.append({'id':k,'file':path,'size':list(v.size),'mode':'RGBA','pivot_top_left_px':[v.width/2,v.height/2],
                       'sha256':hashlib.sha256((ROOT/path).read_bytes()).hexdigest()})
    for k,loop,timings in [('petal_8f',True,[90]*8),('ember_8f',False,[70,70,80,80,90,90,100,100])]:
        path='Art/fx/'+k+'.png'
        assets.append({'id':k,'file':path,'size':[256,32],'mode':'RGBA','frame_size':[32,32],'frame_count':8,
            'layout':'horizontal-left-to-right','frame_index_base':0,'frame_rects':[[32*i,0,32,32] for i in range(8)],
            'pivot_top_left_px':[16,16],'frame_durations_ms':timings,'total_duration_ms':sum(timings),
            'loop':loop,'hide_after_last_frame':not loop,'translation_baked':False,
            'sha256':hashlib.sha256((ROOT/path).read_bytes()).hexdigest()})
    manifest={'schema_version':1,'bundle_id':'assets-a8-battle-ornaments','created_utc':'2026-11-08','row':'A8',
              'branch':'codex/assets-a8-20261108','reference_commit':'114ee430310c5a2df02ea048f23d1f8375c22c66',
              'native_pixel_art':True,'premultiplied_alpha':False,'alpha_values':[0,255],'filter':'nearest',
              'mipmaps':False,'auto_trim':False,'core_asset_count':6,'coordinate_system':'image top-left; x right; y down',
              'placement':{'banner_caps':{'size':[64,48],'inside_name_band':True,'minimum_band_height':48,
                   'minimum_blank_gap_to_text_px':8,'left_content_inner_edge_x':62,'right_content_inner_edge_x':2,
                   'scale':'1x or integer nearest; do not stretch horizontally','pair_relation':'exact horizontal mirrors',
                   'existing_main_name_band_heights':[54,62],'vertical_inset_at_these_heights':[3,7],
                   'existing_full_width_rails':'separate UI elements; no pixel-perfect join to those rails is assumed'},
                   'outcome_emblems':{'size':[96,96],'position':'beside or above outcome title, never behind the glyphs',
                   'minimum_blank_gap_to_text_px':12},
                   'particles':{'recommended_spawn_count_per_burst':[4,12],'single_particle_per_frame':True,
                   'world_motion':'apply drift/velocity to the whole 32x32 cell; pivot stays fixed',
                   'blend_mode':'normal-alpha, no additive glow required','petal_use':'gentle boss-clear celebration',
                   'ember_use':'short boss-clear accent, hide after frame 7'}},
              'legacy_name_note':'lose_ink.png retained for backlog filename compatibility; content is fractured night glass, not ink.',
              'decisions':'Size not prescribed for A8 in backlog; adopted sizes and import rules documented in README.',
              'runtime_integration_tested':False,'assets':assets}
    (ROOT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')

if __name__=='__main__': main()
