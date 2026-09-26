using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using SkyCity.Runtime.WorldGeneration;

namespace SkyCity.Runtime
{
    /// <summary>Opt-in exploration, collision, recall and streaming regression in a rendered player.</summary>
    public sealed class SkyCityWorldProbe : MonoBehaviour
    {
        public SkyCityFirstPerson player;
        public bool Running {get;private set;}
        public string Status {get;private set;}="Idle";
        readonly List<double> frameMs=new List<double>(),gpuMs=new List<double>();
        readonly FrameTiming[] timing=new FrameTiming[1];
        double requestedAt,began;
        string output;bool quit,called,released,travelled,returned,teleported,collision,look,wheel,movement;
        float initialDistance,bestDistance=10000;
        int errors,rendered,reflectionStart;ulong initialHash;bool hashKnown;
        SkyCityInfiniteWorld world;SkyCityWaterReflection reflection;
        void Start()
        {
            var args=Environment.GetCommandLineArgs();
            foreach(var arg in args)if(arg=="-skyworld-review")
            {
                string path=Path.Combine(Application.persistentDataPath,"SkyCityFirstPersonRecorder.json");
                for(int i=0;i<args.Length-1;i++)if(args[i]=="-skyworld-output")path=args[i+1];
                Begin(path,true);break;
            }
        }
        public void Begin(string path,bool exit=false)
        {
            if(Running)throw new InvalidOperationException("World review is already running.");
            world=player.world;reflection=FindObjectOfType<SkyCityWaterReflection>();output=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(output));quit=exit;
            player.acceptInput=false;player.CapturePointer(false);player.ReturnHome();Application.runInBackground=true;
            requestedAt=Time.realtimeSinceStartupAsDouble;Running=true;Status="Preparing districts";
            Application.logMessageReceived+=Log;RenderPipelineManager.endCameraRendering+=Rendered;
        }
        void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
        void Rendered(ScriptableRenderContext context,Camera c){if(Running&&began>0&&c==player.view)rendered++;}
        void InputChecks()
        {
            var p=player.view.transform.position;var q=player.view.transform.rotation;
            player.Look(new Vector2(12,5));look=Quaternion.Angle(q,player.view.transform.rotation)>10;player.SetPose(p,q);
            float speed=player.speed;player.ChangeSpeed(5);wheel=player.speed>speed;player.speed=speed;
            player.SetPose(new Vector3(4,50,-36),Quaternion.identity);
            for(int i=0;i<60;i++)player.Move(Vector3.forward,1f/60);
            movement=player.view.transform.position.z>-31;
            var start=new Vector3(-23,17,-10);RaycastHit hit;
            if(Physics.Raycast(start,Vector3.forward,out hit,70,1,QueryTriggerInteraction.Ignore))
            {
                player.SetPose(start,Quaternion.identity);player.speed=36;int contacts=player.Contacts;
                for(int i=0;i<150;i++)player.Move(Vector3.forward,1f/60);
                collision=player.Contacts>contacts&&player.view.transform.position.z-start.z<hit.distance+.7f;
            }
            player.speed=speed;player.ReturnHome();
        }
        void Update()
        {
            if(!Running)return;double now=Time.realtimeSinceStartupAsDouble;
            if(began==0)
            {
                if(now-requestedAt>90){errors++;Finish();return;}
                if(!world.Ready||world.ResidentCount<20||world.PendingCount>0||now-requestedAt<8)return;
                InputChecks();hashKnown=world.TryGetFingerprint(new SkyCityWfc.Coord(1,0),out initialHash);
                reflectionStart=reflection.RenderCount;world.ResetTimingPeaks();began=now;return;
            }
            double t=now-began;frameMs.Add(Time.unscaledDeltaTime*1000);
            FrameTimingManager.CaptureFrameTimings();if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].gpuFrameTime>0)gpuMs.Add(timing[0].gpuFrameTime);
            if(!called&&t>1){called=player.CallBirds();initialDistance=player.flock.MeanInvitationDistance;}
            if(t>3&&t<18)bestDistance=Mathf.Min(bestDistance,player.flock.MeanInvitationDistance);
            if(t>20&&!player.flock.InvitationActive)released=true;
            if(t>21&&t<34)
            {
                if(!travelled){player.SetPose(new Vector3(10,134,-28),Quaternion.LookRotation(new Vector3(.9f,0,.44f)));player.speed=36;travelled=true;}
                player.Move(Vector3.forward,Time.unscaledDeltaTime,true);
            }
            if(t>=34&&t<42)
            {
                if(!teleported){world.Teleport(new SkyCityWfc.Coord(1000000,-1000000),new Vector3(0,134,0));teleported=true;}
                player.Move(Vector3.forward,Time.unscaledDeltaTime,true);
            }
            if(t>=42&&!returned){player.ReturnHome();returned=true;}
            Status=t<21?"Checking bird recall":t<42?"Crossing streamed districts":"Returning to the gardens";
            if(t>=52&&world.PendingCount==0||t>=65)Finish();
        }
        static double P(List<double> values,double p)
        {if(values.Count==0)return -1;var a=values.ToArray();Array.Sort(a);return a[Math.Min(a.Length-1,(int)Math.Ceiling((a.Length-1)*p))];}
        void Finish()
        {
            ulong hash;bool deterministic=hashKnown&&world.TryGetFingerprint(new SkyCityWfc.Coord(1,0),out hash)&&hash==initialHash;
            bool gathered=called&&bestDistance<12&&bestDistance<initialDistance*.75f;
            bool bounded=world.PeakResident<=world.maximumResidentChunks&&world.PeakPending<=world.maximumWorkers+1&&world.PoolCount<=2;
            bool restored=returned&&world.OriginX==0&&world.OriginZ==0&&world.authoredArrival.position.sqrMagnitude<.001f;
            bool functional=look&&wheel&&movement&&collision&&gathered&&released&&deterministic&&bounded&&restored&&world.OriginShifts>=2&&world.UnloadedChunks>10&&world.FailedJobs==0&&errors==0;
            bool coverage=frameMs.Count>0&&rendered>=frameMs.Count*.95;
            bool performance=coverage&&P(frameMs,.95)<33.4&&P(frameMs,.99)<50&&P(frameMs,1)<250;
            var report=new{passed=functional&&performance,functional,performance,standalone=!Application.isEditor,look,wheel,movement,collision,
                gathered,released,initialBirdDistance=initialDistance,bestBirdDistance=bestDistance,deterministic,bounded,restored,
                world.CompletedChunks,world.UnloadedChunks,world.OriginShifts,world.PeakResident,world.PeakPending,world.CollisionChunks,world.PeakMainThreadMs,world.PeakUploadMs,
                frames=frameMs.Count,renderedFrames=rendered,seconds=began>0?Time.realtimeSinceStartupAsDouble-began:0,p95=P(frameMs,.95),p99=P(frameMs,.99),maximum=P(frameMs,1),gpuP95=P(gpuMs,.95),
                reflectionUpdates=reflection.RenderCount-reflectionStart,errors,width=Screen.width,height=Screen.height,gpu=SystemInfo.graphicsDeviceName,
                note="Public input handlers exercised before timing; real rendered traversal, collision, bird convergence/release, origin shifts, million-coordinate teleport and deterministic return. No screenshots during timing. Physical mouse/keyboard checks are separate."};
            File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
            Running=false;Status="Complete: "+output;Application.logMessageReceived-=Log;RenderPipelineManager.endCameraRendering-=Rendered;
            player.speed=9;player.acceptInput=true;if(quit)Application.Quit(report.passed?0:2);
        }
        void OnDestroy(){Application.logMessageReceived-=Log;RenderPipelineManager.endCameraRendering-=Rendered;}
    }
}
