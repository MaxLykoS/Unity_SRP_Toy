#ifndef COMMON_INPUT_INCLUDE
#define COMMON_INPUT_INCLUDE

float3 _WorldSpaceCameraPos;
float4x4 unity_MatrixVP;

#define TRANSFORM_TEX(tex, name) ((tex.xy) * name##_ST.xy + name##_ST.zw)

///UnityPerDraw��Unity����Լ���õ�һ��CBUFFER,����ı���������Լ���õģ������޸�
CBUFFER_START(UnityPerDraw)
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4 unity_LODFade; // x is the fade value ranging within [0,1]. y is x quantized into 16 levels
float4 unity_WorldTransformParams; // w is usually 1.0, or -1.0 for odd-negative scale transforms

half4 unity_LightData;
half4 unity_LightIndices[2];
CBUFFER_END



#endif