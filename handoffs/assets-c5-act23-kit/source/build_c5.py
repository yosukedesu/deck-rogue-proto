"""Original modular geometry; existing repository textures copied unchanged. Blender 4.3+."""
import bpy, bmesh, math, json, random, os, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
random.seed(20261108)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene; scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
M={};MODULES=[];COL=None

def material(key,tex=None,color=(1,1,1,1),metal=0,rough=.86):
 m=bpy.data.materials.new(key);m.use_nodes=True;m.diffuse_color=color
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=color;p.inputs['Roughness'].default_value=rough;p.inputs['Metallic'].default_value=metal
 m.surface_render_method='DITHERED';m.use_backface_culling=True
 if tex:
  im=bpy.data.images.load(str(ROOT/'textures'/tex),check_existing=True);im.pack();t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=im;t.interpolation='Closest';t.extension='REPEAT';m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color'])
 M[key]=m;return m
material('A2_Timber','act2_side_timber_a.png');material('A2_Plank','act2_side_plank_a.png');material('A2_Iron','act2_side_iron_a.png',metal=.6,rough=.76)
material('A3_Limestone','act3_side_pillar_a.png');material('A3_Step','act3_side_step_a.png');material('A3_Wall','act3_side_wall_a.png')
material('A2_Rope',color=(.19,.14,.08,1),rough=.97);material('A3_Brass',color=(.28,.205,.085,1),metal=.75,rough=.65)

def reg(o,name,mat):
 o.name=name
 for c in list(o.users_collection):c.objects.unlink(o)
 COL.objects.link(o);o.data.materials.append(M[mat]);return o

def box(name,loc,dim,mat,bevel=.025,rot=0):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.dimensions=dim;o.rotation_euler.z=rot;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('single_edge_wear','BEVEL');mod.width=min(bevel,min(dim)*.22);mod.segments=1;bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
 return reg(o,name,mat)

def cylinder(name,loc,radius,depth,mat,axis='Z',vertices=12):
 bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,end_fill_type='NGON',location=loc);o=bpy.context.object
 if axis=='X':o.rotation_euler.y=math.pi/2
 if axis=='Y':o.rotation_euler.x=math.pi/2
 return reg(o,name,mat)

def bar(name,a,b,width,depth,mat):
 a,b=Vector(a),Vector(b);o=box(name,(a+b)*.5,(width,depth,(b-a).length),mat,.02);o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o

def bolt(name,x,y,z,r=.045):cylinder(name,(x,y,z),r,.025,'A2_Iron','Y',6)

def begin(act,id,jp):
 global COL
 COL=bpy.data.collections.new(id);scene.collection.children.link(COL)
 return {'id':id,'act':act,'label_ja':jp}

def unwrap(o):
 bpy.context.view_layer.objects.active=o
 # Mesh coordinates transformed into common module coordinates, 25 texels/metre.
 if not o.data.uv_layers:o.data.uv_layers.new(name='UVMap')
 uv=o.data.uv_layers.active.data
 for poly in o.data.polygons:
  n=o.rotation_euler.to_matrix()@poly.normal;axis=max(range(3),key=lambda i:abs(n[i]))
  for li in poly.loop_indices:
   v=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co
   a,b= ((v.y,v.z) if axis==0 else (v.x,v.z) if axis==1 else (v.x,v.y))
   uv[li].uv=(a*25/64,b*25/64)

def finish(meta,notes,connections=None):
 meshes=list(COL.objects);bpy.context.view_layer.update()
 pts=[o.matrix_world@v.co for o in meshes for v in o.data.vertices]
 mn=Vector([min(p[i] for p in pts) for i in range(3)]);mx=Vector([max(p[i] for p in pts) for i in range(3)]);off=Vector(((mn.x+mx.x)/2,(mn.y+mx.y)/2,mn.z))
 for o in meshes:o.location-=off
 bpy.context.view_layer.update()
 for o in meshes:
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(o.data);bm.free();o.data.update();unwrap(o)
 root=bpy.data.objects.new(meta['id'],None);COL.objects.link(root)
 for o in meshes:o.parent=root
 bpy.ops.object.select_all(action='DESELECT');root.select_set(True)
 for o in meshes:o.select_set(True)
 bpy.context.view_layer.objects.active=meshes[0]
 out=ROOT/'models'/('act'+str(meta['act']))/meta['id']
 bpy.ops.export_scene.gltf(filepath=str(out)+'.glb',export_format='GLB',use_selection=True,export_yup=True,export_cameras=False,export_lights=False,export_apply=True,export_extras=True)
 bpy.ops.export_scene.fbx(filepath=str(out)+'.fbx',use_selection=True,apply_unit_scale=True,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True,object_types={'MESH','EMPTY'},bake_anim=False)
 tri=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
 size=mx-mn;meta.update({'glb':str(out.relative_to(ROOT))+'.glb','fbx':str(out.relative_to(ROOT))+'.fbx','dimensions_m_xyz':[round(size.x,5),round(size.z,5),round(size.y,5)],'pivot_m_xyz':[0,0,0],'triangles':tri,'mesh_count':len(meshes),'materials':sorted(set(m.name for o in meshes for m in o.data.materials)),'notes':notes,'connections':connections or []})
 MODULES.append(meta);root['c5_module_id']=meta['id'];root['units']='metres';root['pivot']='ground centre';return root

