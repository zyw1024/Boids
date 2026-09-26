"""Authored limestone silhouettes, executed in an inspected Blender MCP session.

Large masses are explicitly placed per island; only small erosion is stochastic.
Coordinates and exports are Unity Y-up. All solids are closed and outward-facing.
Existing scenes and the original scanned reference are preserved.
"""
import bpy, bmesh, math, random, json, hashlib
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.noise import noise_vector

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
SCENE_NAME = 'Limestone - authored cliff family'

# (x, z, width, breadth, top, depth, lean_x, lean_z, end_width)
# The large footprint supports the existing architecture; the pendant masses
# distinguish each silhouette, with uneven roots and genuine intervening gaps.
DESIGNS = {
 'Main': [(0,0,1.02,1.02,.02,.60,-.05,.04,.52),
          (-.67,-.42,.42,.49,-.06,.98,.11,-.04,.09),
          (-.13,-.65,.47,.37,-.08,1.38,-.16,.09,.035),
          (.53,-.42,.48,.48,-.03,.99,-.20,-.10,.07),
          (.68,.32,.38,.48,-.08,.80,-.07,.04,.10),
          (-.55,.52,.43,.37,-.10,.87,.10,.03,.09)],
 'Terrace': [(0,0,1.02,1.03,.02,.50,.10,-.05,.65),
             (-.48,-.33,.51,.59,-.09,1.24,.25,.12,.045),
             (.41,.08,.53,.74,-.04,.92,-.04,.17,.055),
             (.57,-.55,.31,.33,-.14,.62,-.12,.05,.10)],
 'Pavilion': [(0,0,1.02,1.03,.02,.43,-.04,.05,.72),
              (-.42,-.17,.60,.76,-.08,1.22,.14,.06,.045),
              (.47,.26,.52,.63,-.02,.94,-.06,-.06,.065),
              (.24,-.64,.32,.40,-.12,.65,.05,.04,.08)],
 'Plateau': [(0,0,1.04,1.01,.02,.40,.04,.04,.74),
             (-.64,-.30,.40,.57,-.06,.61,.08,.04,.11),
             (.03,-.64,.54,.34,-.07,.76,-.06,.13,.12),
             (.66,.10,.39,.64,-.07,.56,-.06,-.03,.15),
             (-.17,.55,.67,.45,-.10,.49,.10,-.08,.25)],
 'Split': [(0,0,1.01,1.04,.02,.36,0,.08,.80),
           (-.49,-.21,.60,.75,-.07,1.17,-.06,.08,.065),
           (.53,.04,.51,.87,-.05,.93,.08,-.09,.045),
           (-.11,.66,.53,.36,-.12,.51,.02,-.10,.12)],
 'Blade': [(0,0,1.03,1.02,.02,.40,-.03,.04,.76),
           (-.28,-.21,.66,.74,-.05,1.25,-.29,.09,.04),
           (.62,-.31,.36,.52,-.14,.72,-.30,.07,.065),
           (.08,.66,.74,.38,-.13,.54,-.15,.0,.13)],
 'Needle': [(0,0,1.02,1.00,.02,.35,-.04,.06,.82),
            (-.15,-.16,.75,.82,-.09,1.30,.09,.07,.018),
            (-.75,.15,.29,.45,-.05,.60,.08,.14,.07),
            (.69,-.20,.37,.47,-.03,.73,-.15,.03,.04)],
 'Saddle': [(0,0,1.05,1.02,.02,.32,0,0,.81),
            (-.63,.04,.40,.92,-.08,.82,.07,-.05,.065),
            (.56,-.13,.45,.83,-.08,.92,-.01,.11,.04),
            (.0,.58,.57,.43,-.10,.46,.06,.04,.12)],
 'Broken': [(0,0,1.03,1.03,.02,.42,-.03,.05,.67),
            (-.59,-.47,.45,.47,-.01,.82,.09,.02,.075),
            (.17,-.66,.45,.34,-.09,1.04,-.14,.05,.035),
            (.62,.0,.38,.72,-.04,.62,-.12,.04,.16),
            (-.48,.56,.49,.41,-.08,.61,.08,.02,.10)],
}

def native(p): return (-p[0],-p[2],p[1])
def unity(p): return (-p[0],p[2],-p[1])
def smooth(a): return a*a*(3-2*a)

