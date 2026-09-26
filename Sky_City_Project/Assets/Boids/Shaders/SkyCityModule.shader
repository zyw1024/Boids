Shader "Boids/SkyCity/Modular Architecture"
{
    Properties
    {
        _Reveal("Arrival",Range(0,1))=1
        _BridgeReveal("Connected bridge ends",Vector)=(1,1,1,1)
        _StoneTex("Mineral strata",2D)="white"{}
        _ArchitectureTex("Honed limestone",2D)="white"{}
        _StoneRelief("Limestone surface relief",Range(0,.3))=.065
        _StoneVariation("Limestone variation",Range(0,1))=.32
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float _Reveal;
        float4 _BridgeReveal;
        float _StoneRelief,_StoneVariation;
        CBUFFER_END
        float4 _SkyWorldOffset;
        TEXTURE2D(_StoneTex);SAMPLER(sampler_StoneTex);
        TEXTURE2D(_ArchitectureTex);SAMPLER(sampler_ArchitectureTex);
        struct A { float3 p:POSITION; float3 n:NORMAL; float4 c:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
        struct V { float4 p:SV_POSITION; float3 world:TEXCOORD0; float3 n:TEXCOORD1; float4 c:TEXCOORD2; float fog:TEXCOORD3; float connected:TEXCOORD4; };
        V vert(A a)
        {
            UNITY_SETUP_INSTANCE_ID(a);
            if(a.c.a>.92)
            {
                float phase=unity_ObjectToWorld._m03*.77+unity_ObjectToWorld._m23*.39;
                float beat=sin(_Time.y*(17.5+sin(phase)*1.8)+phase);
                float glide=smoothstep(.55,.8,sin(_Time.y*.7+phase));
                a.p.y+=abs(a.p.x)*(.43*beat*(1-glide)-.10*glide);
                a.p.z-=abs(a.p.x)*.07*sin(_Time.y*17+phase-abs(a.p.x));
            }
            V o; o.world=TransformObjectToWorld(a.p); o.n=TransformObjectToWorldNormal(a.n);o.c=a.c;
            o.connected=a.uv.x< -9 ? _BridgeReveal[(int)(-a.uv.x-9.5)] : 1;
            if(abs(a.c.a-.4)<.03)
            {
                float flutter=sin(_Time.y*1.6+a.p.x*.53+a.p.z*.42)*sin(a.p.y*1.8);
                o.world.xz+=float2(.09,.055)*flutter;
            }
            if(a.c.a>.75 && a.c.a<.92)
            {
                float u=saturate(a.uv.x),phase=_Time.y*4.4-u*8.5+a.p.x*.19;
                o.world.z+=pow(u,1.25)*(.30*sin(phase)+.09*sin(phase*1.83+a.uv.y*3));
                o.world.y+=u*.16*sin(phase*.86+a.uv.y*1.6);
            }
            o.p=TransformWorldToHClip(o.world);o.fog=ComputeFogFactor(o.p.z);return o;
        }
        void arrival(float2 pixel,float connected)
        {
            float noise=frac(52.9829189*frac(dot(pixel,float2(.06711056,.00583715))));
            clip(min(_Reveal,connected)-max(noise,.0001));
        }
        ENDHLSL
        Pass
        {
            Name "Architecture" Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            half4 frag(V i,bool front:SV_IsFrontFace):SV_Target
            {
                arrival(i.p.xy,i.connected);
                half3 n=normalize(i.n)*(front?1:-1);
                if(i.c.a>.72) { half3 fold=normalize(cross(ddy(i.world),ddx(i.world)));n=dot(fold,n)<0?-fold:fold; }
                float3 p=i.world+_SkyWorldOffset.xyz;
                half3 albedo=i.c.rgb;
                half rock=1-step(.035,abs(i.c.a-.1));
                half copper=1-step(.06,abs(i.c.a-.2));half gold=1-step(.06,abs(i.c.a-.6));
                half stone=1-step(.035,i.c.a);
                half leaf=1-step(.06,abs(i.c.a-.4));
                half3 blend=pow(abs(n),4);blend/=max(.001,blend.x+blend.y+blend.z);
                float scale=lerp(.18,.10,rock);
                half3 mineral;
                if(stone>.5)
                    mineral=SAMPLE_TEXTURE2D(_ArchitectureTex,sampler_ArchitectureTex,p.zy*.65).rgb*blend.x
                        +SAMPLE_TEXTURE2D(_ArchitectureTex,sampler_ArchitectureTex,p.xz*.65).rgb*blend.y
                        +SAMPLE_TEXTURE2D(_ArchitectureTex,sampler_ArchitectureTex,p.xy*.65).rgb*blend.z;
                else mineral=SAMPLE_TEXTURE2D(_StoneTex,sampler_StoneTex,p.zy*scale).rgb*blend.x
                    +SAMPLE_TEXTURE2D(_StoneTex,sampler_StoneTex,p.xz*scale).rgb*blend.y
                    +SAMPLE_TEXTURE2D(_StoneTex,sampler_StoneTex,p.xy*scale).rgb*blend.z;
                float height=dot(mineral,half3(.3,.5,.2));
                albedo*=lerp(1,lerp(.43,1.75,saturate(height*1.5)),rock);
                albedo*=lerp(1,clamp(height/.72,.65,1.15),stone*_StoneVariation);
                // Oxide is matte; exposed copper catches the sky in a softer highlight.
                half patina=smoothstep(.15,.52,height);
                albedo=lerp(albedo,lerp(albedo*half3(1.06,.91,.77),albedo*half3(.86,1.07,1.11),patina),copper*.45);
                float relief=rock*.16+stone*_StoneRelief+copper*.008;
                float3 dx=ddx(i.world),dy=ddy(i.world),r1=cross(dy,n),r2=cross(n,dx);
                float det=dot(dx,r1);
                float3 gradient=(r1*ddx(height)+r2*ddy(height))*sign(det)/max(abs(det),.00001);
                n=normalize(n-gradient*relief);
                InputData input=(InputData)0;input.positionWS=i.world;input.normalWS=n;
                input.viewDirectionWS=SafeNormalize(GetWorldSpaceViewDir(i.world));input.shadowCoord=TransformWorldToShadowCoord(i.world);
                input.bakedGI=SampleSH(n);
                input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);input.shadowMask=1;
                SurfaceData s=(SurfaceData)0;s.albedo=albedo;s.alpha=1;s.occlusion=1;s.normalTS=half3(0,0,1);
                s.metallic=copper*lerp(.60,.24,patina)+gold*.72;
                s.smoothness=.16+stone*height*.16+copper*lerp(.32,.12,patina)+gold*.4;
                Light sun=GetMainLight(input.shadowCoord);
                s.emission=albedo*leaf*sun.color*saturate(dot(-n,sun.direction))*.12*sun.shadowAttenuation;
                return half4(MixFog(UniversalFragmentPBR(input,s).rgb,i.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster" Tags { "LightMode"="ShadowCaster" } ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment frag
            #pragma multi_compile_instancing
            float3 _LightDirection;
            V shadowVert(A a)
            {
                V o=vert(a);o.p=TransformWorldToHClip(ApplyShadowBias(o.world,normalize(o.n),_LightDirection));
                #if UNITY_REVERSED_Z
                o.p.z=min(o.p.z,o.p.w*UNITY_NEAR_CLIP_VALUE);
                #else
                o.p.z=max(o.p.z,o.p.w*UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }
            half4 frag(V i):SV_Target {arrival(i.p.xy,i.connected);return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags { "LightMode"="DepthOnly" } ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            half4 frag(V i):SV_Target {arrival(i.p.xy,i.connected);return 0;}
            ENDHLSL
        }
    }
}