# Act 2: timber/iron construction, original geometry.
a=begin(2,'act2_timber_post','坑木の支柱')
box('post',(0,0,2.2),(.5,.5,4.4),'A2_Timber',.045);box('cap',(0,0,4.4),(.95,.75,.28),'A2_Timber',.035)
for z in (.3,1.8,3.9):
 box('strap',(0,0,z),(.525,.525,.13),'A2_Iron',.012)
 for x in (-.15,.15):bolt('rivet',x,-.275,z,.037)
finish(a,'Column shaft .5×.5×4.4m plus cap. Max cap height 4.54m. Display stand is not included.')
a=begin(2,'act2_timber_beam','坑木の梁')
box('beam',(0,0,.21),(4,.44,.42),'A2_Timber',.04)
for x in (-1.72,0,1.72):
 box('iron_band',(x,0,.21),(.13,.47,.45),'A2_Iron',.012)
 bolt('band_pin',x,-.25,.21)
finish(a,'4m reusable section, rather than baking the scene-wide 8.9m beam.', [{'point_m_xyz':[-2,0.225,0],'type':'beam_end'},{'point_m_xyz':[2,.225,0],'type':'beam_end'}])
a=begin(2,'act2_timber_brace','坑木の筋交い')
bar('diagonal',(-.65,0,.15),(.65,0,1.45),.24,.28,'A2_Timber')
for x,z in [(-.61,.18),(.61,1.42)]:
 box('end_plate',(x,-.16,z),(.37,.08,.34),'A2_Iron',.012);bolt('brace_pin',x,-.215,z,.05)
finish(a,'Diagonal brace supplied as its own solid module; ground-pivot describes its AABB, not an attachment plane.')
a=begin(2,'act2_mine_portal','坑口の木枠')
for x in (-1.28,1.28):
 box('portal_post',(x,0,1.475),(.36,.5,2.95),'A2_Timber',.035)
 for z in (.18,2.65):
  box('portal_band',(x,0,z),(.4,.54,.15),'A2_Iron',.012);bolt('portal_pin',x,-.29,z)
box('lintel',(0,0,3.1),(3.3,.65,.4),'A2_Timber',.045)
for sign in (-1,1):bar('knee',(sign*1.25,0,2.15),(sign*.65,0,2.92),.19,.28,'A2_Timber')
finish(a,'Open timber portal, not a sealed black card or cave wall. Opening 2.2m wide.')
a=begin(2,'act2_rail_segment','枕木付きレール')
for i,x in enumerate([-1.65,-.7,.45,1.5]):
 box('sleeper_%d'%i,(x,0,.055),(.22,1.35,.11),'A2_Timber',.018,rot=(.025,-.018,.02,-.03)[i])
for y in (-.45,.45):
 box('rail_base',(0,y,.13),(4,.12,.04),'A2_Iron',.006);box('rail_web',(0,y,.175),(4,.045,.07),'A2_Iron',.005);box('rail_head',(0,y,.225),(4,.09,.04),'A2_Iron',.006)
 for x in [-1.65,-.7,.45,1.5]:
  for dy in (-.09,.09):cylinder('track_spike',(x,y+dy,.13),.025,.07,'A2_Iron',vertices=6)
