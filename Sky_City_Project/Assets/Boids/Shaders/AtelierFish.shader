Shader "Boids/Atelier/Living Pearl"
{
    Properties
    {
        _BaseColor("Pearl pigment",Color)=(.7,.7,.6,1)
        _MainTex("Authored fish atlas",2D)="white"{}
        _Membrane("Silk membrane",Float)=0
        [HideInInspector] _ZWrite("Depth write",Int)=1
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent"}
        Cull Off ZWrite [_ZWrite] Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "RedonCommon.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;float _Membrane;
            CBUFFER_END
            struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
            struct V{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;};
            V vert(A a){V o;o.world=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(a.normalOS);o.uv=a.uv;return o;}
            half4 frag(V i,bool front:SV_IsFrontFace):SV_Target
            {
                float4 tex=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                float lum=dot(tex.rgb,float3(.21,.72,.07));
                float3 n=normalize(i.normal)*(front?1:-1);
                float3 view=SafeNormalize(GetWorldSpaceViewDir(i.world));
                float diffuse=saturate(dot(n,normalize(float3(-.4,.65,-.7)))*.55+.45);
                float3 pearl=_BaseColor.rgb*lerp(float3(.32,.51,.68),float3(1.42,1.27,.95),diffuse);
                pearl*=lerp(.55,1.35,sqrt(lum));
                float gleam=pow(saturate(dot(reflect(-normalize(_RedonLight.xyz),n),view)),18);
                pearl+=gleam*float3(.18,.16,.12);
                // The body atlas reserves its upper strip for the modeled eyes and gills.
                float detail=step(.89,i.uv.y)*(1-_Membrane);
                pearl=lerp(pearl,tex.rgb*(.7+.3*diffuse),detail);
                float alpha=lerp(1,saturate(tex.a*.80),_Membrane);
                return half4(RedonFogColor(pearl,i.world),alpha);
            }
            ENDHLSL
        }
    }
}
