using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Apply the authored island's surface reference to streamed assets.</summary>
public static class SkyCityStyleReview
{
    public static void RefreshAuthoredMeshes(bool rebuildLightingMeshes=false)
    {
        if(Application.isPlaying)throw new InvalidOperationException("Leave Play first.");
        string scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        if(scene!=SkyCityWorldBuilder.ScenePath&&scene!=SkyCityHeroBuilder.ScenePath)throw new InvalidOperationException("Open the authored island or first-person world.");
        var models=new[]{"HangingGardens","HangingGardens_LOD1","HangingGardens_LOD2"}
            .SelectMany(n=>AssetDatabase.LoadAssetAtPath<GameObject>(SkyCityHeroBuilder.Root+"/"+n+".fbx").GetComponentsInChildren<MeshFilter>()).ToDictionary(f=>f.name);
        if(rebuildLightingMeshes)
        {
            foreach(string prefix in new[]{"01","06"})
            {
                var source=models.Values.First(f=>f.name.StartsWith(prefix)&&!f.name.Contains("distance"));
                var fresh=UnityEngine.Object.Instantiate(source.sharedMesh);var unwrap=new UnwrapParam();UnwrapParam.SetDefaults(out unwrap);unwrap.packMargin=.003f;
                if(prefix=="01")Unwrapping.GenerateSecondaryUVSet(fresh,unwrap);
                string path=SkyCityHeroBuilder.Root+"/LightingMeshes/"+prefix+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved==null)AssetDatabase.CreateAsset(fresh,path);
                else{fresh.name=saved.name;saved.Clear();EditorUtility.CopySerialized(fresh,saved);saved.UploadMeshData(false);UnityEngine.Object.DestroyImmediate(fresh);EditorUtility.SetDirty(saved);}
            }
        }
        var root=GameObject.Find("Hanging Gardens - authored architecture");
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            MeshFilter source;if(!models.TryGetValue(filter.name,out source))continue;
            filter.transform.localPosition=source.transform.localPosition;filter.transform.localRotation=source.transform.localRotation;filter.transform.localScale=source.transform.localScale;
            string prefix=filter.name.Substring(0,2);
            filter.sharedMesh=!filter.name.Contains("distance")&&(prefix=="01"||prefix=="06")?AssetDatabase.LoadAssetAtPath<Mesh>(SkyCityHeroBuilder.Root+"/LightingMeshes/"+prefix+".asset"):source.sharedMesh;
            if(prefix=="06")
            {
                var renderer=filter.GetComponent<MeshRenderer>();renderer.receiveGI=ReceiveGI.LightProbes;renderer.lightmapIndex=-1;
                // One renderer spans all three islands. Its centre lies inside
                // the rock volume, where a single interpolated baked probe is
                // unrepresentative of the open, cloud-lit cliff surfaces.
                renderer.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.Off;
                EditorUtility.SetDirty(renderer);
            }
            EditorUtility.SetDirty(filter);EditorUtility.SetDirty(filter.transform);
        }
        var collision=GameObject.Find("Arrival collision shells");
        if(collision!=null)foreach(var collider in collision.GetComponentsInChildren<MeshCollider>())
        {
            MeshFilter source;if(!models.TryGetValue(collider.name,out source))continue;
            collider.sharedMesh=source.sharedMesh;collider.transform.localPosition=source.transform.position;collider.transform.localRotation=source.transform.rotation;collider.transform.localScale=source.transform.lossyScale;
            EditorUtility.SetDirty(collider);EditorUtility.SetDirty(collider.transform);
        }
        root.GetComponent<LODGroup>().RecalculateBounds();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
    }
    static Material Load(string root,string name){return AssetDatabase.LoadAssetAtPath<Material>(root+"/"+name+".mat");}
    public static void ApplyReferenceStyle()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Leave Play before changing authored materials.");
        var stone=Load(SkyCityHeroBuilder.Root,"Warm carved limestone");
        var copper=Load(SkyCityHeroBuilder.Root,"Quiet verdigris copper");
        var water=Load(SkyCityHeroBuilder.Root,"Sheltered reflecting water");
        string normalPath=SkyCityHeroBuilder.Root+"/LimestoneScanNormal.jpg";
        var importer=(TextureImporter)AssetImporter.GetAtPath(normalPath);
        importer.textureType=TextureImporterType.NormalMap;importer.sRGBTexture=false;importer.maxTextureSize=2048;importer.SaveAndReimport();
        var albedo=AssetDatabase.LoadAssetAtPath<Texture2D>(SkyCitySceneBuilder.Root+"/Textures/WeatheredLimestone.png");
        var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        var cliff=Load(SkyCityHeroBuilder.Root,"Stratified pale cliff");
        cliff.SetFloat("_UseRockScan",1);cliff.SetFloat("_TextureAmount",0);cliff.SetFloat("_Smoothness",.12f);
        cliff.SetTexture("_RockScanAlbedo",albedo);cliff.SetTexture("_RockScanNormal",normal);EditorUtility.SetDirty(cliff);
        foreach(var material in new[]{Load(SkyCityInfiniteBuilder.Root,"Ivory copper and silk"),Load(SkyCityWorldBuilder.Root,"Weathered ivory city")})
        {
            material.SetFloat("_StoneRelief",stone.GetFloat("_Relief"));
            material.SetFloat("_StoneVariation",stone.GetFloat("_TextureAmount"));
            material.SetTexture("_ArchitectureTex",stone.GetTexture("_SurfaceTex"));
            material.SetColor("_CopperMean",copper.GetColor("_PigmentMean"));
            material.SetTexture("_RockScanAlbedo",albedo);material.SetTexture("_RockScanNormal",normal);
            EditorUtility.SetDirty(material);
        }
        foreach(var material in new[]{Load(SkyCityInfiniteBuilder.Root,"Infinite reflecting water"),Load(SkyCityWorldBuilder.Root,"Archipelago reflecting pools")})
        {
            foreach(string property in new[]{"_RippleStrength","_ReflectionStrength","_OpticalDepth"})material.SetFloat(property,water.GetFloat(property));
            foreach(string property in new[]{"_ShallowColor","_DeepColor"})material.SetColor(property,water.GetColor(property));
            material.SetFloat("_MultipleElevations",1);EditorUtility.SetDirty(material);
        }
        AssetDatabase.SaveAssets();
    }
    public static object Validate()
    {
        var shaders=AssetDatabase.FindAssets("t:Shader",new[]{"Assets/Boids/Shaders"})
            .Select(id=>AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(id)))
            .Where(s=>s.name.StartsWith("Boids/SkyCity/"));
        var errors=shaders.SelectMany(s=>ShaderUtil.GetShaderMessages(s).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).Select(m=>s.name+": "+m.message)).ToArray();
        var stone=Load(SkyCityHeroBuilder.Root,"Warm carved limestone");
        var materials=new[]{Load(SkyCityInfiniteBuilder.Root,"Ivory copper and silk"),Load(SkyCityWorldBuilder.Root,"Weathered ivory city")};
        bool match=materials.All(m=>Mathf.Approximately(m.GetFloat("_StoneRelief"),stone.GetFloat("_Relief"))&&Mathf.Approximately(m.GetFloat("_StoneVariation"),stone.GetFloat("_TextureAmount"))&&m.GetTexture("_ArchitectureTex")==stone.GetTexture("_SurfaceTex"));
        var report=new{passed=errors.Length==0&&match,shaderErrors=errors,stoneParametersMatchReference=match,referenceScene=SkyCityHeroBuilder.ScenePath,
            categories=new[]{"Static module plants: individual leaves and tapered branches at all LODs","District trees and cypresses: same leaf-spray geometry family","Living fractal trees and flowers: shared leaf surface response","Masonry: reference palette, window reveals, cornices, smoother curved profiles","Cliffs: reference island strata and buttresses","Ground boulders: irregular weathered ledges","Water: reference pool parameters and warm cascade tint","Clouds, birds and global lighting: existing authored-island systems"}};
        Directory.CreateDirectory("Captures/SkyCityWorld/StyleReview");File.WriteAllText("Captures/SkyCityWorld/StyleReview/Materials.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return report;
    }
}
