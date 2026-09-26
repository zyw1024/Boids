using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SkyCity.Runtime
{
    /// <summary>One world-space cloud field, integrated before transparent water.</summary>
    public sealed class SkyCityAtmosphereFeature : ScriptableRendererFeature
    {
        public Material cloudMaterial;
        [Range(.5f,1)] public float resolutionScale=1f;
        CloudPass pass;
        public override void Create(){pass?.Dispose();pass=new CloudPass();}
        public override void AddRenderPasses(ScriptableRenderer renderer,ref RenderingData data)
        {
            if(cloudMaterial==null||data.cameraData.cameraType==CameraType.Preview)return;
            pass.material=cloudMaterial;pass.scale=resolutionScale;
            pass.renderPassEvent=(RenderPassEvent)((int)RenderPassEvent.BeforeRenderingTransparents-5);
            pass.ConfigureInput(ScriptableRenderPassInput.Depth|ScriptableRenderPassInput.Color);
            renderer.EnqueuePass(pass);
        }
        protected override void Dispose(bool disposing){pass?.Dispose();}
        sealed class CloudPass : ScriptableRenderPass
        {
            public Material material;public float scale;
            sealed class Targets
            {
                public RTHandle volume,sceneColor;
                public void Release(){volume?.Release();sceneColor?.Release();}
            }
            readonly Dictionary<Camera,Targets> targets=new Dictionary<Camera,Targets>();
            readonly List<Camera> expired=new List<Camera>();
            readonly ProfilingSampler marker=new ProfilingSampler("Sky City / volumetric atmosphere");
            public override void OnCameraSetup(CommandBuffer cmd,ref RenderingData data)
            {
                expired.Clear();foreach(var pair in targets)if(pair.Key==null)expired.Add(pair.Key);
                foreach(var camera in expired){targets[camera].Release();targets.Remove(camera);}
                Targets target;if(!targets.TryGetValue(data.cameraData.camera,out target)){target=new Targets();targets.Add(data.cameraData.camera,target);}
                var d=data.cameraData.cameraTargetDescriptor;d.msaaSamples=1;d.depthBufferBits=0;
                d.graphicsFormat=UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat;
                RenderingUtils.ReAllocateIfNeeded(ref target.sceneColor,d,FilterMode.Bilinear,TextureWrapMode.Clamp,name:"_SkySceneColor");
                float cameraScale=data.cameraData.cameraType==CameraType.Reflection?scale*.6f:scale;
                d.width=Mathf.Max(1,Mathf.RoundToInt(d.width*cameraScale));d.height=Mathf.Max(1,Mathf.RoundToInt(d.height*cameraScale));
                RenderingUtils.ReAllocateIfNeeded(ref target.volume,d,FilterMode.Bilinear,TextureWrapMode.Clamp,name:"_SkyCloudVolume");
                ResetTarget();
            }
            public override void Execute(ScriptableRenderContext context,ref RenderingData data)
            {
                Targets target;if(!targets.TryGetValue(data.cameraData.camera,out target))return;
                var volume=target.volume;var sceneColor=target.sceneColor;
                var cmd=CommandBufferPool.Get();
                using(new ProfilingScope(cmd,marker))
                {
                    CoreUtils.SetRenderTarget(cmd,volume,ClearFlag.Color,Color.clear);
                    cmd.SetGlobalVector("_CloudTargetSize",new Vector4(volume.rt.width,volume.rt.height,1f/volume.rt.width,1f/volume.rt.height));
                    cmd.DrawProcedural(Matrix4x4.identity,material,0,MeshTopology.Triangles,3);
                    cmd.SetGlobalTexture("_SkyCloudVolume",volume.nameID);
                    CoreUtils.SetRenderTarget(cmd,data.cameraData.renderer.cameraColorTargetHandle);
                    cmd.DrawProcedural(Matrix4x4.identity,material,1,MeshTopology.Triangles,3);
                    Blitter.BlitCameraTexture(cmd,data.cameraData.renderer.cameraColorTargetHandle,sceneColor);
                    cmd.SetGlobalTexture("_SkySceneColor",sceneColor.nameID);
                }
                context.ExecuteCommandBuffer(cmd);CommandBufferPool.Release(cmd);
            }
            public void Dispose(){foreach(var pair in targets)pair.Value.Release();targets.Clear();}
        }
    }
}
