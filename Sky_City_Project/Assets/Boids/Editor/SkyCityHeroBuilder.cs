using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Boids.Art;
using Object=UnityEngine.Object;

/// <summary>Authored quality reference, isolated from the streamed world's existing lighting.</summary>
public static class SkyCityHeroBuilder
{
    public const string Root="Assets/Boids/Art/SkyCityHero";
    public const string ScenePath="Assets/Boids/Scenes/SkyCityHangingGardens.unity";
    static Material Mat(string name,string shader="Boids/SkyCity/Hanging Gardens")
    {
        string path=Root+"/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}
        m.shader=Shader.Find(shader);m.enableInstancing=true;return m;
    }
    static Dictionary<string,Material> Materials()
    {
        var stone=Mat("Warm carved limestone");stone.SetFloat("_Smoothness",.24f);
        stone.SetTexture("_SurfaceTex",AssetDatabase.LoadAssetAtPath<Texture2D>(SkyCityInfiniteBuilder.Root+"/HonedIvoryLimestone.png"));
        stone.SetFloat("_TextureScale",.55f);stone.SetFloat("_TextureNeutral",.68f);stone.SetFloat("_TextureAmount",.16f);stone.SetFloat("_Relief",.002f);
        var copper=Mat("Quiet verdigris copper");copper.SetFloat("_Metallic",.34f);copper.SetFloat("_Smoothness",.37f);
        copper.SetFloat("_PigmentSmoothing",.62f);copper.SetColor("_PigmentMean",new Color(.42f,.61f,.55f));
        var gold=Mat("Aged brass");gold.SetFloat("_Metallic",.64f);gold.SetFloat("_Smoothness",.48f);
        var rock=Mat("Stratified pale cliff");rock.SetTexture("_SurfaceTex",AssetDatabase.LoadAssetAtPath<Texture2D>(SkyCitySceneBuilder.Root+"/Textures/WeatheredLimestone.png"));
        rock.SetFloat("_TextureScale",.28f);rock.SetFloat("_TextureNeutral",.33f);rock.SetFloat("_TextureAmount",.32f);rock.SetFloat("_Relief",.007f);rock.SetFloat("_Smoothness",.1f);rock.SetColor("_BaseColor",new Color(1.13f,1.12f,1.09f));
        var leaf=Mat("Living leaf sprays");leaf.SetFloat("_Foliage",.8f);leaf.SetFloat("_LeafMotion",1);leaf.SetFloat("_Smoothness",.20f);
        var wood=Mat("Branch bark");wood.SetFloat("_Smoothness",.1f);
        var dark=Mat("Recessed glazing and shutters");dark.SetFloat("_Smoothness",.68f);dark.SetFloat("_Metallic",.25f);
        var tile=Mat("Warm terracotta");tile.SetFloat("_Smoothness",.23f);
        var earth=Mat("Garden beds");earth.SetFloat("_Smoothness",.08f);
        var silk=Mat("Apricot pennants");silk.SetFloat("_Wind",1);silk.SetFloat("_Smoothness",.3f);
        var cascade=Mat("Breaking cascades","Boids/SkyCity/Hanging Cascades");
        var pool=Mat("Sheltered reflecting water","Boids/SkyCity/Reflecting Garden Water");pool.SetFloat("_RippleStrength",.018f);pool.SetFloat("_ReflectionStrength",.92f);
        pool.SetFloat("_OpticalDepth",.32f);pool.SetColor("_ShallowColor",new Color(.17f,.43f,.37f));pool.SetColor("_DeepColor",new Color(.05f,.22f,.20f));
        return new Dictionary<string,Material>{{"01",stone},{"02",stone},{"03",copper},{"04",gold},{"05",dark},{"06",rock},{"07",leaf},{"08",wood},{"09",cascade},{"11",silk},{"12",stone},{"13",pool},{"14",tile},{"15",earth}};
    }
    public static void Import()
    {
        foreach(string file in new[]{"HangingGardens","HangingGardens_LOD1","HangingGardens_LOD2"})
        {
            string path=Root+"/"+file+".fbx";AssetDatabase.ImportAsset(path);
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.isReadable=false;importer.generateSecondaryUV=false;
            importer.meshCompression=ModelImporterMeshCompression.Off;
            importer.SaveAndReimport();
        }
    }
    static GameObject Model(string name,Transform parent,Dictionary<string,Material> materials)
    {
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/"+name+".fbx"));
        instance.transform.SetParent(parent,false);
        foreach(var r in instance.GetComponentsInChildren<MeshRenderer>())
        {
            string prefix=r.name.Substring(0,2);
            r.sharedMaterial=materials.ContainsKey(prefix)?materials[prefix]:materials["01"];
            r.shadowCastingMode=prefix=="09"||prefix=="13"?ShadowCastingMode.Off:ShadowCastingMode.TwoSided;
            r.lightProbeUsage=prefix=="06"?LightProbeUsage.Off:LightProbeUsage.BlendProbes;r.reflectionProbeUsage=ReflectionProbeUsage.Simple;
            if(prefix=="09"||prefix=="13")r.gameObject.layer=4;
        }
        return instance;
    }
    static int Renderer()
    {
        string path=Root+"/GardensRenderer.asset";
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
        if(renderer==null)
        {
            renderer=Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/URP-HighFidelity-Renderer.asset"));
            renderer.name="GardensRenderer";renderer.rendererFeatures.Clear();AssetDatabase.CreateAsset(renderer,path);
            var original=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(SkyCityInfiniteBuilder.Root+"/InfiniteRenderer.asset");
            var template=original.rendererFeatures.First(f=>f.name=="Soft architectural contact shadows");
            var ao=Object.Instantiate(template);ao.name="Gardens contact shadows";AssetDatabase.AddObjectToAsset(ao,renderer);renderer.rendererFeatures.Add(ao);
            var settings=new SerializedObject(ao);settings.FindProperty("m_Settings.Radius").floatValue=.65f;settings.FindProperty("m_Settings.Intensity").floatValue=.9f;
            settings.ApplyModifiedPropertiesWithoutUndo();ao.Create();EditorUtility.SetDirty(ao);
            var volume=ScriptableObject.CreateInstance<SkyCityAtmosphereFeature>();volume.name="Gardens volumetric clouds";
            AssetDatabase.AddObjectToAsset(volume,renderer);renderer.rendererFeatures.Add(volume);
        }
        var cloud=Mat("Rolling sunlit cloud banks","Boids/SkyCity/Hanging Gardens Clouds");
        cloud.SetTexture("_NoiseTex",AssetDatabase.LoadAssetAtPath<Texture3D>(SkyCitySceneBuilder.Root+"/PerlinWorleyVolume.asset"));
        cloud.SetTexture("_CloudField",AssetDatabase.LoadAssetAtPath<Texture3D>(SkyCityHeroCloudBuilder.FieldPath));
        var feature=renderer.rendererFeatures.OfType<SkyCityAtmosphereFeature>().First();feature.cloudMaterial=cloud;feature.resolutionScale=1f;feature.SetActive(true);feature.Create();
        EditorUtility.SetDirty(cloud);EditorUtility.SetDirty(feature);renderer.SetDirty();EditorUtility.SetDirty(renderer);
        var pipeline=(UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        var serialized=new SerializedObject(pipeline);var list=serialized.FindProperty("m_RendererDataList");
        for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==renderer)return i;
        int index=list.arraySize;list.arraySize++;list.GetArrayElementAtIndex(index).objectReferenceValue=renderer;
        serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);return index;
    }
    [MenuItem("Boids/Hanging Gardens/Build authored reference")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Leave Play mode first.");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene before building.");
        Import();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var materials=Materials();int rendererIndex=Renderer();
        Shader.SetGlobalVector("_SkyWorldOffset",Vector4.zero);
        var root=new GameObject("Hanging Gardens - authored architecture");
        var near=Model("HangingGardens",root.transform,materials);
        PrefabUtility.UnpackPrefabInstance(near,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        var middle=Model("HangingGardens_LOD1",root.transform,materials);
        var far=Model("HangingGardens_LOD2",root.transform,materials);
        // Moving water and silk remain outside the geometry LOD selection.
        var dynamicRoot=new GameObject("Flowing water and pennants");dynamicRoot.transform.SetParent(root.transform);
        foreach(var r in near.GetComponentsInChildren<Renderer>().Where(r=>r.gameObject.layer==4||r.name.StartsWith("11")).ToArray())r.transform.SetParent(dynamicRoot.transform,true);
        var lod=root.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.37f,near.GetComponentsInChildren<Renderer>()),new LOD(.14f,middle.GetComponentsInChildren<Renderer>()),new LOD(.018f,far.GetComponentsInChildren<Renderer>())});lod.RecalculateBounds();
        // Reusable complete island prefab: no scene camera, sun or audio embedded in it.
        PrefabUtility.SaveAsPrefabAsset(root,Root+"/HangingGardens.prefab");
        var camera=new GameObject("Gardens camera").AddComponent<Camera>();camera.tag="MainCamera";
        camera.transform.position=new Vector3(5,24,-72);camera.transform.LookAt(new Vector3(0,9.5f,11));
        camera.fieldOfView=37;camera.nearClipPlane=.2f;camera.farClipPlane=460;camera.allowHDR=true;
        camera.gameObject.AddComponent<RedonFixedFrame>();camera.aspect=1.5f;
        var data=camera.GetUniversalAdditionalCameraData();data.SetRenderer(rendererIndex);data.requiresDepthTexture=true;data.requiresColorTexture=true;data.renderPostProcessing=true;
        data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        var sun=new GameObject("Warm afternoon sunlight").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2.0f;
        sun.color=new Color(1,.86f,.69f);sun.transform.rotation=Quaternion.LookRotation(-new Vector3(.65f,.66f,-.35f));
        sun.shadows=LightShadows.Soft;sun.shadowBias=.012f;sun.shadowNormalBias=.035f;sun.shadowStrength=1;RenderSettings.sun=sun;
        var sky=Mat("Soft blue apricot sky","Boids/SkyCity/Dawn Sky");sky.SetColor("_Zenith",new Color(.32f,.55f,.80f));sky.SetColor("_Horizon",new Color(.70f,.79f,.91f));RenderSettings.skybox=sky;
        var daylight=sun.gameObject.AddComponent<SkyCityDaylight>();daylight.sun=sun;daylight.sky=sky;daylight.Apply();
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.59f,.66f,.82f);
        RenderSettings.ambientEquatorColor=new Color(.46f,.48f,.58f);RenderSettings.ambientGroundColor=new Color(.34f,.31f,.28f);
        RenderSettings.ambientIntensity=1;RenderSettings.reflectionIntensity=.8f;
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.70f,.76f,.84f);RenderSettings.fogStartDistance=105;RenderSettings.fogEndDistance=300;
        string gradePath=Root+"/GardensGrade.asset";
        if(AssetDatabase.LoadAssetAtPath<VolumeProfile>(gradePath)==null)AssetDatabase.CopyAsset(SkyCitySceneBuilder.Root+"/SkyCityGrade.asset",gradePath);
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(gradePath);ColorAdjustments color;profile.TryGet(out color);color.postExposure.Override(.18f);color.contrast.Override(9);color.saturation.Override(5);
        Bloom bloom;profile.TryGet(out bloom);bloom.intensity.Override(.13f);bloom.threshold.Override(1.2f);
        var volume=new GameObject("Gardens colour grade").AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;EditorUtility.SetDirty(color);EditorUtility.SetDirty(bloom);EditorUtility.SetDirty(profile);
        var reflection=new GameObject("Basin - live planar reflection").AddComponent<SkyCityWaterReflection>();reflection.rendererIndex=rendererIndex;reflection.waterHeight=3.91f;reflection.textureWidth=1024;reflection.updatesPerSecond=30;
        DistantCities(materials);Spray();Birds();SkyCityPresentationBuilder.Create();
        camera.GetComponent<SkyCityCameraRig>().maximumDistance=125;
        camera.GetComponent<SkyCityCameraRig>().minimumDistance=12;
        camera.GetComponent<SkyCityCameraRig>().panLimits=new Vector3(22,20,20);
        camera.GetComponent<SkyCityCameraRig>().homeFocusDistance=85;
        camera.gameObject.AddComponent<SkyCityHeroPerformanceProbe>();
        // Local probe volumes receive the actual sky and colour bleed when baked.
        var probes=new GameObject("Terrace light probes").AddComponent<LightProbeGroup>();var positions=new List<Vector3>();
        for(int x=-36;x<=24;x+=5)for(int z=0;z<=23;z+=5)foreach(float y in new[]{4f,8f,13f,19f,25f})positions.Add(new Vector3(x,y,z));probes.probePositions=positions.ToArray();
        SkyCityHeroCloudBuilder.Bake();DynamicGI.UpdateEnvironment();
        foreach(var m in materials.Values)EditorUtility.SetDirty(m);EditorUtility.SetDirty(sky);
        EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
        if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
        Debug.Log("Hanging Gardens reference built. Inspect overview, near architecture and basin before acceptance.");
    }
    static void Spray()
    {
        var material=Mat("Waterfall mist","Boids/SkyCity/Soft Water Spray");
        foreach(var point in new[]{new Vector3(-23,-6,2.1f),new Vector3(-10.8f,-8,-3.95f),new Vector3(23.3f,-.8f,11.7f)})
        {
            var go=new GameObject("Waterfall spray");go.layer=4;go.transform.position=point;var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.startLifetime=new ParticleSystem.MinMaxCurve(2.5f,4.5f);main.startSpeed=.22f;main.startSize=new ParticleSystem.MinMaxCurve(.4f,1.2f);main.startColor=new Color(.85f,.92f,1,.19f);main.maxParticles=220;main.prewarm=true;
            var emission=ps.emission;emission.rateOverTime=40;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.6f;
            var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.x=new ParticleSystem.MinMaxCurve(.15f,.5f);velocity.y=new ParticleSystem.MinMaxCurve(.05f,.5f);velocity.z=new ParticleSystem.MinMaxCurve(-.10f,.12f);
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.3f,1,2.4f));
            var colour=ps.colorOverLifetime;colour.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.7f,.2f),new GradientAlphaKey(0,1)});colour.color=gradient;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;ps.Play();
        }
    }
    static void Birds()
    {
        // Keep the proven articulated flight implementation and the original score.
        SkyCitySceneBuilder.CreateBirds(AssetDatabase.LoadAssetAtPath<GameObject>(SkyCitySceneBuilder.Root+"/SkyCity_Swallow.fbx"));
        ConfigureBirds();
    }
    public static void ConfigureBirds()
    {
        var flock=Object.FindObjectOfType<SkyCityFlock>();
        flock.ConfigureObstacles(new[]{new Vector3(-18,14,13),new Vector3(21,9,16),new Vector3(7,5,12),new Vector3(-11,2,1)},
            new[]{new Vector3(21,23,12),new Vector3(6,9,6),new Vector3(12,2,3),new Vector3(7,5,6)});
        var random=new System.Random(81367);var camera=Camera.main;
        var birds=flock.transform.Cast<Transform>().ToArray();var material=Mat("Swallow ivory and slate");material.SetFloat("_Smoothness",.31f);
        for(int i=0;i<birds.Length;i++)
        {
            if(i<4)
            {
                var viewport=new[]{new Vector3(.13f,.26f,36),new Vector3(.28f,.13f,42),new Vector3(.41f,.23f,53),new Vector3(.55f,.31f,68)};
                birds[i].position=camera.ViewportToWorldPoint(viewport[i]);birds[i].localScale=Vector3.one*(1.23f-i*.13f);
            }
            else
            {
                float t=(i-4)/60f;
                Vector3 p=t<.36f?Vector3.Lerp(new Vector3(-3,2,-5),new Vector3(13,9,5),t/.36f):
                    t<.72f?Vector3.Lerp(new Vector3(16,14,24),new Vector3(29,23,31),(t-.36f)/.36f):
                    Vector3.Lerp(new Vector3(-1,29,32),new Vector3(23,31,48),(t-.72f)/.28f);
                p+=new Vector3((float)random.NextDouble()*5-2.5f,(float)random.NextDouble()*3-1.5f,(float)random.NextDouble()*5-2.5f);
                birds[i].position=p;birds[i].localScale=Vector3.one*Mathf.Lerp(.55f,.29f,t);
            }
            birds[i].rotation=Quaternion.LookRotation(new Vector3(1,.1f+(float)random.NextDouble()*.15f,.38f))*Quaternion.Euler(0,0,-15+(float)random.NextDouble()*30);
            foreach(var r in birds[i].GetComponentsInChildren<Renderer>())r.sharedMaterial=material;
        }
        EditorUtility.SetDirty(material);
    }
    public static void BuildPlayer()
    {
        Directory.CreateDirectory("Builds/HangingGardens");PlayerSettings.enableFrameTimingStats=true;Application.runInBackground=true;
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/HangingGardens/HangingGardens.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        File.WriteAllText("Captures/HangingGardens/Build.json",JsonUtility.ToJson(new BuildSummary{result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,seconds=report.summary.totalTime.TotalSeconds},true));
    }
    [Serializable] class BuildSummary{public string result;public int errors,warnings;public double seconds;}
    static void DistantCities(Dictionary<string,Material> materials)
    {
        var horizon=new GameObject("Distant cities - layered silhouettes");
        var positions=new[]{new Vector3(-44,12,112),new Vector3(19,13,129),new Vector3(74,27,166)};
        var scales=new[]{.42f,.33f,.59f};var rotations=new[]{24f,-48f,80f};
        for(int i=0;i<positions.Length;i++)
        {
            var city=Model("HangingGardens_LOD2",horizon.transform,materials);
            city.transform.position=positions[i];city.transform.localScale=Vector3.one*scales[i];city.transform.rotation=Quaternion.Euler(0,rotations[i],0);
            foreach(var r in city.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
        }
    }
    [MenuItem("Boids/Hanging Gardens/Bake terrace indirect light")]
    public static void Bake()
    {
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=ScenePath)throw new InvalidOperationException("Open Hanging Gardens before baking.");
        Directory.CreateDirectory(Root+"/LightingMeshes");AssetDatabase.Refresh();
        foreach(var filter in GameObject.Find("HangingGardens").GetComponentsInChildren<MeshFilter>())
        {
            string prefix=filter.name.Substring(0,2);
            if(prefix=="07"||prefix=="08"||prefix=="04")continue;
            var renderer=filter.GetComponent<MeshRenderer>();
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/HangingGardens.fbx").GetComponentsInChildren<MeshFilter>().First(f=>f.name==filter.name);
            // Filigree has thousands of sub-texel islands. It contributes to the bake,
            // but receives probe lighting and geometric AO rather than bleeding charts.
            if(prefix!="01"&&prefix!="06")
            {
                filter.sharedMesh=original.sharedMesh;GameObjectUtility.SetStaticEditorFlags(filter.gameObject,StaticEditorFlags.ContributeGI);
                renderer.receiveGI=ReceiveGI.LightProbes;continue;
            }
            string meshPath=Root+"/LightingMeshes/"+prefix+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(mesh!=null){AssetDatabase.DeleteAsset(meshPath);mesh=null;}
            if(mesh==null)
            {
                mesh=Object.Instantiate(original.sharedMesh);mesh.name=original.sharedMesh.name+" lightmap UV";
                var parameters=new UnwrapParam();UnwrapParam.SetDefaults(out parameters);parameters.packMargin=.003f;
                Unwrapping.GenerateSecondaryUVSet(mesh,parameters);AssetDatabase.CreateAsset(mesh,meshPath);
            }
            filter.sharedMesh=mesh;GameObjectUtility.SetStaticEditorFlags(filter.gameObject,StaticEditorFlags.ContributeGI);
            renderer.receiveGI=ReceiveGI.Lightmaps;
            var settings=new SerializedObject(renderer);settings.FindProperty("m_ScaleInLightmap").floatValue=prefix=="02"?.45f:1f;settings.ApplyModifiedPropertiesWithoutUndo();
        }
        string lightingPath=Root+"/GardensLighting.asset";var lighting=AssetDatabase.LoadAssetAtPath<LightingSettings>(lightingPath);
        if(lighting==null){lighting=new LightingSettings();AssetDatabase.CreateAsset(lighting,lightingPath);}
        lighting.bakedGI=true;lighting.realtimeGI=false;lighting.lightmapper=LightingSettings.Lightmapper.ProgressiveGPU;
        lighting.mixedBakeMode=MixedLightingMode.IndirectOnly;lighting.lightmapResolution=12;lighting.lightmapMaxSize=2048;
        lighting.indirectSampleCount=256;lighting.directSampleCount=32;lighting.environmentSampleCount=128;lighting.maxBounces=4;
        Lightmapping.lightingSettings=lighting;RenderSettings.sun.lightmapBakeType=LightmapBakeType.Mixed;
        EditorUtility.SetDirty(lighting);EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        if(!Lightmapping.BakeAsync())throw new InvalidOperationException("Unity could not start the light bake.");
    }
    public static void Capture(string name="Overview",float cloudTime=-1)
    {
        Directory.CreateDirectory("Captures/HangingGardens");
        var cloud=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Rolling sunlit cloud banks.mat");
        float prior=cloud.GetFloat("_CloudTime");cloud.SetFloat("_CloudTime",cloudTime);
        try{RedonSceneBuilder.CaptureCamera(Camera.main,"Captures/HangingGardens/"+name+".png",1800,1200);}
        finally{cloud.SetFloat("_CloudTime",prior);}
    }
}
