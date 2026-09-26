Shader "Boids/SkyCity/Infinite Cascades"
{
    Properties
    {
        _FlowSpeed("Current",Float)=1
        _Refraction("Refraction",Float)=.004
        _Reveal("Arrival",Range(0,1))=1
    }
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
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE2D(_SkySceneColor);SAMPLER(sampler_SkySceneColor);
            CBUFFER_START(UnityPerMaterial)
            float _FlowSpeed,_Refraction,_Reveal;
            CBUFFER_END
            struct A{float4 p:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;float4 c:COLOR;};
            struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float3 normal:TEXCOORD2;float fog:TEXCOORD3;float2 spray:TEXCOORD4;};
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
            V vert(A a)
            {
                V o;o.world=TransformObjectToWorld(a.p.xyz);o.normal=TransformObjectToWorldNormal(a.normal);
                // The mesh supplies gravity. Only fine surface turbulence moves
                // here; broad sinusoidal displacement would read as hanging cloth.
                o.spray=0;
                if(a.uv.x>=2)
                {
                    float age=frac(_Time.y*(.19+a.c.b*.10)+a.c.r);
                    o.world+=float3((age-.5)*.65,-age*2.2,-age*.45);
                    float2 corner=float2(a.uv.x-2,a.uv.y)-.5;
                    float radius=a.c.g*3*(.6+age);
                    o.world+=(UNITY_MATRIX_I_V._m00_m10_m20*corner.x+UNITY_MATRIX_I_V._m01_m11_m21*corner.y)*radius*2;
                    o.spray=float2(1,sin(age*3.14159));
                }
                else
                {
                    float ripple=sin(a.uv.x*74+a.uv.y*53-_Time.y*21)*.008*a.uv.y;
                    o.world+=o.normal*ripple;
                }
                o.p=TransformWorldToHClip(o.world);o.uv=a.uv;o.fog=ComputeFogFactor(o.p.z);return o;
            }
            half4 frag(V i):SV_Target
            {
                float t=_Time.y*_FlowSpeed;
                if(i.spray.x>.5)
                {
                    float2 uv=GetNormalizedScreenSpaceUV(i.p);
                    float gap=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams)+TransformWorldToView(i.world).z;
                    float2 p=float2(i.uv.x-2,i.uv.y)*2-1;
                    float soft=pow(saturate(1-dot(p,p)),2);
                    float grain=noise(p*5+float2(0,-t*1.7));
                    return half4(MixFog(float3(.86,.96,1),i.fog),soft*grain*.13*i.spray.y*saturate(gap)*_Reveal);
                }
                // v is flight time, so features accelerate with the curved mesh.
                float2 q=float2(i.uv.x*37,i.uv.y*10-t*2.5);
                float threads=noise(q)*.65+noise(q*float2(2.3,1.7))*.35;
                float droplets=noise(float2(i.uv.x*91,i.uv.y*85-t*20));
                float turbulence=smoothstep(.08,.72,i.uv.y);
                float edgeNoise=noise(float2(i.uv.y*42-t*7,i.uv.x*7));
                float edgeWidth=.025+turbulence*.13;
                float edge=smoothstep(0,edgeWidth,min(i.uv.x,1-i.uv.x)-edgeNoise*.08*turbulence);
                float breakup=smoothstep(.22+turbulence*.13,.55,threads);
                float tail=1-smoothstep(.58+threads*.25,.97,i.uv.y);
                float2 uv=GetNormalizedScreenSpaceUV(i.p);
                float eye=-TransformWorldToView(i.world).z;
                float gap=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams)-eye;
                float3 n=normalize(i.normal+float3((threads-.5)*.24,(droplets-.5)*.055,0));
                float3 view=SafeNormalize(GetWorldSpaceViewDir(i.world));
                if(dot(n,view)<0)n=-n;
                float2 offset=n.xy*_Refraction*(.35+threads);
                if(LinearEyeDepth(SampleSceneDepth(uv+offset),_ZBufferParams)<eye)offset=0;
                float3 behind=SAMPLE_TEXTURE2D(_SkySceneColor,sampler_SkySceneColor,uv+offset).rgb;
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
                float fresnel=pow(1-saturate(dot(n,view)),4);
                float glint=pow(saturate(dot(n,normalize(sun.direction+view))),105)*sun.shadowAttenuation;
                float aeration=smoothstep(.44,.77,threads)*lerp(.22,.74,turbulence);
                float3 water=behind*float3(.66,.90,.96);
                water=lerp(water,float3(1.10,1.12,1.06),aeration+fresnel*.15);
                water+=sun.color*glint*.48;
                float density=lerp(.56,.26,turbulence)+breakup*.38;
                float alpha=density*edge*tail*lerp(1,breakup,turbulence*.68)*saturate(gap*4)*_Reveal;
                return half4(MixFog(water,i.fog),alpha);
            }
            ENDHLSL
        }
    }
}
