Shader "Boids/Atelier/Pigment"
{
    Properties
    {
        _BaseColor("Pigment tint",Color)=(1,1,1,1)
        _PigmentTex("Paint surface",2D)="gray"{}
        _MainTex("Authored color",2D)="white"{}
        _GlazeTex("Symbolist pigment",2D)="gray"{}
        _Glaze("Colored glaze",Range(0,1))=.45
        _GlazeMidpoint("Paint median luminance",Float)=.22
        _BrushRelief("Impasto relief",Range(0,.1))=.018
        _TextureColor("Authored color amount",Range(0,1))=0
        _VertexColor("Sculpted pigment",Range(0,1))=1
        _PaintScale("Paint scale",Float)=.95
        _PaintStrength("Broken pigment",Range(0,1))=.30
        _Rim("Glazed edge",Range(0,1))=.18
        _Warmth("Transmitted warmth",Range(0,1))=.14
        _Transmission("Thin membrane light",Range(0,1))=0
        _Value("Pigment value",Float)=1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        HLSLINCLUDE
        #include "RedonCommon.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_PigmentTex);SAMPLER(sampler_PigmentTex);
        TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
        TEXTURE2D(_GlazeTex);SAMPLER(sampler_GlazeTex);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor;
        float _TextureColor,_VertexColor,_PaintScale,_PaintStrength,_Rim,_Warmth,_Value,_Glaze,_GlazeMidpoint,_BrushRelief,_Transmission;
        CBUFFER_END
        struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;float4 color:COLOR;};
        struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;float4 color:COLOR;};
        V vert(A a)
        {
            V o;o.world=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);
            o.normal=TransformObjectToWorldNormal(a.normalOS);o.uv=a.uv;o.color=a.color;return o;
        }
        ENDHLSL
        Pass
        {
            Name "Pigment" Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            half4 frag(V i,bool front:SV_IsFrontFace):SV_Target
            {
                float3 n=normalize(i.normal)*(front?1:-1);
                float3 eye=SafeNormalize(GetWorldSpaceViewDir(i.world));
                float2 uv=(i.world.xy+i.world.z*float2(.071,.039))*_PaintScale;
                float p=SAMPLE_TEXTURE2D(_PigmentTex,sampler_PigmentTex,uv).r;
                float wide=SAMPLE_TEXTURE2D(_PigmentTex,sampler_PigmentTex,uv*.27+.17).r;
                float fine=SAMPLE_TEXTURE2D(_PigmentTex,sampler_PigmentTex,uv*3.7+.39).r;
                float3 painted=SAMPLE_TEXTURE2D(_GlazeTex,sampler_GlazeTex,uv*.19+i.uv*.09).rgb;
                float lum=dot(painted,float3(.2126,.7152,.0722));
                float height=lum+p*.12;
                float3 dpdx=ddx(i.world),dpdy=ddy(i.world);
                float3 r1=cross(dpdy,n),r2=cross(n,dpdx);
                float determinant=dot(dpdx,r1);
                float3 surfaceGradient=(r1*ddx(height)+r2*ddy(height))/max(abs(determinant),.000001)*sign(determinant);
                n=normalize(n-surfaceGradient*_BrushRelief);
                float3 baseColor=_BaseColor.rgb*lerp(float3(1,1,1),i.color.rgb,_VertexColor);
                float3 tex=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv*float2(.35,.75)+float2(.32,.08)).rgb;
                float bodyValue=dot(baseColor,float3(.21,.72,.07));
                float texLum=dot(tex,float3(.21,.72,.07));
                float3 paintedBase=baseColor*clamp(pow(texLum/.12,.6),.45,1.5);
                paintedBase*=lerp(float3(1,1,1),tex/max(.025,texLum),.38);
                baseColor=lerp(baseColor,paintedBase,_TextureColor);
                float diffuse=dot(n,normalize(_RedonLight.xyz));
                float light=smoothstep(-.65,.95,diffuse+(p-.5)*.23);
                Light key=GetMainLight(TransformWorldToShadowCoord(i.world));
                light*=lerp(.48,1,key.shadowAttenuation);
                float3 shadow=baseColor*float3(.29,.42,.68);
                float3 lit=baseColor*float3(1.32,1.16,1.03);
                float3 color=lerp(shadow,lit,light);
                color*=lerp(1,pow(i.color.a,1.4),_VertexColor);
                float pigment=(p-.48)*.55+(wide-.48)*.35+(fine-.48)*.08;
                color*=1+pigment*_PaintStrength;
                color*=lerp(1,clamp(pow(max(.03,lum)/max(.03,_GlazeMidpoint),.85),.30,1.8),_Glaze);
                float3 chroma=painted/max(.035,lum);
                color*=lerp(float3(1,1,1),chroma,_Glaze*.35);
                float glaze=smoothstep(.38,.65,wide)*smoothstep(.3,.64,p);
                color=lerp(color,color*float3(1.10,.93,1.08),glaze*.19);
                float rim=pow(1-saturate(abs(dot(n,eye))),3);
                float warm=RedonFocus(i.world);
                color+=_RedonWarm.rgb*rim*_Rim*(.25+.65*warm)*smoothstep(.3,.67,p);
                color=lerp(color,color*float3(1.32,1.08,.86),warm*_Warmth);
                color+=baseColor*_RedonWarm.rgb*warm*_Warmth*.65;
                float membrane=saturate(dot(-n,normalize(_RedonLight.xyz)));
                membrane*=pow(saturate(i.uv.y),1.8)*(.3+.7*smoothstep(.1,.8,i.uv.x));
                color+=baseColor*float3(1.65,.88,.55)*membrane*_Transmission*(.65+.35*wide);
                float goldThread=smoothstep(.20,.43,tex.r-tex.b)*_TextureColor;
                color=lerp(color,float3(.62,.35,.12)*(0.55+light*.7),goldThread*.20);
                color*=lerp(.65,1,smoothstep(-1,5,i.world.y));
                color*=_Value;
                return half4(RedonFogColor(max(color,0),i.world),1);
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
                V o=vert(a);
                o.positionCS=TransformWorldToHClip(ApplyShadowBias(o.world,normalize(o.normal),_LightDirection));
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
            #pragma fragment depth
            half4 depth(V i):SV_Target{return 0;}
            ENDHLSL
        }
    }
}
