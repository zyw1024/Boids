using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Boids.Art;

public static class SkyCityVolumetricBuilder
{
    public static void Create()
    {
        string root=SkyCitySceneBuilder.Root;
        string path=root+"/PerlinWorleyVolume.asset";
        var texture=AssetDatabase.LoadAssetAtPath<Texture3D>(path);
        if(texture==null)
        {
            const int size=128;
            var compute=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Boids/Shaders/SkyCityCloudNoise.compute");
            var rt=new RenderTexture(size,size,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear)
            {dimension=TextureDimension.Tex3D,volumeDepth=size,enableRandomWrite=true,wrapMode=TextureWrapMode.Repeat};
            rt.Create();compute.SetInt("_Size",size);compute.SetTexture(0,"Result",rt);compute.Dispatch(0,size/4,size/4,size/4);
            var request=AsyncGPUReadback.Request(rt,0,TextureFormat.RGBA32);request.WaitForCompletion();
            if(request.hasError)throw new System.InvalidOperationException("Cloud noise GPU readback failed.");
            texture=new Texture3D(size,size,size,TextureFormat.RGBA32,true){name="Periodic Perlin Worley cloud field",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear};
            var pixels=new byte[size*size*size*4];
            for(int layer=0;layer<request.layerCount;layer++)
                System.Array.Copy(request.GetData<byte>(layer).ToArray(),0,pixels,layer*request.layerDataSize,request.layerDataSize);
            texture.SetPixelData(pixels,0);texture.Apply(true,true);AssetDatabase.CreateAsset(texture,path);
            rt.Release();Object.DestroyImmediate(rt);
        }
        var material=AssetDatabase.LoadAssetAtPath<Material>(root+"/Materials/Volumetric atmosphere.mat");
        if(material==null){material=new Material(Shader.Find("Boids/SkyCity/Atmosphere Raymarch"));AssetDatabase.CreateAsset(material,root+"/Materials/Volumetric atmosphere.mat");}
        material.SetTexture("_NoiseTex",texture);material.SetFloat("_Density",.58f);material.SetFloat("_Coverage",.49f);material.SetFloat("_Detail",.065f);
        material.SetVector("_Wind",new Vector4(.95f,.025f,.22f,0));
        material.SetColor("_SunColor",new Color(1.85f,1.60f,1.30f));material.SetColor("_ShadeColor",new Color(.24f,.27f,.38f));
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(root+"/SkyCityRenderer.asset");
        SkyCityAtmosphereFeature feature=null;
        foreach(var item in renderer.rendererFeatures)if(item is SkyCityAtmosphereFeature)feature=(SkyCityAtmosphereFeature)item;
        if(feature==null){feature=ScriptableObject.CreateInstance<SkyCityAtmosphereFeature>();feature.name="Sky City Volumetric Atmosphere";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);}
        feature.cloudMaterial=material;feature.resolutionScale=1f;feature.SetActive(true);feature.Create();
        renderer.SetDirty();EditorUtility.SetDirty(renderer);EditorUtility.SetDirty(feature);EditorUtility.SetDirty(material);
        if(GameObject.Find("Cloud Sea - World Space Volumetric Atmosphere")==null)new GameObject("Cloud Sea - World Space Volumetric Atmosphere");
    }
}
