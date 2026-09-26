using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SkyCity.Runtime.WorldGeneration;
using UnityEditor;
using UnityEngine;

public static class SkyCityModuleReview
{
    [Serializable] public sealed class SolverReport
    {
        public bool passed,deterministic,orderIndependent,cancellationObserved;
        public int chunks,neighborChecks,seamChecks,observations,propagations,restarts,fallbacks,moduleCount,distinctModulesObserved,silhouetteChecks;
        public double elapsedMilliseconds;
        public string error;
    }
    public static Task<SolverReport> SolverTask;
    [MenuItem("Sky City/Validation/Validate WFC constraints")]
    public static void RunSolver()
    {
        if(SolverTask!=null&&!SolverTask.IsCompleted)throw new InvalidOperationException("Solver review is running.");
        SolverTask=Task.Run(()=>CheckSolver());EditorApplication.update+=FinishSolver;
    }
    static void FinishSolver()
    {
        if(SolverTask==null||!SolverTask.IsCompleted)return;EditorApplication.update-=FinishSolver;
        Directory.CreateDirectory("Captures");File.WriteAllText("Captures/SkyCityInfinite_SolverValidation.json",JsonUtility.ToJson(SolverTask.Result,true));
        UnityEngine.Debug.Log("WFC constraint validation: "+SolverTask.Result.passed);
    }
    public static SolverReport CheckSolver()
    {
        var report=new SolverReport{moduleCount=SkyCityWfc.ModuleCount,deterministic=true,orderIndependent=true};var watch=Stopwatch.StartNew();
        try
        {
            var seen=new HashSet<int>();
            int[] seeds={93641,1,2027};
            var origins=new[]{new SkyCityWfc.Coord(0,0),new SkyCityWfc.Coord(-7,12),new SkyCityWfc.Coord(1000000,-1000000),new SkyCityWfc.Coord(-1000000000,1000000000)};
            foreach(int seed in seeds)foreach(var origin in origins)
            {
                var layouts=new Dictionary<SkyCityWfc.Coord,SkyCityWfc.Result>();
                for(int z=0;z<3;z++)for(int x=0;x<3;x++)
                {
                    var key=new SkyCityWfc.Coord(origin.x+x,origin.z+z);var r=SkyCityWfc.Solve(key,seed,CancellationToken.None);layouts.Add(key,r);report.chunks++;
                    for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)if(dx!=0||dz!=0)
                    {Require(r.composition!=SkyCityWfc.Composition(new SkyCityWfc.Coord(key.x+dx,key.z+dz),seed),"Adjacent landmark repetition");report.silhouetteChecks++;}
                    report.observations+=r.observations;report.propagations+=r.propagations;report.restarts+=r.restarts;if(r.usedSafeFallback)report.fallbacks++;
                    foreach(int state in r.states)if(state!=SkyCityWfc.Empty)seen.Add(state/4);
                    for(int iz=0;iz<8;iz++)for(int ix=0;ix<8;ix++)
                    {
                        if(ix<7){Require(SkyCityWfc.Compatible(r.states[iz*8+ix],r.states[iz*8+ix+1],1),"East socket contradiction");report.neighborChecks++;}
                        if(iz<7){Require(SkyCityWfc.Compatible(r.states[iz*8+ix],r.states[(iz+1)*8+ix],0),"North socket contradiction");report.neighborChecks++;}
                    }
                }
                for(int z=2;z>=0;z--)for(int x=2;x>=0;x--)
                {
                    var key=new SkyCityWfc.Coord(origin.x+x,origin.z+z);var a=layouts[key];
                    var again=SkyCityWfc.Solve(key,seed,CancellationToken.None);Require(a.fingerprint==again.fingerprint,"Regeneration/order mismatch");
                    if(x<2){var b=layouts[new SkyCityWfc.Coord(key.x+1,key.z)];for(int i=0;i<8;i++){Require(SkyCityWfc.Compatible(a.states[i*8+7],b.states[i*8],1),"East district seam");report.seamChecks++;}}
                    if(z<2){var b=layouts[new SkyCityWfc.Coord(key.x,key.z+1)];for(int i=0;i<8;i++){Require(SkyCityWfc.Compatible(a.states[56+i],b.states[i],0),"North district seam");report.seamChecks++;}}
                }
            }
            var cancellation=new CancellationTokenSource();cancellation.Cancel();
            try{SkyCityWfc.Solve(new SkyCityWfc.Coord(0,0),5,cancellation.Token);}catch(OperationCanceledException){report.cancellationObserved=true;}
            cancellation.Dispose();Require(report.cancellationObserved,"Cancellation ignored");Require(report.observations>0&&report.propagations>0,"WFC did not observe/propagate");
            report.distinctModulesObserved=seen.Count;Require(seen.Count>100,"Insufficient generated module diversity");
            Require(report.fallbacks==0,"The normal rules required fallback layouts");
            report.passed=true;
        }
        catch(Exception exception){report.error=exception.ToString();report.passed=false;}
        report.elapsedMilliseconds=watch.Elapsed.TotalMilliseconds;return report;
    }
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    [Serializable] public sealed class AssetReport
    {
        public bool passed;public int prefabs,distinctIds,lodMeshes,bootstrapDistricts,shaderErrors,compositions,transparentCascadeLods,waterCurrentMeshes;
        public long compressedLibraryBytes,sharedLibraryBytes;public string error;
    }
    public static AssetReport ValidateAssets()
    {
        var report=new AssetReport();
        try
        {
            var data=SkyCityModuleData.Read(File.ReadAllBytes("Assets/SkyCity/Resources/SkyCityInfinite/Modules.bytes"),CancellationToken.None);
            report.compositions=data.districts.GetLength(0);report.sharedLibraryBytes=data.Bytes;
            report.compressedLibraryBytes=new FileInfo("Assets/SkyCity/Resources/SkyCityInfinite/Modules.bytes").Length;
            for(int district=0;district<8;district++)for(int lod=0;lod<3;lod++)
            {
                var cascade=data.districts[district,lod,5];
                Require(cascade.indices.Length>0,"Transparent waterfall missing from LOD");
                bool hasLip=false,hasTail=false,hasSpray=false;
                foreach(var uv in cascade.uv){hasLip|=uv.x<=1&&uv.y<.01f;hasTail|=uv.x<=1&&uv.y>.95f;hasSpray|=uv.x>=2;}
                Require(hasLip&&hasTail&&hasSpray,"Incomplete waterfall surface/spray UVs");report.transparentCascadeLods++;
                for(int patch=0;patch<4;patch++)foreach(var color in data.districts[district,lod,patch].colors)
                    Require(Math.Abs(color.a/255f-.7f)>.025f,"Water baked into opaque architecture");
                foreach(var uv in data.districts[district,lod,4].uv)Require(uv.sqrMagnitude>.9f,"Pool current vector missing");
                report.waterCurrentMeshes++;
            }
            Require(SkyCityWfc.Waterway(11)&&SkyCityWfc.Sanctuary(19),"Buildings obstruct the south spillway");
            var ids=new HashSet<int>();
            foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{SkyCityModuleBuilder.Root+"/Modules"}))
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));report.prefabs++;
                var descriptor=prefab.GetComponent<SkyCityModuleDescriptor>();Require(descriptor!=null,"Missing descriptor");
                Require(ids.Add(descriptor.moduleId),"Duplicate module ID");
                var lods=prefab.GetComponent<LODGroup>().GetLODs();Require(lods.Length==3,"Missing distance LOD");
                for(int i=0;i<3;i++)
                {
                    var mesh=lods[i].renderers[0].GetComponent<MeshFilter>().sharedMesh;
                    Require(mesh.vertexCount==data.modules[descriptor.moduleId,i,0].vertices.Length,"Prefab differs from runtime vocabulary");report.lodMeshes++;
                }
            }
            report.distinctIds=ids.Count;Require(report.prefabs==128&&ids.Count==128,"Expected 128 unique modules");
            foreach(string name in new[]{"SkyCity/Modular Architecture","SkyCity/Atmosphere Raymarch","SkyCity/Reflecting Garden Water","SkyCity/Infinite Cascades"})
            {var shader=Shader.Find(name);if(shader==null||ShaderUtil.ShaderHasError(shader))report.shaderErrors++;}
            Require(report.shaderErrors==0,"Shader error");
            var world=UnityEngine.Object.FindObjectOfType<SkyCityInfiniteWorld>();Require(world!=null,"Streaming world missing");
            Require(world.cascadeMaterial!=null&&world.cascadeMaterial.shader.name=="SkyCity/Infinite Cascades"&&world.cascadeMaterial.renderQueue>=3000,"Cascade material must render transparently");
            report.bootstrapDistricts=world.transform.childCount;Require(report.bootstrapDistricts==0&&!Application.isPlaying,"Scene preloads district objects");
            report.passed=true;
        }
        catch(Exception e){report.error=e.ToString();}
        File.WriteAllText("Captures/SkyCityInfinite_AssetValidation.json",JsonUtility.ToJson(report,true));return report;
    }
}
