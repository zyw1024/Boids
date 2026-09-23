using System;
using System.IO;
using Boids.Art;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Reproducible camera frames of real Play-mode movement and the screen-point feeding path.</summary>
[InitializeOnLoad]
public static class RedonPreviewRecorder
{
    const string RunningKey="Boids.Redon.Recording";
    const int FrameCount=420, FramesPerSecond=30;
    static int frame,lastUnityFrame,previousCaptureRate;
    static bool previousBackground;

    static RedonPreviewRecorder(){EditorApplication.playModeStateChanged+=OnPlayState;}

    [MenuItem("Boids/Redon/Record Interaction Preview Frames")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Start recording in Edit mode.");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=RedonSceneBuilder.ScenePath || scene.isDirty)
            throw new InvalidOperationException("Open and save RedonDream before recording.");
        SessionState.SetBool(RunningKey,true);
        EditorApplication.isPlaying=true;
    }

    static void OnPlayState(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(RunningKey,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            frame=0;lastUnityFrame=-1;
            previousCaptureRate=Time.captureFramerate;previousBackground=Application.runInBackground;
            Time.captureFramerate=FramesPerSecond;Application.runInBackground=true;
            Directory.CreateDirectory("Captures/InteractionFrames");
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
        else if(state==PlayModeStateChange.ExitingPlayMode)Cleanup();
    }

    static void Tick()
    {
        if(!Application.isPlaying || Time.frameCount==lastUnityFrame)return;
        var school=UnityEngine.Object.FindObjectOfType<DreamSchoolController>();
        if(school==null || school.AgentCount==0)return;
        lastUnityFrame=Time.frameCount;
        try
        {
            if(frame==75)school.TryFeedScreenPoint(Camera.main.WorldToScreenPoint(new Vector3(0,5.6f,2.4f)));
            if(frame==285)school.TryFeedScreenPoint(Camera.main.WorldToScreenPoint(new Vector3(3.4f,6.5f,2.4f)));
            RedonSceneBuilder.CaptureCamera(Camera.main,"Captures/InteractionFrames/frame_"+frame.ToString("D4")+".png",960,640);
            frame++;
            if(frame<FrameCount)return;
            File.WriteAllText("Captures/Redon_Preview.json",JsonUtility.ToJson(new PreviewInfo
            {
                frames=frame,fps=FramesPerSecond,consumedPortions=school.ConsumedPortions,
                completedFeedings=school.CompletedFeedings,width=960,height=640
            },true));
            Cleanup();EditorApplication.isPlaying=false;
            Debug.Log("Redon interaction preview captured: 420 actual Unity camera frames at 30 fps.");
        }
        catch(Exception error)
        {
            Cleanup();EditorApplication.isPlaying=false;Debug.LogException(error);
        }
    }

    static void Cleanup()
    {
        Time.captureFramerate=previousCaptureRate;Application.runInBackground=previousBackground;
        SessionState.SetBool(RunningKey,false);EditorApplication.update-=Tick;
    }

    [Serializable] sealed class PreviewInfo
    {
        public int frames,fps,width,height,consumedPortions,completedFeedings;
    }
}
