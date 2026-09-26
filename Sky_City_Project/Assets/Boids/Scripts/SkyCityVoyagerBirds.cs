using UnityEngine;
using UnityEngine.Rendering;

namespace Boids.Art.Infinite
{
    public sealed class SkyCityVoyagerBirds : MonoBehaviour
    {
        public Camera view;
        public SkyCityInfiniteWorld world;
        public Mesh birdMesh;
        public Material material;
        const int Count=48;
        readonly Vector3[] positions=new Vector3[Count], velocities=new Vector3[Count];
        readonly Matrix4x4[] transforms=new Matrix4x4[Count];
        Vector3 lastCamera;
        void Start()
        {
            lastCamera=view.transform.position;
            for(int i=0;i<Count;i++)
            {
                float a=i*2.39996f;
                positions[i]=lastCamera+view.transform.forward*55+new Vector3(Mathf.Cos(a)*22,Mathf.Sin(i*.7f)*7-8,Mathf.Sin(a)*22);
                velocities[i]=view.transform.forward*10;
            }
        }
        void Update()
        {
            if(view==null||birdMesh==null||material==null)return;
            Vector3 cameraDelta=view.transform.position-lastCamera;lastCamera=view.transform.position;
            // Rebases and teleports preserve the local flock without retaining old-world birds.
            if(cameraDelta.sqrMagnitude>10000)for(int i=0;i<Count;i++)positions[i]+=cameraDelta;
            float dt=Mathf.Min(Time.deltaTime,.04f),time=Time.time;
            Vector3 center=view.transform.position+view.transform.forward*65;center.y=Mathf.Max(48,view.transform.position.y-8);
            for(int i=0;i<Count;i++)
            {
                int group=i/16;float phase=time*.10f+group*2.09f;
                Vector3 target=center+new Vector3(Mathf.Cos(phase)*28,Mathf.Sin(phase*1.7f)*5,Mathf.Sin(phase)*21);
                Vector3 steer=(target-positions[i])*.17f;
                if(world!=null)
                {
                    Vector3 ahead=positions[i]+velocities[i]*1.4f;
                    int dx=Mathf.RoundToInt((ahead.x-42)/SkyCityWfc.ChunkSize),dz=Mathf.RoundToInt((ahead.z-42)/SkyCityWfc.ChunkSize);
                    int type=SkyCityWfc.Composition(new SkyCityWfc.Coord(world.OriginX+dx,world.OriginZ+dz),world.seed);
                    float top=SkyCityWfc.Elevation(type)+(type==4?56:type==0||type==1||type==3?38:20);
                    Vector3 away=ahead-new Vector3(dx*SkyCityWfc.ChunkSize+36,ahead.y,dz*SkyCityWfc.ChunkSize+48);
                    if(away.sqrMagnitude<32*32&&ahead.y<top+5)
                        steer+=away.normalized*12+Vector3.up*9;
                }
                for(int j=0;j<Count;j++)
                {
                    if(j==i)continue;Vector3 separation=positions[i]-positions[j];float distance=separation.sqrMagnitude;
                    if(distance<12 && distance>.01f)steer+=separation*(2.5f/distance);
                    if(j/16==group&&distance<180)steer+=(velocities[j]-velocities[i])*.006f;
                }
                steer.y+=Mathf.Sin(time*.7f+i*1.27f)*.35f;
                Vector3 before=velocities[i];Vector3 desired=steer.normalized*(8+2*Mathf.Sin(time*.8f+i));
                velocities[i]=Vector3.RotateTowards(before,desired,dt*1.9f,dt*5);
                positions[i]+=velocities[i]*dt;positions[i].y=Mathf.Max(44,positions[i].y);
                float bank=Mathf.Clamp(Vector3.SignedAngle(before,velocities[i],Vector3.up)/Mathf.Max(dt,.001f)*-.55f,-55,55);
                transforms[i]=Matrix4x4.TRS(positions[i],Quaternion.LookRotation(velocities[i])*Quaternion.Euler(0,0,bank),Vector3.one*(.48f+(i%7)*.045f));
            }
#pragma warning disable 0618
            Graphics.DrawMeshInstanced(birdMesh,0,material,transforms,Count,null,ShadowCastingMode.Off,false,0,null,LightProbeUsage.Off,null);
#pragma warning restore 0618
        }
    }
}
