"""128 modeled architectural parcels, authored in Blender; Unity Y-up coordinates.
Run in a separate Blender --background --factory-startup process.
The binary is a small shared mesh vocabulary, never pre-generated world chunks.
"""
import bpy, math, json, struct, hashlib, random, gzip
from pathlib import Path
from math import sin, cos, pi
from mathutils import Vector

SOURCE = Path(__file__).resolve().parent
ROOT = SOURCE.parents[2]
OUT = ROOT / 'Boids_Proj/Assets/Boids/Resources/SkyCityInfinite'
OUT.mkdir(parents=True, exist_ok=True)
for ob in list(bpy.data.objects): bpy.data.objects.remove(ob, do_unlink=True)

def rgb(h):
    a = [int(h[i:i+2],16)/255 for i in (0,2,4)]
    return tuple(((x+.055)/1.055)**2.4 if x>.04045 else x/12.92 for x in a)
IVORY=rgb('E8DBBD'); TRIM=rgb('F1E6CB'); COPPER=rgb('568F8C'); GOLD=rgb('C6A063')
DARK=rgb('354C62'); ROCK=rgb('7C8986'); LEAF=rgb('527767'); SILK=rgb('DFA17E')
FAMILIES=['Arcaded promenade','Turning loggia','Market colonnade','Fountain piazza',
          'Hanging belvedere','Domed sanctuary','Bell campanile','Garden cloister',
          'Terraced orchard','Palace library','Sky aqueduct','Curved skybridge',
          'Ceremonial gate','Celestial observatory','Glass conservatory','Crown palace']
VARIANTS=['Dawn','Iris','Cypress','Pearl','Saffron','Linden','Azure','Solstice']
MASKS=[5,3,11,15,1,5,1,3,5,11,5,3,5,1,11,15] # N,E,S,W bits, prior to rotation

