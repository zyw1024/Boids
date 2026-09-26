"""Garden islands: coherent cliffs, tiered arcades and eight landmark silhouettes.

Reuses the source sculpting vocabulary of the authored Sky City. No FBX scene is
instantiated per chunk. All meshes are baked into the bounded shared atlas.
"""
from pathlib import Path
from math import sin,cos,pi,sqrt,exp
from mathutils import Vector
import random,ast,bpy
from structural_vocabulary import install,curved_stair,straight_stair,retaining_arcades
from hero_style import install_reference

NAMES=['Palace of the Wind','Cypress Abbey','Hanging Gardens','The Great Library',
       'Astronomers Court','The Spring Garden','Garden Village','The Open Cloister']

def vocabulary(lod):
    path=Path(__file__).resolve().parents[1]/'build_sky_city.py'
    source=path.read_text(encoding='utf-8').split('# The dominant city:')[0]
    # Keep definitions and palette assignments, omit Blender scene/file side effects.
    tree=ast.parse(source)
    keep=[]
    for node in tree.body:
        if isinstance(node,(ast.Import,ast.ImportFrom,ast.FunctionDef,ast.ClassDef)):keep.append(node)
        elif isinstance(node,ast.Assign):
            names=[t.id for t in node.targets if isinstance(t,ast.Name)]
            if names and names[0] not in ('ROOT','OUT','SOURCE','scene'):keep.append(node)
    code=ast.unparse(ast.Module(body=keep,type_ignores=[]))
    # Curved architectural profiles remain round even in the far LOD.
    replacements={
      'rows = 26':f'rows = {[16,10,5][lod]}','cols = 96':f'cols = {[64,40,24][lod]}',
      'rows = 30':f'rows = {[24,14,8][lod]}','cols = 100':f'cols = {[80,48,28][lod]}',
      'n=80':f'n={[64,40,24][lod]}','range(290)':f'range({[48,12,0][lod]})',
      'range(82)':f'range({[18,6,0][lod]})','range(15)':f'range({[15,9,5][lod]})',
    }
    for old,new in replacements.items():code=code.replace(old,new)
    code=code.replace('r * 0.36 + 0.38','r * 0.36')
    env={'__file__':str(path)};exec(compile(code,str(path),'exec'),env)
    if lod>0:
        cls=env['Mesh'];orb=cls.orb;cylinder=cls.cylinder;arch=env['arch']
        def sampled_orb(self,p,scale,c,seed,rows=10,cols=16,rough=.12):
            return orb(self,p,scale,c,seed,min(rows,[10,5,3][lod]),min(cols,[16,8,6][lod]),rough)
        def sampled_cylinder(self,p,r,h,c,top=None,n=32):return cylinder(self,p,r,h,c,top,min(n,[80,24,14][lod]))
        def sampled_arch(mesh,p,width,spring,thick,depth,rot=0,c=env['IVORY'],stones=16):return arch(mesh,p,width,spring,thick,depth,rot,c,min(stones,[16,9,5][lod]))
        cls.orb=sampled_orb;cls.cylinder=sampled_cylinder;env['arch']=sampled_arch
    install_reference(env,lod)
    install(env,lod)
    return env

