using UnityEngine;

namespace Boids.Art.Infinite
{
    /// <summary>Authoring metadata. Rotations are solver states, not extra module designs.</summary>
    public sealed class SkyCityModuleDescriptor : MonoBehaviour
    {
        public int moduleId;
        public string architecturalFamily,variant;
        [Tooltip("North, East, South, West walkways: one bit per direction.")]
        public int socketMask;
        public float footprint=12;
        void OnDrawGizmosSelected()
        {
            for(int d=0;d<4;d++)
            {
                Vector3 direction=Quaternion.Euler(0,90*d,0)*Vector3.forward;
                Gizmos.color=(socketMask&(1<<d))!=0?new Color(.2f,.9f,.6f):new Color(.6f,.6f,.6f,.4f);
                Gizmos.DrawSphere(transform.TransformPoint(direction*footprint*.5f+Vector3.up*.25f),.22f);
            }
        }
    }
}
