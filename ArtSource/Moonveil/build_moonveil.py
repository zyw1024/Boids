"""Moonveil fish: original procedural mesh, painted textures, rig and animation.
Run in Blender 5.x. Units: metres. Authoring forward +X; Z up.
All geometry and textures are authored here; no external art assets.
"""
import bpy, math, json, os
import numpy as np
from mathutils import Vector
from pathlib import Path
from math import sin, cos, pi, exp

BASE = Path(os.environ['BOIDS_ROOT']).resolve() if os.environ.get('BOIDS_ROOT') else Path(__file__).resolve().parents[2]
SOURCE = BASE / 'ArtSource/Moonveil'
OUT = BASE / 'Sky_City_Project/Assets/Boids/Art/Moonveil'
PREVIEW = SOURCE / 'Preview'
for p in (SOURCE, OUT, OUT/'Textures', PREVIEW): p.mkdir(parents=True, exist_ok=True)

# Build in a dedicated scene and retain any existing work.
name = 'Moonveil_Studio'
if name in bpy.data.scenes:
    old = bpy.data.scenes[name]
    for ob in list(old.objects): bpy.data.objects.remove(ob, do_unlink=True)
    bpy.data.scenes.remove(old)
for pool in [bpy.data.materials,bpy.data.images]:
    for item in list(pool):
        if item.users==0 and item.name.startswith('Moonveil'):pool.remove(item)
