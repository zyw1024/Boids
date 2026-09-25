"""The Hanging Gardens: an authored, physically modelled reference island.

Run with Blender --background --factory-startup --python build_hero.py.
Metres, Unity Y-up. No reference image, backdrop or camera projection is exported.
All openings, reveals, bridge spandrels, gardens and cliff undercuts are geometry.
"""
import sys, math, random, json, time
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
import geometry as g
from geometry import *
from mathutils.bvhtree import BVHTree
START=time.time()
random.seed(71309)
g.scene.name='Hanging Gardens - authored hero island'
g.IVORY=IVORY=rgb('DCD0B7');g.TRIM=TRIM=rgb('F0E2C6')
g.COPPER=COPPER=rgb('649B91');g.STONE=STONE=rgb('A0A39C')
g.DARK=DARK=rgb('555F5B')
terracotta=Mesh('14 - Hand laid terracotta roofs','Terracotta')
soil=Mesh('15 - Garden soil and moss','Earth')
pool=Mesh('13 - Sheltered reflecting basin','Pool')

def cornice_rect(p,w,d):
    for y,over,h in [(0,.10,.13),(.13,.18,.08),(.21,.29,.12),(.33,.21,.07)]:
        trim.box(add(p,(0,y+h/2,0)),(w+over*2,h,d+over*2),TRIM)

def pilaster(p,h,w=.32,rot=0):
    trim.box(add(p,(0,h*.5,0)),(w,h,.23),IVORY,rot)
    for y,s,t in [(0,1.55,.17),(.18,1.23,.10),(h-.20,1.30,.10),(h-.10,1.65,.16)]:
        trim.box(add(p,(0,y+t*.5,0)),(w*s,t,.33),TRIM,rot)

def handrail(a,b,height=.85):
    a,b=Vector(a),Vector(b);distance=(b-a).length;n=max(2,int(distance/.33))
    trim.tube([a+Vector((0,height,0)),b+Vector((0,height,0))],.065,TRIM,8,0)
    trim.tube([a+Vector((0,.12,0)),b+Vector((0,.12,0))],.065,IVORY,6,0)
    for j in range(n+1):
        q=a.lerp(b,j/n)
        for y,r,h in [(.12,.069,.09),(.21,.040,.12),(.33,.068,.15),(.48,.043,height-.51)]:
            trim.cylinder(q+Vector((0,y,0)),r,max(.03,h),TRIM,n=8)
    for q in (a,b):
        trim.box(q+Vector((0,.41,0)),(.24,.82,.24),IVORY)
        trim.box(q+Vector((0,.87,0)),(.34,.10,.34),TRIM)

def arch_wall(p,w,h,spring,depth,rot=0,opening=None):
    """Solid masonry wall pierced by a genuine arch; barrel intrados and back faces."""
    opening=opening or w*.70;r=opening/2;thick=.22
    for side in (-1,1):
        width=(w-opening)/2
        architecture.box(frame(p,side*(opening/2+width/2),h/2,0,rot),(width,h,depth),IVORY,rot)
    # Spandrel quads continue up from the curved intrados to the floor beam.
    for j in range(16):
        x0=-r+2*r*j/16;x1=-r+2*r*(j+1)/16
        y0=spring+sqrt(max(0,r*r-x0*x0));y1=spring+sqrt(max(0,r*r-x1*x1))
        c=mul(IVORY,.98+.03*(j%3)/2)
        for z in (-depth/2,depth/2):
            pts=[frame(p,x0,y0,z,rot),frame(p,x1,y1,z,rot),frame(p,x1,h,z,rot),frame(p,x0,h,z,rot)]
            architecture.quad(pts if z>0 else list(reversed(pts)),c)
        architecture.quad([frame(p,x0,y0,-depth/2,rot),frame(p,x0,y0,depth/2,rot),frame(p,x1,y1,depth/2,rot),frame(p,x1,y1,-depth/2,rot)],mul(IVORY,.87))
    arch(trim,frame(p,0,0,-depth/2-.035,rot),opening,spring,thick,.12,rot,TRIM,20)
    for s in (-1,1):pilaster(frame(p,s*(r+.12),0,-depth/2-.075,rot),spring,.24,rot)

