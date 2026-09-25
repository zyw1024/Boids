"""Sky City: authored architecture and a swallow for the Unity scene.
Run only in a fresh Blender --background --factory-startup process.
All design coordinates below are Unity Y-up. No concept image is used by the model.
"""
import bpy, math, random, json
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.noise import noise
from math import sin, cos, pi, sqrt

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/Boids/Art/SkyCityHero'
SOURCE=Path(__file__).resolve().parent
OUT.mkdir(parents=True,exist_ok=True)
random.seed(93641)
scene=bpy.context.scene
for ob in list(scene.objects):bpy.data.objects.remove(ob,do_unlink=True)
scene.name='Sky City - The Garden of Winds'
scene.unit_settings.system='METRIC'
groups={}
def rgb(h):
    v=[int(h[i:i+2],16)/255 for i in (0,2,4)]
    return tuple(((x+.055)/1.055)**2.4 if x>.04045 else x/12.92 for x in v)
def mix(a,b,t):return tuple(x*(1-t)+y*t for x,y in zip(a,b))
def mul(a,s):return tuple(x*s for x in a)
def add(a,b):return tuple(x+y for x,y in zip(a,b))
def nz(p,s=1):return noise(Vector(p)*s,noise_basis='PERLIN_ORIGINAL')
def clip(x):return max(0,min(1,x))
IVORY=rgb('DFD6BE');TRIM=rgb('EEE4CA');COPPER=rgb('548E8B');GOLD=rgb('B99B60')
STONE=rgb('899191');DARK=rgb('384C59');WOOD=rgb('665955')

class Mesh:
    def __init__(self,name,kind='Stone'):
        self.name=name;self.kind=kind;self.v=[];self.f=[];self.c=[];self.uv=[]
        groups[name]=self
    def vert(self,p,c,uv=(0,0)):
        self.v.append(tuple(p));self.c.append((*c[:3],1));self.uv.append(uv);return len(self.v)-1
    def quad(self,pts,c):
        k=len(self.v)
        for p,uv in zip(pts,[(0,0),(1,0),(1,1),(0,1)]):self.vert(p,c,uv)
        self.f.append(tuple(range(k,k+4)))
    def grid(self,pts,cols,rows,reverse=False):
        k=len(self.v)
        for p,c,uv in pts:self.vert(p,c,uv)
        for j in range(rows):
            for i in range(cols):
                a=k+j*(cols+1)+i;f=(a,a+1,a+cols+2,a+cols+1)
                self.f.append(tuple(reversed(f)) if reverse else f)
    def box(self,p,size,c,rot=0):
        sx,sy,sz=[v/2 for v in size];v=[]
        for x,y,z in [(-sx,-sy,-sz),(sx,-sy,-sz),(sx,sy,-sz),(-sx,sy,-sz),(-sx,-sy,sz),(sx,-sy,sz),(sx,sy,sz),(-sx,sy,sz)]:
            v.append(add(p,(x*cos(rot)-z*sin(rot),y,x*sin(rot)+z*cos(rot))))
        for face in [(0,3,2,1),(4,5,6,7),(0,4,7,3),(1,2,6,5),(3,7,6,2),(0,1,5,4)]:self.quad([v[i] for i in face],c)
    def cylinder(self,p,r,h,c,top=None,n=32):
        if top is None:top=r
        pts=[]
        for y,rad in [(0,r),(h,top)]:
            for j in range(n+1):
                a=j/n*2*pi;pts.append((add(p,(rad*cos(a),y,rad*sin(a))),c,(j/n,y)))
        self.grid(pts,n,1,True)
        for y,rad,reverse in [(0,r,False),(h,top,True)]:
            k=self.vert(add(p,(0,y,0)),c)
            ring=[self.vert(add(p,(rad*cos(j/n*2*pi),y,rad*sin(j/n*2*pi))),c) for j in range(n)]
            for j in range(n):
                f=(k,ring[j],ring[(j+1)%n]);self.f.append(tuple(reversed(f)) if reverse else f)
    def tube(self,pts,r,c,sides=6,taper=.6):
        start=len(self.v)
        for j,p in enumerate(pts):
            tangent=(Vector(pts[min(j+1,len(pts)-1)])-Vector(pts[max(0,j-1)])).normalized()
            a=tangent.cross(Vector((0,0,1)))
            if a.length<.01:a=tangent.cross(Vector((0,1,0)))
            a.normalize();b=tangent.cross(a).normalized();rr=r*(1-taper*j/(len(pts)-1))
            for i in range(sides):self.vert(Vector(p)+rr*(a*cos(i*2*pi/sides)+b*sin(i*2*pi/sides)),c,(i/sides,j/(len(pts)-1)))
        for j in range(len(pts)-1):
            for i in range(sides):
                a=start+j*sides+i;b=start+j*sides+(i+1)%sides;self.f.append((a,b,b+sides,a+sides))
    def orb(self,p,scale,c,seed,rows=10,cols=16,rough=.12):
        pts=[]
        for j in range(rows+1):
            th=j/rows*pi
            for k in range(cols+1):
                ph=k/cols*2*pi;u=Vector((sin(th)*cos(ph),cos(th),sin(th)*sin(ph)))
                bump=1+rough*nz(add(tuple(u*2.9),(seed,0,0)))
                q=add(p,tuple(u[i]*scale[i]*bump for i in range(3)))
                shade=.88+.18*clip(.5+nz(q,2))
                pts.append((q,mul(c,shade),(k/cols,j/rows)))
        self.grid(pts,cols,rows)

