from pathlib import Path
import sys, zipfile, json, hashlib, struct, subprocess, os
sys.path.insert(0,'/tmp/deckrogue-texture-codecs/unitypy')
import UnityPy
from PIL import Image
import numpy as np
w=Path('/mnt/c/Users/yosuke/Documents/Codex/2026-10-05/task');r=Path('/home/yosuke/projects/deck-rogue-proto')
apk=w/'DeckRogue-UXFix-LOCAL.apk';root=w/'ux-final-android-inspection';root.mkdir(exist_ok=True)
z=zipfile.ZipFile(apk);assert z.testzip() is None
for n in z.namelist():
 if n.startswith('assets/bin/Data/') and not n.endswith('/'):
  rel=Path(n[len('assets/bin/Data/'):]);assert '..' not in rel.parts
  dest=root/rel;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(z.read(n))
env=UnityPy.load(str(root));textures=[];profile=None;shaders={};rendererTypes=set();mizore=[]
decoded=w/'ux-final-android-decoded';decoded.mkdir(exist_ok=True)
for obj in env.objects:
 if obj.type.name=='MonoScript':
  script=obj.read()
  if 'Render' in script.m_ClassName:rendererTypes.add(script.m_ClassName)
 if obj.type.name=='Shader':
  shader=obj.read();form=shader.m_ParsedForm
  if form and form.m_Name in ['DeckRogue/ElementBattleFx','DeckRogue/WorldBattleFx','DeckRogue/ElementLuminousFx']:
   passes=[]
   for sub in form.m_SubShaders:
    for ps in sub.m_Passes:
     state=ps.m_State
     passes.append({'name':ps.m_Name,'tags':dict(ps.m_Tags.tags+ps.m_State.m_Tags.tags),'zTest':state.zTest.val,'zWrite':state.zWrite.val,'srcBlend':state.rtBlend0.srcBlend.val,'destBlend':state.rtBlend0.destBlend.val})
   shaders[form.m_Name]={'platformCodes':shader.platforms,'compressedBytes':len(shader.compressedBlob or []),'passes':passes}
 if obj.type.name=='TextAsset':
  t=obj.read()
  if t.m_Name=='card-vfx':
   raw=t.m_Script.encode('utf-8','surrogateescape') if isinstance(t.m_Script,str) else t.m_Script
   expected=(r/'unity/Assets/Resources/Data/card-vfx.json').read_bytes()
   assert raw==expected;profile={'exactSourceBytes':True,'profiles':len(json.loads(raw))}
 if obj.type.name!='Texture2D':continue
 t=obj.read()
 if t.m_Name in ['leader_blue','leader_blue_idle_0']:
  source=r/'unity/Assets/Resources/Art/leaders'/('anim/' if t.m_Name.endswith('_0') else '')/(t.m_Name+'.png')
  a=np.asarray(Image.open(source).convert('RGBA'));b=np.asarray(t.image.convert('RGBA'))
  assert a.shape==b.shape and np.array_equal(a[:,:,3],b[:,:,3])
  visible=a[:,:,3]>0;assert np.array_equal(a[visible],b[visible])
  mizore.append({'name':t.m_Name,'width':t.m_Width,'height':t.m_Height,'format':t.m_TextureFormat,'alphaAndVisibleRGBAExact':True})
 if not(t.m_Name.startswith('element-') and t.m_Name.endswith('-8f')):continue
 raw=t.get_image_data();settings=t.m_TextureSettings
 rec={'name':t.m_Name,'width':t.m_Width,'height':t.m_Height,'format':t.m_TextureFormat,'mipCount':t.m_MipCount,'dataBytes':len(raw),'filterMode':settings.m_FilterMode,'wrapU':settings.m_WrapU,'wrapV':settings.m_WrapV,'colorSpace':t.m_ColorSpace}
 assert (t.m_Width,t.m_Height,t.m_TextureFormat,t.m_MipCount)==(1024,512,50,1)
 assert len(raw)==171*86*16 and settings.m_FilterMode==1 and settings.m_WrapU==1 and settings.m_WrapV==1
 header=struct.pack('<I',0x5ca1ab13)+bytes([6,6,1])+(1024).to_bytes(3,'little')+(512).to_bytes(3,'little')+(1).to_bytes(3,'little')
 astc=decoded/(t.m_Name+'.astc');png=decoded/(t.m_Name+'.png');astc.write_bytes(header+raw)
 process_env=os.environ.copy();process_env['LD_LIBRARY_PATH']='/tmp/deckrogue-texture-codecs/root/usr/lib/x86_64-linux-gnu'
 subprocess.run(['/tmp/deckrogue-texture-codecs/root/usr/bin/astcenc','-ds',str(astc),str(png),'-silent'],env=process_env,check=True)
 im=Image.open(png).transpose(Image.Transpose.FLIP_TOP_BOTTOM).convert('RGBA');im.save(png)
 source=Image.open(r/'unity/Assets/Resources/Art/fx'/(t.m_Name+'.png')).convert('RGBA')
 a=np.asarray(source).astype(float)/255;b=np.asarray(im).astype(float)/255
 rec['alphaMeanAbsoluteError']=float(np.abs(a[:,:,3]-b[:,:,3]).mean())
 rec['premultipliedRGBMeanAbsoluteError']=float(np.abs(a[:,:,:3]*a[:,:,3:]-b[:,:,:3]*b[:,:,3:]).mean())
 rec['alphaNonzeroPixels']=int((b[:,:,3]>0).sum());assert rec['alphaNonzeroPixels']>0
 textures.append(rec)
