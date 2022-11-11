#ifndef MAX_PBRLIT_PASS_INCLUDED
#define MAX_PBRLIT_PASS_INCLUDED

#include "../ShaderLibrary/Lighting.hlsl"
#include "../ShaderLibrary/Shadow.hlsl"
#include "../ShaderLibrary/SpaceTransform.hlsl"
#include "../ShaderLibrary/Packing.hlsl"

struct a2v
{
	float4 pO : POSITION;
	float2 uv : TEXCOORD0;
	float3 nO : NORMAL;
	float4 tO : TANGENT;
};

struct v2f
{
	float2 uv : TEXCOORD0;
	float4 pH : SV_POSITION;
	float3 nW : TEXCOORD1;
	float3 pW : TEXCOORD2;
	float3 tW : TEXCOORD3;
	float3 bW : TEXCOORD4;
};

CBUFFER_START(UnityPerMaterial)
float4 _AlbedoMap_ST;  
float _Transparency;
float _Metalness;
float _Roughness;
CBUFFER_END

UNITY_DECLARE_TEX2D(_NormalMap);
UNITY_DECLARE_TEX2D(_AlbedoMap);     
UNITY_DECLARE_TEX2D(_MetalnessMap);

v2f PBRVertex(a2v v)
{
	v2f o;
	o.pH = UnityObjectToClipPos(v.pO);
	o.uv = v.uv;
	o.uv = o.uv * _AlbedoMap_ST.xy + _AlbedoMap_ST.zw;
	o.pW = mul(unity_ObjectToWorld, v.pO).xyz;

	o.nW = TransformObjectToWorldNormal(v.nO);
	o.tW = normalize(TransformObjectToWorld(v.tO.xyz));
	o.bW = normalize(cross(o.nW, o.tW) * v.tO.w);

	return o;
}

float4 PBRFragment(v2f o) : SV_Target
{
	float4 albedo = UNITY_SAMPLE_TEX2D(_AlbedoMap, o.uv);
	float4 metalInfo = UNITY_SAMPLE_TEX2D(_MetalnessMap, o.uv);
	float roughness = (1 - metalInfo.a * (1 - _Roughness));
	float metalness = _Metalness * metalInfo.r;

	o.nW = normalize(o.nW);
	o.tW = normalize(o.tW - dot(o.tW,o.nW) * o.nW);
	o.bW = normalize(o.bW);
	float3x3 t2w = float3x3(o.tW, o.bW, o.nW);
	float3 bump = DecodeNormal(UNITY_SAMPLE_TEX2D(_NormalMap, o.uv), 1.0);
	bump = normalize(mul(bump, t2w));

	float3 c = PBR_Shading( o.pW, bump, albedo.rgb, metalness, roughness);
	return float4(c, albedo.a);
}

float4 PBRFragmentTransparent(v2f o) : SV_Target
{
	float4 albedo = UNITY_SAMPLE_TEX2D(_AlbedoMap, o.uv);
	float4 metalInfo = UNITY_SAMPLE_TEX2D(_MetalnessMap, o.uv);
	float roughness = (1 - metalInfo.a * (1 - _Roughness));
	float metalness = _Metalness * metalInfo.r;

	o.nW = normalize(o.nW);
	o.tW = normalize(o.tW - dot(o.tW,o.nW) * o.nW);
	o.bW = normalize(o.bW);
	float3x3 t2w = float3x3(o.tW, o.bW, o.nW);
	float3 bump = DecodeNormal(UNITY_SAMPLE_TEX2D(_NormalMap, o.uv), 1.0);
	bump = normalize(mul(bump, t2w));

	float3 c = PBR_Shading(o.pW, bump, albedo.rgb, metalness, roughness);
	return float4(c, _Transparency);
}
#endif