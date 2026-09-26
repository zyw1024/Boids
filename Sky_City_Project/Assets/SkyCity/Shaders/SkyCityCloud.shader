Shader "SkyCity/Volumetric Clouds"
{
    Properties
    {
        _DensityTex("Cloud density",3D)="white"{}
        _Density("Optical density",Float)=.9
        _Seed("Variation",Float)=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Cull Front ZWrite Off ZTest Always Blend One OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE3D(_DensityTex);SAMPLER(sampler_DensityTex);
            CBUFFER_START(UnityPerMaterial)
            float _Density,_Seed;
            CBUFFER_END
            struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;};
            V vert(float3 p:POSITION){V o;o.world=TransformObjectToWorld(p);o.positionCS=TransformWorldToHClip(o.world);return o;}
            float density(float3 p)
            {
                float3 uv=p+.5;
                if(any(uv<0)||any(uv>1))return 0;
                return SAMPLE_TEXTURE3D_LOD(_DensityTex,sampler_DensityTex,uv,0).r*_Density;
            }
            half4 frag(V i):SV_Target
            {
                float3 origin=GetCameraPositionWS(),ray=normalize(i.world-origin);
                float3 ro=TransformWorldToObject(origin),rd=mul((float3x3)unity_WorldToObject,ray);
                float3 inv=rcp(rd),a=(-.5-ro)*inv,b=(.5-ro)*inv;
                float3 near3=min(a,b),far3=max(a,b);
                float entry=max(0,max(near3.x,max(near3.y,near3.z)));
                float finish=min(far3.x,min(far3.y,far3.z));
                float2 screen=GetNormalizedScreenSpaceUV(i.positionCS);
                float sceneZ=LinearEyeDepth(SampleSceneDepth(screen),_ZBufferParams);
                float forward=max(.01,-mul((float3x3)UNITY_MATRIX_V,ray).z);
                finish=min(finish,sceneZ/forward);
                if(finish<=entry)return 0;
                float stepSize=(finish-entry)/96;
                float3 lightDir=GetMainLight().direction;
                float3 lightOS=mul((float3x3)unity_WorldToObject,lightDir);
                float viewSun=pow(saturate(dot(ray,lightDir)),5);
                float transmission=1;float3 result=0;
                // Fixed sample placement avoids crawling noise as the birds move.
                [loop]for(int sample=0;sample<96;sample++)
                {
                    float t=entry+(sample+.5)*stepSize;float3 p=ro+rd*t;
                    float2 volume=SAMPLE_TEXTURE3D_LOD(_DensityTex,sampler_DensityTex,p+.5,0).rg;
                    float d=volume.r*_Density;if(d<.004)continue;
                    float sunlight=volume.g;
                    float3 shade=float3(.24,.29,.48);
                    float3 lit=float3(2.0,1.42,.95);
                    float3 color=lerp(shade,lit,sunlight);
                    color+=float3(.20,.15,.10)*viewSun*sunlight;
                    float4 clip=TransformWorldToHClip(origin+ray*t);
                    color=lerp(color,MixFog(color,ComputeFogFactor(clip.z)),.30);
                    float alpha=1-exp(-d*stepSize*.70);
                    result+=color*alpha*transmission;transmission*=1-alpha;
                    if(transmission<.008)break;
                }
                return half4(result,1-transmission);
            }
            ENDHLSL
        }
    }
}
