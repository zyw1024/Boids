using UnityEngine;
using UnityEngine.EventSystems;

namespace Boids.Art.Infinite
{
    [RequireComponent(typeof(Camera))]
    public sealed class SkyCityVoyager : MonoBehaviour
    {
        public SkyCityInfiniteWorld world;
        public float speed=24, sensitivity=.16f;
        public bool cruise;
        public bool acceptInput=true;
        Vector3 velocity,lastPointer;
        float yaw,pitch;
        bool dragging,panning;
        Camera view;
        void Start() { view=GetComponent<Camera>();SyncAngles(); }
        public void SyncAngles() { yaw=transform.eulerAngles.y;pitch=Mathf.DeltaAngle(0,transform.eulerAngles.x); }
        void Update()
        {
            if (!acceptInput || world==null || !world.Ready) return;
            bool ui=EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject();
            bool inside=view.pixelRect.Contains(Input.mousePosition) && !ui;
            if (Input.GetMouseButtonDown(1) && inside) {dragging=true;lastPointer=Input.mousePosition;}
            if (Input.GetMouseButtonDown(2) && inside) {panning=true;lastPointer=Input.mousePosition;}
            if (Input.GetMouseButtonUp(1)) dragging=false;
            if (Input.GetMouseButtonUp(2)) panning=false;
            Vector3 delta=Input.mousePosition-lastPointer;lastPointer=Input.mousePosition;
            if(dragging) {yaw+=delta.x*sensitivity;pitch=Mathf.Clamp(pitch-delta.y*sensitivity,-65,78);}
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.Euler(pitch,yaw,0),1-Mathf.Exp(-Time.unscaledDeltaTime*14));
            Vector3 input=Vector3.zero;
            if(!ui)
            {
                if(Input.GetKeyDown(KeyCode.Space))cruise=!cruise;
                if(Input.GetKeyDown(KeyCode.F))ReturnHome();
                if(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow))input.z++;
                if(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow))input.z--;
                if(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow))input.x++;
                if(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow))input.x--;
                if(Input.GetKey(KeyCode.E))input.y++;
                if(Input.GetKey(KeyCode.Q))input.y--;
                if(inside)speed=Mathf.Clamp(speed*Mathf.Exp(Input.mouseScrollDelta.y*.12f),6,65);
            }
            if(cruise)input.z+=.65f;
            Vector3 forward=Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized;
            Vector3 move=(forward*input.z+transform.right*input.x+Vector3.up*input.y);
            if(move.sqrMagnitude>1)move.Normalize();
            float rate=speed*(Input.GetKey(KeyCode.LeftShift)?2:1);
            velocity=Vector3.Lerp(velocity,move*rate,1-Mathf.Exp(-Time.unscaledDeltaTime*5));
            transform.position+=velocity*Mathf.Min(Time.unscaledDeltaTime,.05f);
            if(panning)transform.position-=(transform.right*delta.x+transform.up*delta.y)*.08f;
            Vector3 p=transform.position;p.y=Mathf.Clamp(p.y,40,130);transform.position=p;
        }
        public void ReturnHome()
        {
            cruise=false;velocity=Vector3.zero;
            world.Teleport(new SkyCityWfc.Coord(0,0),new Vector3(80,47,-64));
            transform.rotation=Quaternion.LookRotation(new Vector3(50,27,44)-transform.position);SyncAngles();
        }
        void OnApplicationFocus(bool focused) {if(!focused){dragging=false;panning=false;velocity=Vector3.zero;}}
    }
}
