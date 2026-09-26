using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Boids.Art;

/// <summary>Deterministic cloud composition and cached light transport for the authored island.</summary>
public static class SkyCityHeroCloudBuilder
{
    public const string FieldPath=SkyCityHeroBuilder.Root+"/SculptedCloudField.asset";
    static readonly Vector3 Minimum=new Vector3(-160,-65,-55),Extent=new Vector3(350,165,350);
    const float Extinction=.58f;
    static readonly List<Vector4> centers=new List<Vector4>(),radii=new List<Vector4>();
    static System.Random random;
    static float R(float a,float b){return Mathf.Lerp(a,b,(float)random.NextDouble());}
    static void Lobe(Vector3 p,Vector3 r){centers.Add(new Vector4(p.x,p.y,p.z,0));radii.Add(new Vector4(r.x,r.y,r.z,0));}
    static void Bank(Vector3 p,Vector3 size,int seed)
    {
        random=new System.Random(seed);
        // Broad, connected bases carry several rising turrets. Smaller daughter
        // billows break each turret's silhouette without repeating a five-ball template.
        for(int k=0;k<4;k++)
        {
            float t=(k+.3f)/4;
            var center=p+new Vector3((t-.5f)*size.x*1.3f,R(-.15f,.12f)*size.y,R(-.22f,.22f)*size.z);
            var radius=new Vector3(size.x*R(.20f,.30f),size.y*R(.32f,.62f),size.z*R(.35f,.52f));
            radius.y=Mathf.Min(radius.y,radius.x*R(.9f,1.25f));
            Lobe(center,radius);
            for(int j=0;j<11;j++)
            {
                float a=R(0,Mathf.PI*2),y=R(-.20f,.9f),side=Mathf.Sqrt(1-y*y);
                var offset=Vector3.Scale(new Vector3(Mathf.Cos(a)*side,y,Mathf.Sin(a)*side),radius)*.88f;
                float s=R(.22f,.42f);Lobe(center+offset,radius*s*R(.95f,1.25f));
            }
        }
    }
    [MenuItem("Boids/Hanging Gardens/Sculpt cloud sea")]
    public static void Bake()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Leave Play mode to rebuild cloud assets.");
        centers.Clear();radii.Clear();
        Bank(new Vector3(-69,-7,70),new Vector3(49,30,34),120);
        Bank(new Vector3(69,-6,75),new Vector3(48,26,37),310);
        Bank(new Vector3(-89,-8,155),new Vector3(42,48,33),407);
        Bank(new Vector3(112,-7,180),new Vector3(53,42,34),619);
        Bank(new Vector3(-27,-9,153),new Vector3(44,18,30),724);
        Bank(new Vector3(24,-13,125),new Vector3(39,20,28),731);
        Bank(new Vector3(20,-12,248),new Vector3(77,24,26),875);
        Bank(new Vector3(-44,-25,-8),new Vector3(38,17,31),960);
        Bank(new Vector3(49,-27,-3),new Vector3(42,19,29),971);
        var compute=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Boids/Shaders/SkyCityHeroCloudBake.compute");
        var noise=AssetDatabase.LoadAssetAtPath<Texture3D>(SkyCitySceneBuilder.Root+"/PerlinWorleyVolume.asset");
        const int x=256,y=128,z=256;
        var density=new RenderTexture(x,y,0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear)
        {dimension=TextureDimension.Tex3D,volumeDepth=z,enableRandomWrite=true,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear};
        var lit=new RenderTexture(density.descriptor){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear};
        ComputeBuffer cs=null,rs=null;
        try
        {
            density.Create();lit.Create();cs=new ComputeBuffer(centers.Count,16);rs=new ComputeBuffer(radii.Count,16);
            cs.SetData(centers);rs.SetData(radii);
            compute.SetInts("Size",x,y,z);compute.SetVector("BoundsMin",Minimum);compute.SetVector("BoundsSize",Extent);
            compute.SetVector("SunDirection",RenderSettings.sun!=null?-RenderSettings.sun.transform.forward:new Vector3(.65f,.66f,-.35f).normalized);
            compute.SetFloat("Extinction",Extinction);compute.SetInt("LobeCount",centers.Count);
            int shape=compute.FindKernel("Shape"),lighting=compute.FindKernel("Lighting");
            compute.SetBuffer(shape,"Centers",cs);compute.SetBuffer(shape,"Radii",rs);compute.SetTexture(shape,"Noise",noise);compute.SetTexture(shape,"Result",density);
            compute.Dispatch(shape,x/4,y/4,z/4);
            compute.SetTexture(lighting,"Density",density);compute.SetTexture(lighting,"Noise",noise);compute.SetTexture(lighting,"Result",lit);compute.Dispatch(lighting,x/4,y/4,z/4);
            var request=AsyncGPUReadback.Request(lit,0,TextureFormat.RGBA32);request.WaitForCompletion();
            if(request.hasError)throw new InvalidOperationException("Cloud field readback failed.");
            byte[] pixels=new byte[x*y*z*4];
            for(int layer=0;layer<request.layerCount;layer++)Array.Copy(request.GetData<byte>(layer).ToArray(),0,pixels,layer*request.layerDataSize,request.layerDataSize);
            var texture=AssetDatabase.LoadAssetAtPath<Texture3D>(FieldPath);
            var fresh=new Texture3D(x,y,z,TextureFormat.RGBA32,false){name="SculptedCloudField",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear};
            fresh.SetPixelData(pixels,0);fresh.Apply(false,true);
            if(texture==null){texture=fresh;AssetDatabase.CreateAsset(texture,FieldPath);}
            else{EditorUtility.CopySerialized(fresh,texture);UnityEngine.Object.DestroyImmediate(fresh);EditorUtility.SetDirty(texture);}
            var material=AssetDatabase.LoadAssetAtPath<Material>(SkyCityHeroBuilder.Root+"/Rolling sunlit cloud banks.mat");
            material.SetTexture("_CloudField",texture);material.SetVector("_FieldMin",Minimum);material.SetVector("_FieldSize",Extent);
            material.SetFloat("_Density",Extinction);material.SetFloat("_Steps",1536);material.SetFloat("_Detail",.13f);
            material.SetFloat("_CloudTime",-1);
            material.SetColor("_SunColor",new Color(1.12f,1.04f,.94f));material.SetColor("_ShadeColor",new Color(.52f,.52f,.68f));
            EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
            Debug.Log("Cloud sea rebuilt: "+centers.Count+" authored lobes; 32 MiB cached density/light field.");
        }
        finally{cs?.Release();rs?.Release();density.Release();lit.Release();UnityEngine.Object.DestroyImmediate(density);UnityEngine.Object.DestroyImmediate(lit);}
    }
}
