Shader "Boids/SkyCity/Atmosphere Raymarch"
{
    Properties
    {
        _NoiseTex("Three dimensional cloud noise",3D)="white"{}
        _Density("Extinction",Float)=.58
        _Coverage("Coverage",Range(0,1))=.52
        _Detail("Edge erosion",Range(0,1))=.10
        _Wind("Wind velocity",Vector)=(.95,.025,.22,0)
        _SunColor("Sun scattering",Color)=(2.1,1.77,1.42,1)
        _ShadeColor("Sky scattering",Color)=(.22,.29,.48,1)
        _InfiniteMode("Endless cloud field",Float)=0
        _Steps("Ray samples",Range(32,224))=224
        _LightSteps("Light samples",Range(3,7))=7
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
        TEXTURE3D(_NoiseTex);SAMPLER(sampler_NoiseTex);
        TEXTURE2D(_SkyCloudVolume);SAMPLER(sampler_SkyCloudVolume);
        float4 _CloudTargetSize;
        float4 _SkyWorldOffset;
        CBUFFER_START(UnityPerMaterial)
        float _Density,_Coverage,_Detail;float4 _Wind,_SunColor,_ShadeColor;
        float _InfiniteMode,_Steps,_LightSteps;
        CBUFFER_END
        struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
        V vert(uint id:SV_VertexID){V o;o.positionCS=GetFullScreenTriangleVertexPosition(id);o.uv=GetFullScreenTriangleTexCoord(id);return o;}
        float remap(float v,float lo,float hi){return saturate((v-lo)/max(.001,hi-lo));}
        float infiniteDensity(float3 world,bool detail)
        {
                float3 absolute=world+_SkyWorldOffset.xyz;
                float weather=SAMPLE_TEXTURE3D_LOD(_NoiseTex,sampler_NoiseTex,float3(absolute.x*.0035,.17,absolute.z*.0035),0).r;
                // Large drifting cloud towers occupy the spaces between cities.
                // The low layer stays open around terraces; tall banks frame silhouettes.
                float2 drift=absolute.xz-_Wind.xz*_Time.y*.32;
                float bank=saturate(sin(drift.x*.018+sin(drift.y*.013))*cos(drift.y*.019-drift.x*.004));
                float ceiling=-5+weather*32+pow(bank,3)*80;
                if(world.y<-58||world.y>ceiling)return 0;
                float h=remap(world.y,-58,ceiling);
                float profile=smoothstep(0,.16,h)*(1-smoothstep(.53,1,h));
                float3 p=absolute-_Wind.xyz*_Time.y;
                float4 n=SAMPLE_TEXTURE3D_LOD(_NoiseTex,sampler_NoiseTex,p*.015,0);
                float base=n.r*.78+n.g*.22;
                float d=remap(base*profile,1-(_Coverage+weather*.17),1);
                if(detail&&d>.002)
                {
                    float4 fine=SAMPLE_TEXTURE3D_LOD(_NoiseTex,sampler_NoiseTex,p*.067+_Time.y*float3(.009,.002,-.004),0);
                    d=remap(d,(1-(fine.g*.55+fine.b*.3+fine.a*.15))*_Detail,1);
                }
                return d*5;
        }
        float cloudDensity(float3 world,bool detail)
        {
            #if defined(SKY_INFINITE_CLOUDS)
            return infiniteDensity(world,detail);
            #else
            // Near banks travel faster than the distant towers, with gentle vertical rolling.
            float shear=lerp(1.25,.60,saturate((world.z-10)/180));
            float3 p=world-_Wind.xyz*_Time.y*shear;
            p.y+=sin(world.x*.09+world.z*.045-_Time.y*.28)*1.15;
            p.z+=sin(world.y*.13+world.x*.06-_Time.y*.18)*.8;
            float3 shape=world-float3(sin(_Time.y*.023)*14,sin(_Time.y*.017)*.8,cos(_Time.y*.019)*5);
            float weather=SAMPLE_TEXTURE3D_LOD(_NoiseTex,sampler_NoiseTex,float3(shape.x*.0035,.17,shape.z*.0035),0).r;
            float2 l=(shape.xz-float2(-54,95))/float2(29,34);
            float2 r=(shape.xz-float2(67,116))/float2(38,45);
            float towers=47*exp(-dot(l,l)*1.4)+58*exp(-dot(r,r)*1.5);
            float ceiling=-13+weather*24+towers;
            ceiling-=22*(1-smoothstep(5,52,shape.z));
            float h=remap(shape.y,-60,ceiling);
            if(shape.y<-60||shape.y>ceiling)return 0;
            float profile=smoothstep(0,.16,h)*(1-smoothstep(.50,1,h));
            float4 n=SAMPLE_TEXTURE3D_LOD(_NoiseTex,sampler_NoiseTex,p*.015,0);
            float worley=n.g*.55+n.b*.30+n.a*.15;
            float base=remap(n.r*.78+n.g*.22,-(1-worley)*.10,1);
            float coverage=_Coverage+weather*.17;
            float d=remap(base*profile,1-coverage,1);
            if(detail&&d>.002)
            {
                float4 fine=SAMPLE_TEXTURE3D_LOD(_NoiseTex,sampler_NoiseTex,p*.067+_Time.y*float3(.009,.002,-.004),0);
                float erosion=(1-(fine.g*.55+fine.b*.3+fine.a*.15))*_Detail;
                d=remap(d,erosion,1);
            }
            return d*5;
            #endif
        }
        float sunlight(float3 p,float3 direction)
        {
            float optical=0,distance=0,step=1.5;
            [unroll]for(int j=0;j<7;j++)
            {
                if(j>=(int)_LightSteps)break;
                distance+=step*.5;
                optical+=cloudDensity(p+direction*distance,false)*step;
                distance+=step*.5;step*=1.55;
            }
            float tau=optical*_Density;
            return exp(-tau)*.72+exp(-tau*.27)*.20+exp(-tau*.075)*.08;
        }
        ENDHLSL
        Pass
        {
            Name "Integrate cloud density"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _ SKY_INFINITE_CLOUDS
            half4 frag(V i):SV_Target
            {
                float raw=SampleSceneDepth(i.uv);
                #if !UNITY_REVERSED_Z
                raw=lerp(UNITY_NEAR_CLIP_VALUE,1,raw);
                #endif
                float3 farPoint=ComputeWorldSpacePosition(i.uv,raw,UNITY_MATRIX_I_VP);
                float3 origin=GetCameraPositionWS(),ray=normalize(farPoint-origin);
                float maxDistance=min(length(farPoint-origin),_InfiniteMode>.5?580:330);
                float3 bmin=float3(-160,-60,-42),bmax=float3(160,84,275);
                if(_InfiniteMode>.5){bmin=float3(origin.x-580,-60,origin.z-580);bmax=float3(origin.x+580,100,origin.z+580);}
                float3 inv=rcp(ray),a=(bmin-origin)*inv,b=(bmax-origin)*inv;
                float3 near3=min(a,b),far3=max(a,b);
                float entry=max(0,max(near3.x,max(near3.y,near3.z)));
                float end=min(maxDistance,min(far3.x,min(far3.y,far3.z)));
                if(end<=entry)return 0;
                int steps=clamp((int)_Steps,32,224);
                float step=(end-entry)/steps;
                uint2 pixel=(uint2)floor(i.uv*_CloudTargetSize.xy);
                uint bits=pixel.x*1973u+pixel.y*9277u+89173u;bits=(bits<<13)^bits;
                float jitter=(bits*(bits*bits*15731u+789221u)+1376312589u)/4294967295.0;
                float3 light=normalize(float3(.65,.65,.45));
                float mu=dot(ray,light);float phase=.6+.4*pow(saturate(mu),5);
                float transmittance=1;float3 color=0;
                [loop]for(int j=0;j<steps;j++)
                {
                    float distance=entry+(j+jitter)*step;
                    float3 p=origin+ray*distance;
                    float d=cloudDensity(p,true);if(d<.001)continue;
                    float sun=sunlight(p,light);
                    float silver=pow(saturate(mu),9)*sun*.75;
                    float ambient=saturate((p.y+20)/40)*.17;
                    float3 lightColor=_ShadeColor.rgb*(.9+ambient)+_SunColor.rgb*(sun*phase+silver);
                    float haze=1-exp(-distance*.0030);
                    lightColor=lerp(lightColor,float3(.63,.70,.86),haze*.55);
                    float alpha=1-exp(-d*step*_Density);
                    color+=lightColor*alpha*transmittance;transmittance*=1-alpha;
                    if(transmittance<.008)break;
                }
                return half4(color,1-transmittance);
            }
            ENDHLSL
        }
        Pass
        {
            Name "Blend volumetric atmosphere"
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