class Mesh:
    def __init__(self,lod): self.v=[]; self.n=[]; self.c=[]; self.uv=[]; self.t=[]; self.lod=lod
    def face(self,pts,c,kind=0,uv=None):
        # Cone tips and dome poles collapse two consecutive corners. Keep the
        # remaining triangle instead of dropping the entire roof face.
        kept=[]
        for j,p in enumerate(pts):
            if not kept or (Vector(p)-Vector(pts[kept[-1]])).length_squared>1e-12:kept.append(j)
        if len(kept)>1 and (Vector(pts[kept[0]])-Vector(pts[kept[-1]])).length_squared<1e-12:kept.pop()
        if len(kept)<3:return
        if uv:uv=[uv[j] for j in kept]
        pts=[pts[j] for j in kept]
        n=(Vector(pts[1])-Vector(pts[0])).cross(Vector(pts[2])-Vector(pts[0]))
        if n.length<1e-8:return
        n.normalize(); start=len(self.v)
        for j,p in enumerate(pts):
            self.v.append(tuple(p));self.n.append(tuple(n));self.c.append((*c,kind))
            self.uv.append(uv[j] if uv else (0,0))
        for j in range(1,len(pts)-1):self.t.extend([start,start+j,start+j+1])
    def box(self,p,s,c=IVORY,kind=0,rot=0):
        sx,sy,sz=[x*.5 for x in s]; q=[]
        for x,y,z in [(-sx,-sy,-sz),(sx,-sy,-sz),(sx,sy,-sz),(-sx,sy,-sz),(-sx,-sy,sz),(sx,-sy,sz),(sx,sy,sz),(-sx,sy,sz)]:
            q.append((p[0]+x*cos(rot)-z*sin(rot),p[1]+y,p[2]+x*sin(rot)+z*cos(rot)))
        for f in [(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(3,7,6,2),(0,1,5,4)]: self.face([q[i] for i in f],c,kind)
    def ring(self,p,r,h,c=IVORY,top=None,n=None,kind=0):
        if top is None:top=r
        if n is None:n=[12,8,6][self.lod]
        low=[]; high=[]
        for j in range(n):
            a=j*2*pi/n;low.append((p[0]+r*cos(a),p[1],p[2]+r*sin(a)))
            high.append((p[0]+top*cos(a),p[1]+h,p[2]+top*sin(a)))
        for j in range(n):self.face([low[j],high[j],high[(j+1)%n],low[(j+1)%n]],c,kind)
        self.face(high[::-1],c,kind);self.face(low,c,kind)
    def dome(self,p,r,h,c=COPPER,style=0):
        if style==3:
            self.ring(p,r,h*1.7,c,0,n=[16,10,6][self.lod],kind=.2)
            self.ring((p[0],p[1]+h*1.7,p[2]),.10,.6,GOLD,0,6,.6)
            return
        rows=[7,4,2][self.lod]; cols=[20,12,8][self.lod]
        for j in range(rows):
            def pt(y,x):
                a=x*2*pi/cols;theta=y/rows*pi/2
                rad=r*cos(theta)*(1+.07*sin(theta*2*pi)*(style%3))
                return(p[0]+rad*cos(a),p[1]+h*sin(theta),p[2]+rad*sin(a))
            for i in range(cols):
                points=[pt(j,i),pt(j+1,i),pt(j+1,i+1),pt(j,i+1)]
                before=len(self.v);self.face(points,c,.2)
                for k in range(len(self.v)-before):
                    q=self.v[before+k];self.n[before+k]=tuple(Vector(((q[0]-p[0])/(r*r),(q[1]-p[1])/(h*h),(q[2]-p[2])/(r*r))).normalized())
        self.ring((p[0],p[1]+h,p[2]),.12,.65,GOLD,0,6,.6)
    def arch(self,p,width,height,depth=.38,rot=0):
        r=width*.5; spring=height-r; thick=.23
        def pt(x,y,z):return(p[0]+x*cos(rot)-z*sin(rot),p[1]+y,p[2]+x*sin(rot)+z*cos(rot))
        for x in [-r-thick*.5,r+thick*.5]: self.box(pt(x,spring*.5,0),(thick,spring,depth),IVORY,rot=rot)
        if self.lod<2:
            for x in [-r-thick*.5,r+thick*.5]:
                self.box(pt(x,.12,0),(thick+.12,.18,depth+.14),TRIM,rot=rot)
                self.box(pt(x,spring-.08,0),(thick+.14,.16,depth+.16),TRIM,rot=rot)
        count=[10,6,3][self.lod]
        for j in range(count):
            a=j*pi/count;b=(j+1)*pi/count
            ends=[[pt(rad*cos(t),spring+rad*sin(t),z) for rad,t in [(r,a),(r,b),(r+thick,b),(r+thick,a)]] for z in [-depth*.5,depth*.5]]
            self.face(ends[0],TRIM);self.face(ends[1][::-1],TRIM)
            for k in range(4): self.face([ends[0][k],ends[1][k],ends[1][(k+1)%4],ends[0][(k+1)%4]],TRIM)
    def tree(self,x,z,h=3,base=.2):
        self.ring((x,base,z),.11,h*.65,rgb('655D4C'),.055,n=5)
        # Layered rounded crowns, with irregular edges and individual leaf clusters.
        for k in range(4 if self.lod<2 else 2):
            a=k*2.399; r=h*(.16 if k else .22)
            cx=x+cos(a)*h*.14;cz=z+sin(a)*h*.14;cy=base+h*(.58+k*.09)
            rows=[5,4,3][self.lod];cols=[9,7,5][self.lod]
            for j in range(rows):
                def p(t,u):
                    th=t/rows*pi;ph=u/cols*2*pi;rr=r*(1+.14*sin(ph*5+k))
                    return(cx+sin(th)*cos(ph)*rr,cy+cos(th)*r*.9,cz+sin(th)*sin(ph)*rr)
                for i in range(cols):self.face([p(j,i),p(j+1,i),p(j+1,i+1),p(j,i+1)],tuple(c*(.85+k*.09) for c in LEAF),.4)
    def flag(self,x,z,h,color=SILK):
        self.ring((x,.25,z),.055,h,GOLD,n=5,kind=.6)
        cols=[9,4,1][self.lod]; rows=2 if self.lod==0 else 1
        for j in range(rows):
            for i in range(cols):
                pts=[];uv=[]
                for u,v in [(i/cols,j/rows),(i/cols,(j+1)/rows),((i+1)/cols,(j+1)/rows),((i+1)/cols,j/rows)]:
                    pts.append((x+u*1.5,h-.8*v-.14*u,z+.055*sin(u*8)));uv.append((u,v))
                self.face(pts,color,.8,uv)

def parcel(f,v,lod):
    m=Mesh(lod);water=Mesh(lod); rng=random.Random(f*101+v*811)
    # Buildings share the district's continuous rock and garden terraces.
    # There is deliberately no miniature floating cone under each building.
    bridge=f in (10,11)
    if not bridge:m.box((0,.08,0),(8.9,.34,8.9),IVORY)
    if f==10:m.box((0,-.10,0),(3.6,.70,12),IVORY)
    if f==11:
        m.box((0,-.10,2),(4.0,.70,8),IVORY)
        m.box((2,-.10,0),(8,.70,4.0),IVORY)
    # Every socket is a 2.4 m walkable route at exactly y=.25 on a 12 m grid.
    for d in range(4):
        if not(MASKS[f]&(1<<d)):continue
        x,z=sin(d*pi/2)*3,cos(d*pi/2)*3
        m.box((x,.1,z),(2.4,.3,6),TRIM,rot=d*pi/2)
        for side in ([-1,1] if bridge else []):
            xx=x+cos(d*pi/2)*side*1.65;zz=z-sin(d*pi/2)*side*1.65
            m.box((xx,.90,zz),(.14,1.30,6),IVORY,rot=d*pi/2)
    height=2.9+v*.22; variantWidth=1.65+(v%3)*.26
    def pavilion(x,z,h,r,style=0):
        if v%4==1:m.box((x,h*.5+.2,z),(r*1.7,h,r*1.7),IVORY)
        elif v%4==2:
            m.ring((x,.2,z),r*1.05,h*.55,IVORY,r*.88,n=8)
            m.ring((x,h*.55+.2,z),r*.92,h*.45,IVORY,r*.82,n=8)
            m.ring((x,h*.55+.2,z),r*1.03,.18,TRIM,n=8)
        else:m.ring((x,.2,z),r,h,IVORY,n=8+v%3*2)
        m.ring((x,h+.2,z),r+.17,.22,TRIM,n=8)
        m.dome((x,h+.42,z),r+.08,r*(.58+style*.11),style=style)
        if lod<2:m.ring((x,h+.39,z),r+.11,.065,GOLD,n=[20,12][lod],kind=.6)
        if lod<2:
            for j in range([6,3][lod]):
                a=j*2*pi/[6,3][lod]
                m.box((x+cos(a)*(r+.025),h*.65,z+sin(a)*(r+.025)),(.045,h*.4,r*.45),DARK,rot=-a)
    def arcade(x,z,alongX=True,count=3,y=.24,h=2.7):
        spacing=1.7;span=1.35
        if count==3 and v%3==1:count=4;spacing=1.28;span=.94
        elif count==3 and v%3==2:count=2;spacing=2.45;span=2.05
        for j in range(count):
            off=(j-(count-1)*.5)*spacing
            m.arch((x+off if alongX else x,y,z if alongX else z+off),span,h,rot=0 if alongX else pi/2)
        m.box((x,y+h+.14,z),((count*spacing+.2) if alongX else .55,.25,.55 if alongX else count*spacing+.2),TRIM)
    def pool(x,z,w=2.1,d=2.1):
        # Deck .25; all reflecting pools share y=.30 for a single reflection camera.
        m.box((x,.28,z),(w+.3,.16,d+.3),DARK)
        water.face([(x-w*.5,.38,z-d*.5),(x-w*.5,.38,z+d*.5),(x+w*.5,.38,z+d*.5),(x+w*.5,.38,z-d*.5)],(1,1,1),0,[(0,0),(0,1),(1,1),(1,0)])
    def round_pavilion(x,z,r,h):
        # Four complete sides carry the roof; a single arcade row cannot support a dome.
        for side in [-1,1]:
            m.arch((x+side*r*.74,.25,z),r*1.05,h,rot=pi/2)
            m.arch((x,.25,z+side*r*.74),r*1.05,h)
        crown=.25+h+.23
        m.box((x,crown+.04,z),(r*1.85,.20,r*1.85),TRIM)
        m.dome((x,crown+.14,z),r, r*.55)
    if f==0:
        arcade(-2.9,0,False,3,h=height);arcade(2.9,0,False,3,h=height-.4)
        m.box((-3.15,height+.55,0),(1.05,.35,5.9),COPPER,.2)
    elif f==1:
        arcade(-2.9,.2,False,3,h=height);arcade(0,-2.9,True,3,h=height)
        pavilion(-2.6,-2.6,height+1,1.2,v%3)
    elif f==2:
        for x in [-2.6,0,2.6]:
            arcade(x,-2.9,True,1,h=2.6)
            m.box((x,3.0,-2.9),(2.2,.25,1.8),SILK if v%2 else COPPER,.2)
        pavilion(-3,2.6,height,1.0,v%3)
    elif f==3:
        pool(-2.6,-2.6,2.3,2.3);m.ring((-2.6,.4,-2.6),.22,1.8,TRIM,n=8)
        m.dome((-2.6,2.2,-2.6),.7,.18,TRIM)
        for x,z in [(2.9,2.9),(-2.9,2.9),(2.9,-2.9)]:m.tree(x,z,2.7+v*.3)
    elif f==4:
        round_pavilion(0,-2.7,2.0,height)
        for x in [-3.4,3.4]:m.tree(x,.1,3+v*.18)
    elif f==5:
        pavilion(-2.65,-1.1,height,variantWidth,v%4);pavilion(2.6,2.0,height*.62,1.25,v%3)
        pool(2.5,-2.5,2.0,2.0)
    elif f==6:
        h=9+v*1.2;w=2.0+(v%3)*.4
        if v%3==0:m.box((0,h*.4,-1.8),(w,h*.8,w),IVORY)
        elif v%3==1:
            for x in [-w*.32,w*.32]:m.box((x,h*.4,-1.8),(w*.43,h*.8,w*.8),IVORY)
        else:m.ring((0,.2,-1.8),w*.68,h*.8,IVORY,w*.5,n=8)
        for y in [h*.22,h*.47,h*.78]:m.box((0,y,-1.8),(w+.28,.22,w+.28),TRIM)
        for z in [-1.8-w*.43,-1.8+w*.43]:m.arch((0,h*.8,z),w*.65,2.6)
        roof=h*.8+2.6+.23
        m.box((0,roof+.03,-1.8),(w+.35,.22,w+.35),TRIM)
        m.dome((0,roof+.14,-1.8),w*.7,2+v*.1,style=v%4)
        if v%2:arcade(2.8,-.5,False,2)
    elif f==7:
        arcade(-3,0,False,3,h=height);arcade(0,-3,True,3,h=height)
        pool(-1.4,-1.4,2,2);m.tree(2.9,2.6,4.2)
        m.box((-3,height+.5,0),(1,.25,5.6),COPPER,.2)
    elif f==8:
        for x,z,h in [(-3,-2,2.0),(3,2,1.5),(-3,2.8,1.0)]:
            m.box((x,h*.5,z),(2.4,h,2.2),IVORY);m.tree(x,z,3+v*.25,base=h)
        pool(2.8,-2.5,1.8,2.7)
    elif f==9:
        h=height+2;m.box((0,h*.5,-2.9),(6,h,2.2),IVORY)
        arcade(0,-4.08,True,3,h=3)
        for x in [-2.1,2.1]:pavilion(x,-2.9,h+.15,1.1,v%4)
        if lod<2:
            for x in [-2,0,2]:m.box((x,h*.7,-4.03),(.7,1.3,.03),DARK)
    elif f==10:
        # A 12 m covered gallery with feet ON its deck. Boundary bridges repeat
        # these bays at near-native proportions; their bearing arch is separate.
        bays=[3,4,3,4,2,3,2,4][v];spacing=12/bays
        for x in [-1.52,1.52]:
            for j in range(bays):m.arch((x,.25,-6+(j+.5)*spacing),spacing-.44,3.35,depth=.42,rot=pi/2)
            m.box((x,3.77,0),(.60,.24,12),TRIM)
        m.box((0,3.95,0),(3.72,.30,12),COPPER,.2)
        for side in [-1,1]:m.box((side*1.83,4.14,0),(.12,.12,12),TRIM)
        for j in range(v+1):
            z=-5.2+j*10.4/max(1,v)
            m.ring((1.83,4.20,z),.10,.24+.025*v,GOLD,n=6,kind=.6)
    elif f==11:
        h=3.2+v*.06
        for z in [0,3.8]:m.arch((-1.52,.25,z),3.25,h,rot=pi/2)
        for x in [0,3.8]:m.arch((x,.25,-1.52),3.25,h)
        m.arch((1.52,.25,4.2),2.9,h,rot=pi/2);m.arch((4.2,.25,1.52),2.9,h)
        top=.25+h+.23
        m.box((0,top+.09,2),(4.05,.25,8),COPPER,.2)
        m.box((2,top+.09,0),(8,.25,4.05),COPPER,.2)
    elif f==12:
        for x in [-2.6,2.6]:pavilion(x,0,height+1.4,1.1,v%4)
        m.arch((0,.25,0),2.2,height+1)
        m.box((0,height+1.8,0),(5.7,.4,1),TRIM)
    elif f==13:
        h=height+4;m.ring((0,.2,-1.8),2.2,h,IVORY,n=10)
        m.ring((0,h+.2,-1.8),2.6,.35,TRIM,n=10)
        m.dome((0,h+.55,-1.8),2.5,1.5+v*.16,style=v%3)
        if lod<2:
            for j in range(8):
                a=j*pi/4;m.box((cos(a)*2.22,h*.6,-1.8+sin(a)*2.22),(.05,2,.4),DARK,rot=-a)
    elif f==14:
        round_pavilion(0,-2.5,2.2,height)
        for x in [-3,3]:m.tree(x,2.7,3.5+v*.24)
        pool(-2.6,-2.6,1.7,1.7)
    else:
        for x,z,h,r in [(-2.7,-2.7,height+3.5,1.6),(2.8,-2.7,height+1,1.1),(-2.8,2.8,height+1,1.1),(2.8,2.8,height*.65,1.25)]:pavilion(x,z,h,r,v%4)
        m.arch((0,.25,-2.7),2.3,3.6);m.arch((-2.7,.25,0),2.3,3.6,rot=pi/2)
    # Architectural variants change silhouette, circulation decoration and planting.
    if not bridge:
        # Eight structural profiles include a stair, a raised gallery, a small chapel,
        # and a pergola: these are architectural changes, not rotation/color counts.
        if v==1:
            for step in range([9,6,3][lod]):
                t=(step+1)/[9,6,3][lod];top=.09+t*1.05
                m.box((3.9,(.09+top)*.5,-3.9+t*2.6),(.95,top-.09,2.6/[9,6,3][lod]+.04),TRIM)
            m.box((3.9,.62,-.75),(1.35,1.06,1.1),IVORY)
        if v==2:
            m.box((-3.8,2.60,-1.5),(1.2,.24,2.3),TRIM)
            for z in [-2.3,-.7]:m.box((-3.8,1.29,z),(.60,2.38,.45),IVORY)
            m.arch((-3.8,2.72,-1.5),1.2,2.4,rot=pi/2)
        if v==3:pavilion(3.6,-3.6,2.8,.72,3)
        if v==5:
            for z in [-3.9,-2.3]:m.arch((3.7,.25,z),1.0,2.7)
            m.box((3.7,3.0,-3.1),(1.45,.18,2.2),LEAF,.4)
        if v in (2,5,7):m.tree(3.9,-2.0,3.2+v*.18)
        if v in (1,4,6):
            m.ring((-3.8,.22,1.8),.55,1.7,TRIM,n=8);m.dome((-3.8,1.92,1.8),.7,.8,COPPER)
        m.flag(3.6,1.8,4.2+v*.18)
        if lod==0:
            for j in range(5):
                a=j*1.256+v*.41
                m.ring((cos(a)*4.5,-1.0-rng.random()*1.5,sin(a)*4.5),.35,2.1,LEAF,.5,n=5,kind=.4)
    water.uv=[(0,-1) for _ in water.v]
    return m,water,Mesh(lod)

# The island and landmark vocabulary is separately authored and is NOT counted
# toward the 128 reusable buildings. Legacy sculpting helpers are reused as source.
import sys
sys.path.insert(0,str(SOURCE))
from build_districts import build_districts
districts=build_districts(Mesh)
catalog=[]; records=[]; hashes=set()
for f in range(16):
    for v in range(8):
        id=f*8+v; label=FAMILIES[f]+' / '+VARIANTS[v]; levels=[]
        for lod in range(3): levels.append(parcel(f,v,lod))
        records.append(levels)
        high=levels[0][0]
        signature=hashlib.sha256(b''.join(struct.pack('<3f',*p) for p in high.v)).hexdigest();hashes.add(signature)
        # A native editable Blender catalog arranged in a readable 16 x 8 gallery.
        mesh=bpy.data.meshes.new(label)
        mesh.from_pydata([(p[0],-p[2],p[1]) for p in high.v],[],[tuple(high.t[j:j+3]) for j in range(0,len(high.t),3)])
        colors=mesh.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='POINT')
        colors.data.foreach_set('color',[a for c in high.c for a in (*c[:3],1)])
        ob=bpy.data.objects.new('%03d %s'%(id,label),mesh);bpy.context.collection.objects.link(ob);ob.location=(f*15,-v*16,0)
        catalog.append({'id':id,'name':label,'family':f,'variant':v,'sockets':MASKS[f],
                        'lodTriangles':[len(p[0].t)//3 for p in levels], 'meshHash':signature,
                        'water':bool(levels[0][1].t)})
        print('MODULE',id,label, catalog[-1]['lodTriangles'],flush=True)

with gzip.open(OUT/'Modules.bytes','wb',compresslevel=6) as stream:
    stream.write(struct.pack('<iiii',0x534B594D,3,128,len(districts)))
    for levels in records+districts:
        for mesh,water,cascades in levels:
            for part in [mesh,water,cascades]:
                stream.write(struct.pack('<ii',len(part.v),len(part.t)))
                for p,n,c,uv in zip(part.v,part.n,part.c,part.uv):stream.write(struct.pack('<12f',*p,*n,*c,*uv))
                if part.t:stream.write(struct.pack('<%di'%len(part.t),*part.t))
report={'moduleCount':128,'districtCompositions':len(districts),'distinctGeometryHashes':len(hashes),'families':FAMILIES,'rotationsCountedAsModules':False,
        'tileSize':12,'levelsOfDetail':3,'binaryBytes':(OUT/'Modules.bytes').stat().st_size,
        'totalTrianglesByLOD':[sum(row['lodTriangles'][l] for row in catalog) for l in range(3)],'modules':catalog}
(SOURCE/'module-catalog.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':area.spaces.active.shading.color_type='VERTEX'
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'SkyCity_Modules.blend'),compress=True)
print('MODULE_LIBRARY_COMPLETE',json.dumps({k:v for k,v in report.items() if k!='modules'}),flush=True)
