#ifndef MAX_Light_INCLUDED
#define MAX_Light_INCLUDED

#include "BRDF.hlsl"
#include "LightInput.hlsl"
#include "GlobalIllumination.hlsl"

float DistanceAtten(float distanceSqr, float rangeSqr)
{
    float factor = saturate(1 - distanceSqr * rcp(rangeSqr));
    factor = factor * factor;
    return factor * rcp(max(distanceSqr, 0.001));
}

float3 PBR_Lit(float3 Po, float3 Pw, float3 N, float3 albedo, float metalness, float roughness, float3 camP)
{
    //      direct lighting
    // directional light shading
    Surface surface;
    surface.L = _MaxDirectionalLightDirection.xyz;
    surface.V = normalize(camP - Pw);
    surface.N = normalize(N);
    surface.albedo = albedo;
    surface.metalness = metalness;
    surface.roughness = roughness;

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
        float distance    = length(otherLightPos.xyz - Pw);
        float lightRange = otherLight.positionRange.w;
        float attenuation = DistanceAtten(distance * distance, lightRange * lightRange);
        float3 radiance     = otherLight.color.rgb * attenuation;

        surface.L = normalize(otherLightPos - Pw);
        surface.V = normalize(camP - Pw);
        float NdotL = max(dot(surface.N, surface.L), 0);

        Lo = Lo + BRDF(surface) * radiance * NdotL;
    }

    // indirect lighting
    // diffuse (from light probe SH)
    float3 reflectDir = reflect(-surface.V, surface.N);
    reflectDir = normalize(reflectDir);
    float NdotV = max(0, dot(surface.N, surface.V));
    float3 indirectSpec = GetSpec(reflectDir, surface.roughness, NdotV);
    float3 indirectDiffuse = CubemapApprox(N);
    Lo = Lo + IndirectBRDF(surface, indirectDiffuse, indirectSpec);
    return Lo;
    //return Lo + _AmbientColor.rgb;
}
#endif