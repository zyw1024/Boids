"""Export the reviewed live Blender kit for the deterministic Unity pipeline."""
import bpy, numpy as np, json
from pathlib import Path
from mathutils import Vector
OUT=Path(r'C:\Users\Administrator\Desktop\游戏开发备课\Projects\Sky_City_Project\ArtSource\SkyCity\Rocks')
original=bpy.context.window.scene;summary=[]
for title,name,ratios in [('Limestone cliff - editable scanned faces','Cliff',[1,.25,.09,.025]),('Limestone garden boulder','Boulder',[1,.5,.2,.06])]:
    scene=bpy.data.scenes[title];bpy.context.window.scene=scene
    sources=list(scene.objects)
    for lod,ratio in enumerate(ratios):
        points=[];normals=[];uvs=[];triangles=[]
        for source in sources:
            ob=source.copy();ob.data=source.data.copy();scene.collection.objects.link(ob);bpy.context.view_layer.objects.active=ob
            if ratio<1:
                modifier=ob.modifiers.new('Reviewed distance silhouette','DECIMATE');modifier.ratio=ratio
                bpy.ops.object.modifier_apply(modifier=modifier.name)
            me=ob.data;me.calc_loop_triangles();uv=me.uv_layers.active.data;lookup={}
            for tri in me.loop_triangles:
                indices=[]
                for li in tri.loops:
                    vi=me.loops[li].vertex_index;q=uv[li].uv;key=(vi,round(q.x,7),round(q.y,7))
                    if key not in lookup:
                        lookup[key]=len(points);v=ob.matrix_world@me.vertices[vi].co;n=ob.matrix_world.to_3x3().inverted().transposed()@me.vertices[vi].normal;n.normalize()
                        points.append((-v.x,v.z,-v.y));normals.append((-n.x,n.z,-n.y));uvs.append(tuple(q))
                    indices.append(lookup[key])
                triangles.append(indices[::-1])
            mesh=ob.data;bpy.data.objects.remove(ob,do_unlink=True);bpy.data.meshes.remove(mesh)
        np.savez_compressed(OUT/(name+'_LOD'+str(lod)+'.npz'),p=np.asarray(points,dtype='<f4'),n=np.asarray(normals,dtype='<f4'),uv=np.asarray(uvs,dtype='<f4'),t=np.asarray(triangles,dtype='<i4'))
        summary.append(dict(asset=name,lod=lod,vertices=len(points),triangles=len(triangles)))
bpy.context.window.scene=original
(OUT/'KitReport.json').write_text(json.dumps(dict(authoring='Live Blender 5.2.2 through Blender MCP',reference='ArtSource/SkyCity/SkyCity_Concept.png',source='https://polyhaven.com/a/rock_face_01',license='CC0',objects=summary),indent=2))
print(json.dumps(summary))
