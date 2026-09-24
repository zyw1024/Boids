Shader "Boids/SkyCity/Reflecting Garden Water"
{
    Properties
    {
        _ShallowColor("Shallows",Color)=(.12,.43,.39,1)
        _DeepColor("Deep water",Color)=(.028,.14,.19,1)
        _FlowSpeed("Flow speed",Float)=.8
        _ReflectionStrength("Reflection",Range(0,1))=.78
        _RippleStrength("Ripples",Float)=.045
    }
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-15" "RenderType"="Transparent"}
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE2D(_SkySceneColor);SAMPLER(sampler_SkySceneColor);
            TEXTURE2D(_SkyPlanarReflection);SAMPLER(sampler_SkyPlanarReflection);
            CBUFFER_START(UnityPerMaterial)
            float4 _ShallowColor,_DeepColor;float _FlowSpeed,_ReflectionStrength,_RippleStrength;
            CBUFFER_END
            struct A{float3 position:POSITION;float2 uv:TEXCOORD0;};
            struct V{float4 position:SV_POSITION;float3 world:TEXCOORD0;float2 uv:TEXCOORD1;float fog:TEXCOORD2;};
            V vert(A a)
            {
                V o;o.world=TransformObjectToWorld(a.position);
                o.world.y+=sin(o.world.x*2.2+o.world.z*1.7-_Time.y*1.5)*.016+sin(o.world.z*3.2+_Time.y*1.2)*.008;
                o.position=TransformWorldToHClip(o.world);o.uv=a.uv;o.fog=ComputeFogFactor(o.position.z);return o;
            }
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
            float currentPattern(float2 p){return noise(p*float2(4.5,1.4))*.64+noise(p*float2(9.2,2.1))*.36;}
            half4 frag(V i):SV_Target
            {
                float t=_Time.y*_FlowSpeed;float2 p=i.world.xz;
                float a=dot(p,float2(2.2,1.7))-t*1.8,b=dot(p,float2(-1.4,3.6))-t*2.7,c=dot(p,float2(7.3,4.2))+t*4.1;
                float2 slope=cos(a)*float2(2.2,1.7)*.022+cos(b)*float2(-1.4,3.6)*.012+cos(c)*float2(7.3,4.2)*.003;
                // Two spillways drive a continuous current through the modeled basin.
                float2 toLeft=float2(-8.95,-6.05)-p,toRight=float2(-4.18,-6.1)-p;
                float2 flowVector=toLeft/(dot(toLeft,toLeft)+.8)+toRight/(dot(toRight,toRight)+.8);
                float2 flow=flowVector*rsqrt(max(dot(flowVector,flowVector),.00001));
                float phase0=frac(t*.18),phase1=frac(t*.18+.5),blend=abs(phase0*2-1);
                float current=lerp(currentPattern(p-flow*phase0*4.5),currentPattern(p-flow*phase1*4.5),blend);
                slope+=flow*(current-.5)*.055;
                float3 normal=normalize(float3(-slope.x,1,-slope.y));
                float3 view=SafeNormalize(GetWorldSpaceViewDir(i.world));
                float2 uv=GetNormalizedScreenSpaceUV(i.position);
                float eye=-TransformWorldToView(i.world).z;
                float depth=max(0,LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams)-eye);
                float2 distorted=uv+normal.xz*_RippleStrength;
                if(LinearEyeDepth(SampleSceneDepth(distorted),_ZBufferParams)<eye)distorted=uv;
                float3 under=SAMPLE_TEXTURE2D(_SkySceneColor,sampler_SkySceneColor,distorted).rgb;
                float3 absorption=exp(-depth*float3(.70,.25,.18));
                float3 tint=lerp(_ShallowColor.rgb,_DeepColor.rgb,1-exp(-depth*.4));
                float3 water=under*absorption+tint*(1-absorption);
                float3 reflected=SAMPLE_TEXTURE2D(_SkyPlanarReflection,sampler_SkyPlanarReflection,uv+normal.xz*_RippleStrength*.8).rgb;
                float fresnel=.035+.965*pow(1-saturate(dot(normal,view)),5);
                water=lerp(water,reflected,saturate(.18+fresnel*_ReflectionStrength));
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
                float3 halfVector=normalize(sun.direction+view);
                float spec=pow(saturate(dot(normal,halfVector)),180)*1.6;
                water+=sun.color*spec*sun.shadowAttenuation;
                float foam=(1-smoothstep(.018,.22,depth))*(.55+.45*sin(p.x*19+p.y*13-t*4));
                float spill=1-smoothstep(.2,2.5,min(length(toLeft),length(toRight)));
                float driftingFoam=smoothstep(.60,.81,current)*(.04+spill*.22);
                water=lerp(water,float3(1.05,1.07,.98),saturate(foam*.65+driftingFoam));
                return half4(MixFog(water,i.fog),.97);
            }
            ENDHLSL
        }
    }
}