def make_mass(name, spec, seed, collection, material):
    rng=random.Random(seed)
    cx,cz,sx,sz,top,depth,lx,lz,end=spec
    # Long fracture planes, with a few unequal cross-cutting ledges. The ledges
    # dip together, while individual corners break off at different heights.
    sides=9; segments=10; rings=60
    angles=[2*math.pi*i/sides+rng.uniform(-.065,.065) for i in range(sides)]
    radii=[rng.uniform(.88,1.08) for _ in range(sides)]
    base=[Vector((math.cos(a)*r,math.sin(a)*r)) for a,r in zip(angles,radii)]
    levels=[.0,.13,.17,.35,.39,.62,.66,.84,1.0]
    widths=[1.0,.98,.975,.93,.89,.73,.66,.39,end]
    # Core has a broad torn underside; long hanging ribs taper to broken points.
    if end>.45: widths=[1,1,.99,.96,.93,.85,.82,.70,end]
    tilt=rng.uniform(-.075,.075)
    coords=[]; faces=[]
    for row in range(rings+1):
        t=row/rings
        k=next((k for k in range(len(levels)-1) if t<=levels[k+1]),len(levels)-2)
        f=(t-levels[k])/(levels[k+1]-levels[k])
        scale=widths[k]*(1-f)+widths[k+1]*f
        for i in range(sides):
            for j in range(segments):
                u=j/segments
                q=base[i].lerp(base[(i+1)%sides],u)
                # Broad concave joints between planar ribs, not sinusoidal rings.
                joint=(max(0,1-abs(u-.14)/.24)**2)*.13*math.sin(math.pi*t)
                # A fracture terminates at a different height on each face;
                # no complete horizontal bands wrap around the rock.
                break_t=.28+.34*(.5+.5*math.sin(seed+i*2.37))
                ledge=.06*max(0,1-abs(t-break_t)/.045)*math.sin(math.pi*u)**2
                section=scale-joint+ledge
                x=cx+q.x*sx*section+lx*t
                z=cz+q.y*sz*section+lz*t
                y=top-depth*t+depth*(.055*q.x+tilt*q.y)*math.sin(math.pi*t)
                # Low-amplitude weathering after the silhouette is established.
                n=noise_vector(Vector((x*8.3+seed*.37,y*5.1,z*8.3)))
                fine=noise_vector(Vector((x*31+seed,y*23,z*31)))
                strength=min(sx,sz,depth)*(.13*min(1,t*12))
                x+=(n.x+.24*fine.x)*strength
                y+=(n.y+.24*fine.y)*strength*.55
                z+=(n.z+.24*fine.z)*strength
                coords.append(native((x,y,z)))
    count=sides*segments
    for row in range(rings):
        for i in range(count):
            a=row*count+i;b=row*count+(i+1)%count
            faces.append((a,b,b+count,a+count))
    faces.append(tuple(range(count-1,-1,-1)))
    faces.append(tuple(rings*count+i for i in range(count)))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(coords,[],faces);mesh.update()
    bm=bmesh.new();bm.from_mesh(mesh);bm.verts.ensure_lookup_table()
    # Preserve the sharp edges of the authored vertical fracture planes.
    seams=[e for e in bm.edges if abs(e.verts[0].index-e.verts[1].index)==count and e.verts[0].index%segments==0]
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bmesh.ops.split_edges(bm,edges=seams);bm.to_mesh(mesh);bm.free()
    for face in mesh.polygons: face.use_smooth=True
    mesh.materials.append(material)
    uv=mesh.uv_layers.new(name='Local fracture coordinates')
    for loop in mesh.loops:
        p=unity(mesh.vertices[loop.vertex_index].co);uv.data[loop.index].uv=(p[0],p[1])
    color=mesh.color_attributes.new(name='Pigment',type='FLOAT_COLOR',domain='POINT')
    for v,c in zip(mesh.vertices,color.data):
        p=unity(v.co); variation=.96+.045*math.sin(p[1]*8+seed)
        c.color=(.49*variation,.465*variation,.405*variation,1)
    ob=bpy.data.objects.new(name,mesh);collection.objects.link(ob)
    ob['Surface family']='Rock';ob['design_role']='Core cliff' if end>.45 else 'Descending fracture buttress'
    anchors=[]
    for t in ((.18,) if end>.45 else (.23,.52)):
        row=round(t*rings)
        ring=[unity(Vector(p)) for p in coords[row*count:(row+1)*count]]
        anchor=min(ring,key=lambda p:p[2]);anchors.append((anchor[0],anchor[1]+.015,anchor[2]-.016))
    ob['plant_anchors']=json.dumps(anchors)
    return ob