def facade(p,w,h,bays,depth=.5,rot=0,storeys=1):
    """Front and rear surfaces enclose actual 0.45 m window reveals, not decals."""
    bay=w/bays;level=h/storeys
    for floor in range(storeys):
        for j in range(bays):
            q=frame(p,-w/2+bay*(j+.5),floor*level,0,rot)
            sill=.62;opening=bay*.44;spring=max(.70,level-.65-opening/2-sill)
            architecture.box(frame(q,0,sill/2,0,rot),(bay,sill,depth),IVORY,rot)
            arch_wall(frame(q,0,sill,0,rot),bay,level-sill,spring,depth,rot,opening)
            # Recessed oak shutters and glazing behind the reveal; crossed mullions.
            windows.box(frame(q,0,sill+(spring+opening*.40)/2,depth*.56,rot),(opening*.99,spring+opening*.40,.07),DARK,rot)
            trim.box(frame(q,0,sill+.015,-depth*.60,rot),(opening+.35,.12,.38),TRIM,rot)
            for sx in (-.22,.22):trim.box(frame(q,opening*sx,sill+spring*.54,depth*.39,rot),(.045,spring*1.08,.065),IVORY,rot)
            trim.box(frame(q,0,sill+spring*.43,depth*.38,rot),(opening,.045,.07),IVORY,rot)
            pilaster(frame(q,-bay/2+.04,.12,-depth*.51,rot),level-.24,.22,rot)
        trim.box(frame(p,0,(floor+1)*level-.13,-depth/2-.05,rot),(w+.12,.18,.26),TRIM,rot)

def palazzo(p,w,d,h,bays=5,storeys=2):
    # Walls built around the perimeter. Front/rear window holes remain physically open.
    for rot,span,count in [(0,w,bays),(pi,w,bays),(pi/2,d,max(2,round(bays*d/w))),(3*pi/2,d,max(2,round(bays*d/w)))]:
        q=frame(p,0,0,-(d if rot in (0,pi) else w)/2,rot)
        facade(q,span,h,count,.52,rot,storeys)
    for y in [0,h/storeys,h]:architecture.box(add(p,(0,y-.12,0)),(w,.24,d),IVORY)
    cornice_rect(add(p,(0,h,0)),w,d)

def tile_roof(p,w,d,h):
    # Gabled roofs, with individual curved cover tiles along each slope.
    c=rgb('A16F50')
    # Masonry gables close both ends and carry the pitched roof. Keep the
    # terracotta eaves projecting beyond the wall rather than sealing at the tip.
    for side in (-1,1):
        x=side*(w/2-.34);half_depth=d/2-.34
        y=h*(1-half_depth/(d/2))
        for dx in (-.12,.12):
            points=[add(p,(x+dx,0,-half_depth)),add(p,(x+dx,0,half_depth)),
                    add(p,(x+dx,y,half_depth)),add(p,(x+dx,h,0)),add(p,(x+dx,y,-half_depth))]
            ids=[architecture.vert(q,IVORY) for q in points]
            architecture.f.append(tuple(ids if dx>0 else reversed(ids)))
        for sign in (-1,1):
            architecture.quad([add(p,(x-.12,y,sign*half_depth)),add(p,(x+.12,y,sign*half_depth)),
                               add(p,(x+.12,h,0)),add(p,(x-.12,h,0))],IVORY)
    for side in (-1,1):
        pts=[add(p,(-w/2,0,side*d/2)),add(p,(w/2,0,side*d/2)),add(p,(w/2,h,0)),add(p,(-w/2,h,0))]
        terracotta.quad(pts,c)
        nx=int(w/.18);ny=max(3,int(sqrt(h*h+d*d/4)/.34))
        for j in range(nx+1):
            x=-w/2+w*j/nx
            for k in range(ny):
                t0=k/ny;t1=min(1,(k+1.09)/ny)
                a=add(p,(x,h*t0+.04,side*d/2*(1-t0)));b=add(p,(x,h*t1+.04,side*d/2*(1-t1)))
                terracotta.tube([a,b],.055,mix(c,rgb('D2A271'),random.random()*.6),5,0)
    terracotta.tube([add(p,(-w/2,h+.10,0)),add(p,(w/2,h+.10,0))],.12,rgb('CA956D'),10,0)

