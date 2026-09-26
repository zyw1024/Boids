"""E dream garden: individually composed botanical forms, sculpted reef, pigment colors.
Run with Blender --background --factory-startup --python this_file.
Coordinates in the authoring functions are Unity world coordinates (Y up).
The source is a complete editable Blender scene, exported as FBX for Unity.
"""
import bpy, math, random, json, os
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.noise import noise_vector, noise
from math import sin, cos, pi, exp, sqrt

ROOT = Path(__file__).resolve().parents[3]
SOURCE = Path(__file__).resolve().parent
OUT = ROOT / 'Sky_City_Project/Assets/Boids/Art/Atelier'
OUT.mkdir(parents=True, exist_ok=True)
random.seed(240923)
scene = bpy.context.scene
scene.name = 'E - The Submerged Garden'
# This script is run in a fresh factory-startup background process.
for ob in list(scene.objects): bpy.data.objects.remove(ob, do_unlink=True)
scene.unit_settings.system = 'METRIC'
groups = {}

def rgb(h):
    v = [int(h[i:i+2],16)/255 for i in (0,2,4)]
    return tuple(((x+.055)/1.055)**2.4 if x>.04045 else x/12.92 for x in v)
def lerp(a,b,t): return tuple(x*(1-t)+y*t for x,y in zip(a,b))
def clamp(x): return min(1,max(0,x))
def add(a,b): return tuple(x+y for x,y in zip(a,b))
def mul(a,t): return tuple(x*t for x in a)
def n3(p,scale=1): return noise(Vector(p)*scale, noise_basis='PERLIN_ORIGINAL')
def cubic(a,b,c,d,t):
    s=1-t
    return add(add(mul(a,s*s*s),mul(b,3*s*s*t)),add(mul(c,3*s*t*t),mul(d,t*t*t)))

class Shape:
    def __init__(self,name,kind):
        self.name=name; self.kind=kind; self.v=[];self.f=[];self.c=[];self.uv=[]
        groups[name]=self
    def vertex(self,p,color,uv=(0,0)):
        self.v.append(p);self.c.append((*color[:3],1));self.uv.append(uv)
        return len(self.v)-1
    def grid(self,points,cols,rows,reverse=False):
        start=len(self.v)
        for p,c,uv in points:self.vertex(p,c,uv)
        for y in range(rows):
            for x in range(cols):
                a=start+y*(cols+1)+x
                f=(a,a+1,a+cols+2,a+cols+1)
                self.f.append(tuple(reversed(f)) if reverse else f)
    def tube(self,points,radius,color,sides=7):
        start=len(self.v)
        for i,p in enumerate(points):
            tangent=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(i-1,0)])
            tangent.normalize();side=tangent.cross(Vector((0,0,1)))
            if side.length<.01:side=tangent.cross(Vector((0,1,0)))
            side.normalize();up=tangent.cross(side).normalized()
            r=radius*(1-.85*i/max(1,len(points)-1))
            for j in range(sides):
                q=Vector(p)+r*(side*cos(j*2*pi/sides)+up*sin(j*2*pi/sides))
                self.vertex(tuple(q),color,(j/sides,i/len(points)))
        for i in range(len(points)-1):
            for j in range(sides):
                a=start+i*sides+j;b=start+i*sides+(j+1)%sides
                self.f.append((a,b,b+sides,a+sides))
    def orb(self,center,scale,color,seed=0,rows=9,cols=14,rough=.12):
        points=[]
        for y in range(rows+1):
            v=y/rows;th=v*pi
            for x in range(cols+1):
                u=x/cols;ph=u*2*pi
                unit=(sin(th)*cos(ph),cos(th),sin(th)*sin(ph))
                noisev=n3(add(mul(unit,2.8),(seed,0,0)))
                r=1+noisev*rough
                p=add(center,tuple(unit[i]*scale[i]*r for i in range(3)))
                patch=.82+.28*clamp(.5+n3(add(mul(unit,4),(seed,0,0))))
                c=mul(color,patch)
                points.append((p,c,(u,v)))
        self.grid(points,cols,rows)

leaf=Shape('01 - Iridescent laminae','Leaves')
vein=Shape('02 - Branching leaf veins','Veins')
flower=Shape('03 - Apricot corollas','Petals')
violet=Shape('04 - Violet and peacock corollas','Petals')
stoneNear=Shape('05 - Foreground sculpted reef','Stone')
stoneMid=Shape('06 - Middle terraces and rooted cliffs','Stone')
stoneFar=Shape('07 - Distant reef architecture','Distant')
stems=Shape('08 - Botanical stems and branching roots','Stone')
sprigs=Shape('09 - Golden branching inflorescences','Gold')
detail=Shape('10 - Reef garden and small corals','Petals')
fanVeins=Shape('14 - Glazed fan veins','Veins')

