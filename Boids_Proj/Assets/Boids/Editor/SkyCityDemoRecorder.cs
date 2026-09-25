using System;
using System.IO;
using Boids.Art.Infinite;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

/// <summary>Reproducible live Unity Recorder take. No scene assets are modified.</summary>
public sealed class SkyCityDemoRecorder
{
    static SkyCityDemoRecorder active;
    int lastFrame=-1;
    RecorderController recorder;
    RecorderControllerSettings settings;
    MovieRecorderSettings movie;
    SkyCityInfiniteWorld world;
    SkyCityVoyager voyager;
    Camera view;
    Vector3 savedPosition;
    Quaternion savedRotation;
    SkyCityWfc.Coord savedOrigin;
    float savedFov, duration, started;
    bool savedInput, savedCruise, recording;
    int frames, targetFrames;
    string output;
    public static string Status { get; private set; } = "Idle";

    [MenuItem("Boids/Sky City Infinite/Record classroom demo (80 seconds)")]
    public static void RecordDemo()
    {
        // The Windows encoder timestamp pass fails on some non-ASCII project paths.
        Begin(Path.Combine(Path.GetTempPath(),"SkyCityClassroom","SkyCity_Demo_Recorder"),80);
    }

    public static void Begin(string outputStem, float seconds)
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter SkyCityInfinite Play mode first.");
        if (active!=null) throw new InvalidOperationException("A take is already active.");
        active=new SkyCityDemoRecorder();
        try { active.Initialize(outputStem,seconds);EditorApplication.update+=active.Tick; }
        catch { active=null;throw; }
    }

    void Initialize(string stem,float seconds)
    {
        view=Camera.main; world=UnityEngine.Object.FindObjectOfType<SkyCityInfiniteWorld>();
        if(view==null||world==null) throw new InvalidOperationException("Sky City camera/world missing.");
        voyager=view.GetComponent<SkyCityVoyager>();
        savedPosition=view.transform.position;savedRotation=view.transform.rotation;savedFov=view.fieldOfView;
        savedOrigin=new SkyCityWfc.Coord(world.OriginX,world.OriginZ);
        savedInput=voyager.acceptInput;savedCruise=voyager.cruise;voyager.acceptInput=false;voyager.cruise=false;
        duration=Mathf.Max(1,seconds);targetFrames=Mathf.RoundToInt(duration*30);output=Path.GetFullPath(stem);
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        world.Teleport(new SkyCityWfc.Coord(0,0),new Vector3(80,47,-64));
        Pose(0);started=Time.realtimeSinceStartup;Status="Warming live world";
    }

    void Tick()
    {
        if(!Application.isPlaying||view==null){Cleanup();return;}
        if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
        try { UpdateTake(); }
        catch(Exception e) {Status="Failed: "+e.Message;Cleanup();Debug.LogException(e);}
    }

    void UpdateTake()
    {
        if(world==null)return;
        if(!recording)
        {
            if(!world.Ready||world.PendingCount!=0||world.ResidentCount<25||Time.realtimeSinceStartup-started<5)return;
            settings=ScriptableObject.CreateInstance<RecorderControllerSettings>();
            settings.SetRecordModeToManual();settings.FrameRate=30;settings.CapFrameRate=true;
            movie=ScriptableObject.CreateInstance<MovieRecorderSettings>();movie.name="Sky City live classroom take";movie.Enabled=true;
            movie.EncoderSettings=new CoreEncoderSettings { Codec=CoreEncoderSettings.OutputCodec.MP4,EncodingQuality=CoreEncoderSettings.VideoEncodingQuality.High };
            movie.ImageInputSettings=new CameraInputSettings { Source=ImageSource.MainCamera,OutputWidth=1920,OutputHeight=1080,CaptureUI=false };
            movie.CaptureAudio=true;movie.CaptureAlpha=false;movie.OutputFile=output;
            settings.AddRecorderSettings(movie);recorder=new RecorderController(settings);
            var music=UnityEngine.Object.FindObjectOfType<Boids.Art.SkyCitySoundscape>();
            if(music!=null){var audio=music.GetComponent<AudioSource>();audio.time=0;audio.Play();}
            recorder.PrepareRecording();
            if(!recorder.StartRecording()){Status="Recorder failed to start";Cleanup();return;}
            recording=true;frames=0;Status="Recording";return;
        }
        if(frames>=targetFrames)
        {
            recorder.StopRecording();recording=false;
            Status="Complete: "+output+".mp4 ("+frames+" frames)";
            File.WriteAllText(output+".json","{\"recorder\":\"com.unity.recorder@4.0.3\",\"scene\":\"SkyCityInfinite\",\"width\":1920,\"height\":1080,\"fps\":30,\"requestedFrames\":"+frames+",\"audio\":true,\"captureIsPerformanceBenchmark\":false}");
            Debug.Log(Status);Cleanup();return;
        }
        Pose(frames/30f);frames++;Status="Recording "+frames+" / "+targetFrames;
    }

    void Pose(float t)
    {
        // Camera cuts are part of this recorded take. Every frame is rendered by the running scene.
        Vector3 p,target;float u;
        if(t<22){u=t/22;p=Vector3.Lerp(new Vector3(80,47,-64),new Vector3(105,52,-37),u);target=new Vector3(42,33,42);}
        else if(t<40){u=(t-22)/18;p=Vector3.Lerp(new Vector3(81,43,4),new Vector3(107,48,44),u);target=new Vector3(42,35,42);}
        else if(t<56){u=(t-40)/16;p=Vector3.Lerp(new Vector3(117,62,71),new Vector3(127,54,95),u);target=new Vector3(42,24,126);}
        else {u=(t-56)/24;p=Vector3.Lerp(new Vector3(110,75,-25),new Vector3(120,90,535),u);target=p+new Vector3(-60,-19,116);}
        Vector3 origin=new Vector3(world.OriginX*168f,0,world.OriginZ*168f);
        view.transform.position=p-origin;view.transform.rotation=Quaternion.LookRotation(target-p);view.fieldOfView=46;
    }

    void Cleanup()
    {
        EditorApplication.update-=Tick;active=null;
        if(recorder!=null)recorder.StopRecording();
        if(view!=null&&world!=null){world.Teleport(savedOrigin,savedPosition);view.transform.rotation=savedRotation;view.fieldOfView=savedFov;}
        if(voyager!=null){voyager.acceptInput=savedInput;voyager.cruise=savedCruise;voyager.SyncAngles();}
        if(movie!=null)UnityEngine.Object.DestroyImmediate(movie);if(settings!=null)UnityEngine.Object.DestroyImmediate(settings);
    }
}