def loggia(p,w,d,h,bays=5,rail=True):
    bay=w/bays;opening=bay-.34;spring=h-.38-.20-opening/2
    assert spring>.6,(w,h,bays)
    architecture.box(add(p,(0,-.12,0)),(w+.6,.24,d+.3),IVORY)
    for side in (-1,1):
        for j in range(bays+1):column(add(p,(-w/2+j*bay,0,side*d/2)),spring,.13)
        for j in range(bays):
            q=add(p,(-w/2+(j+.5)*bay,0,side*d/2))
            arch_wall(q,bay,h-.22,spring,.28,0,opening)
    for x in (-w/2,w/2):
        arch_wall(add(p,(x,0,0)),d,h-.22,spring,.28,pi/2,min(d-.34,opening))
    architecture.box(add(p,(0,h-.13,0)),(w+.5,.26,d+.5),IVORY)
    cornice_rect(add(p,(0,h,0)),w+.25,d+.25)
    if rail:handrail(add(p,(-w/2,h+.4,-d/2-.12)),add(p,(w/2,h+.4,-d/2-.12)))

def terrace(p,w,d,foundation=2,rail=True):
    architecture.box(add(p,(0,-foundation/2,0)),(w,foundation,d),mix(IVORY,STONE,.22))
    cornice_rect(add(p,(0,-.40,0)),w,d)
    # Thin paving slabs catch sun at the joints without loud texture contrast.
    for j in range(int(w/1.25)):
        for k in range(int(d/1.1)):
            x=-w/2+(j+.5)*w/int(w/1.25);z=-d/2+(k+.5)*d/int(d/1.1)
            architecture.box(add(p,(x,.026,z)),(w/int(w/1.25)-.025,.045,d/int(d/1.1)-.025),mul(IVORY,random.uniform(.96,1.06)))
    if rail:
        handrail(add(p,(-w/2,0,-d/2)),add(p,(w/2,0,-d/2)))
        handrail(add(p,(-w/2,0,-d/2)),add(p,(-w/2,0,d/2)))

def stair(a,b,w=2.1):
    a,b=Vector(a),Vector(b);n=max(2,int(abs(b.y-a.y)/.17));yaw=math.atan2(b.z-a.z,b.x-a.x)-pi/2
    horizontal=Vector((b.x-a.x,0,b.z-a.z)).length;depth=horizontal/n
    for j in range(n):
        t=(j+.5)/n;p=a.lerp(b,t)
        # Solid riser joins the retaining wall below each stair.
        architecture.box((p.x,(a.y+p.y)/2-.1,p.z),(w,p.y-a.y+.2,depth+.025),IVORY,yaw)
        trim.box((p.x,p.y+.04,p.z),(w+.07,.09,depth+.06),TRIM,yaw)
    normal=Vector((cos(yaw),0,sin(yaw)))*w*.5
    for side in (-1,1):handrail(a+normal*side,b+normal*side)

