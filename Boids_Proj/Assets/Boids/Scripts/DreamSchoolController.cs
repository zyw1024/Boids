using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Boids.Art
{
    /// <summary>Readable CPU Boids for a small art scene, with limited steering and consumable food.</summary>
    public sealed class DreamSchoolController : MonoBehaviour
    {
        [SerializeField] Transform[] fish;
        [SerializeField] Material brushMaterial;
        [SerializeField] Texture2D brushAtlas;
        [Header("School")]
        [Range(.2f, 2)] public float cruiseSpeed = .85f;
        public float feedingSpeed = 1.55f;
        public float neighbourRadius = 2.4f;
        public float separationRadius = .62f;
        public float maxAcceleration = 1.7f;
        [Header("Feeding")]
        public float foodDepth = 2.4f;
        public int portionsPerFeeding = 18;
        public int maxFoodClusters = 3;
        public float attractionRadius = 11f;

        sealed class Agent
        {
            public Transform body;
            public MoonveilMotion motion;
            public Vector3 velocity, naturalScale, scatter;
            public float phase, biteAfter, scatterUntil, nextFeedAnimation;
        }
        sealed class Food
        {
            public Vector3 position;
            public GameObject visual, ripple;
            public int remaining;
            public float born;
        }
        Agent[] agents;
        Vector3[] positions, nextVelocities;
        readonly List<Food> foods = new List<Food>();
        Material foodPaint;
        Mesh crumbs, rippleMesh;
        MaterialPropertyBlock block;
        Camera view;
        public int FoodCount => foods.Count;
        public int ConsumedPortions { get; private set; }
        public int CompletedFeedings { get; private set; }
        public int AgentCount => agents == null ? 0 : agents.Length;
        public Vector3 SchoolCenter { get; private set; }

        public void Configure(Transform[] bodies, Material paint, Texture2D atlas)
        {
            fish = bodies; brushMaterial = paint; brushAtlas = atlas;
        }
        void Start()
        {
            view = Camera.main;
            agents = new Agent[fish.Length];
            positions = new Vector3[fish.Length];
            nextVelocities = new Vector3[fish.Length];
            for (int i=0;i<fish.Length;i++)
            {
                float phase = Mathf.Repeat(i*.618034f,1);
                agents[i] = new Agent
                {
                    body=fish[i], motion=fish[i].GetComponent<MoonveilMotion>(),
                    velocity=fish[i].forward*cruiseSpeed, phase=phase,
                    naturalScale=fish[i].localScale / DepthScale(fish[i].position.z)
                };
                var animator=fish[i].GetComponentInChildren<Animator>();
                if(animator!=null)animator.Play("Locomotion",0,phase);
            }
            foodPaint = new Material(brushMaterial) { name="Food pigment (runtime)" };
            foodPaint.SetTexture("_BrushAtlas",brushAtlas);
            foodPaint.SetColor("_Tint",new Color(1.8f,1.12f,.45f));
            foodPaint.SetColor("_Accent",new Color(2.2f,1.65f,.77f));
            foodPaint.SetFloat("_Opacity",.95f);
            crumbs=MakeDabs(false);rippleMesh=MakeDabs(true);
            block=new MaterialPropertyBlock();
        }

        void Update()
        {
            if (agents == null) return;
            if (Input.GetMouseButtonDown(0) &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                TryFeedScreenPoint(Input.mousePosition);
            float dt = Mathf.Min(Time.deltaTime,.04f);
            SchoolCenter = Vector3.zero;
            for (int i=0;i<agents.Length;i++)
            {
                positions[i]=agents[i].body.position;
                SchoolCenter+=positions[i];
            }
            if(agents.Length>0)SchoolCenter/=agents.Length;

            // Snapshot all positions first so the result does not depend on update order.
            for(int i=0;i<agents.Length;i++)
            {
                Agent a=agents[i]; Vector3 p=positions[i];
                Vector3 separation=Vector3.zero,alignment=Vector3.zero,cohesion=Vector3.zero;
                int neighbours=0;
                for(int j=0;j<agents.Length;j++)
                {
                    if(i==j)continue;
                    Vector3 offset=p-positions[j];float distance=offset.sqrMagnitude;
                    if(distance<.00001f || distance>neighbourRadius*neighbourRadius)continue;
                    neighbours++;alignment+=agents[j].velocity;cohesion+=positions[j];
                    if(distance<separationRadius*separationRadius)
                        separation+=offset/Mathf.Max(.04f,distance);
                }
                Vector3 steering=separation*1.35f;
                if(neighbours>0)
                {
                    steering+=(alignment/neighbours-a.velocity)*.7f;
                    steering+=(cohesion/neighbours-p)*.24f;
                }
                Food target=null;float nearest=attractionRadius*attractionRadius;
                foreach(var f in foods)
                {
                    float d=(f.position-p).sqrMagnitude;
                    if(f.remaining>0 && d<nearest){target=f;nearest=d;}
                }
                float speed=cruiseSpeed*(.85f+a.phase*.3f);
                if(Time.time<a.scatterUntil)
                {
                    speed*=1.25f;
                    steering+=(a.scatter*speed-a.velocity)*1.4f;
                }
                else if(target!=null)
                {
                    speed=feedingSpeed;
                    Vector3 approach=target.position-p;
                    float arrival=Mathf.Clamp01(approach.magnitude/1.8f);
                    steering+=(approach.normalized*speed*Mathf.Max(.25f,arrival)-a.velocity)*1.15f;
                    if(nearest<.62f*.62f && Time.time>a.biteAfter && Time.time-target.born>.6f)
                    {
                        a.biteAfter=Time.time+.5f;
                        target.remaining--;ConsumedPortions++;
                        if(a.motion!=null && Time.time>a.nextFeedAnimation)
                        {
                            a.motion.Feed();a.nextFeedAnimation=Time.time+2.3f;
                        }
                        if(target.remaining==0)
                        {
                            CompletedFeedings++;
                            for(int j=0;j<agents.Length;j++)
                            {
                                Vector3 away=positions[j]-target.position;
                                if(away.sqrMagnitude>36)continue;
                                away+=new Vector3(Mathf.Sin(j*2.4f),Mathf.Cos(j*2.4f)*.6f,Mathf.Sin(j))*.5f;
                                agents[j].scatter=away.normalized;
                                agents[j].scatterUntil=Time.time+3.5f;
                            }
                        }
                    }
                }
                else
                {
                    float phase=Time.time*.045f + a.phase*.65f;
                    var goal=new Vector3(Mathf.Sin(phase)*5.5f,6.6f+Mathf.Sin(phase*1.3f)*2.0f,
                        5.3f+Mathf.Cos(phase)*5.0f);
                    steering+=((goal-p).normalized*speed-a.velocity)*.45f;
                }
                steering+=BoundsForce(p);
                // Broad foliage volumes give the school room to turn before the near leaves.
                steering+=Avoid(p,new Vector3(-7.6f,8,0),new Vector3(2.6f,3.3f,2.0f));
                steering+=Avoid(p,new Vector3(8.6f,4,0),new Vector3(1.7f,4,2.5f));
                Vector3 velocity=a.velocity+Vector3.ClampMagnitude(steering,maxAcceleration)*dt;
                float limit=target==null?speed*1.2f:feedingSpeed;
                velocity=Vector3.ClampMagnitude(velocity,limit);
                if(velocity.magnitude<.22f)velocity=a.body.forward*.22f;
                nextVelocities[i]=velocity;
            }
            for(int i=0;i<agents.Length;i++)
            {
                Agent a=agents[i];a.velocity=nextVelocities[i];
                a.body.position+=a.velocity*dt;
                var facing=Quaternion.LookRotation(a.velocity.normalized,Vector3.up);
                a.body.rotation=Quaternion.Slerp(a.body.rotation,facing,1-Mathf.Exp(-dt*2.8f));
                a.body.localScale=a.naturalScale*DepthScale(a.body.position.z);
                if(a.motion!=null)a.motion.SetSwimSpeed(Mathf.Lerp(.15f,.62f,Mathf.Clamp01(a.velocity.magnitude/feedingSpeed)));
            }
            UpdateFood();
        }

        static float DepthScale(float z) => Mathf.Lerp(1,.42f,Mathf.InverseLerp(-4,18,z));
        static Vector3 BoundsForce(Vector3 p)
        {
            var min=new Vector3(-8.4f,2.0f,-4);
            var max=new Vector3(7.8f,11.7f,18);
            Vector3 push=Vector3.zero;
            for(int k=0;k<3;k++)
            {
                if(p[k]<min[k]+1.4f)push[k]+=(min[k]+1.4f-p[k])*1.1f;
                if(p[k]>max[k]-1.4f)push[k]-=(p[k]-max[k]+1.4f)*1.1f;
            }
            return push;
        }
        static Vector3 Avoid(Vector3 p,Vector3 center,Vector3 radii)
        {
            Vector3 d=p-center;
            float q=Vector3.Scale(d,new Vector3(1/radii.x,1/radii.y,1/radii.z)).magnitude;
            return q<1.3f?d.normalized*(1.3f-q)*2.2f:Vector3.zero;
        }
        public bool TryFeedScreenPoint(Vector2 screenPoint)
        {
            if(view==null || !view.pixelRect.Contains(screenPoint))return false;
            var plane=new Plane(Vector3.forward,new Vector3(0,0,foodDepth));
            Ray ray=view.ScreenPointToRay(screenPoint);
            float distance;
            if(!plane.Raycast(ray,out distance))return false;
            var point=ray.GetPoint(distance);
            if(point.x < -8.4f || point.x>7.8f || point.y<2 || point.y>11.7f)return false;
            return DropFood(point);
        }
        public bool DropFood(Vector3 point)
        {
            if(agents==null)return false;
            if(foods.Count>=Mathf.Max(1,maxFoodClusters))RemoveFood(0);
            var food=new Food{position=point,remaining=Mathf.Max(1,portionsPerFeeding),born=Time.time};
            food.visual=FoodObject("Golden food",crumbs,point);
            food.ripple=FoodObject("Feeding brush ripple",rippleMesh,point+Vector3.back*.03f);
            foods.Add(food);return true;
        }
        public float MeanDistanceTo(Vector3 point)
        {
            if(agents==null || agents.Length==0)return 0;
            float distance=0;foreach(var a in agents)distance+=Vector3.Distance(a.body.position,point);
            return distance/agents.Length;
        }
        GameObject FoodObject(string label,Mesh mesh,Vector3 point)
        {
            var go=new GameObject(label);go.transform.SetParent(transform,false);go.transform.position=point;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=foodPaint;
            r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
            return go;
        }
        void UpdateFood()
        {
            for(int i=foods.Count-1;i>=0;i--)
            {
                var f=foods[i];float age=Time.time-f.born;
                if(f.remaining<=0 || age>35){RemoveFood(i);continue;}
                float size=.55f+.45f*Mathf.Sqrt((float)f.remaining/Mathf.Max(1,portionsPerFeeding));
                f.visual.transform.localScale=Vector3.one*size;
                f.visual.transform.position=f.position+Vector3.up*Mathf.Sin(age*1.3f)*.055f;
                f.visual.transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(age)*9);
                f.ripple.transform.localScale=Vector3.one*(1+age*.8f);
                block.SetFloat("_Opacity",Mathf.Max(0,.55f-age*.65f));
                f.ripple.GetComponent<Renderer>().SetPropertyBlock(block);
                if(age>.9f)f.ripple.SetActive(false);
            }
        }
        void RemoveFood(int index)
        {
            Destroy(foods[index].visual);Destroy(foods[index].ripple);foods.RemoveAt(index);
        }
        static Mesh MakeDabs(bool ring)
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
            int count=ring?20:9;
            for(int i=0;i<count;i++)
            {
                float angle=i*(ring?Mathf.PI*2/count:2.39996f);
                float radius=ring?.46f:.19f*Mathf.Sqrt((i+1f)/count);
                Vector3 center=new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0);
                float size=ring?.052f:.048f;
                Vector3 right=Vector3.right*size,up=Vector3.up*size*.65f;
                int k=v.Count;
                v.Add(center-right-up);v.Add(center-right+up);v.Add(center+right+up);v.Add(center+right-up);
                var tile=new Vector2(i%4,(i/4)%4)*.25f;
                uv.Add(tile);uv.Add(tile+new Vector2(0,.25f));uv.Add(tile+Vector2.one*.25f);uv.Add(tile+new Vector2(.25f,0));
                for(int j=0;j<4;j++)colors.Add(new Color(.7f,.8f,1,1));
                triangles.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
            }
            var mesh=new Mesh{name=ring?"Food brush ripple":"Food pigment crumbs"};
            mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            return mesh;
        }
        void OnDestroy()
        {
            if(foodPaint!=null)Destroy(foodPaint);
            if(crumbs!=null)Destroy(crumbs);
            if(rippleMesh!=null)Destroy(rippleMesh);
        }
    }
}
