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
OUT=ROOT/'Sky_City_Project/Assets/Boids/Art/SkyCity'
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

# The dominant city: three useful, open garden terraces and a clear main dome.
island((-8,5.25,12),8.5,11.8,31)
arcade((-8,5.35,12),7.8,2.25,24);balustrade((-8,8.0,12),7.8,110)
arcade((-9,8.45,13),5.5,2.40,18);balustrade((-9,11.25,13),5.5,82)
arcade((-9.2,11.65,13.4),3.5,3.50,14)
architecture.cylinder((-9.2,11.85,13.4),3.25,3.3,IVORY,n=96)
for j in range(14):
    a=(j+.5)/14*2*pi
    window((-9.2+3.27*cos(a),12.35,13.4+3.27*sin(a)),.68,2.15,a-pi/2)
dome((-9.2,15.52,13.4),3.78,3.6,4)
for name,p,w,h in [('Dawn',(-14,8.8,12),1.40,10.9),('East',(-3.8,8.4,15.6),1.55,13.0),('Garden',(-13.0,8.0,18.3),1.05,10.6),('Front',(-5.4,7.8,7.5),1.06,8.4)]:tower(name,p,w,h)
villa((-12.0,9.0,8.5),3.0,2.25,2.4,81)
villa((-6.0,9.0,10.2),2.9,2.3,2.25,82)
arcade((-4.7,8.8,12),1.7,2.3,10);dome((-4.7,11.48,12),1.9,1.7,14)
villa((-13.5,5.9,8.9),2.5,2.6,2.6,85)
villa((-3.0,5.85,10.8),2.6,2.5,2.45,86)
island((-7.8,3.25,5.6),3.5,7.0,93)
villa((-7.8,3.35,5.6),3.4,2.6,2.6,89)
balustrade((-7.8,3.4,5.6),3.0,48,start=pi,end=2*pi)
tree((-10.0,3.6,4.8),.82,833);cypress((-5.6,3.5,5.7),3.0,834)
for j in range(7):ivy((-10.2+j*.68,3.3,3.5),random.uniform(2.3,4.5),900+j,.3)
garden_stair((-7.8,0,8.7),4.15,3.55,5.85,-pi*.78,-pi*.32,20)
garden_stair((-8,0,12),7.0,5.95,8.08,-.86,-.34,18)

# Open aqueduct bridge, from the palace terrace to a second floating belvedere.
bridge=Mesh('12 - The wind bridge')
start=Vector((-.8,6.50,10.0));end=Vector((10.2,6.8,16.0));direction=end-start
rot=math.atan2(direction.z,direction.x);length=Vector((direction.x,0,direction.z)).length
count=5;span=length/count
for j in range(count):
    t=(j+.5)/count;mid=start.lerp(end,t)
    arch(bridge,tuple(mid-Vector((0,3.0,0))),span-.34,1.88,.26,1.22,rot,IVORY,18)
    q=start.lerp(end,j/count)
    bridge.box(tuple(q-Vector((0,1.45,0))),(.40,3.1,1.3),IVORY,rot)
    # Gracefully tapered hanging supports, instead of piers reaching a ground.
    bridge.box(tuple(q-Vector((0,3.0,0))),(.65,.18,1.5),TRIM,rot)
    ivy(tuple(q-Vector((0,.25,.65))),2.1,600+j)
for side in (-1,1):
    sidev=Vector((-sin(rot),0,cos(rot)))*side*.69
    bridge.box(tuple((start+end)*.5+sidev+Vector((0,.7,0))),(length+.5,.16,.18),TRIM,rot)
    for j in range(60):
        p=start.lerp(end,j/59)+sidev
        trim.cylinder(tuple(p+Vector((0,.18,0))),.055,.49,TRIM,top=.045,n=7)
bridge.box(tuple((start+end)*.5+Vector((0,.10,0))),(length+.5,.26,1.55),IVORY,rot)
island((12,6.65,16),3.5,6.9,49)
arcade((12,6.8,16),2.45,3.55,10);dome((12,10.7,16),2.65,2.30,22)
balustrade((12,7.05,16),3.0,46)
for x,z,h in [(10.6,17.8,3.8),(13.8,16.1,4.1),(12.8,18.0,3.2)]:cypress((x,6.9,z),h,int(x*20))