def rock_island(p,rx,rz,depth,seed):
    rng=random.Random(seed);pts=[];rows=32;cols=112
    for j in range(rows+1):
        t=j/rows
        for k in range(cols+1):
            a=k/cols*2*pi;u=Vector((cos(a),t*3.2,sin(a)))
            profile=(1-t)**.54*(.93+.10*sin(t*13+sin(a*5)))
            outline=1+.13*sin(a*3+seed)+.075*cos(a*7)+.045*sin(a*13)
            ridge=1+.13*nz((cos(a)*5,t*2,sin(a)*5),1)+.04*nz((cos(a)*12,t*5,sin(a)*12),1)
            x=rx*profile*outline*ridge*cos(a)+t*rx*.23;z=rz*profile*outline*ridge*sin(a)
            y=-depth*t+(.3+.25*sin(a*4))*sin(pi*t)+.42*(1-t)*sin(a*5)
            weather=clip(.48+.55*nz((x*.6,y*.9,z*.6)))
            c=mix(rgb('646E6D'),rgb('C4C0A7'),weather)
            if t<.09:c=mix(c,rgb('6E8055'),.36)
            pts.append((add(p,(x,y,z)),c,(k/cols,t)))
    cliffs.grid(pts,cols,rows)
    # Deep projecting buttresses break the smooth conical silhouette.
    for j in range(23):
        a=j*2.399;rr=rng.uniform(.64,.88);h=rng.uniform(depth*.24,depth*.61)
        q=add(p,(cos(a)*rx*rr,-h*.78-1.1,sin(a)*rz*rr))
        cliffs.orb(q,(rng.uniform(.95,1.8),h*.63,rng.uniform(.75,1.5)),mix(STONE,rgb('B9B6A4'),rng.random()*.5),seed+j,16,15,.42)

def leafy_tree(p,h,seed,cypress=False):
    """Branching leaf sprays without solid spheres: visible air between crowns."""
    rng=random.Random(seed);trunk=[p,add(p,(.06,h*.35,0)),add(p,(-.05,h*.65,.06)),add(p,(.08,h*.92,0))]
    branches.tube(trunk,h*.031,WOOD,9,.88)
    small=h<1.0
    count=9 if small else (31 if cypress else 19)
    for j in range(count):
        t=.23+.71*j/count;a=j*2.399+seed
        reach=h*(.17*(sin(pi*t)**.75)*(1-t*.45) if cypress else rng.uniform(.20,.36)*sin(pi*t)**.7)
        root=Vector(p)+Vector((0,h*t,0));tip=root+Vector((cos(a)*reach,h*.13,sin(a)*reach))
        branches.tube([root,root.lerp(tip,.6)+Vector((0,.07*h,0)),tip],h*.012,WOOD,5,.9)
        for s in range(2 if small else 4):
            angle=a+s*1.8;end=tip+Vector((cos(angle)*h*.065,h*.04,sin(angle)*h*.065))
            branches.tube([tip,end],h*.004,WOOD,4,.8)
            for k in range(10 if small else (17 if cypress else 22)):
                phi=rng.random()*2*pi;z=rng.uniform(-1,1);rad=sqrt(1-z*z)
                normal=(cos(phi)*rad,z,sin(phi)*rad)
                radius=h*(.058 if cypress else .096)*rng.uniform(.35,1.1)
                q=end+Vector(normal)*radius
                color=mix(rgb('2B4D3C') if cypress else rgb('3D613E'),rgb('829557') if cypress else rgb('9CAC5E'),rng.random()*.90)
                leaf(tuple(q),h*rng.uniform(.017,.032),h*rng.uniform(.010,.020),normal,rng.random()*pi,color)

def shrubs(p,w,d,seed):
    rng=random.Random(seed)
    for j in range(5):
        q=add(p,(rng.uniform(-w/2,w/2),0,rng.uniform(-d/2,d/2)))
        leafy_tree(q,rng.uniform(.34,.65),seed*10+j)

def bed(p,w,d,seed,flowers=True):
    soil.box(add(p,(0,-.07,0)),(w,.14,d),rgb('58674A'))
    for side in (-1,1):trim.box(add(p,(0,.04,side*d/2)),(w+.10,.18,.12),IVORY)
    shrubs(p,w*.85,d*.85,seed)
    if flowers:g.flowers(add(p,(w*.28,.25,0)),seed)

