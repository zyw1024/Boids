using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SkyCity.Runtime;
using UnityEngine;

public static class SkyCityGardenReview
{
    public static object Run()
    {
        var checks=new Dictionary<string,bool>();
        var soil=Enumerable.Repeat(true,81).ToArray();
        var a=new SkyCityGardenAutomaton(9,9,soil);var b=new SkyCityGardenAutomaton(9,9,soil);
        a.Sow(4,4,0);b.Sow(4,4,0);
        for(int i=0;i<50;i++){a.Tick(.2f,1,1,3,4);b.Tick(.2f,1,1,3,4);}
        checks["deterministicEvolution"]=a.Fingerprint()==b.Fingerprint();
        checks["propagationBeyondSeeds"]=a.Awakened>8;
        bool symmetric=true;for(int y=0;y<9;y++)for(int x=0;x<9;x++)if(a.Cells[y*9+x]!=a.Cells[(8-y)*9+8-x])symmetric=false;
        checks["synchronousSymmetry"]=symmetric;
        var masked=(bool[])soil.Clone();for(int y=0;y<9;y++)masked[y*9+5]=false;
        var c=new SkyCityGardenAutomaton(9,9,masked);c.Sow(3,4,0);
        for(int i=0;i<40;i++)c.Tick(.2f,1,1,3,4);
        checks["soilMaskBlocksSpread"]=Enumerable.Range(0,81).Where(i=>i%9>=5).All(i=>c.Cells[i]==SkyCityGardenAutomaton.Phase.Rest);
        var high=new SkyCityGardenAutomaton(9,9,soil);high.Sow(4,4,0);for(int i=0;i<50;i++)high.Tick(.2f,4,1,3,4);
        checks["thresholdChangesBehaviour"]=high.Awakened==0&&a.Awakened>0;
        var edge=new SkyCityGardenAutomaton(9,9,soil);edge.Sow(0,0,0);for(int i=0;i<6;i++)edge.Tick(.2f,1,1,3,4);
        checks["noEdgeWrapping"]=edge.Cells[8]==SkyCityGardenAutomaton.Phase.Rest&&edge.Cells[72]==SkyCityGardenAutomaton.Phase.Rest;
        int branchA,branchB;var first=SkyCityBotanyGeometry.Tree(5,1,out branchA);var second=SkyCityBotanyGeometry.Tree(5,1,out branchB);
        checks["deterministicBranchGeometry"]=branchA==branchB&&first.vertices.SequenceEqual(second.vertices)&&first.triangles.SequenceEqual(second.triangles);
        var v=first.vertices;var n=first.normals;var tr=first.triangles;float lowest=1;
        // The first six rings are the trunk; winding and normals must agree at every segment.
        for(int t=0;t<3*6*2*3;t+=3){int i=tr[t],j=tr[t+1],k=tr[t+2];lowest=Mathf.Min(lowest,Vector3.Dot(Vector3.Cross(v[j]-v[i],v[k]-v[i]).normalized,n[i]));}
        checks["outwardTrunkFaces"]=lowest>.7f;
        checks["boundedBranchBudget"]=branchA==139&&first.vertexCount<40000;
        checks["finiteGeometry"]=v.All(p=>!float.IsNaN(p.x)&&!float.IsInfinity(p.x)&&p.sqrMagnitude<1000);
        UnityEngine.Object.DestroyImmediate(first);UnityEngine.Object.DestroyImmediate(second);
        var result=new{passed=checks.All(p=>p.Value),checks,branches=branchA,lowestNormalAgreement=lowest};
        Directory.CreateDirectory("Captures/SkyCityWorld");File.WriteAllText("Captures/SkyCityWorld/BotanyAlgorithms.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
    }
}
