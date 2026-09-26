using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using SkyCity.Runtime;

public sealed class SkyCityFirstPersonRecorder
{
    static SkyCityFirstPersonRecorder active;
    RecorderController recorder;RecorderControllerSettings settings;MovieRecorderSettings movie;
    SkyCityFirstPerson player;Vector3 position;Quaternion rotation;
    bool accepted;int frames,lastFrame=-1;string output;
    public static string Status {get;private set;}="Idle";
    public static void Record()
    {
        if(!Application.isPlaying||active!=null)throw new InvalidOperationException("Enter Play and finish the existing take.");
        active=new SkyCityFirstPersonRecorder();active.Begin();
    }
    void Begin()
    {
        player=UnityEngine.Object.FindObjectOfType<SkyCityFirstPerson>();accepted=player.acceptInput;
        position=player.view.transform.position;rotation=player.view.transform.rotation;player.acceptInput=false;
        if(player.flockMenu!=null)player.flockMenu.SetOpen(false);
        player.CapturePointer(false);player.ReturnHome();
        output=Path.Combine(Path.GetTempPath(),"SkyCityWorld","FirstPersonJourney");Directory.CreateDirectory(Path.GetDirectoryName(output));
        settings=ScriptableObject.CreateInstance<RecorderControllerSettings>();settings.SetRecordModeToManual();settings.FrameRate=30;settings.CapFrameRate=true;
        movie=ScriptableObject.CreateInstance<MovieRecorderSettings>();movie.name="Sky City first person journey";movie.Enabled=true;
        movie.EncoderSettings=new CoreEncoderSettings{Codec=CoreEncoderSettings.OutputCodec.MP4,EncodingQuality=CoreEncoderSettings.VideoEncodingQuality.High};
        // SRP camera input cannot composite Screen Space Overlay UI. Record the
        // Game view so the live controls are included in the same rendered take.
        movie.ImageInputSettings=new GameViewInputSettings{OutputWidth=1920,OutputHeight=1080};movie.CaptureAudio=true;movie.OutputFile=output;
        settings.AddRecorderSettings(movie);recorder=new RecorderController(settings);recorder.PrepareRecording();
        if(!recorder.StartRecording()){Cleanup();throw new InvalidOperationException("Recorder did not start.");}
        Status="Recording 0 / 900";EditorApplication.update+=Tick;
    }
    void Tick()
    {
        if(!Application.isPlaying||player==null){Cleanup();return;}
        if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
        if(frames>=900)
        {
            recorder.StopRecording();Status="Complete: "+output+".mp4";
            File.WriteAllText(output+".json","{\"recorder\":\"Unity Recorder 4.0.3\",\"scene\":\"SkyCityWorld\",\"timelineFrames\":900,\"fps\":30,\"audio\":true,\"birdCallAtSecond\":1,\"camera\":\"First person tour\",\"performanceBenchmark\":false}");Cleanup();return;
        }
        float t=frames/30f;Vector3 p,target;
        if(player.flockMenu!=null)
        {
            if(frames==240){player.flockMenu.ShowWorldSettings(false);player.flockMenu.SetOpen(true);}
            if(frames==270)foreach(var slider in player.flockMenu.GetComponentsInChildren<UnityEngine.UI.Slider>())if(slider.name=="BirdCount")slider.value=96;
            if(frames==345){player.flockMenu.ResetDefaults();player.flockMenu.SetOpen(false);}
        }
        if(t<12){p=player.homeEye;target=player.homeLook;if(frames==30)player.CallBirds();}
        else if(t<20){float a=Mathf.SmoothStep(0,1,(t-12)/8);p=Vector3.Lerp(player.homeEye,new Vector3(-8,10,-3),a);target=Vector3.Lerp(player.homeLook,new Vector3(-20,11,13),a);}
        else{float a=Mathf.SmoothStep(0,1,(t-20)/10);p=Vector3.Lerp(new Vector3(-8,10,-3),new Vector3(65,39,-10),a);target=Vector3.Lerp(new Vector3(-20,11,13),new Vector3(152,29,28),a);}
        player.SetPose(p,Quaternion.LookRotation(target-p));frames++;Status="Recording "+frames+" / 900";
    }
    void Cleanup()
    {
        EditorApplication.update-=Tick;if(recorder!=null)recorder.StopRecording();
        if(player!=null)
        {
            if(player.flockMenu!=null){player.flockMenu.ResetDefaults();player.flockMenu.SetOpen(false);}
            player.SetPose(position,rotation);player.acceptInput=accepted;
        }
        if(movie!=null)UnityEngine.Object.DestroyImmediate(movie);if(settings!=null)UnityEngine.Object.DestroyImmediate(settings);active=null;
    }
}