def draped_ivy(p,length,seed,spread=.6):
    for j in range(3):g.ivy(add(p,((j-1)*spread*.65,.05,0)),length*(.7+.3*random.random()),seed+j,spread*.45)

def campanile(p,w,h,seed):
    body=h-3.4
    palazzo(p,w,w,body,1,3)
    loggia(add(p,(0,body+.43,0)),w+.1,w+.1,2.25,1,False)
    dome(add(p,(0,h-.18,0)),w*.76,w*.95,seed,False)
    gold.tube([add(p,(0,h+w*.85,0)),add(p,(0,h+w*.85+1,0))],.025,GOLD,8,.4)

def round_drum(p,r,h,n):
    # Polygonal bays with real windows and coupled pilasters under the main dome.
    span=2*r*sin(pi/n)
    for j in range(n):
        a=j*2*pi/n;rot=a+pi/2
        facade(add(p,(r*cos(pi/n)*cos(a),0,r*cos(pi/n)*sin(a))),span,h,1,.45,rot,1)
    cornice(add(p,(0,h,0)),r)
    architecture.cylinder(add(p,(0,-.13,0)),r+.15,.23,IVORY,n=96)

# MAIN ISLAND: asymmetric stepped massing. Gardens sit on occupied arcaded wings.
rock_island((-18,5.7,12),18.5,10.5,13.5,44)
terrace((-16.5,6.05,11),30,17,2.1)
loggia((-19.5,6.1,8.8),24,4.3,4.0,10)
terrace((-21.5,10.5,14.8),18.5,10.3,4.8,False)
loggia((-23,10.55,12.1),14.0,4.5,3.55,5)
# Occupied lower palace continues behind the loggia and supports the upper storeys.
palazzo((-23,10.5,16.7),10.8,7.8,4.05,5,1)
palazzo((-23,14.55,16.7),10.8,7.8,5.8,5,2)
tile_roof((-23,20.77,16.7),11.5,8.6,1.65)
round_drum((-23,21.0,17),4.0,3.1,16)
dome((-23,24.35,17),4.34,4.35,302)
campanile((-29.5,10.5,15.8),1.55,14.8,61)
campanile((-13.8,10.5,19.0),1.65,17.5,65)
campanile((-30.0,6.05,8.6),1.32,12.4,77)
campanile((-18.0,14.55,14.8),1.05,9.5,83)
terrace((-16.8,14.55,16.0),2.8,2.5,4.05,False)
palazzo((-12.5,6.1,17.2),6.0,7.5,8.5,3,3)
tile_roof((-12.5,15.02,17.2),6.7,8.2,1.45)

# Lower east wing creates a descending roof line toward the bridge.
palazzo((-5.7,6.1,13.2),6.6,6.8,6.0,3,2)
tile_roof((-5.7,12.52,13.2),7.3,7.5,1.38)
loggia((-5.5,6.1,8.0),6.0,3.1,3.6,3)
round_drum((-5.7,12.8,13.2),2.20,2.25,12)
dome((-5.7,15.32,13.2),2.45,2.32,351)
terrace((-32,6.1,15),6,7,1.5,False)
palazzo((-32,6.1,15),4.5,5.0,6.7,3,2)
tile_roof((-32,13.22,15),5.2,5.7,1.3)

# Cascading front garden, retaining arches, and a substantial reflecting basin.
rock_island((-10.7,3.2,1.3),6.6,5.2,11.5,701)
terrace((-11.5,3.55,.0),10.7,7.2,.75)
# Carved corbels transfer the projecting water terrace back into the cliff.
for x in (-16,-14.2,-12.4,-10.6,-8.8,-7):
    ends=[]
    for side in (-1,1):
        ends.append([(x+side*.18,3.12,-3.45),(x+side*.18,3.12,-1.1),(x+side*.18,.85,-1.1)])
    for face in (ends[0],list(reversed(ends[1]))):
        ids=[architecture.vert(p,IVORY) for p in face];architecture.f.append(tuple(ids))
    for j in range(3):architecture.quad([ends[0][j],ends[1][j],ends[1][(j+1)%3],ends[0][(j+1)%3]],IVORY)
