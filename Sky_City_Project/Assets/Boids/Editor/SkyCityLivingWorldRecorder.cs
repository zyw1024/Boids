using System;
using System.IO;
using System.Linq;
using Boids.Art;
using Boids.Art.Infinite;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

/// <summary>A reproducible real-time scene tour, recorded by Unity Recorder (not a benchmark).</summary>
public sealed class SkyCityLivingWorldRecorder
{
    static SkyCityLivingWorldRecorder active;
    RecorderController recorder;
    RecorderControllerSettings settings;
    MovieRecorderSettings movie;
    SkyCityFirstPerson player;
    SkyCityGardenDirector garden;
    SkyCityDistrictGardens districts;
    SkyCityLivingGarden remoteBed;
    int frames,lastFrame=-1;
    Vector3 originalPosition;
    Quaternion originalRotation;
    bool originalInput;
    string output;
    const int TotalFrames=2160;
    public static string Status {get;private set;}="Idle";

    [MenuItem("Boids/Sky City World/Record living city tour (Play mode)")]
    public static void Record()
    {
        if(!Application.isPlaying||active!=null)throw new InvalidOperationException("Enter Play and finish the existing take.");
        active=new SkyCityLivingWorldRecorder();
        try{active.Begin();}catch{active.Cleanup();throw;}
    }
    void Begin()
    {
        player=UnityEngine.Object.FindObjectOfType<SkyCityFirstPerson>();
        districts=player.world.GetComponent<SkyCityDistrictGardens>();garden=districts.director;
        originalPosition=player.view.transform.position;originalRotation=player.view.transform.rotation;originalInput=player.acceptInput;
        player.acceptInput=false;player.flockMenu.SetOpen(false);player.CapturePointer(false);player.ReturnHome();garden.ResetDefaults();
        Application.runInBackground=true;
        output=Path.Combine(Path.GetTempPath(),"SkyCityWorld","LivingCityJourney");Directory.CreateDirectory(Path.GetDirectoryName(output));
        settings=ScriptableObject.CreateInstance<RecorderControllerSettings>();settings.SetRecordModeToManual();settings.FrameRate=30;settings.CapFrameRate=true;
        movie=ScriptableObject.CreateInstance<MovieRecorderSettings>();movie.name="Living Sky City - actual Unity scene";movie.Enabled=true;
        movie.EncoderSettings=new CoreEncoderSettings{Codec=CoreEncoderSettings.OutputCodec.MP4,EncodingQuality=CoreEncoderSettings.VideoEncodingQuality.High};
        movie.ImageInputSettings=new GameViewInputSettings{OutputWidth=1920,OutputHeight=1080};movie.CaptureAudio=true;movie.OutputFile=output;
        settings.AddRecorderSettings(movie);recorder=new RecorderController(settings);recorder.PrepareRecording();
        if(!recorder.StartRecording())throw new InvalidOperationException("Recorder did not start.");
        EditorApplication.update+=Tick;Status="Recording 0 / "+TotalFrames;
    }
    void Tick()
    {
        if(!Application.isPlaying||player==null){Cleanup();return;}
        if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
        if(frames>=TotalFrames)
        {
            recorder.StopRecording();Status="Complete: "+output+".mp4";
            File.WriteAllText(output+".json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
                recorder="Unity Recorder 4.0.3",scene="SkyCityWorld",timelineFrames=TotalFrames,fps=30,audio=true,
                camera="Scripted first-person camera inside the actual running scene",performanceBenchmark=false,
                chapters=new[]{"00:00 Arrival and bird call","00:08 Recursive tree growth","00:26 Cellular flower wave","00:34 Live garden controls","00:40 Travel to a streamed district","00:45 Remote living garden","01:04 Infinite archipelago"}
            },Newtonsoft.Json.Formatting.Indented));Cleanup();return;
        }
        float t=frames/30f;Vector3 eye,target;
        if(frames==30)player.CallBirds();
        if(frames==240)garden.ReplayTree();
        if(frames==780){garden.ClearGardens();garden.AwakenGardens();garden.evolutionSpeed=1.4f;}
        if(frames==1020){player.flockMenu.SetOpen(true);player.flockMenu.ShowGardenSettings();}
        if(frames==1080){garden.showRules=true;garden.RefreshAppearance();}
        if(frames==1170){player.flockMenu.SetOpen(false);garden.showRules=false;garden.RefreshAppearance();player.CapturePointer(false);}
        if(frames==1200)player.world.Teleport(new SkyCityWfc.Coord(12,-7),new Vector3(38,68,38));
        if(frames==1350)
        {
            remoteBed=districts.Coordinates.SelectMany(c=>districts.GardensAt(c)).Where(g=>g.transform.Find("Recursive courtyard tree")!=null)
                .OrderBy(g=>(g.transform.position-player.view.transform.position).sqrMagnitude).FirstOrDefault();
            garden.ResetDefaults();garden.ReplayTree();
        }
        if(t<8)
        {eye=player.homeEye;target=player.homeLook;}
        else if(t<26)
        {float a=Mathf.SmoothStep(0,1,(t-8)/18);eye=Vector3.Lerp(new Vector3(-3,12,-12),new Vector3(-12,11,-6),a);target=new Vector3(-20.2f,10,4.65f);}
        else if(t<40)
        {eye=new Vector3(-14,8,-3);target=new Vector3(-19,6.5f,3);}
        else if(t<45)
        {eye=new Vector3(38,68,38);target=new Vector3(40,12,90);}
        else if(t<64&&remoteBed!=null)
        {
            float a=Mathf.SmoothStep(0,1,(t-45)/19);
            target=remoteBed.transform.position+Vector3.up*1.2f;
            eye=target+remoteBed.transform.TransformDirection(Vector3.Lerp(new Vector3(5,3.5f,-1),new Vector3(4,2.7f,-.2f),a));
            if(frames==1410){garden.AwakenGardens();player.CallBirds();}
        }
        else
        {
            var centre=remoteBed!=null?remoteBed.transform.position:new Vector3(38,12,38);
            float a=Mathf.SmoothStep(0,1,(t-64)/8);eye=centre+Vector3.Lerp(new Vector3(-4,5,-9),new Vector3(-34,44,-58),a);target=centre+new Vector3(18,2,30);
        }
        player.SetPose(eye,Quaternion.LookRotation(target-eye));
        if(frames==150)SkyCityWorldBuilder.Capture("LivingCity_Hero");
        if(frames==960)SkyCityWorldBuilder.Capture("LivingCity_Flowers");
        if(frames==1110)ScreenCapture.CaptureScreenshot(Path.GetFullPath("Captures/SkyCityWorld/LivingCity_Menu.png"));
        if(frames==1830)SkyCityWorldBuilder.Capture("LivingCity_RemoteGarden");
        if(frames==2100)SkyCityWorldBuilder.Capture("LivingCity_Archipelago");
        frames++;Status="Recording "+frames+" / "+TotalFrames;
    }
    void Cleanup()
    {
        EditorApplication.update-=Tick;if(recorder!=null)recorder.StopRecording();
        // Exiting Play restores the authored scene; never write runtime poses back into it.
        if(Application.isPlaying&&player!=null){player.flockMenu.SetOpen(false);garden.ResetDefaults();player.ReturnHome();player.SetPose(originalPosition,originalRotation);player.acceptInput=originalInput;}
        if(movie!=null)UnityEngine.Object.DestroyImmediate(movie);if(settings!=null)UnityEngine.Object.DestroyImmediate(settings);active=null;
    }
}
