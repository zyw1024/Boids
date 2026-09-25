using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Boids.Art
{
    /// <summary>Opt-in standalone camera tour; normal scene use has no benchmark overhead.</summary>
    public sealed class SkyCityHeroPerformanceProbe : MonoBehaviour
    {
        [Serializable] public sealed class Report
        {
            public bool passed,standalonePlayer,renderCoverage,reflectionMoved,birdsMoved;
            public string gpu,unityVersion,note;
            public int width,height,frames,renderedFrames,errors,reflectionUpdates;
            public double seconds,p95FrameMs,p99FrameMs,maxFrameMs,gpuP95Ms,allocatedMiB;
        }
        public bool Running {get;private set;}
        readonly List<double> frames=new List<double>(),gpu=new List<double>();
        readonly FrameTiming[] timing=new FrameTiming[1];
        Camera view;SkyCityCameraRig rig;SkyCityWaterReflection reflection;SkyCityFlock flock;
        double start,warm;int rendered,errors,initialReflections;string output;bool quit;
        Vector3 savedPosition;Quaternion savedRotation;float savedFov;
        void Start()
        {
            var args=Environment.GetCommandLineArgs();
            foreach(var arg in args)if(arg=="-gardens-benchmark")
            {
                string path=Path.Combine(Application.persistentDataPath,"HangingGardens_Performance.json");
                for(int i=0;i<args.Length-1;i++)if(args[i]=="-gardens-output")path=args[i+1];
                Begin(path,true);break;
            }
        }
        public void Begin(string path,bool exit=false)
        {
            view=Camera.main;rig=view.GetComponent<SkyCityCameraRig>();reflection=FindObjectOfType<SkyCityWaterReflection>();flock=FindObjectOfType<SkyCityFlock>();
            savedPosition=view.transform.position;savedRotation=view.transform.rotation;savedFov=view.fieldOfView;
            rig.enabled=false;output=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(output));quit=exit;
            Running=true;warm=Time.realtimeSinceStartupAsDouble;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;Application.runInBackground=true;
            Application.logMessageReceived+=Log;RenderPipelineManager.endCameraRendering+=Rendered;
        }
        void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
        void Rendered(ScriptableRenderContext context,Camera camera){if(start>0&&camera==view)rendered++;}
        void Update()
        {
            if(!Running)return;
            double now=Time.realtimeSinceStartupAsDouble;
            if(start==0){if(now-warm<6)return;start=now;initialReflections=reflection.RenderCount;return;}
            float t=(float)(now-start);frames.Add(Time.unscaledDeltaTime*1000.0);FrameTimingManager.CaptureFrameTimings();
            if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].gpuFrameTime>0)gpu.Add(timing[0].gpuFrameTime);
            Vector3 p,target;view.fieldOfView=39;
            if(t<10){p=Vector3.Lerp(savedPosition,savedPosition+new Vector3(9,3,7),t/10);target=new Vector3(-3,10,11);}
            else if(t<20){p=Vector3.Lerp(new Vector3(-11,11,-10),new Vector3(-5,12,-7),(t-10)/10);target=new Vector3(-15,13,11);view.fieldOfView=48;}
            else if(t<30){p=Vector3.Lerp(new Vector3(-7,5.2f,-8),new Vector3(-14,5.4f,-7),(t-20)/10);target=new Vector3(-11,4.2f,4);view.fieldOfView=48;}
            else{p=Vector3.Lerp(new Vector3(37,21,-5),new Vector3(24,22,3),(t-30)/10);target=new Vector3(-11,14,13);view.fieldOfView=48;}
            view.transform.SetPositionAndRotation(p,Quaternion.LookRotation(target-p));
            if(t>=40)Finish();
        }
        static double P(List<double> values,double fraction){if(values.Count==0)return -1;var a=values.ToArray();Array.Sort(a);return a[Math.Min(a.Length-1,(int)Math.Ceiling((a.Length-1)*fraction))];}
        void Finish()
        {
            bool coverage=frames.Count>0&&rendered>=frames.Count*.95;
            var r=new Report{standalonePlayer=!Application.isEditor,renderCoverage=coverage,reflectionMoved=reflection.RenderCount-initialReflections>100,
                birdsMoved=flock.GroupDecisions>8&&flock.PeakBankAngle>10,gpu=SystemInfo.graphicsDeviceName,unityVersion=Application.unityVersion,
                width=Screen.width,height=Screen.height,frames=frames.Count,renderedFrames=rendered,errors=errors,reflectionUpdates=reflection.RenderCount-initialReflections,
                seconds=Time.realtimeSinceStartupAsDouble-start,p95FrameMs=P(frames,.95),p99FrameMs=P(frames,.99),maxFrameMs=P(frames,1),gpuP95Ms=P(gpu,.95),
                allocatedMiB=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576.0,
                note="Real standalone elapsed frames: overview, close architecture, water reflection, side orbit. 6 second warmup. 60 fps cap. No screenshots or writes during timed frames. This measures the authored reference slice, not infinite streaming."};
            r.passed=coverage&&errors==0&&r.reflectionMoved&&r.birdsMoved&&r.p95FrameMs<33.4&&r.p99FrameMs<50;
            File.WriteAllText(output,JsonUtility.ToJson(r,true));Running=false;
            Application.logMessageReceived-=Log;RenderPipelineManager.endCameraRendering-=Rendered;
            view.transform.SetPositionAndRotation(savedPosition,savedRotation);view.fieldOfView=savedFov;rig.enabled=true;
            if(quit)Application.Quit(r.passed?0:2);
        }
        void OnDestroy(){Application.logMessageReceived-=Log;RenderPipelineManager.endCameraRendering-=Rendered;}
    }
}