architecture=Mesh('01 - Main palace and arcaded terraces')
trim=Mesh('02 - Limestone cornices and balustrades')
roofs=Mesh('03 - Verdigris domes','Copper')
gold=Mesh('04 - Gilded ribs and finials','Brass')
windows=Mesh('05 - Recessed windows','Dark')
cliffs=Mesh('06 - Suspended stratified cliffs','Rock')
garden=Mesh('07 - Terrace gardens and hanging ivy','Foliage')
branches=Mesh('08 - Roots and cypress trunks','Wood')
water=Mesh('09 - Falling water','Water')
clouds=Mesh('10 - Sculpted cloud banks','Cloud')
flags=Mesh('11 - Silk pennants','Cloth')

def frame(p,x,y,z,rot):return add(p,(x*cos(rot)-z*sin(rot),y,x*sin(rot)+z*cos(rot)))
def arch(mesh,p,width,spring,thick,depth,rot=0,c=IVORY,stones=16):
    r=width/2
    for j in range(stones):
        a=j/stones*pi+.003;b=(j+1)/stones*pi-.003
        col=mul(c,random.uniform(.96,1.035))
        ends=[]
        for z in (-depth/2,depth/2):
            ends.append([frame(p,rad*cos(t),spring+rad*sin(t),z,rot) for rad,t in [(r,a),(r,b),(r+thick,b),(r+thick,a)]])
        mesh.quad(ends[0],col);mesh.quad(ends[1][::-1],col)
        for k in range(4):mesh.quad([ends[0][k],ends[1][k],ends[1][(k+1)%4],ends[0][(k+1)%4]],col)
def column(p,h,r=.15):
    for y,rad,height,top in [(0,r*1.8,.10,r*1.8),(.10,r*1.4,.12,r*1.15),(.22,r,h-.44,r*.88),(h-.22,r*1.45,.11,r*1.55),(h-.11,r*1.85,.11,r*1.85)]:
        trim.cylinder(add(p,(0,y,0)),rad,height,TRIM,top,12)
def cornice(p,r):
    for y,rr,h in [(0,r,.12),(.12,r+.10,.10),(.22,r+.17,.12),(.34,r+.10,.055)]:trim.cylinder(add(p,(0,y,0)),rr,h,TRIM,n=80)
