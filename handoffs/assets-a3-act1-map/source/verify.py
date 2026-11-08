#!/usr/bin/env python3
"""Validate A3 dimensions, transparency, margins, IDs and reproducible native draw."""
from pathlib import Path
from PIL import Image, ImageDraw
import hashlib, json, subprocess, sys, tempfile
ROOT=Path(__file__).resolve().parents[1]
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
LM=['map_gate_act1','map_boss_enemy_brute','map_boss_enc_kin_ritual','map_boss_enemy_haze_stag']
ND=['node_battle','node_elite','node_unknown','node_campfire','node_shop','node_workshop','node_treasure','node_boss']
NAMES=LM+ND

def sheet():
 w,h=512,284
 out=Image.new('RGB',(w,h),'#101a28');d=ImageDraw.Draw(out)
 d.text((8,5),'ACT 1 | NATIVE PIXEL ASSETS | NIGHT GLASS + BRASS',fill='#c3aa72')
 for i,name in enumerate(LM):
  im=Image.open(ROOT/'png'/f'{name}.png');x=16+i*128
  out.paste(im,(x,23),im); d.text((x,121),['GATE','BRUTE DEN','RITUAL','HAZE GROVE'][i],fill='#a8bec7')
 for i,name in enumerate(ND):
  im=Image.open(ROOT/'png'/f'{name}.png'); x=8+i*64
  out.paste(im,(x,158),im);d.text((x,212),name.removeprefix('node_'),fill='#c3aa72')
 d.text((8,249),'96x96 landmarks / 48x48 nodes. Native shown; no smoothing.',fill='#8498a5')
 d.text((8,265),'Final game placement, labels and interaction are not tested.',fill='#8498a5')
 out.save(ROOT/'qa'/'contactsheet_native.png')
 out.resize((1536,852),Image.Resampling.NEAREST).save(ROOT/'qa'/'contactsheet_3x.png')
 # two backgrounds at native scale, never the source of an asset
 out=Image.new('RGB',(4*104,4*104),'#101a28');d=ImageDraw.Draw(out)
 for row,bg in enumerate(['#101a28','#91a1af']):
  d.rectangle((0,row*208,415,row*208+207),fill=bg)
  for i,name in enumerate(LM):
   im=Image.open(ROOT/'png'/f'{name}.png');out.paste(im,(i*104+4,row*208+4),im)
  for i,name in enumerate(ND):
   im=Image.open(ROOT/'png'/f'{name}.png');out.paste(im,((i%4)*104+28,row*208+106+(i//4)*50),im)
 out.save(ROOT/'qa'/'background_test_native.png')
 # Old 32px icons are shown at their own native size beside new 48px icons.
 out=Image.new('RGB',(8*72,112),'#101a28');d=ImageDraw.Draw(out)
 for i,name in enumerate(ND):
  old=Image.open(ROOT/'references'/f'{name}.png');new=Image.open(ROOT/'png'/f'{name}.png')
  out.paste(old,(i*72+20,4),old);out.paste(new,(i*72+12,45),new);d.text((i*72+2,98),name.removeprefix('node_'),fill='#b8a36e')
 out.resize((1152,224),Image.Resampling.NEAREST).save(ROOT/'qa'/'old_new_nodes_2x.png')

def main():
 manifest=json.loads((ROOT/'manifest.json').read_text()); tests=[]; assets=[]
 def check(name,v): tests.append({'name':name,'pass':bool(v)}); assert v,name
 check('exact_final_asset_count',sorted(p.stem for p in (ROOT/'png').glob('*.png'))==sorted(NAMES))
 check('manifest_ids_match', sorted(x['id'] for x in manifest['assets'])==sorted(NAMES))
 for name in NAMES:
  p=ROOT/'png'/f'{name}.png'; im=Image.open(p); expected=96 if name in LM else 48;alpha=im.getchannel('A');bb=alpha.getbbox();vals=sorted(set(alpha.getdata()));cols=len({c for c in im.getdata() if c[3]});margin=[bb[0],bb[1],expected-bb[2],expected-bb[3]]
  check(name+'_dimensions', im.size==(expected,expected));check(name+'_rgba',im.mode=='RGBA');check(name+'_binary_alpha',vals==[0,255]);check(name+'_transparent_all_edges',min(margin)>=1)
  if name in LM:check(name+'_common_base_boundary',bb[3]==92)
  entry=next(x for x in manifest['assets'] if x['id']==name);check(name+'_manifest_hash',entry['sha256']==H(p));check(name+'_manifest_dimensions',entry['size']==[expected,expected])
  assets.append({'id':name,'size':list(im.size),'mode':im.mode,'alphaValues':vals,'alphaBBoxExclusive':list(bb),'marginsLTRB':margin,'opaqueColorCount':cols,'sha256':H(p)})
 with tempfile.TemporaryDirectory() as td:
  for script in ['draw_landmarks.py','draw_nodes.py']:
   subprocess.run([sys.executable,str(ROOT/'source'/script),td],check=True,stdout=subprocess.DEVNULL)
  for name in NAMES:check(name+'_regeneration_bytes_match',H(Path(td)/f'{name}.png')==H(ROOT/'png'/f'{name}.png'))
 check('only_expected_resource_path_prefix',all(x['intendedResourcePath'].startswith('Art/map/') for x in manifest['assets']))
 result={'status':'pass','checksPassed':len(tests),'checksFailed':0,'method':'Original integer-coordinate drawing on 96x96/48x48 final canvas. No source raster scaling. Nearest enlargement used only for QA display.','assets':assets,'tests':tests,'runtimeStatus':'not_tested','limitations':['No integration into the current game','Published main predates user-described 2026-11-05 theme addendum; selected palette is a documented proposal, not canonical exact color constants','Simplified map-marker rendering, not a recreation of all existing scenery pixels']}
 (ROOT/'qa'/'validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
 sheet();print(json.dumps({'status':'pass','checks':len(tests),'assets':len(assets)}))
if __name__=='__main__':main()