# Vegetation is composed around the terraces, with open sight lines to arcades.
for x,y,z,h in [(-14.1,8.3,7.9,4.3),(-11.8,8.2,7.2,3.7),(-4.5,8.0,9.0,3.4),(-8.2,11.6,9.3,3.7),(-12.8,11.5,12.4,3.9),(-6.4,11.5,14.1,3.1),(-3.5,5.8,7.8,3.5),(-15.5,5.9,10.9,4.5),(-10.3,8.0,17.4,3.5)]:cypress((x,y,z),h,int((x+30)*20))
for j in range(24):
    a=j/24*2*pi;r=random.uniform(6.5,7.4);p=(-8+r*cos(a),8.05,12+r*sin(a))
    if j%3:tree(p,random.uniform(.5,.83),100+j)
    if j%2:flowers(add(p,(.25*cos(a),.10,.25*sin(a))),1250+j)
    ivy(add(p,(.55*cos(a),-.10,.55*sin(a))),random.uniform(1.4,4.0),300+j,.52)
for j in range(13):
    a=j/13*2*pi;p=(-9+5.05*cos(a),11.3,13+5.05*sin(a))
    tree(p,random.uniform(.43,.68),420+j);ivy(p,random.uniform(1.2,2.0),480+j)
for p,l,w,s in [((-13.5,5.2,6.4),10.4,.80,1),((-5.0,5.2,4.8),9.5,1.1,2),((13.3,6.5,13.4),6.8,.43,3)]:waterfall(p,l,w,s)

# A lower reflecting water garden: real basin, submerged floor and open spillways.
island((-6.5,1.92,-2.8),5.65,4.8,2041)
pool=Mesh('13 - Reflecting water garden','Water')
pts=[];rows=14;cols=128
for j in range(rows+1):
    r=j/rows
    for k in range(cols+1):
        a=k/cols*2*pi
        pts.append(((-6.5+5.15*r*cos(a),2.42,-2.8+3.65*r*sin(a)),rgb('74B6B3'),(r*cos(a)*.5+.5,r*sin(a)*.5+.5)))
pool.grid(pts,cols,rows)
for k in range(128):
    a=k/128*2*pi;b=(k+1)/128*2*pi
    if k not in (85,86,87,104,105,106):
        trim.quad([(-6.5+rx*cos(t),2.56,-2.8+rz*sin(t)) for rx,rz,t in [(5.15,3.65,a),(5.15,3.65,b),(5.52,4.02,b),(5.52,4.02,a)]],TRIM)
        trim.quad([(-6.5+5.52*cos(a),1.9,-2.8+4.02*sin(a)),(-6.5+5.52*cos(b),1.9,-2.8+4.02*sin(b)),(-6.5+5.52*cos(b),2.56,-2.8+4.02*sin(b)),(-6.5+5.52*cos(a),2.56,-2.8+4.02*sin(a))],IVORY)
    architecture.quad([(-6.5,2.03,-2.8),(-6.5+5.3*cos(a),2.03,-2.8+3.8*sin(a)),(-6.5+5.3*cos(b),2.03,-2.8+3.8*sin(b)),(-6.5,2.03,-2.8)],rgb('5F9993'))
for j in range(13):
    t=j/12;p=(-6.5-1.3*t,2.52+.91*t,.98+2.7*t)
    trim.box(p,(1.2,.16,.30),TRIM,.44)
    if j%3==0:
        for side in (-1,1):trim.cylinder(add(p,(side*.55,.08,0)),.045,.64,TRIM,n=7)
for a in (4.22,5.18):
    p=(-6.5+5.17*cos(a),2.42,-2.8+3.66*sin(a))
    waterfall(p,6.8,.72,int(a*100))
for j in range(10):
    a=j/10*2*pi;p=(-6.5+5.65*cos(a),2.42,-2.8+4.16*sin(a))
    if j in (3,7,8):continue
    ivy(p,random.uniform(1.5,3.0),2200+j,.27)
    if j%2==0:flowers(p,2300+j)

# Distant cities have distinct grouped skylines and atmospheric depth.
for index,(x,y,z,r) in enumerate([(17,16,60,4.2),(-24,15,52,3.1),(2.0,15.8,71,3.5),(30,9,90,5.4)]):
    island((x,y,z),r,r*1.5,720+index)
    arcade((x,y+.1,z),r*.73,r*.36,12)
    villa((x-r*.24,y+r*.40,z),r*.85,r*.65,r*.62,1700+index)
    dome((x-r*.24,y+r*1.05,z),r*.34,r*.40,80+index)
    for j in range(6):
        a=j*2.399+index;rr=r*(.35+.08*(j%3))
        tower('Far %d %d'%(index,j),(x+rr*cos(a),y+r*.35,z+rr*sin(a)),r*.15,r*(1.05+.23*(j%4)))

