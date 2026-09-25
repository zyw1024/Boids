using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Boids.Art.Infinite
{
    /// <summary>Immutable shared vocabulary and worker-side mesh assembly.</summary>
    public sealed class SkyCityModuleData
    {
        public sealed class Geometry
        {
            public Vector3[] vertices, normals;
            public Color32[] colors;
            public Vector2[] uv;
            public int[] indices;
            public long Bytes { get { return vertices.Length * 36L + indices.Length * 4L; } }
        }
        public sealed class Payload
        {
            public SkyCityWfc.Result layout;
            public Geometry[] opaque = new Geometry[4];
            public Geometry water, cascades;
            public int lod;
            public double workerMilliseconds;
            public long Bytes { get { long n = water.Bytes + cascades.Bytes; foreach (var g in opaque) n += g.Bytes; return n; } }
        }
        public readonly Geometry[,,] modules = new Geometry[SkyCityWfc.ModuleCount,3,3];
        public readonly Geometry[,,] districts = new Geometry[8,3,6];
        public long Bytes { get; private set; }
        public static SkyCityModuleData Read(byte[] compressed, CancellationToken cancellation)
        {
            var data = new SkyCityModuleData();
            using (var memory = new MemoryStream(compressed, false))
            using (var gzip = new GZipStream(memory, CompressionMode.Decompress))
            using (var reader = new BinaryReader(gzip))
            {
                if (reader.ReadInt32() != 0x534B594D || reader.ReadInt32() != 3 || reader.ReadInt32() != SkyCityWfc.ModuleCount || reader.ReadInt32() != 8)
                    throw new InvalidDataException("Unsupported Sky City module library.");
                for (int module = 0; module < SkyCityWfc.ModuleCount + 8; module++)
                    for (int lod = 0; lod < 3; lod++) for (int part = 0; part < 3; part++)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        int count = reader.ReadInt32(), indices = reader.ReadInt32();
                        if (count < 0 || count > 800000 || indices < 0 || indices > 2400000) throw new InvalidDataException("Invalid geometry size.");
                        var g = Allocate(count, indices);
                        for (int v = 0; v < count; v++)
                        {
                            g.vertices[v] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                            g.normals[v] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                            g.colors[v] = new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                            g.uv[v] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                        }
                        for (int i = 0; i < indices; i++) { int index = reader.ReadInt32(); if ((uint)index >= count) throw new InvalidDataException("Invalid mesh index."); g.indices[i] = index; }
                        if (module < SkyCityWfc.ModuleCount) { data.modules[module,lod,part] = g; data.Bytes += g.Bytes; }
                        else if (part > 0) { data.districts[module-SkyCityWfc.ModuleCount,lod,3+part] = g; data.Bytes += g.Bytes; }
                        else for (int patch=0;patch<4;patch++)
                        { var divided=Partition(g,patch); data.districts[module-SkyCityWfc.ModuleCount,lod,patch]=divided; data.Bytes+=divided.Bytes; }
                    }
            }
            return data;
        }
        static Geometry Allocate(int v, int t)
        { return new Geometry { vertices = new Vector3[v], normals = new Vector3[v], colors = new Color32[v], uv = new Vector2[v], indices = new int[t] }; }
        static Geometry Partition(Geometry source,int patch)
        {
            var map=new Dictionary<int,int>();var order=new List<int>();var triangles=new List<int>();
            for(int i=0;i<source.indices.Length;i+=3)
            {
                Vector3 c=(source.vertices[source.indices[i]]+source.vertices[source.indices[i+1]]+source.vertices[source.indices[i+2]])/3;
                if((c.x>=42?1:0)+(c.z>=42?2:0)!=patch)continue;
                for(int j=0;j<3;j++)
                {
                    int sourceIndex=source.indices[i+j],target;
                    if(!map.TryGetValue(sourceIndex,out target)){target=order.Count;map.Add(sourceIndex,target);order.Add(sourceIndex);}
                    triangles.Add(target);
                }
            }
            var g=Allocate(order.Count,triangles.Count);g.indices=triangles.ToArray();
            for(int i=0;i<order.Count;i++){int s=order[i];g.vertices[i]=source.vertices[s];g.normals[i]=source.normals[s];g.colors[i]=source.colors[s];g.uv[i]=source.uv[s];}
            return g;
        }

        public Payload Build(SkyCityWfc.Result layout, int lod, CancellationToken cancellation)
        {
            var payload = new Payload { layout = layout, lod = lod };
            var bridges=new Geometry[SkyCityWfc.CellCount];
            for(int cell=0;cell<SkyCityWfc.CellCount;cell++)
            {
                int x=cell%8,z=cell/8,state=layout.states[cell];
                if(state!=SkyCityWfc.Empty&&(x==0||x==7||z==0||z==7))
                { cancellation.ThrowIfCancellationRequested(); bridges[cell]=SkyCityBridgeGeometry.Build(layout,cell,lod,modules[state/4,lod,0]); }
            }
            for (int patch = 0; patch < 4; patch++) payload.opaque[patch] = Combine(layout, lod, 0, patch, cancellation,bridges);
            payload.water = Combine(layout, lod, 1, -1, cancellation,bridges);
            payload.cascades = Combine(layout, lod, 2, -1, cancellation,bridges);
            return payload;
        }
        Geometry Combine(SkyCityWfc.Result layout, int lod, int part, int patch, CancellationToken cancellation,Geometry[] bridges)
        {
            int[] states=layout.states;
            Vector2 shape=DistrictScale(layout.composition);
            float elevation=SkyCityWfc.Elevation(layout.composition);
            var baseMesh=districts[layout.composition,lod,part>0?3+part:patch];
            int vertices = baseMesh.vertices.Length, indices = baseMesh.indices.Length;
            for (int cell = 0; cell < SkyCityWfc.CellCount; cell++)
            {
                if (SkyCityWfc.Sanctuary(cell) || SkyCityWfc.Waterway(cell) || states[cell] == SkyCityWfc.Empty || (patch >= 0 && Patch(cell) != patch)) continue;
                var g = part==0&&bridges[cell]!=null?bridges[cell]:modules[states[cell] / 4,lod,part]; vertices += g.vertices.Length; indices += g.indices.Length;
            }
            var output = Allocate(vertices, indices); int vOffset = 0, tOffset = 0;
            for (int cell = 0; cell < SkyCityWfc.CellCount; cell++)
            {
                cancellation.ThrowIfCancellationRequested(); int state = states[cell];
                if (SkyCityWfc.Sanctuary(cell) || SkyCityWfc.Waterway(cell) || state == SkyCityWfc.Empty || (patch >= 0 && Patch(cell) != patch)) continue;
                var g = part==0&&bridges[cell]!=null?bridges[cell]:modules[state / 4,lod,part]; int turn = state % 4;
                Vector3 offset = new Vector3((cell % SkyCityWfc.Size) * SkyCityWfc.TileSize, elevation, (cell / SkyCityWfc.Size) * SkyCityWfc.TileSize);
                int x=cell%8,z=cell/8;bool bridge=x==0||x==7||z==0||z==7;
                for (int v = 0; v < g.vertices.Length; v++)
                {
                    Vector3 p=Rotate(g.vertices[v],turn),n=Rotate(g.normals[v],turn);
                    if(bridge)
                    { output.vertices[vOffset+v]=g.vertices[v];output.normals[vOffset+v]=g.normals[v]; }
                    else
                    { output.vertices[vOffset+v]=Shape(p+offset,shape);output.normals[vOffset+v]=new Vector3(n.x/shape.x,n.y,n.z/shape.y).normalized; }
                    output.colors[vOffset + v] = g.colors[v]; output.uv[vOffset + v] = g.uv[v];
                    if(part==1) { var f=Rotate(new Vector3(g.uv[v].x,0,g.uv[v].y),turn);output.uv[vOffset+v]=new Vector2(f.x*shape.x,f.z*shape.y).normalized; }
                }
                for (int t = 0; t < g.indices.Length; t++) output.indices[tOffset + t] = g.indices[t] + vOffset;
                vOffset += g.vertices.Length; tOffset += g.indices.Length;
            }
            for(int v=0;v<baseMesh.vertices.Length;v++)
            {
                output.vertices[vOffset+v]=Shape(baseMesh.vertices[v]+Vector3.up*elevation,shape);
                Vector3 n=baseMesh.normals[v];output.normals[vOffset+v]=new Vector3(n.x/shape.x,n.y,n.z/shape.y).normalized;output.colors[vOffset+v]=baseMesh.colors[v];output.uv[vOffset+v]=baseMesh.uv[v];
            }
            for(int t=0;t<baseMesh.indices.Length;t++)output.indices[tOffset+t]=baseMesh.indices[t]+vOffset;
            return output;
        }
        public static Vector2 DistrictScale(int composition)
        {
            switch(composition)
            {
                case 1:return new Vector2(.58f,1.0f);case 2:return new Vector2(.92f,.72f);
                case 3:return new Vector2(.8f,.88f);case 4:return new Vector2(.50f,.55f);
                case 5:return new Vector2(1.0f,.68f);case 6:return new Vector2(1.02f,.50f);
                case 7:return new Vector2(.66f,.90f);default:return Vector2.one;
            }
        }
        static Vector3 Shape(Vector3 p,Vector2 scale){return new Vector3(42+(p.x-42)*scale.x,p.y,42+(p.z-42)*scale.y);}
        static int Patch(int cell) { return (cell % 8 >= 4 ? 1 : 0) + (cell / 8 >= 4 ? 2 : 0); }
        static Vector3 Rotate(Vector3 p, int r)
        {
            // Unity positive yaw rotates +Z toward +X; sockets rotate identically.
            switch (r) { case 1:return new Vector3(p.z,p.y,-p.x); case 2:return new Vector3(-p.x,p.y,-p.z); case 3:return new Vector3(-p.z,p.y,p.x); default:return p; }
        }
        public static Mesh Upload(Geometry g, string name)
        {
            var mesh = new Mesh { name = name, indexFormat = g.vertices.Length > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.vertices = g.vertices; mesh.normals = g.normals; mesh.colors32 = g.colors; mesh.uv = g.uv;
            mesh.SetIndices(g.indices, MeshTopology.Triangles, 0, true);
            mesh.UploadMeshData(true); return mesh;
        }
    }
}