def balustrade(p,r,n=80,start=0,end=2*pi):
    for j in range(n):
        a=start+(end-start)*j/n;q=add(p,(r*cos(a),0,r*sin(a)))
        for h,rr,hh,top in [(0,.07,.08,.07),(.08,.055,.14,.087),(.22,.087,.18,.037),(.4,.052,.05,.052)]:
            trim.cylinder(add(q,(0,h,0)),rr,hh,TRIM,top,6)
    path=[add(p,(r*cos(start+(end-start)*j/(n*2)),.49,r*sin(start+(end-start)*j/(n*2)))) for j in range(n*2+1)]
    trim.tube(path,.068,TRIM,6,taper=0)
def arcade(p,r,h,n):
    architecture.cylinder(p,r+.20,.22,IVORY,n=80)
    for j in range(n):
        a=j/n*2*pi;column(add(p,(r*cos(a),.22,r*sin(a))),h-.35,.13 if r<4 else .17)
        mid=a+pi/n;half=r*sin(pi/n);q=add(p,(r*cos(pi/n)*cos(mid),.22,r*cos(pi/n)*sin(mid)))
        arch(architecture,q,half*2-.28,h-.50-(half-.14),.16,.34,mid+pi/2)
    cornice(add(p,(0,h,0)),r+.12)
    architecture.cylinder(add(p,(0,h+.05,0)),r,.20,IVORY,n=80)

def dome(p,r,height,seed=0,lantern=True):
    pts=[];rows=26;cols=96
    for j in range(rows+1):
        t=j/rows;theta=t*pi/2
        for k in range(cols+1):
            a=k/cols*2*pi;rad=r*cos(theta)*(1+.008*cos(a*16))
            q=add(p,(rad*cos(a),height*sin(theta),rad*sin(a)))
            pat=nz(add(q,(seed,0,0)),1.7)
            c=mix(rgb('356C73'),rgb('8AAE9D'),clip(.55+pat*.85+t*.12))
            pts.append((q,c,(k/cols,t)))
    roofs.grid(pts,cols,rows,True)
    for k in range(16):
        a=k/16*2*pi
        path=[add(p,(r*1.008*cos(j/28*pi/2)*cos(a),height*sin(j/28*pi/2)+.018,r*1.008*cos(j/28*pi/2)*sin(a))) for j in range(29)]
        gold.tube(path,max(.018,r*.009),mix(COPPER,GOLD,.44),6,.4)
    cornice(add(p,(0,-.35,0)),r)
    top=add(p,(0,height,0))
    if lantern and r>1.3:
        arcade(top,r*.16,r*.36,8)
        dome(add(top,(0,r*.36+.38,0)),r*.22,r*.27,seed+99,False)
    else:
        gold.cylinder(top,.035,.48,GOLD,top=.012,n=8)
        gold.orb(add(top,(0,.45,0)),(.065,.065,.065),GOLD,seed,5,8,.02)

def window(p,w,h,rot):
    r=w/2;s=h-r
    windows.box(frame(p,0,s/2,0,rot),(w,s,.035),DARK,rot)
    for j in range(16):
        a=j*pi/16;b=(j+1)*pi/16
        windows.quad([frame(p,0,s,0,rot),frame(p,r*cos(a),s+r*sin(a),0,rot),frame(p,r*cos(b),s+r*sin(b),0,rot),frame(p,0,s,0,rot)],DARK)
    arch(trim,p,w,s,.075,.12,rot,TRIM,12)
    for sign in (-1,1):trim.box(frame(p,sign*(r+.038),s/2,-.03,rot),(.075,s,.13),TRIM,rot)
    trim.box(frame(p,0,.02,-.05,rot),(w+.24,.12,.22),TRIM,rot)
    trim.box(frame(p,0,s*.52,-.065,rot),(.045,s*.97,.07),TRIM,rot)

