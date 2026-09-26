#ifndef SKY_CITY_ROCK_INCLUDED
#define SKY_CITY_ROCK_INCLUDED
TEXTURE2D(_RockScanAlbedo); SAMPLER(sampler_RockScanAlbedo);
TEXTURE2D(_RockScanNormal); SAMPLER(sampler_RockScanNormal);
half3 SkyCityRockSample(float3 world,half3 normal)
{
    // Metre-scale triplanar sampling survives island stretching and floating origin.
    half3 w=pow(abs(normal),4);w/=max(dot(w,1),.001);
    float3 p=world*.115;
    half3 c=SAMPLE_TEXTURE2D(_RockScanAlbedo,sampler_RockScanAlbedo,p.zy).rgb*w.x
           +SAMPLE_TEXTURE2D(_RockScanAlbedo,sampler_RockScanAlbedo,p.xz).rgb*w.y
           +SAMPLE_TEXTURE2D(_RockScanAlbedo,sampler_RockScanAlbedo,p.xy).rgb*w.z;
    return c;
}
half3 SkyCityRockColor(float3 world,half3 normal)
{
    half3 c=SkyCityRockSample(world,normal);
    half mineral=saturate(dot(c,half3(.2126,.7152,.0722))*1.9);
    return half3(.48,.465,.425)*lerp(.65,1.22,mineral);
}
half3 SkyCityRockNormal(float3 world,half3 normal,float2 uv)
{
    // Derive shallow relief from the continuous limestone texture. The old
    // scan's tangent normal atlas is valid only on its original scanned mesh.
    float height=dot(SkyCityRockSample(world,normal),half3(.2126,.7152,.0722));
    float3 dx=ddx(world),dy=ddy(world),r1=cross(dy,normal),r2=cross(normal,dx);
    float det=dot(dx,r1);
    float3 gradient=(r1*ddx(height)+r2*ddy(height))*sign(det)/max(abs(det),.00001);
    float3 relief=gradient*.035;relief/=max(1,length(relief)*2);
    return normalize(normal-relief);
}
#endif
