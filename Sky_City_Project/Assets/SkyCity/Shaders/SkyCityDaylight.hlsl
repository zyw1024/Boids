#ifndef SKY_CITY_DAYLIGHT_INCLUDED
#define SKY_CITY_DAYLIGHT_INCLUDED
// Bound once by SkyCityDaylight. Sky, water and clouds share the scene's sun.
float4 _SkySunDirection, _SkySunRadiance, _SkyDawnZenith, _SkyDawnHorizon;
float3 SkyCitySkyRadiance(float3 direction)
{
    float elevation=saturate((direction.y+.08)*2.5);
    float3 sky=lerp(_SkyDawnHorizon.rgb,_SkyDawnZenith.rgb,pow(elevation,.55));
    float alignment=saturate(dot(direction,_SkySunDirection.xyz));
    float halo=pow(alignment,12)*.16+pow(alignment,64)*.23;
    sky+=_SkySunRadiance.rgb*halo;
    return sky;
}
#endif