scene = bpy.data.scenes.new(name)
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.render.fps = 30
scene.render.engine = 'CYCLES'
scene.cycles.samples = 48
scene.cycles.use_denoising = True
scene.render.resolution_x = 1500
scene.render.resolution_y = 1000
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.view_settings.view_transform = 'AgX'
scene.view_settings.look = 'AgX - Medium High Contrast'
scene.world = bpy.data.worlds.new('Moonveil • midnight water')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs[0].default_value = (0.012,0.026,0.05,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value = .35
fish_collection = bpy.data.collections.new('MOONVEIL | exportable fish')
scene.collection.children.link(fish_collection)
studio = bpy.data.collections.new('STUDIO | presentation only')
scene.collection.children.link(studio)
meshes = []

def move_to(ob, collection):
    for c in list(ob.users_collection): c.objects.unlink(ob)
    collection.objects.link(ob)

def image_from_array(name, rgba, path, noncolor=False):
    h,w,_ = rgba.shape
    im = bpy.data.images.new(name,width=w,height=h,alpha=True)
    if noncolor: im.colorspace_settings.name = 'Non-Color'
    im.pixels.foreach_set(np.clip(rgba,0,1).astype(np.float32).ravel())
    im.filepath_raw = str(path); im.file_format = 'PNG'; im.save()
    return im

def texmat(name, image, metallic=.0, rough=.35, alpha=False, emission=None, normal=None):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt=m.node_tree; bs=nt.nodes.get('Principled BSDF')
    bs.inputs['Metallic'].default_value=metallic
    bs.inputs['Roughness'].default_value=rough
    bs.inputs['Coat Weight'].default_value=.3
    bs.inputs['Coat Roughness'].default_value=.22
    tx=nt.nodes.new('ShaderNodeTexImage'); tx.image=image; tx.location=(-400,140)
    nt.links.new(tx.outputs['Color'],bs.inputs['Base Color'])
    if alpha:
        nt.links.new(tx.outputs['Alpha'],bs.inputs['Alpha'])
        if hasattr(m,'surface_render_method'): m.surface_render_method='DITHERED'
        m.use_backface_culling=False
    if emission:
        et=nt.nodes.new('ShaderNodeTexImage'); et.image=emission; et.location=(-400,-120)
        nt.links.new(et.outputs['Color'],bs.inputs['Emission Color'])
        bs.inputs['Emission Strength'].default_value=.65
    if normal:
        tx2=nt.nodes.new('ShaderNodeTexImage'); tx2.image=normal; tx2.location=(-650,-350)
        nm=nt.nodes.new('ShaderNodeNormalMap'); nm.inputs['Strength'].default_value=.28
        nt.links.new(tx2.outputs['Color'],nm.inputs['Color']); nt.links.new(nm.outputs['Normal'],bs.inputs['Normal'])
    return m

def solid(name,color,metallic=0,rough=.35,emission=0):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    b=m.node_tree.nodes.get('Principled BSDF')
    b.inputs['Base Color'].default_value=(*color,1)
    b.inputs['Metallic'].default_value=metallic; b.inputs['Roughness'].default_value=rough
    b.inputs['Coat Weight'].default_value=.35
    if emission:
        b.inputs['Emission Color'].default_value=(*color,1); b.inputs['Emission Strength'].default_value=emission
    return m

# UV painted body: shaded back, pearl underside, fine staggered scales and living light.
W,H=1024,512
u,v=np.meshgrid(np.linspace(0,1,W),np.linspace(0,1,H))
z=np.sin(v*2*pi)
belly=np.clip((-z-.03)/.75,0,1)[...,None]
back=np.clip((z-.08)/.92,0,1)[...,None]
side=np.array([.055,.54,.58]); pearl=np.array([.79,.9,.82]); dorsal=np.array([.018,.12,.235])
rgb=np.broadcast_to(side,(H,W,3)).copy()
rgb=rgb*(1-belly)+pearl*belly
rgb=rgb*(1-back*.92)+dorsal*back*.92
head=np.clip((u-.73)/.27,0,1)[...,None]
rgb=rgb*(1-head*.28)+np.array([.26,.70,.67])*head*.28
row=np.floor(v*28); cellx=(u*48+(row%2)*.5)%1; celly=(v*28)%1
arc=np.exp(-((np.sqrt(((cellx-.5)*1.5)**2+(celly+.05)**2)-.87)/.045)**2)
scale_mask=np.clip((.88-u)*10,0,1)*np.clip((u-.03)*9,0,1)
rgb+=arc[...,None]*scale_mask[...,None]*np.array([.025,.06,.065])
shimmer=(np.sin(u*73+v*48)*np.sin(v*137-u*15))*.008
rgb+=shimmer[...,None]
lateral=np.exp(-((z-(.06+.055*np.sin(u*11)))/.026)**2)*np.clip((.87-u)*16,0,1)*np.clip((u-.06)*15,0,1)
rgb=rgb*(1-lateral[...,None]*.65)+np.array([.65,.88,.65])*lateral[...,None]*.65
spots=(np.exp(-(((u*17)%1-.5)/.13)**2)*np.exp(-((z+.09+.025*np.sin(u*19))/.045)**2)*np.clip((.77-u)*14,0,1)*np.clip((u-.16)*10,0,1))
rgb+=spots[...,None]*np.array([.1,.21,.21])
rgba=np.concatenate([rgb,np.ones((H,W,1))],axis=-1)
# Reserve a small strip for eye/gill pigments, keeping the opaque fish to one material.
swatches=[(.75,.52,.19),(.006,.013,.028),(.10,.47,.48),(.016,.14,.18),(.68,.98,1)]
strip=np.ones((64,W,4))
for i,color in enumerate(swatches):strip[:,int(i*W/5):int((i+1)*W/5),:3]=color
rgba=np.concatenate([rgba,strip],axis=0)
body_img=image_from_array('Moonveil • painted pearl scales',rgba,OUT/'Textures/Moonveil_Body_BaseColor.png')
em=np.zeros_like(rgba); em[...,3]=1
em[:H,:,:3]=spots[...,None]*np.array([.09,.30,.34])+lateral[...,None]*np.array([.01,.018,.014])
em[H:,int(4*W/5):,:3]=(.18,.35,.4)
em_img=image_from_array('Moonveil • photophores',em,OUT/'Textures/Moonveil_Body_Emission.png')
height=arc*scale_mask
dy,dx=np.gradient(height)
normal=np.stack([-dx*.55,-dy*.65,np.ones_like(dx)],axis=-1)
normal/=np.linalg.norm(normal,axis=-1,keepdims=True)
nmrgba=np.concatenate([normal*.5+.5,np.ones((H,W,1))],axis=-1)
normal_strip=np.ones((64,W,4));normal_strip[:,:,:3]=(.5,.5,1)
nmrgba=np.concatenate([nmrgba,normal_strip],axis=0)
normal_img=image_from_array('Moonveil • scale normals',nmrgba,OUT/'Textures/Moonveil_Body_Normal.png',True)
body_mat=texmat('Moonveil_Body',body_img,.34,.3,emission=em_img,normal=normal_img)

# All membranes share one UV painted atlas. Fine ribs follow root-to-tip UVs.
fu,fv=np.meshgrid(np.linspace(0,1,512),np.linspace(0,1,512))
root=np.array([.06,.4,.46]); tip=np.array([.50,.43,.83])
blend=(fu**1.7)[...,None]
finrgb=root*(1-blend)+tip*blend
rib=np.exp(-((np.sin(fv*pi*18))/.16)**2)
finrgb+=rib[...,None]*np.array([.13,.24,.25])*(.35+.65*fu[...,None])
edge=np.exp(-((fu-.965)/.027)**2)
finrgb=finrgb*(1-edge[...,None]*.4)+np.array([.59,.94,.89])*edge[...,None]*.4
fa=.78-.25*fu+rib*.17+edge*.15
fin_img=image_from_array('Moonveil • silk membranes',np.concatenate([finrgb,fa[...,None]],axis=-1),OUT/'Textures/Moonveil_Fins_BaseColor.png')
fin_mat=texmat('Moonveil_Fins',fin_img,.18,.29,True)
gold_mat=solid('Moonveil_Iris',(.75,.52,.19),.7,.22)
pupil_mat=solid('Moonveil_Pupil',(.006,.013,.028),.15,.1)
rim_mat=solid('Moonveil_EyeRim',(.10,.47,.48),.5,.24)
gill_mat=solid('Moonveil_Gill',(.016,.14,.18),.15,.4)
glint_mat=solid('Moonveil_Glint',(.68,.98,1),.15,.17,.45)

def mesh(name,verts,faces,uvs,mat,weights):
    me=bpy.data.meshes.new(name); me.from_pydata(verts,[],faces); me.update()
    ob=bpy.data.objects.new(name,me); fish_collection.objects.link(ob)
    me.materials.append(mat)
    if uvs:
        layer=me.uv_layers.new(name='UVMap')
        for p in me.polygons:
            for li in p.loop_indices: layer.data[li].uv=uvs[me.loops[li].vertex_index]
    for p in me.polygons: p.use_smooth=True
    for i,ws in enumerate(weights):
        for b,w in ws.items():
            vg=ob.vertex_groups.get(b) or ob.vertex_groups.new(name=b)
            vg.add([i],w,'REPLACE')
    meshes.append(ob)
    return ob

knots=np.array([[-1.25,.025,.065],[-1.08,.058,.10],[-.85,.105,.19],[-.55,.18,.33],[-.18,.25,.44],[.2,.272,.455],[.52,.255,.40],[.80,.21,.30],[1.01,.155,.205],[1.14,.09,.115],[1.19,.018,.048]])
# Smooth interpolator with no dependency on scipy.
def profile(x,axis):
    i=max(0,min(len(knots)-2,int(np.searchsorted(knots[:,0],x)-1)))
    a,b=knots[i,0],knots[i+1,0]; t=(x-a)/(b-a)
    vals=knots[:,axis]
    m0=(vals[min(i+1,len(vals)-1)]-vals[max(i-1,0)])/(knots[min(i+1,len(vals)-1),0]-knots[max(i-1,0),0])
    m1=(vals[min(i+2,len(vals)-1)]-vals[i])/(knots[min(i+2,len(vals)-1),0]-knots[i,0])
    return (2*t**3-3*t*t+1)*vals[i]+(t**3-2*t*t+t)*(b-a)*m0+(-2*t**3+3*t*t)*vals[i+1]+(t**3-t*t)*(b-a)*m1

centers=[(.66,'Spine_Front'),(.0,'Spine_Mid'),(-.62,'Spine_Rear'),(-1.22,'Caudal')]
def body_weights(x):
    if x>=centers[0][0]: return {centers[0][1]:1}
    if x<=centers[-1][0]: return {centers[-1][1]:1}
    for (xa,ba),(xb,bb) in zip(centers,centers[1:]):
        if xb<=x<=xa:
            t=(xa-x)/(xa-xb); t=t*t*(3-2*t)
            return {ba:1-t,bb:t}

verts=[]; uvs=[]; weights=[]; faces=[]
NX,NR=65,40
for i in range(NX):
    x=-1.25+2.44*i/(NX-1)
    for j in range(NR+1):
        a=2*pi*j/NR; y=profile(x,1)*cos(a); z=profile(x,2)*sin(a)
        # Slightly fuller forehead, lifted caudal peduncle.
        z+=.018*cos((x+.2)*1.8)
        verts.append((x,y,z));uvs.append((i/(NX-1),j/NR*H/(H+64)));weights.append(body_weights(x))
for i in range(NX-1):
    for j in range(NR):
        a=i*(NR+1)+j; faces.append((a,a+1,a+NR+2,a+NR+1))
faces.extend([tuple(range(NR-1,-1,-1)),tuple((NX-1)*(NR+1)+j for j in range(NR))])
body=mesh('Moonveil_Body',verts,faces,uvs,body_mat,weights)

def membrane(name,root_func,tip_func,bone,rows=33,cols=7):
    vs=[];uv=[];ws=[];fs=[]
    for j in range(rows):
        t=j/(rows-1); a=Vector(root_func(t)); b=Vector(tip_func(t))
        for k in range(cols):
            s=k/(cols-1); p=a.lerp(b,s)
            p.y+=.018*sin(t*pi*3)*sin(s*pi)
            vs.append(tuple(p));uv.append((s,t))
            base=body_weights(a.x); blend=min(1,s*1.7)
            w={n:v*(1-blend) for n,v in base.items()}; w[bone]=w.get(bone,0)+blend
            ws.append(w)
    for j in range(rows-1):
        for k in range(cols-1):
            a=j*cols+k;fs.append((a,a+1,a+cols+1,a+cols))
    return mesh(name,vs,fs,uv,fin_mat,ws)

membrane('Caudal_Silk',lambda t:(-1.19,0,(t-.5)*.17),
    lambda t:(-1.7-.53*abs(2*t-1)**1.15, .008*sin(t*pi*2), (2*t-1)*(.69+.025*sin(t*pi))), 'Caudal_Fan',41,8)
membrane('Dorsal_Sail',lambda t:(.62-1.56*t,0,profile(.62-1.56*t,2)+.008),
    lambda t:(.62-1.86*t,0,profile(.62-1.56*t,2)+.34*(sin(pi*t)**.72)), 'Dorsal')
membrane('Anal_Sail',lambda t:(-.10-.95*t,0,-profile(-.10-.95*t,2)+.005),
    lambda t:(-.12-1.13*t,0,-profile(-.10-.95*t,2)-.23*sin(pi*t)**.8), 'Anal',25,6)
for side,sign in [('L',-1),('R',1)]:
    membrane('Pectoral_'+side,lambda t:(.56-.12*t,sign*(.215+.02*sin(t*pi)),.00-.15*t),
        lambda t:(.43-.89*t,sign*(.22+.48*sin(t*pi)**.75),-.06-.23*t), 'Pectoral_'+side,25,6)
    membrane('Pelvic_'+side,lambda t:(.12-.15*t,sign*.105,-.37+.015*t),
        lambda t:(.11-.57*t,sign*(.12+.22*sin(t*pi)),-.37-.27*sin(pi*t)**.65), 'Pelvic_'+side,21,5)

def ellipsoid(name,loc,scale,mat,bone,segments=24,rings=12):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=loc)
    ob=bpy.context.object;ob.name=name;ob.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    move_to(ob,fish_collection);ob.data.materials.append(mat)
    for p in ob.data.polygons:p.use_smooth=True
    vg=ob.vertex_groups.new(name=bone);vg.add(list(range(len(ob.data.vertices))),1,'REPLACE')
    meshes.append(ob);return ob