def tower(name,p,w,h):
    global architecture
    old=architecture;architecture=Mesh('01 - Campanile '+name)
    architecture.box(add(p,(0,h*.415,0)),(w,h*.83,w),IVORY)
    for level in range(4):
        y=level*h*.195
        trim.box(add(p,(0,y,0)),(w+.24,.16,w+.24),TRIM)
        if level:
            for face in range(4):
                rot=face*pi/2
                q=frame(p,0,y+.34,-w/2-.025,rot)
                window(q,w*.30,min(h*.13,1.18),rot)
    top=add(p,(0,h*.83,0));hh=h*.12
    for face in range(4):
        rot=face*pi/2
        q=frame(top,0,0,-w*.46,rot)
        arch(architecture,q,w*.72,hh-w*.36,.15,.22,rot)
    for x,z in [(-1,-1),(-1,1),(1,1),(1,-1)]:column(add(top,(x*w*.46,0,z*w*.46)),hh,.11)
    trim.box(add(top,(0,hh+.10,0)),(w+.36,.24,w+.36),TRIM)
    roofs.cylinder(add(top,(0,hh+.22,0)),w*.72,w*1.3,COPPER,top=.02,n=8)
    for j in range(8):
        a=j/8*2*pi
        gold.tube([add(top,(cos(a)*w*.725,hh+.22,sin(a)*w*.725)),add(top,(0,hh+.22+w*1.3,0))],.018,GOLD,5,.7)
    gold.cylinder(add(top,(0,hh+w*1.3+.22,0)),.035,.65,GOLD,top=.008,n=8)
    architecture=old

def hip_roof(p,w,d,h):
    a=[add(p,(x*w/2,0,z*d/2)) for x,z in [(-1,-1),(1,-1),(1,1),(-1,1)]]
    q=add(p,(0,h,0))
    for j in range(4):
        k=len(roofs.v)
        for point in (a[j],a[(j+1)%4],q):roofs.vert(point,rgb('A87562'))
        roofs.f.append((k,k+1,k+2))
        # Thin courses describe the tiled pitch instead of a bare pyramid.
        for line in range(1,10):
            t=line/11
            u=Vector(a[j]).lerp(Vector(q),t);v=Vector(a[(j+1)%4]).lerp(Vector(q),t)
            roofs.tube([tuple(u+Vector((0,.015,0))),tuple(v+Vector((0,.015,0)))],.022,rgb('B3866B'),4,0)
def villa(p,w,d,h,seed):
    architecture.box(add(p,(0,h*.5,0)),(w,h,d),IVORY)
    trim.box(add(p,(0,h+.05,0)),(w+.30,.17,d+.30),TRIM)
    hip_roof(add(p,(0,h+.16,0)),w+.50,d+.5,h*.45)
    for face,span in [(0,w),(1,d),(3,d)]:
        rot=face*pi/2;deep=d if face==0 else w
        n=max(2,int(span/1.05))
        for i in range(n):window(frame(p,(i-(n-1)/2)*span/n,.50,-deep/2-.028,rot),.40,h*.53,rot)
    arcade(add(p,(0,-.06,-d*.60)),w*.36,h*.68,8)

def island(p,r,depth,seed):
    pts=[];rows=30;cols=100
    for j in range(rows+1):
        t=j/rows;y=-depth*t
        for k in range(cols+1):
            a=k/cols*2*pi
            rr=r*(1-t)**.68*(1+.065*sin(a*7+seed)+.08*sin(a*3+seed*.5))
            rr*=1+.09*nz((cos(a)*3+seed,t*9,sin(a)*3),1)
            rr+=r*.035*sin(t*21+a*2)*(1-t)
            q=add(p,(rr*cos(a),y,rr*sin(a)*.86))
            patch=nz(add(q,(seed,0,0)),.68)
            c=mix(rgb('6B717B'),rgb('B5AEA1'),clip(.4+.4*patch+.35*(1-t)))
            c=mix(c,rgb('697E62'),clip((.14-t)*6)*.55)
            pts.append((q,c,(k/cols,t)))
    cliffs.grid(pts,cols,rows)
    for j in range(11):
        a=j/15*2*pi;rr=r*random.uniform(.48,.83)
        q=add(p,(rr*cos(a),-random.uniform(.6,2.8),rr*sin(a)*.86))
        # Interlocking irregular mineral masses soften the conical base.
        cliffs.orb(add(q,(0,-depth*.25,0)),(r*.16,depth*.32,r*.15),STONE,seed*20+j,14,18,.65)

