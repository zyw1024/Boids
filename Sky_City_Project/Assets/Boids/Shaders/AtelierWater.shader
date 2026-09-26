Shader "Boids/Atelier/Water Glaze"
{
    Properties
    {
        _PigmentTex("Dry scumble",2D)="gray"{}
        _GlazeTex("Oil pigment",2D)="gray"{}
        _DeepColor("Depth",Color)=(.03,.08,.16,1)
        _GoldColor("Apricot opening",Color)=(.95,.52,.22,1)
        _PearlColor("Warm heart",Color)=(1,.8,.45,1)
        _TextureStrength("Pigment amount",Range(0,1))=.8
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Background"}
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "RedonCommon.hlsl"
            TEXTURE2D(_PigmentTex);SAMPLER(sampler_PigmentTex);
            TEXTURE2D(_GlazeTex);SAMPLER(sampler_GlazeTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _DeepColor,_GoldColor,_PearlColor;float _TextureStrength;
            CBUFFER_END
            struct A{float4 positionOS:POSITION;};
            struct V{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;};
            V vert(A a){V o;o.world=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);return o;}
            half4 frag(V i):SV_Target
            {
                float2 uv=i.world.xy;
                float p=SAMPLE_TEXTURE2D(_PigmentTex,sampler_PigmentTex,uv*.064).r;
                float q=SAMPLE_TEXTURE2D(_PigmentTex,sampler_PigmentTex,uv*.21+float2(.1,.4)).r;
                float3 paint=SAMPLE_TEXTURE2D(_GlazeTex,sampler_GlazeTex,uv*.065).rgb;
                float lum=dot(paint,float3(.21,.72,.07));
                float3 water=lerp(_DeepColor.rgb,_RedonWater.rgb,smoothstep(-2,7,uv.y));
                water=lerp(water,_RedonUpperWater.rgb,smoothstep(5,15,uv.y)*.55);
                float2 d=(uv-_RedonFocus.xy)/_RedonExtent.xy;
                d+=float2(p-.5,q-.5)*.5;
                float glow=exp(-dot(d,d)*1.4);
                float3 color=lerp(water,_GoldColor.rgb,glow*.96);
                color=lerp(color,_PearlColor.rgb,pow(glow,3)*.62);
                color*=1+((lum-.20)*1.3+(p-.5)*.45+(q-.5)*.22)*_TextureStrength;
                color*=lerp(float3(1,1,1),paint/max(.045,lum),.10*(1-glow));
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
