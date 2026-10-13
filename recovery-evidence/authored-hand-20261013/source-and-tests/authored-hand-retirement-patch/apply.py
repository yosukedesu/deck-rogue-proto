#!/usr/bin/env python3
"""Dry run by default. Check every hash/hunk/new-file guard before any write."""
from pathlib import Path
import argparse,hashlib,json
p=argparse.ArgumentParser();p.add_argument('project',type=Path);p.add_argument('--apply',action='store_true');a=p.parse_args();r=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest()
if not (a.project/'DeckRogue/GodotPort/UI/GameUI.HandExit.cs').exists():raise SystemExit('Prerequisite missing: install hand-exit-return-patch first. No writes.')
updates={}
for c in json.loads((r/'changes.json').read_text()):
 for side in ('before','after'):
  if sha(c[side].encode())!=c[side+'_sha256']:raise SystemExit('Corrupt hunk: '+c['name'])
 t=a.project/c['path'];s=updates.get(t,t.read_text())
 if s.count(c['after'])==1:continue
 if s.count(c['before'])!=1:raise SystemExit('Guard failed, no writes: '+c['path']+' / '+c['name'])
 updates[t]=s.replace(c['before'],c['after'],1)
for f in json.loads((r/'manifest.json').read_text())['payload']:
 data=(r/'payload'/f['path']).read_bytes();t=a.project/f['path']
 if sha(data)!=f['sha256']:raise SystemExit('Payload hash failed, no writes: '+f['path'])
 if t.exists() and t.read_bytes()!=data:raise SystemExit('New-file conflict, no writes: '+f['path'])
 if not t.exists():updates[t]=data.decode()
if a.apply:
 for t,s in updates.items():t.parent.mkdir(parents=True,exist_ok=True);t.write_text(s)
print(('Applied ' if a.apply else 'Guarded only ')+str(len(updates))+' files. Build, token/ledger, and unrelated source preserved.')