def lamina(shape,controls,width,palette,seed,fold=.35,veins=True,cols=40,rows=96):
    if shape is leaf: shape=Shape('01 - Hero lamina %02d'%seed,'Leaves')
    low,mid,high=[rgb(x) for x in palette]
    def point(t,u):
        center=cubic(*controls,t)
        tang=Vector(cubic(*controls,min(.9999,t+.002)))-Vector(cubic(*controls,max(.0001,t-.002)))
        side=Vector((tang.y,-tang.x,0)).normalized()
        s=2*u-1
        envelope=sin(pi*max(.0001,min(.9999,t)))**.42
        envelope*=.35+.65*t
        scallop=1+.045*sin(t*21+seed)+.012*sin(t*49+seed*.7)
        asym=1+.12*s*sin(t*4+seed)
        p=Vector(center)+side*s*width*envelope*scallop*asym
        p.z+=width*envelope*(fold*s*s-.1*exp(-s*s*50))
        p.z+=.045*width*sin(t*11+abs(s)*5+seed)*abs(s)**1.5*envelope
        p.z+=.012*width*sin(t*24+s*3)*abs(s)**4
        return tuple(p)
    pts=[]
    for y in range(rows+1):
        t=y/rows
        for x in range(cols+1):
            u=x/cols;s=abs(u*2-1);p=point(t,u)
            patch=n3(add(mul(p,1.6),(seed*3,0,0)))
            broad=n3(add(mul(p,.48),(0,seed,0)))
            light=clamp(.46+.36*t+.28*patch-.20*(1-s))
            c=lerp(low,mid,clamp(light*1.5))
            c=lerp(c,high,clamp((light-.58)*1.8))
            c=lerp(c,rgb('587B86'),clamp(broad*.40+.025))
            # Broken warm lip, localized and unequal, carried by the actual folded rim.
            edge=clamp((s-.965)/.035)*clamp(.3+patch*2)*(.18+.62*t)
            c=lerp(c,rgb('D4AB74'),edge*.65)
            pts.append((p,c,(u,t)))
    shape.grid(pts,cols,rows,reverse=True)
    if veins:
        central=[add(point(t,.5),(0,0,-.024)) for t in [i/38 for i in range(2,37)]]
        vein.tube(central,.008*sqrt(width),lerp(mid,rgb('A49670'),.42),5)
        for j in range(13):
            t0=.18+j*.053
            for sign in (-1,1):
                path=[]
                end=min(.96,t0+.22+random.random()*.08)
                for k in range(17):
                    f=k/16;t=t0+(end-t0)*f
                    path.append(add(point(t,.5+sign*.48*sin(f*pi*.5)),(0,0,-.024)))
                col=lerp(mid,rgb('CFAB77'),random.uniform(.12,.36))
                vein.tube(path,.0055*sqrt(width),col,5)
                for f0 in (.38,.65):
                    twig=[]
                    for k in range(8):
                        f=k/7;t=t0+(end-t0)*(f0+f*.13)
                        u=.5+sign*(.48*sin(f0*pi*.5)+f*.14)
                        twig.append(add(point(t,min(.99,max(.01,u))),(0,0,-.025)))
                    vein.tube(twig,.0032*sqrt(width),col,4)

def fan_lamina(name,root,rim,palette,seed,cup):
    """A hand-composed scalloped fan contour, with folds radiating from its root."""
    form=Shape('01 - Hero fan '+name,'Leaves');low,mid,high=[rgb(x) for x in palette]
    def edge(u):
        f=u*(len(rim)-1);i=min(len(rim)-2,int(f));t=f-i
        p0=Vector(rim[max(0,i-1)]);p1=Vector(rim[i]);p2=Vector(rim[i+1]);p3=Vector(rim[min(len(rim)-1,i+2)])
        return .5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t)
    rows=110;cols=96;points=[]
    for y in range(rows+1):
        t=max(.0001,y/rows)
        for x in range(cols+1):
            u=x/cols;e=edge(u)
            # A living lip changes at several scales; it never reads as a cutout.
            flutter=.011*sin(u*39+seed)+.006*sin(u*89+seed*.8)+.003*sin(u*197+seed*2)
            e=Vector(root)+(e-Vector(root))*(1+flutter)
            p=Vector(root).lerp(e,t)
            p.x+=.16*sin(t*pi)*sin(u*pi*2+seed)
            p.z+=cup*sin(t*pi)*(.45+.55*sin(u*pi))
            p.z+=.34*sin(u*13+seed+t*.7)*sin(t*pi)**.65*t
            p.z+=.10*sin(u*37+seed+t*2)*t*t
            p.z+=.045*sin(u*79+seed)*t**5
            curl=clamp((t-.80)/.20)
            gesture=sin(u*pi)**1.5
            p.z+=.65*sin(curl*pi*.72)*gesture
            p.y-=.24*curl*curl*gesture
            patch=n3(tuple(p),1.8)
            v=clamp(.18+.61*t+.13*sin(u*5+seed)+patch*.18)
            c=lerp(low,mid,clamp(v*1.6));c=lerp(c,high,clamp((v-.53)*1.45))
            lip=clamp((t-.984)/.016)*clamp(.3+patch*2)
            c=lerp(c,rgb('C59E76'),lip*.48)
            points.append((tuple(p),c,(u,t)))
    form.grid(points,cols,rows,reverse=True)
    def on_surface(t,u):
        ft=clamp(t)*rows;fu=clamp(u)*cols;it=min(rows-1,int(ft));iu=min(cols-1,int(fu));vt=ft-it;vu=fu-iu
        at=lambda row,col:Vector(points[row*(cols+1)+col][0])
        p=at(it,iu).lerp(at(it,iu+1),vu).lerp(at(it+1,iu).lerp(at(it+1,iu+1),vu),vt)
        p.z-=.018
        return tuple(p)
    for rib,end_u in enumerate((.06,.24,.43,.66,.91)):
        def trace(t):return .49+(end_u-.49)*sin(t*pi*.5)+.018*sin(t*6+seed+rib)*t
        ts=[.10+i*.86/55 for i in range(56)]
        fanVeins.tube([on_surface(t,trace(t)) for t in ts],.012,rgb('AF967D'),6)
        for branch in range(5):
            t0=.25+branch*.13
            for sign in (-1,1):
                path=[]
                for k in range(24):
                    f=k/23;t=t0+.16*f
                    u=trace(t)+sign*(.065+.02*sin(seed+branch))*sin(f*pi*.5)
                    path.append(on_surface(t,u))
                fanVeins.tube(path,.0075,rgb('A89486'),5)

