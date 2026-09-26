using System;
using System.IO;
using System.Linq;
using Boids.Art;
using Boids.Art.Infinite;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

public static class SkyCityWorldBuilder
{
    public const string Root="Assets/Boids/Art/SkyCityWorld";
    public const string ScenePath="Assets/Boids/Scenes/SkyCityWorld.unity";
    static Material CopyMaterial(string source,string name)
    {
        string path=Root+"/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=Object.Instantiate(AssetDatabase.LoadAssetAtPath<Material>(source));material.name=name;AssetDatabase.CreateAsset(material,path);}
        return material;
    }
    [MenuItem("Boids/Sky City World/Build first person world")]
    public static void Build()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Leave Play before building the world.");
        EditorSceneManager.SaveOpenScenes();Directory.CreateDirectory(Root);AssetDatabase.Refresh();
        // Save a scene copy to retain the authored island's renderer/lightmap links.
        var scene=EditorSceneManager.OpenScene(SkyCityHeroBuilder.ScenePath);
        EditorSceneManager.SaveScene(scene,ScenePath);
        var horizon=GameObject.Find("Distant cities - layered silhouettes");if(horizon!=null)Object.DestroyImmediate(horizon);
        var camera=Camera.main;Object.DestroyImmediate(camera.GetComponent<SkyCityCameraRig>());
        Object.DestroyImmediate(camera.GetComponent<RedonFixedFrame>());Object.DestroyImmediate(camera.GetComponent<SkyCityHeroPerformanceProbe>());
        camera.name="First person view";camera.rect=new Rect(0,0,1,1);camera.ResetAspect();camera.fieldOfView=60;camera.nearClipPlane=.12f;camera.farClipPlane=570;
        var arrival=new GameObject("Hanging Gardens - arrival district");
        GameObject.Find("Hanging Gardens - authored architecture").transform.SetParent(arrival.transform,true);
        foreach(var item in scene.GetRootGameObjects())
            if(item.name=="Waterfall spray"||item.name=="Terrace light probes")item.transform.SetParent(arrival.transform,true);
        // Separate physical shells from rendering LOD: ivy, water and pennants do not block movement.
        var collision=new GameObject("Arrival collision shells");collision.transform.SetParent(arrival.transform,false);
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(SkyCityHeroBuilder.Root+"/HangingGardens.fbx");
        foreach(var filter in model.GetComponentsInChildren<MeshFilter>())
        {
            string prefix=filter.name.Substring(0,2);
            if(prefix!="01"&&prefix!="02"&&prefix!="03"&&prefix!="05"&&prefix!="06"&&prefix!="14")continue;
            var go=new GameObject(filter.name);go.transform.SetParent(collision.transform,false);
            go.transform.localPosition=filter.transform.position;go.transform.localRotation=filter.transform.rotation;go.transform.localScale=filter.transform.lossyScale;
            go.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
        }
        string rendererPath=Root+"/WorldRenderer.asset";
        if(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath)==null)AssetDatabase.CopyAsset(SkyCityHeroBuilder.Root+"/GardensRenderer.asset",rendererPath);
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        var clouds=CopyMaterial(SkyCityHeroBuilder.Root+"/Rolling sunlit cloud banks.mat","Endless layered cloud sea");clouds.SetFloat("_Endless",1);
        var atmosphere=renderer.rendererFeatures.OfType<SkyCityAtmosphereFeature>().First();atmosphere.cloudMaterial=clouds;atmosphere.resolutionScale=.8f;atmosphere.Create();
        EditorUtility.SetDirty(clouds);EditorUtility.SetDirty(atmosphere);renderer.SetDirty();EditorUtility.SetDirty(renderer);
        var pipeline=new SerializedObject(GraphicsSettings.currentRenderPipeline);var list=pipeline.FindProperty("m_RendererDataList");int rendererIndex=-1;
        for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==renderer)rendererIndex=i;
        if(rendererIndex<0){rendererIndex=list.arraySize;list.arraySize++;list.GetArrayElementAtIndex(rendererIndex).objectReferenceValue=renderer;pipeline.ApplyModifiedPropertiesWithoutUndo();}
        camera.GetUniversalAdditionalCameraData().SetRenderer(rendererIndex);
        var architecture=CopyMaterial(SkyCityInfiniteBuilder.Root+"/Ivory copper and silk.mat","Weathered ivory city");
        architecture.SetFloat("_StoneRelief",.025f);architecture.SetFloat("_StoneVariation",.22f);EditorUtility.SetDirty(architecture);
        var water=CopyMaterial(SkyCityInfiniteBuilder.Root+"/Infinite reflecting water.mat","Archipelago reflecting pools");
        var cascades=AssetDatabase.LoadAssetAtPath<Material>(SkyCityInfiniteBuilder.Root+"/Living transparent cascades.mat");
        var world=new GameObject("Streaming archipelago").AddComponent<SkyCityInfiniteWorld>();world.view=camera;
        world.architectureMaterial=architecture;world.waterMaterial=water;world.cascadeMaterial=cascades;
        world.reserveArrival=true;world.authoredArrival=arrival.transform;world.layoutOffset=new Vector3(-48,0,-42);
        world.enableCollisions=true;world.showWelcome=false;world.loadRadius=2;world.maximumResidentChunks=25;world.maximumWorkers=2;
        var traveller=new GameObject("First person traveller");traveller.layer=2;
        var controller=traveller.AddComponent<CharacterController>();controller.height=1.75f;controller.radius=.32f;controller.center=new Vector3(0,.875f,0);
        controller.skinWidth=.035f;controller.stepOffset=.3f;controller.minMoveDistance=0;controller.slopeLimit=65;
        camera.transform.SetParent(traveller.transform,false);camera.transform.localPosition=new Vector3(0,1.65f,0);
        traveller.transform.position=new Vector3(5,14.35f,-38);
        var look=Quaternion.LookRotation(new Vector3(-8,12,8)-new Vector3(5,16,-38));
        traveller.transform.rotation=Quaternion.Euler(0,look.eulerAngles.y,0);camera.transform.localRotation=Quaternion.Euler(look.eulerAngles.x,0,0);
        var explorer=traveller.AddComponent<SkyCityFirstPerson>();explorer.view=camera;explorer.world=world;world.travellerRoot=traveller.transform;
        var flock=Object.FindObjectOfType<SkyCityFlock>();flock.freeRoaming=true;flock.acceptPointerInvitations=false;flock.world=world;flock.cruiseSpeed=7.4f;flock.steeringLimit=17;
        explorer.flock=flock;
        explorer.flockMenu=traveller.AddComponent<SkyCityFlockMenu>();
        explorer.flockMenu.font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/SkyCityUI.ttf");
        traveller.AddComponent<SkyCityWorldProbe>().player=explorer;
        var birds=flock.transform.Cast<Transform>().ToArray();
        for(int i=0;i<birds.Length;i++)
        {
            float a=i*2.39996f;birds[i].position=new Vector3(5+Mathf.Cos(a)*24,15+Mathf.Sin(i*1.71f)*8,-20+Mathf.Sin(a)*23);
            birds[i].localScale=Vector3.one*(.34f+(i%7)*.025f);
        }
        var reflection=Object.FindObjectOfType<SkyCityWaterReflection>();reflection.rendererIndex=rendererIndex;reflection.maximumReflectionDistance=240;reflection.textureWidth=768;
        RenderSettings.fogStartDistance=150;RenderSettings.fogEndDistance=490;
        Shader.SetGlobalVector("_SkyWorldOffset",Vector4.zero);
        EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
        if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
        Debug.Log("Sky City World saved: authored arrival, first person flight, callable articulated flock and streamed WFC districts.");
        SkyCityGardenBuilder.Build();
    }
    public static void BuildPlayer()
    {
        Directory.CreateDirectory("Builds/SkyCityWorld");Directory.CreateDirectory("Captures/SkyCityWorld");
        EditorSceneManager.SaveOpenScenes();
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/SkyCityWorld/SkyCityWorld.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        File.WriteAllText("Captures/SkyCityWorld/Build.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,seconds=report.summary.totalTime.TotalSeconds},Newtonsoft.Json.Formatting.Indented));
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new InvalidOperationException("World build failed.");
    }
    public static void Capture(string name)
    {
        Directory.CreateDirectory("Captures/SkyCityWorld");RedonSceneBuilder.CaptureCamera(Camera.main,"Captures/SkyCityWorld/"+name+".png",1920,1080);
    }
}
