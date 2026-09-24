using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class SkyCityCloudBuilder
{
    public static void Create()
    {
        const string root=SkyCitySceneBuilder.Root;
        string path=root+"/CloudDensity_v4.asset";
        var texture=AssetDatabase.LoadAssetAtPath<Texture3D>(path);
        if(texture==null)
        {
            const int size=128;
            var centers=new Vector3[169];var radii=new Vector3[169];var random=new System.Random(51615);
            centers[0]=new Vector3(0,-.10f,0);radii[0]=new Vector3(.30f,.26f,.31f);
            for(int j=1;j<41;j++)
            {
                float a=j*2.39996f;float r=.29f*Mathf.Sqrt((float)random.NextDouble());
                centers[j]=new Vector3(Mathf.Cos(a)*r,(float)random.NextDouble()*.35f-.13f,Mathf.Sin(a)*r*.88f);
                float s=.09f+(float)random.NextDouble()*.10f;
                radii[j]=new Vector3(s,s*(.9f+(float)random.NextDouble()*.3f),s);
            }
            for(int j=41;j<centers.Length;j++)
            {
                int parent=1+(j-41)%40;float a=j*2.39996f;float y=(float)random.NextDouble()*1.8f-.8f;float s=Mathf.Sqrt(1-y*y);
                centers[j]=centers[parent]+Vector3.Scale(radii[parent],new Vector3(Mathf.Cos(a)*s,y,Mathf.Sin(a)*s))*.86f;
                float radius=.026f+(float)random.NextDouble()*.037f;radii[j]=Vector3.one*radius;
            }
            var pixels=new Color[size*size*size];int index=0;
            for(int z=0;z<size;z++)for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                var p=new Vector3(x/(float)(size-1)-.5f,y/(float)(size-1)-.5f,z/(float)(size-1)-.5f);
                float field=-1;
                for(int j=0;j<centers.Length;j++)
                {
                    var delta=p-centers[j];var radius=radii[j];
                    float q=1-Mathf.Sqrt(delta.x*delta.x/(radius.x*radius.x)+delta.y*delta.y/(radius.y*radius.y)+delta.z*delta.z/(radius.z*radius.z));
                    field=Mathf.Max(field,q);
                }
                float a=Mathf.PerlinNoise(p.x*19+31,p.y*19+17);
                float b=Mathf.PerlinNoise(p.y*25+15,p.z*25+42);
                float c=Mathf.PerlinNoise(p.z*51+77,p.x*51+68);
                float density=Mathf.SmoothStep(0,1,Mathf.Clamp01((field-.015f-(a*.12f+b*.08f+c*.055f))/.15f));
                pixels[index++]=new Color(density,0,0,1);
            }
            // Sweep sun optical depth down through each slice. Lighting is baked into the
            // volume itself, so the live ray marcher needs just one sample per step.
            var depths=new float[pixels.Length];
            for(int y=size-1;y>=0;y--)for(int z=0;z<size;z++)for(int x=0;x<size;x++)
            {
                int k=x+y*size+z*size*size;
                float depth=y==size-1?0:SampleSlice(depths,size,x+.55f/.75f,y+1,z-.35f/.75f);
                depth+=pixels[k].r*.24f;
                depths[k]=depth;pixels[k].g=Mathf.Exp(-depth*.72f);
            }
            texture=new Texture3D(size,size,size,TextureFormat.RGBA32,false){name="Cloud density and sun transmission",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear};
            texture.SetPixels(pixels);texture.Apply(false,true);AssetDatabase.CreateAsset(texture,path);
        }
        var material=AssetDatabase.LoadAssetAtPath<Material>(root+"/Materials/Cloud volume.mat");
        if(material==null){material=new Material(Shader.Find("Boids/SkyCity/Volumetric Clouds"));AssetDatabase.CreateAsset(material,root+"/Materials/Cloud volume.mat");}
        material.SetTexture("_DensityTex",texture);material.SetFloat("_Density",1.25f);EditorUtility.SetDirty(material);
        var group=new GameObject("Cloud Sea - Volumetric Light");
        var centersWorld=new[]{new Vector3(-12,-6,7),new Vector3(16,-3,18),new Vector3(-19,-5,-8)};
        var scales=new[]{new Vector3(18,10,14),new Vector3(12,7,10),new Vector3(16,10,11)};
        material.SetFloat("_Density",.075f);
        for(int j=0;j<centersWorld.Length;j++)
        {
            var cloud=GameObject.CreatePrimitive(PrimitiveType.Cube);cloud.name="Cloud bank "+j;cloud.transform.SetParent(group.transform);
            cloud.transform.position=centersWorld[j];cloud.transform.localScale=scales[j];
            UnityEngine.Object.DestroyImmediate(cloud.GetComponent<Collider>());
            var renderer=cloud.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
    }
    static float SampleSlice(float[] data,int n,float x,int y,float z)
    {
        if(x<0||x>=n-1||z<0||z>=n-1)return 0;
        int ix=(int)x,iz=(int)z;float fx=x-ix,fz=z-iz;int k=ix+y*n+iz*n*n;
        return Mathf.Lerp(Mathf.Lerp(data[k],data[k+1],fx),Mathf.Lerp(data[k+n*n],data[k+n*n+1],fx),fz);
    }
}
