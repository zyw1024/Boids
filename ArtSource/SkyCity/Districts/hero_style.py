"""Shared streamed-world forms derived from the Hanging Gardens reference.

Branches and individual folded leaves remain at every LOD; distance reduces
spray count, never replaces a plant by a closed polygonal crown.
"""
from math import sin,cos,pi,sqrt
from mathutils import Vector
import random,ast
from pathlib import Path

def rgb(h):
    return tuple(((int(h[i:i+2],16)/255+.055)/1.055)**2.4 if int(h[i:i+2],16)/255>.04045 else int(h[i:i+2],16)/255/12.92 for i in (0,2,4))
PALETTE={k:rgb(v) for k,v in dict(IVORY='DCD0B7',TRIM='F0E2C6',COPPER='649B91',GOLD='B99B60',STONE='A0A39C',DARK='555F5B',WOOD='665955').items()}
def mix(a,b,t):return tuple(x*(1-t)+y*t for x,y in zip(a,b))

class Botany:
    def __init__(self):self.v=[];self.n=[];self.c=[];self.uv=[];self.t=[]
    def vertex(self,p,n,c,kind,uv):
        self.v.append(tuple(p));self.n.append(tuple(n));self.c.append((*[round(max(0,min(1,x))*255)/255 for x in c],kind));self.uv.append(uv);return len(self.v)-1
    def leaf(self,p,length,width,normal,angle,color):
        n=Vector(normal).normalized();u=n.cross(Vector((0,1,0)))
        if u.length<.05:u=n.cross(Vector((1,0,0)))
        u.normalize();v=n.cross(u);u,v=u*cos(angle)+v*sin(angle),-u*sin(angle)+v*cos(angle)
        p=Vector(p);start=self.vertex(p+n*width*.12,n,tuple(x*1.03 for x in color),.4,(.5,.5))
        outline=[(-1,0),(-.48,-.75),(.1,-1),(.67,-.57),(1,0),(.67,.57),(.1,1),(-.48,.75)]
        for x,y in outline:self.vertex(p+u*x*length+v*y*width,(n+v*y*.15).normalized(),tuple(z*(.96+.06*x) for z in color),.4,((x+1)*.5,(y+1)*.5))
        for j in range(8):self.t.extend((start,start+1+j,start+1+(j+1)%8))
    def tube(self,path,radius,lod):
        sides=[8,6,5][lod];start=len(self.v)
        for j,p in enumerate(path):
            axis=(Vector(path[min(j+1,len(path)-1)])-Vector(path[max(0,j-1)])).normalized()
            u=axis.cross(Vector((0,0,1)))
            if u.length<.01:u=axis.cross(Vector((0,1,0)))
            u.normalize();v=axis.cross(u);r=radius*(1-.78*j/(len(path)-1))
            for k in range(sides):
                n=u*cos(k*2*pi/sides)+v*sin(k*2*pi/sides)
                self.vertex(Vector(p)+n*r,n,PALETTE['WOOD'],.3,(j/(len(path)-1),k/sides))
        for j in range(len(path)-1):
            for k in range(sides):
                a=start+j*sides+k;b=start+j*sides+(k+1)%sides
                self.t.extend((a,b,b+sides,a,b+sides,a+sides))
    def tree(self,p,h,seed,lod,cypress=False):
        rng=random.Random(seed);p=Vector(p)
        self.tube([p,p+Vector((.04,h*.35,0)),p+Vector((-.04,h*.66,.04)),p+Vector((.04,h*.93,0))],h*.027,lod)
        count=([16,12,8] if cypress else [12,9,6])[lod]
        sprays=2;leaves=[10,7,4][lod];size=[1.15,1.30,1.65][lod]
        for j in range(count):
            t=.23+.70*j/count;a=j*2.399+seed
            reach=h*(.15*(sin(pi*t)**.75)*(1-t*.45) if cypress else rng.uniform(.17,.26)*sin(pi*t)**.7)
            root=p+Vector((0,h*t,0));tip=root+Vector((cos(a)*reach,h*.11,sin(a)*reach))
            self.tube([root,root.lerp(tip,.6)+Vector((0,.045*h,0)),tip],h*.009,lod)
            for s in range(sprays):
                angle=a+s*1.8;end=tip+Vector((cos(angle)*h*.048,h*.025,sin(angle)*h*.048))
                for k in range(leaves):
                    phi=rng.random()*2*pi;z=rng.uniform(-1,1);rad=sqrt(1-z*z);normal=Vector((cos(phi)*rad,z,sin(phi)*rad))
                    q=end+normal*h*(.052 if cypress else .080)*rng.uniform(.35,1.1)
                    color=mix(rgb('2B4D3C') if cypress else rgb('3D613E'),rgb('829557') if cypress else rgb('9CAC5E'),rng.random()*.90)
                    self.leaf(q,h*rng.uniform(.021,.034)*size,h*rng.uniform(.012,.020)*size,normal,rng.random()*pi,color)
    def ivy(self,p,length,seed,lod,spread=.40):
        rng=random.Random(seed);p=Vector(p);steps=[10,7,4][lod]
        for strand in range([2,2,1][lod]):
            root=p+Vector((rng.uniform(-spread,spread),0,0));end=root+Vector((rng.uniform(-.14,.14),-length*rng.uniform(.7,1),-.10))
            path=[root.lerp(end,j/(steps-1))+Vector((sin(j/(steps-1)*7+seed)*.055,0,-sin(j/(steps-1)*pi)*.12)) for j in range(steps)]
            self.tube(path,.018,lod)
            for j,q in enumerate(path):
                r=rng.uniform(.10,.16)*(1-j/(steps*1.7))*[1,1.12,1.3][lod]
                for side in (-1,1):self.leaf(q+Vector((side*r*.7,.025,-.035)),r*1.2,r*.72,(rng.uniform(-.3,.3),rng.uniform(-.3,.3),-1),side*.75,mix(rgb('30574A'),rgb('91A968'),rng.random()))
    def append(self,target):
        start=len(target.v)
        for field in ('v','n','c','uv'):getattr(target,field).extend(getattr(self,field))
        target.t.extend(i+start for i in self.t)