# Main silhouettes are composed as broad fans instead of repeated ribbon leaves.
fan_lamina('Lavender sail',(-9.8,2.8,-2.8),[(-11.3,9.5,.4),(-10.8,11.2,.2),(-9.4,12.0,.1),(-7.4,11.5,-.4),(-5.9,10.4,-.9),(-6.1,9.0,-1.3),(-7.5,7.7,-1.7)],['29334D','796E8E','B29CB2'],3,-1.2)
fan_lamina('Turning mauve wave',(-9.6,2.8,-4.3),[(-9.0,8.1,-3.6),(-7.8,9.2,-3.9),(-5.7,9.5,-4.0),(-3.5,8.9,-4.1),(-3.0,8.0,-4.3),(-4.0,7.0,-4.5),(-6.3,5.9,-4.4)],['244558','687F89','B3A2B0'],8,-.8)
fan_lamina('Indigo rear sail',(-10.2,2.5,.7),[(-12,10.0,2),(-11.4,12.1,2.3),(-10.2,12.6,2.2),(-9.0,11.9,2.0),(-8.7,10.5,1.8)],['293249','5C627F','8C8CA8'],15,-.7)
lamina(leaf,[(-9.2,2.2,-3.8),(-7.8,3.4,-4.4),(-5.6,6.7,-5.2),(-4.5,6.3,-5)],1.17,['203C50','4E677A','9F9BAA'],20,.35)
lamina(leaf,[(-10,-1,-7),(-8.8,2.2,-7),(-6.5,3.1,-7),(-5.8,2,-7)],1.15,['142836','253C50','5B6474'],25,.44)
lamina(leaf,[(-10,-1,-7.3),(-10.1,1.5,-8),(-8.4,3.3,-8.2),(-7.9,4.3,-8)],.83,['142333','324756','5F6B77'],28,.32)
lamina(leaf,[(10.8,-1,-6),(9.2,3,-5),(7.9,7.4,-2.5),(11.2,8.8,-1.5)],1.65,['152C37','2F5059','6D7C80'],31,.72)
lamina(leaf,[(10.4,-1,-7),(9.2,.2,-7.5),(6.5,2.4,-7),(5.5,1.2,-7)],.95,['162B35','2B4754','706F72'],35,.38)
lamina(leaf,[(10.3,-.6,-7.5),(11.1,1,-7),(11,3.2,-6),(10.2,4.4,-4)],.85,['182F3C','385567','746C83'],38,.26)

def coral_cup(shape,center,normal,radius,color,seed,rows=5,cols=18):
    """A shallow, scalloped, fully curved coral plate rooted to a solid surface."""
    normal=Vector(normal).normalized();tangent=normal.cross(Vector((0,1,0)))
    if tangent.length<.1:tangent=normal.cross(Vector((1,0,0)))
    tangent.normalize();up=normal.cross(tangent).normalized();points=[]
    for j in range(rows+1):
        t=max(.001,j/rows)
        for k in range(cols+1):
            a=k/cols*2*pi
            scallop=1+.065*sin(a*5+seed)+.038*sin(a*9+seed*.37)
            radial=t*radius*scallop
            depth=radius*(.14*(1-t*t)+.025*sin(a*6+seed)*t**4)
            p=Vector(center)+tangent*(cos(a)*radial)+up*(sin(a)*radial*(.62+.15*sin(seed)))+normal*depth
            pigment=.74+.28*t+.06*sin(a*3+seed)+.06*n3(tuple(p),9)
            c=mul(color,pigment)
            points.append((tuple(p),c,(k/cols,t)))
    shape.grid(points,cols,rows,reverse=True)

