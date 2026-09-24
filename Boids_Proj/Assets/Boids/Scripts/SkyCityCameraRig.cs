using UnityEngine;
using UnityEngine.EventSystems;

namespace Boids.Art
{
    /// <summary>Gentle, bounded camera exploration around the authored opening composition.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class SkyCityCameraRig : MonoBehaviour
    {
        public float minimumDistance=32,maximumDistance=95;
        public float orbitDegreesPerPixel=.18f;
        public float smoothing=.20f;
        Vector3 homeTarget,target,targetVelocity,panOffset,lastPointer;
        float homeYaw,homePitch,homeDistance,yaw,pitch,distance;
        float desiredYaw,desiredPitch,desiredDistance,yawVelocity,pitchVelocity,distanceVelocity;
        bool orbiting,panning;
        Camera view;
        public float Distance => distance;
        public Vector3 FocusPoint => target;
        public int OrbitInputs {get;private set;}
        public int ZoomInputs {get;private set;}
        public int PanInputs {get;private set;}
        public int ResetInputs {get;private set;}
        void Awake()
        {
            view=GetComponent<Camera>();homeDistance=62;
            homeTarget=transform.position+transform.forward*homeDistance;
            Vector3 offset=transform.position-homeTarget;
            homeYaw=Mathf.Atan2(-offset.x,-offset.z)*Mathf.Rad2Deg;
            homePitch=Mathf.Atan2(offset.y,new Vector2(offset.x,offset.z).magnitude)*Mathf.Rad2Deg;
            target=homeTarget;yaw=desiredYaw=homeYaw;pitch=desiredPitch=homePitch;distance=desiredDistance=homeDistance;
        }
        void Update()
        {
            bool overUI=EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject();
            bool inView=view.pixelRect.Contains(Input.mousePosition)&&!overUI;
            if(Input.GetMouseButtonDown(1)&&inView){orbiting=true;lastPointer=Input.mousePosition;}
            if(Input.GetMouseButtonDown(2)&&inView){panning=true;lastPointer=Input.mousePosition;}
            if(Input.GetMouseButtonUp(1))orbiting=false;
            if(Input.GetMouseButtonUp(2))panning=false;
            Vector2 delta=Input.mousePosition-lastPointer;lastPointer=Input.mousePosition;
            if(orbiting&&Input.GetMouseButton(1))Orbit(delta);
            if(panning&&Input.GetMouseButton(2))Pan(delta);
            if(inView&&Mathf.Abs(Input.mouseScrollDelta.y)>.001f)Zoom(Input.mouseScrollDelta.y);
            if(Input.GetKeyDown(KeyCode.F)&&!overUI)ResetView();
        }
        void LateUpdate()
        {
            float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);
            yaw=Mathf.SmoothDampAngle(yaw,desiredYaw,ref yawVelocity,smoothing,1000,dt);
            pitch=Mathf.SmoothDampAngle(pitch,desiredPitch,ref pitchVelocity,smoothing,1000,dt);
            distance=Mathf.SmoothDamp(distance,desiredDistance,ref distanceVelocity,smoothing,1000,dt);
            target=Vector3.SmoothDamp(target,homeTarget+panOffset,ref targetVelocity,smoothing,1000,dt);
            transform.position=target+Quaternion.Euler(pitch,yaw,0)*new Vector3(0,0,-distance);
            transform.rotation=Quaternion.LookRotation(target-transform.position,Vector3.up);
        }
        public void Orbit(Vector2 pixels)
        {
            if(pixels.sqrMagnitude<.00001f)return;
            desiredYaw=Mathf.Clamp(desiredYaw+pixels.x*orbitDegreesPerPixel,homeYaw-65,homeYaw+65);
            desiredPitch=Mathf.Clamp(desiredPitch-pixels.y*orbitDegreesPerPixel,2.5f,38);
            OrbitInputs++;
        }
        public void Zoom(float wheelSteps)
        {desiredDistance=Mathf.Clamp(desiredDistance*Mathf.Exp(-wheelSteps*.105f),minimumDistance,maximumDistance);ZoomInputs++;}
        public void Pan(Vector2 pixels)
        {
            if(pixels.sqrMagnitude<.00001f)return;
            float scale=2*distance*Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad)/Mathf.Max(1,view.pixelHeight);
            panOffset-=(transform.right*pixels.x+transform.up*pixels.y)*scale;
            panOffset=new Vector3(Mathf.Clamp(panOffset.x,-5,5),Mathf.Clamp(panOffset.y,-4,4),Mathf.Clamp(panOffset.z,-4,4));
            PanInputs++;
        }
        public void ResetView()
        {desiredYaw=homeYaw;desiredPitch=homePitch;desiredDistance=homeDistance;panOffset=Vector3.zero;ResetInputs++;}
        void OnApplicationFocus(bool focus){if(!focus){orbiting=false;panning=false;}}
        void OnDisable(){orbiting=false;panning=false;}
    }
}
