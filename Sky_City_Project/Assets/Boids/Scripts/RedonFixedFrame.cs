using UnityEngine;

namespace Boids.Art
{
    /// <summary>Preserves the authored 3:2 composition without changing camera position or lens.</summary>
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class RedonFixedFrame : MonoBehaviour
    {
        public const float Aspect = 1.5f;
        Camera view;
        void OnEnable() { view = GetComponent<Camera>(); Apply(); }
        void LateUpdate() => Apply();
        void Apply()
        {
            if (view == null || view.targetTexture != null || Screen.height == 0) return;
            float screenAspect = (float)Screen.width / Screen.height;
            if (screenAspect > Aspect)
            {
                float width = Aspect / screenAspect;
                view.rect = new Rect((1-width)*.5f,0,width,1);
            }
            else
            {
                float height = screenAspect / Aspect;
                view.rect = new Rect(0,(1-height)*.5f,1,height);
            }
            view.aspect = Aspect;
        }
    }
}