# Eyes are slightly raised, inset into a turquoise ring; black pupils remain readable at distance.
for side,sign in [('L',-1),('R',1)]:
    loc=(.81,sign*.178,.155)
    ellipsoid('Eye_Rim_'+side,loc,(.085,.025,.085),rim_mat,'Spine_Front')
    ellipsoid('Golden_Iris_'+side,(.815,sign*.197,.16),(.067,.021,.067),gold_mat,'Spine_Front')
    ellipsoid('Pupil_'+side,(.834,sign*.214,.16),(.038,.009,.046),pupil_mat,'Spine_Front')
    ellipsoid('Catchlight_'+side,(.846,sign*.223,.18),(.010,.003,.011),glint_mat,'Spine_Front',12,8)

def tube(name,points,radius,mat,bone,steps=6):
    vs=[];uv=[];fs=[]
    for i,p in enumerate(points):
        p=Vector(p); prev=Vector(points[max(0,i-1)]);nxt=Vector(points[min(len(points)-1,i+1)])
        tangent=(nxt-prev).normalized(); guide=Vector((0,1,0))
        if abs(tangent.dot(guide))>.9:guide=Vector((0,0,1))
        b=tangent.cross(guide).normalized();c=tangent.cross(b).normalized()
        r=radius*(.4+.6*max(0,sin(pi*i/(len(points)-1)))**.3)
        for j in range(steps):
            q=p+r*(b*cos(j*2*pi/steps)+c*sin(j*2*pi/steps));vs.append(q);uv.append((j/steps,i/(len(points)-1)))
    for i in range(len(points)-1):
        for j in range(steps):
            a=i*steps+j;b=i*steps+(j+1)%steps;fs.append((a,b,b+steps,a+steps))
    return mesh(name,vs,fs,uv,mat,[{bone:1}]*len(vs))

