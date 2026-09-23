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
public static class RedonSceneBuilder
{
    public const string Root = "Assets/Boids/Art/Redon";
    public const string ScenePath = "Assets/Boids/Scenes/RedonDream.unity";
    static readonly List<Vector3> vertices = new List<Vector3>();
    static readonly List<Vector2> uvs = new List<Vector2>();
    static readonly List<Color> colors = new List<Color>();
    static readonly List<int> triangles = new List<int>();
    static readonly List<Vector3> normals = new List<Vector3>();
    static Texture2D pigment, atlas, underpainting;
    static Mesh[] petals, petalBrushes, rocks, bouquets;
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
        if (!compositionDraft && (pigment == null || atlas == null || underpainting == null))
            throw new InvalidOperationException("All three generated pigment textures must be imported first.");
        if (pigment == null) pigment = Texture2D.grayTexture;
        if (atlas == null) atlas = Texture2D.blackTexture;
        foreach (string dir in new[]{"Materials","Geometry","Rendering"})
            Directory.CreateDirectory(Root+"/"+dir);
        AssetDatabase.Refresh();
        ImportMask(pigment, TextureWrapMode.Mirror);
        ImportMask(atlas, TextureWrapMode.Clamp);
        if(underpainting!=null)
        {
            var ti=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(underpainting));
            ti.sRGBTexture=true;ti.wrapMode=TextureWrapMode.Mirror;ti.mipmapEnabled=true;
            ti.maxTextureSize=2048;ti.textureCompression=TextureImporterCompression.Uncompressed;
            ti.filterMode=FilterMode.Trilinear;ti.SaveAndReimport();
        }
        var randomState = Random.state;
        Random.InitState(73129);
        try
        {
            BuildMaterials();
            BuildGeometry();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var settings = new GameObject("Art Direction - Redon").AddComponent<RedonStyleSettings>();
            settings.warmExtent = new Vector2(4.5f,4.5f);
            settings.waterColor = Hex("396B83");
            settings.fogStart = 31f;
            settings.Apply();
            plantsRoot = new GameObject("Petal Gardens").transform;
            rockRoot = new GameObject("Painted Cliffs").transform;
            fishRoot = new GameObject("Fish - Static Composition").transform;
            brushRoot = new GameObject("Anchored Pigment and Golden Spores").transform;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Hex("697484");
            CreateCamera();
            CreateAtmosphere();
            CreateCliffs();
            CreateGardens();
            CreateBranchwork();
            CreateCoralGardens();
            CreateSchool();
            CreateSpores();
            CreateVolume();
            settings.Apply();
            EditorSceneManager.SaveScene(scene,ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = settings.gameObject;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.LookAt(new Vector3(0,6,5),Quaternion.identity,18);
            }
            Debug.Log("Redon fixed-camera art scene built. Fish are staged; no Boids or feeding is active.");
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
        mat.SetColor("_DeepColor",Hex("264759"));
        mat.SetColor("_GoldColor",Hex("DA9D70"));
        mat.SetColor("_PearlColor",Hex("F5DBA5"));
        mat.SetFloat("_TextureStrength",.7f);EditorUtility.SetDirty(mat);
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
        var grade=Effect<ColorAdjustments>(p);grade.postExposure.Override(.1f);grade.contrast.Override(5);grade.saturation.Override(-3);
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
        petals=new Mesh[8];petalBrushes=new Mesh[8];
        for(int i=0;i<petals.Length;i++)
        {
            ClearMesh();const int across=40,along=36;
            for(int y=0;y<=along;y++)for(int x=0;x<=across;x++)
            {
                float u=(float)x/across,t=(float)y/along;
                vertices.Add(Petal(u,t,i));uvs.Add(new Vector2(u,t));
                colors.Add(Color.white);
            }
            Grid(across,along);
            petals[i]=SaveMesh("Petal_"+i,FinishMesh());
            ClearMesh();
            for(int b=0;b<110;b++)
            {
                float u=Random.Range(.06f,.94f),t=Random.Range(.10f,.96f);
                float du=Random.Range(.02f,.12f),dt=Random.Range(.01f,.045f);
                Vector3 center=Petal(u,t,i);
                Vector3 right=(Petal(Mathf.Min(.99f,u+du),t,i)-Petal(Mathf.Max(.01f,u-du),t,i))*.5f;
                Vector3 up=(Petal(u,Mathf.Min(.99f,t+dt),i)-Petal(u,Mathf.Max(.01f,t-dt),i))*.5f;
                Quad(center+Vector3.back*.007f,right,up,new Color(Random.value,Random.value,1,Random.Range(.22f,.7f)),Random.Range(0,16));
            }
            // Short anchored marks soften selected contours without a screen-space filter.
            for(int b=0;b<48;b++)
            {
                float u=(b+.5f)/48;
                Vector3 center=Petal(u,.997f,i);
                Vector3 tangent=(Petal(Mathf.Min(.999f,u+.018f),.997f,i)-Petal(Mathf.Max(.001f,u-.018f),.997f,i))*.65f;
                Quad(center+Vector3.back*.009f,tangent,Vector3.up*Random.Range(.006f,.018f),
                    new Color(Random.value,.6f,1,Random.Range(.4f,.9f)),Random.Range(0,16));
            }
            petalBrushes[i]=SaveMesh("Petal_Brushes_"+i,FinishMesh());
        }
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
        bouquets=new Mesh[3];
        for(int variant=0;variant<3;variant++)
        {
            var pieces=new List<CombineInstance>();
            for(int p=0;p<14;p++)
            {
                float a=p*137.5f*Mathf.Deg2Rad;
                var pos=new Vector3(Mathf.Cos(a)*.33f,Random.Range(.05f,.45f),Mathf.Sin(a)*.23f);
                var rot=Quaternion.Euler(Random.Range(-20,30),Random.Range(-40,40),Mathf.Cos(a)*70);
                var scale=new Vector3(Random.Range(.4f,.85f),Random.Range(.5f,1.1f),.35f);
                pieces.Add(new CombineInstance{mesh=petals[(p%4)*2],transform=Matrix4x4.TRS(pos,rot,scale)});
            }
            var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pieces.ToArray(),true,true);
            bouquets[variant]=SaveMesh("Coral_Bouquet_"+variant,mesh);
        }
    }
    static Vector3 Petal(float u,float t,int variant)
    {
        float s=u*2-1,phase=variant*.83f;
        if(variant%2==0)
        {
            float angle=s*1.35f;
            float edge=1+.018f*Mathf.Sin(s*19+phase)+.012f*Mathf.Sin(s*37+phase);
            float fx=Mathf.Sin(angle)*t*.60f*edge;
            float fy=t*(.84f+.16f*Mathf.Cos(angle))*edge;
            float fz=t*(s*s*.24f+.045f*Mathf.Cos(s*11+phase))+Mathf.Sin(t*Mathf.PI)*.14f;
            return new Vector3(fx,fy,fz);
        }
        float outline=Mathf.Pow(Mathf.Max(.00001f,Mathf.Sin(Mathf.PI*t)),.62f);
        float scallop=1+.045f*Mathf.Sin(t*36+phase)+.035f*Mathf.Sin(t*63+phase*2);
        float x=s*outline*scallop*.5f + Mathf.Sin(t*Mathf.PI)*.13f*Mathf.Sin(phase);
        float y=t + .05f*Mathf.Sin(s*3.2f+phase)*Mathf.Sin(t*Mathf.PI);
        float z=(s*s*.26f + Mathf.Sin(t*Mathf.PI)*.16f + t*t*.13f*Mathf.Sin(phase*1.3f));
        z+=Mathf.Sin(s*10+t*8+phase)*.016f*outline;
        return new Vector3(x,y,z);
    }

    static void CreateCliffs()
    {
        // Side masses frame an open middle-distance passage.
        Cliff(new Vector3(-10,-2,0),new Vector3(4.5f,3.4f,2.6f),0);
        Cliff(new Vector3(-7.5f,-2,4),new Vector3(3.4f,2.8f,2.2f),0);
        Cliff(new Vector3(-4.8f,-3.2f,8),new Vector3(3.0f,2.1f,2),1);
        Cliff(new Vector3(10,-2,0),new Vector3(3.4f,3.8f,2.4f),0);
        Cliff(new Vector3(8.7f,.7f,8),new Vector3(1.7f,3.3f,1.6f),1);
        Cliff(new Vector3(9.8f,6.6f,11),new Vector3(1.3f,4.7f,1.3f),1);
        Cliff(new Vector3(-9.1f,8.3f,10),new Vector3(1.5f,5.0f,1.3f),1);
        for(int i=0;i<28;i++)
        {
            float side=i%2==0?-1:1;
            float z=Random.Range(5,32),x=side*Random.Range(5.4f,11.8f);
            float h=Random.Range(.6f,3.0f);
            Cliff(new Vector3(x,Random.Range(-3,4),z),new Vector3(Random.Range(.4f,1.4f),h,Random.Range(.4f,1.3f)),z>17?2:1);
        }
        // Small distant suspended islands supply scale without closing the opening.
        for(int i=0;i<6;i++)
        {
            float x=Random.Range(-6.5f,7),y=Random.Range(-3,3);
            if(x>-.2f&&x<5&&y>6)continue;
            Cliff(new Vector3(x,y,Random.Range(27,43)),new Vector3(Random.Range(.3f,.8f),Random.Range(.7f,1.8f),.55f),2);
        }
    }
    static void Cliff(Vector3 p,Vector3 scale,int palette)
    {
        Instance("Painted Cliff",rocks[Random.Range(0,rocks.Length)],stone[palette],p,scale,
            Quaternion.Euler(Random.Range(-12,12),Random.Range(0,180),Random.Range(-12,12)),rockRoot);
    }
    static void CreateGardens()
    {
        // Hero broad petals: intentional silhouette and negative space.
        Leaf(new Vector3(-9.4f,3.9f,-.5f),new Vector3(7.4f,5.8f,2.2f),new Vector3(-7,4,-18),0,0);
        Leaf(new Vector3(-8.4f,1.4f,-1),new Vector3(4.8f,5.7f,2),new Vector3(7,-12,-40),3,2);
        Leaf(new Vector3(-8.5f,3.6f,2.8f),new Vector3(4.7f,8.3f,2.8f),new Vector3(9,21,20),1,2);
        Leaf(new Vector3(-8.5f,-.5f,-1.3f),new Vector3(4.9f,6.1f,2.6f),new Vector3(-8,-13,-65),0,3);
        Leaf(new Vector3(-8.3f,.1f,-1.5f),new Vector3(4.6f,5.4f,2.6f),new Vector3(6,6,56),2,4);
        Leaf(new Vector3(10,1,1),new Vector3(4.0f,8.2f,2.8f),new Vector3(5,-30,13),2,6);
        Leaf(new Vector3(10,-1,-.5f),new Vector3(3.8f,7.2f,2.1f),new Vector3(-7,8,42),4,3);
        Leaf(new Vector3(9.9f,-.7f,2.5f),new Vector3(3.2f,9.2f,2.4f),new Vector3(10,5,-25),0,7);
        Leaf(new Vector3(8.9f,-1,5),new Vector3(3.4f,7.8f,2.7f),new Vector3(10,-20,16),3,2);
        // Dark foreground curled fans establish the frame.
        for(int i=0;i<10;i++)
        {
            float side=i%2==0?-1:1;
            Leaf(new Vector3(side*Random.Range(6.2f,12),-2.0f,Random.Range(-6,-3)),
                new Vector3(Random.Range(2.3f,4.2f),Random.Range(3.0f,5.6f),2.6f),
                new Vector3(Random.Range(-20,20),Random.Range(-30,30),side*Random.Range(15,72)),4,i%8);
        }
        // Medium-distance petals and crowns.
        for(int i=0;i<42;i++)
        {
            float side=i%2==0?-1:1;
            float x=side*Random.Range(5.4f,12),y=Random.Range(-2,10),z=Random.Range(9,32);
            float length=Random.Range(1.1f,4.4f);
            Leaf(new Vector3(x,y,z),new Vector3(length*Random.Range(.36f,.65f),length,length*.3f),
                new Vector3(Random.Range(-22,22),Random.Range(-40,40),Random.Range(-65,65)),
                z>22?5:Random.Range(0,4),i%8);
        }
        for(int i=0;i<15;i++)
        {
            float side=i%2==0?-1:1;
            Leaf(new Vector3(side*Random.Range(3.5f,7),Random.Range(-3,4),Random.Range(30,42)),
                new Vector3(Random.Range(.8f,1.7f),Random.Range(1.8f,3.2f),1),
                new Vector3(0,0,Random.Range(-45,45)),5,i%8);
        }
    }
    static void Leaf(Vector3 p,Vector3 scale,Vector3 euler,int palette,int shape)
    {
        var leaf=Instance("Petal "+palette+"."+shape,petals[shape],foliage[palette],p,scale,Quaternion.Euler(euler),plantsRoot);
        if(!compositionDraft)
            Instance("Surface Brushwork",petalBrushes[shape],foliageBrushes[palette],Vector3.zero,Vector3.one,Quaternion.identity,leaf.transform);
    }

    static void CreateBranchwork()
    {
        Material stem=Paint("Golden_Stems","8C806D","354556","D9B17C",.3f,.05f,0,1);
        ClearMesh();
        Sprig(new Vector3(-9.0f,-1,-2),new Vector3(.23f,1,.06f),7.3f,.022f);
        Sprig(new Vector3(-7.4f,-.9f,-.5f),new Vector3(-.1f,1,0),5.6f,.018f);
        Sprig(new Vector3(-6.3f,-1,1.1f),new Vector3(-.2f,1,.1f),3.2f,.013f);
        Sprig(new Vector3(9.2f,-1,1),new Vector3(-.05f,1,.1f),6.3f,.020f);
        Instance("Gilded Branches",SaveMesh("Gilded_Branches",FinishMesh()),stem,Vector3.zero,Vector3.one,Quaternion.identity,plantsRoot);
        // Smaller coral gardens break up the foreground masses.
        for(int group=0;group<3;group++)
        {
            ClearMesh();
            for(int i=0;i<14;i++)
            {
                float side=i%2==0?-1:1;
                Vector3 root=new Vector3(side*Random.Range(4.7f,10.5f),Random.Range(-1.8f,.4f),Random.Range(-1,9));
                Sprig(root,new Vector3(Random.Range(-.6f,.6f),1,0),Random.Range(.45f,1.65f),.01f);
            }
            Instance("Coral Filaments "+group,SaveMesh("Coral_Filaments_"+group,FinishMesh()),foliage[group],Vector3.zero,Vector3.one,Quaternion.identity,plantsRoot);
        }
    }
    static void CreateCoralGardens()
    {
        for(int i=0;i<64;i++)
        {
            float side=i%2==0?-1:1;
            Vector3 p=new Vector3(side*Random.Range(4.1f,10.8f),Random.Range(-.8f,1.9f),Random.Range(-3,13));
            float scale=Random.Range(.45f,1.65f);
            Instance("Coral Blossom",bouquets[i%3],foliage[i%4],p,new Vector3(scale,scale*.75f,scale),Quaternion.Euler(0,Random.Range(-45,45),Random.Range(-22,22)),plantsRoot);
        }
        for(int i=0;i<18;i++)
        {
            float side=i%2==0?-1:1;
            Vector3 p=new Vector3(side*Random.Range(7.8f,12.0f),Random.Range(3.6f,12),Random.Range(24,42));
            float scale=Random.Range(1.0f,2.4f);
            Cliff(p-Vector3.up*1.4f,new Vector3(scale*.28f,1.5f,scale*.3f),2);
            Instance("Distant Flower Crown",bouquets[i%3],foliage[5],p,new Vector3(scale,scale*.8f,scale),Quaternion.Euler(0,Random.Range(-30,30),Random.Range(-15,15)),plantsRoot);
        }
        Instance("Rose Dream Blossom",bouquets[1],foliage[3],new Vector3(-2.7f,7.1f,25),new Vector3(4.2f,3.2f,2),Quaternion.Euler(0,-8,-12),plantsRoot);
        Cliff(new Vector3(-2.7f,3.6f,26),new Vector3(.6f,3.8f,.6f),2);
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

    static void CreateSchool()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(MoonveilAssetSetup.PrefabPath);
        if(prefab==null)throw new InvalidOperationException("Moonveil prefab missing.");
        for(int i=0;i<FishCount;i++)
        {
            float t=(i+.2f)/FishCount;
            Vector3 p=SchoolPath(t);
            p+=new Vector3(Random.Range(-.5f,.5f),Random.Range(-.62f,.62f),Random.Range(-.8f,.8f));
            Vector3 direction=SchoolPath(Mathf.Min(1,t+.012f))-SchoolPath(Mathf.Max(0,t-.012f));
            direction+=new Vector3(Random.Range(-.2f,.2f),Random.Range(-.2f,.2f),Random.Range(-.3f,.3f));
            var fish=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            fish.name="Pigment Fish "+i.ToString("00");fish.transform.SetParent(fishRoot);
            direction.z *= .15f;
            fish.transform.position=p;fish.transform.rotation=Quaternion.LookRotation(direction.normalized,Vector3.up);
            float size=Mathf.Lerp(.86f,.27f,t)*Random.Range(.78f,1.15f);
            fish.transform.localScale=new Vector3(.86f,.86f,1.3f)*size;
            var animator=fish.GetComponentInChildren<Animator>();
            if(animator!=null)animator.enabled=false;
            var motion=fish.GetComponent<MoonveilMotion>();if(motion!=null)motion.enabled=false;
            int palette=t>.72f?3:Random.Range(0,fishPaint.Length);
            foreach(var r in fish.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                r.sharedMaterial=fishPaint[palette];r.shadowCastingMode=ShadowCastingMode.Off;
                r.receiveShadows=false;r.updateWhenOffscreen=false;
            }
        }
    }
    const int FishCount=70;
    static Vector3 SchoolPath(float t)
    {
        Vector3 a=new Vector3(-7,5.8f,-.5f),b=new Vector3(11,3.7f,4),c=new Vector3(7,8.7f,13),d=new Vector3(2.1f,11.6f,22);
        float u=1-t;return u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;
    }
    static void CreateSpores()
    {
        if(compositionDraft)return;
        ClearMesh();
        for(int i=0;i<740;i++)
        {
            float t=Random.value;
            Vector3 p=new Vector3(Mathf.Lerp(-8.5f,-4.0f,t)+Random.Range(-.7f,.7f),
                Mathf.Lerp(1.6f,-1.1f,t)+Random.Range(-.7f,.7f),Random.Range(-.5f,7));
            float size=Random.Range(.014f,.08f);
            Quad(p,Vector3.right*size,Vector3.up*size*.7f,new Color(Random.value,Random.value,1,Random.Range(.3f,.9f)),Random.Range(0,16));
        }
        for(int i=0;i<120;i++)
        {
            var p=new Vector3(Random.Range(-8,9),Random.Range(-.8f,11.6f),Random.Range(5,28));
            float size=Random.Range(.009f,.032f);
            Quad(p,Vector3.right*size,Vector3.up*size,new Color(Random.value,Random.value,1,Random.Range(.2f,.7f)),Random.Range(0,16));
        }
        Instance("Golden Pigment Flecks",SaveMesh("Golden_Flecks",FinishMesh()),brushGold,Vector3.zero,Vector3.one,Quaternion.identity,brushRoot);
        ClearMesh();
        for(int i=0;i<250;i++)
        {
            Vector3 p=new Vector3(Random.Range(-12,12),Random.Range(-3,15),Random.Range(30,46));
            float size=Random.Range(.14f,.8f);
            Quad(p,Vector3.right*size,Vector3.up*size*Random.Range(.25f,.7f),
                new Color(Random.value,Random.value,1,Random.Range(.15f,.6f)),Random.Range(0,16));
        }
        Instance("Distant Broken Pigment",SaveMesh("Distant_Brushes",FinishMesh()),brushDistant,Vector3.zero,Vector3.one,Quaternion.identity,brushRoot);
    }

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
        if(existing!=null){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;}
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
    public static void CaptureCamera(Camera camera,string path)
    {
        var previous=camera.targetTexture;var active=RenderTexture.active;var rect=camera.rect;float aspect=camera.aspect;
        var rt=RenderTexture.GetTemporary(1536,1024,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        var image=new Texture2D(1536,1024,TextureFormat.RGB24,false);
        try
        {
            camera.rect=new Rect(0,0,1,1);camera.aspect=1.5f;camera.targetTexture=rt;camera.Render();
            RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1536,1024),0,0);image.Apply();
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
        var materials=renderers.SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
        var shaderErrors=materials.Where(m=>m!=null).Select(m=>m.shader).Distinct()
            .SelectMany(s=>ShaderUtil.GetShaderMessages(s))
            .Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
            .Select(m=>m.message).ToArray();
        var volume=Object.FindObjectOfType<Volume>();
        bool profileOk=volume!=null && volume.sharedProfile!=null && volume.sharedProfile.components.Count>=4
            && volume.sharedProfile.components.All(c=>c!=null && AssetDatabase.Contains(c));
        bool textureOk=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/PigmentScumble.png")!=null
            && AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/DryBrushAtlas.png")!=null
            && AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/PetalUnderpainting.png")!=null;
        int fishCount=GameObject.Find("Fish - Static Composition").transform.childCount;
        bool passed=missing==0 && fixedCamera && profileOk && textureOk && fishCount==FishCount
            && shaderErrors.Length==0 && materials.All(m=>m!=null && m.shader.isSupported);
        var report=new {passed,scene=scene.path,missingScripts=missing,fixedCamera,fishCount,
            validPersistentVolume=profileOk,generatedTexturesImported=textureOk,shaderErrors,
            rendererCount=renderers.Length,materialCount=materials.Length,
            device=SystemInfo.graphicsDeviceName,unity=Application.unityVersion,
            scope="Fixed-camera art study. Static staged fish; no Boids or feeding."};
        Directory.CreateDirectory("Captures");
        File.WriteAllText("Captures/Redon_Validation.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
        if(!passed)throw new InvalidOperationException("Redon scene validation failed; see Captures/Redon_Validation.json.");
        Debug.Log("Redon style scene validation passed.");
    }
}
