using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Boids.Art;
using UnityEngine;

/// <summary>Opt-in geometry audit against the actual architecture collision meshes.</summary>
public static class SkyCityBotanyClearanceReview
{
    sealed class Finding
    {
        public string plant,architecture,point;public float windTime;
    }
    static bool Architecture(Collider collider)
    {
        return collider is MeshCollider && (collider.name.StartsWith("Architecture patch ") ||
            (collider.transform.parent!=null&&collider.transform.parent.name=="Arrival collision shells"));
    }
    public static object Run(string name="BotanyClearance",bool streamed=false)
    {
        var player=UnityEngine.Object.FindObjectOfType<SkyCityFirstPerson>();
        var director=player==null?null:player.world.GetComponent<SkyCityDistrictGardens>().director;
        if(director==null)throw new InvalidOperationException("Open SkyCityWorld.");
        var filters=new List<MeshFilter>();
        if(streamed)
        {
            var streaming=director.player.world.GetComponent<SkyCityDistrictGardens>();
            foreach(var coord in streaming.Coordinates)foreach(var g in streaming.GardensAt(coord))
            {
                // Only test districts with physical near-LOD geometry; distant LODs have no shells.
                if(!g.isActiveAndEnabled||!g.transform.parent.parent.GetComponentsInChildren<MeshCollider>().Any(c=>c.sharedMesh!=null&&c.enabled))continue;
                filters.Add(g.flowers.GetComponent<MeshFilter>());
                var tree=g.transform.Find("Recursive courtyard tree");if(tree!=null)filters.Add(tree.GetComponent<MeshFilter>());
            }
        }
        else
        {
            foreach(var generation in director.treeGenerations)filters.AddRange(generation.GetComponentsInChildren<MeshFilter>(true));
            filters.AddRange(director.gardens.Select(g=>g.flowers.GetComponent<MeshFilter>()));
        }
        Physics.SyncTransforms();bool old=Physics.queriesHitBackfaces;Physics.queriesHitBackfaces=true;
        var findings=new List<Finding>();var results=new List<object>();var hits=new RaycastHit[32];long tested=0;
        try
        {
            foreach(var filter in filters)
            {
                var mesh=filter.sharedMesh;var vertices=mesh.vertices;var colors=mesh.colors;var roots=new List<Vector3>();mesh.GetUVs(2,roots);
                bool tree=filter.name.Contains("Wind tree")||filter.name.Contains("Recursive courtyard tree");
                var world=new Vector3[vertices.Length];var indices=mesh.triangles;var edges=new HashSet<ulong>();
                for(int i=0;i<indices.Length;i+=3)for(int j=0;j<3;j++)
                {uint a=(uint)indices[i+j],b=(uint)indices[i+(j+1)%3];edges.Add(((ulong)Math.Min(a,b)<<32)|Math.Max(a,b));}
                int before=findings.Count;long prior=tested;
                foreach(float time in new[]{0f,1.7f,4.2f,7.5f})
                {
                    for(int i=0;i<vertices.Length;i++)
                    {
                        var p=filter.transform.TransformPoint(vertices[i]);
                        float leaf=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.1f,.5f,colors[i].a));
                        float weight=tree?leaf*.08f:Mathf.Max(0,vertices[i].y-roots[i].y)*.13f;
                        float wind=Mathf.Sin(time*1.3f+p.x*.7f+p.z*.9f)+.3f*Mathf.Sin(time*2.2f+p.x*2);
                        p.x+=wind*weight*2;p.z+=Mathf.Sin(time*1.7f+p.x)*weight*.45f*2;world[i]=p;
                    }
                    foreach(ulong edge in edges)
                    {
                        var a=world[(int)(edge>>32)];var delta=world[(int)(edge&0xffffffff)]-a;float distance=delta.magnitude;
                        if(distance<.0001f)continue;
                        tested++;int n=Physics.RaycastNonAlloc(a,delta/distance,hits,distance,1,QueryTriggerInteraction.Ignore);
                        for(int h=0;h<n;h++)
                        {
                            if(!Architecture(hits[h].collider)||hits[h].distance<.0002f||hits[h].distance>distance-.0002f)continue;
                            findings.Add(new Finding{plant=filter.transform.parent.parent.parent.name+" / "+filter.transform.parent.name+" / "+filter.name,architecture=hits[h].collider.name,point=hits[h].point.ToString("F4"),windTime=time});break;
                        }
                        if(findings.Count-before>=12)break;
                    }
                    if(findings.Count-before>=12)break;
                }
                results.Add(new{plant=filter.transform.parent.name+" / "+filter.name,segments=tested-prior,intersections=findings.Count-before});
            }
        }
        finally{Physics.queriesHitBackfaces=old;}
        var result=new{passed=filters.Count>0&&findings.Count==0,mode=streamed?"streamed near LOD":"authored island: all branch depths and LODs",meshes=filters.Count,testedSegments=tested,maximumBreeze=2,windTimes=new[]{0f,1.7f,4.2f,7.5f},results,findings,note="Full-grown plant mesh edges against actual architecture collision surfaces; four maximum-breeze samples. This checks the reported overlap, not every possible wind time or every world seed."};
        Directory.CreateDirectory("Captures/SkyCityWorld/ClippingReview");File.WriteAllText("Captures/SkyCityWorld/ClippingReview/"+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
