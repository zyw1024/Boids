using System.Collections.Generic;
using Boids.Art;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

// Camera-specific botanical design: three authored hero silhouettes, layered flower groves,
// and depth-separated brushwork. Curves are in world space so their composition stays editable.
public static partial class RedonSceneBuilder
{
    static Mesh[] flowerCrowns, flowerBrushes;
    static Dictionary<Material,Material> crownPaint;
    static Material apricotFlower, violetFlower, tealFlower, darkFlower, distantFlower;
    static int ribbonIndex;

    static void BuildGardenPalette()
    {
        foliage[0] = Paint("Petal_Lavender","75739C","293F66","C4A4C2",.28f,.15f,.5f);
        foliage[1] = Paint("Petal_Indigo","405C86","152F4B","8B97BC",.3f,.15f,.35f);
        foliage[2] = Paint("Petal_Peacock","31727E","163E56","89ADA4",.28f,.12f,.32f);
        foliage[3] = Paint("Petal_Rose","B77A91","514B77","E2B196",.27f,.16f,.4f);
        foliage[4] = Paint("Petal_Foreground","193449","0B1F32","536178",.25f,.20f,.3f);
        foliage[5] = Paint("Petal_Distant","2E5265","193A50","6D7A86",.38f,.03f,0);
        for (int i=0;i<foliage.Length;i++)
        {
            foliage[i].SetFloat("_Underpaint",i==4?.40f:.63f);
            if(i==0||i==1||i==3)
            {
                foliage[i].SetTexture("_ColorTex",petalVeining);
                foliage[i].SetFloat("_ColorUV",1);
                foliage[i].SetFloat("_Underpaint",.78f);
                foliage[i].SetFloat("_Veins",.08f);
            }
            foliageBrushes[i]=Brush("Petal_Brush_"+i,foliage[i].GetColor("_BaseColor"),
                foliage[i].GetColor("_LightColor"),.42f,.75f);
        }
        foliage[5].SetFloat("_Underpaint",.25f);
        apricotFlower=Paint("Flower_Apricot","DB9476","805475","F8C88D",.58f,.06f,0);
        violetFlower=Paint("Flower_Violet","686991","30496F","A597B2",.33f,.09f,0);
        tealFlower=Paint("Flower_Teal","396D79","1B3D58","869D9D",.32f,.08f,0);
        darkFlower=Paint("Flower_Foreground","24415B","0C253E","655A82",.36f,.10f,0);
        distantFlower=Paint("Flower_Distant","486A85","2D4B6B","9595A5",.26f,.05f,0);
        foreach(var m in new[]{apricotFlower,violetFlower,tealFlower,darkFlower,distantFlower})
            m.SetFloat("_Underpaint",m==apricotFlower?.28f:.5f);
        crownPaint=new Dictionary<Material,Material>();
        foreach(var m in new[]{apricotFlower,violetFlower,tealFlower,darkFlower,distantFlower})
            crownPaint[m]=Brush(m.name+"_Marks",m.GetColor("_BaseColor")*.85f,m.GetColor("_LightColor"),.52f,.8f);
        fishPaint[0]=Paint("Fish_Pearl","CDB987","4B747D","E8D2A1",.18f,.14f,0);
        fishPaint[1]=Paint("Fish_Peach","C78D80","4E687D","E9B99A",.18f,.12f,0);
        fishPaint[2]=Paint("Fish_Jade","72A4A3","214F69","B9C9AA",.2f,.10f,0);
        fishPaint[3]=Paint("Fish_Dusk","2E5E78","173F5D","6892A5",.2f,.06f,0);
        fishPaint[4]=Paint("Fish_Gold","C1A46F","53737C","EDD2A0",.18f,.12f,0);
    }

