#ifndef MAX_Light_INCLUDED
#define MAX_Light_INCLUDED

#include "BRDF.hlsl"
#include "LightInput.hlsl"
#include "GlobalIllumination.hlsl"
#include "Shadow.hlsl"

float DistanceAtten(float distanceSqr, float rangeSqr)
{
    float factor = saturate(1 - distanceSqr * rcp(rangeSqr));
    factor = factor * factor;
    return factor * rcp(max(distanceSqr, 0.001));
}

float3 PBR_DirectLit(Surface surface)
{
    //      direct lighting
    // directional light shading
    float3 Lo = float3(0.0, 0.0, 0.0);
    float3 radiance = _MaxDirectionalLightColor.rgb;
    float NdotL = max(dot(surface.N, surface.L), 0);

    Lo = Lo + BRDF(surface) * radiance * NdotL;

    // point light shading
    int lightCount = clamp(OTHER_LIGHT_COUNT,0, MAX_OTHER_LIGHT_PER_OBJECT);
    for(int i1 = 0; i1 < lightCount; ++i1)
    {
        MaxOtherLight otherLight = GetOtherLight(i1);
        float3 otherLightPos = otherLight.positionRange.xyz;
        float distance    = length(otherLightPos.xyz - surface.P);
        float lightRange = otherLight.positionRange.w;
        float attenuation = DistanceAtten(distance * distance, lightRange * lightRange);
        float3 radiance     = otherLight.color.rgb * attenuation;

        surface.L = normalize(otherLightPos - surface.P);
        surface.V = normalize(_WorldSpaceCameraPos - surface.P);
        float NdotL = max(dot(surface.N, surface.L), 0);

        Lo = Lo + BRDF(surface) * radiance * NdotL;
    }

    return Lo;
}

float3 PBR_IndirectLit(Surface surface)
{
    // indirect lighting
    // diffuse (from light probe SH)
    float3 Lo = float3(0.0, 0.0, 0.0);

    float3 reflectDir = reflect(-surface.V, surface.N);
    reflectDir = normalize(reflectDir);
    float NdotV = max(0, dot(surface.N, surface.V));
    float3 indirectSpec = GetSpec(reflectDir, surface.roughness, NdotV);
    float3 indirectDiffuse = CubemapApprox(surface.N);
    Lo = Lo + IndirectBRDF(surface, indirectDiffuse, indirectSpec);
    
    //return Lo + _AmbientColor.rgb;
    return Lo;
}

float3 PBR_Shading(float3 Pw, float3 N, float3 albedo, float metalness, float roughness)
{
    Surface surface;
    surface.P = Pw;
    surface.L = _MaxDirectionalLightDirection.xyz;
    surface.V = normalize(_WorldSpaceCameraPos - Pw);
    surface.N = normalize(N);
    surface.albedo = albedo;
    surface.metalness = metalness;
    surface.roughness = roughness;

    float3 c_dir = PBR_DirectLit(surface);
    float3 c_indir = PBR_IndirectLit(surface);

    float visibility = (1 - GetMainLightShadowAtten(surface.P, surface.N));
    visibility = visibility < 1.0 ? 0 : 1;
    
    return c_dir * visibility + c_indir;
}
#endif