Shader "Boids/SkyCity/Hanging Gardens Clouds"
{
    Properties
    {
        _NoiseTex("Drifting detail",3D)="white"{}
        _CloudField("Sculpted density and light transport",3D)="black"{}
        _FieldMin("Cloud bounds minimum",Vector)=(-160,-65,-55,0)
        _FieldSize("Cloud bounds extent",Vector)=(350,165,350,0)
        _Density("Extinction",Float)=.58
        _Detail("Fine edge erosion",Range(0,.3))=.13
        _SunColor("Sun scattering",Color)=(1.03,1.01,.98,1)
        _ShadeColor("Sky scattering",Color)=(.49,.54,.67,1)
        _Steps("Ray samples",Range(64,1536))=1536
        _CloudTime("Preview time, negative for live animation",Float)=-1
        _Endless("Continuous world cloud sea",Float)=0
    }
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline"}
        Cull Off ZWrite Off ZTest Always
        HLSLINCLUDE
        #pragma target 4.5
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "SkyCityDaylight.hlsl"
        TEXTURE3D(_NoiseTex);SAMPLER(sampler_NoiseTex);
        TEXTURE3D(_CloudField);SAMPLER(sampler_CloudField);
        TEXTURE2D(_SkyCloudVolume);SAMPLER(sampler_SkyCloudVolume);
        float4 _CloudTargetSize;
        float4 _SkyWorldOffset;
        CBUFFER_START(UnityPerMaterial)
        float4 _FieldMin,_FieldSize,_SunColor,_ShadeColor;
        float _Density,_Detail,_Steps,_CloudTime,_Endless;
        CBUFFER_END
        struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
        V vert(uint id:SV_VertexID){V o;o.positionCS=GetFullScreenTriangleVertexPosition(id);o.uv=GetFullScreenTriangleTexCoord(id);return o;}
        float3 drift()
        {
            // Bounded broad translation preserves composition during long runs.
            float t=_CloudTime<0?_Time.y:_CloudTime;
            return float3(sin(t*.012)*13,sin(t*.025)*.65,sin(t*.010)*8);
        }
        float4 field(float3 p)
        {
            float3 uv=(p-_FieldMin.xyz)/_FieldSize.xyz;
            float4 result=float4(0,0,1,1);
            if(_Endless>.5)
            {
                // 70 m overlaps blend neighbouring 280 m cells. 84000 m, the
                // floating-origin noise period, is an exact multiple of 280.
                float2 local=p.xz+_SkyWorldOffset.xz-_FieldMin.xz;
                local-=floor(local/280)*280;
                float2 blend=smoothstep(0,70,local);
                float3 a=float3(local.x/_FieldSize.x,uv.y,local.y/_FieldSize.z);
                float3 dx=float3(280/_FieldSize.x,0,0),dz=float3(0,0,280/_FieldSize.z);
                float4 f00=SAMPLE_TEXTURE3D_LOD(_CloudField,sampler_CloudField,a,0);
                float4 f10=SAMPLE_TEXTURE3D_LOD(_CloudField,sampler_CloudField,a+dx,0);
                float4 f01=SAMPLE_TEXTURE3D_LOD(_CloudField,sampler_CloudField,a+dz,0);
                float4 f11=SAMPLE_TEXTURE3D_LOD(_CloudField,sampler_CloudField,a+dx+dz,0);
                result=lerp(lerp(f11,f01,blend.x),lerp(f10,f00,blend.x),blend.y);
                // Never average the empty-space hint across an occupied cell.
                result.a=min(min(f00.a,f10.a),min(f01.a,f11.a));
            }
            else if(all(uv>=0)&&all(uv<=1))result=SAMPLE_TEXTURE3D_LOD(_CloudField,sampler_CloudField,uv,0);
            return result;
        }
        float phaseHG(float mu,float g)
        {
            return (1-g*g)/pow(max(.01,1+g*g-2*g*mu),1.5);
        }
        ENDHLSL
        Pass
        {
            Name "Integrate sculpted cloud sea"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(V i):SV_Target
            {
                float raw=SampleSceneDepth(i.uv);
                #if !UNITY_REVERSED_Z
                raw=lerp(UNITY_NEAR_CLIP_VALUE,1,raw);
                #endif
                float3 farPoint=ComputeWorldSpacePosition(i.uv,raw,UNITY_MATRIX_I_VP);
                float3 origin=GetCameraPositionWS(),ray=normalize(farPoint-origin);
                float maxDistance=min(length(farPoint-origin),460);
                float3 motion=drift();
                float3 bmin=_FieldMin.xyz+motion,bmax=bmin+_FieldSize.xyz;
                if(_Endless>.5){bmin.xz=origin.xz-461;bmax.xz=origin.xz+461;}
                float3 inv=rcp(ray),a=(bmin-origin)*inv,b=(bmax-origin)*inv;
                float3 near3=min(a,b),far3=max(a,b);
                float entry=max(0,max(near3.x,max(near3.y,near3.z)));
                float end=min(maxDistance,min(far3.x,min(far3.y,far3.z)));
                if(end<=entry)return 0;
                int steps=clamp((int)_Steps,64,1536);
                // Bound optical integration spacing inside dense billows. Spanning the
                // whole box with a fixed sample count produced visible shadow terraces.
                float step=.32;
                uint2 pixel=(uint2)floor(i.uv*_CloudTargetSize.xy);
                uint bits=pixel.x*1973u+pixel.y*9277u+89173u;bits=(bits<<13)^bits;
                float jitter=(bits*(bits*bits*15731u+789221u)+1376312589u)/4294967295.0;
                float mu=dot(ray,GetMainLight().direction);
                float phase=.62*phaseHG(mu,.50)+.38*phaseHG(mu,-.20);
                float3 sunTint=lerp(GetMainLight().color,float3(1.7,1.7,1.7),.32);
                float3 sky=SkyCitySkyRadiance(ray);
                float transmittance=1;float3 color=0;
                float distance=entry+jitter*step;
                [loop]for(int j=0;j<1536;j++)
                {
                    if(j>=steps||distance>=end||transmittance<.006)break;
                    float3 p=origin+ray*distance-motion;
                    float t=_CloudTime<0?_Time.y:_CloudTime;
                    float3 billow=SAMPLE_TEXTURE3D_LOD(_NoiseTex,sampler_NoiseTex,p*.021+float3(t*.003,0,t*.001),0).rgb;
                    p+=(billow-.5)*1.4;
                    // Density, solar optical depth in two channels and empty
                    // distance share one fetch instead of repeated light marches.
                    float4 f=field(p);
                    float stride=step;
                    if(f.r<.003){distance+=max(stride,min(f.a*24,9));continue;}
                    float4 noise=SAMPLE_TEXTURE3D_LOD(_NoiseTex,sampler_NoiseTex,p*.045+float3(t*.005,0,-t*.003),0);
                    float density=saturate((f.r-.16-(1-noise.g)*_Detail)*3.0);
                    if(density<.01){distance+=max(stride,.8);continue;}
                    float tau=dot(f.gb,float2(1,1.0/255))*24;
                    float direct=exp(-tau);
                    float multiple=.18*exp(-tau*.32)+.06*exp(-tau*.07);
                    float powder=lerp(.85,1.15,1-exp(-density*4));
                    float skyVisibility=exp(-tau*.04)*lerp(.6,1,saturate((p.y+45)/80));
                    float3 illumination=_ShadeColor.rgb*(.25+.35*skyVisibility)+
                        _SunColor.rgb*sunTint*(direct*phase*.42+multiple)*powder;
                    float haze=1-exp(-distance*.0019);
                    illumination=lerp(illumination,sky,haze*.60);
                    float alpha=1-exp(-density*_Density*stride);
                    color+=illumination*alpha*transmittance;
                    transmittance*=1-alpha;distance+=stride;
                }
                return half4(color,1-transmittance);
            }
            ENDHLSL
        }
        Pass
        {
            Name "Depth aware cloud composition"
            Blend One OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment composite
            half4 composite(V i):SV_Target
            {
                float centerDepth=LinearEyeDepth(SampleSceneDepth(i.uv),_ZBufferParams);
                half4 total=0;float totalWeight=0;
                [unroll]for(int y=-1;y<=1;y++)[unroll]for(int x=-1;x<=1;x++)
                {
                    float2 uv=i.uv+float2(x,y)*_CloudTargetSize.zw;
                    float otherDepth=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams);
                    float weight=(x==0?2:1)*(y==0?2:1)*exp(-abs(centerDepth-otherDepth)*2);
                    total+=SAMPLE_TEXTURE2D(_SkyCloudVolume,sampler_SkyCloudVolume,uv)*weight;totalWeight+=weight;
                }
                return total/max(.001,totalWeight);
            }
            ENDHLSL
        }
    }
}