def cloud_crown(shape,center,radius,palette,seed):
    rng=random.Random(seed);dark,body,light=[rgb(x) for x in palette]
    # Each overlapping membrane has its own gesture. Their shared throat holds
    # an asymmetric cloud together while avoiding concentric, repeated shells.
    gestures=[(0,0,1.0,-.04),(-.30,-.12,.80,.38),(.24,-.08,.82,-.38),
              (-.08,.17,.64,.15),(-.25,-.32,.57,.67),(.22,-.31,.53,-.61)]
    for layer,(cx,cy,size,rotation) in enumerate(gestures):
        r=radius*size
        rotation+=rng.uniform(-.16,.16)
        origin=Vector(add(center,(cx*radius,cy*radius,-radius*.135*layer)))
        root=origin+Vector((-.12*r*sin(rotation),-.78*r,.06*r))
        points=[];rows=60;cols=112
        for j in range(rows+1):
            t=max(.001,j/rows)
            for k in range(cols+1):
                u=k/cols;angle=(-.17+1.34*u)*pi
                scallop=1+.065*sin(angle*5+seed+layer*.8)+.034*sin(angle*13+seed*.4)+.014*sin(angle*37+layer)
                px=cos(angle)*r*scallop;py=sin(angle)*r*.88
                rim=origin+Vector((px*cos(rotation)-py*sin(rotation),px*sin(rotation)+py*cos(rotation),.045*r*sin(angle*3+seed)))
                p=root.lerp(rim,t)
                p.z-=r*.31*sin(pi*t)*(.5+.5*sin(pi*u))
                p.z+=r*.075*t**7
                p.z+=r*.040*sin(angle*8+seed+layer*.7)*sin(pi*t)**.7*t
                p.z+=r*.025*sin(angle*23+seed+t*3)*t*t
                p.z+=r*.012*sin(angle*51+layer)*t**5
                p.z+=r*.09*n3((p.x+seed,p.y,layer*.3),3.2)*sin(pi*t)**.5
                p.x+=r*.055*sin(t*pi)*sin(angle*2+seed)
                patch=n3(tuple(p),2.2)
                value=clamp(.34+.34*t+.12*cos(angle)+.16*patch)
                c=lerp(dark,body,clamp(value*1.7));c=lerp(c,light,clamp((value-.52)*1.8))
                # Thin broken light on the genuine curved rim, with violet folds.
                lip=clamp((t-.973)/.027)*clamp(.3+patch*1.4)
                c=lerp(c,light,lip*.30)
                points.append((tuple(p),c,(u,t)))
        shape.grid(points,cols,rows)

def corolla(shape,center,radius,palette,seed,layers=6):
    rng=random.Random(seed)
    if radius>1:
        shape=Shape(('03' if shape is flower else '04')+' - Sculpted crown %02d'%seed,'Petals')
        cloud_crown(shape,center,radius,palette,seed)
        return
    dark,body,light=[rgb(x) for x in palette]
    tilt=rng.uniform(-.48,.48)
    yaw=rng.uniform(-.40,.40)
    # Each ring has a different gesture and depth; broad scalloped laminae overlap.
    for layer in range(layers):
        ring=layer/max(1,layers-1)
        count=4+int((1-ring)*3)
        for petal in range(count):
            angle=2*pi*petal/count+layer*.57+rng.uniform(-.24,.24)
            length=radius*(1-.42*ring)*rng.uniform(.75,1.18)
            breadth=length*rng.uniform(.47,.71)
            base=radius*(.06+.02*ring)
            seedp=rng.random()*20
            points=[]; cols=12;rows=23
            for y in range(rows+1):
                t=y/rows
                reach=base+length*t
                for x in range(cols+1):
                    u=2*x/cols-1
                    w=sin(pi*max(.0001,min(.9999,t)))**.48*breadth
                    w*=1+.055*sin(t*35+seedp)+.025*sin(t*71)
                    cross=u*w
                    px=cos(angle)*reach-sin(angle)*cross+sin(layer*2.4)*radius*.13
                    py=sin(angle)*reach+cos(angle)*cross+cos(layer*1.9)*radius*.10
                    depth=-radius*(.15+ring*.48)-sin(t*pi)*length*.27+(t**6)*length*.14
                    depth+=u*u*w*.52+.06*length*sin(t*24+u*6+seedp)*abs(u)
                    p=add(center,(px*cos(yaw)-depth*sin(yaw),py*.84*cos(tilt)-depth*sin(tilt),depth*cos(yaw)*cos(tilt)+px*sin(yaw)+py*sin(tilt)))
                    patch=n3(add(mul(p,3),(seedp,0,0)))
                    v=clamp(.25+.49*t+.18*sin(angle)+patch*.22)
                    c=lerp(dark,body,clamp(v*1.65))
                    c=lerp(c,light,clamp((v-.49)*1.65))
                    c=mul(c,.83+.17*abs(u)+.10*t)
                    points.append((p,c,(x/cols,t)))
            shape.grid(points,cols,rows,reverse=True)
    # A recessed centre with individually shaded stamens.
    for j in range(7 if radius<.7 else 0):
        a=j*2.39996;r=radius*.105*sqrt((j+1)/14)
        p=add(center,(cos(a)*r,sin(a)*r,-radius*.77))
        shape.orb(p,(radius*.033,radius*.028,radius*.032),lerp(body,light,.65),seed+j,4,7,.04)

