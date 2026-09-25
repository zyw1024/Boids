using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Boids.Art.Infinite;
using UnityEditor;
using UnityEngine;

public static class SkyCityStructureReview
{
    [Serializable] public sealed class Report
    {
        public bool passed;
        public int nativeBridgeLods, pointedRoofLods, links, seamPoints, compositions, generatedLods, triangles, steepSpans;
        public float maxSeamError, maxEntranceRadius, maxFootingRadius, minBayLength=100, maxBayLength;
        public string error;
    }
    static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    [MenuItem("Boids/Sky City Infinite/Validate architectural connections")]
    public static void Run(){Check();}
    public static Report Check()
    {
        var report=new Report();
        try
        {
            var data=SkyCityModuleData.Read(File.ReadAllBytes("Assets/Boids/Resources/SkyCityInfinite/Modules.bytes"),CancellationToken.None);
            foreach(int variant in new[]{3,7})for(int lod=0;lod<3;lod++)
            {
                var roof=data.modules[48+variant,lod,0];float top=0;
                for(int i=0;i<roof.vertices.Length;i++)if(roof.colors[i].a==51)top=Mathf.Max(top,roof.vertices[i].y);
                Require(top>(9+variant*1.2f)*.8f+5.5f,"Pointed campanile roof lost during triangulation");report.pointedRoofLods++;
            }
            // Direct geometry checks catch the original negative-height gallery,
            // including a regression that only appears after an LOD switch.
            for(int module=80;module<96;module++)for(int lod=0;lod<3;lod++)
            {
                var mesh=data.modules[module,lod,0];int feet=0;
                foreach(var p in mesh.vertices)
                {
                    Require(p.y>=-.451f,"Arcade hangs below the bridge deck: module "+module+" LOD "+lod);
                    if(Mathf.Abs(p.y-.25f)<.001f)feet++;
                }
                Require(feet>12,"Bridge has no gallery/deck junction");report.nativeBridgeLods++;
            }
            var shapes=new HashSet<int>();var geometryCases=new HashSet<string>();
            long[] origins={0,-1000000000,1000000};int[] dx={0,1,0,-1},dz={1,0,-1,0};
            foreach(long origin in origins)for(int z=-4;z<=4;z++)for(int x=-4;x<=4;x++)
            {
                var coord=new SkyCityWfc.Coord(origin+x,-origin+z);
                var layout=new SkyCityWfc.Result{coord=coord,seed=93641,composition=SkyCityWfc.Composition(coord,93641)};
                for(int dir=0;dir<4;dir++)
                {
                    int gate=SkyCityWfc.Gate(coord,dir,layout.seed);if(gate<0)continue;
                    int cell=dir==0?56+gate:dir==1?gate*8+7:dir==2?gate:gate*8;
                    int reverse=dir==0?gate:dir==1?gate*8:dir==2?56+gate:gate*8+7;
                    var neighbor=new SkyCityWfc.Coord(coord.x+dx[dir],coord.z+dz[dir]);
                    var next=new SkyCityWfc.Result{coord=neighbor,seed=layout.seed,composition=SkyCityWfc.Composition(neighbor,layout.seed)};
                    var a=SkyCityBridgeGeometry.Describe(layout,cell);var b=SkyCityBridgeGeometry.Describe(next,reverse);
                    Vector3 offset=new Vector3(dx[dir]*168,0,dz[dir]*168);
                    foreach(float side in new[]{-1.8f,0,1.8f})foreach(float height in new[]{-.45f,.25f,1.55f,4.10f})
                    {
                        Vector3 p=SkyCityBridgeGeometry.Place(a,new Vector3(side,height,6),a.repeats-1);
                        Vector3 q=SkyCityBridgeGeometry.Place(b,new Vector3(-side,height,6),b.repeats-1)+offset;
                        float error=Vector3.Distance(p,q);report.maxSeamError=Mathf.Max(report.maxSeamError,error);
                        Require(error<.001f,"Separated deck/roof edges at "+coord+" direction "+dir);report.seamPoints++;
                    }
                    Vector2 scale=SkyCityModuleData.DistrictScale(layout.composition);
                    foreach(float side in new[]{-1.825f,1.825f})
                    {
                        Vector3 entrance=a.island+a.right*side,foot=entrance-a.forward*a.inset;
                        float radius=new Vector2((entrance.x-42)/scale.x,(entrance.z-42)/scale.y).magnitude;
                        float footRadius=new Vector2((foot.x-42)/scale.x,(foot.z-42)/scale.y).magnitude;
                        report.maxEntranceRadius=Mathf.Max(report.maxEntranceRadius,radius);report.maxFootingRadius=Mathf.Max(report.maxFootingRadius,footRadius);
                        Require(radius<36.5f&&footRadius<27,"Bridge abutment misses the island");
                    }
                    float bay=a.length/a.repeats;report.minBayLength=Mathf.Min(report.minBayLength,bay);report.maxBayLength=Mathf.Max(report.maxBayLength,bay);
                    Require(bay>=9&&bay<=12.001f,"Distorted gallery bay proportions");
                    float slope=(a.seam.y-a.island.y)/a.length;
                    if(Mathf.Abs(slope)>.10f)report.steepSpans++;
                    // One transformed mesh for every composition/direction pair
                    // also tests sloping stair geometry and normal transport.
                    if(geometryCases.Add(layout.composition+":"+dir))for(int lod=0;lod<3;lod++)
                    {
                        var mesh=SkyCityBridgeGeometry.Build(layout,cell,lod,data.modules[80,lod,0]);
                        foreach(var uv in mesh.uv)Require(Mathf.Abs(uv.x-(-10-dir))<.001f,"Bridge missing its residency visibility tag");
                        for(int i=0;i<mesh.indices.Length;i+=3)
                        {
                            int ia=mesh.indices[i],ib=mesh.indices[i+1],ic=mesh.indices[i+2];
                            Vector3 normal=Vector3.Cross(mesh.vertices[ib]-mesh.vertices[ia],mesh.vertices[ic]-mesh.vertices[ia]);
                            if(normal.sqrMagnitude<1e-10f)continue;
                            Require(Vector3.Dot(normal.normalized,mesh.normals[ia])>.98f,"Invalid transformed bridge normal");report.triangles++;
                        }
                        report.generatedLods++;
                    }
                    shapes.Add(layout.composition);report.links++;
                }
            }
            report.compositions=shapes.Count;Require(shapes.Count==8&&report.steepSpans>0,"Missing district/elevation coverage");report.passed=true;
        }
        catch(Exception e){report.error=e.ToString();}
        Directory.CreateDirectory("Captures");File.WriteAllText("Captures/SkyCityInfinite_StructureValidation.json",JsonUtility.ToJson(report,true));
        return report;
    }
    [Serializable] public sealed class LiveReport
    {
        public bool passed=true;
        public int samples,visibleBridgeHalves,hiddenBridgeHalves,unpairedBridgeHalves;
        public string error;
    }
    static LiveReport live=new LiveReport();
    public static LiveReport CheckLive()
    {
        try
        {
            var world=UnityEngine.Object.FindObjectOfType<SkyCityInfiniteWorld>();
            Require(Application.isPlaying&&world!=null&&world.Ready,"Wait for a running world");
            var block=new MaterialPropertyBlock();
            var drawn=new Dictionary<SkyCityWfc.Coord,Vector4>();
            foreach(Transform root in world.transform)
            {
                if(!root.gameObject.activeSelf)continue;
                var coord=new SkyCityWfc.Coord(world.OriginX+(long)Math.Round(root.position.x/168),world.OriginZ+(long)Math.Round(root.position.z/168));
                root.GetComponentInChildren<MeshRenderer>().GetPropertyBlock(block);drawn.Add(coord,block.GetVector("_BridgeReveal"));
            }
            foreach(var entry in drawn)
            {
                var coord=entry.Key;Vector4 actual=entry.Value;
                for(int dir=0;dir<4;dir++)
                {
                    if(SkyCityWfc.Gate(coord,dir,world.seed)<0)continue;
                    var neighbor=new SkyCityWfc.Coord(coord.x+(dir==1?1:dir==3?-1:0),coord.z+(dir==0?1:dir==2?-1:0));
                    ulong hash;bool resident=world.TryGetFingerprint(neighbor,out hash);
                    if(!resident&&actual[dir]>0)live.unpairedBridgeHalves++;
                    Require(resident||actual[dir]==0,"A bridge renders without its opposite island");
                    if(resident)Require(Mathf.Abs(actual[dir]-drawn[neighbor][(dir+2)%4])<.001f,"Opposite bridge halves reveal at different times");
                    if(actual[dir]>0)live.visibleBridgeHalves++;else live.hiddenBridgeHalves++;
                }
            }
            live.samples++;
        }
        catch(Exception e){live.passed=false;live.error=e.ToString();}
        File.WriteAllText("Captures/SkyCityInfinite_StructureRuntime.json",JsonUtility.ToJson(live,true));return live;
    }
}
