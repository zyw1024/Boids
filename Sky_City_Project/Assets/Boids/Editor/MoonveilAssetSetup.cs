using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Boids.Art;

public static class MoonveilAssetSetup
{
    public const string Root = "Assets/Boids/Art/Moonveil";
    public const string ModelPath = Root + "/Moonveil.fbx";
    public const string PrefabPath = Root + "/Moonveil.prefab";
    public const string ScenePath = "Assets/Boids/Scenes/MoonveilPreview.unity";

    [MenuItem("Boids/Moonveil/Build Art Preview")]
    public static void Build()
    {
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Animation");
        Directory.CreateDirectory("Assets/Boids/Scenes");
        AssetDatabase.Refresh();
        foreach (string file in Directory.GetFiles(Root + "/Textures", "*.png"))
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.maxTextureSize = 1024;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.mipmapEnabled = true;
            if (file.Contains("Normal")) ti.textureType = TextureImporterType.NormalMap;
            ti.SaveAndReimport();
        }
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.importAnimation = true;
        importer.importCameras = false; importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.optimizeGameObjects = false;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        importer.isReadable = false;
        var settings = importer.defaultClipAnimations;
        foreach (var clip in settings)
        {
            clip.loopTime = clip.name != "Feed";
            clip.loopPose = clip.loopTime;
            clip.lockRootPositionXZ = true; clip.lockRootHeightY = true; clip.lockRootRotation = true;
            clip.keepOriginalPositionXZ = true; clip.keepOriginalPositionY = true; clip.keepOriginalOrientation = true;
        }
        importer.clipAnimations = settings;
        importer.SaveAndReimport();
        var clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).ToDictionary(c => c.name);
        if (clips.Count != 4) throw new Exception("Expected four imported Moonveil clips.");

        var body = Material("Moonveil_Body");
        body.SetTexture("_BaseMap",Texture("Moonveil_Body_BaseColor"));
        body.SetTexture("_BumpMap",Texture("Moonveil_Body_Normal"));body.EnableKeyword("_NORMALMAP");
        body.SetFloat("_BumpScale",.28f);body.SetFloat("_Metallic",.34f);body.SetFloat("_Smoothness",.65f);
        body.SetTexture("_EmissionMap",Texture("Moonveil_Body_Emission"));
        body.SetColor("_EmissionColor",Color.white*.65f);body.EnableKeyword("_EMISSION");
        var fins = Material("Moonveil_Fins");
        fins.SetTexture("_BaseMap",Texture("Moonveil_Fins_BaseColor"));
        fins.SetFloat("_Metallic",.18f);fins.SetFloat("_Smoothness",.65f);
        fins.SetFloat("_Surface",1);fins.SetFloat("_Blend",0);fins.SetFloat("_Cull",0);fins.SetFloat("_ZWrite",0);
        fins.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);fins.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        fins.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);fins.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
        fins.SetOverrideTag("RenderType","Transparent");fins.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        fins.SetShaderPassEnabled("ShadowCaster",false);fins.renderQueue=(int)RenderQueue.Transparent;

        string controllerPath = Root + "/Animation/Moonveil.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        foreach (var state in controller.layers[0].stateMachine.states)
            controller.layers[0].stateMachine.RemoveState(state.state);
        controller.parameters = Array.Empty<AnimatorControllerParameter>();
        controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
        controller.AddParameter("Feed",AnimatorControllerParameterType.Trigger);
        BlendTree tree;
        var swimState = controller.CreateBlendTreeInController("Locomotion",out tree);
        tree.blendType=BlendTreeType.Simple1D;tree.blendParameter="Speed";tree.useAutomaticThresholds=false;
        tree.AddChild(clips["Hover"],0);tree.AddChild(clips["Swim"],.45f);tree.AddChild(clips["Dart"],1);
        var machine=controller.layers[0].stateMachine;machine.defaultState=swimState;
        var feed=machine.AddState("Feed");feed.motion=clips["Feed"];
        var enter=swimState.AddTransition(feed);enter.hasExitTime=false;enter.duration=.12f;enter.hasFixedDuration=true;
        enter.AddCondition(AnimatorConditionMode.If,0,"Feed");
        var leave=feed.AddTransition(swimState);leave.hasExitTime=true;leave.exitTime=.93f;leave.duration=.14f;leave.hasFixedDuration=true;
        EditorUtility.SetDirty(controller);

        var wrapper = new GameObject("Moonveil");
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
        visual.name="Visual";visual.transform.SetParent(wrapper.transform,false);
        // Blender +X is imported as Unity -X. Rotate to the conventional agent forward +Z.
        visual.transform.localRotation=Quaternion.Euler(0,90,0);visual.transform.localScale=Vector3.one*.2f;
        var animator=visual.GetComponent<Animator>();
        if (animator==null) animator=visual.AddComponent<Animator>();
        animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
        foreach(var r in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            r.sharedMaterial=r.name.Contains("Membranes")?fins:body;
            r.localBounds=new Bounds(Vector3.zero,new Vector3(6,4,4));
            r.shadowCastingMode=r.name.Contains("Membranes")?ShadowCastingMode.Off:ShadowCastingMode.On;
        }
        var motion=wrapper.AddComponent<MoonveilMotion>();motion.Configure(animator);
        var prefab=PrefabUtility.SaveAsPrefabAsset(wrapper,PrefabPath);
        UnityEngine.Object.DestroyImmediate(wrapper);

        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.36f,.50f,.56f);
        var sh=new SphericalHarmonicsL2();sh.AddAmbientLight(new Color(.25f,.34f,.39f));RenderSettings.ambientProbe=sh;
        RenderSettings.fog=false;
        var fish=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var camGo=new GameObject("Moonveil Camera");camGo.tag="MainCamera";
        var cam=camGo.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=.33f;
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.055f,.105f,.14f);
        cam.nearClipPlane=.01f;cam.farClipPlane=20;cam.allowHDR=true;cam.allowMSAA=true;
        cam.transform.position=new Vector3(1.44f,.42f,.70f);cam.transform.LookAt(new Vector3(0,0,-.07f));
        var urp=cam.GetUniversalAdditionalCameraData();urp.renderPostProcessing=true;urp.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        Light("Pearl Key",new Vector3(35,-135,0),new Color(.79f,.93f,1),1.6f);
        Light("Lavender Rim",new Vector3(25,45,0),new Color(.63f,.52f,1),1.0f);
        Light("Sea Glass Fill",new Vector3(-25,-90,0),new Color(.64f,1,.87f),.9f);
        var volume=new GameObject("Ocean Grading").AddComponent<Volume>();volume.isGlobal=true;
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/MoonveilPreviewVolume.asset");
        if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Root+"/MoonveilPreviewVolume.asset");}
        Bloom bloom;if(!profile.TryGet(out bloom))bloom=profile.Add<Bloom>(true);
        bloom.threshold.Override(1f);bloom.intensity.Override(.18f);bloom.scatter.Override(.6f);
        Tonemapping tone;if(!profile.TryGet(out tone))tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
        Vignette vignette;if(!profile.TryGet(out vignette))vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.24f);vignette.smoothness.Override(.7f);
        volume.sharedProfile=profile;EditorUtility.SetDirty(profile);
        var preview=new GameObject("Motion Preview").AddComponent<MoonveilPreview>();
        preview.fish=fish.GetComponent<MoonveilMotion>();preview.previewCamera=cam;
        EditorSceneManager.SaveScene(scene,ScenePath);
        AssetDatabase.SaveAssets();Selection.activeGameObject=fish;
        if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.FrameSelected();
        Debug.Log("Moonveil ready: 4 clips, 13 bones, 2 skinned meshes. Preview: "+ScenePath);
    }

    [MenuItem("Boids/Moonveil/Verify Imported Animation")]
    public static void Verify()
    {
        var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        try
        {
            var animator=obj.GetComponentInChildren<Animator>();animator.enabled=false;
            var renderers=obj.GetComponentsInChildren<SkinnedMeshRenderer>();
            var clips=AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            var results=new System.Collections.Generic.List<object>();
            foreach(var clip in clips)
            {
                clip.SampleAnimation(animator.gameObject,0);var start=BakedVertices(renderers);
                clip.SampleAnimation(animator.gameObject,clip.length);var end=BakedVertices(renderers);
                clip.SampleAnimation(animator.gameObject,clip.length*.25f);var mid=BakedVertices(renderers);
                float seam=0,motion=0;
                for(int i=0;i<start.Length;i++){seam=Mathf.Max(seam,Vector3.Distance(start[i],end[i]));motion=Mathf.Max(motion,Vector3.Distance(start[i],mid[i]));}
                if(motion<.001f)throw new Exception(clip.name+" did not deform the imported mesh.");
                if(clip.isLooping && seam>.001f)throw new Exception(clip.name+" has a visible loop seam: "+seam);
                results.Add(new {name=clip.name,seconds=clip.length,loop=clip.isLooping,maxSeam=seam,maxMotion=motion});
            }
            var front=animator.GetComponentsInChildren<Transform>().Single(t=>t.name=="Spine_Front");
            bool forwardOk=Vector3.Dot(front.position-obj.transform.position,obj.transform.forward)>0;
            if(!forwardOk)throw new Exception("Prefab forward axis is wrong.");
            var report=new {passed=true,forwardPositiveZ=forwardOk,skinnedMeshes=renderers.Length,materials=renderers.Sum(r=>r.sharedMaterials.Length),triangles=renderers.Sum(r=>r.sharedMesh.triangles.Length/3),clips=results};
            Directory.CreateDirectory("Captures");
            File.WriteAllText("Captures/Moonveil_Validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
            Debug.Log("Moonveil animation and prefab validation passed.");
        }
        finally {UnityEngine.Object.DestroyImmediate(obj);}
    }

    static Vector3[] BakedVertices(SkinnedMeshRenderer[] renderers)
    {
        var list=new System.Collections.Generic.List<Vector3>();
        foreach(var renderer in renderers)
        {
            var mesh=new Mesh();renderer.BakeMesh(mesh);list.AddRange(mesh.vertices);UnityEngine.Object.DestroyImmediate(mesh);
        }
        return list.ToArray();
    }

    [MenuItem("Boids/Moonveil/Capture Preview")]
    public static void Capture()
    {
        var cam=UnityEngine.Object.FindObjectOfType<MoonveilPreview>().previewCamera;
        var previous=cam.targetTexture;var active=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(1500,1000,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        var image=new Texture2D(1500,1000,TextureFormat.RGB24,false);
        try
        {
            cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            image.ReadPixels(new Rect(0,0,1500,1000),0,0);image.Apply();
            Directory.CreateDirectory("Captures");File.WriteAllBytes("Captures/Moonveil_Unity.png",image.EncodeToPNG());
        }
        finally {cam.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
    }

    static Material Material(string name)
    {
        string path=Root+"/Materials/"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name=name;AssetDatabase.CreateAsset(mat,path);}
        return mat;
    }
    static Texture2D Texture(string name)=>AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+name+".png");
    static void Light(string name,Vector3 rotation,Color color,float power)
    {
        var go=new GameObject(name);go.transform.rotation=Quaternion.Euler(rotation);
        var light=go.AddComponent<Light>();light.type=LightType.Directional;light.color=color;light.intensity=power;
    }
}