loggia((-16.4,3.58,3.5),4.2,2.8,2.5,2)
stair((-5.6,3.6,2.4),(-5.6,6.08,6.0),2.0)
stair((-28.0,6.1,7.0),(-28.0,10.5,12.6),2.15)
stair((-16.0,10.5,10.3),(-16.0,14.55,15.4),1.75)
# Rectangular octagonal pool has believable depth, edge coping and two open spillways.
pc=(-10.8,3.91,-1.0);pw=7.5;pd=4.2
architecture.box(add(pc,(0,-.43,0)),(pw,.22,pd),rgb('729A87'))
pool.quad([add(pc,(-pw/2,0,-pd/2)),add(pc,(-pw/2,0,pd/2)),add(pc,(pw/2,0,pd/2)),add(pc,(pw/2,0,-pd/2))],rgb('85BDB3'))
pool.quad([(-11.425,3.91,-3.1),(-11.425,3.91,-3.68),(-10.175,3.91,-3.68),(-10.175,3.91,-3.1)],rgb('85BDB3'))
for x in (-11.51,-10.09):trim.box((x,3.88,-3.44),(.16,.28,.62),TRIM)
for side in (-1,1):
    trim.box(add(pc,(side*(pw/2+.12),-.12,0)),(.25,.56,pd+.5),TRIM)
    if side>0:trim.box(add(pc,(0,-.12,side*(pd/2+.12))),(pw+.5,.56,.25),TRIM)
    else:
        for s in (-1,1):trim.box(add(pc,(s*(pw/4+.34),-.12,-pd/2-.12)),(pw/2-.63,.56,.25),TRIM)

# Elegant bridge: deck continuous with BOTH terraces. Piers carry arch spring points.
start=Vector((-1.8,6.08,9.5));end=Vector((17.0,6.08,14.4));delta=end-start
length=Vector((delta.x,0,delta.z)).length;rot=math.atan2(delta.z,delta.x);n=4;span=length/n
bridge=Mesh('12 - Cut stone bridge spandrels')
for j in range(n):
    q=start.lerp(end,(j+.5)/n)
    arch_wall((q.x,.6,q.z),span,5.23,2.64,1.7,rot,span-.55)
for j in range(n+1):
    q=start.lerp(end,j/n)
    architecture.box((q.x,2.3,q.z),(.58,5.3,1.80),IVORY,rot)
    for y,w,h in [(-.36,1.0,.24),(-.65,.80,.25),(-.90,.58,.26)]:
        trim.box((q.x,y,q.z),(w,h,2.0),TRIM,rot)
    draped_ivy((q.x,5.9,q.z-.90),2.4,910+j,.4)
architecture.box(tuple((start+end)/2+Vector((0,-.13,0))),(length+.65,.27,2.06),TRIM,rot)
for side in (-1,1):
    offset=Vector((-sin(rot),0,cos(rot)))*side*1.0
    handrail(start+offset,end+offset,.83)

# Companion island: open rotunda and a winding approach, deliberately below main dome.
rock_island((20.2,5.8,15.3),5.0,4.5,8.3,527)
terrace((20.2,6.08,15.3),7.6,6.8,1.5)
stair((17.5,6.1,12.9),(20.1,8.0,15.2),2.15)
terrace((20.9,8.0,16.9),5.4,4.4,1.9,False)
g.arcade((20.9,8.0,16.9),2.35,3.7,10)
dome((20.9,12.13,16.9),2.70,2.6,204)

