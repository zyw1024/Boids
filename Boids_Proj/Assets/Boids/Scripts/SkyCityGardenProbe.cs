using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Boids.Art.Infinite;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Boids.Art
{
    /// <summary>Opt-in rendered exercise of real controls, planting, growth and world streaming.</summary>
    public sealed class SkyCityGardenProbe : MonoBehaviour
    {
        SkyCityFirstPerson player;SkyCityGardenDirector garden;SkyCityFlockMenu menu;
        readonly Dictionary<string,bool> checks=new Dictionary<string,bool>();
        readonly List<double> times=new List<double>();
        readonly Dictionary<string,List<double>> phases=new Dictionary<string,List<double>>();
        string phase="nearGarden";
        string output;bool measure,quit;int frames,errors;double started;
        RenderTexture offscreenTarget;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoRun()
        {
            var args=Environment.GetCommandLineArgs();if(!args.Contains("-garden-review"))return;
            var p=FindObjectOfType<SkyCityFirstPerson>();if(p==null)return;
            string path=Path.Combine(Application.persistentDataPath,"GardenReview.json");
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-garden-output")path=args[i+1];
            p.gameObject.AddComponent<SkyCityGardenProbe>().Begin(path,true);
        }
        public void Begin(string path,bool exit=false)
        {
            player=GetComponent<SkyCityFirstPerson>();garden=FindObjectOfType<SkyCityGardenDirector>();menu=player.flockMenu;
            output=Path.GetFullPath(path);quit=exit;player.acceptInput=false;player.CapturePointer(false);Application.runInBackground=true;
            if(Environment.GetCommandLineArgs().Contains("-garden-baseline"))foreach(var r in garden.GetComponentsInChildren<Renderer>(true))r.forceRenderingOff=true;
            if(Environment.GetCommandLineArgs().Contains("-garden-offscreen"))
            {
                offscreenTarget=new RenderTexture(1600,900,24,RenderTextureFormat.ARGBHalf){name="Botany benchmark render target"};offscreenTarget.Create();
            }
            Application.logMessageReceived+=Log;RenderPipelineManager.endCameraRendering+=Rendered;StartCoroutine(Run());
        }
        void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
        void Rendered(ScriptableRenderContext c,Camera camera){if(measure&&camera==player.view)frames++;}
        void Update(){if(measure){double ms=Time.unscaledDeltaTime*1000;times.Add(ms);if(!phases.ContainsKey(phase))phases.Add(phase,new List<double>());phases[phase].Add(ms);}}
        void LateUpdate()
        {
            if(offscreenTarget==null||player==null)return;
            // Reflection callbacks identify Camera.main, which must be enabled during the render.
            player.view.enabled=true;
            try{player.view.Render();}finally{player.view.enabled=false;}
        }
        Slider Control(string id)=>menu.GetComponentsInChildren<Slider>(true).First(s=>s.name==id);
        void Press(string id)=>menu.GetComponentsInChildren<Button>(true).First(b=>b.name==id).onClick.Invoke();
        IEnumerator Run()
        {
            // Let every scene Start resolve Camera.main before disabling automatic rendering.
            yield return null;
            if(offscreenTarget!=null){player.view.targetTexture=offscreenTarget;player.view.enabled=false;}
            float until=Time.realtimeSinceStartup+90;while((!player.world.GenerationComplete||player.world.PendingCount>0)&&Time.realtimeSinceStartup<until)yield return null;
            checks["worldReady"]=player.world.GenerationComplete;yield return new WaitForSecondsRealtime(2);
            menu.SetOpen(true);Press("Garden tab");checks["thirdTabAndPointerRelease"]=menu.GardenPageActive&&!player.Captured;
            Control("BranchDepth").value=3;checks["depth3SelectsCachedGeometry"]=garden.branchDepth==3&&garden.treeGenerations[0].activeSelf&&!garden.treeGenerations[2].activeSelf;
            Control("BranchDepth").value=5;Control("GardenNeighbours").value=4;Control("GardenSpeed").value=3;Control("GardenBreeze").value=2;
            checks["liveGardenControls"]=garden.branchDepth==5&&garden.neighbours==4&&garden.evolutionSpeed==3&&garden.breeze==2;
            Press("Garden rules");checks["realStateOverlay"]=garden.showRules&&garden.gardens.All(g=>g.rules.enabled);
            checks["menuBlocksPlantingAndRecall"]=!garden.TryPlant()&&!player.CallBirds();
            int birdCount=player.flock.BirdCount,worldSeed=player.world.seed;Press("Reset garden parameters");
            checks["independentDefaults"]=garden.branchDepth==5&&garden.neighbours==1&&garden.evolutionSpeed==1&&!garden.showRules&&player.flock.BirdCount==birdCount&&player.world.seed==worldSeed;
            Press("Reset garden states");checks["clearRemovesAllActivity"]=garden.gardens.All(g=>g.Simulation.Cells.All(x=>x==SkyCityGardenAutomaton.Phase.Rest));
            menu.SetOpen(false);player.CapturePointer(false);
            var bed=garden.gardens[0];var point=bed.transform.TransformPoint(new Vector3(-bed.size.x*.5f+bed.bedSpacing*.5f,0,0));
            var eye=point+new Vector3(0,5,-2);player.SetPose(eye,Quaternion.LookRotation(point-eye));Physics.SyncTransforms();
            checks["aimedPlanting"]=garden.TryPlant();
            var obstruction=GameObject.CreatePrimitive(PrimitiveType.Cube);obstruction.name="Temporary planting occlusion test";
            obstruction.transform.position=Vector3.Lerp(eye,point,.5f);obstruction.transform.localScale=Vector3.one*.8f;Physics.SyncTransforms();
            checks["solidGeometryBlocksSeeds"]=!garden.TryPlant();Destroy(obstruction);yield return null;Physics.SyncTransforms();
            var missed=eye+Vector3.up*15;player.SetPose(eye,Quaternion.LookRotation(missed-eye));checks["skyRejectsPlanting"]=!garden.TryPlant();
            player.SetPose(eye,Quaternion.LookRotation(point-eye));checks["rightClickRecallPreserved"]=player.CallBirds();
            Press("Awaken gardens");garden.windSeeds=true;garden.ReplayTree();
            var cameraEye=new Vector3(-12,11,-11);player.SetPose(cameraEye,Quaternion.LookRotation(new Vector3(-20,10,5)-cameraEye));
            var reflection=FindObjectOfType<SkyCityWaterReflection>();int reflectionStart=reflection.RenderCount;
            started=Time.realtimeSinceStartupAsDouble;measure=true;
            yield return new WaitForSecondsRealtime(6);
            checks["branchGrowthProgresses"]=garden.TreeGrowth>.2f&&garden.TreeGrowth<.6f;
            checks["neighbourPropagationOccurs"]=garden.gardens.Sum(g=>g.Simulation.Awakened)>0;
            phase="distantStreaming";
            // Floating origin moves the entire authored island; distance disables simulation, preserving exact state.
            player.world.Teleport(new SkyCityWfc.Coord(1000000,-1000000),new Vector3(0,100,0));yield return null;yield return null;
            var state=garden.gardens.Select(g=>g.Simulation.Fingerprint()).ToArray();var generations=garden.gardens.Select(g=>g.Simulation.Generation).ToArray();
            yield return new WaitForSecondsRealtime(5);
            checks["distantSimulationFrozen"]=garden.gardens.Select(g=>g.Simulation.Fingerprint()).SequenceEqual(state)&&garden.gardens.All(g=>!g.Simulating);
            checks["arrivalUnloadedVisually"]=!player.world.authoredArrival.gameObject.activeSelf;
            phase="returnStreaming";
            player.ReturnHome();player.SetPose(cameraEye,Quaternion.LookRotation(new Vector3(-20,10,5)-cameraEye));
            // Before the next Update, the exact integer states must still match.
            checks["returnPreservesCells"]=garden.gardens.Select(g=>g.Simulation.Fingerprint()).SequenceEqual(state);
            yield return new WaitForSecondsRealtime(2);
            checks["returnResumesEvolution"]=garden.gardens.Where((g,i)=>g.Simulation.Generation>generations[i]).Count()==garden.gardens.Length;
            checks["originRestored"]=player.world.OriginX==0&&player.world.OriginZ==0&&player.world.authoredArrival.position.sqrMagnitude<.001f;
            phase="settledGarden";
            yield return new WaitForSecondsRealtime(15);
            checks["treeFinishesGrowth"]=garden.TreeGrowth>=1;
            checks["boundedStreaming"]=player.world.PeakResident<=25&&player.world.PeakPending<=3&&player.world.FailedJobs==0;
            checks["waterReflectionsRendered"]=reflection.RenderCount>reflectionStart+30;
            measure=false;
            bool coverage=frames>=times.Count*.95,performance=coverage&&Percentile(.95)<33.4&&Percentile(.99)<50&&Percentile(1)<250;
            var phaseTimes=phases.ToDictionary(p=>p.Key,p=>new{frames=p.Value.Count,p95=P(p.Value,.95),p99=P(p.Value,.99),maximum=P(p.Value,1)});
            var report=new{passed=checks.All(p=>p.Value)&&performance&&errors==0,checks,performance,phaseTimes,baseline=Environment.GetCommandLineArgs().Contains("-garden-baseline"),standalone=!Application.isEditor,frames=times.Count,renderedFrames=frames,seconds=Time.realtimeSinceStartupAsDouble-started,p95=Percentile(.95),p99=Percentile(.99),maximum=Percentile(1),errors,cells=garden.gardens.Sum(g=>g.Simulation.Cells.Length),player.world.PeakResident,player.world.PeakPending,player.world.PeakMainThreadMs,gpu=SystemInfo.graphicsDeviceName,width=offscreenTarget!=null?1600:Screen.width,height=offscreenTarget!=null?900:Screen.height,renderPath=offscreenTarget!=null?"Explicit URP Camera.Render into HDR render texture; hidden player window. Overlay UI visually verified separately in editor.":"Game view",note="Public planting and recall handlers plus actual UI callbacks; physical keyboard events are not synthesized. Garden is intentionally local to the authored island in this milestone."};
            Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
            garden.ResetDefaults();player.ReturnHome();player.acceptInput=true;Application.logMessageReceived-=Log;RenderPipelineManager.endCameraRendering-=Rendered;
            if(quit)Application.Quit(report.passed?0:2);
        }
        double Percentile(double p){if(times.Count==0)return -1;var sorted=times.OrderBy(t=>t).ToArray();return sorted[Math.Min(sorted.Length-1,(int)Math.Ceiling((sorted.Length-1)*p))];}
        static double P(List<double> values,double p){var sorted=values.OrderBy(t=>t).ToArray();return sorted[Math.Min(sorted.Length-1,(int)Math.Ceiling((sorted.Length-1)*p))];}
        void OnDestroy(){Application.logMessageReceived-=Log;RenderPipelineManager.endCameraRendering-=Rendered;if(offscreenTarget!=null){if(player!=null){player.view.targetTexture=null;player.view.enabled=true;}offscreenTarget.Release();Destroy(offscreenTarget);}}
    }
}
