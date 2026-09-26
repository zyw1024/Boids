"""Execute in the inspected live Blender session through Blender MCP.

The editable pieces use Poly Haven's CC0 Rock Face 01 scan by Dario Barresi.
Composition follows SkyCity_Concept.png; no procedural noise makes the rock shape.
"""
import bpy, math, random
from pathlib import Path
from mathutils import Vector

ROOT=Path(r'C:\Users\Administrator\Desktop\游戏开发备课\Projects\Sky_City_Project')
hero=bpy.data.scenes['Sky City - concept rock reconstruction']
source=bpy.data.objects['rock_face_01']
kit=bpy.data.scenes.new('Limestone cliff - editable scanned faces')
bpy.context.window.scene=kit
material=source.data.materials[0].copy();material.name='Warm grey limestone - concept reference'
nodes=material.node_tree.nodes;links=material.node_tree.links
bsdf=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
diff=next(n for n in nodes if n.type=='TEX_IMAGE' and 'diff' in n.image.name.lower())
hue=nodes.new('ShaderNodeHueSaturation');hue.inputs['Saturation'].default_value=.20;hue.inputs['Value'].default_value=1.45
links.new(diff.outputs['Color'],hue.inputs['Color'])
tone=nodes.new('ShaderNodeMixRGB')
assert 'MULTIPLY' in [i.identifier for i in tone.bl_rna.properties['blend_type'].enum_items]
tone.blend_type='MULTIPLY';tone.inputs[0].default_value=1;tone.inputs[2].default_value=(1.10,1.04,.91,1)
links.new(hue.outputs['Color'],tone.inputs[1]);links.new(tone.outputs['Color'],bsdf.inputs['Base Color'])
bsdf.inputs['Roughness'].default_value=.88

base=source.copy();base.data=source.data.copy();kit.collection.objects.link(base)
bpy.context.view_layer.objects.active=base;base.select_set(True)
reduce=base.modifiers.new('Retain scanned fracture planes','DECIMATE');reduce.ratio=.20
bpy.ops.object.modifier_apply(modifier=reduce.name)
template=base.data.copy();bpy.data.objects.remove(base,do_unlink=True)
pieces=[]
for j in range(14):
    a=j*math.tau/14+.045*math.sin(j*2.1)
    ob=bpy.data.objects.new('Cliff face %02d'%(j+1),template.copy());kit.collection.objects.link(ob)
    ob.data.materials.clear();ob.data.materials.append(material)
    reach=.77+.21*(.5+.5*math.sin(j*2.39+.3))
    for vertex in ob.data.vertices:
        x,y,z=vertex.co;u=(x-.100935)/7.108232;t=1-(z+.058686)/4.980437
        # Compensate the scan's original ground slope before laying its faces
        # around an undercut island. Keep its fractures and uneven boundary.
        residual=(y-.78*z)*.115
        radius=1.00-.47*t-residual
        tangent=u*(math.tau/14)*1.55+.045*math.sin(j*1.7)*t
        vertex.co=(math.cos(a)*radius-math.sin(a)*tangent,math.sin(a)*radius+math.cos(a)*tangent,-t*reach+.024)
    ob.data.update();pieces.append(ob)
    ob['source']='https://polyhaven.com/a/rock_face_01';ob['author']='Dario Barresi';ob['license']='CC0'
kit['reference']='ArtSource/SkyCity/SkyCity_Concept.png'
kit['design']='Overlapping scanned fracture faces; staggered roots; warm grey limestone'

# Link editable instances below the actual architectural model for proportion QA.
bpy.context.window.scene=hero
old=next(o for o in hero.objects if o.name.startswith('06 - '));old.hide_render=True;old.hide_set(True)
for title,center,scale in [('Main',(18,-12,5.7),(18.5,10.5,13.5)),('Water terrace',(10.7,-1.3,3.2),(6.6,5.2,11.5)),('East pavilion',(-20.2,-15.3,5.8),(5,4.5,8.3))]:
    for j,part in enumerate(pieces):
        ob=part.copy();ob.name='%s / fracture %02d'%(title,j+1);hero.collection.objects.link(ob)
        ob.location=center;ob.scale=scale
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        s=area.spaces.active;s.shading.type='MATERIAL';s.overlay.show_overlays=False
        s.region_3d.view_location=(8,-9,8);s.region_3d.view_distance=59
        s.region_3d.view_rotation=Vector((-22,50,15)).to_track_quat('Z','Y')
print('Editable scan composition ready:',len(pieces),'faces; reference',hero.name)
