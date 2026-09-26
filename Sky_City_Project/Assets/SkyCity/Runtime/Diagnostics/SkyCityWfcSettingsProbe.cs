using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SkyCity.Runtime.WorldGeneration;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace SkyCity.Runtime
{
    /// <summary>Opt-in rendered regression for applying settings during streaming.</summary>
    public sealed class SkyCityWfcSettingsProbe : MonoBehaviour
    {
        SkyCityFirstPerson player;SkyCityInfiniteWorld world;SkyCityFlockMenu menu;
        readonly List<double> times=new List<double>();
        readonly Dictionary<string,object> checks=new Dictionary<string,object>();
        string output;bool measure,quit;int frames,errors,peakResident,peakPending;
        double started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoRun()
        {
            var args=Environment.GetCommandLineArgs();if(!args.Contains("-wfc-settings-review"))return;
            var p=FindObjectOfType<SkyCityFirstPerson>();if(p==null)return;
            string path=Path.Combine(Application.persistentDataPath,"WfcSettingsReview.json");
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-wfc-settings-output")path=args[i+1];
            p.gameObject.AddComponent<SkyCityWfcSettingsProbe>().Begin(path,true);
        }
        public void Begin(string path,bool exit=false)
        {
            player=GetComponent<SkyCityFirstPerson>();world=player.world;menu=player.flockMenu;
            output=Path.GetFullPath(path);quit=exit;player.acceptInput=false;player.CapturePointer(false);Application.runInBackground=true;
            Application.logMessageReceived+=Log;RenderPipelineManager.endCameraRendering+=Rendered;StartCoroutine(Run());
        }
        void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
        void Rendered(ScriptableRenderContext c,Camera camera){if(measure&&camera==player.view)frames++;}
        void Update()
        {
            if(!measure)return;times.Add(Time.unscaledDeltaTime*1000);peakResident=Math.Max(peakResident,world.ResidentCount);peakPending=Math.Max(peakPending,world.PendingCount);
        }
        Slider Control(string name)=>menu.GetComponentsInChildren<Slider>(true).First(s=>s.name==name);
        InputField Seed=>menu.GetComponentInChildren<InputField>(true);
        IEnumerator Settle(string phase)
        {
            double until=Time.realtimeSinceStartupAsDouble+90;
            yield return null;
            while(Time.realtimeSinceStartupAsDouble<until&&(!world.GenerationComplete||world.PendingCount>0||world.ResidentCount>world.maximumResidentChunks))yield return null;
            checks[phase]=world.GenerationComplete&&world.PendingCount==0&&world.ResidentCount<=world.maximumResidentChunks;
        }
        IEnumerator Run()
        {
            yield return Settle("warmup");yield return new WaitForSecondsRealtime(2);
            player.ReturnHome();menu.SetOpen(true);menu.ShowWorldSettings(true);
            ulong baseline;world.TryGetFingerprint(new SkyCityWfc.Coord(1,0),out baseline);
            int originalSeed=world.seed,revision=world.GenerationRevision;var original=world.ActiveSettings;
            var eye=player.view.transform.position;var island=world.authoredArrival.position;
            started=Time.realtimeSinceStartupAsDouble;measure=true;world.ResetTimingPeaks();
            Control("GardenWeight").value=4;Control("BridgeProbability").value=1;Control("WfcAttempts").value=1;
            checks["draftDoesNotRegenerate"]=world.GenerationRevision==revision&&world.ActiveSettings.Equals(original);
            Seed.text="";menu.ApplyWorldSettings();checks["invalidSeedRejected"]=world.GenerationRevision==revision;
            Seed.text=(originalSeed+1).ToString();int kept=world.ResidentCount;menu.ApplyWorldSettings();
            checks["oldWorldRetained"]=world.ResidentCount==kept;checks["firstApply"]=world.GenerationRevision==revision+1;
            yield return null;yield return null;
            checks["workersInFlight"]=world.PendingCount>0;
            Seed.text=(originalSeed+2).ToString();Control("BridgeProbability").value=.1f;Control("ShoreCompleteness").value=.35f;Control("TowerWeight").value=4;menu.ApplyWorldSettings();
            yield return Settle("latestRequestFinished");
            var settings=world.ActiveSettings;int seed=world.seed;
            var expected=Task.Run(()=>SkyCityWfc.Solve(new SkyCityWfc.Coord(1,0),seed,CancellationToken.None,settings));
            while(!expected.IsCompleted)yield return null;
            ulong current;checks["latestFingerprint"]=world.TryGetFingerprint(new SkyCityWfc.Coord(1,0),out current)&&current==expected.Result.fingerprint;
            checks["latestRevisionOnly"]=world.CurrentGenerationChunks==world.TargetChunks&&world.GenerationRevision==revision+2;
            Control("LoadRadius").value=3;Control("FrameBudget").value=4;yield return Settle("expandedTo49Limit");
            checks["expandedResidentCount"]=world.ResidentCount>=45&&world.ResidentCount<=49;
            Control("LoadRadius").value=1;Control("FrameBudget").value=.5f;yield return Settle("reducedTo9Limit");
            checks["reducedResidentCount"]=world.ResidentCount<=9;
            menu.ResetWorldDefaults();checks["resetIsDraft"]=world.seed==originalSeed+2;menu.ApplyWorldSettings();yield return Settle("defaultsRestored");
            checks["originalFingerprint"]=world.TryGetFingerprint(new SkyCityWfc.Coord(1,0),out current)&&current==baseline;
            checks["unchangedPlayerAndIsland"]=player.view.transform.position==eye&&world.authoredArrival.position==island;
            checks["pointerReleased"]=!player.Captured;checks["noJobsFailed"]=world.FailedJobs==0;
            measure=false;
            bool functional=checks.Values.All(v=>v is bool&&(bool)v)&&errors==0&&peakResident<=49&&peakPending<=world.maximumWorkers+1;
            double p95=P(.95),p99=P(.99),max=P(1);bool performance=frames>=times.Count*.95&&p95<33.4&&p99<50&&max<250;
            var report=new{passed=functional&&performance,functional,performance,checks,peakResident,peakPending,world.RegeneratedChunks,world.CancelledJobs,
                frames=times.Count,rendered=frames,seconds=Time.realtimeSinceStartupAsDouble-started,p95,p99,maximum=max,world.PeakMainThreadMs,world.PeakUploadMs,
                errors,standalone=!Application.isEditor,width=Screen.width,height=Screen.height,gpu=SystemInfo.graphicsDeviceName};
            Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
            Application.logMessageReceived-=Log;RenderPipelineManager.endCameraRendering-=Rendered;
            menu.SetOpen(false);player.acceptInput=true;if(quit)Application.Quit(report.passed?0:2);else menu.SetOpen(true);
        }
        double P(double q){if(times.Count==0)return -1;var a=times.ToArray();Array.Sort(a);return a[Math.Min(a.Length-1,(int)Math.Ceiling((a.Length-1)*q))];}
        void OnDestroy(){Application.logMessageReceived-=Log;RenderPipelineManager.endCameraRendering-=Rendered;}
    }
}
