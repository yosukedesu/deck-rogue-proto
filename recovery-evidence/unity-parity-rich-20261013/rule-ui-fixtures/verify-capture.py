#!/usr/bin/env python3
"""Compare an F9 save with a validated engine outcome, or two F9 saves for cancel."""
import argparse,json,sys,hashlib
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('actual');p.add_argument('expected');p.add_argument('--cancel',action='store_true',help='Require entire export equality instead of only authoritative run state')
a=p.parse_args(); actual=json.loads(Path(a.actual).read_text()); expected=json.loads(Path(a.expected).read_text())
if not a.cancel: actual=actual['run'];expected=expected['run']
def canon(x):return json.dumps(x,sort_keys=True,separators=(',',':'),ensure_ascii=False)
def diffs(a,b,path=''):
 if type(a)!=type(b): return [path+': type mismatch']
 if isinstance(a,dict):
  out=[]
  for k in sorted(a.keys()|b.keys()):
   out+= [path+'/'+k+': missing'] if k not in a or k not in b else diffs(a[k],b[k],path+'/'+k)
  return out
 if isinstance(a,list):
  if len(a)!=len(b):return [path+f': length {len(a)} != {len(b)}']
  return sum((diffs(x,y,path+f'/{i}') for i,(x,y) in enumerate(zip(a,b))),[])
 return [] if a==b else [path+f': actual={a!r}, expected={b!r}']
d=diffs(actual,expected)
if d: print('\n'.join(d[:30]));sys.exit(1)
print('PASS: '+('entire export unchanged' if a.cancel else 'authoritative run equals real engine outcome')+'; canonical SHA256 '+hashlib.sha256(canon(actual).encode()).hexdigest())