for side,sign in [('L',-1),('R',1)]:
    pts=[]
    for i in range(26):
        t=i/25; zz=.27-.54*t;xx=.49-.105*sin(t*pi)
        yy=sign*(profile(xx,1)*math.sqrt(max(.02,1-(zz/profile(xx,2))**2))+.008)
        pts.append((xx,yy,zz))
    tube('Gill_Arc_'+side,pts,.008,gill_mat,'Spine_Front')
    # Small mouth seam instead of a cartoon protruding lip.
    pts=[(1.18-.11*t,sign*(.024+.087*t),-.018-.035*sin(pi*t)) for t in np.linspace(0,1,14)]
    tube('Mouth_'+side,pts,.009,gill_mat,'Jaw')

# A clean generic skeleton; all deformations are skin weights (no runtime modifiers).
bpy.ops.object.armature_add(enter_editmode=True,location=(0,0,0))
rig=bpy.context.object;rig.name='Moonveil_Rig';move_to(rig,fish_collection)
arm=rig.data;arm.name='Moonveil_Skeleton';arm.edit_bones.remove(arm.edit_bones[0])
def bone(name,head,tail,parent=None):
    b=arm.edit_bones.new(name);b.head=head;b.tail=tail
    if parent:b.parent=arm.edit_bones[parent]
    return b
