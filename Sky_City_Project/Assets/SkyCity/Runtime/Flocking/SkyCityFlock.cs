using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SkyCity.Runtime
{
    /// <summary>Independent flight decisions, local flocking and predictive obstacle steering.</summary>
    public sealed class SkyCityFlock : MonoBehaviour
    {
        [SerializeField] Transform[] birds;
        public float cruiseSpeed=5.3f;
        public float neighbourRadius=5.2f;
        public float separationRadius=1.35f;
        public float steeringLimit=12f;
        [Range(0,8)] public float separationWeight=3.8f;
        [Range(0,2)] public float alignmentWeight=.36f;
        [Range(0,.6f)] public float cohesionWeight=.09f;
        [Range(5,40)] public float callDuration=18;
        [Range(2,12)] public float callRadius=5.1f;
        public bool freeRoaming,acceptPointerInvitations=true;
        public SkyCity.Runtime.WorldGeneration.SkyCityInfiniteWorld world;
        [SerializeField] Vector3[] customObstacleCenters;
        [SerializeField] Vector3[] customObstacleRadii;
        Vector3[] ActiveCenters => customObstacleCenters!=null&&customObstacleRadii!=null&&customObstacleCenters.Length>0&&customObstacleCenters.Length==customObstacleRadii.Length?customObstacleCenters:obstacleCenters;
        Vector3[] ActiveRadii => ActiveCenters==customObstacleCenters?customObstacleRadii:obstacleRadii;
        public void ConfigureObstacles(Vector3[] centers,Vector3[] radii){customObstacleCenters=centers;customObstacleRadii=radii;}
        sealed class Bird
        {
            public Transform body,left,right;
            public Renderer leftRenderer,rightRenderer;
            public Vector3 velocity;
            public float phase,scale,bank,wingClock,effort;
            public int group;
            public MaterialPropertyBlock leftProperties=new MaterialPropertyBlock(),rightProperties=new MaterialPropertyBlock();
        }
        sealed class FlightGroup {public Vector3 goal,center,heading;public float changeAt;}
        Bird[] agents;
        int activeCount;
        readonly int[] groupSizes=new int[4];
        readonly FlightGroup[] groups=new FlightGroup[4];
        Vector3[] positions,velocities;
        Camera view;
        Vector3 invitation;
        Vector3 gardenVisit;
        float gardenVisitUntil;
        float invitedUntil,flightTime;
        readonly System.Random random=new System.Random(641903);
        static readonly Vector3[] obstacleCenters={new Vector3(-8,10,12),new Vector3(12,9,16),new Vector3(5.1f,6.4f,13),new Vector3(-6.5f,0,-2.8f)};
        static readonly Vector3[] obstacleRadii={new Vector3(8.9f,13.5f,8.5f),new Vector3(4.3f,7.5f,4.3f),new Vector3(6.6f,1.5f,2.0f),new Vector3(6.1f,3.1f,4.7f)};
        public int BirdCount => agents==null?(birds==null?0:birds.Length):activeCount;
        public int BirdCapacity => agents==null?0:agents.Length;
        public int WindInvitations {get;private set;}
        public float WingAngle {get;private set;}
        public float PeakTurnRate {get;private set;}
        public float PeakBankAngle {get;private set;}
        public float MinFlightSpeed {get;private set;}=100;
        public float MaxFlightSpeed {get;private set;}
        public int GroupDecisions {get;private set;}
        public int GlidingBirds {get;private set;}
        public bool InvitationActive => agents!=null&&flightTime<invitedUntil;
        public Vector3 InvitationPoint => invitation;
        public float MeanInvitationDistance
        {get{if(agents==null||activeCount==0)return 0;float sum=0;for(int i=0;i<activeCount;i++)sum+=Vector3.Distance(agents[i].body.position,invitation);return sum/activeCount;}}
        public void Configure(Transform[] bodies){birds=bodies;}
        void Start()
        {
            view=Camera.main;activeCount=birds.Length;agents=new Bird[birds.Length];positions=new Vector3[birds.Length];velocities=new Vector3[birds.Length];
            if(world!=null)world.OriginShifted+=ShiftOrigin;
            var firstGoals=new[]{new Vector3(17,6,-5),new Vector3(19,19,22),new Vector3(-2,27,28),new Vector3(-19,21,27)};
            for(int g=0;g<groups.Length;g++)groups[g]=new FlightGroup{goal=firstGoals[g],changeAt=5.0f+g*.9f};
            for(int i=0;i<birds.Length;i++)
            {
                agents[i]=CreateAgent(birds[i],i);
            }
        }
        Bird CreateAgent(Transform body,int index)
        {
            float phase=Mathf.Repeat(index*.618034f,1);
            var left=FindPart(body,"Left Wing");var right=FindPart(body,"Right Wing");
            return new Bird{body=body,left=left,right=right,leftRenderer=left.GetComponent<Renderer>(),rightRenderer=right.GetComponent<Renderer>(),
                velocity=body.forward*cruiseSpeed,phase=phase,scale=body.localScale.x,group=index<64?index/16:index%4,wingClock=phase,effort=1};
        }
        /// <summary>Bounded pool; slider changes reuse birds and keep existing flight state.</summary>
        public void SetBirdCount(int count)
        {
            if(agents==null||agents.Length==0)return;
            count=Mathf.Clamp(count,16,128);
            if(count>agents.Length)
            {
                int previous=agents.Length;
                Array.Resize(ref agents,128);Array.Resize(ref positions,128);Array.Resize(ref velocities,128);
                for(int i=previous;i<128;i++)
                {
                    var body=Instantiate(birds[i%birds.Length],transform);body.name="Pooled swallow "+i;body.gameObject.SetActive(false);
                    agents[i]=CreateAgent(body,i);
                }
            }
            for(int i=activeCount;i<count;i++)
            {
                var a=agents[i];float angle=i*2.39996f;
                a.body.position=view.transform.position+view.transform.forward*18+new Vector3(Mathf.Cos(angle)*10,4+Mathf.Sin(i)*5,Mathf.Sin(angle)*10);
                a.velocity=view.transform.forward*cruiseSpeed;a.body.gameObject.SetActive(true);
            }
            for(int i=count;i<activeCount;i++)agents[i].body.gameObject.SetActive(false);
            activeCount=count;
        }
        static Transform FindPart(Transform root,string part)
        {foreach(var t in root.GetComponentsInChildren<Transform>())if(t.name.Contains(part))return t;return null;}
        void Update()
        {
            if(agents==null)return;
            if(acceptPointerInvitations&&Input.GetMouseButtonDown(0)&&(EventSystem.current==null||!EventSystem.current.IsPointerOverGameObject()))InviteScreenPoint(Input.mousePosition);
            float dt=Mathf.Min(Time.deltaTime,.05f);if(dt<=0)return;flightTime+=dt;
            for(int g=0;g<4;g++){groups[g].center=Vector3.zero;groups[g].heading=Vector3.zero;groupSizes[g]=0;}
            for(int i=0;i<activeCount;i++)
            {
                if(freeRoaming&&(agents[i].body.position-view.transform.position).sqrMagnitude>180*180)
                {
                    var vp=view.WorldToViewportPoint(agents[i].body.position);
                    if(vp.z<0||vp.x<0||vp.x>1||vp.y<0||vp.y>1)
                        agents[i].body.position=view.transform.position-view.transform.forward*45+view.transform.right*((i%16-7.5f)*2)+Vector3.up*(4+agents[i].group*3);
                }
                positions[i]=agents[i].body.position;groups[agents[i].group].center+=positions[i];
                groups[agents[i].group].heading+=agents[i].velocity;groupSizes[agents[i].group]++;
            }
            for(int g=0;g<4;g++)
            {
                if(groupSizes[g]>0){groups[g].center/=groupSizes[g];groups[g].heading/=groupSizes[g];}
                if(flightTime>groups[g].changeAt||Vector3.Distance(groups[g].center,groups[g].goal)<4.0f)ChooseGoal(groups[g],g);
            }
            bool invited=flightTime<invitedUntil;
            for(int i=0;i<activeCount;i++)
            {
                var a=agents[i];var p=positions[i];Vector3 separation=Vector3.zero,alignment=Vector3.zero,cohesion=Vector3.zero;int count=0;
                for(int j=0;j<activeCount;j++)
                {
                    if(i==j)continue;var offset=p-positions[j];float d2=offset.sqrMagnitude;
                    if(d2>.0001f&&d2<neighbourRadius*neighbourRadius)
                    {
                        float personalSpace=separationRadius*(.68f+.40f*(a.scale+agents[j].scale));
                        if(d2<personalSpace*personalSpace)separation+=offset/Mathf.Max(.09f,d2)*(personalSpace-Mathf.Sqrt(d2));
                        if(agents[j].group==a.group){count++;alignment+=agents[j].velocity;cohesion+=positions[j];}
                    }
                }
                float thermal=Mathf.Sin(flightTime*.45f+a.phase*12);
                var goal=groups[a.group].goal;
                if(!invited&&a.group==0&&flightTime<gardenVisitUntil)
                    goal=gardenVisit+new Vector3(Mathf.Cos(flightTime*.5f+a.phase*6)*5,4+Mathf.Sin(a.phase*8)*1.5f,Mathf.Sin(flightTime*.5f+a.phase*6)*5);
                var wander=new Vector3(Mathf.PerlinNoise(a.phase*19,flightTime*.28f)-.5f,Mathf.PerlinNoise(a.phase*31,flightTime*.31f)-.5f,Mathf.PerlinNoise(a.phase*43,flightTime*.23f)-.5f)*5;
                if(invited)
                {
                    float theta=flightTime*.65f+a.phase*6.28318f;
                    float radius=freeRoaming?callRadius*(.627451f+a.phase*.745098f):5.5f;
                    goal=invitation+new Vector3(Mathf.Cos(theta)*radius,Mathf.Sin(theta*.7f)*2.3f,Mathf.Sin(theta)*radius);
                }
                float desiredSpeed=cruiseSpeed*(.83f+a.phase*.29f)+Mathf.Max(0,-a.velocity.y)*.18f+Mathf.Max(0,thermal)*.7f;
                var toGoal=goal+wander-p;var desired=toGoal.normalized*desiredSpeed;
                var force=(desired-a.velocity)*1.45f+separation*separationWeight;
                if(count>0)force+=(alignment/count-a.velocity)*alignmentWeight+(cohesion/count-p)*cohesionWeight;
                force+=ObstacleForce(p,a.velocity);
                if(!freeRoaming)
                {
                var predicted=p+a.velocity*.7f;var viewport=view.WorldToViewportPoint(predicted);
                if(viewport.x<.075f)force+=view.transform.right*(.075f-viewport.x)*100;
                if(viewport.x>.925f)force-=view.transform.right*(viewport.x-.925f)*100;
                if(viewport.y<.09f)force+=view.transform.up*(.09f-viewport.y)*100;
                if(viewport.y>.93f)force-=view.transform.up*(viewport.y-.93f)*100;
                if(p.z<-17)force.z+=(-17-p.z)*3;if(p.z>43)force.z-=(p.z-43)*3;
                if(p.y<.8f)force.y+=(.8f-p.y)*4;if(p.y>28)force.y-=(p.y-28)*4;
                }
                else
                {
                    Vector3 following=view.transform.position-p;
                    if(following.sqrMagnitude>85*85)force+=following.normalized*8;
                    if(p.y<-18)force.y+=(-18-p.y)*4;if(p.y>135)force.y-=(p.y-135)*4;
                }
                var v=a.velocity+Vector3.ClampMagnitude(force,steeringLimit)*dt;
                float speed=Mathf.Clamp(v.magnitude,cruiseSpeed*.60f,cruiseSpeed*1.6f);
                var direction=Vector3.RotateTowards(a.velocity.normalized,v.normalized,Mathf.Deg2Rad*(85+a.phase*42)*dt,0);
                velocities[i]=direction*speed;
            }
            GlidingBirds=0;
            for(int i=0;i<activeCount;i++)
            {
                var a=agents[i];var prior=a.velocity;a.velocity=velocities[i];
                Vector3 next=a.body.position+a.velocity*dt;RaycastHit contact;
                if(freeRoaming&&Physics.Linecast(a.body.position,next,out contact,1,QueryTriggerInteraction.Ignore))
                {next=contact.point+contact.normal*.5f;a.velocity=Vector3.Reflect(a.velocity,contact.normal);}
                a.body.position=next;
                float yaw=Vector3.SignedAngle(prior,a.velocity,Vector3.up)/dt;
                float turn=Vector3.Angle(prior,a.velocity)/dt;
                float desiredBank=-Mathf.Atan2(yaw*Mathf.Deg2Rad*a.velocity.magnitude,9.81f)*Mathf.Rad2Deg;
                a.bank=Mathf.Lerp(a.bank,Mathf.Clamp(desiredBank,-66,66),1-Mathf.Exp(-dt*9));
                var rotation=Quaternion.LookRotation(a.velocity.normalized,Vector3.up)*Quaternion.AngleAxis(a.bank,Vector3.forward);
                a.body.rotation=Quaternion.Slerp(a.body.rotation,rotation,1-Mathf.Exp(-dt*12));
                bool beating=Mathf.Repeat(flightTime+a.phase*11,6.7f)<3.5f||a.velocity.y>1.3f||turn>40;
                if(!beating)GlidingBirds++;
                a.effort=Mathf.Lerp(a.effort,beating?1:.06f,1-Mathf.Exp(-dt*7));
                a.wingClock+=dt*(2.8f+a.phase*.7f+Mathf.Max(0,a.velocity.y)*.16f);
                float cycle=Mathf.Repeat(a.wingClock,1);
                float wing=cycle<.38f?Mathf.Lerp(42,-54,Mathf.SmoothStep(0,1,cycle/.38f)):Mathf.Lerp(-54,42,Mathf.SmoothStep(0,1,(cycle-.38f)/.62f));
                wing=Mathf.Lerp(7+Mathf.Sin(flightTime*2+a.phase*9)*2,wing,a.effort);
                float fold=(.5f+.5f*Mathf.Sin(cycle*6.28318f-1))*a.effort;
                a.left.localRotation=Quaternion.Euler(0,-fold*13,-wing+a.bank*.09f);
                a.right.localRotation=Quaternion.Euler(0,fold*13,wing+a.bank*.09f);
                WingProperties(a.leftRenderer,a.leftProperties,-1,cycle,a.effort,fold);
                WingProperties(a.rightRenderer,a.rightProperties,1,cycle,a.effort,fold);
                if(i==0)WingAngle=wing;
                PeakTurnRate=Mathf.Max(PeakTurnRate,turn);PeakBankAngle=Mathf.Max(PeakBankAngle,Mathf.Abs(a.bank));
                MinFlightSpeed=Mathf.Min(MinFlightSpeed,a.velocity.magnitude);MaxFlightSpeed=Mathf.Max(MaxFlightSpeed,a.velocity.magnitude);
            }
        }
        static void WingProperties(Renderer renderer,MaterialPropertyBlock block,float sign,float cycle,float effort,float fold)
        {
            block.SetFloat("_BirdWing",sign);block.SetFloat("_WingPhase",cycle*6.28318f);block.SetFloat("_WingEffort",effort);block.SetFloat("_WingFold",fold);renderer.SetPropertyBlock(block);
        }
        void ChooseGoal(FlightGroup group,int index)
        {
            if(freeRoaming)
            {
                float angle=(float)random.NextDouble()*Mathf.PI*2;
                group.goal=view.transform.position+view.transform.forward*24+new Vector3(Mathf.Cos(angle)*24,3+index*2,Mathf.Sin(angle)*24);
                group.changeAt=flightTime+4+(float)random.NextDouble()*3;GroupDecisions++;return;
            }
            Vector3 best=group.goal;
            for(int attempt=0;attempt<24;attempt++)
            {
                float angle=(float)(random.NextDouble()*165-82.5);if(Mathf.Abs(angle)<25)angle+=35;
                var heading=group.heading.sqrMagnitude>.01f?group.heading.normalized:Vector3.forward;
                var direction=Quaternion.Euler(0,angle,0)*heading;
                var p=group.center+direction*(14+(float)random.NextDouble()*13);
                p.y=Mathf.Clamp(group.center.y+(float)random.NextDouble()*17-8,2.0f,27);
                p.x=Mathf.Clamp(p.x,-22,23);p.z=Mathf.Clamp(p.z,-15,42);
                var vp=view.WorldToViewportPoint(p);
                if(vp.z>0&&vp.x>.10f&&vp.x<.90f&&vp.y>.12f&&vp.y<.90f&&ActiveClearance(p)>1.12f){best=p;break;}
                best=new Vector3(9+index*2,18+index*2,5+index*8);
            }
            group.goal=best;group.changeAt=flightTime+4.5f+(float)random.NextDouble()*4;GroupDecisions++;
        }
        Vector3 ObstacleForce(Vector3 p,Vector3 velocity)
        {
            if(freeRoaming)
            {
                RaycastHit hit;
                if(Physics.SphereCast(p,.48f,velocity.normalized,out hit,3+velocity.magnitude*.9f,1,QueryTriggerInteraction.Ignore))
                    return (hit.normal+Vector3.up*.35f)*35;
                return Vector3.zero;
            }
            Vector3 force=Vector3.zero;
            var centers=ActiveCenters;var radii=ActiveRadii;
            for(int j=0;j<centers.Length;j++)
            {
                var radius=radii[j];var inverse=new Vector3(1/Mathf.Max(.1f,radius.x),1/Mathf.Max(.1f,radius.y),1/Mathf.Max(.1f,radius.z));
                var d=p-centers[j];var look=d+velocity*.8f;
                float q=Vector3.Scale(d,inverse).magnitude;float predicted=Vector3.Scale(look,inverse).magnitude;
                float near=Mathf.Min(q,predicted);
                if(near<1.32f)
                {
                    var gradient=Vector3.Scale(look,new Vector3(inverse.x*inverse.x,inverse.y*inverse.y,inverse.z*inverse.z)).normalized;
                    force+=gradient*(1.32f-near)*32;
                }
            }
            return force;
        }
        public static float Clearance(Vector3 p)
        {
            float result=100;
            for(int j=0;j<obstacleCenters.Length;j++){var r=obstacleRadii[j];result=Mathf.Min(result,Vector3.Scale(p-obstacleCenters[j],new Vector3(1/r.x,1/r.y,1/r.z)).magnitude);}
            return result;
        }
        float ActiveClearance(Vector3 p)
        {
            float result=100;var centers=ActiveCenters;var radii=ActiveRadii;
            for(int j=0;j<centers.Length;j++){var r=radii[j];result=Mathf.Min(result,Vector3.Scale(p-centers[j],new Vector3(1/Mathf.Max(.1f,r.x),1/Mathf.Max(.1f,r.y),1/Mathf.Max(.1f,r.z))).magnitude);}
            return result;
        }
        // Initial art direction only; live birds do not track this curve.
        public static Vector3 Route(float progress)
        {
            float q=Mathf.Repeat(progress,1);Vector3 a,b,c,d;float t;
            if(q<.6f){t=q/.6f;a=new Vector3(-14.5f,1.5f,-13);b=new Vector3(3,-2,-5);c=new Vector3(27,17,15);d=new Vector3(6,23,36);}
            else{t=(q-.6f)/.4f;a=new Vector3(6,23,36);b=new Vector3(-15,29,42);c=new Vector3(-25,11,-16);d=new Vector3(-14.5f,1.5f,-13);}
            float s=1-t;return s*s*s*a+3*s*s*t*b+3*s*t*t*c+t*t*t*d;
        }
        public bool InviteScreenPoint(Vector2 screenPoint)
        {
            if(view==null||!view.pixelRect.Contains(screenPoint))return false;
            var plane=new Plane(Vector3.forward,new Vector3(0,0,2));float distance;var ray=view.ScreenPointToRay(screenPoint);
            if(!plane.Raycast(ray,out distance))return false;var p=ray.GetPoint(distance);
            if(p.x<1||p.x>21||p.y<0||p.y>24)return false;
            invitation=p;invitedUntil=flightTime+7;WindInvitations++;return true;
        }
        public bool Summon(Camera caller)
        {
            if(agents==null||caller==null)return false;
            var ray=new Ray(caller.transform.position,caller.transform.forward);RaycastHit hit;float distance=14;
            if(Physics.SphereCast(ray,2.8f,out hit,20,1,QueryTriggerInteraction.Ignore))distance=Mathf.Clamp(hit.distance-4,3,14);
            invitation=ray.GetPoint(distance)+Vector3.up*1.5f;invitedUntil=flightTime+callDuration;WindInvitations++;return true;
        }
        public void SetCallDuration(float seconds)
        {
            // Preserve elapsed time when the live slider changes an existing invitation.
            if(InvitationActive)invitedUntil+=seconds-callDuration;
            callDuration=seconds;
        }
        public void VisitGarden(Vector3 point)
        {
            if(view==null||(point-view.transform.position).sqrMagnitude>80*80)return;
            gardenVisit=point;gardenVisitUntil=flightTime+14;
        }
        void ShiftOrigin(Vector3 shift)
        {
            invitation-=shift;
            gardenVisit-=shift;
            if(agents!=null)foreach(var a in agents)a.body.position-=shift;
            foreach(var g in groups)if(g!=null){g.goal-=shift;g.center-=shift;}
        }
        void OnDestroy(){if(world!=null)world.OriginShifted-=ShiftOrigin;}
    }
}