    static void BuildFlowerGeometry()
    {
        Random.InitState(2731);
        var cups=new Mesh[4];
        for(int k=0;k<cups.Length;k++)
        {
            ClearMesh();
            const int around=36,rings=10;
            for(int r=0;r<=rings;r++)for(int a=0;a<=around;a++)
            {
                float t=(float)r/rings,angle=(float)a/around*Mathf.PI*2;
                float edge=1+.11f*Mathf.Sin(angle*7+k)+.04f*Mathf.Sin(angle*13);
                float x=Mathf.Sin(angle)*t*.55f*edge;
                float y=Mathf.Cos(angle)*t*.64f*edge+.28f;
                float z=t*t*.29f+.12f*Mathf.Cos(angle*2+k)*t;
                vertices.Add(new Vector3(x,y,z));
                uvs.Add(new Vector2(x*.75f+.5f,y*.65f+.3f));
                colors.Add(Color.Lerp(new Color(.7f,.76f,.86f),Color.white,t));
            }
            Grid(around,rings);cups[k]=FinishMesh();
        }
        flowerCrowns=new Mesh[4];flowerBrushes=new Mesh[4];
        for(int k=0;k<flowerCrowns.Length;k++)
        {
            var pieces=new List<CombineInstance>();
            for(int p=0;p<27;p++)
            {
                float angle=p*2.39996f;
                float radius=Mathf.Sqrt((p+.5f)/27)*.83f;
                var pos=new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius*.78f,
                    .38f-radius*.65f+Random.Range(-.15f,.15f));
                var rot=Quaternion.Euler(Random.Range(-28,28),Random.Range(-30,30),angle*Mathf.Rad2Deg);
                var scale=Vector3.one*Random.Range(.65f,1.02f);
                pieces.Add(new CombineInstance{mesh=cups[(p+k)%4],transform=Matrix4x4.TRS(pos,rot,scale)});
            }
            var m=new Mesh{indexFormat=IndexFormat.UInt32};
            m.CombineMeshes(pieces.ToArray(),true,true);
            flowerCrowns[k]=SaveMesh("Garden_Flower_"+k,m);
            ClearMesh();
            var surface=flowerCrowns[k].vertices;
            for(int mark=0;mark<640;mark++)
            {
                var point=surface[Random.Range(0,surface.Length)];
                float a=Random.Range(0,Mathf.PI),s=Random.Range(.025f,.14f);
                var right=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*s;
                var up=new Vector3(-Mathf.Sin(a),Mathf.Cos(a),0)*s*Random.Range(.25f,.65f);
                Quad(point+Vector3.back*.025f,right,up,new Color(Random.value,Random.value,1,Random.Range(.25f,.85f)),Random.Range(0,16));
            }
            flowerBrushes[k]=SaveMesh("Garden_Flower_Marks_"+k,FinishMesh());
        }
        foreach(var c in cups)Object.DestroyImmediate(c);
    }

    static Vector3 Cubic(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float t)
    {
        float s=1-t;return s*s*s*a+3*s*s*t*b+3*s*t*t*c+t*t*t*d;
    }
    static Vector3 RibbonPoint(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float width,float roll,float u,float t)
    {
        var center=Cubic(a,b,c,d,t);
        var tangent=Cubic(a,b,c,d,Mathf.Min(1,t+.005f))-Cubic(a,b,c,d,Mathf.Max(0,t-.005f));
        var side=new Vector3(tangent.y,-tangent.x,0).normalized;
        float w=Mathf.Pow(Mathf.Max(.00001f,Mathf.Sin(t*Mathf.PI)),.62f)*width;
        w*=1+.025f*Mathf.Sin(t*37)+.016f*Mathf.Sin(t*69+u);
        float s=u*2-1;
        center+=side*w*s;
        center.z+=roll*w*s*s + Mathf.Sin(t*Mathf.PI)*.3f + Mathf.Sin(s*8+t*5)*w*.018f;
        return center;
    }
    static void Ribbon(string name,Vector3 a,Vector3 b,Vector3 c,Vector3 d,float width,float roll,int palette)
    {
        string key="Garden_Ribbon_"+(ribbonIndex++).ToString("00");
        ClearMesh();
        const int across=24,along=56;
        for(int y=0;y<=along;y++)for(int x=0;x<=across;x++)
        {
            float t=(float)y/along,u=(float)x/across;
            vertices.Add(RibbonPoint(a,b,c,d,width,roll,u,t));uvs.Add(new Vector2(u,t));
            colors.Add(Color.Lerp(new Color(.65f,.72f,.85f),Color.white,Mathf.SmoothStep(0,1,t*1.5f)));
        }
        Grid(across,along);
        Instance(name,SaveMesh(key,FinishMesh()),foliage[palette],Vector3.zero,Vector3.one,Quaternion.identity,plantsRoot);
        // Marks follow the authored ribbon curvature and remain fixed to the mesh.
        ClearMesh();
        for(int s=0;s<200;s++)
        {
            float u=Random.Range(.03f,.97f),t=Random.Range(.08f,.97f);
            float du=Random.Range(.02f,.11f),dt=Random.Range(.007f,.033f);
            var p=RibbonPoint(a,b,c,d,width,roll,u,t);
            var right=(RibbonPoint(a,b,c,d,width,roll,Mathf.Min(1,u+du),t)-RibbonPoint(a,b,c,d,width,roll,Mathf.Max(0,u-du),t))*.5f;
            var up=(RibbonPoint(a,b,c,d,width,roll,u,Mathf.Min(1,t+dt))-RibbonPoint(a,b,c,d,width,roll,u,Mathf.Max(0,t-dt)))*.5f;
            Quad(p+Vector3.back*.018f,right,up,new Color(Random.value,Random.value,1,Random.Range(.22f,.7f)),Random.Range(0,16));
        }
        Instance(name+" brushwork",SaveMesh(key+"_Brushes",FinishMesh()),foliageBrushes[palette],Vector3.zero,Vector3.one,Quaternion.identity,brushRoot);
    }

    static void CreateHeroPetals()
    {
        Random.InitState(1113);ribbonIndex=0;
        Ribbon("Left lavender sail",new Vector3(-9.6f,4.5f,-1),new Vector3(-7.8f,7,0),new Vector3(-7,11.2f,1),new Vector3(-9.6f,11.1f,0),1.8f,.28f,0);
        Ribbon("Left unfurling canopy",new Vector3(-8.7f,4.8f,0),new Vector3(-5.5f,10.5f,1),new Vector3(-2.4f,9.1f,2),new Vector3(-4,7.4f,.5f),1.45f,.48f,0);
        Ribbon("Left indigo countercurve",new Vector3(-10.8f,2.1f,2),new Vector3(-7.3f,7.6f,3),new Vector3(-10.8f,12.1f,2),new Vector3(-12,10.6f,0),1.75f,.6f,1);
        Ribbon("Rose lip",new Vector3(-8.7f,4.5f,1),new Vector3(-7.5f,8.4f,1),new Vector3(-5,10.5f,3),new Vector3(-4.8f,9.2f,0),1.1f,.7f,3);
        Ribbon("Right teal canopy",new Vector3(10.5f,0,0),new Vector3(9,4.5f,2),new Vector3(9,7.3f,1),new Vector3(11.3f,8.7f,0),1.3f,.3f,2);
        Ribbon("Right dark folded sail",new Vector3(11,-2,-3),new Vector3(8.8f,1.8f,-2),new Vector3(8.5f,5.5f,-1),new Vector3(10.3f,6.3f,-3),1,.4f,4);
        // Long low foreground arcs replace the previous wall of boulders.
        Ribbon("Foreground left flourish",new Vector3(-11,-1.5f,-7),new Vector3(-9,2.5f,-6),new Vector3(-5.8f,3,-6),new Vector3(-3.8f,1,-5),.8f,.55f,4);
        Ribbon("Foreground right flourish",new Vector3(12,-2,-6),new Vector3(11,1,-5),new Vector3(8.6f,1.9f,-5),new Vector3(5.5f,.3f,-6),.65f,.5f,4);
        Ribbon("Left turquoise underleaf",new Vector3(-11,-2,-4),new Vector3(-8,3.4f,-2),new Vector3(-5.7f,4.1f,-1),new Vector3(-5.8f,2.2f,-2),.9f,.5f,2);
    }

    static void Crown(string name,Vector3 pos,float scale,Material material,int variant)
    {
        var flower=Instance(name,flowerCrowns[variant%4],material,pos,new Vector3(scale,scale,scale*.8f),
            Quaternion.Euler(Random.Range(-10,10),Random.Range(-15,15),Random.Range(-30,30)),plantsRoot);
        Instance("Broken flower pigment",flowerBrushes[variant%4],crownPaint[material],Vector3.zero,Vector3.one,Quaternion.identity,flower.transform);
    }
    static void Grove(string name,Vector3 root,Vector3 head,float size,Material material,int variant)
    {
        ClearMesh();
        Vector3 mid=Vector3.Lerp(root,head,.5f)+new Vector3(.6f,0,.8f);
        const int rings=36,sides=12;
        for(int y=0;y<=rings;y++)for(int x=0;x<=sides;x++)
        {
            float t=(float)y/rings,u=1-t,angle=(float)x/sides*Mathf.PI*2;
            Vector3 center=u*u*root+2*u*t*mid+t*t*head;
            float radius=size*(.014f+Mathf.Pow(t,1.8f)*.4f);
            radius*=1+Mathf.Sin(t*32+angle*3)*.16f+Mathf.Sin(t*59-angle*5)*.09f;
            center+=new Vector3(Mathf.Sin(t*19)*radius*.3f,0,0);
            vertices.Add(center+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));
            uvs.Add(new Vector2((float)x/sides,t*2));colors.Add(Color.white);
        }
        Grid(sides,rings);
        Instance(name+" stems",SaveMesh(name.Replace(" ","_")+"_Stems",FinishMesh()),foliage[5],
            Vector3.zero,Vector3.one,Quaternion.identity,plantsRoot);
        Crown(name,head,size*.62f,material,variant);
        for(int i=0;i<9;i++)
        {
            float a=i*2.39996f;
            float r=Mathf.Sqrt((i+.5f)/9)*size*.67f;
            Vector3 offset=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r*.82f,Random.Range(-.65f,.65f));
            Crown(name+" small flowering branch",head+offset,size*Random.Range(.32f,.51f),material,i+variant);
        }
        ClearMesh();
        for(int i=0;i<220;i++)
        {
            float t=Random.value,u=1-t;
            Vector3 p=u*u*root+2*u*t*mid+t*t*head;
            p+=new Vector3(Random.Range(-.17f,.17f)*size,Random.Range(-.3f,.3f),-.15f);
            float s=Random.Range(.04f,.13f)*size;
            Quad(p,Vector3.right*s*.38f,Vector3.up*s*Random.Range(2,6),new Color(Random.value,Random.value,1,Random.Range(.2f,.6f)),Random.Range(0,16));
        }
        Instance(name+" stem brushwork",SaveMesh(name.Replace(" ","_")+"_Stem_Marks",FinishMesh()),foliageBrushes[5],
            Vector3.zero,Vector3.one,Quaternion.identity,brushRoot);
    }
    static void CreateBotanicalGrove()
    {
        Random.InitState(3993);
        // Depth progression: distant trees enclose the light, then overlapping midground blossoms.
        Grove("Distant left cathedral",new Vector3(-3,-4,38),new Vector3(-4.5f,12,38),1.9f,distantFlower,0);
        Grove("Distant right cathedral",new Vector3(6,-3,40),new Vector3(7.6f,12.7f,40),1.7f,tealFlower,1);
        Grove("Far gold silhouette",new Vector3(4,4,42),new Vector3(4.6f,11.6f,42),.85f,violetFlower,2);
        Grove("Apricot dream crown",new Vector3(-2.8f,-1,23),new Vector3(-2.3f,8.1f,23),2.5f,apricotFlower,2);
        Crown("Rose lower crown",new Vector3(-3.2f,6.6f,20),1.7f,violetFlower,1);
        Grove("Right violet tower",new Vector3(6.6f,-3,21),new Vector3(6.8f,4.3f,21),1.6f,violetFlower,3);
        Grove("Right upper teal tower",new Vector3(9.4f,3,23),new Vector3(9.6f,10.6f,23),1.6f,tealFlower,2);
        Grove("Left upper violet tower",new Vector3(-7.2f,5,19),new Vector3(-5.9f,13.1f,19),2.4f,violetFlower,0);
        Grove("Left shadow shrub",new Vector3(-7.5f,-1,8),new Vector3(-8,4.8f,8),1.45f,darkFlower,3);
        Grove("Small far island",new Vector3(3,-3,34),new Vector3(3.2f,2.0f,34),.78f,distantFlower,3);
        Grove("Small left island",new Vector3(-.9f,-3,31),new Vector3(-1.6f,1.3f,31),.7f,distantFlower,1);
        // A descending foreground garden, rather than repeated upright cliffs.
        for(int i=0;i<38;i++)
        {
            float t=(float)i/37;
            float x=Mathf.Lerp(-11,1,t)+Random.Range(-.55f,.55f);
            float y=Mathf.Lerp(2.3f,-2.3f,t)+Random.Range(-.4f,.4f);
            float z=Mathf.Lerp(1,14,t)+Random.Range(-1,2);
            float s=Random.Range(.4f,1.1f);
            Crown("Foreground flower bed",new Vector3(x,y,z),s,i%13==0?apricotFlower:(i%4==0?violetFlower:darkFlower),i);
            if(i%4==0)Cliff(new Vector3(x,y-.9f,z+1),new Vector3(s,s*.8f,s),0);
        }
        for(int i=0;i<18;i++)
        {
            float x=Random.Range(7.4f,11.5f),y=Random.Range(-2,2),z=Random.Range(1,15);
            Crown("Right flower bed",new Vector3(x,y,z),Random.Range(.45f,1.2f),i%3==0?violetFlower:darkFlower,i);
        }
        for(int i=0;i<100;i++)
        {
            float t=Random.value;
            float x=Mathf.Lerp(-11,.5f,t)+Random.Range(-.6f,.6f);
            float y=Mathf.Lerp(2.5f,-2,t)+Random.Range(-1.3f,1.1f);
            Crown("Meadow small blossom",new Vector3(x,y,Mathf.Lerp(-1,11,t)-.2f),
                Random.Range(.12f,.4f),i%17==0?apricotFlower:(i%4==0?violetFlower:(i%3==0?tealFlower:darkFlower)),i);
        }
    }

    static void CreateGardenAccents()
    {
        Random.InitState(877);
        var gold=Paint("Golden_Stems","B3966B","314B65","EDD2A1",.18f,.08f,0);
        ClearMesh();
        Sprig(new Vector3(-9,0,-2),new Vector3(.24f,1,0),5.2f,.012f);
        Sprig(new Vector3(-7.2f,.3f,-1),new Vector3(-.13f,1,0),3.5f,.01f);
        Sprig(new Vector3(-5.8f,-.8f,2),new Vector3(-.2f,1,0),2.7f,.009f);
        Instance("Golden meadow stems",SaveMesh("Garden_Gold_Stems",FinishMesh()),gold,Vector3.zero,Vector3.one,Quaternion.identity,plantsRoot);
        ClearMesh();
        for(int i=0;i<1600;i++)
        {
            float t=Random.value;
            var p=new Vector3(Mathf.Lerp(-10.3f,-1,t)+Random.Range(-.8f,.8f),Mathf.Lerp(3.0f,-1.7f,t)+Random.Range(-1.1f,1.1f),Mathf.Lerp(-3,9,t));
            float s=Random.Range(.011f,.044f);
            Quad(p,Vector3.right*s,Vector3.up*s*Random.Range(.5f,1.8f),new Color(Random.value,Random.value,1,Random.Range(.35f,1)),Random.Range(0,16));
        }
        Instance("Golden pollen meadow",SaveMesh("Garden_Gold_Dabs",FinishMesh()),brushGold,Vector3.zero,Vector3.one,Quaternion.identity,brushRoot);
        // Broad atmospheric brush clusters sit at several actual depths.
        for(int group=0;group<3;group++)
        {
            ClearMesh();
            for(int i=0;i<340;i++)
            {
                Vector3 p=new Vector3(Random.Range(-11,11),Random.Range(-2,14),35+group*6);
                float s=Random.Range(.10f,.45f);
                Quad(p,Vector3.right*s,Vector3.up*s*Random.Range(.35f,1.1f),new Color(Random.value,Random.value,1,Random.Range(.12f,.4f)),Random.Range(0,16));
            }
            Instance("Water brush field "+group,SaveMesh("Garden_Water_Dabs_"+group,FinishMesh()),brushDistant,Vector3.zero,Vector3.one,Quaternion.identity,brushRoot);
        }
    }

    static Vector3 LivingSchoolPath(float t)
    {
        return Cubic(new Vector3(-7.0f,5.0f,-3),new Vector3(9.2f,3.0f,3),
            new Vector3(8.0f,8.0f,10),new Vector3(2.3f,10.8f,17),t);
    }
    static void CreateLivingSchool()
    {
        Random.InitState(6381);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(MoonveilAssetSetup.PrefabPath);
        var agents=new List<Transform>();
        for(int i=0;i<FishCount;i++)
        {
            float t=(i+Random.value*.7f)/FishCount;
            var p=LivingSchoolPath(t);
            float spread=Mathf.Lerp(1.05f,.2f,t);
            p+=new Vector3(Random.Range(-.45f,.45f),Random.Range(-spread,spread),Random.Range(-1.2f,1.2f));
            var tangent=LivingSchoolPath(Mathf.Min(1,t+.01f))-LivingSchoolPath(Mathf.Max(0,t-.01f));
            tangent.z*=.22f;
            var fish=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            fish.name="Dream Fish "+i.ToString("00");fish.transform.SetParent(fishRoot);
            fish.transform.SetPositionAndRotation(p,Quaternion.LookRotation(tangent,Vector3.up));
            float size=Mathf.Lerp(1.13f,.25f,t)*Random.Range(.65f,1.22f);
            fish.transform.localScale=new Vector3(.85f,.86f,1.25f)*size;
            var animator=fish.GetComponentInChildren<Animator>();if(animator!=null)animator.enabled=true;
            var motion=fish.GetComponent<MoonveilMotion>();if(motion!=null)motion.enabled=true;
            int palette=t>.48f?3:(i%4==0?3:Random.Range(0,fishPaint.Length));
            foreach(var renderer in fish.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                renderer.sharedMaterial=fishPaint[palette];
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
            agents.Add(fish.transform);
        }
        var school=fishRoot.gameObject.AddComponent<DreamSchoolController>();
        school.Configure(agents.ToArray(),brushGold,atlas);
    }
}
