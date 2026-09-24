using UnityEditor;
using UnityEngine;
using Boids.Art;

public static class SkyCityPresentationBuilder
{
    public const string MusicPath=SkyCitySceneBuilder.Root+"/Audio/GardenOfWinds.ogg";
    public static void Create()
    {
        var camera=Camera.main;
        if(camera.GetComponent<SkyCityCameraRig>()==null)camera.gameObject.AddComponent<SkyCityCameraRig>();
        if(camera.GetComponent<AudioListener>()==null)camera.gameObject.AddComponent<AudioListener>();
        var clip=AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
        if(clip==null)return;
        var importer=AssetImporter.GetAtPath(MusicPath) as AudioImporter;
        var settings=importer.defaultSampleSettings;
        settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.85f;settings.preloadAudioData=true;
        importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.SaveAndReimport();
        var go=GameObject.Find("Garden of Winds - original score");
        if(go==null)go=new GameObject("Garden of Winds - original score");
        var source=go.GetComponent<AudioSource>();if(source==null)source=go.AddComponent<AudioSource>();
        source.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);source.loop=true;source.playOnAwake=false;source.spatialBlend=0;source.volume=.48f;
        source.priority=128;source.dopplerLevel=0;
        if(go.GetComponent<SkyCitySoundscape>()==null)go.AddComponent<SkyCitySoundscape>();
    }
}
