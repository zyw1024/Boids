using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Boids.Art;

/// <summary>Capture real Play-mode frames while checking the visible flock and interaction.</summary>
[InitializeOnLoad]
public static class SkyCityReview
{
    const string Key="Boids.SkyCityReview";
    const string LongKey="Boids.SkyCityLongReview";
    static int frame,lastFrame,previousRate,minVisible,errors;
    static bool previousBackground;
    static float startTime,minWing,maxWing,maxTravel,minClearance;
    static int maxGliding;
    static Transform[] birds;
    static Vector3[] initial;
    static Vector3 cameraPosition;
    static Quaternion cameraRotation;
    public static bool IsRunning => SessionState.GetBool(Key,false);
    static bool IsLong => SessionState.GetBool(LongKey,false);
    static SkyCityReview(){EditorApplication.playModeStateChanged+=State;}

    [MenuItem("Boids/Sky City/Record and Validate Flight")]
    public static void Run()
    {
        SessionState.SetBool(LongKey,false);StartReview();
    }
    [MenuItem("Boids/Sky City/Validate Two Minute Autonomous Flight")]
    public static void RunLoopVerification()
    {
        SessionState.SetBool(LongKey,true);StartReview();
    }
    static void StartReview()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Leave Play mode first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!=SkyCitySceneBuilder.ScenePath||scene.isDirty)throw new InvalidOperationException("Open and save SkyCity first.");
        SkyCitySceneBuilder.Validate();SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange state)
    {
        if(!IsRunning)return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            frame=0;lastFrame=-1;minVisible=64;errors=0;minWing=1000;maxWing=-1000;maxTravel=0;minClearance=100;maxGliding=0;
            previousRate=Time.captureFramerate;previousBackground=Application.runInBackground;
            Time.captureFramerate=30;Application.runInBackground=true;
            birds=UnityEngine.Object.FindObjectsOfType<Transform>().Where(t=>t.name.StartsWith("Garden Swallow ")).OrderBy(t=>t.name).ToArray();
            initial=birds.Select(t=>t.position).ToArray();cameraPosition=Camera.main.transform.position;cameraRotation=Camera.main.transform.rotation;
            Directory.CreateDirectory("Captures/SkyCityFrames");Application.logMessageReceived+=Log;
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
        else if(state==PlayModeStateChange.ExitingPlayMode)Cleanup();
    }
    static void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors++;}
    static void Tick()
    {
        if(!Application.isPlaying||Time.frameCount==lastFrame)return;
        var flock=UnityEngine.Object.FindObjectOfType<SkyCityFlock>();if(flock==null||flock.BirdCount!=64)return;
        lastFrame=Time.frameCount;
        try
        {
            if(frame==0)startTime=Time.time;
            if(!IsLong&&frame==165)flock.InviteScreenPoint(Camera.main.WorldToScreenPoint(new Vector3(15,8,2)));
            if(!IsLong&&frame==360)flock.InviteScreenPoint(Camera.main.WorldToScreenPoint(new Vector3(7,14,2)));
            maxGliding=Mathf.Max(maxGliding,flock.GlidingBirds);
            minWing=Mathf.Min(minWing,flock.WingAngle);maxWing=Mathf.Max(maxWing,flock.WingAngle);
            int visible=0;
            for(int j=0;j<birds.Length;j++)
            {
                var p=birds[j].position;if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z))throw new Exception("Nonfinite bird position");
                maxTravel=Mathf.Max(maxTravel,Vector3.Distance(initial[j],p));
                if(frame>30)minClearance=Mathf.Min(minClearance,SkyCityFlock.Clearance(p));
                var v=Camera.main.WorldToViewportPoint(p);if(v.z>0&&v.x>0&&v.x<1&&v.y>0&&v.y<1)visible++;
            }
            minVisible=Mathf.Min(minVisible,visible);
            if(!IsLong)RedonSceneBuilder.CaptureCamera(Camera.main,"Captures/SkyCityFrames/frame_"+frame.ToString("D4")+".png",1536,1024);
            else if(frame==1799||frame==3599)SkyCitySceneBuilder.Capture("SkyCity_Loop_"+frame);
            frame++;
            if(frame<(IsLong?3600:540))return;
            bool fixedCamera=Vector3.Distance(cameraPosition,Camera.main.transform.position)<.0001f&&Quaternion.Angle(cameraRotation,Camera.main.transform.rotation)<.001f;
            bool outsideRejected=!flock.InviteScreenPoint(new Vector2(-50,-50));
            var reflection=UnityEngine.Object.FindObjectOfType<SkyCityWaterReflection>();
            bool liveReflection=reflection!=null&&reflection.ReflectionTexture!=null&&reflection.RenderCount>100;
            bool passed=liveReflection&&flock.GroupDecisions>0&&flock.PeakTurnRate>40&&maxGliding>0&&minClearance>.85f&&birds.Length==64&&maxTravel>5&&maxWing-minWing>20&&minVisible>=55&&flock.WindInvitations==(IsLong?0:2)&&outsideRejected&&fixedCamera&&errors==0;
            var report=new{passed,frames=frame,fps=30,width=1536,height=1024,offlineCapture=true,simulationSeconds=Time.time-startTime,
                birds=birds.Length,minVisible,maxTravel,wingRange=maxWing-minWing,windInvitations=flock.WindInvitations,outsideRejected,fixedCamera,errors,minClearance,maxGliding,liveReflection,reflectionRenders=reflection.RenderCount,
                peakTurnRate=flock.PeakTurnRate,peakBankAngle=flock.PeakBankAngle,minSpeed=flock.MinFlightSpeed,maxSpeed=flock.MaxFlightSpeed,groupDecisions=flock.GroupDecisions};
            File.WriteAllText(IsLong?"Captures/SkyCity_LongFlightValidation.json":"Captures/SkyCity_RuntimeValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
            Cleanup();EditorApplication.isPlaying=false;
            if(!passed)Debug.LogError("SkyCity flight review failed: inspect Captures/SkyCity_RuntimeValidation.json");
            else Debug.Log("SkyCity flight review passed.");
        }
        catch(Exception e){Cleanup();EditorApplication.isPlaying=false;Debug.LogException(e);}
    }
    static void Cleanup()
    {
        Time.captureFramerate=previousRate;Application.runInBackground=previousBackground;
        SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
    }
}
