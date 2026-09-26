using System;
using UnityEditor;
using UnityEngine;

/// <summary>Reimport authored meshes without rebuilding or replacing the scene.</summary>
public static class SkyCityIslandImporter
{
    [MenuItem("Sky City/Assets/Reimport island meshes")]
    public static void Import()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Leave Play first.");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=SkyCityAssetPaths.Scene)throw new InvalidOperationException("Open SkyCityWorld first.");
        foreach(string file in new[]{"HangingGardens","HangingGardens_LOD1","HangingGardens_LOD2"})
        {
            string path=SkyCityAssetPaths.Island+"/"+file+".fbx";AssetDatabase.ImportAsset(path);
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.isReadable=false;importer.generateSecondaryUV=false;
            importer.meshCompression=ModelImporterMeshCompression.Off;importer.SaveAndReimport();
        }
        SkyCityStyleReview.RefreshAuthoredMeshes(true);
    }
}