# Cloud meshes are truly three dimensional; their soft light comes from Unity.
for index,(x,y,z,sx,sy,sz) in enumerate([(-21,-6,-2,9,3.8,6),(17,-7,-1,11,3.8,8),(-2,-10,13,15,3.2,10),(26,2,24,9,4.0,9),(-28,5,25,8,4,8),(1,2,53,12,3,9),(26,7,65,11,3,10),(-20,5,78,16,3,12),(0,-3,95,26,4,18)]):
    for j in range(24):
        a=j*2.399;rr=sqrt(random.random())
        p=(x+cos(a)*sx*rr,y+random.uniform(-.6,.7)*sy,z+sin(a)*sz*rr)
        r=random.uniform(.18,.42)
        clouds.orb(p,(sx*r,sy*random.uniform(.7,1.2),sz*r),rgb('F1EDE7'),index*30+j,18,26,.16)

# A few long pennants put a visible scale and wind direction into the city.
for index,(x,y,z) in enumerate([(-14,21.2,12),(-3.8,23.0,15.6),(12,14.2,16)]):
    gold.cylinder((x,y,z),.025,1.45,GOLD,n=8)
    pts=[];cols=32;rows=8
    for j in range(rows+1):
        for k in range(cols+1):
            u=k/cols;v=j/rows;p=(x+u*1.9,y+.98-v*.52*(1-u*.55)+.075*u*sin(u*8),z+.13*u*sin(u*9+index))
            pts.append((p,rgb('D4A075'),(u,v)))
    flags.grid(pts,cols,rows)

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

environment=create_objects(list(groups.values()))
# Bake geometric crevice shade into alpha; this does not bake a camera image.
from mathutils.bvhtree import BVHTree
solid=[o for o in environment if o['Surface family'] not in ('Cloud','Water','Cloth')]
verts=[];faces=[]
for ob in solid:
    offset=len(verts);verts.extend([v.co[:] for v in ob.data.vertices])
    faces.extend([tuple(offset+i for i in p.vertices) for p in ob.data.polygons])
bvh=BVHTree.FromPolygons(verts,faces,all_triangles=False)
for ob in solid:
    me=ob.data;positions=[v.co.copy() for v in me.vertices];normals=[v.normal.copy().normalized() for v in me.vertices]
    colors=me.color_attributes['Pigment'];rgba=np.empty(len(me.vertices)*4,dtype=np.float32);colors.data.foreach_get('color',rgba);rgba=rgba.reshape(-1,4)
    for i in range(0,len(me.vertices),4):
        n=normals[i]
        if n.length<.5:continue
        axis=n.cross(Vector((0,0,1)))
        if axis.length<.1:axis=n.cross(Vector((0,1,0)))
        axis.normalize();other=n.cross(axis);occlusion=0
        for j in range(4):
            a=j*2.39996;z=.35+j*.15;rr=sqrt(1-z*z)
            ray=n*z+axis*rr*cos(a)+other*rr*sin(a)
            loc,normal,idx,distance=bvh.ray_cast(positions[i]+n*.018,ray,1.5)
            if loc is not None:occlusion+=(1-distance/1.5)**.5
        rgba[i:i+4,3]=1-.65*occlusion/4
    colors.data.foreach_set('color',rgba.ravel())
    print('AO',ob.name,flush=True)

# Set useful local origins after the shared-coordinate occlusion pass.
for ob in environment:
    center=sum((Vector(c) for c in ob.bound_box),Vector())/8
    for v in ob.data.vertices:v.co-=center
    ob.location=center

def export(path,objects):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in objects:ob.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',
        apply_unit_scale=True,use_space_transform=True,bake_space_transform=True,mesh_smooth_type='FACE',
        add_leaf_bones=False,bake_anim=False,colors_type='LINEAR')
export(OUT/'SkyCity_Environment.fbx',environment)

