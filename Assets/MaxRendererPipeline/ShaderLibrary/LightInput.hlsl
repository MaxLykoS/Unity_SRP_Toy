#ifndef MAX_LIGHT_INPUT_INCLUDED
#define MAX_LIGHT_INPUT_INCLUDED

#include "HLSLSupport.cginc"
#include "./CommonInput.hlsl"

#define MAX_OTHER_VISIBLE_LIGHT_COUNT 32
#define MAX_OTHER_LIGHT_PER_OBJECT 8

CBUFFER_START(MaxLighting)
float4 _AmbientColor;

float4x4 _MaxMainLightMatrixWorldToShadowMap;
float4 _MaxDirectionalLightColor;
float4 _MaxDirectionalLightDirection;

//������Դ��λ�úͷ�Χ,xyz����λ�ã�w����Χ
float4 _MaxOtherLightPositionAndRanges[MAX_OTHER_VISIBLE_LIGHT_COUNT];
//������Դ����ɫ
half4 _MaxOtherLightColors[MAX_OTHER_VISIBLE_LIGHT_COUNT];
CBUFFER_END

#define OTHER_LIGHT_COUNT unity_LightData.y

struct MaxDirLight
{
    float3 direction;
    half4 color;
};

struct MaxOtherLight
{
    float4 positionRange;
    half4 color;
};

MaxDirLight GetMainLight()
{
    MaxDirLight light;
    light.direction = _MaxDirectionalLightDirection;
    light.color = _MaxDirectionalLightColor;
    return light;
}

MaxOtherLight GetOtherLight(uint index)
{
    MaxOtherLight light;
    uint idx = index / 4;
    uint offset = index % 4;
    uint lightIndex  = unity_LightIndices[idx][offset];
    float4 positionRange = _MaxOtherLightPositionAndRanges[lightIndex];
    half4 color = _MaxOtherLightColors[lightIndex];
    light.positionRange = positionRange;
    light.color = color;
    return light;
}

#endif