def export_family(scene, by_name):
    report=[]
    for name,objects in by_name.items():
        for lod,ratio in enumerate((1,.36,.13,.045)):
            points=[];normals=[];uvs=[];colors=[];triangles=[]
            for source in objects:
                ob=source.copy();ob.data=source.data.copy();scene.collection.objects.link(ob)
                bpy.context.view_layer.objects.active=ob
                if ratio<1:
                    mod=ob.modifiers.new('Distance silhouette','DECIMATE');mod.ratio=ratio
                    bpy.ops.object.modifier_apply(modifier=mod.name)
                mesh=ob.data;mesh.calc_loop_triangles();start=len(points)
                points.extend(unity(v.co) for v in mesh.vertices)
                normals.extend(unity(v.normal) for v in mesh.vertices)
                colors.extend(tuple(c.color) for c in mesh.color_attributes['Pigment'].data)
                uvs.extend((p[0],p[1]) for p in points[start:])
                triangles.extend(tuple(start+i for i in reversed(tri.vertices)) for tri in mesh.loop_triangles)
                bpy.data.objects.remove(ob,do_unlink=True);bpy.data.meshes.remove(mesh)
            path=OUT/('Cliff_'+name+'_LOD%d.npz'%lod)
            anchors=[a for ob in objects for a in json.loads(ob['plant_anchors'])]
            np.savez_compressed(path,p=np.asarray(points,dtype='<f4'),n=np.asarray(normals,dtype='<f4'),c=np.asarray(colors,dtype='<f4'),uv=np.asarray(uvs,dtype='<f4'),t=np.asarray(triangles,dtype='<i4'),anchors=np.asarray(anchors,dtype='<f4'))
            report.append(dict(design=name,lod=lod,vertices=len(points),triangles=len(triangles),hash=hashlib.sha256(np.asarray(points,dtype='<f4').tobytes()).hexdigest()))
    (OUT/'AuthoredCliffs.json').write_text(json.dumps(dict(designs=list(DESIGNS),assets=report),indent=2))

def build():
    if bpy.data.scenes.get(SCENE_NAME):
        old=bpy.data.scenes[SCENE_NAME]
        for ob in list(old.objects):
            if ob.users_scene==(old,):bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.scenes.remove(old)
    scene=bpy.data.scenes.new(SCENE_NAME);bpy.context.window.scene=scene
    mat=bpy.data.materials.get('Limestone clay review') or bpy.data.materials.new('Limestone clay review')
    mat.use_nodes=True;mat.diffuse_color=(.58,.57,.53,1)
    bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    bsdf.inputs['Base Color'].default_value=(.58,.57,.53,1);bsdf.inputs['Roughness'].default_value=.9
    by_name={}
    for idx,(name,specs) in enumerate(DESIGNS.items()):
        collection=bpy.data.collections.new('Cliff / '+name);scene.collection.children.link(collection)
        parts=[make_mass(name+' / '+str(j),spec,811+idx*103+j*71,collection,mat) for j,spec in enumerate(specs)]
        for ob in parts:ob.location=(idx%3*3.4,idx//3*3.4,0)
        by_name[name]=parts
    export_family(scene,by_name)
    scene['design_note']='Nine distinct authored geological mass compositions; no scanned-face ring.'
    for area in bpy.context.screen.areas:
        if area.type=='VIEW_3D':
            s=area.spaces.active;s.shading.type='SOLID';s.overlay.show_overlays=False
            s.region_3d.view_location=(3.3,3.3,-.45);s.region_3d.view_distance=13
            s.region_3d.view_rotation=Vector((3,-8,5)).to_track_quat('Z','Y')
    bpy.data.libraries.write(str(OUT/'AuthoredCliffs.blend'),{scene},fake_user=True,compress=True)
    print('Authored cliff family:',[(k,len(v)) for k,v in by_name.items()])

if __name__=='__main__': build()
