using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Boids.Art;

/// <summary>Exercise the public camera controls and soundtrack while recording real scene frames.</summary>
[InitializeOnLoad]
public static class SkyCityPresentationReview
{
    const string Key="Boids.SkyCityPresentationReview";
    static int frame,lastFrame,oldRate,errors;
    static bool oldBackground,audible;
    static Vector3 homePosition;
    static Quaternion homeRotation;
    static float maxPositionChange,minDistance,maxAudioPeak;
    static readonly float[] audioSamples=new float[512];
    public static bool IsRunning => SessionState.GetBool(Key,false);
    static SkyCityPresentationReview(){EditorApplication.playModeStateChanged+=State;}
    [MenuItem("Boids/Sky City/Record Wind Music and Camera")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Leave Play mode first.");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the scene first.");
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static void State(PlayModeStateChange state)
    {
        if(!IsRunning)return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            frame=0;lastFrame=-1;errors=0;maxPositionChange=0;minDistance=1000;maxAudioPeak=0;audible=false;
            oldRate=Time.captureFramerate;oldBackground=Application.runInBackground;
            Time.captureFramerate=30;Application.runInBackground=true;
            homePosition=Camera.main.transform.position;homeRotation=Camera.main.transform.rotation;
            Directory.CreateDirectory("Captures/SkyCityMotionFrames");
            Application.logMessageReceived+=Log;EditorApplication.update+=Tick;
        }
        else if(state==PlayModeStateChange.ExitingPlayMode)Cleanup();
    }
    static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)errors++;}
    static void Tick()
    {
        if(!Application.isPlaying||Time.frameCount==lastFrame)return;
        lastFrame=Time.frameCount;
        try
        {
            var rig=Camera.main.GetComponent<SkyCityCameraRig>();
            var music=UnityEngine.Object.FindObjectOfType<SkyCitySoundscape>().GetComponent<AudioSource>();
            if(frame>=120&&frame<240)rig.Orbit(new Vector2(1.05f,-.26f));
            if(frame==245)rig.Zoom(1.6f);
            if(frame==300)rig.Pan(new Vector2(38,-16));
            if(frame==390)rig.ResetView();
            maxPositionChange=Mathf.Max(maxPositionChange,Vector3.Distance(homePosition,Camera.main.transform.position));
            minDistance=Mathf.Min(minDistance,rig.Distance);
            if(music.isPlaying)
            {
                music.GetOutputData(audioSamples,0);
                foreach(float sample in audioSamples)maxAudioPeak=Mathf.Max(maxAudioPeak,Mathf.Abs(sample));
                audible|=maxAudioPeak>.0001f;
            }
            RedonSceneBuilder.CaptureCamera(Camera.main,"Captures/SkyCityMotionFrames/frame_"+frame.ToString("D4")+".png",1536,1024);
            if(frame==30||frame==105||frame==330)SkyCitySceneBuilder.Capture("SkyCity_Motion_"+frame);
            frame++;if(frame<540)return;
            float resetPositionError=Vector3.Distance(homePosition,Camera.main.transform.position);
            float resetRotationError=Quaternion.Angle(homeRotation,Camera.main.transform.rotation);
            bool passed=errors==0&&maxPositionChange>15&&minDistance<55&&resetPositionError<.02f&&resetRotationError<.05f&&audible&&music.loop&&music.clip.length>60;
            File.WriteAllText("Captures/SkyCity_PresentationValidation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new
            {passed,frames=frame,simulationSeconds=18,errors,maxPositionChange,minDistance,resetPositionError,resetRotationError,
                rig.OrbitInputs,rig.ZoomInputs,rig.PanInputs,rig.ResetInputs,audible,maxAudioPeak,looping=music.loop,musicDuration=music.clip.length,
                listeners=UnityEngine.Object.FindObjectsOfType<AudioListener>().Length},Newtonsoft.Json.Formatting.Indented));
            Cleanup();EditorApplication.isPlaying=false;
            if(!passed)Debug.LogError("Sky City presentation review failed; inspect the report.");
        }
        catch(Exception e){Cleanup();EditorApplication.isPlaying=false;Debug.LogException(e);}
    }
    static void Cleanup()
    {
        Time.captureFramerate=oldRate;Application.runInBackground=oldBackground;
        SessionState.SetBool(Key,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Log;
    }
}
