using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Boids.Art;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

/// <summary>Deterministic authoring of a fixed-camera, three-dimensional painterly scene.</summary>
public static partial class RedonSceneBuilder
{
    public const string Root = "Assets/Boids/Art/Redon";
    public const string ScenePath = "Assets/Boids/Scenes/RedonDream.unity";
    static readonly List<Vector3> vertices = new List<Vector3>();
    static readonly List<Vector2> uvs = new List<Vector2>();
    static readonly List<Color> colors = new List<Color>();
    static readonly List<int> triangles = new List<int>();
    static readonly List<Vector3> normals = new List<Vector3>();
    static Texture2D pigment, atlas, underpainting, petalVeining;
    static Mesh[] rocks;
    static Material[] foliage, foliageBrushes, stone, fishPaint;
    static Material brushGold, brushDistant;
    static Transform plantsRoot, rockRoot, fishRoot, brushRoot;
    static bool compositionDraft;

    [MenuItem("Boids/Redon/Build Fixed Camera Style Scene")]
    public static void Build()
    {
        compositionDraft = false;
        BuildScene();
    }

    [MenuItem("Boids/Redon/Build Composition Draft")]
    public static void BuildCompositionDraft()
    {
        compositionDraft = true;
        try { BuildScene(); }
        finally { compositionDraft = false; }
    }

