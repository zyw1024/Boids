using UnityEngine;

namespace SkyCity.Runtime
{
    /// <summary>One lighting direction and sky palette for all procedural surfaces.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class SkyCityDaylight : MonoBehaviour
    {
        public Light sun;
        public Material sky;
        static readonly int Direction=Shader.PropertyToID("_SkySunDirection");
        static readonly int Radiance=Shader.PropertyToID("_SkySunRadiance");
        static readonly int Zenith=Shader.PropertyToID("_SkyDawnZenith");
        static readonly int Horizon=Shader.PropertyToID("_SkyDawnHorizon");
        void OnEnable(){Apply();DynamicGI.UpdateEnvironment();}
        void OnDisable(){Shader.SetGlobalVector(Direction,Vector4.zero);}
        void LateUpdate(){Apply();}
        public void Apply()
        {
            var light=sun!=null?sun:RenderSettings.sun;
            var material=sky!=null?sky:RenderSettings.skybox;
            if(light==null||material==null)return;
            Shader.SetGlobalVector(Direction,-light.transform.forward);
            Shader.SetGlobalColor(Radiance,light.color.linear*light.intensity);
            if(material.HasProperty("_Zenith"))Shader.SetGlobalColor(Zenith,material.GetColor("_Zenith").linear);
            if(material.HasProperty("_Horizon"))Shader.SetGlobalColor(Horizon,material.GetColor("_Horizon").linear);
        }
    }
}
