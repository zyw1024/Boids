using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using SkyCity.Runtime.WorldGeneration;

namespace SkyCity.Runtime
{
    public sealed class SkyCityDistrictGardenProbe : MonoBehaviour
    {
        SkyCityFirstPerson player;SkyCityDistrictGardens streaming;SkyCityGardenDirector director;
        readonly Dictionary<string,bool> checks=new Dictionary<string,bool>();readonly List<double> frameTimes=new List<double>();
        readonly Dictionary<string,List<double>> phases=new Dictionary<string,List<double>>();string phase="remoteArrival";
        RenderTexture target;int errors,rendered;bool measure,quit;string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoRun()
        {
            var args=Environment.GetCommandLineArgs();if(!args.Contains("-district-garden-review"))return;
            string output=Path.Combine(Application.persistentDataPath,"DistrictGardenReview.json");for(int i=0;i<args.Length-1;i++)if(args[i]=="-garden-output")output=args[i+1];
            FindObjectOfType<SkyCityFirstPerson>().gameObject.AddComponent<SkyCityDistrictGardenProbe>().Begin(output,true);
        }
        public void Begin(string file,bool exit=false)
        {
            output=Path.GetFullPath(file);quit=exit;player=GetComponent<SkyCityFirstPerson>();streaming=player.world.GetComponent<SkyCityDistrictGardens>();director=streaming.director;
            player.acceptInput=false;player.CapturePointer(false);Application.runInBackground=true;
            target=new RenderTexture(1600,900,24,RenderTextureFormat.ARGBHalf);target.Create();
            Application.logMessageReceived+=Log;RenderPipelineManager.endCameraRendering+=Rendered;StartCoroutine(Run());
        }
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
        void Rendered(ScriptableRenderContext context,Camera camera){if(measure&&camera==player.view)rendered++;}
        void Update(){if(measure){double ms=Time.unscaledDeltaTime*1000d;frameTimes.Add(ms);if(!phases.ContainsKey(phase))phases.Add(phase,new List<double>());phases[phase].Add(ms);}}
        void LateUpdate(){if(target==null)return;player.view.enabled=true;try{player.view.Render();}finally{player.view.enabled=false;}}
        IEnumerator Settled()
        {
            float until=Time.realtimeSinceStartup+60;yield return null;
            while((!player.world.GenerationComplete||player.world.PendingCount>0||streaming.PendingDistricts>0)&&Time.realtimeSinceStartup<until)yield return null;
        }
        IEnumerator Run()
        {
            yield return null;player.view.targetTexture=target;player.view.enabled=false;
            yield return Settled();checks["worldReady"]=player.world.GenerationComplete;
            checks["streamedPlantsCreated"]=streaming.LiveGardens>0&&streaming.LiveTrees>0;
            measure=true;
            var remote=new SkyCityWfc.Coord(1000000,-1000000);player.world.Teleport(remote,new Vector3(0,70,0));yield return Settled();
            checks["plantsBeyondMainIsland"]=streaming.LiveGardens>0&&!player.world.authoredArrival.gameObject.activeSelf;
            var coord=streaming.Coordinates.First(c=>streaming.GardensAt(c).Length>0);var gardens=streaming.GardensAt(coord);var bed=gardens[0];
            var point=bed.transform.TransformPoint(new Vector3(-.75f,0,-.45f));var eye=point+Vector3.up*4+Vector3.back;
            player.SetPose(eye,Quaternion.LookRotation(point-eye));Physics.SyncTransforms();yield return null;
            checks["remoteAimedPlanting"]=director.TryPlant();
            player.flockMenu.SetOpen(true);player.flockMenu.ShowGardenSettings();checks["remoteMenuAndIsolation"]=player.flockMenu.GardenPageActive&&!director.TryPlant()&&!player.CallBirds();
            director.ApplyDepth(3);director.showRules=true;director.RefreshAppearance();yield return null;
            checks["remoteLiveRules"]=gardens.All(g=>g.rules.enabled);
            checks["remoteTreeDepth"]=streaming.TreeMeshesMatchDepth&&director.branchDepth==3;
            player.flockMenu.SetOpen(false);player.CapturePointer(false);director.ResetDefaults();director.windSeeds=false;
            director.AwakenGardens();director.ReplayTree();phase="remoteGarden";yield return new WaitForSecondsRealtime(6);
            checks["remoteCellPropagation"]=gardens.Sum(g=>g.Simulation.Awakened)>0;
            checks["remoteTreeAnimation"]=director.TreeGrowth>.2f&&director.TreeGrowth<.65f;
            director.evolutionSpeed=0;var before=gardens.Select(g=>g.Simulation.Fingerprint()).ToArray();
            var originX=player.world.OriginX;var originZ=player.world.OriginZ;var savedEye=player.view.transform.position;var savedRotation=player.view.transform.rotation;
            phase="unloadAndReturn";player.world.Teleport(new SkyCityWfc.Coord(-2000000,2000000),new Vector3(0,70,0));yield return Settled();
            checks["oldPlantsUnloaded"]=streaming.GardensAt(coord).Length==0;
            checks["stateWrittenWithoutResidentCopies"]=streaming.SavedDistricts>0&&Directory.GetFiles(streaming.StateDirectory).Length>0;
            player.world.Teleport(new SkyCityWfc.Coord(originX,originZ),savedEye);player.SetPose(savedEye,savedRotation);yield return Settled();
            var returned=streaming.GardensAt(coord);
            checks["exactStateRestored"]=returned.Select(g=>g.Simulation.Fingerprint()).SequenceEqual(before)&&streaming.RestoredDistricts>0;
            director.evolutionSpeed=1;yield return new WaitForSecondsRealtime(2);
            checks["restoredCellsResume"]=!returned.Select(g=>g.Simulation.Fingerprint()).SequenceEqual(before);
            phase="seedReplacement";int seed=player.world.seed;player.world.ApplyGeneration(seed+101,player.world.ActiveSettings);yield return Settled();
            checks["generationChangeReplacesPlants"]=returned.All(g=>g==null)&&streaming.ResidentDistricts==player.world.ResidentCount;
            checks["registryHasNoStaleGardens"]=director.AllGardens.Count()==director.gardens.Length+streaming.LiveGardens;
            player.world.SetStreaming(1,2);yield return Settled();yield return new WaitForSecondsRealtime(2);
            checks["shrinkBoundsPlants"]=streaming.ResidentDistricts<=9&&streaming.LiveGardens<=36;
            phase="maximumRadius";player.world.SetStreaming(3,2);yield return Settled();
            checks["expandedPlantBudget"]=streaming.ResidentDistricts>25&&streaming.ResidentDistricts<=49&&streaming.LiveGardens<=196&&streaming.LiveTrees<=98;
            phase="homeReturn";
            player.world.ApplyGeneration(seed,player.world.ActiveSettings);player.world.SetStreaming(2,2);player.ReturnHome();director.ResetDefaults();yield return Settled();
            phase="settledHome";yield return new WaitForSecondsRealtime(8);measure=false;
            checks["mainIslandStillWorks"]=director.gardens.All(g=>g.isActiveAndEnabled)&&player.world.OriginX==0&&player.world.OriginZ==0;
            checks["boundedPlantBudget"]=streaming.ResidentDistricts<=25&&streaming.LiveGardens<=100&&streaming.LiveTrees<=50&&streaming.PeakDistricts<=49;
            checks["allMeshesShared"]=streaming.PlantMeshesAreShared&&streaming.TreeMeshesMatchDepth;
            var sorted=frameTimes.OrderBy(x=>x).ToArray();Func<double,double> percentile=p=>sorted.Length==0?-1:sorted[Math.Min(sorted.Length-1,(int)Math.Ceiling((sorted.Length-1)*p))];
            bool performance=rendered>=frameTimes.Count*.95&&percentile(.95)<33.4&&percentile(.99)<50;
            var phaseTimes=phases.ToDictionary(p=>p.Key,p=>new{frames=p.Value.Count,p95=Percentile(p.Value,.95),p99=Percentile(p.Value,.99),maximum=p.Value.Max()});
            var result=new{passed=checks.All(p=>p.Value)&&errors==0&&performance,checks,errors,performance,frames=frameTimes.Count,renderedFrames=rendered,p95=percentile(.95),p99=percentile(.99),maximum=percentile(1),phaseTimes,streaming.LiveGardens,streaming.LiveTrees,streaming.PeakDistricts,streaming.SavedDistricts,streaming.RestoredDistricts,player.world.PeakResident,player.world.PeakPending,gpu=SystemInfo.graphicsDeviceName,width=1600,height=900,note="Explicit URP HDR camera rendering. Public input handlers and real menu callbacks. Includes remote generation, seed replacement and 9/49/25 resident limits. Session snapshots are deleted when the world closes."};
            Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            player.acceptInput=true;if(quit)Application.Quit(result.passed?0:2);else Destroy(this);
        }
        static double Percentile(List<double> values,double p){var sorted=values.OrderBy(x=>x).ToArray();return sorted[Math.Min(sorted.Length-1,(int)Math.Ceiling((sorted.Length-1)*p))];}
        void OnDestroy(){Application.logMessageReceived-=Log;RenderPipelineManager.endCameraRendering-=Rendered;if(target!=null){if(player!=null){player.view.targetTexture=null;player.view.enabled=true;}target.Release();Destroy(target);}}
    }
}
