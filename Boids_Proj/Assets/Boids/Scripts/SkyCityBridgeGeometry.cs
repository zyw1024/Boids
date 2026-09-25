using System;
using System.Collections.Generic;
using UnityEngine;

namespace Boids.Art.Infinite
{
    /// <summary>Covered galleries above the deck; a continuous bearing arch joins island abutments below.</summary>
    public static class SkyCityBridgeGeometry
    {
        public struct Span
        {
            public Vector3 island, seam, forward, right;
            public float length, rise, inset;
            public int repeats;
        }
        public static Span Describe(SkyCityWfc.Result layout,int cell)
        {
            int x=cell%8,z=cell/8;bool axisX=x==0||x==7,positive=x==7||z==7;
            int sign=positive?1:-1;float gate=(axisX?z:x)*12;
            var neighbor=new SkyCityWfc.Coord(layout.coord.x+(axisX?sign:0),layout.coord.z+(axisX?0:sign));
            int other=SkyCityWfc.Composition(neighbor,layout.seed);
            Vector2 shape=SkyCityModuleData.DistrictScale(layout.composition),next=SkyCityModuleData.DistrictScale(other);
            float reach=Mathf.Sqrt(34*34-(gate-42)*(gate-42));
            Vector3 inner=axisX?new Vector3(42+sign*reach*shape.x,0,42+(gate-42)*shape.y):new Vector3(42+(gate-42)*shape.x,0,42+sign*reach*shape.y);
            Vector3 far=axisX?new Vector3(sign*168+42-sign*reach*next.x,0,42+(gate-42)*next.y):new Vector3(42+(gate-42)*next.x,0,sign*168+42-sign*reach*next.y);
            Vector3 seam=axisX?new Vector3(positive?126:-42,0,gate):new Vector3(gate,0,positive?126:-42);
            float length=Vector3.Distance(inner,seam),otherLength=Vector3.Distance(far,seam);
            inner.y=SkyCityWfc.Elevation(layout.composition)+.25f;
            seam.y=Mathf.Lerp(inner.y,SkyCityWfc.Elevation(other)+.25f,length/(length+otherLength));
            Vector3 forward=Vector3.ProjectOnPlane(seam-inner,Vector3.up).normalized;
            float[] depth={32,47,28,51,67,22,31,55};
            // Keep cross sections parallel to the canonical seam. Different island
            // widths then cannot rotate opposite deck edges away from each other.
            return new Span{island=inner,seam=seam,forward=forward,right=axisX?new Vector3(0,0,-sign):new Vector3(sign,0,0),length=length,
                rise=Mathf.Min(14,depth[layout.composition]*.40f),inset=14*(axisX?shape.x:shape.y),repeats=Mathf.CeilToInt(length/12)};
        }
        public static Vector3 Place(Span span,Vector3 local,int repeat)
        {
            float t=(repeat+(local.z+6)/12)/span.repeats;
            return Vector3.LerpUnclamped(span.island,span.seam,t)+span.right*local.x+Vector3.up*(local.y-.25f);
        }
        public static SkyCityModuleData.Geometry Build(SkyCityWfc.Result layout,int cell,int lod,SkyCityModuleData.Geometry bay)
        {
            var span=Describe(layout,cell);
            float slope=(span.seam.y-span.island.y)/span.length;
            int segments=lod==0?28:lod==1?18:12;
            int steps=Mathf.Abs(slope)>.10f?Mathf.CeilToInt(Mathf.Abs(span.seam.y-span.island.y)/.17f):0;
            int walls=segments*4+steps+1;
            var b=new Builder(span.repeats*bay.vertices.Length+walls*24,span.repeats*bay.indices.Length+walls*36);
            Vector3 along=(span.seam-span.island)/(12*span.repeats);
            Vector3 normalX=Vector3.Cross(Vector3.up,along),normalY=Vector3.Cross(along,span.right),normalZ=Vector3.Cross(span.right,Vector3.up);
            for(int repeat=0;repeat<span.repeats;repeat++)
            {
                int start=b.v.Count;
                for(int i=0;i<bay.vertices.Length;i++)
                {
                    Vector3 n=bay.normals[i];b.v.Add(Place(span,bay.vertices[i],repeat));
                    b.n.Add((normalX*n.x+normalY*n.y+normalZ*n.z).normalized);
                    b.c.Add(bay.colors[i]);b.uv.Add(bay.uv[i]);
                }
                foreach(int i in bay.indices)b.t.Add(start+i);
            }
            // Solid spandrels transfer the deck load into two continuous arch ribs.
            // Both independently generated halves meet at the exact same crown.
            Color32 stone=new Color(.66f,.60f,.46f,0),trim=new Color(.86f,.78f,.62f,0);
            // Steeper connections are stairs, not implausibly steep smooth ramps.
            // Treads sit on the continuous bearing deck and keep the same endpoints.
            if(Mathf.Abs(slope)>.10f)
            {
                for(int j=0;j<steps;j++)
                {
                    Vector3 a=Vector3.Lerp(span.island,span.seam,(float)j/steps),c=Vector3.Lerp(span.island,span.seam,(float)(j+1)/steps);
                    float top=Mathf.Max(a.y,c.y)+.002f;
                    b.Wall(a,c,span.right,0,3.58f,a.y-top,c.y-top,.025f,.025f,trim);
                }
            }
            for(int side=-1;side<=1;side+=2)for(int j=0;j<segments;j++)
            {
                float u=(float)j/segments,v=(float)(j+1)/segments;
                Vector3 a=Vector3.Lerp(span.island,span.seam,u),c=Vector3.Lerp(span.island,span.seam,v);
                float da=.70f+span.rise*(1-u)*(1-u),dc=.70f+span.rise*(1-v)*(1-v);
                b.Wall(a,c,span.right,side*1.40f,.45f,.70f,.70f,da+.85f,dc+.85f,stone);
                b.Wall(a,c,span.right,side*1.40f,.56f,da,dc,da+.90f,dc+.90f,trim);
            }
            // The springings extend INTO the rock, instead of ending in mid-air.
            Vector3 foot=span.island-span.forward*span.inset;
            b.Wall(foot,span.island+span.forward*.5f,span.right,0,3.65f,.70f,.70f,span.rise+2,span.rise+1.6f,stone);
            var result=b.Finish();
            int x=cell%8,z=cell/8,direction=z==7?0:x==7?1:z==0?2:3;
            // A bridge can be hidden without separate renderers or extra uploads
            // until BOTH of its island endpoints have entered the resident set.
            for(int i=0;i<result.uv.Length;i++)result.uv[i]=new Vector2(-10-direction,0);
            return result;
        }
        sealed class Builder
        {
            public readonly List<Vector3> v,n;
            public readonly List<Color32> c;
            public readonly List<Vector2> uv;public readonly List<int> t;
            public Builder(int vertices,int indices)
            {v=new List<Vector3>(vertices);n=new List<Vector3>(vertices);c=new List<Color32>(vertices);uv=new List<Vector2>(vertices);t=new List<int>(indices);}
            void Face(Vector3 a,Vector3 b,Vector3 d,Vector3 e,Color32 color)
            {
                int start=v.Count;Vector3 normal=Vector3.Cross(b-a,d-a).normalized;
                v.Add(a);v.Add(b);v.Add(d);v.Add(e);
                for(int i=0;i<4;i++){n.Add(normal);c.Add(color);uv.Add(Vector2.zero);}
                t.Add(start);t.Add(start+1);t.Add(start+2);t.Add(start);t.Add(start+2);t.Add(start+3);
            }
            public void Wall(Vector3 a,Vector3 b,Vector3 right,float x,float width,float upperA,float upperB,float lowerA,float lowerB,Color32 color)
            {
                Vector3 al=a+right*(x-width*.5f),ar=a+right*(x+width*.5f),bl=b+right*(x-width*.5f),br=b+right*(x+width*.5f);
                Vector3 au=al-Vector3.up*upperA,av=ar-Vector3.up*upperA,bu=bl-Vector3.up*upperB,bv=br-Vector3.up*upperB;
                Vector3 ad=al-Vector3.up*lowerA,ae=ar-Vector3.up*lowerA,bd=bl-Vector3.up*lowerB,be=br-Vector3.up*lowerB;
                Face(au,bu,bv,av,color);Face(ad,ae,be,bd,color);
                Face(au,ad,bd,bu,color);Face(av,bv,be,ae,color);
                Face(au,av,ae,ad,color);Face(bu,bd,be,bv,color);
            }
            public SkyCityModuleData.Geometry Finish(){return new SkyCityModuleData.Geometry{vertices=v.ToArray(),normals=n.ToArray(),colors=c.ToArray(),uv=uv.ToArray(),indices=t.ToArray()};}
        }
    }
}
