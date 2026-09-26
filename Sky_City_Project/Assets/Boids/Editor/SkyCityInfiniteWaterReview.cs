using System;
using System.IO;
using Boids.Art;
using Boids.Art.Infinite;
using UnityEditor;
using UnityEngine;

/// <summary>Records actual water, spray and live reflection at a fixed simulation rate.</summary>
public static class SkyCityInfiniteWaterReview
{
    static int frame,lastFrame,oldRate,reflections;
    static bool oldInput;
    static SkyCityVoyager voyager;
    static SkyCityWaterReflection reflection;
    public static bool Running { get; private set; }
    public static void Record()
    {
        if(Running||!Application.isPlaying)throw new InvalidOperationException("Run the infinite scene first.");
        voyager=UnityEngine.Object.FindObjectOfType<SkyCityVoyager>();
        if(!voyager.world.Ready||voyager.world.PendingCount!=0)throw new InvalidOperationException("Wait for the nearby islands.");
        reflection=UnityEngine.Object.FindObjectOfType<SkyCityWaterReflection>();
        oldInput=voyager.acceptInput;voyager.acceptInput=false;voyager.cruise=false;
        oldRate=Time.captureFramerate;Time.captureFramerate=30;
        frame=0;lastFrame=-1;reflections=reflection.RenderCount;Running=true;
        Directory.CreateDirectory("Captures/SkyCityInfiniteWaterFrames");
        EditorApplication.update+=Tick;
    }
    static void Tick()
    {
        if(!Application.isPlaying||voyager==null){Cleanup();return;}
        if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
        try
        {
            RedonSceneBuilder.CaptureCamera(Camera.main,"Captures/SkyCityInfiniteWaterFrames/frame_"+frame.ToString("D4")+".png",1536,1024);
            if(frame==15)File.Copy("Captures/SkyCityInfiniteWaterFrames/frame_0015.png","Captures/SkyCityInfinite_Water.png",true);
            if(++frame<150)return;
            int updates=reflection.RenderCount-reflections;
            File.WriteAllText("Captures/SkyCityInfinite_WaterValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new
            {passed=updates>=60,frames=frame,simulationSeconds=5,liveReflectionUpdates=updates,
             residentIslands=voyager.world.ResidentCount,failedJobs=voyager.world.FailedJobs,
             note="Actual Unity render frames; fixed simulation time for motion review. This capture is not a performance measurement."},Newtonsoft.Json.Formatting.Indented));
            Cleanup();
        }
        catch(Exception e){Cleanup();Debug.LogException(e);}
    }
    static void Cleanup()
    {
        EditorApplication.update-=Tick;Time.captureFramerate=oldRate;
        if(voyager!=null)voyager.acceptInput=oldInput;Running=false;
    }
}
