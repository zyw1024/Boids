using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Boids.Art
{
    [ExecuteAlways]
    public sealed class SkyCityWaterReflection : MonoBehaviour
    {
        public float waterHeight=2.42f;
        public int rendererIndex;
        public int textureWidth=1024;
        Camera reflectedCamera;
        RenderTexture reflection;
        static bool rendering;
        public int RenderCount {get;private set;}
        public RenderTexture ReflectionTexture => reflection;
        void OnEnable(){RenderPipelineManager.beginCameraRendering+=RenderReflection;}
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering-=RenderReflection;
            if(reflectedCamera!=null)DestroyOwned(reflectedCamera.gameObject);
            if(reflection!=null){reflection.Release();DestroyOwned(reflection);}
        }
        static void DestroyOwned(Object item){if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
        void RenderReflection(ScriptableRenderContext context,Camera source)
        {
            if(rendering||source!=Camera.main||source.cameraType==CameraType.Reflection)return;
            if(reflectedCamera==null)
            {
                var go=new GameObject("Water garden reflection camera"){hideFlags=HideFlags.HideAndDontSave};
                reflectedCamera=go.AddComponent<Camera>();reflectedCamera.enabled=false;
            }
            int height=Mathf.Max(64,Mathf.RoundToInt(textureWidth/source.aspect));
            if(reflection==null||reflection.width!=textureWidth||reflection.height!=height)
            {
                if(reflection!=null){reflection.Release();DestroyOwned(reflection);}
                reflection=new RenderTexture(textureWidth,height,24,RenderTextureFormat.ARGBHalf)
                {name="Sky City live planar reflection",hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear};
                reflection.Create();
            }
            reflectedCamera.CopyFrom(source);reflectedCamera.enabled=false;reflectedCamera.cameraType=CameraType.Reflection;
            reflectedCamera.targetTexture=reflection;reflectedCamera.rect=new Rect(0,0,1,1);
            reflectedCamera.cullingMask=source.cullingMask&~(1<<4);reflectedCamera.allowMSAA=false;
            var data=reflectedCamera.GetUniversalAdditionalCameraData();data.SetRenderer(rendererIndex);
            data.renderPostProcessing=false;data.requiresDepthTexture=true;data.requiresColorTexture=true;
            var mirror=Matrix4x4.identity;mirror.m11=-1;mirror.m13=2*waterHeight;
            reflectedCamera.transform.position=mirror.MultiplyPoint(source.transform.position);
            reflectedCamera.transform.rotation=Quaternion.LookRotation(mirror.MultiplyVector(source.transform.forward),mirror.MultiplyVector(source.transform.up));
            reflectedCamera.worldToCameraMatrix=source.worldToCameraMatrix*mirror;
            var clipPoint=reflectedCamera.worldToCameraMatrix.MultiplyPoint(new Vector3(0,waterHeight+.035f,0));
            var normal=reflectedCamera.worldToCameraMatrix.MultiplyVector(Vector3.up).normalized;
            var plane=new Vector4(normal.x,normal.y,normal.z,-Vector3.Dot(clipPoint,normal));
            reflectedCamera.projectionMatrix=source.CalculateObliqueMatrix(plane);
            bool previous=GL.invertCulling;
            try
            {
                rendering=true;GL.invertCulling=!previous;
#pragma warning disable 0618
                UniversalRenderPipeline.RenderSingleCamera(context,reflectedCamera);
#pragma warning restore 0618
                Shader.SetGlobalTexture("_SkyPlanarReflection",reflection);RenderCount++;
            }
            finally{GL.invertCulling=previous;rendering=false;}
        }
    }
}
