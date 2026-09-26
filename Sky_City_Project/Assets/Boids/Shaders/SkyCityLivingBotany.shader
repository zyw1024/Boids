Shader "Boids/SkyCity/Living Botany"
{
    Properties
    {
        _StateTex("Cell states",2D)="white"{}
        _Phases("Discrete phases",2D)="black"{}
        _Tree("Tree geometry",Float)=0
        _Growth("Tree growth",Range(0,1))=1
        _Generations("Generations",Float)=6
        _Breeze("Breeze",Float)=1
        _StateTime("Last tick",Float)=0
        _Transition("Tick interpolation",Float)=.2
        _Rules("Show cell states",Float)=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "SkyCityFoliage.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float _Tree,_Growth,_Generations,_Breeze,_StateTime,_Transition,_Rules;
        CBUFFER_END
        TEXTURE2D(_StateTex);SAMPLER(sampler_StateTex);
        TEXTURE2D(_Phases);SAMPLER(sampler_Phases);
        struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 color:COLOR;float2 uv:TEXCOORD0;float2 cell:TEXCOORD1;float3 root:TEXCOORD2;float3 flower:TEXCOORD3;};
        struct V{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float4 color:TEXCOORD2;float2 uv:TEXCOORD3;float fog:TEXCOORD4;float amount:TEXCOORD5;float bloom:TEXCOORD6;float phase:TEXCOORD7;};
        V vert(A a)
        {
            V o=(V)0;float amount=1,bloom=1;float3 p=a.positionOS.xyz;
            if(_Tree>.5)
            {
                amount=saturate(_Growth*_Generations-a.cell.x);
                p=a.root+(p-a.root)*amount;
            }
            else if(_Rules<1.5)
            {
                float4 state=SAMPLE_TEXTURE2D_LOD(_StateTex,sampler_StateTex,a.cell,0);
                float f=saturate((_Time.y-_StateTime)/max(.001,_Transition));
                float growth=lerp(state.b,state.r,f);bloom=lerp(state.a,state.g,f);
                if(a.color.a>.9)p=a.flower+(p-a.flower)*lerp(.055,1,bloom);
                p=a.root+(p-a.root)*lerp(.4,1,growth);
            }
            o.world=TransformObjectToWorld(p);
            o.phase=SAMPLE_TEXTURE2D_LOD(_Phases,sampler_Phases,a.cell,0).r*3;
            float leaf=smoothstep(.1,.5,a.color.a);
            float weight=_Rules>1.5?0:(_Tree>.5?leaf*.08:max(0,p.y-a.root.y)*.13);
            float wind=sin(_Time.y*1.3+o.world.x*.7+o.world.z*.9)+.3*sin(_Time.y*2.2+o.world.x*2);
            o.world.x+=wind*weight*_Breeze;o.world.z+=sin(_Time.y*1.7+o.world.x)*weight*.45*_Breeze;
            o.normal=TransformObjectToWorldNormal(a.normalOS);o.color=a.color;o.uv=a.uv;o.amount=amount;o.bloom=bloom;
            o.positionCS=TransformWorldToHClip(o.world);o.fog=ComputeFogFactor(o.positionCS.z);return o;
        }
        half4 frag(V i,bool front:SV_IsFrontFace):SV_Target
        {
            clip(i.amount-.001);
            if(_Rules>1.5)
            {
                clip(min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y))-.035);
                float3 color=i.phase<.5?float3(.10,.32,.54):(i.phase<1.5?float3(.18,.65,.32):(i.phase<2.5?float3(1,.66,.16):float3(.52,.24,.63)));
                return half4(color,1);
            }
            float3 n=normalize(i.normal)*(front?1:-1);
            float3 albedo=i.color.rgb;
            if(i.color.a>.2)
            {
                albedo=SkyCityLeafPigment(albedo,i.uv);
            }
            else
            {
                float grain=sin(i.uv.y*85+sin(i.uv.x*23)*2)*sin(i.uv.y*137+i.uv.x*19);
                albedo*=.90+grain*.09;
            }
            InputData input=(InputData)0;input.positionWS=i.world;input.normalWS=n;
            input.viewDirectionWS=SafeNormalize(GetWorldSpaceViewDir(i.world));input.shadowCoord=TransformWorldToShadowCoord(i.world);
            input.bakedGI=SampleSH(n);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);input.shadowMask=1;
            SurfaceData surface=(SurfaceData)0;surface.albedo=albedo;surface.smoothness=i.color.a>.2?.20:.10;surface.normalTS=float3(0,0,1);surface.occlusion=1;surface.alpha=1;
            Light sun=GetMainLight(input.shadowCoord);
            surface.emission=albedo*smoothstep(.15,.6,i.color.a)*saturate(dot(-n,sun.direction))*.22*sun.color*sun.shadowAttenuation;
            return half4(MixFog(UniversalFragmentPBR(input,surface).rgb,i.fog),1);
        }
        ENDHLSL
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex shadowVert
            #pragma fragment depthFrag
            float3 _LightDirection;
            V shadowVert(A a)
            {
                V o=vert(a);o.positionCS=TransformWorldToHClip(ApplyShadowBias(o.world,normalize(o.normal),_LightDirection));
                #if UNITY_REVERSED_Z
                o.positionCS.z=min(o.positionCS.z,o.positionCS.w*UNITY_NEAR_CLIP_VALUE);
                #else
                o.positionCS.z=max(o.positionCS.z,o.positionCS.w*UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }
            half4 depthFrag(V i):SV_Target{clip(i.amount-.001);return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment depthFrag
            half4 depthFrag(V i):SV_Target{clip(i.amount-.001);return 0;}
            ENDHLSL
        }
    }
}
