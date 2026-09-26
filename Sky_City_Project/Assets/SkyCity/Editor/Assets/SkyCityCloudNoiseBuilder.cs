using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class SkyCityCloudNoiseBuilder
{
    [MenuItem("Sky City/Assets/Create cloud noise")]
    public static void Create()
    {
        string root=SkyCityAssetPaths.Shared;
        string path=root+"/PerlinWorleyVolume.asset";
        var texture=AssetDatabase.LoadAssetAtPath<Texture3D>(path);
        if(texture==null)
        {
            const int size=128;
            var compute=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/SkyCity/Shaders/SkyCityCloudNoise.compute");
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
        AssetDatabase.SaveAssets();
    }
}
