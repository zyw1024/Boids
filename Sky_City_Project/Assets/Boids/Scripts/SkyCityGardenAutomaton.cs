using System;

namespace Boids.Art
{
    /// <summary>Synchronous, four-state Moore-neighbour automaton. No Unity objects or frame-rate dependence.</summary>
    public sealed class SkyCityGardenAutomaton
    {
        public enum Phase : byte { Rest, Bud, Bloom, Recover }
        public readonly int Width, Height;
        public readonly bool[] Soil;
        public Phase[] Cells { get; private set; }
        public float[] Ages { get; private set; }
        Phase[] next;
        float[] nextAge;
        public int Generation { get; private set; }
        public int BloomCount { get; private set; }
        public int Awakened { get; private set; }
        public SkyCityGardenAutomaton(int width,int height,bool[] soil)
        {
            Width=width;Height=height;Soil=(bool[])soil.Clone();
            if(width<1||height<1||soil.Length!=width*height)throw new ArgumentException("Garden dimensions do not match soil mask.");
            Cells=new Phase[soil.Length];next=new Phase[soil.Length];Ages=new float[soil.Length];nextAge=new float[soil.Length];
        }
        public bool Sow(int x,int y,int radius=1)
        {
            bool changed=false;
            for(int z=Math.Max(0,y-radius);z<=Math.Min(Height-1,y+radius);z++)
                for(int a=Math.Max(0,x-radius);a<=Math.Min(Width-1,x+radius);a++)
                {int i=z*Width+a;if(!Soil[i])continue;Cells[i]=Phase.Bud;Ages[i]=0;changed=true;}
            return changed;
        }
        public void Tick(float dt,int threshold,float budSeconds,float bloomSeconds,float recoverySeconds)
        {
            threshold=Math.Max(1,Math.Min(4,threshold));BloomCount=0;
            for(int y=0;y<Height;y++)for(int x=0;x<Width;x++)
            {
                int i=y*Width+x;var phase=Cells[i];float age=Ages[i]+dt;
                if(!Soil[i]){next[i]=Phase.Rest;nextAge[i]=0;continue;}
                int blooming=0;
                for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                {
                    if(dx==0&&dy==0)continue;int nx=x+dx,ny=y+dy;
                    if(nx>=0&&nx<Width&&ny>=0&&ny<Height&&Cells[ny*Width+nx]==Phase.Bloom)blooming++;
                }
                if(phase==Phase.Rest&&blooming>=threshold){phase=Phase.Bud;age=0;Awakened++;}
                else if(phase==Phase.Bud&&age>=Math.Max(.2f,budSeconds)){phase=Phase.Bloom;age=0;}
                else if(phase==Phase.Bloom&&age>=Math.Max(.2f,bloomSeconds)){phase=Phase.Recover;age=0;}
                else if(phase==Phase.Recover&&age>=Math.Max(.2f,recoverySeconds)){phase=Phase.Rest;age=0;}
                next[i]=phase;nextAge[i]=age;if(phase==Phase.Bloom)BloomCount++;
            }
            var swap=Cells;Cells=next;next=swap;var ages=Ages;Ages=nextAge;nextAge=ages;Generation++;
        }
        public void Clear()
        {Array.Clear(Cells,0,Cells.Length);Array.Clear(Ages,0,Ages.Length);Generation=BloomCount=Awakened=0;}
        public void Restore(byte[] cells,float[] ages,int generation,int blooms,int awakened)
        {
            if(cells==null||ages==null||cells.Length!=Cells.Length||ages.Length!=Ages.Length)throw new ArgumentException("Garden state dimensions changed.");
            for(int i=0;i<cells.Length;i++){if(cells[i]>3||float.IsNaN(ages[i])||ages[i]<0)throw new ArgumentException("Invalid garden state.");Cells[i]=(Phase)cells[i];Ages[i]=ages[i];}
            Generation=generation;BloomCount=blooms;Awakened=awakened;
        }
        public ulong Fingerprint()
        {ulong h=1469598103934665603UL;for(int i=0;i<Cells.Length;i++){h^=(byte)Cells[i];h*=1099511628211UL;h^=(ulong)(Ages[i]*1000);h*=1099511628211UL;}return h;}
    }
}
