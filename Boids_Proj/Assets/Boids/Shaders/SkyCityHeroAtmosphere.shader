Shader "Boids/SkyCity/Hanging Gardens Clouds"
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
        #include "SkyCityDaylight.hlsl"
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
                float ceiling=-5+weather*32+pow(bank,2)*86;
                // Cities are centered at (42,42) in each 168 m district. Keep the
                // inhabited terraces clear, even as weather drifts across the grid.
                float2 cityDelta=frac((absolute.xz-42+84)/168)*168-84;
                float clearing=smoothstep(39,67,length(cityDelta));
                ceiling=lerp(1,ceiling,clearing);
                if(world.y<-58||world.y>ceiling)return 0;
                float h=remap(world.y,-58,ceiling);
                float profile=smoothstep(0,.16,h)*(1-smoothstep(.53,1,h));
                float3 p=absolute-_Wind.xyz*_Time.y;
                float4 n=SAMPLE_TEXTURE3D_LOD(_NoiseTex,sampler_NoiseTex,p*.011,0);
                float base=n.r*.78+n.g*.22;
                float d=remap(base*profile,1-(_Coverage+weather*.17),1);
                if(detail&&d>.002)
                {
                    float4 fine=SAMPLE_TEXTURE3D_LOD(_NoiseTex,sampler_NoiseTex,p*.067+_Time.y*float3(.009,.002,-.004),0);
                    d=remap(d,(1-(fine.g*.55+fine.b*.3+fine.a*.15))*_Detail,1);
                }
                return d*5;
        }
        float bank(float3 p,float3 center,float3 scale)
        {
            float3 q=(p-center)/scale;
            float d=length(q-float3(-.45,-.15,0))-.59;
            d=min(d,length(q-float3(.05,.18,-.08))-.68);
            d=min(d,length(q-float3(.52,-.04,.03))-.54);
            d=min(d,length(q-float3(-.08,.53,.08))-.46);
            d=min(d,length(q-float3(.30,-.42,.08))-.57);
            return 1-smoothstep(-.16,.16,d);
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
            // Distort the silhouette at two scales before the bank union. The
            // density noise alone only textures a smooth ellipsoid's interior.
            float3 sculpt=SAMPLE_TEXTURE3D_LOD(_NoiseTex,sampler_NoiseTex,p*.0045,0).rgb;
            float3 billow=SAMPLE_TEXTURE3D_LOD(_NoiseTex,sampler_NoiseTex,p*.018+float3(.17,.31,.09),0).rgb;
            shape+=(sculpt-.48)*14+(billow-.48)*2.0;
            float weather=SAMPLE_TEXTURE3D_LOD(_NoiseTex,sampler_NoiseTex,float3(shape.x*.0035,.17,shape.z*.0035),0).r;
            float ceiling=-17+weather*18;
            float h=remap(shape.y,-60,ceiling);
            float profile=shape.y>ceiling?0:smoothstep(0,.12,h)*(1-smoothstep(.36,1,h));
            profile=max(profile,bank(shape,float3(-69,7,125),float3(28,19,24)));
            profile=max(profile,bank(shape,float3(-45,-3,78),float3(18,12,18)));
            profile=max(profile,bank(shape,float3(88,20,140),float3(31,22,27)));
            profile=max(profile,bank(shape,float3(47,-1,85),float3(20,12,20)));
            profile=max(profile,bank(shape,float3(-18,-3,120),float3(18,9,15)));
            profile=max(profile,bank(shape,float3(16,-7,98),float3(17,8,17)));
            profile=max(profile,bank(shape,float3(9,-5,175),float3(49,14,26)));
            profile=max(profile,bank(shape,float3(-72,31,168),float3(32,26,24)));
            profile=max(profile,bank(shape,float3(110,45,190),float3(34,16,25)));
            profile=max(profile,bank(shape,float3(43,23,238),float3(28,8,17)));
            profile=max(profile,bank(shape,float3(3,31,245),float3(19,6,14)));
            profile=max(profile,bank(shape,float3(-43,-20,-5),float3(25,11,23)));
            profile=max(profile,bank(shape,float3(48,-21,5),float3(30,13,25)));
            if(profile<.001)return 0;
            float4 n=SAMPLE_TEXTURE3D_LOD(_NoiseTex,sampler_NoiseTex,p*.024,0);
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
                float3 light=GetMainLight().direction;
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
                    float3 sunTint=GetMainLight().color;
                    float3 lightColor=_ShadeColor.rgb*(.9+ambient)+_SunColor.rgb*sunTint*(sun*phase+silver);
                    float haze=1-exp(-distance*.0030);
                    float3 horizon=SkyCitySkyRadiance(ray);
                    lightColor=lerp(lightColor,horizon,haze*.45);
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