finish(a,'4m along local X. Gauge .9m; 4 irregular sleepers. No scene-wide 56m/60m geometry.', [{'point_m_xyz':[-2,0,0],'type':'track_end'},{'point_m_xyz':[2,0,0],'type':'track_end'}])
a=begin(2,'act2_winch','坑道の巻上機')
for x in (-.64,.64):
 box('skid',(x,0,.09),(.26,1.4,.18),'A2_Timber',.025)
 box('support',(x,0,.69),(.2,.38,1.2),'A2_Timber',.025)
 cylinder('bearing',(x,0,1.05),.2,.22,'A2_Iron','X',12)
cylinder('axle',(0,0,1.05),.075,1.62,'A2_Iron','X')
cylinder('drum',(0,0,1.05),.3,1.0,'A2_Timber','X',12)
for x in (-.53,.53):cylinder('flange',(x,0,1.05),.4,.09,'A2_Iron','X',16)
for i in range(12):
 # Closed, low-poly rope coils around the fixed horizontal barrel.
 bpy.ops.mesh.primitive_torus_add(major_radius=.315,minor_radius=.024,major_segments=16,minor_segments=5,location=(-.43+i*.078,0,1.05),rotation=(0,math.pi/2,0));reg(bpy.context.object,'rope_coil','A2_Rope')
bar('crank',( .88,0,1.05),(.88,0,1.62),.07,.07,'A2_Iron');cylinder('grip',(1.02,0,1.62),.07,.32,'A2_Timber','X')
bar('foot_brace_L',(-.64,-.5,.17),(-.64,0,.87),.12,.13,'A2_Timber');bar('foot_brace_R',(.64,-.5,.17),(.64,0,.87),.12,.13,'A2_Timber')
finish(a,'Static prop. Crank and barrel are authored geometry, no skeleton or mechanical animation. Size inferred, not an existing 3D asset replacement.')
a=begin(2,'act2_gallery_section','坑道の歩廊')
for i in range(8):box('plank_%d'%i,(-1.3125+i*.375,0,.07),(.35,1.2,.14),'A2_Plank',.012)
for y in (-.43,.43):box('underbeam',(0,y,-.08),(3,.2,.2),'A2_Timber',.02)
for x in (-1.38,0,1.38):box('railing_post',(x,.53,.58),(.13,.13,1.02),'A2_Timber',.018)
for z in (.58,1.07):box('guard_rail',(0,.53,z),(3,.12,.12),'A2_Timber',.015)
finish(a,'3m modular gallery, width1.2. Floor surface is above ground pivot because underbeams are included. One-sided guardrail.')
a=begin(2,'act2_timber_stairs','坑道の木階段')
for i in range(5):box('tread_%d'%i,(0,-.8+i*.4,.14+i*.2),(1.5,.44,.15),'A2_Plank',.018)
for x in (-.58,.58):bar('stringer',(x,-1,0),(x,1,1),.16,.16,'A2_Timber')
finish(a,'Five open risers, nominal rise .2m/run .4m. Ascends local -Z after Y-up export. Decorative stage prop; no collision mesh.')

# Act 3: monumental stone construction, no forest moss/vines.
a=begin(3,'act3_ancient_pillar','古代都市の柱')
box('base',(0,0,.18),(1.13,1.13,.36),'A3_Step',.04)
for i in range(9):
 z=.36+i*.77+.38;box('shaft_course_%02d'%i,(0,0,z),(.85,.85,.75),'A3_Limestone',.024)
box('neck',(0,0,7.34),(.99,.99,.2),'A3_Limestone',.025);box('capital',(0,0,7.6),(1.17,1.17,.4),'A3_Limestone',.04)
for z in (.42,7.15):box('inlay',(0,-.434,z),(.62,.018,.055),'A3_Brass',.004)
finish(a,'Monumental pillar: shaft .85m, total authored height7.8m including capital. Separate 3.6m broken variant.')
a=begin(3,'act3_broken_pillar','折れた古代柱')
box('base',(0,0,.17),(1.16,1.16,.34),'A3_Step',.04)
for i in range(3):box('shaft_course',(0,0,.34+i*.77+.38),(.9,.9,.75),'A3_Limestone',.025)
# Closed irregular broken cap with visibly sloping fracture surface.
v=[(-.45,-.45,2.65),(.45,-.45,2.65),(.45,.45,2.65),(-.45,.45,2.65),(-.45,-.45,3.12),(.45,-.45,3.6),(.45,.45,3.27),(-.45,.45,3.45)]
me=bpy.data.meshes.new('broken_cap');me.from_pydata(v,[],[(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6),(4,6,7)]);me.update();o=bpy.data.objects.new('broken_cap',me);COL.objects.link(o);o.data.materials.append(M['A3_Limestone'])
finish(a,'3.6m broken pillar with closed irregular fracture; no loose pieces embedded below ground.')

