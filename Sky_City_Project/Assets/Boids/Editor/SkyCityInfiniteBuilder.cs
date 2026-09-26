using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Boids.Art;
using Boids.Art.Infinite;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

public static class SkyCityInfiniteBuilder
{
    public const string Root="Assets/Boids/Art/SkyCityInfinite";
    public const string ScenePath="Assets/Boids/Scenes/SkyCityInfinite.unity";
    static readonly string[] Families={"ArcadedPromenade","TurningLoggia","MarketColonnade","FountainPiazza","HangingBelvedere","DomedSanctuary","BellCampanile","GardenCloister","TerracedOrchard","PalaceLibrary","SkyAqueduct","CurvedSkybridge","CeremonialGate","CelestialObservatory","GlassConservatory","CrownPalace"};
    public static readonly string[] Variants={"Dawn","Iris","Cypress","Pearl","Saffron","Linden","Azure","Solstice"};
    static Material Material(string name,string shader)
    {
        var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/"+name+".mat");
        if(mat==null){mat=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(mat,Root+"/"+name+".mat");}
        return mat;
    }
    [MenuItem("Boids/Sky City Infinite/Build streaming world")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Leave play mode first.");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the open scene before building.");
        Directory.CreateDirectory(Root+"/Modules");AssetDatabase.Refresh();
        var architecture=Material("Ivory copper and silk","Boids/SkyCity/Modular Architecture");architecture.enableInstancing=true;
        architecture.SetTexture("_StoneTex",AssetDatabase.LoadAssetAtPath<Texture2D>(SkyCitySceneBuilder.Root+"/Textures/WeatheredLimestone.png"));EditorUtility.SetDirty(architecture);
        // The runtime flock has no baked scene renderers from which Unity can infer variants.
        var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var instancing=graphics.FindProperty("m_InstancingStripping");instancing.intValue=2;graphics.ApplyModifiedPropertiesWithoutUndo();
        var water=Material("Infinite reflecting water","Boids/SkyCity/Reflecting Garden Water");
        water.SetFloat("_MultipleElevations",1);water.SetFloat("_OpticalDepth",1.1f);EditorUtility.SetDirty(water);
        var cascades=Material("Living transparent cascades","Boids/SkyCity/Infinite Cascades");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        int rendererIndex=Renderer();
        var camera=new GameObject("Voyager camera").AddComponent<Camera>();camera.tag="MainCamera";
        camera.transform.position=new Vector3(80,47,-64);camera.transform.LookAt(new Vector3(50,27,44));
        camera.fieldOfView=46;camera.nearClipPlane=.3f;camera.farClipPlane=570;camera.allowHDR=true;
        camera.clearFlags=CameraClearFlags.Skybox;camera.gameObject.AddComponent<AudioListener>();
        var cameraData=camera.GetUniversalAdditionalCameraData();cameraData.SetRenderer(rendererIndex);
        cameraData.renderPostProcessing=true;cameraData.requiresDepthTexture=true;cameraData.requiresColorTexture=true;
        cameraData.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        var sun=new GameObject("Apricot morning sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.95f;
        sun.color=new Color(1,.89f,.76f);sun.transform.rotation=Quaternion.Euler(34,-36,0);sun.shadows=LightShadows.Soft;
        sun.shadowBias=.02f;sun.shadowNormalBias=.15f;sun.shadowStrength=.82f;RenderSettings.sun=sun;
        var sky=Material("Pearl horizon","Boids/SkyCity/Dawn Sky");sky.SetColor("_Zenith",new Color(.23f,.42f,.61f));sky.SetColor("_Horizon",new Color(.81f,.73f,.64f));
        RenderSettings.skybox=sky;EditorUtility.SetDirty(sky);
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.5f,.62f,.76f);
        RenderSettings.ambientEquatorColor=new Color(.46f,.49f,.62f);RenderSettings.ambientGroundColor=new Color(.34f,.36f,.43f);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.66f,.74f,.83f);
        RenderSettings.fogStartDistance=175;RenderSettings.fogEndDistance=560;
        var grade=new GameObject("Pearl dawn grade").AddComponent<Volume>();grade.isGlobal=true;
        grade.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(SkyCitySceneBuilder.Root+"/SkyCityGrade.asset");
        var world=new GameObject("Bounded streaming world").AddComponent<SkyCityInfiniteWorld>();world.view=camera;
        world.architectureMaterial=architecture;world.waterMaterial=water;world.cascadeMaterial=cascades;
        var voyager=camera.gameObject.AddComponent<SkyCityVoyager>();voyager.world=world;
        var reflection=new GameObject("One shared pool reflection").AddComponent<SkyCityWaterReflection>();
        reflection.waterHeight=18.38f;reflection.rendererIndex=rendererIndex;reflection.textureWidth=512;
        reflection.updatesPerSecond=30;reflection.maximumReflectionDistance=230;
        var audio=new GameObject("Garden of Winds original score").AddComponent<AudioSource>();
        audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(SkyCitySceneBuilder.Root+"/Audio/GardenOfWinds.ogg");audio.playOnAwake=false;audio.loop=true;
        audio.gameObject.AddComponent<SkyCitySoundscape>();
        var flock=new GameObject("Travelling swallows").AddComponent<SkyCityVoyagerBirds>();
        flock.view=camera;flock.world=world;flock.material=architecture;flock.birdMesh=BuildBird();
        var benchmark=new GameObject("Streaming verification").AddComponent<SkyCityStreamingProbe>();benchmark.world=world;benchmark.voyager=voyager;
        SkyCityRenderingSetup.ApplyToScene();
        EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
        if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
        Debug.Log("Infinite Sky City saved: bootstrap only; zero resident districts in the scene file.");
    }
    static int Renderer()
    {
        string path=Root+"/InfiniteRenderer.asset";
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
        if(renderer==null)
        {
            renderer=Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/URP-HighFidelity-Renderer.asset"));
            renderer.name="InfiniteRenderer";renderer.rendererFeatures.Clear();AssetDatabase.CreateAsset(renderer,path);
        }
        if(!renderer.rendererFeatures.Any(f=>f!=null&&f.name=="Soft architectural contact shadows"))
        {
            var source=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/URP-HighFidelity-Renderer.asset");
            var template=source.rendererFeatures.FirstOrDefault(f=>f!=null&&f.name=="SSAO");
            if(template!=null)
            {
                var ao=Object.Instantiate(template);ao.name="Soft architectural contact shadows";AssetDatabase.AddObjectToAsset(ao,renderer);
                var settings=new SerializedObject(ao);settings.FindProperty("m_Settings.Source").intValue=0;
                settings.FindProperty("m_Settings.AfterOpaque").boolValue=true;settings.FindProperty("m_Settings.Downsample").boolValue=true;
                settings.FindProperty("m_Settings.Radius").floatValue=1.15f;settings.FindProperty("m_Settings.Intensity").floatValue=.85f;
                settings.FindProperty("m_Settings.Falloff").floatValue=220;settings.ApplyModifiedPropertiesWithoutUndo();
                renderer.rendererFeatures.Insert(0,ao);ao.Create();EditorUtility.SetDirty(ao);
            }
        }
        var atmosphere=Material("Endless volumetric cloud sea","Boids/SkyCity/Atmosphere Raymarch");
        atmosphere.SetTexture("_NoiseTex",AssetDatabase.LoadAssetAtPath<Texture3D>(SkyCitySceneBuilder.Root+"/PerlinWorleyVolume.asset"));
        atmosphere.SetFloat("_InfiniteMode",1);atmosphere.SetFloat("_Steps",96);atmosphere.SetFloat("_LightSteps",4);
        atmosphere.EnableKeyword("SKY_INFINITE_CLOUDS");
        atmosphere.SetFloat("_Coverage",.53f);atmosphere.SetFloat("_Density",.48f);atmosphere.SetFloat("_Detail",.065f);
        atmosphere.SetColor("_SunColor",new Color(1.85f,1.6f,1.3f));atmosphere.SetColor("_ShadeColor",new Color(.24f,.27f,.38f));
        SkyCityAtmosphereFeature feature=renderer.rendererFeatures.OfType<SkyCityAtmosphereFeature>().FirstOrDefault();
        if(feature==null){feature=ScriptableObject.CreateInstance<SkyCityAtmosphereFeature>();feature.name="Bounded cost volumetric atmosphere";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);}
        feature.cloudMaterial=atmosphere;feature.resolutionScale=.5f;feature.SetActive(true);feature.Create();renderer.SetDirty();
        EditorUtility.SetDirty(atmosphere);EditorUtility.SetDirty(feature);EditorUtility.SetDirty(renderer);
        var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        var serialized=new SerializedObject(pipeline);var list=serialized.FindProperty("m_RendererDataList");
        for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==renderer)return i;
        int index=list.arraySize;list.arraySize++;list.GetArrayElementAtIndex(index).objectReferenceValue=renderer;
        serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);return index;
    }
    static Mesh BuildBird()
    {
        string path=Root+"/InstancedSwallow.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(SkyCitySceneBuilder.Root+"/SkyCity_Swallow.fbx");
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var triangles=new List<int>();var colors=new List<Color32>();
        foreach(var filter in model.GetComponentsInChildren<MeshFilter>())
        {
            var source=filter.sharedMesh;var transform=model.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
            var vv=source.vertices;var nn=source.normals;var cc=source.colors32;int start=vertices.Count;
            for(int i=0;i<vv.Length;i++)
            {
                vertices.Add(transform.MultiplyPoint3x4(vv[i]));normals.Add(transform.MultiplyVector(nn[i]).normalized);
                Color32 color=cc.Length==vv.Length?cc[i]:new Color32(215,211,194,255);color.a=255;colors.Add(color);
            }
            foreach(int index in source.triangles)triangles.Add(index+start);
        }
        var mesh=new Mesh{name="InstancedSwallow"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.SetNormals(normals);
        mesh.uv=new Vector2[vertices.Count];mesh.RecalculateBounds();
        if(existing!=null){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;}
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    [MenuItem("Boids/Sky City Infinite/Bake 128 reusable prefabs")]
    public static void BakePrefabs()
    {
        Directory.CreateDirectory(Root+"/Modules");AssetDatabase.Refresh();
        var data=SkyCityModuleData.Read(File.ReadAllBytes("Assets/Boids/Resources/SkyCityInfinite/Modules.bytes"),CancellationToken.None);
        var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Ivory copper and silk.mat");
        var water=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Infinite reflecting water.mat");
        AssetDatabase.StartAssetEditing();
        try
        {
            for(int id=0;id<SkyCityWfc.ModuleCount;id++)
            {
                string name=id.ToString("000")+"_"+Families[id/8]+"_"+Variants[id%8];string asset=Root+"/Modules/"+name+".asset";
                var root=new GameObject(name);var lods=new LOD[3];
                var descriptor=root.AddComponent<SkyCityModuleDescriptor>();descriptor.moduleId=id;descriptor.architecturalFamily=Families[id/8];
                descriptor.variant=Variants[id%8];descriptor.socketMask=SkyCityWfc.FamilyMasks[id/8];
                for(int lod=0;lod<3;lod++)
                {
                    var mesh=SkyCityModuleData.Upload(data.modules[id,lod,0],name+" LOD"+lod);
                    var saved=AssetDatabase.LoadAllAssetsAtPath(asset).OfType<Mesh>().FirstOrDefault(m=>m.name==mesh.name);
                    // Legacy main meshes use the parcel name without " LOD0".
                    // Reuse that object so rebaking cannot replace its subasset IDs.
                    if(saved==null&&lod==0)saved=AssetDatabase.LoadAssetAtPath<Mesh>(asset);
                    if(saved!=null){mesh.name=saved.name;EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(mesh);}
                    else if(lod==0)AssetDatabase.CreateAsset(mesh,asset);else AssetDatabase.AddObjectToAsset(mesh,asset);
                    var child=new GameObject("LOD"+lod);child.transform.SetParent(root.transform,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;
                    var renderer=child.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;lods[lod]=new LOD(new[]{.18f,.065f,.005f}[lod],new Renderer[]{renderer});
                }
                if(data.modules[id,0,1].indices.Length>0)
                {
                    var mesh=SkyCityModuleData.Upload(data.modules[id,0,1],name+" water");
                    var saved=AssetDatabase.LoadAllAssetsAtPath(asset).OfType<Mesh>().FirstOrDefault(m=>m.name==mesh.name);
                    if(saved!=null){EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(mesh);}else AssetDatabase.AddObjectToAsset(mesh,asset);
                    var child=new GameObject("Pool");child.layer=4;child.transform.SetParent(root.transform,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;
                    child.AddComponent<MeshRenderer>().sharedMaterial=water;
                }
                var group=root.AddComponent<LODGroup>();group.SetLODs(lods);group.RecalculateBounds();
                PrefabUtility.SaveAsPrefabAsset(root,Root+"/Modules/"+name+".prefab");Object.DestroyImmediate(root);
            }
        }
        finally{AssetDatabase.StopAssetEditing();AssetDatabase.SaveAssets();}
        Debug.Log("128 editable module prefabs baked. Runtime references only the compressed shared vocabulary.");
    }
}
