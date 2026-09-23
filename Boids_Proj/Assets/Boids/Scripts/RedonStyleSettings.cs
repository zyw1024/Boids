using UnityEngine;

namespace Boids.Art
{
    /// <summary>Art controls only. This study intentionally has no camera input or flock simulation.</summary>
    [ExecuteAlways]
    public sealed class RedonStyleSettings : MonoBehaviour
    {
        [Header("Painterly surface")]
        [Range(0f, 1.5f)] public float pigmentStrength = 1f;
        [Range(0f, 1f)] public float paintedNormals = 0.035f;
        [Header("Spatial color")]
        public Color waterColor = new Color(0.24f, 0.39f, 0.48f);
        public Color upperWaterColor = new Color(0.42f, 0.43f, 0.54f);
        public Color warmColor = new Color(0.91f, 0.66f, 0.40f);
        public Vector3 warmFocus = new Vector3(2.8f, 10.5f, 28f);
        public Vector2 warmExtent = new Vector2(5.5f, 6.5f);
        [Range(0f, 0.12f)] public float fogDensity = 0.045f;
        public float fogStart = 25f;
        public Vector3 lightDirection = new Vector3(-0.35f, 0.6f, -0.75f);

        void OnEnable() => Apply();
        void OnValidate() => Apply();

        public void Apply()
        {
            Shader.SetGlobalFloat("_RedonPaint", pigmentStrength);
            Shader.SetGlobalFloat("_RedonRelief", paintedNormals);
            Shader.SetGlobalColor("_RedonWater", waterColor.linear);
            Shader.SetGlobalColor("_RedonUpperWater", upperWaterColor.linear);
            Shader.SetGlobalColor("_RedonWarm", warmColor.linear);
            Shader.SetGlobalVector("_RedonFocus", warmFocus);
            Shader.SetGlobalVector("_RedonExtent", new Vector4(Mathf.Max(.1f, warmExtent.x), Mathf.Max(.1f, warmExtent.y), 0, 0));
            Shader.SetGlobalVector("_RedonFog", new Vector4(fogStart, fogDensity, 0, 0));
            Shader.SetGlobalVector("_RedonLight", lightDirection.normalized);
        }
    }
}