def build_districts(Mesh):
    records=[]
    for variant in range(8):
        levels=[]
        for lod in range(3):
            g=vocabulary(lod);g['random'].seed(1201+variant)
            ivory,trim,copper=g['IVORY'],g['TRIM'],g['COPPER']
            arch,arcade,dome,tower,tree,cypress,ivy,balustrade=[g[n] for n in ('arch','arcade','dome','tower','tree','cypress','ivy','balustrade')]
            a=g['architecture'];t=g['trim'];fol=g['garden'];cliff=g['cliffs']
            center=(42,-1.1,42)
            # Broad shoulders and interlocking buttresses produce a single crag.
            if variant==2:
                g['island']((31,-1.1,41),31,25,53)
                g['island']((55,-1.1,44),28,42,89)
            else:g['island'](center,40,[32,47,28,51,67,22,31,55][variant],31+variant*9)
            ground='84906B' if variant==2 else '91A088' if variant==5 else 'A6A28C' if variant==7 else 'B3AE92'
            a.cylinder((42,-1.2,42),36.5,1.30,g['rgb'](ground),n=[80,48,28][lod])
            # Perimeter architecture varies with the district's history: retaining
            # walls, incomplete aqueducts, planted cliff edges, or palace arcades.
            retaining_arcades(g,lod,variant)
            if variant in (0,4):t.cylinder((42,-.26,42),36.9,.35,trim,n=[96,64,40][lod])
            # Raised sanctuary occupies the central 3x3 reservation. Ground routes
            # pass around it, and a broad south stair leads to the upper garden.
            cx,cz=36,48
            if variant in (0,1,3,4):
                if variant==0:
                    arcade((cx,.25,cz),13.3,5.55,24)
                    balustrade((cx,5.8,cz),13.15,n=[96,60,36][lod],start=-pi*.47,end=pi*1.47)
                elif variant in (1,3):
                    a.box((cx,2.95,cz),(25,5.7,24),ivory)
                    t.box((cx,5.68,cz),(25.6,.24,24.6),trim)
                    for dx in [-10,-6,-2,2,6,10]:
                        g['window']((cx+dx,.7,cz-12.04),2.1,3.6,0)
                else:
                    a.cylinder((cx,.1,cz),9.3,5.70,ivory,n=12)
                if variant==0:
                    arcade((cx-1,5.8,cz+1.3),8.9,6.7,20)
                    arcade((cx-1,12.5,cz+1.3),7.0,7.8,16)
                    dome((cx-1,20.3,cz+1.3),7.5,7.4,27)
                    tower('West wind',(cx-9.4,5.8,cz+4.5),2.3,27)
                    tower('Sunrise',(cx+9,5.8,cz+5.5),2.1,23)
                    arcade((cx+8.0,5.8,cz-4.0),3.2,5.0,10);dome((cx+8,10.8,cz-4),3.5,3.0,40)
                elif variant==1:
                    g['villa']((cx,5.8,cz+1),12,19,11,71)
                    tower('Abbey',(cx-7,5.8,cz-7),3.5,26)
                    for dz in [-7,-3,1,5,9]:
                        a.box((cx+7,9.5,cz+dz),(1.3,7.2,1.0),ivory)
                elif variant==3:
                    for dx,dz,hh in [(-6,3,9),(5,5,12),(-5,-6,5)]:g['villa']((cx+dx,5.8,cz+dz),6.6,7.5,hh,51)
                    tower('Library',(cx+7,5.8,cz-6),3.0,23)
                    g['villa']((cx,5.8,cz-7),13,5,6,41)
                else:
                    tower('Astronomers needle',(cx,5.8,cz),7,43)
                    for h in [15,28]:
                        a.cylinder((cx,h-2.2,cz),3.4,2.2,ivory,top=7.15,n=[48,30,18][lod])
                        arcade((cx,h,cz),6.8,3.6,16)
                        balustrade((cx,h+3.6,cz),6.8,n=[48,30,18][lod])
            else:
                if variant==2:
                    for dx,dz,r,h in [(-5,-4,7,3.4),(5,4,8,3.4),(-6,7,4,7.0)]:
                        a.cylinder((cx+dx,.1,cz+dz),r,h+.15,ivory,n=24)
                        balustrade((cx+dx,h+.25,cz+dz),r,n=[44,28,16][lod])
                        for j in range(6):
                            angle=j*2.399;cypress((cx+dx+cos(angle)*r*.6,h+.25,cz+dz+sin(angle)*r*.6),4+j%3,800+j)
                elif variant==5:
                    # Water and a single slender open pavilion dominate this island.
                    a.cylinder((cx,.02,cz),12.6,.2,trim,n=64)
                    a.cylinder((cx+9,.1,cz+8),3.65,.4,ivory,n=32)
                    arcade((cx+9,.5,cz+8),3.2,5.4,10);dome((cx+9,5.9,cz+8),3.5,2.6,57)
                elif variant==6:
                    a.box((cx,1.1,cz),(27,2.0,19),ivory)
                    for dx,dz,hh in [(-10,3,4),(-3,5,7),(6,-4,5),(12,3,6)]:g['villa']((cx+dx,2.1,cz+dz),5.7,6.2,hh,67)
                else:
                    a.cylinder((cx,.1,cz),12.8,.2,ivory,n=[64,40,24][lod])
                    for j in range(12):
                        angle=j*2*pi/12
                        g['column']((cx+11*cos(angle),.3,cz+11*sin(angle)),6,.48)
                        if j%4!=0:arch(a,(cx+10.6*cos(angle+pi/12),.3,cz+10.6*sin(angle+pi/12)),4.9,6,.6,.8,angle+pi/12+pi/2,ivory)
                    for j in range(9):g['boulder']((cx-7+j*1.6,.10,cz+4*sin(j)),(1.0+.15*sin(j),.8+.25*cos(j),1.0),710+j)
            upper=5.8 if variant in (0,1,3,4) else 2.1 if variant==6 else .3
            # Deep window reveals and inhabited cores behind the open loggias.
            if variant==0:
                drums=[(cx-1,cz+1.3,12.5,5.8,7.5)]
                for dx,dz,y,rad,h in drums:
                    a.cylinder((dx,y,dz),rad,h,ivory,n=[64,40,24][lod])
                    for j in range(14):
                        ang=j*2*pi/14
                        g['window']((dx+(rad+.035)*cos(ang),y+.75,dz+(rad+.035)*sin(ang)),1.1,h-1.6,ang+pi/2)
            if variant==0:a.cylinder((cx,.25,cz),9.6,upper-.55,ivory,n=[64,40,24][lod])
            for j in range(20 if variant==0 else 0):
                angle=j*2*pi/20
                g['window']((cx+9.64*cos(angle),1.0,cz+9.64*sin(angle)),1.0,upper-1.6,angle+pi/2)
            if variant in (0,4):curved_stair(g,(cx,cz),14.2 if variant==0 else 10.2,5.8,lod)
            elif variant in (1,3,6):straight_stair(g,cx-8,cz-(9.5 if variant==6 else 12),upper,lod)
            # Lush planting deliberately gathers around terraces and footings.
            for j in range([18,14,10][lod]):
                angle=j*2.399;rad=(14.6 if variant==5 else 10.0)+(j%3)*.6
                p=(cx+rad*cos(angle),.1 if variant in (4,5) else upper,cz+rad*sin(angle))
                if variant==5 and (p[0]-cx-9)**2+(p[2]-cz-8)**2<4.8**2:continue
                footprints={0:[(8,-4,4,4),(-9.4,4.5,1.8,1.8),(9,5.5,1.7,1.7)],
                    1:[(0,1,6.6,10),(-7,-7,2.3,2.3)],
                    3:[(-6,3,3.9,4.3),(5,5,3.9,4.3),(-5,-6,3.9,4.3),(0,-7,7.1,3.1),(7,-6,2,2)],
                    6:[(-10,3,3.4,3.6),(-3,5,3.4,3.6),(6,-4,3.4,3.6),(12,3,3.4,3.6)]}
                if any(abs(p[0]-cx-dx)<w and abs(p[2]-cz-dz)<d for dx,dz,w,d in footprints.get(variant,[])):continue
                if variant==6 and (abs(p[0]-cx)>13.3 or abs(p[2]-cz)>9.3):p=(p[0],.1,p[2])
                if variant==2:
                    plant_height=.1
                    for dx,dz,r,h in [(-5,-4,7,3.4),(5,4,8,3.4),(-6,7,4,7.0)]:
                        if (p[0]-cx-dx)**2+(p[2]-cz-dz)**2<(r+.5)**2:plant_height=max(plant_height,h+.25)
                    p=(p[0],plant_height,p[2])
                if j%3==0:cypress(p,4.3+(j%4)*.55,300+j+variant)
                else:tree(p,1.4+(j%4)*.15,400+j+variant)
            for j in range([40,26,16][lod]):
                angle=j*2.399+variant*.57;rad=31.5+(j%4)*1.0
                p=(42+rad*cos(angle),.1,42+rad*sin(angle))
                if abs(p[0]-36)<5 and p[2]<22:continue
                if j%4==0:cypress(p,4.4+j%3,900+j)
                else:tree(p,1.3+(j%4)*.3,950+j)
                if j%2==0:ivy((p[0],-.2,p[2]),4.5+j%5,1200+j,spread=.8)
            # A visible reflecting garden on the south terrace, level with every pool.
            water=Mesh(lod);cascades=Mesh(lod)
            if variant==5:
                for j in range(64):
                    aa=j*2*pi/64;bb=(j+1)*2*pi/64
                    water.face([(cx,.38,cz),(cx+12.15*cos(bb),.38,cz+12.15*sin(bb)),(cx+12.15*cos(aa),.38,cz+12.15*sin(aa))],(1,1,1))
            a.cylinder((36,.04,24),6.0,.26,trim,n=48)
            for j in range(48):
                angle=j*2*pi/48;nxt=(j+1)*2*pi/48
                water.face([(36,.38,24),(36+5.6*cos(nxt),.38,24+5.6*sin(nxt)),(36+5.6*cos(angle),.38,24+5.6*sin(angle))],(1,1,1))
            if lod<2:
                for j in range(9):
                    angle=pi+j*pi/8
                    if abs(6.7*cos(angle))<3:continue
                    g['flowers']((36+6.7*cos(angle),.1,24+6.7*sin(angle)),201+j)
            # The garden drains through a modeled open stone channel. Its floor,
            # surface, rounded overflow and falling water meet without a gap.
            width=[3.4,2.1,4.2,2.6,1.8,4.5,2.4,2.8][variant]
            for x in [36-width*.5-.22,36+width*.5+.22]:
                a.box((x,.30,11.5),(.44,.65,15.0),ivory)
                t.box((x,.65,11.5),(.56,.10,15.0),trim)
            a.box((36,-.10,11.5),(width,.32,15.0),g['STONE'])
            water.face([(36-width*.5,.38,19),(36+width*.5,.38,19),
                        (36+width*.5,.38,4),(36-width*.5,.38,4)],(1,1,1),
                       uv=[(-1,0),(1,0),(1,1),(-1,1)])
            # Time along a ballistic path gives a horizontal exit that bends
            # naturally under gravity. No sinusoidal cloth deformation.
            rows=[52,32,18][lod];cols=[14,10,6][lod]
            length=[34,39,27,42,48,24,30,37][variant]
            def falling(u,v):
                contraction=.72+.28*exp(-v*7)
                x=36+(u-.5)*width*contraction+.11*sin(u*17+variant)*v*v
                y=.38-length*v*v
                z=4-7.5*v-.08*sin(u*24)*v
                return (x,y,z)
            for row in range(rows):
                for col in range(cols):
                    u0,u1=col/cols,(col+1)/cols;v0,v1=row/rows,(row+1)/rows
                    uv=[(u0,v0),(u1,v0),(u1,v1),(u0,v1)]
                    cascades.face([falling(*q) for q in uv],(1,1,1),uv=uv)
            # Baked seeds for GPU mist: no particle systems or per-frame CPU
            # allocations. UV.x >= 2 marks a camera-facing spray quad.
            spray=random.Random(172+variant)
            for j in range([72,36,12][lod]):
                v=.34+spray.random()*.60;u=spray.uniform(-.18,1.18)
                p=falling(u,v);start=len(cascades.v)
                c=(spray.random(),spray.uniform(.10,.42),spray.random(),0)
                cascades.v.extend([p]*4);cascades.n.extend([(0,0,-1)]*4)
                cascades.c.extend([c]*4);cascades.uv.extend([(2,0),(3,0),(3,1),(2,1)])
                cascades.t.extend([start,start+1,start+2,start,start+2,start+3])
            # Flatten the source meshes into the shared atlas, retaining smooth
            # normals on dome grids and per-vertex mineral/copper pigmentation.
            output=Mesh(lod)
            kinds={'Copper':.2,'Brass':.6,'Foliage':.4,'Rock':.1,'Wood':.3,'Dark':.05,'Stone':0,'Terracotta':.5,'Cloth':.8,'Water':.7}
            for src in g['groups'].values():
                if not src.f:continue
                if src.kind=='Water':raise RuntimeError('Cascades must use the transparent mesh stream')
                if src.kind=='Rock' and src.name!='06 - Garden boulders':
                    # Cut a continuous chute behind the authored spillway.
                    # The wider cliff shoulders must not occlude falling water.
                    carved=[]
                    for p in src.v:
                        weight=max(0,min(1,(width*.5+.9-abs(p[0]-36))/.9))
                        carved.append((p[0],min(p[1],-.75),p[2]+max(0,4.3-p[2])*weight))
                    src.v=carved
                normals=[Vector((0,0,0)) for _ in src.v]
                for face in src.f:
                    if len(face)<3:continue
                    n=(Vector(src.v[face[1]])-Vector(src.v[face[0]])).cross(Vector(src.v[face[2]])-Vector(src.v[face[0]]))
                    for idx in face:normals[idx]+=n
                start=len(output.v)
                output.v.extend(src.v);output.n.extend(tuple(n.normalized()) for n in normals)
                output.c.extend((*c[:3],kinds.get(src.kind,0)) for c in src.c);output.uv.extend(src.uv)
                for face in src.f:
                    for j in range(1,len(face)-1):output.t.extend((start+face[0],start+face[j],start+face[j+1]))
            for plant in g['_hero_botany']:plant.append(output)
            # LOD uses fewer radial samples and fewer leaf clusters in vocabulary().
            # Structural columns are retained so distant roofs never float in space.
            # Store an island-local current vector in UVs for pools/channels.
            # It is rotated/scaled with geometry at runtime, never aimed at a
            # fixed world coordinate belonging to the original handbuilt scene.
            water.uv=[(0,-1) for _ in water.v]
            levels.append((output,water,cascades))
            print('DISTRICT',variant,NAMES[variant],lod,len(output.t)//3,flush=True)
        for label,high in zip(['Island','Reflecting water','Cascades and spray'],levels[0]):
            mesh=bpy.data.meshes.new(label+' '+NAMES[variant]);mesh.from_pydata([(p[0],-p[2],p[1]) for p in high.v],[],[tuple(high.t[i:i+3]) for i in range(0,len(high.t),3)])
            colors=mesh.color_attributes.new(name='Pigment',type='FLOAT_COLOR',domain='POINT');colors.data.foreach_set('color',[c for row in high.c for c in (*row[:3],1)])
            uv=mesh.uv_layers.new(name='Flow')
            for loop in mesh.loops:uv.data[loop.index].uv=high.uv[loop.vertex_index]
            obj=bpy.data.objects.new(label+' '+NAMES[variant],mesh);bpy.context.collection.objects.link(obj);obj.location=(variant*110,-260,0)
        records.append(levels)
    return records
