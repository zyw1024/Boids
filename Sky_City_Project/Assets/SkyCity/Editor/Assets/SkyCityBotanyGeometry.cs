using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Offline mesh authoring. Parametric production: F(l,r,d) -> curved F + [F(.73l,.58r,d+1)] branches.</summary>
public static class SkyCityBotanyGeometry
{
    public sealed class Surface
    {
        public readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>(),roots=new List<Vector3>(),flowers=new List<Vector3>();
        public readonly List<Vector2> uv=new List<Vector2>(),cells=new List<Vector2>();
        public readonly List<Color> colors=new List<Color>();
        public readonly List<int> triangles=new List<int>();
        public Vector2 cell;
        public Vector3 root,flower;
        public int Add(Vector3 p,Vector3 n,Color c,Vector2 t)
        {int i=vertices.Count;vertices.Add(p);normals.Add(n.normalized);var linear=c.linear;linear.a=c.a;colors.Add(linear);uv.Add(t);cells.Add(cell);roots.Add(root);flowers.Add(flower);return i;}
        public void Tri(int a,int b,int c){triangles.Add(a);triangles.Add(b);triangles.Add(c);}
        public void Tube(Vector3[] path,float radius,Color color,int sides=8)
        {
            int start=vertices.Count;
            var tangent=Vector3.Cross((path[1]-path[0]).normalized,Vector3.right).normalized;
            if(tangent.sqrMagnitude<.01f)tangent=Vector3.forward;
            for(int j=0;j<path.Length;j++)
            {
                var direction=(path[Mathf.Min(j+1,path.Length-1)]-path[Mathf.Max(j-1,0)]).normalized;
                // Parallel transport avoids abrupt ring rotation when a curved branch crosses a reference axis.
                tangent=Vector3.ProjectOnPlane(tangent,direction).normalized;
                var bitangent=Vector3.Cross(direction,tangent);float taper=Mathf.Lerp(1,.52f,j/(float)(path.Length-1));
                for(int k=0;k<sides;k++)
                {float a=k*Mathf.PI*2/sides;var n=tangent*Mathf.Cos(a)+bitangent*Mathf.Sin(a);Add(path[j]+n*radius*taper,n,color,new Vector2(j/(float)(path.Length-1),k/(float)sides));}
                if(j==0)continue;
                for(int k=0;k<sides;k++){int a=start+(j-1)*sides+k,b=start+(j-1)*sides+(k+1)%sides,c=start+j*sides+k,d=start+j*sides+(k+1)%sides;Tri(a,b,c);Tri(b,d,c);}
            }
        }
        public void Leaf(Vector3 origin,Vector3 direction,Vector3 normal,float length,float width,Color color)
        {
            direction.Normalize();var side=Vector3.Cross(normal,direction).normalized;normal=Vector3.Cross(direction,side).normalized;
            int center=Add(origin+direction*length*.48f+normal*width*.24f,normal,color,new Vector2(.48f,.5f));
            Vector2[] outline={new Vector2(0,0),new Vector2(.34f,-1),new Vector2(.72f,-.75f),new Vector2(1,0),new Vector2(.72f,.75f),new Vector2(.34f,1)};
            int first=vertices.Count;
            foreach(var q in outline)Add(origin+direction*(q.x*length)+side*(q.y*width)+normal*(Mathf.Sin(q.x*Mathf.PI)*width*.10f),normal+side*q.y*.22f,color,new Vector2(q.x,q.y*.5f+.5f));
            for(int k=0;k<6;k++)Tri(center,first+k,first+(k+1)%6);
        }
        public Mesh Mesh(string name)
        {
            var mesh=new Mesh{name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetColors(colors);mesh.SetUVs(0,uv);mesh.SetUVs(1,cells);mesh.SetUVs(2,roots);mesh.SetUVs(3,flowers);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            var b=mesh.bounds;b.Expand(.5f);mesh.bounds=b;return mesh;
        }
    }
    static float Next(System.Random random,float min,float max){return Mathf.Lerp(min,max,(float)random.NextDouble());}
    public static Mesh Tree(int depth,int detail,out int branches)
    {
        var surface=new Surface();int count=0;Branch(surface,Vector3.zero,new Vector3(.12f,1,-.13f).normalized,3.05f,.34f,0,depth,71309,detail,ref count);
        for(int i=0;i<7;i++)
        {
            float a=i*2*Mathf.PI/7;var end=new Vector3(Mathf.Cos(a)*1.05f,-.04f,Mathf.Sin(a)*.92f);
            surface.cell=Vector2.zero;surface.root=Vector3.zero;
            surface.Tube(new[]{new Vector3(0,.3f,0),end*.45f+Vector3.up*.12f,end},.13f,new Color(.32f,.29f,.21f,0),detail==0?8:5);
        }
        branches=count;return surface.Mesh("Wind tree - generation "+depth+" LOD "+detail);
    }
    static void Branch(Surface s,Vector3 p,Vector3 direction,float length,float radius,int level,int depth,int seed,int detail,ref int count)
    {
        count++;var rng=new System.Random(seed);int segments=detail==0?7:4;var path=new Vector3[segments];
        for(int j=0;j<segments;j++)
        {
            float t=j/(float)(segments-1);var q=p+direction*length*t+new Vector3(.18f,-.035f,-.10f)*length*t*t;
            // Keep the crown on the open side of the terrace, clear of the palace facade.
            q.z=Mathf.Min(q.z,1.0f);path[j]=q;
        }
        s.cell=new Vector2(level,0);s.root=p;
        s.Tube(path,radius,new Color(.35f+Next(rng,-.03f,.04f),.32f,.24f,0),detail==0?10:6);
        if(level>=depth-1)
        {
            int sprays=level==depth?(detail==0?16:7):(detail==0?8:4);
            for(int k=0;k<sprays;k++)
            {
                float t=Next(rng,.28f,1),angle=Next(rng,0,Mathf.PI*2);var stem=Vector3.Lerp(path[segments/2],path[segments-1],t);
                var axis=new Vector3(Mathf.Cos(angle),Next(rng,.12f,.48f),Mathf.Sin(angle)).normalized;
                float sprayLength=Next(rng,.32f,.72f);
                Color green=Color.Lerp(new Color(.17f,.30f,.19f,.6f),new Color(.48f,.57f,.29f,.6f),Next(rng,0,1));
                s.cell=new Vector2(Mathf.Min(depth,level+.5f),0);s.root=p;
                for(int pair=0;pair<(detail==0?4:2);pair++)for(int sign=-1;sign<=1;sign+=2)
                {
                    var start=stem+axis*(pair*.18f*sprayLength);
                    var leafDirection=(axis*.45f+Vector3.Cross(Vector3.up,axis)*sign*.75f+Vector3.up*Next(rng,-.32f,.6f)).normalized;
                    s.Leaf(start,leafDirection,new Vector3(Next(rng,-.45f,.45f),1,Next(rng,-.45f,.45f)),Next(rng,.21f,.37f),Next(rng,.055f,.092f),green);
                }
                s.Leaf(stem+axis*sprayLength*.65f,axis,Vector3.up,.31f,.085f,green);
                if(k%6==0)
                {
                    var bloom=stem+axis*.3f+Vector3.up*.16f;s.flower=bloom;
                    for(int petal=0;petal<5;petal++){float a=petal*2*Mathf.PI/5;s.Leaf(bloom,new Vector3(Mathf.Cos(a),.2f,Mathf.Sin(a)),Vector3.up,.12f,.065f,new Color(.78f,.72f,.86f,1));}
                }
            }
        }
        if(level>=depth)return;
        int children=level<2?3:2;
        for(int k=0;k<children;k++)
        {
            float angle=Next(rng,0,.5f)+k*Mathf.PI*2/children+level*1.71f;
            var lateral=new Vector3(Mathf.Cos(angle),Next(rng,.15f,.45f),Mathf.Sin(angle));
            var heading=(direction*.46f+lateral*.74f+Vector3.up*.18f+new Vector3(.10f,0,-.12f)).normalized;
            var attachment=Vector3.Lerp(path[segments/2],path[segments-1],.48f+.52f*k/(children-1));
            Branch(s,attachment,heading,length*Next(rng,.69f,.79f),radius*.58f,level+1,depth,unchecked(seed*31+k*713+17),detail,ref count);
        }
    }
    public static Mesh Flowers(SkyCity.Runtime.SkyCityLivingGarden garden,int detail)
    {
        var s=new Surface();var rng=new System.Random(garden.seed);
        for(int y=0;y<garden.rows;y++)for(int x=0;x<garden.columns;x++)
        {
            if(!garden.SoilAt(x,y))continue;
            for(int plant=0;plant<(detail==0?2:1);plant++)
            {
                var p=new Vector3(((x+Next(rng,.15f,.85f))/garden.columns-.5f)*garden.size.x,0,((y+Next(rng,.15f,.85f))/garden.rows-.5f)*garden.size.y);
                s.cell=new Vector2((x+.5f)/garden.columns,(y+.5f)/garden.rows);s.root=p;
                float height=Next(rng,.42f,.79f);var top=p+new Vector3(Next(rng,-.07f,.07f),height,Next(rng,-.06f,.06f));s.flower=top;
                var green=Color.Lerp(new Color(.17f,.29f,.17f,.6f),new Color(.37f,.47f,.23f,.6f),Next(rng,0,1));
                s.Tube(new[]{p,Vector3.Lerp(p,top,.5f),top},.009f,new Color(.2f,.32f,.18f,0),4);
                for(int leaf=0;leaf<3;leaf++)
                {float a=Next(rng,0,6.28f);s.Leaf(Vector3.Lerp(p,top,.15f+leaf*.21f),new Vector3(Mathf.Cos(a),.35f,Mathf.Sin(a)),Vector3.up,.16f,.046f,green);}
                var purple=Color.Lerp(new Color(.39f,.28f,.60f,1),new Color(.84f,.74f,.85f,1),Next(rng,0,1));
                if((x+plant)%7==0)purple=new Color(.95f,.89f,.70f,1);
                for(int petal=0;petal<5;petal++)
                {float a=petal*Mathf.PI*2/5+Next(rng,-.12f,.12f);s.Leaf(top,new Vector3(Mathf.Cos(a),.28f,Mathf.Sin(a)),Vector3.up,.10f,.063f,purple);}
                s.Leaf(top+Vector3.up*.012f,Vector3.right,Vector3.up,.025f,.025f,new Color(.90f,.63f,.19f,1));
            }
        }
        return s.Mesh(garden.name+" flowers LOD "+detail);
    }
    public static Mesh Cells(SkyCity.Runtime.SkyCityLivingGarden garden)
    {
        var s=new Surface();
        for(int y=0;y<garden.rows;y++)for(int x=0;x<garden.columns;x++)
        {
            if(!garden.SoilAt(x,y))continue;s.cell=new Vector2((x+.5f)/garden.columns,(y+.5f)/garden.rows);
            int start=s.vertices.Count;
            foreach(var corner in new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)})
                s.Add(new Vector3(((x+corner.x)/garden.columns-.5f)*garden.size.x,.74f,((y+corner.y)/garden.rows-.5f)*garden.size.y),Vector3.up,Color.white,corner);
            s.Tri(start,start+1,start+2);s.Tri(start,start+2,start+3);
        }
        return s.Mesh(garden.name+" teaching cells");
    }
}