def install_reference(g,lod):
    """Keep district composition, but use the hero's botanical and facade idiom."""
    g.update(PALETTE);g['_hero_botany']=[]
    g['ROCK_DIR']=Path(__file__).resolve().parents[1]/'Rocks';g['_rock_lod']=lod+1
    def plant(p,h,seed,cypress=False):
        mesh=Botany();mesh.tree(p,h,seed,lod,cypress);g['_hero_botany'].append(mesh)
    def ivy(p,length,seed,spread=.4):
        mesh=Botany();mesh.ivy(p,length,seed,lod,spread);g['_hero_botany'].append(mesh)
    g['cypress']=lambda p,h,seed:plant(p,h,seed,True)
    g['tree']=lambda p,r,seed:plant(p,r*1.6,seed)
    g['ivy']=ivy
    # Reuse the actual main-island authoring functions for masonry, window
    # reveals, cornices and clay tile roofs, without executing its scene build.
    hero=Path(__file__).resolve().parents[3]/'Sky_City_Project/Tools/Island/build_island.py'
    names={'cornice_rect','pilaster','arch_wall','facade','palazzo','tile_roof','rock_island'}
    nodes=[n for n in ast.parse(hero.read_text(encoding='utf8')).body if isinstance(n,ast.FunctionDef) and n.name in names]
    for node in nodes:
        if node.name=='rock_island':
            for item in ast.walk(node):
                if isinstance(item,ast.Assign):
                    names=[t.id for t in item.targets if isinstance(t,ast.Name)]
                    if names==['rows']:item.value=ast.copy_location(ast.Constant([24,16,10][lod]),item.value)
                    elif names==['cols']:item.value=ast.copy_location(ast.Constant([88,56,36][lod]),item.value)
                if isinstance(item,ast.Call) and isinstance(item.func,ast.Name) and item.func.id=='range' and len(item.args)==1 and isinstance(item.args[0],ast.Constant) and item.args[0].value==23:
                    item.args[0]=ast.copy_location(ast.Constant([17,12,8][lod]),item.args[0])
        if node.name=='tile_roof':
            for item in ast.walk(node):
                if isinstance(item,ast.BinOp) and isinstance(item.op,ast.Div) and isinstance(item.right,ast.Constant):
                    if item.right.value==.18:item.right.value=[.24,.45,.85][lod]
                    elif item.right.value==.34:item.right.value=[.5,.8,1.4][lod]
    g['terracotta']=g['Mesh']('14 - Hand laid terracotta roofs','Terracotta')
    exec(compile(ast.Module(body=nodes,type_ignores=[]),str(hero),'exec'),g)
    def villa(p,w,d,h,seed):
        floors=max(1,round(h/3.0));bays=max(2,round(w/2.2))
        g['palazzo'](p,w,d,h,bays,floors)
        g['tile_roof'](g['add'](p,(0,h+.41,0)),w+.5,d+.5,min(h*.28,d*.32))
        g['arcade'](g['add'](p,(0,-.06,-d*.60)),w*.36,h*.68,8)
    g['villa']=villa
    # Wider shoulders and interlocking strata replace the old long, regular cone.
    g['island']=lambda p,r,depth,seed:g['rock_island'](p,r,r*.96,depth*.78,seed)
    boulders=g['Mesh']('06 - Garden boulders','Rock')
    def boulder(p,scale,seed):
        import numpy as np
        mesh=boulders;sx,sy,sz=scale;a=seed*2.399
        with np.load(g['ROCK_DIR']/('Boulder_LOD%d.npz'%(lod+1))) as data:
            start=len(mesh.v)
            for vertex,uv in zip(data['p'],data['uv']):
                x,y,z=vertex;point=(p[0]+cos(a)*x*sx-sin(a)*z*sz,p[1]+(y-.12)*sy,p[2]+sin(a)*x*sx+cos(a)*z*sz)
                mesh.vert(point,(1,1,1),tuple(uv))
            mesh.f.extend(tuple(start+int(i) for i in face) for face in data['t'])
    g['boulder']=boulder
