#ifndef MAX_SHADOW_INPUT_INCLUDED
#define MAX_SHADOW_INPUT_INCLUDED

#define MAX_CASADESHADOW_COUNT 4

CBUFFER_START(MaxShadow)

//���ƹ� ����ռ�->ͶӰ�ռ�任����
float4x4 _MaxWorldToMainLightCascadeShadowMapSpaceMatrices[MAX_CASADESHADOW_COUNT];

float4 _MaxCascadeCullingSpheres[MAX_CASADESHADOW_COUNT];

CBUFFER_END

#endif