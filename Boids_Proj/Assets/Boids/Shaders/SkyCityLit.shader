Shader "Boids/SkyCity/Limestone and Copper"
{
    Properties
    {
        _BaseColor("Tint",Color)=(1,1,1,1)
        _Metallic("Metallic",Range(0,1))=0
        _Smoothness("Smoothness",Range(0,1))=.25
        _Cloud("Cloud scattering",Range(0,1))=0
        _Foliage("Leaf transmission",Range(0,1))=0
        _Wind("Wind",Float)=0
        _SurfaceTex("Mineral pigment",2D)="white"{}
        _TextureAmount("Mineral detail",Range(0,1))=0
        _Relief("Mineral relief",Range(0,.2))=.04
        [PerRendererData] _BirdWing("Wing side",Float)=0
        [PerRendererData] _WingPhase("Wing phase",Float)=0
        [PerRendererData] _WingEffort("Wing effort",Float)=0
        [PerRendererData] _WingFold("Wing fold",Float)=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor;
        float _Metallic,_Smoothness,_Cloud,_Foliage,_Wind,_TextureAmount,_Relief;
        float _BirdWing,_WingPhase,_WingEffort,_WingFold;
        CBUFFER_END
        TEXTURE2D(_SurfaceTex);SAMPLER(sampler_SurfaceTex);
        struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 color:COLOR;float2 uv:TEXCOORD0;};
        struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float4 color:TEXCOORD2;float2 uv:TEXCOORD3;float fog:TEXCOORD4;};
        float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
        float noise3(float3 p)
        {
            float3 a=floor(p),f=frac(p);f=f*f*(3-2*f);
            return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),f.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),f.x),f.y),
                lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),f.x),lerp(hash(a+float3(0,1,1)),hash(a+1),f.x),f.y),f.z);
        }
        V vert(A a)
        {
            V o;
            if(abs(_BirdWing)>.5)
            {
                float span=abs(a.positionOS.x);float outer=smoothstep(.35,1.15,span);
                a.positionOS.y+=sin(_WingPhase-span*1.25)*.13*outer*_WingEffort;
                a.positionOS.z-=_WingFold*outer*.12;
                a.positionOS.x*=1-_WingFold*outer*.13;
            }
            o.world=TransformObjectToWorld(a.positionOS.xyz);
            if(_Wind>.001)
            {
                // The hoist stays pinned; traveling folds grow toward the free tip.
                float u=saturate(a.uv.x),v=a.uv.y,weight=pow(u,1.25);
                float phase=_Time.y*4.4-u*8.5+o.world.x*.19;
                float gust=.75+.25*sin(_Time.y*.73+o.world.x*.11);
                o.world.z+=_Wind*weight*gust*(.38*sin(phase)+.12*sin(phase*1.83+v*3.2));
                o.world.y+=_Wind*weight*(.19*sin(phase*.86+v*1.6)+.065*sin(phase*2.1-v*2.8)+.06*gust);
                o.world.x-=_Wind*weight*.085*(1-cos(phase));
            }
            o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(a.normalOS);
            o.color=a.color;o.uv=a.uv;o.fog=ComputeFogFactor(o.positionCS.z);return o;
        }
        ENDHLSL
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            half4 frag(V i,bool front:SV_IsFrontFace):SV_Target
            {
                float3 n=normalize(i.normal)*(front?1:-1);
                if(_Wind>.001)
                {
                    float3 clothNormal=normalize(cross(ddy(i.world),ddx(i.world)));
                    if(dot(clothNormal,n)<0)clothNormal=-clothNormal;
                    n=clothNormal;
                }
                float broad=noise3(i.world*1.8),fine=noise3(i.world*13.0);
                float3 albedo=i.color.rgb*_BaseColor.rgb*(.94+.09*broad+.04*fine);
                if(_TextureAmount>.01)
                {
                    float3 weights=pow(abs(n),4);weights/=max(.001,weights.x+weights.y+weights.z);
                    float3 pigment=SAMPLE_TEXTURE2D(_SurfaceTex,sampler_SurfaceTex,i.world.zy*.17).rgb*weights.x+
                        SAMPLE_TEXTURE2D(_SurfaceTex,sampler_SurfaceTex,i.world.xz*.17).rgb*weights.y+
                        SAMPLE_TEXTURE2D(_SurfaceTex,sampler_SurfaceTex,i.world.xy*.17).rgb*weights.z;
                    float height=dot(pigment,float3(.21,.72,.07));
                    albedo*=lerp(float3(1,1,1),clamp(pigment/.33,.25,1.9),_TextureAmount);
                    float3 dx=ddx(i.world),dy=ddy(i.world),r1=cross(dy,n),r2=cross(n,dx);
                    float det=dot(dx,r1);
                    n=normalize(n-(r1*ddx(height)+r2*ddy(height))/max(abs(det),.00001)*sign(det)*_Relief);
                }
                InputData input=(InputData)0;
                input.positionWS=i.world;input.normalWS=n;input.viewDirectionWS=SafeNormalize(GetWorldSpaceViewDir(i.world));
                input.shadowCoord=TransformWorldToShadowCoord(i.world);
                input.bakedGI=lerp(float3(.21,.23,.30),float3(.37,.44,.51),n.y*.5+.5);
                input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);input.shadowMask=1;
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=albedo;surface.metallic=_Metallic;surface.smoothness=_Smoothness;
                surface.normalTS=float3(0,0,1);surface.occlusion=lerp(.3,1,pow(i.color.a,1.5));surface.alpha=1;
                Light sun=GetMainLight(input.shadowCoord);
                surface.emission=albedo*_Foliage*saturate(dot(-n,sun.direction))*.20;
                float3 color=UniversalFragmentPBR(input,surface).rgb;
                if(_Cloud>.5)
                {
                    float lit=smoothstep(-.8,.9,dot(n,sun.direction));
                    float3 cloud=lerp(float3(.42,.46,.65),float3(1.22,1.10,.96),lit);
                    float rim=pow(1-saturate(dot(n,input.viewDirectionWS)),3);
                    cloud+=float3(.19,.15,.11)*rim;
                    cloud*=.97+.06*noise3(i.world*2.3);
                    color=cloud;
                }
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
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
            half4 shadowFrag(V i):SV_Target{return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragDepth
            half4 fragDepth(V i):SV_Target{return 0;}
            ENDHLSL
        }
    }
}
