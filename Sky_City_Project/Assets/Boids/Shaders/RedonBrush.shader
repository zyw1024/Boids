Shader "Boids/Redon/Anchored Brush"
{
    Properties
    {
        _BrushAtlas("4 x 4 dry brush atlas",2D)="white"{}
        _Tint("Pigment",Color)=(.5,.5,.6,1)
        _Accent("Accent pigment",Color)=(.7,.55,.37,1)
        _Opacity("Coverage",Range(0,1))=.6
        _ColorVariation("Pigment variation",Range(0,1))=.5
        _FogAmount("Atmosphere",Range(0,1))=1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "RedonCommon.hlsl"
            TEXTURE2D(_BrushAtlas); SAMPLER(sampler_BrushAtlas);
            CBUFFER_START(UnityPerMaterial)
            float4 _Tint,_Accent;
            float _Opacity,_ColorVariation,_FogAmount;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float2 uv:TEXCOORD1; float4 color:COLOR; };
            V vert(A a) { V o; o.world=TransformObjectToWorld(a.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.world);o.uv=a.uv;o.color=a.color;return o; }
            half4 frag(V i):SV_Target
            {
                float brush=SAMPLE_TEXTURE2D(_BrushAtlas,sampler_BrushAtlas,i.uv).r;
                float alpha=smoothstep(.025,.8,brush)*_Opacity*i.color.a;
                clip(alpha-.004);
                float3 c=lerp(_Tint.rgb,_Accent.rgb,i.color.r*_ColorVariation);
                c*=lerp(.76,1.2,i.color.g);
                c=lerp(c,RedonFogColor(c,i.world),_FogAmount);
                return half4(c,alpha);
            }
            ENDHLSL
        }
    }
}
