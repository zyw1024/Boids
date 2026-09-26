using System.IO;
using System.Linq;
using System.Threading;
using SkyCity.Runtime.WorldGeneration;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class SkyCityModuleBuilder
{
    public const string Root=SkyCityAssetPaths.Districts;
    static readonly string[] Families={"ArcadedPromenade","TurningLoggia","MarketColonnade","FountainPiazza","HangingBelvedere","DomedSanctuary","BellCampanile","GardenCloister","TerracedOrchard","PalaceLibrary","SkyAqueduct","CurvedSkybridge","CeremonialGate","CelestialObservatory","GlassConservatory","CrownPalace"};
    public static readonly string[] Variants={"Dawn","Iris","Cypress","Pearl","Saffron","Linden","Azure","Solstice"};
    [MenuItem("Sky City/Assets/Bake 128 reusable prefabs")]
    public static void BakePrefabs()
    {
        Directory.CreateDirectory(Root+"/Modules");AssetDatabase.Refresh();
        var data=SkyCityModuleData.Read(File.ReadAllBytes("Assets/SkyCity/Resources/SkyCityInfinite/Modules.bytes"),CancellationToken.None);
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
                    if(saved!=null){mesh.name=saved.name;saved.Clear();EditorUtility.CopySerialized(mesh,saved);saved.UploadMeshData(false);Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(mesh);}
                    else if(lod==0)AssetDatabase.CreateAsset(mesh,asset);else AssetDatabase.AddObjectToAsset(mesh,asset);
                    var child=new GameObject("LOD"+lod);child.transform.SetParent(root.transform,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;
                    var renderer=child.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;lods[lod]=new LOD(new[]{.18f,.065f,.005f}[lod],new Renderer[]{renderer});
                }
                if(data.modules[id,0,1].indices.Length>0)
                {
                    var mesh=SkyCityModuleData.Upload(data.modules[id,0,1],name+" water");
                    var saved=AssetDatabase.LoadAllAssetsAtPath(asset).OfType<Mesh>().FirstOrDefault(m=>m.name==mesh.name);
                    if(saved!=null){saved.Clear();EditorUtility.CopySerialized(mesh,saved);saved.UploadMeshData(false);Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(mesh);}else AssetDatabase.AddObjectToAsset(mesh,asset);
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