def arch(half=False):
 inner=1.65;outer=2.5;spring=3.5;depth=1.1
 for side in ((-1,) if half else (-1,1)):
  for i in range(5):box('pier_course',(side*2.075,0,.35+i*.7),(.85,depth,.68),'A3_Limestone',.025)
 n=12;start=6 if half else 0
 for i in range(start,n):
  a0=i*math.pi/n+.006;a1=(i+1)*math.pi/n-.006
  r=outer+.10 if i in (5,6) else outer
  vv=[(rr*math.cos(aa),y,spring+rr*math.sin(aa)) for y in (-depth/2,depth/2) for rr,aa in [(inner,a0),(r,a0),(r,a1),(inner,a1)]]
  fs=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
  me=bpy.data.meshes.new('voussoir');me.from_pydata(vv,[],fs);me.update();o=bpy.data.objects.new('arch_stone_%02d'%i,me);COL.objects.link(o);o.data.materials.append(M['A3_Limestone'])
a=begin(3,'act3_arch_full','古代の大アーチ');arch();finish(a,'Nominal 5m wide×6m high×1.1m deep; keystone projects .1m above nominal rise. Opening3.3m.')
a=begin(3,'act3_arch_half','崩れた半アーチ');arch(True);finish(a,'Left pier and half arch. Ground-centred independently; use manifest dimensions, not full-arch origin. Cut arch is capped.')
a=begin(3,'act3_stone_stairs','古代都市の石階段')
for i in range(3):
 h=(i+1)*.25;d=1.36-i*.42;y=-.68+i*.42+d/2
 box('step_%d'%i,(0,y,h/2),(3,d,h),'A3_Step',.022)
finish(a,'3m wide, total depth1.36m and height.75m. Three .25m risers. Ascends local -Z after export.')
a=begin(3,'act3_channel_straight','石造水路・直線')
box('bed',(0,0,.1),(4,1.8,.2),'A3_Step',.018)
for y in (-.8,.8):
 for i in range(4):box('wall',( -1.5+i,y,.45),(.98,.2,.5),'A3_Wall',.018)
finish(a,'Dry masonry only. Length4m along X; clear width1.4m, bed top.2m, wall top.7m. No water shader or fluid.', [{'point_m_xyz':[-2,.2,0],'type':'channel_end'},{'point_m_xyz':[2,.2,0],'type':'channel_end'}])
a=begin(3,'act3_channel_corner','石造水路・曲がり')
# Quarter ring from inner .8m to outer2.6m, entrances share straight channel1.8m width.
segments=8
for i in range(segments):
 a0=i*math.pi/2/segments;a1=(i+1)*math.pi/2/segments
 for tag,ri,ro,z,h,mat in [('bed',.8,2.6,0,.2,'A3_Step'),('inner',.8,1.0,.2,.5,'A3_Wall'),('outer',2.4,2.6,.2,.5,'A3_Wall')]:
  vv=[(r*math.cos(ang),r*math.sin(ang),zz) for zz in (z,z+h) for r,ang in [(ri,a0),(ro,a0),(ro,a1),(ri,a1)]]
  fs=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
  me=bpy.data.meshes.new(tag);me.from_pydata(vv,[],fs);me.update();o=bpy.data.objects.new('%s_%d'%(tag,i),me);COL.objects.link(o);o.data.materials.append(M[mat])
finish(a,'90-degree bend; same1.8m outer/1.4m clear channel. No water. Ground-centred AABB shifts the bend centre.', [{'point_m_xyz':[.4,.2,1.3],'type':'channel_end','outward_m_xyz':[0,0,1]},{'point_m_xyz':[-1.3,.2,-.4],'type':'channel_end','outward_m_xyz':[-1,0,0]}])
a=begin(3,'act3_parapet_section','古代都市の胸壁')
box('base',(0,0,.13),(3.2,.36,.26),'A3_Step',.025)
for i,x in enumerate([-1.3,-.42,.46,1.3]):
 h=[.3,.42,.42,.3][i];box('merlon',(x,0,.26+h/2),(.5,.34,h),'A3_Wall',.025)
finish(a,'3.2m repeatable low parapet. Short side battlements retain battle sightlines. Not a full-height wall.')