def leaf(p,length,width,normal,angle,c):
    n=Vector(normal).normalized();u=n.cross(Vector((0,1,0)))
    if u.length<.05:u=n.cross(Vector((1,0,0)))
    u.normalize();v=n.cross(u);u,v=u*cos(angle)+v*sin(angle),-u*sin(angle)+v*cos(angle)
    origin=Vector(p);k=len(garden.v)
    outline=[(-1,0),(-.48,-.75),(.1,-1),(.67,-.57),(1,0),(.67,.57),(.1,1),(-.48,.75)]
    garden.vert(origin+n*width*.32,mul(c,1.06))
    for x,y in outline:garden.vert(origin+u*(x*length)+v*(y*width),mul(c,.94+.10*x))
    for j in range(8):garden.f.append((k,k+1+j,k+1+(j+1)%8))

def cypress(p,h,seed):
    branches.cylinder(p,.09,h*.65,WOOD,top=.045,n=8)
    for j in range(5):
        t=j/4;y=h*(.24+.15*j)
        r=h*(.13-.024*j)
        garden.orb(add(p,(.10*sin(seed+j),y,0)),(r*.83,h*.23,r*.68),mix(rgb('254B49'),rgb('67806A'),j*.12),seed+j,10,13,.28)
    rng=random.Random(seed+127)
    for j in range(290):
        t=rng.uniform(.18,.94);a=j*2.399;rr=h*.16*sin(pi*t)**.85*(1-t*.52)
        q=add(p,(rr*cos(a),t*h,rr*sin(a)*.8))
        leaf(q,rng.uniform(.09,.17),rng.uniform(.035,.07),(cos(a),.3,sin(a)),rng.uniform(-.4,.4),mix(rgb('30554B'),rgb('70845C'),rng.random()))
def tree(p,r,seed):
    branches.tube([p,add(p,(.05,r*.5,.02)),add(p,(.1,r*.95,0))],r*.065,WOOD,7,.4)
    for j in range(7):
        a=j*2.399;rr=r*sqrt(j/7)*.68
        center=add(p,(cos(a)*rr,r*(1.0+random.uniform(-.2,.3)),sin(a)*rr))
        garden.orb(center,(r*.46,r*.40,r*.43),mix(rgb('42665D'),rgb('819B61'),random.random()),seed+j,8,12,.30)
        rng=random.Random(seed*7+j)
        for k in range(82):
            y=rng.uniform(-.8,1);a=k*2.399;side=sqrt(1-y*y);n=(cos(a)*side,y,sin(a)*side)
            q=add(center,(n[0]*r*.62,n[1]*r*.53,n[2]*r*.58))
            leaf(q,rng.uniform(.065,.115),rng.uniform(.04,.078),n,rng.random()*pi,mix(rgb('395F53'),rgb('A3B677'),rng.random()))
def ivy(p,length,seed,spread=.40):
    rng=random.Random(seed)
    for strand in range(5):
        root=add(p,(rng.uniform(-spread,spread),0,rng.uniform(-.2,.2)))
        end=add(root,(rng.uniform(-.3,.3),-length*rng.uniform(.55,1),-.12))
        path=[]
        for j in range(15):
            t=j/14;path.append(tuple(Vector(root).lerp(Vector(end),t)+Vector((sin(t*7+seed)*.08,0,-sin(t*pi)*.22))))
        branches.tube(path,.021,WOOD,5,.7)
        for j in range(15):
            q=path[j];r=rng.uniform(.11,.20)*(1-j/24)
            for side in (-1,1):
                pos=add(q,(side*r*.7,rng.uniform(-.05,.08),-.035))
                leaf(pos,r*1.2,r*.72,(rng.uniform(-.3,.3),rng.uniform(-.5,.3),-1),side*.75,mix(rgb('30574A'),rgb('91A968'),rng.random()))

