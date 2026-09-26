using System;
using System.IO;
using System.Linq;
using Boids.Art;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

/// <summary>Assembles the Blender-authored E garden while preserving the living school.</summary>
public static class AtelierSceneBuilder
{
    public const string Root="Assets/Boids/Art/Atelier";
    public const string ScenePath="Assets/Boids/Scenes/RedonAtelier.unity";
    static Color Hex(string s){Color c;ColorUtility.TryParseHtmlString("#"+s,out c);return c;}
    static Material Pigment(string name,float strength,float rim,float warmth,float value=1)
    {
        string path=Root+"/Materials/"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Boids/Atelier/Pigment"));AssetDatabase.CreateAsset(mat,path);}
        mat.shader=Shader.Find("Boids/Atelier/Pigment");
        mat.SetTexture("_PigmentTex",AssetDatabase.LoadAssetAtPath<Texture2D>(RedonSceneBuilder.Root+"/Textures/PigmentScumble.png"));
        mat.SetTexture("_GlazeTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/SymbolistPigment.png"));
        mat.SetFloat("_Glaze",.55f);
        mat.SetFloat("_GlazeMidpoint",.22f);mat.SetFloat("_BrushRelief",.018f);
        mat.SetFloat("_PaintStrength",strength);mat.SetFloat("_PaintScale",.9f);
        mat.SetFloat("_Rim",rim);mat.SetFloat("_Warmth",warmth);mat.SetFloat("_Value",value);
        mat.SetFloat("_Transmission",0);
        mat.SetFloat("_VertexColor",1);mat.SetColor("_BaseColor",Color.white);
        EditorUtility.SetDirty(mat);return mat;
    }
    [MenuItem("Boids/Atelier/Build Sculpted Garden")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Leave Play mode before authoring.");
        var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(current.isDirty)throw new InvalidOperationException("Save the current scene before assembling the garden.");
        string modelPath=Root+"/E_SubmergedGarden.fbx";
        var importer=AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if(importer==null)throw new InvalidOperationException("Blender garden FBX has not imported.");
        importer.isReadable=true;importer.importCameras=false;importer.importLights=false;
        importer.importAnimation=false;importer.globalScale=1;
        importer.materialImportMode=ModelImporterMaterialImportMode.None;
        importer.SaveAndReimport();
        Directory.CreateDirectory(Root+"/Materials");AssetDatabase.Refresh();
        var scene=EditorSceneManager.OpenScene(RedonSceneBuilder.ScenePath,OpenSceneMode.Single);
        EditorSceneManager.SaveScene(scene,ScenePath);
        foreach(var go in scene.GetRootGameObjects())
            if(new[]{"Petal Gardens","Painted Cliffs","Anchored Pigment and Golden Spores"}.Contains(go.name))
                Object.DestroyImmediate(go);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var garden=(GameObject)PrefabUtility.InstantiatePrefab(source);
        garden.name="E - Sculpted Submerged Garden";
        garden.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
        // Authoring coordinates already account for Blender/Unity handedness.
        garden.transform.localScale=Vector3.one;
        var leaf=Pigment("Leaves",.42f,.20f,.32f,.96f);
        leaf.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/BotanicalGlaze.png"));
        leaf.SetFloat("_TextureColor",.88f);leaf.SetFloat("_Glaze",.20f);leaf.SetFloat("_Transmission",.8f);EditorUtility.SetDirty(leaf);
        var veins=Pigment("Veins",.15f,.035f,.2f,.92f);
        var petals=Pigment("Petals",.36f,.15f,.45f,1.13f);
        petals.SetTexture("_GlazeTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/ApricotImpasto.png"));
        petals.SetFloat("_Glaze",.78f);petals.SetFloat("_GlazeMidpoint",.35f);petals.SetFloat("_BrushRelief",.028f);petals.SetFloat("_Transmission",.32f);EditorUtility.SetDirty(petals);
        var coolCoral=Pigment("Violet Coral",.60f,.12f,.25f,1.16f);
        coolCoral.SetFloat("_Glaze",.88f);EditorUtility.SetDirty(coolCoral);
        var stone=Pigment("Reef",.56f,.08f,.24f,.92f);
        stone.SetFloat("_Glaze",.84f);EditorUtility.SetDirty(stone);
        var reefBloom=Pigment("Reef Blooms",.58f,.035f,.30f,.88f);
        var fanVeins=Pigment("Fan veins",.22f,.08f,.25f,.82f);
        var gold=Pigment("Botanical Gold",.32f,.08f,.12f,1.10f);
        string brushPath=Root+"/Materials/Surface Brush.mat";
        var brush=AssetDatabase.LoadAssetAtPath<Material>(brushPath);
        if(brush==null){brush=new Material(Shader.Find("Boids/Atelier/Surface Brush"));AssetDatabase.CreateAsset(brush,brushPath);}
        brush.SetTexture("_BrushAtlas",AssetDatabase.LoadAssetAtPath<Texture2D>(RedonSceneBuilder.Root+"/Textures/DryBrushAtlas.png"));
        brush.SetFloat("_Opacity",.73f);EditorUtility.SetDirty(brush);
        foreach(var r in garden.GetComponentsInChildren<MeshRenderer>())
        {
            var name=r.name;
            if(name.StartsWith("02"))r.enabled=false;
            r.sharedMaterial=name.StartsWith("01")?leaf:name.StartsWith("02")?veins:
                name.StartsWith("03")?petals:name.StartsWith("04")||name.StartsWith("10")?coolCoral:
                name.StartsWith("09")||name.StartsWith("13")?gold:name.StartsWith("11")?brush:name.StartsWith("12")?reefBloom:name.StartsWith("14")?fanVeins:stone;
            r.shadowCastingMode=ShadowCastingMode.TwoSided;r.receiveShadows=true;
        }
        var settings=Object.FindObjectOfType<RedonStyleSettings>();
        settings.waterColor=Hex("397A9A");settings.upperWaterColor=Hex("7E84A4");
        settings.warmColor=Hex("FFB566");settings.warmFocus=new Vector3(2.5f,11.0f,24);
        settings.warmExtent=new Vector2(4.0f,4.7f);settings.fogStart=32;settings.fogDensity=.037f;
        settings.lightDirection=new Vector3(.6f,.65f,.4f);settings.Apply();
        foreach(var light in Object.FindObjectsOfType<Light>())
        {light.shadows=LightShadows.Soft;light.shadowStrength=.8f;light.shadowBias=.025f;light.shadowNormalBias=.04f;light.transform.rotation=Quaternion.LookRotation(-settings.lightDirection);}
        var atmosphere=GameObject.Find("Painted Water Depth").GetComponent<MeshRenderer>();
        var atmosphereMat=new Material(atmosphere.sharedMaterial);
        string atmospherePath=Root+"/Materials/Water.mat";
        var existing=AssetDatabase.LoadAssetAtPath<Material>(atmospherePath);
        if(existing==null){AssetDatabase.CreateAsset(atmosphereMat,atmospherePath);existing=atmosphereMat;}
        else{EditorUtility.CopySerialized(atmosphereMat,existing);Object.DestroyImmediate(atmosphereMat);}
        existing.shader=Shader.Find("Boids/Atelier/Water Glaze");
        existing.SetTexture("_GlazeTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/SymbolistPigment.png"));
        existing.SetColor("_DeepColor",Hex("173C60"));existing.SetColor("_GoldColor",Hex("F7B565"));
        existing.SetColor("_PearlColor",Hex("FFE0A3"));existing.SetFloat("_TextureStrength",.8f);
        atmosphere.sharedMaterial=existing;EditorUtility.SetDirty(existing);
        var volume=Object.FindObjectOfType<Volume>();
        var profile=Object.Instantiate(volume.sharedProfile);
        profile.name="Atelier Grade";
        string profilePath=Root+"/AtelierGrade.asset";
        var oldProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if(oldProfile!=null)AssetDatabase.DeleteAsset(profilePath);
        AssetDatabase.CreateAsset(profile,profilePath);
        for(int i=0;i<profile.components.Count;i++)
        {
            profile.components[i]=Object.Instantiate(profile.components[i]);
            AssetDatabase.AddObjectToAsset(profile.components[i],profile);
        }
        ColorAdjustments grade;if(profile.TryGet(out grade)){grade.postExposure.Override(.12f);grade.contrast.Override(8);grade.saturation.Override(-3);}
        Vignette vignette;if(profile.TryGet(out vignette)){vignette.intensity.Override(.08f);}
        volume.sharedProfile=profile;
        ConfigureFish();
        EditorUtility.SetDirty(profile);EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Capture("Atelier_01");
        Debug.Log("Blender sculpted garden assembled. Review the actual camera render before accepting the artwork.");
    }
    static void ConfigureFish()
    {
        var root=GameObject.Find("Fish - Living School");
        var colors=new[]{"C7BEA9","B8999A","81A3A5","3D657E","AC9C83"};
        var mats=new Material[colors.Length];
        var fins=new Material[colors.Length];
        for(int i=0;i<mats.Length;i++)
        {
            mats[i]=Pigment("Fish "+i,.18f,.09f,.2f,.9f);
            mats[i].shader=Shader.Find("Boids/Atelier/Living Pearl");
            mats[i].SetFloat("_VertexColor",0);mats[i].SetColor("_BaseColor",Hex(colors[i]));
            mats[i].SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Boids/Art/Moonveil/Textures/Moonveil_Body_BaseColor.png"));
            mats[i].SetFloat("_Membrane",0);mats[i].SetInt("_ZWrite",1);mats[i].renderQueue=2000;
            string finPath=Root+"/Materials/Fins "+i+".mat";
            fins[i]=AssetDatabase.LoadAssetAtPath<Material>(finPath);
            if(fins[i]==null){fins[i]=new Material(mats[i]);AssetDatabase.CreateAsset(fins[i],finPath);}
            EditorUtility.CopySerialized(mats[i],fins[i]);fins[i].name="Fins "+i;
            fins[i].SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Boids/Art/Moonveil/Textures/Moonveil_Fins_BaseColor.png"));
            fins[i].SetFloat("_Membrane",1);fins[i].SetInt("_ZWrite",0);fins[i].renderQueue=3000;
            EditorUtility.SetDirty(mats[i]);
            EditorUtility.SetDirty(fins[i]);
        }
        int k=0;
        foreach(Transform fish in root.transform)
        {
            float t=k/95f;
            var p=fish.position;
            fish.position=new Vector3(p.x*.88f-.3f,p.y+.5f*(1-t),p.z);
            fish.localScale=Vector3.Scale(fish.localScale,new Vector3(1,.82f,1.08f));
            // A few foreground individuals carry the eye into the receding school.
            if(k==6 || k==15 || k==23)fish.localScale*=1.26f;
            int index=k>45?3:k%5;
            foreach(var r in fish.GetComponentsInChildren<SkinnedMeshRenderer>())r.sharedMaterial=r.name.Contains("Membranes")?fins[index]:mats[index];
            k++;
        }
    }
    [MenuItem("Boids/Atelier/Capture Camera")]
    public static void CaptureMenu(){Capture("Atelier_Current");}
    [MenuItem("Boids/Atelier/Validate Scene")]
    public static void Validate()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!=ScenePath)throw new InvalidOperationException("Open RedonAtelier first.");
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        int missing=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        var camera=Camera.main;
        bool fixedCamera=camera!=null && camera.GetComponent<RedonFixedFrame>()!=null
            && camera.transform.position==new Vector3(0,6,-30) && Quaternion.Angle(camera.transform.rotation,Quaternion.identity)<.01f;
        var mats=all.SelectMany(t=>t.GetComponents<Renderer>()).SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
        var errors=mats.Where(m=>m!=null && m.shader!=null).Select(m=>m.shader).Distinct()
            .SelectMany(s=>ShaderUtil.GetShaderMessages(s)).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
            .Select(m=>m.message).ToArray();
        int fish=all.Count(t=>t.GetComponent<MoonveilMotion>()!=null);
        var garden=GameObject.Find("E - Sculpted Submerged Garden");
        int modeledForms=garden==null?0:garden.GetComponentsInChildren<MeshFilter>().Count(f=>f.sharedMesh!=null);
        bool schoolReady=Object.FindObjectOfType<DreamSchoolController>()!=null;
        bool passed=missing==0 && fixedCamera && fish==96 && modeledForms>=25 && schoolReady && errors.Length==0
            && mats.All(m=>m!=null && m.shader!=null && m.shader.isSupported);
        Directory.CreateDirectory("Captures");
        File.WriteAllText("Captures/Atelier_Validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(new
        {passed,scene=scene.path,missingScripts=missing,fixedCamera,fishCount=fish,modeledForms,schoolReady,shaderErrors=errors,
         note="Technical scene integrity only. Artistic quality requires camera review."},Newtonsoft.Json.Formatting.Indented));
        if(!passed)throw new InvalidOperationException("Atelier scene integrity failed; inspect Captures/Atelier_Validation.json.");
    }
    public static void Capture(string name)
    {
        var camera=Camera.main;var previous=camera.targetTexture;var active=RenderTexture.active;
        var rt=new RenderTexture(1536,1024,24,RenderTextureFormat.ARGB32);
        var tex=new Texture2D(1536,1024,TextureFormat.RGB24,false);
        var previousRect=camera.rect;
        try
        {
            camera.rect=new Rect(0,0,1,1);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            tex.ReadPixels(new Rect(0,0,1536,1024),0,0);tex.Apply();
            Directory.CreateDirectory("Captures");File.WriteAllBytes("Captures/"+name+".png",tex.EncodeToPNG());
        }
        finally{camera.rect=previousRect;camera.targetTexture=previous;RenderTexture.active=active;Object.DestroyImmediate(tex);rt.Release();Object.DestroyImmediate(rt);}
    }
}