# Cypress silhouettes frame openings. Broadleaf canopies have branch-level air gaps.
for i,(x,y,z,h) in enumerate([(-30.7,6.2,5,6.0),(-29,10.55,9.3,5.7),(-27.9,14.55,10.5,4.6),(-18.5,14.55,11.3,4.2),(-8.7,6.2,6.2,5.8),(-2.2,6.2,11.7,5.4),(-21,10.55,6.9,4.1),(-1.8,6.2,18.5,4.8),(-33,6.2,11.6,6.1),(18.0,6.2,16.2,4.3),(23.3,6.2,15.4,4.8),(22.7,8.1,18.6,3.9)]):
    architecture.cylinder((x,y-.1,z),.42,.3,IVORY,n=20);soil.cylinder((x,y+.19,z),.36,.02,rgb('526043'),n=20)
    leafy_tree((x,y,z),h,3000+i,True)
for i,(x,y,z,h) in enumerate([(-25.1,6.2,7.0,2.7),(-22.4,6.2,3.4,3.1),(-18.5,6.2,3.5,3.0),(-12.8,6.2,5.6,2.5),(-15.0,10.55,8.3,2.8),(-24.8,10.55,7.9,2.6),(-10.8,10.55,10.4,2.4),(-2.7,6.2,7.3,2.9),(-16.0,3.6,.0,2.5),(-6.8,3.6,.8,2.2),(18.2,6.2,12.5,2.5),(23.2,6.2,13.0,2.2),(-29,6.2,3.4,3.2)]):
    architecture.cylinder((x,y-.1,z),.5,.34,IVORY,n=20);soil.cylinder((x,y+.23,z),.44,.02,rgb('526043'),n=20)
    leafy_tree((x,y,z),h,3200+i)
for i in range(16):
    x=-30+i*1.85
    bed((x,6.17,3.0),1.55,.80,4100+i)
    draped_ivy((x,6.15,2.48),random.uniform(2.0,6.0),5100+i,.70)
for i in range(12):
    x=-30+i*1.9;bed((x,10.54,9.5),1.65,.65,4300+i)
    draped_ivy((x,10.5,8.86),random.uniform(1.2,3.6),5300+i,.55)
for i in range(7):
    x=-16+i*1.55
    if -13.5<x<-8:continue
    bed((x,3.65,-.2),1.15,.7,4700+i)
    draped_ivy((x,3.6,-.62),random.uniform(2.4,5.5),5700+i,.6)
for i in range(9):
    a=i*2*pi/9
    if i in (4,5):continue
    q=(20.2+3.7*cos(a),6.18,15.3+3.25*sin(a))
    bed(q,1.0,.75,4800+i);draped_ivy(add(q,(0,-.15,-.35)),2.8,5900+i,.6)
for i,x in enumerate((-16.2,-14.6,-8.0,-6.7)):
    draped_ivy((x,3.58,-3.6),2.2+i*.17,6500+i,.44)

# Continuous ribbons break into smaller strands; spray is added in Unity.
for p,l,w,s in [((-23,6.1,2.45),16,1.6,7),((-10.8,3.91,-3.68),17.5,1.25,9),((23.3,6.1,12.0),8.5,.55,11)]:
    waterfall(p,l,w,s)
    for j in range(4):waterfall(add(p,((j-1.5)*w*.17,-.35,-.055)),l*.86,w*.15,s+j)

# Pennants remain subdivided for travelling wind folds.
for idx,p in enumerate([(-29.5,26.4,15.8),(-13.8,29.4,19), (20.9,16.4,16.9)]):
    gold.cylinder(p,.025,1.6,GOLD,n=8);pts=[]
    for j in range(7):
        for k in range(29):
            u=k/28;v=j/6
            pts.append((add(p,(u*1.7,1.3-v*.56*(1-u*.5),.1*u*sin(u*8))),rgb('B67553'),(u,v)))
    flags.grid(pts,28,6)

