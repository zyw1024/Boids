Shader "Boids/Redon/Painted Atmosphere"
{
    Properties
    {
        _PigmentTex("Scumbled pigment",2D)="gray"{}
        _DeepColor("Lower water",Color)=(.1,.21,.29,1)
        _GoldColor("Apricot opening",Color)=(.95,.68,.42,1)
        _PearlColor("Warm heart",Color)=(1,.87,.61,1)
        _TextureStrength("Pigment amount",Range(0,1))=.5
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Opaque" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "RedonCommon.hlsl"
            TEXTURE2D(_PigmentTex); SAMPLER(sampler_PigmentTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _DeepColor,_GoldColor,_PearlColor;
            float _TextureStrength;
            CBUFFER_END
            struct A { float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
            struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float2 uv:TEXCOORD1;};
            V vert(A a){V o;o.world=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.uv=a.uv;return o;}
            half4 frag(V i):SV_Target
            {
                float2 uv=i.world.xy;
                float p=SAMPLE_TEXTURE2D(_PigmentTex,sampler_PigmentTex,uv*.064).r;
                float q=SAMPLE_TEXTURE2D(_PigmentTex,sampler_PigmentTex,uv*.26+float2(.1,.4)).r;
                float f=SAMPLE_TEXTURE2D(_PigmentTex,sampler_PigmentTex,uv*.86).r;
                float3 c=lerp(_DeepColor.rgb,_RedonWater.rgb,smoothstep(-3,8,uv.y));
                c=lerp(c,_RedonUpperWater.rgb,smoothstep(3,15,uv.y)*.7);
                float2 d=(uv-_RedonFocus.xy)/_RedonExtent.xy;
                d+=float2(p-.5,q-.5)*.5;
                float glow=exp(-dot(d,d)*.9);
                c=lerp(c,_GoldColor.rgb,glow*.95);
                c=lerp(c,_PearlColor.rgb,pow(glow,3)*.82);
                c*=1+((p-.5)*.4+(q-.5)*.35+(f-.5)*.12)*_TextureStrength*_RedonPaint;
                return half4(c,1);
            }
            ENDHLSL
        }
    }
}