bone('Root',(0,0,0),(0,0,.2))
bone('Spine_Front',(.78,0,0),(.16,0,0),'Root')
bone('Spine_Mid',(.16,0,0),(-.46,0,0),'Spine_Front')
bone('Spine_Rear',(-.46,0,0),(-1.12,0,0),'Spine_Mid')
bone('Caudal',(-1.12,0,0),(-1.48,0,0),'Spine_Rear')
bone('Caudal_Fan',(-1.48,0,0),(-2.08,0,0),'Caudal')
bone('Dorsal',(.12,0,.33),(-.22,0,.69),'Spine_Mid')
bone('Anal',(-.38,0,-.25),(-.70,0,-.51),'Spine_Mid')
bone('Jaw',(1.00,0,-.04),(1.17,0,-.055),'Spine_Front')
for side,sign in [('L',-1),('R',1)]:
    bone('Pectoral_'+side,(.50,sign*.22,-.04),(.02,sign*.58,-.21),'Spine_Front')
    bone('Pelvic_'+side,(.04,sign*.1,-.36),(-.29,sign*.24,-.56),'Spine_Mid')
bpy.ops.object.mode_set(mode='OBJECT')
rig.show_in_front=True
for ob in meshes:
    ob.parent=rig
    mod=ob.modifiers.new('Moonveil Skin','ARMATURE');mod.object=rig