print('Building Blender objects',sum(len(m.v) for m in groups.values()),flush=True)
objects=create_objects(list(groups.values()))
# Bevel architectural edges to produce narrow physically lit highlights.
for ob in objects:
    if ob['Surface family'] in ('Stone','Terracotta','Dark'):
        bpy.context.view_layer.objects.active=ob
        bevel=ob.modifiers.new('Stone arris 18 mm','BEVEL');bevel.width=.018;bevel.segments=2;bevel.limit_method='ANGLE';bevel.angle_limit=.7
        bpy.ops.object.modifier_apply(modifier=bevel.name)
        normal=ob.modifiers.new('Area weighted masonry normals','WEIGHTED_NORMAL');normal.keep_sharp=True;normal.weight=40
        bpy.ops.object.modifier_apply(modifier=normal.name)

# Geometric ambient occlusion in vertex alpha, evaluated per vertex on masonry.
# Foliage is excluded from the occluder BVH to avoid opaque dark balls around crowns.
verts=[];faces=[]
for ob in objects:
    if ob['Surface family'] not in ('Stone','Rock','Copper','Terracotta'):continue
    offset=len(verts);verts.extend(v.co[:] for v in ob.data.vertices)
    faces.extend(tuple(offset+i for i in p.vertices) for p in ob.data.polygons)
bvh=BVHTree.FromPolygons(verts,faces,all_triangles=False)
for ob in objects:
    if ob['Surface family'] in ('Water','Pool','Cloud','Cloth','Brass'):continue
    me=ob.data;colors=me.color_attributes.get('Pigment')
    if not colors:continue
    rgba=np.empty(len(me.vertices)*4,dtype=np.float32);colors.data.foreach_get('color',rgba);rgba=rgba.reshape(-1,4)
    samples=6 if ob['Surface family'] in ('Stone','Rock','Dark') else 3
    for i,v in enumerate(me.vertices):
        normal=v.normal.copy().normalized()
        if normal.length<.5:continue
        axis=normal.cross(Vector((0,0,1)))
        if axis.length<.1:axis=normal.cross(Vector((0,1,0)))
        axis.normalize();other=normal.cross(axis);shade=0
        for j in range(samples):
            z=.28+.65*(j+.5)/samples;a=j*2.39996;rr=sqrt(1-z*z)
            ray=normal*z+axis*rr*cos(a)+other*rr*sin(a)
            hit,_,_,distance=bvh.ray_cast(v.co+normal*.027,ray,2.0)
            if hit is not None:shade+=(1-distance/2)**.7
        rgba[i,3]=max(.30,1-.7*shade/samples)
    colors.data.foreach_set('color',rgba.ravel());print('Occlusion:',ob.name,flush=True)

report={'design':'Hanging Gardens','seed':71309,'objects':[]}
for ob in objects:
    center=sum((Vector(c) for c in ob.bound_box),Vector())/8
    for v in ob.data.vertices:v.co-=center
    ob.location=center
    report['objects'].append({'name':ob.name,'vertices':len(ob.data.vertices),'triangles':sum(len(p.vertices)-2 for p in ob.data.polygons),'family':ob['Surface family']})
export(OUT/'HangingGardens.fbx',objects)
# LODs use real simplified meshes, preserving silhouette and vertex pigment.
for lod,ratio in [(1,.38),(2,.12)]:
    reduced=[]
    for source in objects:
        if source['Surface family'] in ('Water','Pool','Cloth'):continue
        ob=source.copy();ob.data=source.data.copy();scene.collection.objects.link(ob);ob.name=source.name+' - distance '+str(lod)
        bpy.context.view_layer.objects.active=ob
        mod=ob.modifiers.new('Distance silhouette','DECIMATE');mod.ratio=ratio
        bpy.ops.object.modifier_apply(modifier=mod.name);reduced.append(ob)
    export(OUT/('HangingGardens_LOD%d.fbx'%lod),reduced)
    for ob in reduced:bpy.data.objects.remove(ob,do_unlink=True)
report['seconds']=round(time.time()-START,2)
(OUT/'ModelReport.json').write_text(json.dumps(report,indent=2),encoding='utf8')
# The editable Blender source lives beside its deterministic authoring scripts, outside Assets.
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'HangingGardens.blend'),compress=True)
print('HERO COMPLETE',json.dumps(report),flush=True)