corolla(flower,(-.75,9.2,10),2.45,['97637D','D59C73','FFD094'],41)
corolla(flower,(-2.3,8.0,8.8),1.75,['8C637A','C89384','E9B69B'],47,5)
corolla(flower,(-2.0,11.7,16),1.9,['9B7E96','C5A4B1','E1BCC0'],50,5)
corolla(flower,(.2,12.6,20),1.9,['B19A9A','DBC0AC','F2D5AD'],53,5)
for x,y,z,r,s in [(-5.9,13,11,2.15,61),(-8,13.5,13,1.8,62),(-4.5,11.4,14,1.5,63),(7.3,11.7,13,1.45,64),(9.1,9.8,8,1.75,65),(8.6,7.7,7,1.6,66),(10.9,12.8,16,2.2,67)]:
    palette=['273E54','53637D','8C8BA0'] if x<0 else ['24404F','486878','849092']
    corolla(violet,(x,y,z),r,palette,s,5)

# Distant canopies and hanging roots divide the luminous opening into depths.
for x,y,z,r,seed in [(4.4,11.3,29,.72,70),(5.3,9.75,32,.51,71)]:
    farCrown=Shape('04 - Distant cloud crown %d'%seed,'Petals')
    cloud_crown(farCrown,(x,y,z),r,['344957','566B73','959080'],seed)
    for strand in range(7):
        dx=random.uniform(-.55,.55)*r;length=random.uniform(1.5,3.3)*r
        start=(x+dx,y-.38*r,z+random.uniform(-.2,.2))
        tip=add(start,(random.uniform(-.2,.2),-length,.35))
        bend=random.uniform(-.35,.35)
        path=[cubic(start,add(start,(bend,-length*.3,.15)),add(tip,(-bend,length*.25,0)),tip,i/24) for i in range(25)]
        stems.tube(path,random.uniform(.022,.047),rgb('586F78'),6)
        for fork in (9,15):
            p=path[fork];end=add(p,(random.uniform(-.3,.3),-length*.3,.2))
            stems.tube([cubic(p,add(p,(bend*.2,-length*.1,.03)),add(end,(0,length*.1,0)),end,i/13) for i in range(14)],.014,rgb('6C7F82'),5)

def reef(shape,center,scale,palette,seed):
    low,mid,high=[rgb(x) for x in palette]
    rng=random.Random(seed)
    points=[];rows=80;cols=120
    for iy in range(rows+1):
        t=iy/rows;theta=t*pi
        for ix in range(cols+1):
            u=ix/cols;phi=u*2*pi
            unit=Vector((sin(theta)*cos(phi),cos(theta),sin(theta)*sin(phi)))
            q=unit*2.3+Vector((seed*3.3,seed*.4,0))
            broad=noise(q,noise_basis='PERLIN_ORIGINAL')
            grain=noise(q*3.8,noise_basis='PERLIN_ORIGINAL')
            # Eroded lobes and horizontal ledges, with vertical clefts.
            terrace=.04*sin(unit.y*29+unit.x*4)
            fissure=abs(noise(q*6.3,noise_basis='PERLIN_ORIGINAL'))
            radial=1+.26*broad+.11*grain+terrace-.065*fissure
            p=add(center,tuple(unit[i]*scale[i]*radial for i in range(3)))
            p=(p[0]+.11*scale[0]*sin(unit.y*4+seed),p[1],p[2])
            patch=noise(q*2.1,noise_basis='PERLIN_ORIGINAL')
            v=clamp(.4+.28*unit.y+.38*patch)
            c=lerp(low,mid,clamp(v*1.65))
            c=lerp(c,high,clamp((v-.5)*1.35))
            c=lerp(c,rgb('765272'),clamp(grain*.72+.1))
            c=mul(c,.72+.35*clamp(.5+grain*2))
            points.append((p,c,(u,t)))
    shape.grid(points,cols,rows)
    # Interlocking shelves break the parent silhouette and make each reef a place.
    for k in range(7):
        p=add(center,(rng.uniform(-.75,.75)*scale[0],rng.uniform(-.3,.82)*scale[1],-scale[2]*rng.uniform(.65,.98)))
        s=(scale[0]*rng.uniform(.18,.41),scale[1]*rng.uniform(.09,.19),scale[2]*rng.uniform(.19,.42))
        shape.orb(p,s,lerp(mid,high,rng.uniform(.05,.35)),seed+k,12,28,.62)
        for thread in range(2):
            root=add(p,(rng.uniform(-.6,.6)*s[0],-.06,-s[2]*.85))
            length=rng.uniform(.12,.45)*scale[1]
            end=add(root,(rng.uniform(-.15,.15),-length,.18))
            shape.tube([cubic(root,add(root,(.06,-length*.3,-.04)),add(end,(-.03,length*.2,0)),end,i/16) for i in range(17)],rng.uniform(.012,.025),lerp(low,mid,.45),5)

