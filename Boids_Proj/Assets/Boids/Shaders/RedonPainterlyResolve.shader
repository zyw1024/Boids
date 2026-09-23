Shader "Boids/Redon/Painterly Resolve"
{
    Properties
    {
        _Radius("Paint neighborhood in pixels",Range(0,6))=3.5
        _Strength("Paint merge",Range(0,1))=.68
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            Name "VarianceWeightedPaint"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            float4 _BlitTexture_TexelSize;
            CBUFFER_START(UnityPerMaterial)
            float _Radius,_Strength;
            CBUFFER_END
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv=input.texcoord;
                float3 source=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv).rgb;
                float3 result=0;float total=0;
                // Soft variance weights avoid abrupt region switching as fish move.
                [unroll] for(int region=0;region<4;region++)
                {
                    float2 direction=float2(region%2==0?-1:1,region<2?-1:1);
                    float3 mean=0;float first=0,second=0;
                    [unroll] for(int y=0;y<4;y++) [unroll] for(int x=0;x<4;x++)
                    {
                        float2 offset=float2(x,y)*direction*_BlitTexture_TexelSize.xy*_Radius/3;
                        float3 c=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv+offset).rgb;
                        float lum=dot(c,float3(.2126,.7152,.0722));
                        mean+=c;first+=lum;second+=lum*lum;
                    }
                    mean/=16;first/=16;second/=16;
                    float variance=max(0,second-first*first);
                    float weight=rcp(1+variance*3500);
                    weight*=weight;
                    result+=mean*weight;total+=weight;
                }
                return half4(lerp(source,result/max(.00001,total),_Strength),1);
            }
            ENDHLSL
        }
    }
}
