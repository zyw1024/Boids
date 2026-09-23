"""Render a labeled animation reel plus side/front inspection images."""
import bpy, math, json, sys
import numpy as np
from pathlib import Path
from mathutils import Vector

scene=bpy.data.scenes['Moonveil_Studio'];bpy.context.window.scene=scene
rig=bpy.data.objects['Moonveil_Rig']; camera=scene.camera
out=Path(bpy.data.filepath).parent/'Preview';frames=out/'Frames';frames.mkdir(exist_ok=True)
scene.render.engine='CYCLES';scene.cycles.samples=12;scene.cycles.use_denoising=True
scene.render.resolution_x=1080;scene.render.resolution_y=720
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'

def aim(point):camera.rotation_euler=(Vector(point)-camera.location).to_track_quat('-Z','Y').to_euler()
report={'clips':[], 'weight_errors':[]}
meshes=[o for o in scene.objects if o.type=='MESH']
for ob in meshes:
    for v in ob.data.vertices:
        total=sum(g.weight for g in v.groups)
        if abs(total-1)>1e-4:report['weight_errors'].append([ob.name,v.index,total])

def coordinates(frame):
    scene.frame_set(frame);deps=bpy.context.evaluated_depsgraph_get();arr=[]
    for ob in meshes:
        eo=ob.evaluated_get(deps);me=eo.to_mesh();a=np.empty(len(me.vertices)*3);me.vertices.foreach_get('co',a);arr.append(a);eo.to_mesh_clear()
    return np.concatenate(arr)
for name,start,end,loop in [('Hover',1,91,True),('Swim',111,159,True),('Dart',181,205,True),('Feed',231,291,False)]:
    a=coordinates(start);b=coordinates(end);c=coordinates(start+(end-start)//4)
    report['clips'].append({'name':name,'loop':loop,'endpoint_max_delta':float(np.max(abs(a-b))),'quarter_cycle_max_delta':float(np.max(abs(a-c)))})
(out/'blender-animation-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')

# Inspection stills use the same materials and rig as the delivery model.
for label,loc,scale in [('Side',(-.35,-8,.15),4.15),('Front',(8,-.4,.15),2.8)]:
    camera.location=loc;camera.data.ortho_scale=scale;aim((-.35,0,0));scene.frame_set(123)
    scene.render.filepath=str(out/('Moonveil_'+label+'.png'));bpy.ops.render.render(write_still=True)

camera.location=(3.5,-7.2,2.1);camera.data.ortho_scale=4.65;aim((-.4,0,.04))
mat=bpy.data.materials.new('Preview type');mat.use_nodes=True
bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(.55,.85,.88,1)
bs.inputs['Emission Color'].default_value=(.55,.85,.88,1);bs.inputs['Emission Strength'].default_value=1
def label(name,x,y,size):
    curve=bpy.data.curves.new(name,'FONT');curve.size=size;curve.extrude=0
    ob=bpy.data.objects.new(name,curve);scene.collection.objects.link(ob);curve.materials.append(mat)
    ob.parent=camera;ob.location=(x,y,-4);return curve
heading=label('Motion label',-2.12,1.20,.14)
sub=label('Caption',-2.12,1.02,.055);sub.body='MOONVEIL  /  ORIGINAL BOIDS ASSET'
footer=label('Footer',-2.12,-1.30,.047);footer.body='13 BONES    /    4 MOTIONS    /    BLENDER + UNITY'
idx=1
for name,start,end,length in [('HOVER',1,91,3.0),('SWIM',111,159,3.2),('DART',181,205,1.6),('FEED',231,291,2.0)]:
    heading.body=name
    for k in range(round(length*24)):
        duration=(end-start)/30
        t=k/24
        frame=start+(t%duration)*30
        scene.frame_set(int(frame),subframe=frame%1)
        scene.render.filepath=str(frames/f'{idx:04d}.png');bpy.ops.render.render(write_still=True)
        idx+=1
print('MOONVEIL_REEL_COMPLETE',idx-1)
