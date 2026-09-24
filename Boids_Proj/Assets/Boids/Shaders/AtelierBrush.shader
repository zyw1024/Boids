Shader "Boids/Atelier/Surface Brush"
{
    Properties{_BrushAtlas("Brush atlas",2D)="white"{} _Opacity("Coverage",Range(0,1))=.65}
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "RedonCommon.hlsl"
            TEXTURE2D(_BrushAtlas);SAMPLER(sampler_BrushAtlas);
            CBUFFER_START(UnityPerMaterial)
            float _Opacity;
            CBUFFER_END
            struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct V{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float2 uv:TEXCOORD1;float4 color:COLOR;};
            V vert(A a){V o;o.world=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.uv=a.uv;o.color=a.color;return o;}
            half4 frag(V i):SV_Target
            {
                float alpha=smoothstep(.08,.72,SAMPLE_TEXTURE2D(_BrushAtlas,sampler_BrushAtlas,i.uv).r)*_Opacity;
                clip(alpha-.01);
                return half4(RedonFogColor(i.color.rgb,i.world),alpha);
            }
            ENDHLSL
        }
    }
}