def flowers(p,seed):
    rng=random.Random(seed)
    for j in range(18):
        q=add(p,(rng.uniform(-.42,.42),rng.uniform(.05,.3),rng.uniform(-.24,.24)))
        stem=add(q,(0,-.24,0));branches.tube([stem,q],.008,rgb('50704B'),4,.2)
        c=mix(rgb('B978A3'),rgb('EDC3AE'),rng.random())
        for k in range(5):
            a=k*2*pi/5;leaf(add(q,(cos(a)*.038,0,sin(a)*.038)),.046,.03,(0,1,0),a,c)
        garden.orb(add(q,(0,.018,0)),(.016,.018,.016),rgb('DDBA75'),seed+j,4,6,0)

def garden_stair(p,r,y0,y1,start,end,steps):
    for j in range(steps):
        a=start+(end-start)*j/steps;y=y0+(y1-y0)*j/steps
        q=add(p,(r*cos(a),y,r*sin(a)))
        architecture.box(q,(1.1,.20,r*abs(end-start)/steps+.07),IVORY,a)
        trim.box(add(q,(0,.105,0)),(1.15,.07,r*abs(end-start)/steps+.06),TRIM,a)
        if j%3==0:
            for side in (-1,1):trim.cylinder(add(p,((r+side*.57)*cos(a),y+.1,(r+side*.57)*sin(a))),.045,.64,TRIM,n=7)
    for side in (-1,1):
        path=[add(p,((r+side*.57)*cos(start+(end-start)*j/steps),y0+(y1-y0)*j/steps+.78,(r+side*.57)*sin(start+(end-start)*j/steps))) for j in range(steps+1)]
        trim.tube(path,.055,TRIM,6,0)

def waterfall(p,length,width,seed):
    pts=[];rows=42;cols=14
    for j in range(rows+1):
        t=j/rows
        for k in range(cols+1):
            u=k/cols-.5
            q=add(p,(u*width*(1+.12*sin(t*7+seed)),-t*length,-.45*sin(t*pi*.6)+.035*sin(u*45+t*12)))
            pts.append((q,rgb('DAEEEE'),(k/cols,t)))
    water.grid(pts,cols,rows,True)

def create_objects(meshes):
    made=[];mats={}
    for form in meshes:
        if not form.v:continue
        me=bpy.data.meshes.new(form.name)
        me.from_pydata([(-v[0],-v[2],v[1]) for v in form.v],[],[tuple(reversed(f)) for f in form.f]);me.update()
        colors=me.color_attributes.new(name='Pigment',type='FLOAT_COLOR',domain='POINT')
        colors.data.foreach_set('color',np.asarray(form.c,dtype=np.float32).ravel())
        uv=me.uv_layers.new(name='Surface UV')
        for poly in me.polygons:
            poly.use_smooth=form.kind in ('Copper','Foliage','Cloud','Bird','Water','Rock')
            for li in poly.loop_indices:uv.data[li].uv=form.uv[me.loops[li].vertex_index]
        ob=bpy.data.objects.new(form.name,me);scene.collection.objects.link(ob);made.append(ob)
        if form.kind not in mats:
            mat=bpy.data.materials.new('SkyCity_'+form.kind);mat.use_nodes=True
            node=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
            attr=mat.node_tree.nodes.new('ShaderNodeVertexColor');attr.layer_name='Pigment'
            mat.node_tree.links.new(attr.outputs['Color'],node.inputs['Base Color']);node.inputs['Roughness'].default_value=.68
            mats[form.kind]=mat
        me.materials.append(mats[form.kind]);ob['Surface family']=form.kind
    return made

def export(path,objects):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in objects:ob.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',
        apply_unit_scale=True,use_space_transform=True,bake_space_transform=True,mesh_smooth_type='FACE',
        add_leaf_bones=False,bake_anim=False,colors_type='LINEAR')