for pb in rig.pose.bones:pb.rotation_mode='XYZ'

# One baked timeline, four independent actions via NLA for FBX + GLB interoperability.
clips=[('Hover',1,91,1.0,.40),('Swim',111,159,1.0,1.0),('Dart',181,205,1.0,1.85),('Feed',231,291,1.0,.55)]
actions=[]
rig.animation_data_create()
for cname,start,end,cycles,strength in clips:
    action=bpy.data.actions.new(cname);action.use_fake_user=True;rig.animation_data.action=action
    for frame in range(start,end+1):
        t=(frame-start)/(end-start);ph=t*2*pi*cycles
        for pb in rig.pose.bones:pb.rotation_euler=(0,0,0);pb.location=(0,0,0)
        # Longitudinal bones have local Z = world Z: lateral undulation, not dolphin pitch.
        for i,bn in enumerate(['Spine_Front','Spine_Mid','Spine_Rear','Caudal','Caudal_Fan']):
            amp=[.025,.075,.15,.20,.16][i]*strength
            rig.pose.bones[bn].rotation_euler.z=sin(ph-i*.65)*amp
        rig.pose.bones['Dorsal'].rotation_euler.y=.075*sin(ph-1.0)
        rig.pose.bones['Anal'].rotation_euler.y=.08*sin(ph-1.6)
        for side,sign in [('L',-1),('R',1)]:
            rig.pose.bones['Pectoral_'+side].rotation_euler.y=sign*(.20*sin(ph*2+.35)+.08)*(.7 if cname=='Dart' else 1)
            rig.pose.bones['Pelvic_'+side].rotation_euler.y=sign*.12*sin(ph+.5)
        if cname=='Feed':
            pulse=sin(pi*t)**2
            rig.pose.bones['Root'].rotation_euler.z=-.19*pulse
            rig.pose.bones['Spine_Front'].rotation_euler.x=.08*pulse
            rig.pose.bones['Jaw'].rotation_euler.x=.22*pulse*(.55+.45*sin(t*6*pi)**2)
        for pb in rig.pose.bones:
            pb.keyframe_insert(data_path='rotation_euler',frame=frame,group=pb.name)
    actions.append(action)
    # Force constant-rate sampled keys for consistent export in layered Action versions.
    if hasattr(action,'layers'):
        for layer in action.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for fc in bag.fcurves:
                        for kp in fc.keyframe_points:kp.interpolation='LINEAR'
    elif hasattr(action,'fcurves'):
        for fc in action.fcurves:
            for kp in fc.keyframe_points:kp.interpolation='LINEAR'
    rig.animation_data.action=None
    track=rig.animation_data.nla_tracks.new();track.name=cname
    strip=track.strips.new(cname,start,action);strip.name=cname;strip.extrapolation='NOTHING'
scene.frame_start=1;scene.frame_end=291
scene.frame_set(111)
for cname,start,end,_,_ in clips:
    scene.timeline_markers.new(cname,frame=start)

# Atlas all small opaque details; merge to exactly two skinned meshes / material slots.
lookup={gold_mat.name:0,pupil_mat.name:1,rim_mat.name:2,gill_mat.name:3,glint_mat.name:4}
for ob in meshes:
    slot=lookup.get(ob.data.materials[0].name)
    if slot is not None:
        for loop in ob.data.uv_layers.active.data:loop.uv=((slot+.5)/5,(H+32)/(H+64))
        ob.data.materials.clear();ob.data.materials.append(body_mat)
groups=[[ob for ob in meshes if ob.data.materials[0]==body_mat],[ob for ob in meshes if ob.data.materials[0]==fin_mat]]
merged=[]
for group,label in zip(groups,['Moonveil_Opaque','Moonveil_Membranes']):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in group:ob.select_set(True)
    bpy.context.view_layer.objects.active=group[0]
    bpy.ops.object.join();ob=bpy.context.object;ob.name=label;merged.append(ob)
