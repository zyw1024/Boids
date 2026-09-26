using UnityEngine;
using UnityEngine.EventSystems;
using SkyCity.Runtime.WorldGeneration;

namespace SkyCity.Runtime
{
    /// <summary>Collision-aware first-person flight. The camera never orbits a remote target.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class SkyCityFirstPerson : MonoBehaviour
    {
        public Camera view;
        public SkyCityFlock flock;
        public SkyCityInfiniteWorld world;
        public SkyCityFlockMenu flockMenu;
        public bool acceptInput=true;
        public float speed=9, sensitivity=2, eyeHeight=1.65f;
        public Vector3 homeEye=new Vector3(5,16,-38),homeLook=new Vector3(-8,12,8);
        CharacterController body;
        Vector3 velocity;
        float yaw,pitch,noticeUntil,lookReadyAt;
        bool captured;
        int capturedAt;
        AudioSource whistle;
        AudioClip call;
        public int LookInputs {get;private set;}
        public int MoveInputs {get;private set;}
        public int SpeedInputs {get;private set;}
        public int Calls {get;private set;}
        public int Contacts {get;private set;}
        public bool Captured => captured;
        public Vector3 Velocity => velocity;
        void Awake()
        {
            body=GetComponent<CharacterController>();
            yaw=transform.eulerAngles.y;pitch=Mathf.DeltaAngle(0,view.transform.localEulerAngles.x);
            whistle=gameObject.AddComponent<AudioSource>();whistle.playOnAwake=false;whistle.spatialBlend=0;whistle.volume=.13f;
            const int rate=24000;var samples=new float[rate];
            for(int i=0;i<samples.Length;i++)
            {
                float t=i/(float)rate,window=Mathf.Sin(Mathf.PI*Mathf.Clamp01(t/.9f));
                float phase=2*Mathf.PI*(1350*t+360*t*t-220*t*t*t);
                samples[i]=Mathf.Sin(phase)*window*window*.42f;
            }
            call=AudioClip.Create("A gentle call to the swallows",rate,1,rate,false);call.SetData(samples,0);
        }
        void Start(){if(acceptInput&&!Application.isEditor&&Application.isFocused)CapturePointer(true);}
        void Update()
        {
            if(!acceptInput)return;
            bool ui=EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject();
            if(Input.GetKeyDown(KeyCode.Escape))
            {if(flockMenu!=null)flockMenu.SetOpen(!flockMenu.IsOpen);else CapturePointer(false);return;}
            if(flockMenu!=null&&flockMenu.IsOpen)return;
            if(!captured)
            {
                if(!ui&&view.pixelRect.Contains(Input.mousePosition)&&(Input.GetMouseButtonDown(0)||Input.GetMouseButtonDown(1)))
                {CapturePointer(true);if(Input.GetMouseButtonDown(1))CallBirds();}
                return;
            }
            if(ui)return;
            if(Time.frameCount>capturedAt+2&&Time.unscaledTime>=lookReadyAt)Look(new Vector2(Input.GetAxisRaw("Mouse X"),Input.GetAxisRaw("Mouse Y")));
            if(Mathf.Abs(Input.mouseScrollDelta.y)>.001f)ChangeSpeed(Input.mouseScrollDelta.y);
            var input=new Vector3((Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),
                (Input.GetKey(KeyCode.E)?1:0)-(Input.GetKey(KeyCode.Q)?1:0),
                (Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0));
            Move(input,Time.unscaledDeltaTime,Input.GetKey(KeyCode.LeftShift));
            if(Input.GetMouseButtonDown(1))CallBirds();
            if(Input.GetKeyDown(KeyCode.F))ReturnHome();
        }
        public void Look(Vector2 mouse)
        {
            if(mouse.sqrMagnitude<.000001f)return;
            yaw=Mathf.Repeat(yaw+mouse.x*sensitivity,360);pitch=Mathf.Clamp(pitch-mouse.y*sensitivity,-86,86);
            transform.rotation=Quaternion.Euler(0,yaw,0);view.transform.localRotation=Quaternion.Euler(pitch,0,0);LookInputs++;
        }
        public void ChangeSpeed(float steps){speed=Mathf.Clamp(speed*Mathf.Exp(steps*.16f),2,36);SpeedInputs++;}
        public void Move(Vector3 input,float dt,bool boost=false)
        {
            dt=Mathf.Clamp(dt,0,.05f);input=Vector3.ClampMagnitude(input,1);
            Vector3 desired=(view.transform.forward*input.z+transform.right*input.x+Vector3.up*input.y);
            desired=Vector3.ClampMagnitude(desired,1)*speed*(boost?2:1);
            velocity=Vector3.Lerp(velocity,desired,1-Mathf.Exp(-dt*12));
            if(input.sqrMagnitude>0)MoveInputs++;
            if(body.enabled)body.Move(velocity*dt);
            if(view.transform.position.y<-24||view.transform.position.y>140)
            {
                var eye=view.transform.position;eye.y=Mathf.Clamp(eye.y,-24,140);SetEyePosition(eye);velocity.y=0;
            }
        }
        public void SetPose(Vector3 eye,Quaternion rotation)
        {
            SetEyePosition(eye);yaw=rotation.eulerAngles.y;pitch=Mathf.DeltaAngle(0,rotation.eulerAngles.x);
            transform.rotation=Quaternion.Euler(0,yaw,0);view.transform.localRotation=Quaternion.Euler(pitch,0,0);velocity=Vector3.zero;
        }
        void SetEyePosition(Vector3 eye)
        {
            bool active=body.enabled;body.enabled=false;transform.position=eye-Vector3.up*eyeHeight;body.enabled=active;
        }
        public bool CallBirds()
        {
            if(flockMenu!=null&&flockMenu.IsOpen)return false;
            if(flock==null||!flock.Summon(view))return false;
            Calls++;noticeUntil=Time.unscaledTime+3;whistle.PlayOneShot(call);return true;
        }
        public void ReturnHome()
        {
            if(world!=null)world.Teleport(new SkyCityWfc.Coord(0,0),homeEye);
            SetPose(homeEye,Quaternion.LookRotation(homeLook-homeEye));
        }
        public void CapturePointer(bool value)
        {
            if(value&&flockMenu!=null&&flockMenu.IsOpen)value=false;
            captured=value;capturedAt=Time.frameCount;lookReadyAt=Time.unscaledTime+.15f;
            Cursor.lockState=value?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!value;
            if(value)Input.ResetInputAxes();else velocity=Vector3.zero;
        }
        void OnControllerColliderHit(ControllerColliderHit hit){Contacts++;}
        void OnApplicationFocus(bool focus){if(!focus)CapturePointer(false);}
        void OnDisable(){CapturePointer(false);}
        void OnDestroy(){if(call!=null)Destroy(call);}
        void OnGUI()
        {
            if(!acceptInput||(flockMenu!=null&&flockMenu.IsOpen))return;
            Color previous=GUI.color;GUI.color=new Color(1,.96f,.85f,.7f);
            if(captured)GUI.DrawTexture(new Rect(Screen.width*.5f-1,Screen.height*.5f-1,3,3),Texture2D.whiteTexture);
            var style=new GUIStyle(GUI.skin.label){fontSize=14};style.normal.textColor=new Color(1,.96f,.85f,.88f);
            GUI.Label(new Rect(26,Screen.height-60,1200,28),captured?
                "WASD  fly    Mouse  look    Wheel  speed    Right click  call birds    Q / E  descend / rise    Shift  faster    Esc  flock menu":
                "Click to explore the sky    |    Esc  flock menu    F  return to the gardens    M  music",style);
            if(Time.unscaledTime<noticeUntil)GUI.Label(new Rect(Screen.width*.5f-130,Screen.height*.62f,320,30),"The swallows heard your call",style);
            GUI.color=previous;
        }
    }
}