    static void BuildScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Build the art scene in Edit mode.");
        Scene previous = SceneManager.GetActiveScene();
        if (previous.isDirty && previous.path != ScenePath)
            throw new InvalidOperationException("The current scene has unsaved edits. Save it before building.");
        pigment = AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/PigmentScumble.png");
        atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/DryBrushAtlas.png");
        underpainting = AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/PetalUnderpainting.png");
        petalVeining = AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/PetalVeining.png");
        if (!compositionDraft && (pigment == null || atlas == null || underpainting == null || petalVeining == null))
            throw new InvalidOperationException("All four generated pigment textures must be imported first.");
        if (pigment == null) pigment = Texture2D.grayTexture;
        if (atlas == null) atlas = Texture2D.blackTexture;
        foreach (string dir in new[]{"Materials","Geometry","Rendering"})
            Directory.CreateDirectory(Root+"/"+dir);
        AssetDatabase.Refresh();
        ImportMask(pigment, TextureWrapMode.Mirror);
        ImportMask(atlas, TextureWrapMode.Clamp);
        foreach(var colorTexture in new[]{underpainting,petalVeining})
        {
            if(colorTexture==null)continue;
            var ti=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(colorTexture));
            ti.sRGBTexture=true;ti.wrapMode=colorTexture==petalVeining?TextureWrapMode.Clamp:TextureWrapMode.Mirror;ti.mipmapEnabled=true;
            ti.maxTextureSize=2048;ti.textureCompression=TextureImporterCompression.Uncompressed;
            ti.filterMode=FilterMode.Trilinear;ti.SaveAndReimport();
        }
        var randomState = Random.state;
        Random.InitState(73129);
        try
        {
            BuildMaterials();
            BuildGeometry();
            BuildGardenPalette();
            BuildFlowerGeometry();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var settings = new GameObject("Art Direction - Redon").AddComponent<RedonStyleSettings>();
            settings.warmExtent = new Vector2(3.8f,4.5f);
            settings.waterColor = Hex("2B6987");
            settings.upperWaterColor = Hex("65748E");
            settings.warmColor = Hex("EDB17D");
            settings.fogDensity = .019f;
            settings.paintedNormals = 0;
            settings.fogStart = 31f;
            settings.Apply();
            plantsRoot = new GameObject("Petal Gardens").transform;
            rockRoot = new GameObject("Painted Cliffs").transform;
            fishRoot = new GameObject("Fish - Living School").transform;
            brushRoot = new GameObject("Anchored Pigment and Golden Spores").transform;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Hex("697484");
            CreateCamera();
            CreateAtmosphere();
            CreateBotanicalGrove();
            CreateHeroPetals();
            CreateGardenAccents();
            CreateLivingSchool();
            CreateVolume();
            settings.Apply();
            EditorSceneManager.SaveScene(scene,ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = settings.gameObject;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.LookAt(new Vector3(0,6,5),Quaternion.identity,18);
            }
            Debug.Log("Redon dream garden built with a fixed camera, living Boids and click-to-feed interaction.");
        }
        finally { Random.state = randomState; }
    }

    static void ImportMask(Texture2D tex, TextureWrapMode wrap)
    {
        if (!AssetDatabase.GetAssetPath(tex).StartsWith("Assets/")) return;
        var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));
        importer.sRGBTexture = false;
        importer.mipmapEnabled = true;
        importer.wrapMode = wrap;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Trilinear;
        importer.SaveAndReimport();
    }

    static void BuildMaterials()
    {
        foliage = new[]
        {
            Paint("Petal_Lavender","8B8CAB","3C506E","C6AAC2",.48f,.22f,.6f),
            Paint("Petal_Indigo","3F5875","182E43","7D8196",.55f,.12f,.25f),
            Paint("Petal_Peacock","426E77","142E40","91A6A2",.48f,.1f,.35f),
            Paint("Petal_Rose","BD8FA3","5C577D","E2B6A4",.42f,.22f,.4f),
            Paint("Petal_Foreground","293D4D","111F31","626175",.55f,.08f,.23f),
            Paint("Petal_Distant","687A8F","344C64","A4A0B1",.34f,.12f,.18f)
        };
        foliageBrushes = new Material[foliage.Length];
        for(int i=0;i<foliage.Length;i++)
        {
            foliageBrushes[i]=Brush("Petal_Brush_"+i,foliage[i].GetColor("_BaseColor"),
                foliage[i].GetColor("_LightColor"),.30f,.85f);
        }
        stone = new[]
        {
            Paint("Cliff_Near","334958","162836","6E6578",.63f,.07f,0,1),
            Paint("Cliff_Middle","526378","2F405D","998994",.63f,.1f,0,1),
            Paint("Cliff_Far","667A90","42556E","9399A8",.46f,.03f,0,1),
            Paint("Cliff_Warm","8D6E83","3D415D","B99A9C",.54f,.16f,0,1)
        };
        fishPaint = new[]
        {
            Paint("Fish_Pearl","D0C7B5","4C7E88","F4E1B4",.24f,.12f,0),
            Paint("Fish_Peach","D6AD98","697887","F4D4A7",.28f,.13f,0),
            Paint("Fish_Jade","8DB7AB","365F75","E5D7AF",.29f,.12f,0),
            Paint("Fish_Dusk","718EA2","3B637D","B6C0BE",.30f,.09f,0),
            Paint("Fish_Gold","D2B980","586D7D","F8DFAD",.25f,.14f,0)
        };
        brushGold = Brush("Golden_Pigment",Hex("BA9868"),Hex("EFCC89"),.72f,.9f);
        brushDistant = Brush("Distant_Pigment",Hex("7E91A5"),Hex("C1A298"),.23f,.6f);
    }

    static Material Paint(string name,string main,string shadow,string light,float scumble,float rim,float veins,float world=0)
    {
        Material mat = Mat(name,"Boids/Redon/Painted Surface");
        mat.SetColor("_BaseColor",Hex(main));mat.SetColor("_ShadeColor",Hex(shadow));mat.SetColor("_LightColor",Hex(light));
        mat.SetTexture("_PigmentTex",pigment);mat.SetFloat("_Scumble",scumble);
        mat.SetTexture("_ColorTex",underpainting);
        mat.SetFloat("_Underpaint",name.StartsWith("Fish")?.06f:(name.Contains("Stems")?.1f:.38f));
        mat.SetFloat("_ColorUV",0);
        mat.SetFloat("_Rim",rim);mat.SetFloat("_Veins",veins);mat.SetFloat("_WorldUV",world);
        mat.SetFloat("_BrushScale",world>.5f?1.35f:1.1f);mat.SetFloat("_Seed",Random.Range(0,100));
        EditorUtility.SetDirty(mat);return mat;
    }
    static Material Brush(string name,Color tint,Color accent,float opacity,float variation)
    {
        var mat=Mat(name,"Boids/Redon/Anchored Brush");
        mat.SetTexture("_BrushAtlas",atlas);mat.SetColor("_Tint",tint);mat.SetColor("_Accent",accent);
        mat.SetFloat("_Opacity",opacity);mat.SetFloat("_ColorVariation",variation);mat.SetFloat("_FogAmount",1);
        EditorUtility.SetDirty(mat);return mat;
    }
    static Material Mat(string name,string shader)
    {
        string path=Root+"/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader s=Shader.Find(shader);
        if(s==null) throw new InvalidOperationException("Missing shader: "+shader);
        if(m==null){m=new Material(s){name=name};AssetDatabase.CreateAsset(m,path);}else m.shader=s;
        return m;
    }

    static void CreateCamera()
    {
        var go=new GameObject("Redon Fixed Camera");go.tag="MainCamera";
        var cam=go.AddComponent<Camera>();
        cam.transform.position=new Vector3(0,6,-30);cam.transform.rotation=Quaternion.identity;
        cam.orthographic=true;cam.orthographicSize=7.2f;cam.aspect=RedonFixedFrame.Aspect;
        cam.nearClipPlane=.1f;cam.farClipPlane=110;
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Hex("22394B");
        cam.allowHDR=true;cam.allowMSAA=true;
        var data=cam.GetUniversalAdditionalCameraData();
        data.renderPostProcessing=true;
        data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        data.SetRenderer(EnsureRenderer());
        go.AddComponent<RedonFixedFrame>();
        var key=new GameObject("Apricot Directional Light").AddComponent<Light>();
        key.type=LightType.Directional;key.color=Hex("FFE0B1");key.intensity=1;
        key.transform.rotation=Quaternion.Euler(35,-35,0);
        key.shadows=LightShadows.None;
    }
    static int EnsureRenderer()
    {
        var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if(pipeline==null) throw new InvalidOperationException("This scene requires URP.");
        string path=Root+"/Rendering/RedonRenderer.asset";
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
        if(renderer==null)
        {
            var source=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/URP-HighFidelity-Renderer.asset");
            renderer=Object.Instantiate(source);renderer.name="RedonRenderer";
            renderer.rendererFeatures.Clear();
            AssetDatabase.CreateAsset(renderer,path);
        }
        var finish=renderer.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault();
        if(finish==null)
        {
            finish=ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            finish.name="Painterly Resolve";
            renderer.rendererFeatures.Add(finish);AssetDatabase.AddObjectToAsset(finish,renderer);
        }
        var finishMaterial=Mat("Painterly_Resolve","Boids/Redon/Painterly Resolve");
        finishMaterial.SetFloat("_Radius",2.4f);finishMaterial.SetFloat("_Strength",.42f);
        finish.passMaterial=finishMaterial;finish.fetchColorBuffer=true;
        finish.injectionPoint=FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
        finish.requirements=ScriptableRenderPassInput.None;finish.Create();finish.SetActive(true);
        renderer.SetDirty();EditorUtility.SetDirty(finish);EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(finishMaterial);
        var serialized=new SerializedObject(pipeline);
        var list=serialized.FindProperty("m_RendererDataList");
        for(int i=0;i<list.arraySize;i++)
            if(list.GetArrayElementAtIndex(i).objectReferenceValue==renderer)return i;
        int index=list.arraySize;list.arraySize++;
        list.GetArrayElementAtIndex(index).objectReferenceValue=renderer;
        serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);
        return index;
    }
    static void CreateAtmosphere()
    {
        var mat=Mat("Painted_Water","Boids/Redon/Painted Atmosphere");
        mat.SetTexture("_PigmentTex",pigment);
        mat.SetColor("_DeepColor",Hex("153D57"));
        mat.SetColor("_GoldColor",Hex("E8A374"));
        mat.SetColor("_PearlColor",Hex("FFE0A0"));
        mat.SetFloat("_TextureStrength",.85f);EditorUtility.SetDirty(mat);
        ClearMesh();
        Quad(new Vector3(0,6,62),Vector3.right*30,Vector3.up*25,Color.white,0,false);
        var mesh=SaveMesh("Atmosphere",FinishMesh());
        Instance("Painted Water Depth",mesh,mat,Vector3.zero,Vector3.one,Quaternion.identity,null);
    }
    static void CreateVolume()
    {
        var v=new GameObject("Painterly Color Grading").AddComponent<Volume>();
        v.isGlobal=true;
        string path=Root+"/Rendering/RedonVolume.asset";
        var p=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(p==null){p=ScriptableObject.CreateInstance<VolumeProfile>();p.name="RedonVolume";AssetDatabase.CreateAsset(p,path);}
        var bloom=Effect<Bloom>(p);bloom.threshold.Override(1.12f);bloom.intensity.Override(.09f);bloom.scatter.Override(.55f);
        var grade=Effect<ColorAdjustments>(p);grade.postExposure.Override(.12f);grade.contrast.Override(10);grade.saturation.Override(8);
        var tone=Effect<Tonemapping>(p);tone.mode.Override(TonemappingMode.None);
        var vignette=Effect<Vignette>(p);vignette.intensity.Override(.12f);vignette.smoothness.Override(.78f);
        v.sharedProfile=p;EditorUtility.SetDirty(p);
    }
    static T Effect<T>(VolumeProfile p) where T:VolumeComponent
    {
        T c;
        if(!p.TryGet<T>(out c)){c=p.Add<T>(true);AssetDatabase.AddObjectToAsset(c,p);}
        EditorUtility.SetDirty(c);return c;
    }

    static void BuildGeometry()
    {
        rocks=new Mesh[5];
        for(int i=0;i<rocks.Length;i++)
        {
            ClearMesh();const int rings=18,sides=26;
            for(int r=0;r<=rings;r++)for(int s=0;s<=sides;s++)
            {
                float v=(float)r/rings,u=(float)s/sides;
                float theta=v*Mathf.PI,phi=u*2*Mathf.PI;
                Vector3 p=new Vector3(Mathf.Sin(theta)*Mathf.Cos(phi),Mathf.Cos(theta),Mathf.Sin(theta)*Mathf.Sin(phi));
                float irregular=1+Mathf.Sin(p.y*7+p.x*4+i)*.12f+Mathf.Sin(p.z*12-p.y*9+i*2)*.08f;
                p*=irregular;p.x+=p.y*p.y*.10f*Mathf.Sin(i*7);p.z+=Mathf.Sin(p.y*4+i)*.09f;
                vertices.Add(p);uvs.Add(new Vector2(u,v));colors.Add(Color.white);
            }
            Grid(sides,rings);
            rocks[i]=SaveMesh("Cliff_Form_"+i,FinishMesh());
        }
    }

    static void Cliff(Vector3 p,Vector3 scale,int palette)
    {
        Instance("Painted Cliff",rocks[Random.Range(0,rocks.Length)],stone[palette],p,scale,
            Quaternion.Euler(Random.Range(-12,12),Random.Range(0,180),Random.Range(-12,12)),rockRoot);
    }
    static void Sprig(Vector3 root,Vector3 direction,float length,float radius)
    {
        direction.Normalize();
        Vector3 tip=root+direction*length;
        Tube(root,root+direction*length*.48f+Vector3.right*length*.12f,tip,radius);
        for(int b=0;b<9;b++)
        {
            float t=.18f+b*.085f;
            Vector3 start=Vector3.Lerp(root,tip,t)+Vector3.right*Mathf.Sin(t*Mathf.PI)*length*.05f;
            float side=b%2==0?-1:1;
            float branchLength=length*(1-t)*Random.Range(.32f,.65f);
            Vector3 branchTip=start+new Vector3(side*.7f,.8f,Random.Range(-.14f,.14f))*branchLength;
            Tube(start,start+new Vector3(side*.4f,.15f,0)*branchLength,branchTip,radius*.5f);
            for(int c=0;c<4;c++)
            {
                Vector3 subRoot=Vector3.Lerp(start,branchTip,.25f+c*.18f);
                Vector3 subTip=subRoot+new Vector3(side*.32f,.38f,0)*branchLength*(1-c*.17f);
                Tube(subRoot,Vector3.Lerp(subRoot,subTip,.5f)+Vector3.right*side*.04f,subTip,radius*.22f);
            }
        }
    }
    static void Tube(Vector3 a,Vector3 control,Vector3 b,float radius)
    {
        const int steps=10,sides=5;
        int offset=vertices.Count;
        for(int i=0;i<=steps;i++)
        {
            float t=(float)i/steps,u=1-t;
            Vector3 p=u*u*a+2*u*t*control+t*t*b;
            Vector3 tangent=(2*u*(control-a)+2*t*(b-control)).normalized;
            Vector3 right=Vector3.Cross(tangent,Vector3.forward).normalized;
            if(right.sqrMagnitude<.1f)right=Vector3.right;
            Vector3 up=Vector3.Cross(tangent,right).normalized;
            for(int j=0;j<=sides;j++)
            {
                float phi=(float)j/sides*Mathf.PI*2;
                vertices.Add(p+(right*Mathf.Cos(phi)+up*Mathf.Sin(phi))*radius*(1-t*.92f));
                uvs.Add(new Vector2((float)j/sides,t));colors.Add(Color.white);
            }
        }
        for(int i=0;i<steps;i++)for(int j=0;j<sides;j++)
        {
            int p=offset+i*(sides+1)+j,q=p+sides+1;
            triangles.Add(p);triangles.Add(p+1);triangles.Add(q);triangles.Add(p+1);triangles.Add(q+1);triangles.Add(q);
        }
    }

    const int FishCount=96;

    static GameObject Instance(string name,Mesh mesh,Material mat,Vector3 pos,Vector3 scale,Quaternion rot,Transform parent)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);
        go.transform.localPosition=pos;go.transform.localRotation=rot;go.transform.localScale=scale;
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;
        r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
        return go;
    }
    static void ClearMesh(){vertices.Clear();uvs.Clear();colors.Clear();triangles.Clear();normals.Clear();}
    static void Grid(int width,int height)
    {
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            int a=y*(width+1)+x,b=a+1,c=a+width+1,d=c+1;
            triangles.Add(a);triangles.Add(c);triangles.Add(b);triangles.Add(b);triangles.Add(c);triangles.Add(d);
        }
    }
    static void Quad(Vector3 c,Vector3 right,Vector3 up,Color color,int brush,bool atlasUV=true)
    {
        int k=vertices.Count;
        vertices.Add(c-right-up);vertices.Add(c-right+up);vertices.Add(c+right+up);vertices.Add(c+right-up);
        float tile=atlasUV?.25f:1;
        Vector2 offset=atlasUV?new Vector2(brush%4,brush/4)*tile:Vector2.zero;
        uvs.Add(offset);uvs.Add(offset+new Vector2(0,tile));uvs.Add(offset+new Vector2(tile,tile));uvs.Add(offset+new Vector2(tile,0));
        for(int j=0;j<4;j++)colors.Add(color);
        triangles.Add(k);triangles.Add(k+1);triangles.Add(k+2);triangles.Add(k);triangles.Add(k+2);triangles.Add(k+3);
    }
    static Mesh FinishMesh()
    {
        var mesh=new Mesh();
        if(vertices.Count>65535)mesh.indexFormat=IndexFormat.UInt32;
        mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);
        mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static Mesh SaveMesh(string name,Mesh mesh)
    {
        string path=Root+"/Geometry/"+name+".asset";mesh.name=name;
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing!=null)
        {
            // Update through Mesh's native setters so an existing renderer cannot retain stale GPU buffers.
            existing.Clear();existing.indexFormat=mesh.indexFormat;
            existing.vertices=mesh.vertices;existing.uv=mesh.uv;existing.colors=mesh.colors;
            existing.normals=mesh.normals;existing.triangles=mesh.triangles;existing.bounds=mesh.bounds;
            existing.UploadMeshData(false);
            Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;
        }
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static Color Hex(string value)
    {
        Color c;ColorUtility.TryParseHtmlString("#"+value,out c);return c;
    }

    [MenuItem("Boids/Redon/Capture Style Study")]
    public static void Capture()
    {
        var camera=GameObject.Find("Redon Fixed Camera")?.GetComponent<Camera>();
        if(camera==null)throw new InvalidOperationException("Open RedonDream first.");
        CaptureCamera(camera,"Captures/Redon_Style.png");
    }
    public static void CaptureCamera(Camera camera,string path,int width=1536,int height=1024)
    {
        var previous=camera.targetTexture;var active=RenderTexture.active;var rect=camera.rect;float aspect=camera.aspect;
        var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        try
        {
            camera.rect=new Rect(0,0,1,1);camera.aspect=(float)width/height;camera.targetTexture=rt;camera.Render();
            RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,image.EncodeToPNG());
        }
        finally {camera.targetTexture=previous;camera.rect=rect;camera.aspect=aspect;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);}
    }

    [MenuItem("Boids/Redon/Validate Style Scene")]
    public static void Validate()
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=ScenePath)throw new InvalidOperationException("Open RedonDream first.");
        var roots=scene.GetRootGameObjects();
        var all=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        int missing=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        var cameras=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).ToArray();
        bool fixedCamera=cameras.Length==1 && cameras[0].GetComponent<RedonFixedFrame>()!=null
            && cameras[0].transform.position==new Vector3(0,6,-30)
            && Quaternion.Angle(cameras[0].transform.rotation,Quaternion.identity)<.01f;
        var renderers=roots.SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)).ToArray();
        var materials=renderers.SelectMany(r=>r.sharedMaterials)
            .Append(AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Painterly_Resolve.mat")).Distinct().ToArray();
        var shaderErrors=materials.Where(m=>m!=null).Select(m=>m.shader).Distinct()
            .SelectMany(s=>ShaderUtil.GetShaderMessages(s))
            .Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
            .Select(m=>m.message).ToArray();
        var volume=Object.FindObjectOfType<Volume>();
        bool profileOk=volume!=null && volume.sharedProfile!=null && volume.sharedProfile.components.Count>=4
            && volume.sharedProfile.components.All(c=>c!=null && AssetDatabase.Contains(c));
        bool textureOk=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/PigmentScumble.png")!=null
            && AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/DryBrushAtlas.png")!=null
            && AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/PetalUnderpainting.png")!=null
            && AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/PetalVeining.png")!=null;
        int fishCount=roots.SelectMany(r=>r.GetComponentsInChildren<MoonveilMotion>(true)).Count();
        bool schoolReady=Object.FindObjectOfType<DreamSchoolController>()!=null;
        bool passed=missing==0 && fixedCamera && profileOk && textureOk && fishCount==FishCount
            && schoolReady && shaderErrors.Length==0 && materials.All(m=>m!=null && m.shader.isSupported);
        var report=new {passed,scene=scene.path,missingScripts=missing,fixedCamera,fishCount,schoolReady,
            validPersistentVolume=profileOk,generatedTexturesImported=textureOk,shaderErrors,
            rendererCount=renderers.Length,materialCount=materials.Length,
            device=SystemInfo.graphicsDeviceName,unity=Application.unityVersion,
            scope="Fixed-camera painterly garden with live Boids, skeletal animation and click-to-feed interaction."};
        Directory.CreateDirectory("Captures");
        File.WriteAllText("Captures/Redon_Validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        if(!passed)throw new InvalidOperationException("Redon scene validation failed; see Captures/Redon_Validation.json.");
        Debug.Log("Redon style scene validation passed.");
    }
}