meshes=merged

# The preview camera and soft light rig are not exported.
def aim(ob,point):ob.rotation_euler=(Vector(point)-ob.location).to_track_quat('-Z','Y').to_euler()
def area(name,loc,power,color,size,target=(-.35,0,0)):
    d=bpy.data.lights.new(name,'AREA');d.energy=power;d.color=color;d.shape='DISK';d.size=size
    ob=bpy.data.objects.new(name,d);studio.objects.link(ob);ob.location=loc;aim(ob,target)
area('Key • soft ocean', (1.5,-3.8,5),650,(.60,.87,1),4)
area('Rim • lavender',(-1.7,2.1,2.4),800,(.65,.46,1),3)
area('Fill • sea glass',(3,2,.1),450,(.35,1,.84),3)
area('Pearl bounce',(-.2,-2,-3),180,(.48,.70,.95),3)
camera_data=bpy.data.cameras.new('Moonveil portrait');camera=bpy.data.objects.new('Moonveil portrait',camera_data)
studio.objects.link(camera);scene.camera=camera
camera.location=(3.5,-7.2,2.1);aim(camera,(-.4,0,.04));camera_data.type='ORTHO';camera_data.ortho_scale=4.25;camera_data.lens=50

# Select exportable meshes plus armature only.
bpy.ops.object.select_all(action='DESELECT')
for ob in [rig]+meshes:ob.select_set(True)
bpy.context.view_layer.objects.active=rig
scene.frame_set(111)
fbx=OUT/'Moonveil.fbx'
bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'ARMATURE','MESH'},
    add_leaf_bones=False,use_armature_deform_only=True,bake_anim=True,bake_anim_use_nla_strips=True,
    bake_anim_use_all_actions=False,bake_anim_force_startend_keying=True,bake_anim_simplify_factor=0,
    axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=False,mesh_smooth_type='FACE')
bpy.ops.export_scene.gltf(filepath=str(SOURCE/'Moonveil.glb'),export_format='GLB',use_selection=True,use_active_scene=True,
    export_animations=True,export_animation_mode='NLA_TRACKS',export_skins=True,export_yup=True,
    export_extras=True,export_current_frame=False,export_rest_position_armature=True)

scene.render.filepath=str(PREVIEW/'Moonveil_Hero.png')
for im in [body_img,em_img,normal_img,fin_img]:im.pack()
rig['asset_name']='Moonveil / 月光鱼'
rig['forward_axis']='+X in Blender; prefab +Z in Unity'
rig['clips']='Hover (3s loop), Swim (1.6s loop), Dart (0.8s loop), Feed (2s one shot)'
rig['authorship']='Original procedural geometry and textures for Boids teaching project.'
triangles=sum(sum(len(p.vertices)-2 for p in ob.data.polygons) for ob in meshes)
report={'asset':'Moonveil','blender_version':bpy.app.version_string,'mesh_objects':len(meshes),
    'vertices':sum(len(ob.data.vertices) for ob in meshes),'triangles':triangles,'bones':len(arm.bones),
    'clips':[{'name':n,'start':s,'end':e,'seconds':(e-s)/30,'loop':n!='Feed'} for n,s,e,_,_ in clips],
    'fbx':fbx.relative_to(BASE).as_posix(),'blend':(SOURCE/'Moonveil.blend').relative_to(BASE).as_posix(),
    'glb':(SOURCE/'Moonveil.glb').relative_to(BASE).as_posix()}
(SOURCE/'asset-report.json').write_text(json.dumps(report,indent=2,ensure_ascii=False),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Moonveil.blend'))
# Frame the created fish in the live viewport.
for area_ui in (bpy.context.screen.areas if bpy.context.screen else []):
    if area_ui.type=='VIEW_3D':
        area_ui.spaces.active.region_3d.view_perspective='CAMERA'
        area_ui.spaces.active.shading.type='MATERIAL'
print(json.dumps(report,ensure_ascii=False))
