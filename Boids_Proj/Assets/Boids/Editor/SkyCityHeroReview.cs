using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Boids.Art;

public sealed class SkyCityHeroReview
{
    public static void Validate()
    {
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=SkyCityHeroBuilder.ScenePath)throw new InvalidOperationException("Open the Hanging Gardens reference.");
        var all=UnityEngine.Object.FindObjectsOfType<Transform>(true);
        int missing=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        var shaders=UnityEngine.Object.FindObjectsOfType<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.shader)
            .Concat(new[]{Shader.Find("Boids/SkyCity/Hanging Gardens Clouds")}).Distinct();
        var errors=shaders.SelectMany(s=>ShaderUtil.GetShaderMessages(s)).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).Select(m=>m.message).ToArray();
        var lod=GameObject.Find("Hanging Gardens - authored architecture").GetComponent<LODGroup>();
        var reflection=UnityEngine.Object.FindObjectOfType<SkyCityWaterReflection>();
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(SkyCityHeroBuilder.Root+"/GardensRenderer.asset");
        bool clouds=renderer.rendererFeatures.OfType<SkyCityAtmosphereFeature>().Any(f=>f.isActive&&f.cloudMaterial.GetTexture("_NoiseTex") is Texture3D&&f.cloudMaterial.GetTexture("_CloudField") is Texture3D);
        var geometries=lod.GetLODs().Select(l=>l.renderers.OfType<MeshRenderer>().Select(r=>r.GetComponent<MeshFilter>().sharedMesh).Where(m=>m!=null).ToArray()).ToArray();
        var counts=geometries.Select(ms=>ms.Sum(m=>(long)m.GetIndexCount(0)/3)).ToArray();
        var report=new {passed=missing==0&&errors.Length==0&&counts.Length==3&&counts[0]>counts[1]&&counts[1]>counts[2]&&clouds&&LightmapSettings.lightmaps.Length>0,
            scene=SkyCityHeroBuilder.ScenePath,missingScripts=missing,shaderErrors=errors,lodTriangles=counts,volumetricClouds=clouds,
            lightmaps=LightmapSettings.lightmaps.Length,lightProbes=LightmapSettings.lightProbes==null?0:LightmapSettings.lightProbes.count,
            planarReflection=reflection!=null&&reflection.ReflectionTexture!=null,birds=all.Count(t=>t.name.StartsWith("Garden Swallow")),
            note="Structural/rendering checks only. Artistic acceptance still requires overview, close architecture, moving water and side-view review. Infinite streaming is a separate scene."};
        Directory.CreateDirectory("Captures/HangingGardens");File.WriteAllText("Captures/HangingGardens/Validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        if(!report.passed)throw new InvalidOperationException("Hanging Gardens validation failed.");
    }
    static SkyCityHeroReview active;
    RecorderController recorder;RecorderControllerSettings settings;MovieRecorderSettings movie;
    Camera camera;SkyCityCameraRig rig;Vector3 savedPosition;Quaternion savedRotation;float savedFov;
    int frames,lastFrame=-1;string output;
    public static string Status {get;private set;}="Idle";
    public static void Record()
    {
        if(!Application.isPlaying||active!=null)throw new InvalidOperationException("Enter Play mode and finish any existing take.");
        active=new SkyCityHeroReview();active.Begin();
    }
    void Begin()
    {
        camera=Camera.main;rig=camera.GetComponent<SkyCityCameraRig>();rig.enabled=false;
        savedPosition=camera.transform.position;savedRotation=camera.transform.rotation;savedFov=camera.fieldOfView;
        output=Path.Combine(Path.GetTempPath(),"HangingGardens","GardensLive");Directory.CreateDirectory(Path.GetDirectoryName(output));
        settings=ScriptableObject.CreateInstance<RecorderControllerSettings>();settings.SetRecordModeToManual();settings.FrameRate=30;settings.CapFrameRate=true;
        movie=ScriptableObject.CreateInstance<MovieRecorderSettings>();movie.name="Hanging Gardens live URP review";movie.Enabled=true;
        movie.EncoderSettings=new CoreEncoderSettings{Codec=CoreEncoderSettings.OutputCodec.MP4,EncodingQuality=CoreEncoderSettings.VideoEncodingQuality.High};
        movie.ImageInputSettings=new CameraInputSettings{Source=ImageSource.MainCamera,OutputWidth=1800,OutputHeight=1200,CaptureUI=false};movie.CaptureAudio=true;movie.OutputFile=output;
        settings.AddRecorderSettings(movie);recorder=new RecorderController(settings);recorder.PrepareRecording();
        if(!recorder.StartRecording()){Cleanup();throw new InvalidOperationException("Recorder could not start.");}
        Status="Recording 0 / 600";EditorApplication.update+=Tick;
    }
    void Tick()
    {
        if(!Application.isPlaying||camera==null){Cleanup();return;}
        if(Time.frameCount==lastFrame)return;lastFrame=Time.frameCount;
        if(frames>=600)
        {
            recorder.StopRecording();Status="Complete: "+output+".mp4";
            File.WriteAllText(output+".json","{\"recorder\":\"Unity Recorder 4.0.3\",\"scene\":\"SkyCityHangingGardens\",\"frames\":600,\"fps\":30,\"audio\":true,\"performanceBenchmark\":false}");Cleanup();return;
        }
        float t=frames/30f;Vector3 p,target;
        if(t<10){p=Vector3.Lerp(new Vector3(5,24,-72),new Vector3(18,25,-66),t/10);target=new Vector3(0,9.5f,11);camera.fieldOfView=37;}
        else if(t<15){p=Vector3.Lerp(new Vector3(-13,12,-10),new Vector3(-8,13,-8),(t-10)/5);target=new Vector3(-20,14,12);camera.fieldOfView=48;}
        else {p=Vector3.Lerp(new Vector3(-6,6.6f,-11),new Vector3(-13,6.3f,-10),(t-15)/5);target=new Vector3(-11,4,-.5f);camera.fieldOfView=48;}
        camera.transform.SetPositionAndRotation(p,Quaternion.LookRotation(target-p));frames++;Status="Recording "+frames+" / 600";
    }
    void Cleanup()
    {
        EditorApplication.update-=Tick;if(recorder!=null)recorder.StopRecording();
        if(camera!=null){camera.transform.SetPositionAndRotation(savedPosition,savedRotation);camera.fieldOfView=savedFov;}
        if(rig!=null)rig.enabled=true;
        if(movie!=null)UnityEngine.Object.DestroyImmediate(movie);if(settings!=null)UnityEngine.Object.DestroyImmediate(settings);active=null;
    }
}
