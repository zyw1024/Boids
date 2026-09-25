using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Boids.Art;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

public static class SkyCitySceneBuilder
{
    public const string Root="Assets/Boids/Art/SkyCity";
    public const string ScenePath="Assets/Boids/Scenes/SkyCity.unity";
    static Color Hex(string value){Color c;ColorUtility.TryParseHtmlString("#"+value,out c);return c;}
    static Material Material(string name,string shader="Boids/SkyCity/Limestone and Copper")
    {
        string path=Root+"/Materials/"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        var s=Shader.Find(shader);if(s==null)throw new InvalidOperationException("Missing shader "+shader);
        if(mat==null){mat=new Material(s){name=name};AssetDatabase.CreateAsset(mat,path);}else mat.shader=s;
        EditorUtility.SetDirty(mat);return mat;
    }
    static GameObject Import(string name)
    {
        string path=Root+"/"+name+".fbx";
        var importer=AssetImporter.GetAtPath(path) as ModelImporter;
        if(importer==null)throw new InvalidOperationException("Missing modeled FBX: "+path);
        importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;
        importer.isReadable=true;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }
    [MenuItem("Boids/Sky City/Build Garden of Winds")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Leave Play mode first.");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene first.");
        Directory.CreateDirectory(Root+"/Materials");AssetDatabase.Refresh();
        var environment=Import("SkyCity_Environment");var swallow=Import("SkyCity_Swallow");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var limestone=Material("Ivory limestone");limestone.SetFloat("_Smoothness",.23f);
        var copper=Material("Weathered copper");copper.SetFloat("_Metallic",.20f);copper.SetFloat("_Smoothness",.4f);
        var brass=Material("Old brass");brass.SetFloat("_Metallic",.58f);brass.SetFloat("_Smoothness",.5f);
        var rock=Material("Floating stone");rock.SetFloat("_Smoothness",.05f);
        rock.SetTexture("_SurfaceTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/WeatheredLimestone.png"));
        rock.SetFloat("_TextureAmount",.88f);rock.SetFloat("_Relief",.07f);rock.SetColor("_BaseColor",new Color(1.15f,1.15f,1.15f));
        var foliage=Material("Terrace foliage");foliage.SetFloat("_Foliage",.8f);foliage.SetFloat("_Smoothness",.18f);
        var dark=Material("Window recesses");dark.SetFloat("_Smoothness",.1f);
        var wood=Material("Roots");wood.SetFloat("_Smoothness",.1f);
        var cloud=Material("Pearl cloud light");cloud.SetFloat("_Cloud",1);
        var flag=Material("Apricot silk");flag.SetFloat("_Wind",1);flag.SetFloat("_Smoothness",.32f);
        var water=Material("Silver cascades","Boids/SkyCity/Cascades");
        var city=(GameObject)PrefabUtility.InstantiatePrefab(environment);city.name="Sky City - Modeled Architecture";
        foreach(var renderer in city.GetComponentsInChildren<MeshRenderer>())
        {
            string n=renderer.name;
            renderer.sharedMaterial=n.StartsWith("03")?copper:n.StartsWith("04")?brass:n.StartsWith("05")?dark:
                n.StartsWith("06")?rock:n.StartsWith("07")?foliage:n.StartsWith("08")?wood:
                n.StartsWith("09")?water:n.StartsWith("10")?cloud:n.StartsWith("11")?flag:limestone;
            renderer.shadowCastingMode=n.StartsWith("10")||n.StartsWith("09")?ShadowCastingMode.Off:ShadowCastingMode.TwoSided;
            renderer.receiveShadows=true;
            if(n.StartsWith("10"))renderer.enabled=false;
        }
        var cameraGO=new GameObject("Sky City Camera");cameraGO.tag="MainCamera";
        var camera=cameraGO.AddComponent<Camera>();camera.transform.position=new Vector3(0,16,-53);
        camera.transform.LookAt(new Vector3(0,8.7f,6));camera.fieldOfView=35;camera.aspect=1.5f;
        camera.nearClipPlane=.3f;camera.farClipPlane=230;camera.allowHDR=true;camera.allowMSAA=true;
        camera.clearFlags=CameraClearFlags.Skybox;cameraGO.AddComponent<RedonFixedFrame>();
        var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;
        data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        data.SetRenderer(EnsureRenderer());
        data.requiresDepthTexture=true;data.requiresColorTexture=true;
        var key=new GameObject("Morning sun").AddComponent<Light>();key.type=LightType.Directional;
        key.color=Hex("FFE0B0");key.intensity=2.55f;key.transform.rotation=Quaternion.LookRotation(-new Vector3(.55f,.75f,-.35f));
        key.shadows=LightShadows.Soft;key.shadowStrength=.85f;key.shadowBias=.018f;key.shadowNormalBias=.025f;
        RenderSettings.sun=key;RenderSettings.skybox=Material("Dawn sky","Boids/SkyCity/Dawn Sky");
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=Hex("A5BED0");
        RenderSettings.ambientEquatorColor=Hex("979BB8");RenderSettings.ambientGroundColor=Hex("77747F");
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=Hex("BCC6D5");
        RenderSettings.fogStartDistance=67;RenderSettings.fogEndDistance=162;
        var volume=new GameObject("Pearl morning grade").AddComponent<Volume>();volume.isGlobal=true;
        string profilePath=Root+"/SkyCityGrade.asset";
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,profilePath);}
        Tonemapping tone;if(!profile.TryGet(out tone)){tone=profile.Add<Tonemapping>(true);AssetDatabase.AddObjectToAsset(tone,profile);}
        tone.mode.Override(TonemappingMode.ACES);
        ColorAdjustments grade;if(!profile.TryGet(out grade)){grade=profile.Add<ColorAdjustments>(true);AssetDatabase.AddObjectToAsset(grade,profile);}
        grade.postExposure.Override(.05f);grade.contrast.Override(6);grade.saturation.Override(-4);
        Bloom bloom;if(!profile.TryGet(out bloom)){bloom=profile.Add<Bloom>(true);AssetDatabase.AddObjectToAsset(bloom,profile);}
        bloom.intensity.Override(.13f);bloom.threshold.Override(1.2f);bloom.scatter.Override(.6f);
        Vignette vignette;if(!profile.TryGet(out vignette)){vignette=profile.Add<Vignette>(true);AssetDatabase.AddObjectToAsset(vignette,profile);}
        vignette.intensity.Override(.08f);vignette.smoothness.Override(.55f);volume.sharedProfile=profile;EditorUtility.SetDirty(profile);
        CreateBirds(swallow);
        SkyCityVolumetricBuilder.Create();
        SkyCityWaterBuilder.Create(EnsureRenderer());
        SkyCityPresentationBuilder.Create();
        EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
        if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))
            EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
        Capture("SkyCity_01");
    }
    static int EnsureRenderer()
    {
        var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if(pipeline==null)throw new InvalidOperationException("Sky City requires URP.");
        string path=Root+"/SkyCityRenderer.asset";
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
        if(renderer==null)
        {
            var original=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/URP-HighFidelity-Renderer.asset");
            renderer=Object.Instantiate(original);renderer.name="SkyCityRenderer";renderer.rendererFeatures.Clear();AssetDatabase.CreateAsset(renderer,path);
        }
        var serialized=new SerializedObject(pipeline);var list=serialized.FindProperty("m_RendererDataList");
        for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==renderer)return i;
        int index=list.arraySize;list.arraySize++;list.GetArrayElementAtIndex(index).objectReferenceValue=renderer;
        serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);return index;
    }
    public static Vector3 FlightPath(float t)
    {
        return SkyCityFlock.Route(Mathf.Clamp(t,0,.9999f)*.6f);
    }
    public static void CreateBirds(GameObject model)
    {
        var root=new GameObject("Swallows - Living Sky");var birds=new List<Transform>();
        var material=Material("Swallow pearl and slate");material.SetFloat("_Smoothness",.34f);
        var random=new System.Random(91524);
        for(int i=0;i<64;i++)
        {
            float t=i<4?new[]{.015f,.155f,.26f,.345f}[i]:.39f+Mathf.Pow((i-4+(float)random.NextDouble()*.8f)/60,.88f)*.61f;
            Vector3 p=FlightPath(t);float spread=Mathf.Lerp(2.8f,.7f,t);
            p+=new Vector3(((float)random.NextDouble()-.5f)*2,((float)random.NextDouble()-.5f)*spread,((float)random.NextDouble()-.5f)*2);
            Vector3 tangent=FlightPath(Mathf.Min(1,t+.01f))-FlightPath(Mathf.Max(0,t-.01f));
            var bird=(GameObject)PrefabUtility.InstantiatePrefab(model);bird.name="Garden Swallow "+i.ToString("00");bird.transform.SetParent(root.transform);
            bird.transform.SetPositionAndRotation(p,Quaternion.LookRotation(tangent.normalized)*Quaternion.Euler(0,0,30+15*Mathf.Sin(i*1.7f)));
            float size=Mathf.Lerp(1.3f,.38f,t)*(float)(.80+random.NextDouble()*.30);
            if(i==0||i==1)size*=1.55f;
            bird.transform.localScale=Vector3.one*size;
            foreach(var r in bird.GetComponentsInChildren<Renderer>()){r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;}
            birds.Add(bird.transform);
        }
        root.AddComponent<SkyCityFlock>().Configure(birds.ToArray());
    }
    [MenuItem("Boids/Sky City/Capture Camera")]
    public static void CaptureMenu(){Capture("SkyCity_Current");}
    public static void Capture(string name){RedonSceneBuilder.CaptureCamera(Camera.main,"Captures/"+name+".png",1536,1024);}
    [MenuItem("Boids/Sky City/Validate Scene")]
    public static void Validate()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!=ScenePath)throw new InvalidOperationException("Open SkyCity first.");
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        int missing=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        var mats=all.SelectMany(t=>t.GetComponents<Renderer>()).SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
        var atmosphereShader=Shader.Find("Boids/SkyCity/Atmosphere Raymarch");
        var errors=mats.Where(m=>m!=null&&m.shader!=null).Select(m=>m.shader).Concat(new[]{atmosphereShader,RenderSettings.skybox.shader}).Distinct().SelectMany(s=>ShaderUtil.GetShaderMessages(s))
            .Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).Select(m=>m.message).ToArray();
        int birds=all.Count(t=>t.name.StartsWith("Garden Swallow"));int wings=all.Count(t=>t.name.Contains(" Wing"));
        int modeled=GameObject.Find("Sky City - Modeled Architecture").GetComponentsInChildren<MeshFilter>().Length;
        var rendererData=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root+"/SkyCityRenderer.asset");
        var atmosphere=rendererData.rendererFeatures.OfType<SkyCityAtmosphereFeature>().FirstOrDefault();
        bool volumeClouds=atmosphere!=null&&atmosphere.isActive&&atmosphere.cloudMaterial.GetTexture("_NoiseTex") is Texture3D;
        var reflection=Object.FindObjectOfType<SkyCityWaterReflection>();
        bool reflectingWater=reflection!=null&&reflection.ReflectionTexture!=null;
        bool passed=volumeClouds&&reflectingWater&&missing==0&&birds==64&&wings==128&&modeled>=20&&errors.Length==0&&Camera.main!=null&&Object.FindObjectOfType<SkyCityFlock>()!=null;
        Directory.CreateDirectory("Captures");File.WriteAllText("Captures/SkyCity_Validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new
        {passed,scene=scene.path,missingScripts=missing,birds,wings,modeledForms=modeled,volumeClouds,reflectingWater,shaderErrors=errors},Newtonsoft.Json.Formatting.Indented));
        if(!passed)throw new InvalidOperationException("Sky City validation failed; read the report.");
    }
}
