Shader "Boids/Redon/Painted Surface"
{
    Properties
    {
        _BaseColor("Body pigment", Color) = (.36,.4,.52,1)
        _ShadeColor("Shadow pigment", Color) = (.10,.17,.25,1)
        _LightColor("Light pigment", Color) = (.69,.62,.67,1)
        _PigmentTex("Scumbled pigment", 2D) = "gray" {}
        _ColorTex("Layered color underpainting", 2D) = "white" {}
        _Underpaint("Color underpainting strength", Range(0,1)) = .7
        _ColorUV("Use authored petal UV for color", Range(0,1)) = 0
        _BrushScale("Brush scale", Float) = 1.5
        _Scumble("Broken color strength", Range(0,1)) = .48
        _Rim("Selective warm edge", Range(0,1)) = .16
        _Veins("Petal veins", Range(0,1)) = 0
        _WorldUV("World-space rock pigment", Range(0,1)) = 0
        _Seed("Pigment offset", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        HLSLINCLUDE
        #include "RedonCommon.hlsl"
        TEXTURE2D(_PigmentTex); SAMPLER(sampler_PigmentTex);
        TEXTURE2D(_ColorTex); SAMPLER(sampler_ColorTex);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor, _ShadeColor, _LightColor;
        float _BrushScale, _Scumble, _Rim, _Veins, _WorldUV, _Seed, _Underpaint, _ColorUV;
        CBUFFER_END
        struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; float4 color:COLOR; };
        struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; float4 color:COLOR; };
        Varyings vert(Attributes a)
        {
            Varyings o;
            o.positionWS = TransformObjectToWorld(a.positionOS.xyz);
            o.positionCS = TransformWorldToHClip(o.positionWS);
            o.normalWS = TransformObjectToWorldNormal(a.normalOS);
            o.uv = a.uv; o.color = a.color;
            return o;
        }
        ENDHLSL
        Pass
        {
            Name "PaintedColor"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(Varyings i, bool front:SV_IsFrontFace):SV_Target
            {
                float2 uv = lerp(i.uv, i.positionWS.xy*.23+i.positionWS.z*.017, _WorldUV);
                uv = uv*_BrushScale + float2(_Seed*.137,_Seed*.217);
                float p = SAMPLE_TEXTURE2D(_PigmentTex,sampler_PigmentTex,uv).r;
                float wide = SAMPLE_TEXTURE2D(_PigmentTex,sampler_PigmentTex,uv*.43+float2(.17,.41)).r;
                float fine = SAMPLE_TEXTURE2D(_PigmentTex,sampler_PigmentTex,uv*3.6).r;
                float px = SAMPLE_TEXTURE2D_LOD(_PigmentTex,sampler_PigmentTex,uv+float2(.035,0),3).r;
                float py = SAMPLE_TEXTURE2D_LOD(_PigmentTex,sampler_PigmentTex,uv+float2(0,.035),3).r;
                float coarse = SAMPLE_TEXTURE2D_LOD(_PigmentTex,sampler_PigmentTex,uv,3).r;
                float3 n = normalize(i.normalWS) * (front ? 1 : -1);
                n = normalize(n+float3(coarse-px,py-coarse,0)*_RedonRelief*5*_RedonPaint);
                float light = saturate(dot(n,normalize(_RedonLight.xyz))*.22+.48);
                float patch=RedonNoise(uv*float2(7,11)+RedonNoise(uv*4)*2);
                light = saturate(light + (patch-.48)*.27*_RedonPaint);
                float3 color = lerp(_ShadeColor.rgb,_BaseColor.rgb,smoothstep(.08,.70,light));
                color = lerp(color,_LightColor.rgb,smoothstep(.67,1.0,light)*.78);
                float pigment = (p-.46)*.65+(fine-.46)*.015;
                color *= 1 + pigment*_Scumble*_RedonPaint;
                // Exposed underpainting stays colored rather than becoming a white noise layer.
                float exposed = smoothstep(.40,.65,wide)*(1-smoothstep(.38,.63,p));
                color = lerp(color,_ShadeColor.rgb*.9,exposed*.48*_Scumble*_RedonPaint);
                float chroma=smoothstep(.3,.64,wide);
                color=lerp(color,lerp(_ShadeColor.rgb,_LightColor.rgb,chroma),.12*_RedonPaint);
                float3 under=SAMPLE_TEXTURE2D(_ColorTex,sampler_ColorTex,lerp(uv*.77,i.uv,_ColorUV)).rgb;
                float value=max(.25,dot(_BaseColor.rgb,float3(.3,.5,.2))*3.4);
                under*=value*(.6+light*.7);
                color=lerp(color,under,saturate(_Underpaint*_RedonPaint));
                float veinAngle = (i.uv.x-.5)*27 + sin(i.uv.y*4+i.uv.x*8)*.6;
                float veins = pow(saturate(1-abs(sin(veinAngle))),32);
                veins *= smoothstep(.15,.4,i.uv.y)*(1-smoothstep(.92,1,i.uv.y));
                color = lerp(color,lerp(_LightColor.rgb,_RedonWarm.rgb,.4),veins*_Veins*(.15+.25*p));
                float sideEdge=1-smoothstep(.005,.025,min(i.uv.x,1-i.uv.x));
                float edge=sideEdge*smoothstep(.18,.4,i.uv.y)*smoothstep(.33,.66,p);
                color += _RedonWarm.rgb*edge*_Veins*.28;
                float3 v = SafeNormalize(GetWorldSpaceViewDir(i.positionWS));
                float rim = pow(1-saturate(abs(dot(n,v))),3.0);
                float broken = smoothstep(.4,.65,p);
                color += _RedonWarm.rgb*rim*_Rim*broken*(.5+RedonFocus(i.positionWS));
                color *= i.color.rgb;
                return half4(RedonFogColor(max(color,0),i.positionWS),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment depthFrag
            half4 depthFrag(Varyings i):SV_Target { return 0; }
            ENDHLSL
        }
    }
}
