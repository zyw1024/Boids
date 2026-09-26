#ifndef BOIDS_REDON_COMMON_INCLUDED
#define BOIDS_REDON_COMMON_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

float _RedonPaint, _RedonRelief;
float4 _RedonWater, _RedonUpperWater, _RedonWarm;
float4 _RedonFocus, _RedonExtent, _RedonFog, _RedonLight;

float RedonHash(float2 p)
{
    return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453);
}
float RedonNoise(float2 p)
{
    float2 i = floor(p), f = frac(p);
    f = f*f*(3.0-2.0*f);
    return lerp(lerp(RedonHash(i),RedonHash(i+float2(1,0)),f.x),
                lerp(RedonHash(i+float2(0,1)),RedonHash(i+1),f.x),f.y);
}
float RedonFocus(float3 p)
{
    float2 d = (p.xy - _RedonFocus.xy) / max(_RedonExtent.xy,0.1);
    return exp(-dot(d,d)*1.15);
}
float3 RedonAtmosphere(float3 p)
{
    float3 water = lerp(_RedonWater.rgb, _RedonUpperWater.rgb, smoothstep(3,14,p.y));
    return lerp(water,_RedonWarm.rgb,RedonFocus(p)*.65);
}
float3 RedonFogColor(float3 color, float3 world)
{
    float depth = max(0, -TransformWorldToView(world).z - _RedonFog.x);
    float amount = 1.0-exp(-depth*_RedonFog.y);
    return lerp(color,RedonAtmosphere(world),amount);
}
#endif
