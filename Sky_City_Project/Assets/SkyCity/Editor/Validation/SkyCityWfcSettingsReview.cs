using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SkyCity.Runtime.WorldGeneration;
using UnityEditor;

public static class SkyCityWfcSettingsReview
{
    public static Task<object> Pending;
    public static void Run()
    {
        if(Pending!=null&&!Pending.IsCompleted)throw new InvalidOperationException("WFC review already running.");
        Pending=Task.Run(Check);EditorApplication.update+=Finish;
    }
    static void Finish()
    {
        if(Pending==null||!Pending.IsCompleted)return;EditorApplication.update-=Finish;
        Directory.CreateDirectory("Captures/SkyCityWorld");
        File.WriteAllText("Captures/SkyCityWorld/WfcSolver.json",Newtonsoft.Json.JsonConvert.SerializeObject(Pending.Result,Newtonsoft.Json.Formatting.Indented));
    }
    static void Require(bool ok,string text){if(!ok)throw new InvalidOperationException(text);}
    static object Check()
    {
        int layouts=0,adjacency=0,seams=0,fallbacks=0,changed=0;
        var legacy=SkyCityModuleReview.CheckSolver();
        try
        {
            Require(legacy.passed,"Default solver regression: "+legacy.error);
            var low=SkyCityWfc.Settings.Default;low.bridgeProbability=0;low.shoreCompleteness=.35f;low.gardenWeight=.2f;low.towerWeight=.2f;low.variantCoherence=1;low.attempts=1;
            var high=SkyCityWfc.Settings.Default;high.bridgeProbability=1;high.shoreCompleteness=1;high.gardenWeight=4;high.towerWeight=4;high.variantCoherence=4;
            int[] gardens=new int[2],towers=new int[2],coherent=new int[2];
            foreach(var settings in new[]{low,high,SkyCityWfc.Settings.Default})foreach(int seed in new[]{93641,2027})
            {
                var grid=new SkyCityWfc.Result[3,3];
                for(int z=0;z<3;z++)for(int x=0;x<3;x++)
                {
                    var c=new SkyCityWfc.Coord(-7+x,12+z);var a=SkyCityWfc.Solve(c,seed,CancellationToken.None,settings);grid[x,z]=a;layouts++;
                    if(a.usedSafeFallback)fallbacks++;
                    Require(a.fingerprint==SkyCityWfc.Solve(c,seed,CancellationToken.None,settings).fingerprint,"Settings lost determinism");
                    Require(a.restarts<settings.attempts,"Retry limit exceeded");
                    for(int j=0;j<8;j++)for(int i=0;i<8;i++)
                    {
                        if(i<7){Require(SkyCityWfc.Compatible(a.states[j*8+i],a.states[j*8+i+1],1),"Local east seam");adjacency++;}
                        if(j<7){Require(SkyCityWfc.Compatible(a.states[j*8+i],a.states[(j+1)*8+i],0),"Local north seam");adjacency++;}
                        Require(!SkyCityWfc.Land(c,i,j,seed,low)||SkyCityWfc.Land(c,i,j,seed,high),"Shore/gate occupancy not monotonic");
                    }
                }
                for(int z=0;z<3;z++)for(int x=0;x<3;x++)for(int i=0;i<8;i++)
                {
                    if(x<2){Require(SkyCityWfc.Compatible(grid[x,z].states[i*8+7],grid[x+1,z].states[i*8],1),"East district seam");seams++;}
                    if(z<2){Require(SkyCityWfc.Compatible(grid[x,z].states[56+i],grid[x,z+1].states[i],0),"North district seam");seams++;}
                }
            }
            // Only weights change in this comparison, so a preference actually
            // reaches module selection rather than merely changing a UI number.
            for(int k=0;k<32;k++)
            {
                var c=new SkyCityWfc.Coord(7+k%8,-12+k/8);ulong first=0;
                int preferred=(int)(SkyCityWfc.Hash((long)Math.Floor(c.x/3.0),(long)Math.Floor(c.z/3.0),93641)%8);
                for(int level=0;level<2;level++)
                {
                    var settings=SkyCityWfc.Settings.Default;settings.gardenWeight=level==0?.2f:4;settings.towerWeight=level==0?.2f:4;settings.variantCoherence=level==0?1:4;
                    var r=SkyCityWfc.Solve(c,93641,CancellationToken.None,settings);layouts++;
                    if(level==0)first=r.fingerprint;else if(first!=r.fingerprint)changed++;
                    foreach(var s in r.states)if(s!=SkyCityWfc.Empty)
                    {
                        int f=s/32;if(f==7||f==8||f==14)gardens[level]++;if(f==6||f==13||f==15)towers[level]++;
                        if(s/4%8==preferred)coherent[level]++;
                    }
                }
            }
            Require(changed>24&&gardens[1]>gardens[0]&&towers[1]>towers[0]&&coherent[1]>coherent[0],"Weights did not affect selection as expected");
            return new{passed=true,legacy,layouts,adjacency,seams,fallbacks,changed,gardens,towers,coherent};
        }
        catch(Exception e){return new{passed=false,legacy,layouts,adjacency,seams,fallbacks,changed,error=e.ToString()};}
    }
}
