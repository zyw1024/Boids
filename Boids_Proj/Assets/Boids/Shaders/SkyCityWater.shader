Shader "Boids/SkyCity/Cascades"
{
    Properties {_FlowSpeed("Flow speed",Float)=1.0 _Refraction("Refraction",Float)=.008}
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE2D(_SkySceneColor);SAMPLER(sampler_SkySceneColor);
            CBUFFER_START(UnityPerMaterial)
            float _FlowSpeed,_Refraction;
            CBUFFER_END
            struct A{float4 p:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;};
            struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float fog:TEXCOORD1;float3 world:TEXCOORD2;float3 normal:TEXCOORD3;};
            V vert(A a)
            {
                V o;o.world=TransformObjectToWorld(a.p.xyz);o.normal=TransformObjectToWorldNormal(a.normal);
                float wave=sin(a.uv.y*22-_Time.y*_FlowSpeed*8+a.uv.x*8)*.024+sin(a.uv.y*51-_Time.y*13)*.009;
                o.world+=o.normal*wave*a.uv.y;o.p=TransformWorldToHClip(o.world);o.uv=a.uv;o.fog=ComputeFogFactor(o.p.z);return o;
            }
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
            half4 frag(V i):SV_Target
            {
                float t=_Time.y*_FlowSpeed;
                float2 flow=float2(i.uv.x*23,i.uv.y*4-t*2);
                float strands=noise(flow)*.65+noise(flow*2.9)*.35;
                float froth=noise(float2(i.uv.x*17,i.uv.y*31-t*10));
                float2 uv=GetNormalizedScreenSpaceUV(i.p);
                float eye=-TransformWorldToView(i.world).z;
                float gap=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams)-eye;
                float edge=smoothstep(0,.1,i.uv.x)*(1-smoothstep(.86,1,i.uv.x));
                float fade=1-smoothstep(.68,1,i.uv.y);
                float spray=smoothstep(.55,.83,froth)*smoothstep(.2,.9,i.uv.y);
                float3 n=normalize(i.normal+float3((strands-.5)*.45,(froth-.5)*.18,0));
                float3 v=SafeNormalize(GetWorldSpaceViewDir(i.world));
                float2 offset=n.xy*_Refraction*(strands+.3);
                if(LinearEyeDepth(SampleSceneDepth(uv+offset),_ZBufferParams)<eye)offset=0;
                float3 behind=SAMPLE_TEXTURE2D(_SkySceneColor,sampler_SkySceneColor,uv+offset).rgb;
                float fresnel=pow(1-abs(dot(n,v)),3);
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
                float spec=pow(saturate(dot(n,normalize(sun.direction+v))),65)*sun.shadowAttenuation;
                float foam=smoothstep(.39,.68,strands)*.64+spray*.45;
                float3 c=lerp(behind*float3(.66,.90,.96),float3(1.10,1.12,1.06),saturate(foam+fresnel*.20));
                c+=sun.color*spec*.7;
                float alpha=(.28+strands*.40+spray*.22)*edge*fade*saturate(gap*3);
                return half4(MixFog(c,i.fog),alpha);
            }
            ENDHLSL
        }
    }
}
