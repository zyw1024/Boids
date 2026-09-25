using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Boids.Art.Infinite
{
    /// <summary>Opt-in real-time traversal benchmark. Disabled during normal play.</summary>
    public sealed class SkyCityStreamingProbe : MonoBehaviour
    {
        public SkyCityInfiniteWorld world;
        public SkyCityVoyager voyager;
        public bool Running { get; private set; }
        public string LastReport { get; private set; }
        public float ElapsedSeconds { get { return Running && began > 0 ? (float)(Time.realtimeSinceStartupAsDouble-began) : 0; } }
        [Serializable] public sealed class Report
        {
            public bool passed,functionalPassed,performancePassed,standalonePlayer,returnedIdentical,zeroSceneDistrictsAtStart,renderCoveragePassed;
            public string gpu,unityVersion,note;
            public int width,height,frames,renderedFrames,seed,completed,unloaded,cancelled,errors,peakResidents,peakPending,pool,originShifts,fallbacks;
            public double seconds,averageFrameMs,p50FrameMs,p95FrameMs,p99FrameMs,maxFrameMs,p95StreamingCpuMs,maxStreamingCpuMs,maxPatchUploadMs,firstIslandMilliseconds;
            public double gpuP95Ms,ownedMeshPeakMiB,sharedLibraryMiB,unityAllocatedStartMiB,unityAllocatedEndMiB;
            public FrameOutlier[] frameOutliers;
        }
        [Serializable] public sealed class FrameOutlier
        {
            public double elapsedSeconds,frameMs,streamMs;
            public int gcCollections,pending,residents;
            public bool focused;
        }
        readonly List<FrameOutlier> outliers=new List<FrameOutlier>();
        int initialCollections;
        readonly List<double> frameMs=new List<double>(10000),streamMs=new List<double>(10000),gpuMs=new List<double>(10000);
        readonly FrameTiming[] timing=new FrameTiming[1];
        string output;double began,waitingSince;float duration;bool quit,teleported,returned;
        int errors,renderedFrames;ulong originalFingerprint;bool originalKnown;long meshPeak;double memoryStart;
        int teleportBurst;
        void Start()
        {
            string[] args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length;i++)if(args[i]=="-skycity-benchmark")
            {
                string path=Path.Combine(Application.persistentDataPath,"SkyCityInfinite_PlayerPerformance.json");
                for(int j=0;j<args.Length-1;j++)if(args[j]=="-skycity-output")path=args[j+1];
                Begin(path,65,true);break;
            }
        }
        public void Begin(string path,float seconds=65,bool exitWhenDone=false)
        {
            if(Running)throw new InvalidOperationException("A streaming probe is already running.");
            output=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(output));
            duration=seconds;quit=exitWhenDone;Running=true;began=0;waitingSince=Time.realtimeSinceStartupAsDouble;
            errors=0;renderedFrames=0;teleported=returned=false;originalKnown=false;meshPeak=0;teleportBurst=0;
            frameMs.Clear();streamMs.Clear();gpuMs.Clear();voyager.acceptInput=false;voyager.cruise=false;
            outliers.Clear();initialCollections=GC.CollectionCount(0);
            Application.runInBackground=true;Application.logMessageReceived+=OnLog;
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering+=Rendered;
        }
        void Rendered(UnityEngine.Rendering.ScriptableRenderContext context,Camera camera)
        {if(Running&&began>0&&camera==world.view)renderedFrames++;}
        void OnLog(string condition,string stack,LogType type) { if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++; }
        void Update()
        {
            if(!Running)return;
            double now=Time.realtimeSinceStartupAsDouble;
            if(began==0)
            {
                if(now-waitingSince>90) {errors++;Finish();return;}
                if(!world.Ready||world.ResidentCount<world.maximumResidentChunks||world.PendingCount!=0||now-waitingSince<8)return;
                began=now;world.ResetTimingPeaks();originalKnown=world.TryGetFingerprint(new SkyCityWfc.Coord(0,0),out originalFingerprint);
                memoryStart=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576.0;
                return;
            }
            double t=now-began;float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);
            frameMs.Add(Time.unscaledDeltaTime*1000.0);streamMs.Add(world.LastMainThreadMs);meshPeak=Math.Max(meshPeak,world.MeshBytes);
            if(Time.unscaledDeltaTime>.1f)outliers.Add(new FrameOutlier{elapsedSeconds=t,frameMs=Time.unscaledDeltaTime*1000,
                streamMs=world.LastMainThreadMs,gcCollections=GC.CollectionCount(0)-initialCollections,
                pending=world.PendingCount,residents=world.ResidentCount,focused=Application.isFocused});
            FrameTimingManager.CaptureFrameTimings();
            if(FrameTimingManager.GetLatestTimings(1,timing)>0&&timing[0].gpuFrameTime>0)gpuMs.Add(timing[0].gpuFrameTime);
            if(t<duration*.55)
            {
                world.view.transform.position+=new Vector3(.82f,0,.57f)*70*dt;
                world.view.transform.rotation=Quaternion.Slerp(world.view.transform.rotation,Quaternion.LookRotation(new Vector3(.72f,-.32f,.62f)),dt*.4f);
            }
            else if(t<duration*.77)
            {
                if(t<duration*.55+.8)
                {
                    int burst=(int)((t-duration*.55)*30)+1;
                    if(burst!=teleportBurst){world.Teleport(new SkyCityWfc.Coord(1000000+burst*11,-1000000-burst*7),new Vector3(30,65,32));teleportBurst=burst;}
                }
                if(!teleported){world.Teleport(new SkyCityWfc.Coord(1000000,-1000000),new Vector3(30,65,32));teleported=true;}
                world.view.transform.position+=new Vector3(-.7f,0,.7f)*65*dt;
                world.view.transform.rotation=Quaternion.Slerp(world.view.transform.rotation,Quaternion.LookRotation(new Vector3(-.7f,-.32f,.7f)),dt*.8f);
            }
            else if(!returned){voyager.ReturnHome();returned=true;}
            if(t>=duration&&world.PendingCount==0||t>duration+18)Finish();
        }
        void Finish()
        {
            ulong restored;bool identical=originalKnown&&world.TryGetFingerprint(new SkyCityWfc.Coord(0,0),out restored)&&restored==originalFingerprint;
            bool functional=world.Ready&&world.PeakResident<=world.maximumResidentChunks&&world.PeakPending<=world.maximumWorkers+1&&world.PoolCount<=2&&
                world.CompletedChunks>=40&&world.UnloadedChunks>=20&&world.OriginShifts>=2&&world.FailedJobs==0&&errors==0&&identical;
            double p95=Percentile(frameMs,.95),p99=Percentile(frameMs,.99);
            bool coverage=frameMs.Count>0&&renderedFrames>=frameMs.Count*.95;
            bool performance=coverage&&p95<=33.4&&p99<=50&&Percentile(frameMs,1)<=250&&world.PeakUploadMs<=12;
            var report=new Report{passed=functional&&performance,functionalPassed=functional,performancePassed=performance,standalonePlayer=!Application.isEditor,
                returnedIdentical=identical,zeroSceneDistrictsAtStart=world.InitialDistrictObjects==0,gpu=SystemInfo.graphicsDeviceName,unityVersion=Application.unityVersion,
                width=Screen.width,height=Screen.height,frames=frameMs.Count,renderedFrames=renderedFrames,renderCoveragePassed=coverage,seconds=began==0?0:Time.realtimeSinceStartupAsDouble-began,seed=world.seed,
                completed=world.CompletedChunks,unloaded=world.UnloadedChunks,cancelled=world.CancelledJobs,errors=errors+world.FailedJobs,
                peakResidents=world.PeakResident,peakPending=world.PeakPending,pool=world.PoolCount,originShifts=world.OriginShifts,fallbacks=world.Fallbacks,
                averageFrameMs=Average(frameMs),p50FrameMs=Percentile(frameMs,.5),p95FrameMs=p95,p99FrameMs=p99,maxFrameMs=Percentile(frameMs,1),
                firstIslandMilliseconds=world.FirstIslandMilliseconds,
                p95StreamingCpuMs=Percentile(streamMs,.95),maxStreamingCpuMs=world.PeakMainThreadMs,maxPatchUploadMs=world.PeakUploadMs,
                gpuP95Ms=gpuMs.Count==0?-1:Percentile(gpuMs,.95),ownedMeshPeakMiB=meshPeak/1048576.0,sharedLibraryMiB=world.LibraryBytes/1048576.0,
                unityAllocatedStartMiB=memoryStart,unityAllocatedEndMiB=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()/1048576.0,frameOutliers=outliers.ToArray(),
                note="Real elapsed frames after warm-up; continuous travel, rapid million-district teleports, then return. No screenshots or file writes during measured frames. Main-thread budget is a scheduling budget; one atomic mesh upload can exceed it. Owned mesh bytes exclude driver overhead, textures and engine memory. GPU -1 means unavailable. 60 fps cap; functional and performance results are separate."};
            LastReport=JsonUtility.ToJson(report,true);File.WriteAllText(output,LastReport);Running=false;
            Application.logMessageReceived-=OnLog;voyager.acceptInput=true;voyager.SyncAngles();
            UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=Rendered;
            Debug.Log("Sky City streaming probe: "+LastReport);if(quit)Application.Quit(report.passed?0:2);
        }
        static double Average(List<double> values){double total=0;foreach(double v in values)total+=v;return values.Count==0?0:total/values.Count;}
        static double Percentile(List<double> values,double percentile)
        {if(values.Count==0)return 0;var sorted=values.ToArray();Array.Sort(sorted);return sorted[Math.Min(sorted.Length-1,(int)Math.Ceiling((sorted.Length-1)*percentile))];}
        void OnDestroy(){Application.logMessageReceived-=OnLog;UnityEngine.Rendering.RenderPipelineManager.endCameraRendering-=Rendered;}
    }
}
