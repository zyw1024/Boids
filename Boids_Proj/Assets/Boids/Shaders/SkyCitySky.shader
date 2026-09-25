Shader "Boids/SkyCity/Dawn Sky"
{
    Properties
    {
        _Zenith("Zenith",Color)=(.10,.21,.42,1)
        _Horizon("Horizon",Color)=(.58,.49,.51,1)
    }
    SubShader
    {
        Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"}
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "SkyCityDaylight.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _Zenith,_Horizon;
            CBUFFER_END
            struct V {float4 positionCS:SV_POSITION;float3 direction:TEXCOORD0;};
            V vert(float3 p:POSITION){V o;o.positionCS=TransformObjectToHClip(p);o.direction=p;return o;}
            half4 frag(V i):SV_Target
            {
                float3 d=normalize(i.direction);
                float t=smoothstep(-.34,.12,d.y);
                float3 sky=lerp(_Horizon.rgb,_Zenith.rgb,t);
                // The shared lighting component is optional for the older fixed scene.
                if(dot(_SkySunDirection.xyz,_SkySunDirection.xyz)>.5)sky=SkyCitySkyRadiance(d);
                else
                {
                    float glow=pow(saturate(dot(d,normalize(float3(.70,.28,1)))),24);
                    sky=lerp(sky,float3(1.20,.97,.68),glow*.50);
                }
                return half4(sky,1);
            }
            ENDHLSL
        }
    }
}