# A separate articulated swallow. +Z is its flight direction in Unity.
bird_start=set(groups)
body=Mesh('Swallow Body','Bird');body.orb((0,0,.06),(.115,.115,.34),rgb('EBE4CF'),81,18,26,.035)
body.orb((0,.09,.34),(.115,.11,.13),rgb('294A61'),83,14,22,.015)
body.orb((0,.025,.41),(.086,.047,.068),rgb('D8AA77'),84,10,16,.02)
body.tube([(0,.055,.40),(0,.052,.56)],.048,rgb('494A4A'),8,1.0)
for sign in (-1,1):body.orb((sign*.100,.115,.394),(.014,.017,.013),rgb('101D25'),89,6,10,.01)
def feather(mesh,root,tip,width,c):
    axis=Vector(tip)-Vector(root);side=Vector((axis.z,0,-axis.x)).normalized();pts=[]
    rows=12;cols=4
    for j in range(rows+1):
        t=j/rows;w=width*sin(pi*max(.001,min(.999,t)))**.45
        for k in range(cols+1):
            u=k/cols*2-1;p=Vector(root)+axis*t+side*u*w
            p.y+=.018*(1-u*u)*sin(pi*t)
            color=mix(c,rgb('405367'),clip((t-.72)/.28)*.65)
            pts.append((tuple(p),color,(k/cols,t)))
    mesh.grid(pts,cols,rows,True)
for sign,label in [(-1,'Left'),(1,'Right')]:
    wing=Mesh('Swallow '+label+' Wing','Bird')
    mantle=[];rows=26;cols=10
    for j in range(rows+1):
        t=j/rows;leading=.23-.49*t**1.5;chord=.45*max(.001,1-t)**.55
        for k in range(cols+1):
            v=k/cols;p=(sign*(.075+1.10*t),.025+.055*sin(t*pi)+.025*sin(v*pi),leading-chord*v)
            pigment=mix(rgb('E6DECD'),rgb('566D7A'),clip((.32-v)*2.4+t*.42))
            mantle.append((p,pigment,(t,v)))
    wing.grid(mantle,cols,rows,sign<0)
    # Curved mantle and overlapping primary/secondary flight feathers.
    for j in range(12):
        t=j/11
        root=(sign*(.08+t*.60),.02+.035*sin(t*pi),.17-t*.29)
        tip=(sign*(.19+t*.94),.015+.03*sin(t*pi),-.23-t*.23+t*t*.12)
        feather(wing,root,tip,.050 if j<8 else .047,mix(rgb('E6DCCC'),rgb('8999A4'),t*.68))
    feather(wing,(sign*.08,.035,.20),(sign*1.17,.04,-.29),.065,rgb('40596A'))
    tail=body
    feather(tail,(sign*.026,-.015,-.20),(sign*.19,-.035,-.83),.052,rgb('344B60'))
bird_objects=create_objects([groups[k] for k in groups if k not in bird_start])
export(OUT/'SkyCity_Swallow.fbx',bird_objects)
for ob in bird_objects:ob.hide_set(True)

camera_data=bpy.data.cameras.new('Sky City composition');camera=bpy.data.objects.new('Sky City composition',camera_data);scene.collection.objects.link(camera)
camera.location=(0,53,16);target=Vector((0,-6,8.7));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
camera_data.lens=38.06;scene.camera=camera
world=bpy.data.worlds.new('Pearl dawn');scene.world=world;world.color=(.30,.38,.5)
data=bpy.data.lights.new('Sunrise','SUN');data.energy=2.5;data.angle=.08;data.color=(1,.84,.66)
sun=bpy.data.objects.new('Sunrise',data);scene.collection.objects.link(sun);sun.rotation_euler=(.65,-.4,-.9)
scene.render.resolution_x=1536;scene.render.resolution_y=1024;scene.render.resolution_percentage=100
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            space=area.spaces.active;space.shading.color_type='VERTEX';space.overlay.show_overlays=False
            space.region_3d.view_perspective='CAMERA';space.region_3d.view_camera_zoom=10
            space.region_3d.view_location=target;space.region_3d.view_rotation=camera.rotation_euler.to_quaternion()
            space.region_3d.view_distance=(camera.location-target).length
for ob in scene.objects:ob.select_set(False)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'SkyCity.blend'),compress=True)
report={'blender':bpy.app.version_string,'environmentMeshes':len(environment),'birdMeshes':len(bird_objects),
        'vertices':sum(len(o.data.vertices) for o in environment),'faces':sum(len(o.data.polygons) for o in environment)}
(SOURCE/'authoring-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('SKY_CITY_COMPLETE',json.dumps(report),flush=True)
