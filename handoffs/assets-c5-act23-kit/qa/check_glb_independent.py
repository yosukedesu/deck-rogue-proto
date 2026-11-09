#!/usr/bin/env python3
"""Independent read-only GLB structural/geometry checks (not an asset generator)."""
import json,struct,sys,hashlib,io,collections
from pathlib import Path
import numpy as np
from PIL import Image
CT={5120:'i1',5121:'u1',5122:'<i2',5123:'<u2',5125:'<u4',5126:'<f4'}
NC={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT2':4,'MAT3':9,'MAT4':16}
def inspect(path):
    b=path.read_bytes(); issues=[]; warnings=[]
    magic,version,total=struct.unpack_from('<III',b,0)
    if magic!=0x46546c67 or version!=2 or total!=len(b):raise ValueError('invalid GLB header')
    chunks=[];off=12
    while off<len(b):
        n,k=struct.unpack_from('<II',b,off);off+=8;chunks.append((k,b[off:off+n]));off+=n
    g=json.loads(next(d for k,d in chunks if k==0x4e4f534a)); binary=next(d for k,d in chunks if k==0x004e4942)
    def arr(i):
        a=g['accessors'][i];v=g['bufferViews'][a['bufferView']];dt=np.dtype(CT[a['componentType']]);nc=NC[a['type']];offset=v.get('byteOffset',0)+a.get('byteOffset',0);stride=v.get('byteStride',dt.itemsize*nc)
        if a.get('sparse'):raise ValueError('sparse accessor requires dedicated validation')
        z=np.ndarray((a['count'],nc),dtype=dt,buffer=binary,offset=offset,strides=(stride,dt.itemsize)).copy()
        if a.get('normalized') and a['componentType']!=5126:
            info=np.iinfo(dt);z=np.maximum(z.astype(float)/info.max,-1.)
        return z
    def local(node):
        if 'matrix' in node:return np.array(node['matrix']).reshape(4,4).T
        x,y,z,w=node.get('rotation',[0,0,0,1]);m=np.eye(4);m[:3,:3]=np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])@np.diag(node.get('scale',[1,1,1]));m[:3,3]=node.get('translation',[0,0,0]);return m
    positions=[];count=0;degen=0;mesh_instances=0;nonfinite=0;badnormals=0;minarea=float('inf');nodes=[];negative_volume=0;boundary=0;nonmanifold=0
    def walk(i,parent):
        nonlocal count,degen,mesh_instances,nonfinite,badnormals,minarea,negative_volume,boundary,nonmanifold
        n=g['nodes'][i];m=parent@local(n);nodes.append({'name':n.get('name',str(i)),'scale':n.get('scale',[1,1,1]),'translation':n.get('translation',[0,0,0]),'determinant':float(np.linalg.det(m[:3,:3]))})
        if 'mesh' in n:
            mesh_instances+=1
            for p in g['meshes'][n['mesh']]['primitives']:
                if p.get('mode',4)!=4:issues.append('non-triangle primitive');continue
                v=arr(p['attributes']['POSITION']);nonfinite+=int((~np.isfinite(v)).sum());v=(m[:3,:3]@v.T).T+m[:3,3];positions.extend(v.tolist())
                ix=arr(p['indices']).ravel() if 'indices' in p else np.arange(len(v));
                if len(ix)%3:issues.append('index count not multiple of three');continue
                if ix.max()>=len(v) or ix.min()<0:issues.append('index out of range');continue
                f=v[ix.reshape(-1,3)];area=np.linalg.norm(np.cross(f[:,1]-f[:,0],f[:,2]-f[:,0]),axis=1)/2;count+=len(area);degen+=int((area<1e-12).sum());minarea=min(minarea,float(area.min()))
                signed=np.einsum('ij,ij->i',f[:,0],np.cross(f[:,1],f[:,2])).sum()/6;negative_volume+=int(signed < -1e-6)
                _,inverse=np.unique(np.round(v,6),axis=0,return_inverse=True);wf=inverse[ix.reshape(-1,3)];edges=collections.Counter(tuple(sorted(e)) for face in wf for e in [(int(face[0]),int(face[1])),(int(face[1]),int(face[2])),(int(face[2]),int(face[0]))]);boundary+=sum(c==1 for c in edges.values());nonmanifold+=sum(c>2 for c in edges.values())
                if 'NORMAL' not in p['attributes']:issues.append('missing vertex normals')
                else:
                    normal=arr(p['attributes']['NORMAL']);badnormals+=int((np.abs(np.linalg.norm(normal,axis=1)-1)>.01).sum());nonfinite+=int((~np.isfinite(normal)).sum())
                if 'TEXCOORD_0' not in p['attributes']:warnings.append('primitive missing UV0')
                else:nonfinite+=int((~np.isfinite(arr(p['attributes']['TEXCOORD_0']))).sum())
                if 'material' not in p:issues.append('primitive missing material')
                elif p['material']>=len(g.get('materials',[])):issues.append('material index invalid')
        for j in n.get('children',[]):walk(j,m)
    for i in g['scenes'][g.get('scene',0)]['nodes']:walk(i,np.eye(4))
    if not positions:issues.append('no geometry');mn=mx=np.zeros(3)
    else:v=np.array(positions);mn=v.min(0);mx=v.max(0)
    if negative_volume:issues.append(f'{negative_volume} inward-wound closed primitives (negative signed volume)')
    if boundary:issues.append(f'{boundary} open boundary edges')
    if nonmanifold:issues.append(f'{nonmanifold} nonmanifold edges')
    if degen:issues.append(f'{degen} degenerate triangles')
    if nonfinite:issues.append(f'{nonfinite} nonfinite components')
    if badnormals:issues.append(f'{badnormals} nonunit normals')
    if abs(mn[1])>1e-4:issues.append(f'ground minY={mn[1]:.7f}, not zero')
    center=(mn+mx)/2;center[1]=0
    if np.linalg.norm(center)>0.05:warnings.append(f'XZ bounds centre offset {center.tolist()} (verify pivot contract)')
    images=[]
    for im in g.get('images',[]):
        if 'bufferView' in im:
            bv=g['bufferViews'][im['bufferView']];ib=binary[bv.get('byteOffset',0):bv.get('byteOffset',0)+bv['byteLength']]
        elif im.get('uri','').startswith('data:'):
            import base64;ib=base64.b64decode(im['uri'].split(',',1)[1])
        else:
            f=path.parent/im.get('uri','');ib=f.read_bytes() if f.is_file() else b''
        try:
            img=Image.open(io.BytesIO(ib));img.load();images.append({'name':im.get('name'),'dimensions':list(img.size),'sha256':hashlib.sha256(ib).hexdigest()})
        except Exception as e:issues.append('missing/invalid embedded texture '+str(e))
    return {'file':str(path),'sha256':hashlib.sha256(b).hexdigest(),'triangles':count,'mesh_instances':mesh_instances,'bounds_min':mn.tolist(),'bounds_max':mx.tolist(),'dimensions_xyz':(mx-mn).tolist(),'minimum_triangle_area':minarea,'negative_volume_primitives':negative_volume,'welded_boundary_edges':boundary,'nonmanifold_edges':nonmanifold,'materials':[m.get('name') for m in g.get('materials',[])],'samplers':g.get('samplers',[]),'images':images,'nodes':nodes,'errors':list(dict.fromkeys(issues)),'warnings':list(dict.fromkeys(warnings))}
if __name__=='__main__':
    root=Path(sys.argv[1]);paths=sorted(root.rglob('*.glb'));result={'scope':'independent GLB binary inspection; no modifications','count':len(paths),'assets':[]}
    for p in paths:
        try:result['assets'].append(inspect(p))
        except Exception as e:result['assets'].append({'file':str(p),'errors':[str(e)]})
    result['error_count']=sum(len(x['errors']) for x in result['assets']);print(json.dumps(result,indent=2))