assert len(textures)==9 and profile is not None
out={'apk':apk.name,'sha256':hashlib.sha256(apk.read_bytes()).hexdigest(),'bytes':apk.stat().st_size,'zipCRC':'passed','abis':sorted({n.split('/')[1] for n in z.namelist() if n.startswith('lib/') and n.endswith('.so')}),'textures':textures,'profile':profile,'textureDataBytes':sum(t['dataBytes'] for t in textures),'androidDeviceExecuted':False,'distributed':False}
assert out['abis']==['arm64-v8a']
assert len(shaders)==3,shaders
for name,shader in shaders.items():
 assert shader['compressedBytes']>0 and shader['platformCodes']
 for ps in shader['passes']:
  assert ps['zWrite']==0 and ps['zTest']==4,ps
  expected=(1,1) if ps['name']=='EmissiveHalo' else (5,10)
  assert (ps['srcBlend'],ps['destBlend'])==expected,ps
assert shaders['DeckRogue/ElementBattleFx']['passes'][0]['tags'].get('LIGHTMODE')=='ElementAfterPost'
assert shaders['DeckRogue/WorldBattleFx']['passes'][0]['tags'].get('LIGHTMODE')=='UniversalForward'
assert [p['tags'].get('LIGHTMODE') for p in shaders['DeckRogue/ElementLuminousFx']['passes']]==['ElementAfterPost','UniversalForward']
assert len(mizore)==2
out['mizoreTextures']=mizore
out['shaderTagEncoding']='SerializedPass.m_State.m_Tags.tags uses uppercase LIGHTMODE; both exact values asserted'
out['shaders']=shaders;out['rendererScriptTypes']=sorted(rendererTypes)
metadata=(root/'Managed/Metadata/global-metadata.dat').read_bytes()
out['runtimeMetadata']={name:name.encode() in metadata for name in ['ElementVfxPass','RestoreLegacyElements','CanUsePost']}
assert all(out['runtimeMetadata'].values())
out['sourceCommit']=subprocess.check_output(['git','rev-parse','HEAD'],cwd=r,text=True).strip()

(w/'ux-final-android-content-verification.json').write_text(json.dumps(out,indent=2)+'\n');print(json.dumps(out,indent=2))
