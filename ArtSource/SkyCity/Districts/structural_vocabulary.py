"""Construction rules for the infinite scene; the original fixed scene is untouched."""
from math import sin,cos,pi,sqrt

def install(g,lod):
    def arcade(p,r,h,n):
        # h is always the finished floor height, measured from the column feet.
        a,t=g['architecture'],g['trim'];ivory,trim=g['IVORY'],g['TRIM']
        chord=2*r*sin(pi/n);radius=min(.18,chord*.095)
        thickness=min(.22,h*.12);slab=min(.30,h*.12)
        opening=chord-radius*2.6;spring=h-slab-thickness-opening*.5
        assert spring>0,(p,r,h,n)
        a.cylinder((p[0],p[1]-.16,p[2]),r+.25,.16,ivory,n=[80,48,28][lod])
        for j in range(n):
            angle=j*2*pi/n;cx,cz=p[0]+r*cos(angle),p[2]+r*sin(angle)
            base=min(.13,spring*.16);capital=min(.15,spring*.16)
            t.cylinder((cx,p[1],cz),radius*1.65,base,trim,n=8)
            t.cylinder((cx,p[1]+base,cz),radius,spring-base-capital,ivory,top=radius*.91,n=8)
            t.cylinder((cx,p[1]+spring-capital,cz),radius*1.65,capital,trim,n=8)
            mid=angle+pi/n
            center=(p[0]+r*cos(pi/n)*cos(mid),p[1],p[2]+r*cos(pi/n)*sin(mid))
            g['arch'](a,center,opening,spring,thickness,max(.22,radius*2),mid+pi/2,ivory)
        a.cylinder((p[0],p[1]+h-slab,p[2]),r+.25,slab,ivory,n=[80,48,28][lod])
        # Edge molding belongs to the slab, never above it as a floating ring.
        t.cylinder((p[0],p[1]+h-slab*.65,p[2]),r+.31,slab*.45,trim,n=[80,48,28][lod])
    g['arcade']=arcade
    def tower(name,p,w,h):
        a=g['Mesh']('01 - Campanile '+name);t=g['trim'];ivory,trim=g['IVORY'],g['TRIM']
        body=h*.83;bell=h*.12;opening=w*.70;thick=min(.25,w*.10);slab=.25
        spring=bell-slab-thick-opening*.5
        assert spring>.44,(name,w,h)
        a.box((p[0],p[1]+body*.5,p[2]),(w,body,w),ivory)
        for level in range(4):
            y=level*h*.195;t.box((p[0],p[1]+y,p[2]),(w+.24,.16,w+.24),trim)
            if level:
                for face in range(4):
                    rot=face*pi/2
                    g['window'](g['frame'](p,0,y+.34,-w/2-.025,rot),w*.30,min(h*.13,1.18),rot)
        top=(p[0],p[1]+body,p[2])
        for face in range(4):
            rot=face*pi/2
            g['arch'](a,g['frame'](top,0,0,-w*.44,rot),opening,spring,thick,max(.22,w*.07),rot)
        for x,z in [(-1,-1),(-1,1),(1,1),(1,-1)]:
            g['column']((top[0]+x*w*.44,top[1],top[2]+z*w*.44),spring,max(.11,w*.065))
        t.box((top[0],top[1]+bell-slab*.5,top[2]),(w+.36,slab,w+.36),trim)
        g['roofs'].cylinder((top[0],top[1]+bell,top[2]),w*.72,w*1.3,g['COPPER'],top=.02,n=8)
        for j in range(8):
            angle=j*pi/4
            g['gold'].tube([(top[0]+cos(angle)*w*.725,top[1]+bell,top[2]+sin(angle)*w*.725),
                            (top[0],top[1]+bell+w*1.3,top[2])],.018,g['GOLD'],5,.7)
    g['tower']=tower
    original_villa=g['villa']
    def grounded_villa(p,w,d,h,seed):
        original_villa(p,w,d,h,seed)
        # Front loggias can project beyond a shared podium. Their local masonry
        # foundations continue down to the island instead of leaving feet in air.
        if p[1]>.3:
            a=g['architecture'];bottom=.10
            a.box((p[0],(bottom+p[1])*.5,p[2]),(w+.12,p[1]-bottom,d+.12),g['IVORY'])
            a.cylinder((p[0],bottom,p[2]-d*.60),w*.36+.25,p[1]-.06-bottom,g['IVORY'],n=[48,30,18][lod])
    g['villa']=grounded_villa

def curved_stair(g,center,r,top,lod):
    """A broad grounded stair following the terrace, with a landing at each end."""
    a,t=g['architecture'],g['trim'];base=.10;steps=32
    width=2.5;start=-pi*.97;end=-pi*.50
    for j in range(steps):
        angle=start+(end-start)*(j+.5)/steps;y=.25+(top-.25)*(j+1)/steps
        run=r*(end-start)/steps+.04
        p=(center[0]+r*cos(angle),(base+y)*.5,center[1]+r*sin(angle))
        a.box(p,(width,y-base,run),g['IVORY'],rot=angle)
        if j%2==0:
            edge=r+width*.44
            t.cylinder((center[0]+edge*cos(angle),y,center[1]+edge*sin(angle)),.065,.86,g['TRIM'],n=6)
    path=[(center[0]+(r+width*.44)*cos(start+(end-start)*j/steps),.25+(top-.25)*j/steps+.88,
           center[1]+(r+width*.44)*sin(start+(end-start)*j/steps)) for j in range(steps+1)]
    t.tube(path,.085,g['TRIM'],6,0)
    # The inner part of the top landing overlaps the circular upper floor.
    a.box((center[0],top-.15,center[1]-r+.55),(3.0,.30,2.9),g['IVORY'])

def straight_stair(g,x,front,top,lod):
    a,t=g['architecture'],g['trim'];base=.10;steps=max(8,int((top-.25)/.19)+1)
    tread=.34;start=front-steps*tread
    for j in range(steps):
        y=.25+(top-.25)*(j+1)/steps
        a.box((x,(base+y)*.5,start+(j+.5)*tread),(3.6,y-base,tread+.035),g['IVORY'])
    a.box((x,top-.14,front+.35),(3.8,.28,.9),g['IVORY'])

def retaining_arcades(g,lod,variant):
    # A bonded masonry retaining wall connects the terrace underside to the
    # crag. Arches are recessed into that wall, rather than hanging below it.
    a,t=g['architecture'],g['trim'];r=36.15;n=48;base=-3.5;floor=-.16
    a.cylinder((42,base,42),r-.15,floor-base,g['STONE'],n=[96,64,48][lod])
    chord=2*r*sin(pi/n);opening=chord-.64;thick=.26
    spring=floor-base-opening*.5-thick
    for j in range(n):
        angle=j*2*pi/n
        if variant in (2,5,6) and j%3!=0:continue
        if variant==7 and j%5 in (1,2):continue
        for edge in [angle-pi/n,angle+pi/n]:
            p=(42+r*cos(edge),base,42+r*sin(edge))
            t.box((p[0],(base+floor)*.5,p[2]),(.65,floor-base,.90),g['IVORY'],rot=edge+pi/2)
        g['arch'](a,(42+r*cos(pi/n)*cos(angle),base,42+r*cos(pi/n)*sin(angle)),opening,spring,thick,.80,angle+pi/2,g['IVORY'])
