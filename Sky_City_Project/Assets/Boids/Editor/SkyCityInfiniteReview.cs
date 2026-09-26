using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Boids.Art.Infinite;
using UnityEditor;
using UnityEngine;

public static class SkyCityInfiniteReview
{
    [Serializable] public sealed class SolverReport
    {
        public bool passed,deterministic,orderIndependent,cancellationObserved;
        public int chunks,neighborChecks,seamChecks,observations,propagations,restarts,fallbacks,moduleCount,distinctModulesObserved,silhouetteChecks;
        public double elapsedMilliseconds;
        public string error;
    }
    public static Task<SolverReport> SolverTask;
    [MenuItem("Boids/Sky City Infinite/Validate WFC constraints")]
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
            var data=SkyCityModuleData.Read(File.ReadAllBytes("Assets/Boids/Resources/SkyCityInfinite/Modules.bytes"),CancellationToken.None);
            report.compositions=data.districts.GetLength(0);report.sharedLibraryBytes=data.Bytes;
            report.compressedLibraryBytes=new FileInfo("Assets/Boids/Resources/SkyCityInfinite/Modules.bytes").Length;
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
            foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{SkyCityInfiniteBuilder.Root+"/Modules"}))
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
            foreach(string name in new[]{"Boids/SkyCity/Modular Architecture","Boids/SkyCity/Atmosphere Raymarch","Boids/SkyCity/Reflecting Garden Water","Boids/SkyCity/Infinite Cascades"})
            {var shader=Shader.Find(name);if(shader==null||ShaderUtil.ShaderHasError(shader))report.shaderErrors++;}
            Require(report.shaderErrors==0,"Shader error");
            var world=UnityEngine.Object.FindObjectOfType<SkyCityInfiniteWorld>();Require(world!=null,"Streaming world missing");
            Require(world.cascadeMaterial!=null&&world.cascadeMaterial.shader.name=="Boids/SkyCity/Infinite Cascades"&&world.cascadeMaterial.renderQueue>=3000,"Cascade material must render transparently");
            report.bootstrapDistricts=world.transform.childCount;Require(report.bootstrapDistricts==0&&!Application.isPlaying,"Scene preloads district objects");
            report.passed=true;
        }
        catch(Exception e){report.error=e.ToString();}
        File.WriteAllText("Captures/SkyCityInfinite_AssetValidation.json",JsonUtility.ToJson(report,true));return report;
    }
    static int tourIndex;static double nextTour;static bool shot;static SkyCityWfc.Coord[] tourCoords;
    static SkyCityVoyager tourVoyager;
    public static void CaptureDistricts()
    {
        tourVoyager=UnityEngine.Object.FindObjectOfType<SkyCityVoyager>();Require(tourVoyager!=null&&Application.isPlaying,"Run the scene first");
        tourCoords=new SkyCityWfc.Coord[8];var found=new bool[8];
        for(int z=0;z<6;z++)for(int x=0;x<6;x++)
        {var coord=new SkyCityWfc.Coord(x,z);int type=SkyCityWfc.Composition(coord,tourVoyager.world.seed);if(!found[type]){found[type]=true;tourCoords[type]=coord;}}
        foreach(bool f in found)Require(f,"Tour composition missing");
        tourIndex=0;shot=false;tourVoyager.acceptInput=false;tourVoyager.cruise=false;
        PositionTour();EditorApplication.update+=UpdateTour;
    }
    static void PositionTour()
    {
        float elevation=SkyCityWfc.Elevation(tourIndex);
        tourVoyager.world.Teleport(tourCoords[tourIndex],new Vector3(80,elevation+(tourIndex==4?43:29),tourIndex==4?-90:-64));
        tourVoyager.transform.LookAt(new Vector3(50,elevation+(tourIndex==4?20:9),44));tourVoyager.SyncAngles();
        nextTour=EditorApplication.timeSinceStartup+4;shot=false;
    }
    static void UpdateTour()
    {
        if(!Application.isPlaying||tourVoyager==null){EditorApplication.update-=UpdateTour;return;}
        if(EditorApplication.timeSinceStartup<nextTour||tourVoyager.world.PendingCount>0)return;
        if(!shot)
        {
            ScreenCapture.CaptureScreenshot(Path.GetFullPath("Captures/SkyCityInfinite_District"+tourIndex+".png"));shot=true;nextTour=EditorApplication.timeSinceStartup+.7;return;
        }
        tourIndex++;
        if(tourIndex<8){PositionTour();return;}
        EditorApplication.update-=UpdateTour;tourVoyager.ReturnHome();tourVoyager.acceptInput=true;
        UnityEngine.Debug.Log("Eight live district compositions captured.");
    }
    [MenuItem("Boids/Sky City Infinite/Render module atlas")]
    public static void RenderAtlas()
    {
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var camera=new GameObject("Module catalog camera").AddComponent<Camera>();
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camera.gameObject,scene);camera.scene=scene;
        camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.11f,.17f,.23f);
        camera.nearClipPlane=.1f;camera.farClipPlane=180;camera.allowHDR=false;
        var light=new GameObject("Catalog dawn light").AddComponent<Light>();light.type=LightType.Directional;
        light.intensity=2;light.color=new Color(1,.87f,.72f);light.transform.rotation=Quaternion.Euler(48,-35,0);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(light.gameObject,scene);
        var rt=new RenderTexture(192,192,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;
        var pixels=new Texture2D(3072,1536,TextureFormat.RGB24,false);
        var prior=RenderTexture.active;RenderTexture.active=rt;GL.Clear(true,true,camera.backgroundColor);
        try
        {
            string[] ids=AssetDatabase.FindAssets("t:Prefab",new[]{SkyCityInfiniteBuilder.Root+"/Modules"});
            Array.Sort(ids,(a,b)=>string.CompareOrdinal(AssetDatabase.GUIDToAssetPath(a),AssetDatabase.GUIDToAssetPath(b)));
            for(int id=0;id<ids.Length;id++)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(ids[id]));
                var ob=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);UnityEngine.Object.DestroyImmediate(ob.GetComponent<LODGroup>());
                foreach(Transform child in ob.transform)child.gameObject.SetActive(child.name=="LOD0"||child.name=="Pool");
                var bounds=new Bounds(Vector3.zero,Vector3.one);
                foreach(var r in ob.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
                camera.transform.position=bounds.center+new Vector3(1.2f,.85f,-1.5f).normalized*65;
                camera.transform.LookAt(bounds.center);camera.orthographicSize=Mathf.Max(8.5f,bounds.extents.magnitude*.89f);
                camera.rect=new Rect(0,0,1,1);camera.aspect=1;
                camera.Render();RenderTexture.active=rt;
                pixels.ReadPixels(new Rect(0,0,192,192),(id/8)*192,(7-id%8)*192);
                UnityEngine.Object.DestroyImmediate(ob);
            }
            pixels.Apply();Directory.CreateDirectory("Captures");
            File.WriteAllBytes("Captures/SkyCityInfinite_128Modules.png",pixels.EncodeToPNG());UnityEngine.Object.DestroyImmediate(pixels);
        }
        finally
        {
            RenderTexture.active=prior;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        }
    }
    public static void BuildPlayer()
    {
        Directory.CreateDirectory("Builds/SkyCityInfinite");
        PlayerSettings.enableFrameTimingStats=true;PlayerSettings.runInBackground=true;
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{SkyCityInfiniteBuilder.ScenePath},
            locationPathName="Builds/SkyCityInfinite/SkyCityInfinite.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        File.WriteAllText("Captures/SkyCityInfinite_Build.json","{\"result\":\""+result.summary.result+"\",\"errors\":"+result.summary.totalErrors+",\"warnings\":"+result.summary.totalWarnings+"}");
    }
}