nearPal=['101F30','2E4056','665776'];midPal=['193951','395C78','818A9F'];farPal=['2A5570','5B7E94','999BAE']
reef(stoneNear,(-9.2,.25,-2.8),(3.0,3.0,2.1),nearPal,81)
reef(stoneNear,(-6.6,-.65,-4.2),(3.2,2.5,2.4),nearPal,82)
reef(stoneNear,(-2.1,-1.35,-2.8),(3.8,1.8,2.3),midPal,83)
reef(stoneNear,(10.0,1.1,-1),(2.7,4.2,2.6),nearPal,84)
reef(stoneNear,(8.9,5.1,1.9),(1.3,2.7,1.65),nearPal,85)
reef(stoneMid,(-6.0,3.7,7),(2.6,4.0,2.3),midPal,86)
reef(stoneMid,(-3.5,4.7,10),(1.35,3.0,1.5),midPal,87)
reef(stoneMid,(8.0,4.7,8),(2.0,3.5,2),midPal,88)
reef(stoneMid,(9.7,7.5,13),(2,3.2,2),midPal,89)
reef(stoneFar,(2.8,.8,18),(1.3,2.4,1.5),farPal,90)
reef(stoneFar,(.0,-.6,14),(2.8,1.7,2),farPal,91)
reef(stoneFar,(6,-.8,23),(2.3,2,2),farPal,92)
reef(stoneFar,(-5.8,8.8,23),(1.6,4.5,2),farPal,93)
reef(stoneFar,(6.9,8.7,24),(1.4,4.1,1.6),farPal,94)

# Branching stems disappear into reef and foliage rather than ending as bare sticks.
for a,b,c,d,r in [((-4.3,3.6,10),(-3.9,6.1,12),(-1.5,6.9,11),(-.75,9.2,10),.23),((-5.5,6.3,14),(-5.5,8.7,14),(-6.3,9.3,12),(-5.9,13,11),.19),((8,4.7,10),(8.4,7.5,11),(7,10,12),(7.3,11.7,13),.2)]:
    path=[cubic(a,b,c,d,i/30) for i in range(31)]
    stems.tube(path,r,rgb('485768'),12)
    for j in range(5):
        p=path[5+j*4];tip=add(p,((-1 if j%2 else 1)*.7,1.1,-.2))
        stems.tube([cubic(p,add(p,(0,.4,0)),add(tip,(0,-.3,0)),tip,i/12) for i in range(13)],r*.32,rgb('626D7C'),7)
        corolla(detail,tip,random.uniform(.28,.48),['334F66','6C748E','9993AD'],120+j,3)

# Dense reef flora are clustered in compositional islands, leaving the passage open.
islands=[(-8.1,2.5,-6.3,2.1,1.1),(-6.1,.9,-7.0,2.9,1.1),(-2.8,-.1,-5.9,2,.8),(-5.5,5.2,3.7,1.4,1.4),(9.4,3.1,-2.7,1.5,2),(8.5,6.7,5.3,1.1,1),(2.8,2.3,15,1,.45)]
palettes=[['253C52','5B657E','9C8DA7'],['354453','788C88','B9B298'],['594657','A07A87','D0A28C'],['2B4856','56787D','98A99C']]
# All small flora below are rooted directly to sampled reef faces.

def golden_sprig(root,length,seed):
    rng=random.Random(seed)
    dx=rng.uniform(-.45,.45)*length
    a=root;b=add(root,(dx*.15,length*.34,.06));c=add(root,(dx*.65,length*.75,.1));d=add(root,(dx,length,.18))
    pts=[cubic(a,b,c,d,i/24) for i in range(25)]
    color=lerp(rgb('6B705A'),rgb('CFA266'),rng.uniform(.4,.9))
    sprigs.tube(pts,.009*sqrt(length),color,5)
    for j in range(4,22,2):
        p=pts[j];side=-1 if j%4 else 1
        end=add(p,(side*length*rng.uniform(.06,.14),length*.14,-.015))
        branch=[cubic(p,add(p,(side*.08,.03,0)),add(end,(-side*.03,-.07,0)),end,i/7) for i in range(8)]
        sprigs.tube(branch,.0045*sqrt(length),color,4)
        for k in range(3):
            bud=add(end,(rng.uniform(-.04,.04),rng.uniform(-.025,.025),rng.uniform(-.02,.02)))
            size=rng.uniform(.013,.03)*sqrt(length)
            sprigs.orb(bud,(size,size*1.25,size),lerp(color,rgb('E9BF7A'),rng.uniform(.3,.8)),seed+k,4,6,.16)

for i in range(100):
    if i<65:root=(random.uniform(-10,-3.7),random.uniform(-.3,2.1),random.uniform(-6,-4))
    else:root=(random.uniform(7.5,10.4),random.uniform(2.2,6.5),random.uniform(0,3))
    golden_sprig(root,random.uniform(.45,2.4),400+i)

# Colonies grow on reef surfaces rather than floating in front of them.
crust=Shape('12 - Encrusting reef colonies','Stone')
for form in (stoneNear,stoneMid,stoneFar):
    samples=[];weights=[]
    for face in form.f:
        a,b,c=[Vector(form.v[i]) for i in face[:3]];cross=(b-a).cross(c-a)
        if cross.length<.00001:continue
        normal=cross.normalized()
        if normal.z>-.25:continue
        samples.append((face,a,b,c,normal));weights.append(cross.length)
    cumulative=np.cumsum(weights)
    count=4500 if form is stoneNear else (2200 if form is stoneMid else 900)
    foregroundFlowers=0
    for j in range(count):
        face,a,b,c,normal=samples[int(np.searchsorted(cumulative,random.random()*cumulative[-1]))]
        f=sqrt(random.random());g=random.random()
        p=a*(1-f)+b*f*(1-g)+c*f*g+normal*.016
        s=random.uniform(.055,.18)*(1 if form is stoneNear else .85)
        patch=n3(tuple(p),1.0)
        if patch<-.15:continue
        accents=['4A7288','7F688B','AC7D87','66837F','A29272']
        index=int(clamp(.5+patch*.8)*4.99)
        col=lerp(form.c[face[0]][:3],rgb(accents[index]),random.uniform(.16,.38))
        coral_cup(crust,tuple(p),normal,s,col,9000+j,rows=4,cols=14)
        if form is stoneNear and foregroundFlowers<18 and -7.2<p.x<-2.5 and -.75<p.y<1.0 and normal.z<-.7 and j%5==0:
            # Sparse individual blossoms punctuate the encrusting garden.
            # Their roots are sampled on the actual foreground reef surface.
            coralPalette=['493A59','987087','C19A92'] if foregroundFlowers%3 else ['715065','B78673','D1AA82']
            corolla(detail,tuple(p+normal*.018),random.uniform(.13,.26),coralPalette,1600+foregroundFlowers,3)
            foregroundFlowers+=1

