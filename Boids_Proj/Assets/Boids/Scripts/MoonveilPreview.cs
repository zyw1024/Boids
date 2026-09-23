using UnityEngine;

namespace Boids.Art
{
    /// <summary>Small art review scene: four motions and an orbit camera.</summary>
    public sealed class MoonveilPreview : MonoBehaviour
    {
        public MoonveilMotion fish;
        public Camera previewCamera;
        public int selection = 1;
        private readonly string[] labels = { "01   HOVER", "02   SWIM", "03   DART", "04   FEED" };
        private float yaw = 64f, pitch = 15f, distance = 1.7f;
        private float nextFeed;
        private GUIStyle titleStyle, captionStyle, buttonStyle, noteStyle;

        private void Start()
        {
            // Keep this art preview animating while the editor or MCP client loses focus.
            Application.runInBackground = true;
            Select(selection);
        }
        private void Update()
        {
            for (int i = 0; i < 4; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) Select(i);
            bool overControls = Input.mousePosition.y < 108;
            if (Input.GetMouseButton(0) && !overControls)
            {
                yaw += Input.GetAxis("Mouse X") * 3f;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 2f, -65f, 65f);
            }
            distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * .12f, .8f, 3.2f);
            if (previewCamera != null)
            {
                Vector3 center = new Vector3(0, 0, -.06f);
                previewCamera.transform.position = center + Quaternion.Euler(-pitch, yaw, 0) * new Vector3(0, 0, distance);
                previewCamera.transform.LookAt(center);
                previewCamera.orthographicSize = distance * .20f;
            }
            if (selection == 3 && Time.time >= nextFeed)
            {
                fish.Feed(); nextFeed = Time.time + 3.2f;
            }
        }
        public void Select(int index)
        {
            selection = Mathf.Clamp(index, 0, 3);
            if (fish == null || fish.Animator == null) return;
            fish.Animator.ResetTrigger("Feed");
            fish.Animator.SetFloat("Speed", selection == 0 ? 0 : selection == 2 ? 1 : .45f);
            if (selection == 3) { fish.Feed(); nextFeed = Time.time + 3.2f; }
            else fish.Animator.CrossFadeInFixedTime("Locomotion", .2f);
        }
        private void OnGUI()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
                titleStyle.normal.textColor = new Color(.72f,.91f,.94f);
                captionStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
                captionStyle.normal.textColor = new Color(.40f,.66f,.74f);
                buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12, padding = new RectOffset(12,12,10,10) };
                noteStyle = new GUIStyle(captionStyle) { alignment = TextAnchor.MiddleRight };
            }
            GUI.Label(new Rect(34,24,450,42),"M O O N V E I L",titleStyle);
            GUI.Label(new Rect(36,66,480,24),"A LITTLE LIGHT, BENEATH THE SURFACE",captionStyle);
            float width = 124, gap = 10, x = (Screen.width - 4*width - 3*gap)/2;
            for (int i=0;i<4;i++)
            {
                GUI.backgroundColor = i == selection ? new Color(.23f,.66f,.69f) : new Color(.14f,.25f,.32f);
                if (GUI.Button(new Rect(x+i*(width+gap),Screen.height-78,width,38),labels[i],buttonStyle)) Select(i);
            }
            GUI.backgroundColor = Color.white;
            GUI.Label(new Rect(Screen.width-380,34,344,24),"DRAG TO ORBIT   /   SCROLL TO ZOOM",noteStyle);
            GUI.Label(new Rect(36,Screen.height-30,500,22),"MOTION STUDY     /     1 - 4 TO SWITCH",captionStyle);
        }
    }
}
