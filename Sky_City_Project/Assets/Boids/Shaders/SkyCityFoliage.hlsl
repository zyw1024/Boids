#ifndef SKY_CITY_FOLIAGE_INCLUDED
#define SKY_CITY_FOLIAGE_INCLUDED
// The authored island's quiet leaf veins, shared by static and living plants.
float3 SkyCityLeafPigment(float3 albedo,float2 uv)
{
    float width=max(.005,fwidth(uv.y));
    float rib=1-smoothstep(width,width*2,abs(uv.y-.5));
    float veins=1-smoothstep(.035,.08,abs(frac(uv.x*6-abs(uv.y-.5)*3)-.5));
    float fade=1-saturate(max(fwidth(uv.x),fwidth(uv.y))*18);
    return albedo*(1+(rib*.10+veins*.045)*fade);
}
#endif