# Paint marks are small surface-following meshes. Their broken brush alpha is
# resolved by Unity, and their geometry stays attached to the modeled forms.
skins=Shape('11 - Surface impasto marks','Brush')
for form in list(groups.values()):
    if not form.f or form.kind in ('Gold','Veins','Brush'):continue
    count=1800 if form.kind=='Stone' else (1200 if form.name.startswith('01') else 950)
    if form is detail:count=2200
    for j in range(count):
        face=random.choice(form.f)
        a,b,c=[Vector(form.v[i]) for i in face[:3]]
        n=(b-a).cross(c-a)
        if n.length<.00001:continue
        n.normalize()
        if n.z>0:n=-n
        if abs(n.z)<.16:continue
        f=random.uniform(.1,.9);g=random.uniform(.1,.8)*(1-f)
        p=a*(1-f-g)+b*f+c*g+n*.018
        tangent=(b-a).normalized();up=n.cross(tangent).normalized()
        length=random.uniform(.013,.065)*(1.5 if form.kind=='Stone' else 1)
        wide=length*random.uniform(.22,.65)
        col=form.c[face[0]][:3]
        accent=rgb(random.choice(['A890A3','738899','C69B82','8A8B87','B9A786']))
        col=lerp(col,accent,random.uniform(.05,.30))
        col=mul(col,random.uniform(.67,1.4))
        tile=random.randrange(16);tx=tile%4;ty=tile//4
        start=len(skins.v)
        for q,uv in [(-tangent*length-up*wide,(0,0)),(tangent*length-up*wide,(1,0)),(tangent*length+up*wide,(1,1)),(-tangent*length+up*wide,(0,1))]:
            skins.vertex(tuple(p+q),col,((tx+.025+uv[0]*.95)/4,(ty+.025+uv[1]*.95)/4))
        skins.f.append((start,start+1,start+2,start+3))

# Fine suspended pollen threads continue the gold of the rooted foreground
# inflorescences into the water. These are small volumes at varied depths.
pollen=Shape('13 - Drifting golden pollen','Gold')
for j in range(2700):
    x=random.uniform(-10.6,-.8);y=.75-.12*(x+5)+random.gauss(0,.9)
    if y<-.9 or y>3.7:continue
    z=random.uniform(-6.5,.5);size=random.uniform(.006,.019)
    col=lerp(rgb('8B774F'),rgb('E5B777'),random.random()**1.7)
    pollen.orb((x,y,z),(size,size*random.uniform(.6,1.7),size*.7),col,j,3,5,.08)

materials={}
for shape in groups.values():
    if not shape.v:continue
    mesh=bpy.data.meshes.new(shape.name)
    # Unity Y-up -> Blender Z-up, with forward axis reversed for FBX.
    mesh.from_pydata([(-p[0],-p[2],p[1]) for p in shape.v],[],[tuple(reversed(f)) for f in shape.f])
    mesh.update()
    attr=mesh.color_attributes.new(name='Pigment',type='FLOAT_COLOR',domain='POINT')
    for i,c in enumerate(shape.c):attr.data[i].color=c
    uv=mesh.uv_layers.new(name='Botanical UV')
    for poly in mesh.polygons:
        poly.use_smooth=True
        for li in poly.loop_indices:uv.data[li].uv=shape.uv[mesh.loops[li].vertex_index]
    ob=bpy.data.objects.new(shape.name,mesh);scene.collection.objects.link(ob)
    if shape.kind not in materials:
        mat=bpy.data.materials.new('Atelier_'+shape.kind);mat.use_nodes=True
        nodes=mat.node_tree.nodes
        bsdf=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
        color_node=nodes.new('ShaderNodeVertexColor');color_node.layer_name='Pigment'
        mat.node_tree.links.new(color_node.outputs['Color'],bsdf.inputs['Base Color'])
        bsdf.inputs['Roughness'].default_value=.82
        materials[shape.kind]=mat
    ob.data.materials.append(materials[shape.kind])
    ob['Art direction']='E: symbolist underwater garden, authored silhouette and pigment.'
    ob['Unity vertex count']=len(shape.v)

