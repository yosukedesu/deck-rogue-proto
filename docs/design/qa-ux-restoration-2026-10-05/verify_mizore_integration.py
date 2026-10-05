from pathlib import Path
from PIL import Image
import hashlib,io,json,subprocess
r=Path('/home/yosuke/projects/deck-rogue-proto');w=Path('/mnt/c/Users/yosuke/Documents/Codex/2026-10-05/task');a=r/'unity/Assets/Resources/Art/leaders'
def old(rel):return subprocess.check_output(['git','show','HEAD:'+rel],cwd=r)
rows=[];sole=None
for i in range(8):
 p=a/'anim'/f'leader_blue_idle_{i}.png';im=Image.open(p).convert('RGBA');before=Image.open(io.BytesIO(old(str(p.relative_to(r))))).convert('RGBA')
 mask=im.getchannel('A').crop((0,75,128,77)).tobytes()
 if sole is None:sole=mask
 assert mask==sole
 if i==0:assert im.crop((0,0,120,84)).tobytes()==before.crop((8,0,128,84)).tobytes()
 else:assert p.read_bytes()==old(str(p.relative_to(r)))
 rows.append(dict(frame=i,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),bbox=im.getchannel('A').getbbox(),soleIdentical=True))
unchanged=0
for p in (a/'anim').glob('leader_blue_*.png'):
 if p.name!='leader_blue_idle_0.png':assert p.read_bytes()==old(str(p.relative_to(r)));unchanged+=1
report=dict(passed=True,frames=rows,otherFramesByteIdentical=unchanged,runtimePositionCorrectionAdded=False,importerUnchanged=not subprocess.check_output(['git','diff','--','unity/Assets/Editor/ArtImporter.cs'],cwd=r).strip(),libraryFileId='libfile_2198f503d71c8191a2a07d45e7810161',version=0)
(w/'mizore-integration-verification.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
