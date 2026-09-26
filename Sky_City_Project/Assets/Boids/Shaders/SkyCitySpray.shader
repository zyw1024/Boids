Shader "Boids/SkyCity/Soft Water Spray"
{
    Properties {}
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" "RenderType"="Transparent"}
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            struct A{float3 p:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 color:TEXCOORD1;float eye:TEXCOORD2;};
            V vert(A a){V o;float3 w=TransformObjectToWorld(a.p);o.p=TransformWorldToHClip(w);o.uv=a.uv;o.color=a.color;o.eye=-TransformWorldToView(w).z;return o;}
            half4 frag(V i):SV_Target
            {
                float2 p=(i.uv-.5)*2;float a=saturate(1-dot(p,p));a=a*a;
                float depth=LinearEyeDepth(SampleSceneDepth(GetNormalizedScreenSpaceUV(i.p)),_ZBufferParams);
                return half4(i.color.rgb,a*i.color.a*saturate((depth-i.eye)*1.5));
            }
            ENDHLSL
        }
    }
}