# Store geometric ambient occlusion in vertex alpha. Six hemisphere probes at
# neighboring vertex clusters retain cavity depth without baking a camera image.
from mathutils.bvhtree import BVHTree
solid=[ob for ob in scene.objects if ob.type=='MESH' and not ob.name.startswith(('02','09','11','13','14'))]
verts=[];faces=[]
for ob in solid:
    offset=len(verts);verts.extend([v.co[:] for v in ob.data.vertices])
    faces.extend([tuple(offset+i for i in p.vertices) for p in ob.data.polygons])
bvh=BVHTree.FromPolygons(verts,faces,all_triangles=False)
for ob in solid:
    mesh=ob.data;colors=mesh.color_attributes['Pigment']
    reach=.8 if ob.name.startswith(('01','03','04')) else 1.3
    positions=[v.co.copy() for v in mesh.vertices]
    normals=[v.normal.copy().normalized() for v in mesh.vertices]
    rgba=np.empty(len(mesh.vertices)*4,dtype=np.float32)
    colors.data.foreach_get('color',rgba);rgba=rgba.reshape(-1,4)
    for start in range(0,len(mesh.vertices),4):
        normal=normals[start]
        if normal.length<.5:continue
        tangent=normal.cross(Vector((0,0,1)))
        if tangent.length<.1:tangent=normal.cross(Vector((0,1,0)))
        tangent.normalize();bitangent=normal.cross(tangent).normalized()
        origin=positions[start]+normal*.016;obscured=0
        for sample in range(6):
            angle=sample*2.399963;z=.22+.73*(sample+.5)/6;r=sqrt(1-z*z)
            ray=normal*z+tangent*(r*cos(angle))+bitangent*(r*sin(angle))
            loc,n,idx,dist=bvh.ray_cast(origin,ray,reach)
            if loc is not None:obscured+=(1-dist/reach)**.5
        ao=1-.76*obscured/6
        rgba[start:start+4,3]=ao
    # Smooth the sampled occlusion over connected vertices, never across
    # separate petals, so sampling groups cannot appear as triangular bands.
    edges=np.empty(len(mesh.edges)*2,dtype=np.int32);mesh.edges.foreach_get('vertices',edges);edges=edges.reshape(-1,2)
    degree=np.bincount(edges.ravel(),minlength=len(rgba)).astype(np.float32)
    for iteration in range(4):
        total=np.zeros(len(rgba),dtype=np.float32)
        np.add.at(total,edges[:,0],rgba[edges[:,1],3]);np.add.at(total,edges[:,1],rgba[edges[:,0],3])
        rgba[:,3]=(rgba[:,3]*2+total)/(degree+2)
    colors.data.foreach_set('color',rgba.ravel())
    print('OCCLUSION',ob.name,flush=True)

# Local origins make each named form practical to transform and sculpt in Blender.
# Ambient occlusion was evaluated in common authoring coordinates above.
for ob in scene.objects:
    if ob.type!='MESH':continue
    center=sum((Vector(corner) for corner in ob.bound_box),Vector())/8
    for vertex in ob.data.vertices:vertex.co-=center
    ob.location=center

scene.world.color=(.10,.14,.19)
camera_data=bpy.data.cameras.new('Fixed composition');camera=bpy.data.objects.new('Fixed composition',camera_data)
scene.collection.objects.link(camera);camera.location=(0,30,6)
direction=Vector((0,0,6))-camera.location;camera.rotation_euler=direction.to_track_quat('-Z','Y').to_euler()
camera_data.type='ORTHO';camera_data.ortho_scale=21.6;scene.camera=camera
for name,loc,power,size,color in [('Apricot opening',(3,-2,15),2100,12,(1,.76,.54)),('Violet fill',(-7,8,10),1500,11,(.55,.63,1))]:
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;data.color=color
    ob=bpy.data.objects.new(name,data);scene.collection.objects.link(ob);ob.location=loc
    ob.rotation_euler=(Vector((0,0,5))-ob.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=1536;scene.render.resolution_y=1024;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
bpy.ops.object.select_all(action='DESELECT')
for ob in scene.objects:
    if ob.type=='MESH':ob.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'E_SubmergedGarden.fbx'),use_selection=True,object_types={'MESH'},
    axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_space_transform=True,bake_space_transform=True,
    use_mesh_modifiers=True,mesh_smooth_type='FACE',use_tspace=False,add_leaf_bones=False,bake_anim=False,
    colors_type='LINEAR')
# Persist a useful authoring view after export. Unity supplies the transparent
# brush shader; hide those opaque preview quads in Blender's solid viewport.
for ob in scene.objects:
    ob.select_set(False)
    if ob.name.startswith(('02','11')):ob.hide_set(True)
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            space=area.spaces.active
            space.region_3d.view_perspective='CAMERA'
            space.region_3d.view_camera_zoom=15
            space.shading.color_type='VERTEX'
            space.overlay.show_overlays=False
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'E_SubmergedGarden.blend'),compress=True)
report={'blender':bpy.app.version_string,'objects':len(groups),'vertices':sum(len(s.v) for s in groups.values()),
    'faces':sum(len(s.f) for s in groups.values()),'source':str(SOURCE/'E_SubmergedGarden.blend'),
    'fbx':str(OUT/'E_SubmergedGarden.fbx'),'kinds':list(materials)}
(SOURCE/'authoring-report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('ATELIER_COMPLETE',json.dumps(report),flush=True)
