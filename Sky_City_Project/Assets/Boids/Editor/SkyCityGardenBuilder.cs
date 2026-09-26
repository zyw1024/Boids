using System.IO;
using System.Collections.Generic;
using Boids.Art;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static class SkyCityGardenBuilder
{
    public const string Root="Assets/Boids/Art/SkyCityWorld/Botany";
    static T Save<T>(T item,string filename) where T:Object
    {
        string path=Root+"/"+filename;var existing=AssetDatabase.LoadAssetAtPath<T>(path);
        if(existing==null){AssetDatabase.CreateAsset(item,path);return item;}
        var mesh=item as Mesh;var target=existing as Mesh;
        if(mesh!=null&&target!=null)
        {
            // CopySerialized can leave an existing Mesh's GPU streams stale in the open editor.
            target.Clear();target.indexFormat=mesh.indexFormat;target.vertices=mesh.vertices;target.normals=mesh.normals;target.colors=mesh.colors;
            for(int channel=0;channel<4;channel++){var data=new List<Vector4>();mesh.GetUVs(channel,data);target.SetUVs(channel,data);}
            target.triangles=mesh.triangles;target.bounds=mesh.bounds;target.UploadMeshData(false);
        }
        else EditorUtility.CopySerialized(item,existing);
        Object.DestroyImmediate(item);EditorUtility.SetDirty(existing);return existing;
    }
    static MeshRenderer Surface(Transform parent,string name,Mesh mesh,Material material)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.TwoSided;return renderer;
    }
    [MenuItem("Boids/Sky City World/Build living gardens")]
    public static void Build()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("Leave Play to author botany assets.");
        var player=Object.FindObjectOfType<SkyCityFirstPerson>();
        if(player==null||player.world.authoredArrival==null)throw new System.InvalidOperationException("Open SkyCityWorld first.");
        Directory.CreateDirectory(Root);AssetDatabase.Refresh();
        var old=player.world.authoredArrival.Find("The awakening garden");if(old!=null)Object.DestroyImmediate(old.gameObject);
        var root=new GameObject("The awakening garden");root.transform.SetParent(player.world.authoredArrival,false);
        var director=root.AddComponent<SkyCityGardenDirector>();director.player=player;
        var treeMat=new Material(Shader.Find("Boids/SkyCity/Living Botany")){name="Wind tree - sage and silver"};treeMat.SetFloat("_Tree",1);treeMat=Save(treeMat,"Wind tree.mat");
        var flowerMat=Save(new Material(Shader.Find("Boids/SkyCity/Living Botany")){name="Living lavender and ivory"},"Living flowers.mat");
        var treeRoot=new GameObject("The wind tree - recursive crown");treeRoot.transform.SetParent(root.transform,false);treeRoot.transform.localPosition=new Vector3(-20.2f,6.34f,4.65f);
        director.treeGenerations=new GameObject[3];var branchCounts=new List<int>();
        for(int depth=3;depth<=5;depth++)
        {
            var generation=new GameObject("L-system generation "+depth);generation.transform.SetParent(treeRoot.transform,false);director.treeGenerations[depth-3]=generation;
            var renderers=new MeshRenderer[2];
            for(int detail=0;detail<2;detail++)
            {
                int branches;var mesh=SkyCityBotanyGeometry.Tree(depth,detail,out branches);if(detail==0)branchCounts.Add(branches);
                mesh=Save(mesh,"WindTree_G"+depth+"_L"+detail+".asset");renderers[detail]=Surface(generation.transform,mesh.name,mesh,treeMat);
            }
            var lod=generation.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.13f,new Renderer[]{renderers[0]}),new LOD(.018f,new Renderer[]{renderers[1]})});lod.RecalculateBounds();
            generation.SetActive(depth==5);
        }
        // A stone collar and visible soil give the tree a credible attachment to the existing terrace.
        var planter=GameObject.CreatePrimitive(PrimitiveType.Cylinder);planter.name="Wind tree - limestone planting collar";planter.transform.SetParent(treeRoot.transform,false);
        planter.transform.localPosition=new Vector3(0,-.07f,0);planter.transform.localScale=new Vector3(2.35f,.13f,2.35f);
        TintPrimitive(planter,new Color(.80f,.74f,.62f),"Planting collar.asset");
        planter.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Boids/Art/SkyCityHero/Warm carved limestone.mat");
        var earth=GameObject.CreatePrimitive(PrimitiveType.Cylinder);earth.name="Wind tree - recessed soil";earth.transform.SetParent(treeRoot.transform,false);
        earth.transform.localPosition=new Vector3(0,.067f,0);earth.transform.localScale=new Vector3(2.15f,.013f,2.15f);
        TintPrimitive(earth,new Color(.32f,.36f,.20f),"Planting soil.asset");
        earth.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Boids/Art/SkyCityHero/Garden beds.mat");Object.DestroyImmediate(earth.GetComponent<Collider>());
        var trunk=new GameObject("Wind tree - trunk collision",typeof(CapsuleCollider));trunk.transform.SetParent(treeRoot.transform,false);
        var capsule=trunk.GetComponent<CapsuleCollider>();capsule.center=new Vector3(.3f,1.5f,-.2f);capsule.height=3.1f;capsule.radius=.38f;
        var gardens=new List<SkyCityLivingGarden>();
        AddGarden(gardens,root.transform,flowerMat,"Lower terrace",new Vector3(-16.125f,6.19f,3),new Vector2(29.6f,.64f),96,3,1.85f,1.48f,4201);
        AddGarden(gardens,root.transform,flowerMat,"Upper terrace",new Vector3(-19.55f,10.56f,9.5f),new Vector2(22.8f,.56f),72,3,1.9f,1.57f,4301);
        AddGarden(gardens,root.transform,flowerMat,"Pool garden west",new Vector3(-15.225f,3.67f,-.2f),new Vector2(3.1f,.58f),12,3,1.55f,1.08f,4701);
        AddGarden(gardens,root.transform,flowerMat,"Pool garden east",new Vector3(-6.7f,3.67f,-.2f),new Vector2(1.55f,.58f),6,3,1.55f,1.08f,4702);
        director.gardens=gardens.ToArray();
        InstallStreaming();
        EditorUtility.SetDirty(director);EditorSceneManager.MarkSceneDirty(root.scene);EditorSceneManager.SaveScene(root.scene);AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Captures/SkyCityWorld");File.WriteAllText("Captures/SkyCityWorld/BotanyBuild.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{branchCounts,gardens=gardens.Count,seed=71309,tree="bounded stochastic parametric recursive production",source="existing authored terrace planting beds"},Newtonsoft.Json.Formatting.Indented));
    }
    [MenuItem("Boids/Sky City World/Install streamed gardens")]
    public static void InstallStreaming()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("Leave Play before installing garden assets.");
        var director=Object.FindObjectOfType<SkyCityGardenDirector>();var world=director.player.world;
        var streaming=world.GetComponent<SkyCityDistrictGardens>();if(streaming==null)streaming=world.gameObject.AddComponent<SkyCityDistrictGardens>();
        streaming.world=world;streaming.director=director;
        var sample=new GameObject("Temporary shared planter authoring").AddComponent<SkyCityLivingGarden>();
        sample.columns=12;sample.rows=4;sample.size=new Vector2(2.4f,1.5f);sample.continuous=true;sample.seed=9327;
        streaming.flowersMesh=Save(SkyCityBotanyGeometry.Flowers(sample,1),"District flowers.asset");
        streaming.cellsMesh=Save(SkyCityBotanyGeometry.Cells(sample),"District cells.asset");Object.DestroyImmediate(sample.gameObject);
        var box=GameObject.CreatePrimitive(PrimitiveType.Cube);streaming.boxMesh=box.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(box);
        streaming.treeMeshes=new Mesh[3];for(int i=0;i<3;i++)streaming.treeMeshes[i]=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/WindTree_G"+(i+3)+"_L1.asset");
        streaming.treeMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Wind tree.mat");streaming.flowersMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Living flowers.mat");
        var stone=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Courtyard planter limestone"};stone.SetColor("_BaseColor",new Color(.72f,.64f,.49f));stone.SetFloat("_Smoothness",.2f);
        var soil=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Courtyard planting soil"};soil.SetColor("_BaseColor",new Color(.19f,.22f,.12f));soil.SetFloat("_Smoothness",.05f);
        streaming.stoneMaterial=Save(stone,"District limestone.mat");streaming.soilMaterial=Save(soil,"District soil.mat");
        EditorUtility.SetDirty(streaming);EditorSceneManager.MarkSceneDirty(world.gameObject.scene);EditorSceneManager.SaveScene(world.gameObject.scene);AssetDatabase.SaveAssets();
    }
    static void TintPrimitive(GameObject go,Color colour,string filename)
    {
        var filter=go.GetComponent<MeshFilter>();var mesh=Object.Instantiate(filter.sharedMesh);mesh.name=go.name;
        var colors=new Color[mesh.vertexCount];for(int i=0;i<colors.Length;i++)colors[i]=colour.linear;mesh.colors=colors;filter.sharedMesh=Save(mesh,filename);
    }
    static void AddGarden(List<SkyCityLivingGarden> list,Transform parent,Material material,string name,Vector3 position,Vector2 size,int columns,int rows,float spacing,float width,int seed)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;
        var garden=go.AddComponent<SkyCityLivingGarden>();garden.columns=columns;garden.rows=rows;garden.size=size;garden.seed=seed;garden.bedSpacing=spacing;garden.bedWidth=width;
        var mesh=Save(SkyCityBotanyGeometry.Flowers(garden,0),name+".asset");garden.flowers=Surface(go.transform,"Flowers - one combined surface",mesh,material);
        var cells=Save(SkyCityBotanyGeometry.Cells(garden),name+" cells.asset");garden.rules=Surface(go.transform,"Teaching cells - hidden in artwork",cells,material);
        garden.rules.shadowCastingMode=ShadowCastingMode.Off;garden.rules.enabled=false;
        var lod=go.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.009f,new Renderer[]{garden.flowers})});lod.RecalculateBounds();list.Add(garden);
    }
}