# Save import manifest before arranging authoring display.
manifest={'schema':'deckrogue.c5.modular3d.v1','bundle':'C5 Act2+Act3 modular stage parts','created':'2026-11-08','branch':'codex/assets-c5-20261108','unit':'metre','axes':'Y-up, X right, -Z forward after GLB export; Blender source Z-up','pivot':'ground AABB centre for each standalone model','texels_per_metre':25,'texture_resolution_px':[64,64],'material_filter':'nearest + nearest mipmap in GLB; verify game shader override','asset_count':len(MODULES),'production_game_integrated':False,'godot_standalone_import_verified':False,'collision_meshes':False,'animation':False,'lod':'not authored; Godot import LOD policy belongs to integrator','modules':MODULES,'excluded':['characters','portraits','BGM','key visual','src/engine','src/data','Unity code','production stage loaders','water shader'],'material_definitions':{k:{'albedo_texture':next((str(Path(n.image.filepath).name) for n in v.node_tree.nodes if n.type=='TEX_IMAGE'),None),'roughness':v.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value,'metallic':v.node_tree.nodes.get('Principled BSDF').inputs['Metallic'].default_value,'double_sided':False} for k,v in M.items()}}
(ROOT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2))

# Separate catalogue arrangement only in .blend source and preview, never in standalone GLBs.
for act in (2,3):
 for m in MODULES:
  col=bpy.data.collections[m['id']];col.hide_render=m['act']!=act
  root=bpy.data.objects[m['id']]
  index=[v['id'] for v in MODULES if v['act']==act].index(m['id']) if m['act']==act else 0
  root.location=((index%4-1.5)*6.2,(index//4)*8,0)
 # Preview rig is recreated each act to keep source straightforward.
 old=bpy.data.collections.get('PREVIEW_ONLY')
 if old:
  for o in list(old.objects):bpy.data.objects.remove(o,do_unlink=True)
  bpy.data.collections.remove(old)
 COL=bpy.data.collections.new('PREVIEW_ONLY');scene.collection.children.link(COL)
 material('Preview_floor_'+str(act),color=(.022,.036,.043,1),rough=.96)
 box('PREVIEW_FLOOR',(0,3,-.09),(30,22,.15),'Preview_floor_'+str(act),0)
 for m in [v for v in MODULES if v['act']==act]:
  root=bpy.data.objects[m['id']];x,y,z=root.location
  box('PREVIEW_STAND',(x,y,-.025),(5.2,4.0,.05),'Preview_floor_'+str(act),0)
  bpy.ops.object.text_add(location=(x-2.4,y-2.4,.03),rotation=(0,0,0));label=bpy.context.object;label.name='PREVIEW_LABEL';label.data.body=m['id'].replace('act'+str(act)+'_','').upper();label.data.size=.25;label.data.extrude=0
  for c in list(label.users_collection):c.objects.unlink(label)
  COL.objects.link(label);label.data.materials.append(M['A3_Brass'])
 world=scene.world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.15,.22,.30,1);world.node_tree.nodes['Background'].inputs[1].default_value=.45
 for name,pos,power,col,size in [('Key',(-9,-10,17),3000,(1,.77,.52),10),('Fill',(12,-3,13),2200,(.51,.74,1),12),('Rim',(0,14,16),3400,(.62,.82,1),11)]:
  bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.name='PREVIEW_'+name;o.data.energy=power;o.data.color=col;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((0,3,2))-o.location).to_track_quat('-Z','Y').to_euler()
  for c in list(o.users_collection):c.objects.unlink(o)
  COL.objects.link(o)
 bpy.ops.object.camera_add(location=(15,-27,23));cam=bpy.context.object;cam.rotation_euler=(Vector((0,3.5,2.2))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=30;scene.camera=cam
 for c in list(cam.users_collection):c.objects.unlink(cam)
 COL.objects.link(cam)
 scene.render.engine='CYCLES';scene.cycles.samples=64;scene.cycles.use_denoising=False;scene.render.resolution_x=1920;scene.render.resolution_y=1200;scene.render.resolution_percentage=100;scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG';scene.render.filepath=str(ROOT/'previews'/('act%d_catalog.png'%act))
 bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'source'/('act%d_kit.blend'%act)));bpy.ops.render.render(write_still=True)
print('C5_MODELS_READY',len(MODULES),sum(m['triangles'] for m in MODULES))
