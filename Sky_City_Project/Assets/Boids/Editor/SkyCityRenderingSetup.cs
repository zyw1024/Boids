using System;
using System.Linq;
using Boids.Art;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Reproducible art direction for the streamed world, on URP 14.</summary>
public static class SkyCityRenderingSetup
{
    const string Root=SkyCityInfiniteBuilder.Root;
    [MenuItem("Boids/Sky City Infinite/Apply pearl morning lighting")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Leave play mode before saving lighting.");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=SkyCityInfiniteBuilder.ScenePath)
            throw new InvalidOperationException("Open SkyCityInfinite first.");
        ApplyToScene();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
    }
    public static void ApplyToScene()
    {
        var sun=RenderSettings.sun;
        sun.transform.rotation=Quaternion.Euler(30,-74,0);
        sun.color=new Color(1,.83f,.65f);sun.intensity=1.95f;
        sun.shadowStrength=1;sun.shadowBias=.015f;sun.shadowNormalBias=.1f;
        var sky=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Pearl horizon.mat");
        sky.SetColor("_Zenith",new Color(.40f,.60f,.79f));
        sky.SetColor("_Horizon",new Color(.84f,.80f,.78f));EditorUtility.SetDirty(sky);
        var daylight=sun.GetComponent<SkyCityDaylight>();
        if(daylight==null)daylight=sun.gameObject.AddComponent<SkyCityDaylight>();
        daylight.sun=sun;daylight.sky=sky;daylight.Apply();
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.56f,.63f,.79f);
        RenderSettings.ambientEquatorColor=new Color(.40f,.44f,.58f);
        RenderSettings.ambientGroundColor=new Color(.31f,.27f,.23f);
        RenderSettings.ambientIntensity=1;
        RenderSettings.reflectionIntensity=.85f;
        RenderSettings.fogColor=new Color(.68f,.72f,.81f);
        RenderSettings.fogStartDistance=125;RenderSettings.fogEndDistance=560;
        var architecture=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Ivory copper and silk.mat");
        var limestone=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/HonedIvoryLimestone.png");
        if(limestone==null)throw new InvalidOperationException("Wait for the limestone texture import to complete.");
        architecture.SetTexture("_ArchitectureTex",limestone);
        architecture.SetFloat("_StoneRelief",.012f);architecture.SetFloat("_StoneVariation",.3f);
        EditorUtility.SetDirty(architecture);
        var cloud=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Endless volumetric cloud sea.mat");
        cloud.SetFloat("_Steps",112);cloud.SetFloat("_LightSteps",5);
        cloud.SetFloat("_Density",.36f);cloud.SetFloat("_Coverage",.58f);cloud.SetFloat("_Detail",.085f);
        cloud.SetColor("_SunColor",new Color(1.08f,1.10f,1.18f));
        cloud.SetColor("_ShadeColor",new Color(.27f,.35f,.52f));EditorUtility.SetDirty(cloud);
        var water=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Infinite reflecting water.mat");
        water.SetColor("_ShallowColor",new Color(.13f,.42f,.39f));
        water.SetColor("_DeepColor",new Color(.035f,.19f,.22f));
        water.SetFloat("_RippleStrength",.025f);water.SetFloat("_ReflectionStrength",.92f);
        water.SetFloat("_OpticalDepth",.65f);EditorUtility.SetDirty(water);
        var reflection=UnityEngine.Object.FindObjectOfType<SkyCityWaterReflection>();
        reflection.textureWidth=768;reflection.updatesPerSecond=30;
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root+"/InfiniteRenderer.asset");
        var ao=renderer.rendererFeatures.First(f=>f.name=="Soft architectural contact shadows");
        var settings=new SerializedObject(ao);
        settings.FindProperty("m_Settings.Source").intValue=0; // Depth reconstruction; no normal prepass needed.
        settings.FindProperty("m_Settings.NormalSamples").intValue=2;
        settings.FindProperty("m_Settings.AfterOpaque").boolValue=false;
        settings.FindProperty("m_Settings.Downsample").boolValue=false;
        settings.FindProperty("m_Settings.Intensity").floatValue=1.25f;
        settings.FindProperty("m_Settings.Radius").floatValue=.85f;
        settings.FindProperty("m_Settings.DirectLightingStrength").floatValue=.18f;
        settings.ApplyModifiedPropertiesWithoutUndo();ao.Create();renderer.SetDirty();EditorUtility.SetDirty(ao);
        string profilePath=Root+"/PearlMorningGrade.asset";
        if(AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath)==null)
            AssetDatabase.CopyAsset(SkyCitySceneBuilder.Root+"/SkyCityGrade.asset",profilePath);
        var grade=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        ColorAdjustments color;grade.TryGet(out color);
        color.postExposure.Override(.15f);color.contrast.Override(10);color.saturation.Override(2);
        Bloom bloom;grade.TryGet(out bloom);bloom.intensity.Override(.10f);bloom.threshold.Override(1.3f);
        UnityEngine.Object.FindObjectOfType<Volume>().sharedProfile=grade;
        EditorUtility.SetDirty(color);EditorUtility.SetDirty(bloom);EditorUtility.SetDirty(grade);
        // Refresh the environment cubemap for copper and the scene ambient probe.
        DynamicGI.UpdateEnvironment();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
