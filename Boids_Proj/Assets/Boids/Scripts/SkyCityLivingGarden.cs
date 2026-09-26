using UnityEngine;

namespace Boids.Art
{
    /// <summary>One authored planting surface. State survives distance culling and floating-origin shifts.</summary>
    public sealed class SkyCityLivingGarden : MonoBehaviour
    {
        public int columns=64,rows=3,seed=1307;
        public Vector2 size=new Vector2(20,.65f);
        public float bedSpacing=1.85f,bedWidth=1.5f;
        public bool continuous;
        public Renderer flowers;
        public Renderer rules;
        public SkyCityGardenAutomaton Simulation {get;private set;}
        public bool Simulating {get;private set;}
        public Texture2D StateTexture {get;private set;}
        public int SeedsPlanted {get;private set;}
        MaterialPropertyBlock properties;
        Color[] pixels;
        Color[] phases;
        Texture2D phaseTexture;
        float accumulator;
        float windTime;
        int windSequence;
        SkyCityGardenDirector director;
        public bool SoilAt(int x,int y)
        {
            float u=(x+.5f)*size.x/columns;
            return continuous||Mathf.Abs(Mathf.Repeat(u,bedSpacing)-bedSpacing*.5f)<bedWidth*.5f;
        }
        void Start()
        {
            director=GetComponentInParent<SkyCityGardenDirector>();
            var mask=new bool[columns*rows];for(int y=0;y<rows;y++)for(int x=0;x<columns;x++)mask[y*columns+x]=SoilAt(x,y);
            Simulation=new SkyCityGardenAutomaton(columns,rows,mask);
            StateTexture=new Texture2D(columns,rows,TextureFormat.RGBA32,false,true){name=name+" CA state",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            pixels=new Color[mask.Length];phases=new Color[mask.Length];properties=new MaterialPropertyBlock();
            phaseTexture=new Texture2D(columns,rows,TextureFormat.RGBA32,false,true){name=name+" CA phases",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            // A small initial seed demonstrates a travelling bloom, rather than a static full carpet.
            Simulation.Sow(Mathf.Min(columns-1,3),rows/2,1);Publish(true);
        }
        void Update()
        {
            if(Simulation==null||director==null||director.player==null)return;
            Simulating=(director.player.view.transform.position-transform.position).sqrMagnitude<100*100;
            if(!Simulating){accumulator=0;return;}
            accumulator+=Mathf.Min(Time.deltaTime,.1f)*director.evolutionSpeed;
            if(accumulator<.2f)return;
            accumulator-=.2f;
            Simulation.Tick(.2f,director.neighbours,director.budSeconds,director.bloomSeconds,director.recoverySeconds);Publish(false);
            if(director.windSeeds)
            {
                windTime+=.2f;
                if(windTime>3.8f)
                {
                    windTime=0;int start=(int)((uint)(seed+unchecked(++windSequence*73856093))%(uint)Simulation.Cells.Length);
                    for(int j=0;j<Simulation.Cells.Length;j++)
                    {int i=(start+j)%Simulation.Cells.Length;if(Simulation.Soil[i]&&Simulation.Cells[i]==SkyCityGardenAutomaton.Phase.Rest){Simulation.Sow(i%columns,i/columns,0);break;}}
                }
            }
        }
        void OnDisable(){Simulating=false;accumulator=0;}
        void Publish(bool immediate)
        {
            for(int i=0;i<pixels.Length;i++)
            {
                var phase=Simulation.Cells[i];float age=Simulation.Ages[i],growth=.22f,bloom=0;
                if(phase==SkyCityGardenAutomaton.Phase.Bud){growth=Mathf.Lerp(.25f,1,age/Mathf.Max(.2f,director.budSeconds));bloom=Mathf.Clamp01(age/director.budSeconds-.45f)*1.2f;}
                else if(phase==SkyCityGardenAutomaton.Phase.Bloom){growth=1;bloom=1;}
                else if(phase==SkyCityGardenAutomaton.Phase.Recover){bloom=1-Mathf.Clamp01(age/3);growth=Mathf.Lerp(1,.22f,age/Mathf.Max(1,director.recoverySeconds));}
                var previous=pixels[i];pixels[i]=new Color(growth,bloom,immediate?growth:previous.r,immediate?bloom:previous.g);
                phases[i]=new Color((byte)phase/3f,0,0,1);
            }
            StateTexture.SetPixels(pixels);StateTexture.Apply(false,false);
            phaseTexture.SetPixels(phases);phaseTexture.Apply(false,false);
            properties.SetTexture("_StateTex",StateTexture);properties.SetFloat("_StateTime",Time.time);
            properties.SetTexture("_Phases",phaseTexture);
            properties.SetFloat("_Transition",immediate?.001f:.2f/Mathf.Max(.1f,director.evolutionSpeed));
            properties.SetFloat("_Breeze",director.breeze);properties.SetFloat("_Rules",0);
            flowers.SetPropertyBlock(properties);
            if(rules!=null){rules.enabled=director.showRules;properties.SetFloat("_Rules",2);rules.SetPropertyBlock(properties);}
        }
        public bool SowRay(Ray ray,out Vector3 point)
        {
            point=Vector3.zero;float distance;var plane=new Plane(transform.up,transform.position);
            if(!plane.Raycast(ray,out distance)||distance>45)return false;
            point=ray.GetPoint(distance);Vector3 p=transform.InverseTransformPoint(point);
            if(Mathf.Abs(p.x)>size.x*.5f||Mathf.Abs(p.z)>size.y*.5f)return false;
            int x=Mathf.Clamp((int)((p.x/size.x+.5f)*columns),0,columns-1),y=Mathf.Clamp((int)((p.z/size.y+.5f)*rows),0,rows-1);
            if(Simulation==null||!Simulation.Soil[y*columns+x])return false;
            // Reject planting through walls, roofs or the opposite side of the island.
            RaycastHit hit;if(Physics.Raycast(ray,out hit,distance-.15f,1,QueryTriggerInteraction.Ignore))return false;
            return Sow(x,y);
        }
        public bool Sow(int x,int y)
        {if(Simulation==null||!Simulation.Sow(x,y))return false;SeedsPlanted++;Publish(false);return true;}
        public void ResetGarden(){if(Simulation==null)return;Simulation.Clear();Publish(true);}
        public void RefreshAppearance(){if(Simulation!=null)Publish(true);}
        void OnDestroy(){if(StateTexture!=null)Destroy(StateTexture);if(phaseTexture!=null)Destroy(phaseTexture);}
    }
}
