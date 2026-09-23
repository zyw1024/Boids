using UnityEngine;

namespace Boids.Art
{
    /// <summary>Animation only. The Boids agent owns position and heading.</summary>
    public sealed class MoonveilMotion : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int FeedTrigger = Animator.StringToHash("Feed");
        public Animator Animator => animator;

        public void Configure(Animator target) { animator = target; }
        public void SetSwimSpeed(float normalizedSpeed)
        {
            if (animator != null)
                animator.SetFloat(Speed, Mathf.Clamp01(normalizedSpeed), 0.18f, Time.deltaTime);
        }
        public void Feed()
        {
            if (animator != null) animator.SetTrigger(FeedTrigger);
        }
    }
}
