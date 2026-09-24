using UnityEngine;
using UnityEditor;
using Boids.Art;

public static class SkyCityWaterBuilder
{
    public static void Create(int rendererIndex)
    {
        string root=SkyCitySceneBuilder.Root;
        var pool=AssetDatabase.LoadAssetAtPath<Material>(root+"/Materials/Water garden reflection.mat");
        if(pool==null){pool=new Material(Shader.Find("Boids/SkyCity/Reflecting Garden Water"));AssetDatabase.CreateAsset(pool,root+"/Materials/Water garden reflection.mat");}
        pool.SetFloat("_RippleStrength",.023f);EditorUtility.SetDirty(pool);
        foreach(var renderer in Object.FindObjectsOfType<MeshRenderer>())
        {
            if(renderer.name.StartsWith("13 -"))renderer.sharedMaterial=pool;
            if(renderer.name.StartsWith("13 -")||renderer.name.StartsWith("09 -"))renderer.gameObject.layer=4;
        }
        var reflection=new GameObject("Water garden - live URP reflection").AddComponent<SkyCityWaterReflection>();
        reflection.waterHeight=2.42f;reflection.rendererIndex=rendererIndex;
        var spray=AssetDatabase.LoadAssetAtPath<Material>(root+"/Materials/Cascade spray.mat");
        if(spray==null){spray=new Material(Shader.Find("Boids/SkyCity/Soft Water Spray"));AssetDatabase.CreateAsset(spray,root+"/Materials/Cascade spray.mat");}
        var points=new[]{new Vector3(-13.5f,-3,6.1f),new Vector3(-5,-2.7f,4.4f),new Vector3(13.3f,.7f,13.1f),new Vector3(-8.95f,-2.4f,-6.05f),new Vector3(-4.18f,-2.4f,-6.1f)};
        var group=new GameObject("Cascades - spray and airborne mist");
        for(int i=0;i<points.Length;i++)
        {
            var go=new GameObject("Cascade mist "+i);go.layer=4;go.transform.SetParent(group.transform);go.transform.position=points[i];
            var system=go.AddComponent<ParticleSystem>();system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=system.main;main.loop=true;main.duration=5;main.startLifetime=new ParticleSystem.MinMaxCurve(1.6f,3.3f);main.startSpeed=.15f;
            main.startSize=new ParticleSystem.MinMaxCurve(.10f,.45f);main.maxParticles=180;main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startColor=new Color(.86f,.94f,1,.23f);main.prewarm=true;
            var emission=system.emission;emission.rateOverTime=36;
            var shape=system.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.38f;
            var velocity=system.velocityOverLifetime;velocity.enabled=true;velocity.x=new ParticleSystem.MinMaxCurve(.15f,.65f);velocity.y=new ParticleSystem.MinMaxCurve(.1f,.5f);velocity.z=new ParticleSystem.MinMaxCurve(-.1f,.15f);
            var size=system.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.3f),new Keyframe(.5f,1.3f),new Keyframe(1,2)));
            var color=system.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.6f,.2f),new GradientAlphaKey(0,1)});color.color=gradient;
            var noise=system.noise;noise.enabled=true;noise.strength=.18f;noise.frequency=.35f;noise.scrollSpeed=.4f;
            var render=go.GetComponent<ParticleSystemRenderer>();render.sharedMaterial=spray;render.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            system.useAutoRandomSeed=false;system.randomSeed=(uint)(5031+i);system.Play();
        }
    }
}
