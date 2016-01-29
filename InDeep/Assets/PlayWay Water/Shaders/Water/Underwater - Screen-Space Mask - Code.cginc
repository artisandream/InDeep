#define _OBJECT2WORLD _Object2World

#include "UnityCG.cginc"
#include "UnityStandardCore.cginc"

struct VertexInput2
{
	float4 vertex	: POSITION;
};

struct VertexOutput
{
	float4 pos			: SV_POSITION;
	float4 screenPos	: TEXCOORD0;
	float2 depth		: TEXCOORD1;
};

float4x4 _InvProjectionMatrix;
float3 _ViewportDown;
half2 _DepthParams;

VertexOutput vert(VertexInput2 vi)
{
	VertexOutput vo;

	float4 posWorld = GET_WORLD_POS(vi.vertex);

	half2 normal;
	float2 fftUV;
	float3 displacement;
	half mask;
	TransformVertex(posWorld, normal, fftUV, displacement, mask);

	float3 posCameraSpace = posWorld - _WorldSpaceCameraPos;

	float3 forward = float3(posCameraSpace.x, 0.0, posCameraSpace.z);
	posCameraSpace.xz = 0;

	float p = (length(forward.xz) - _ProjectionParams.y - 0.25) * _ProjectionParams.w;
	p = max(0.0, p);

	vo.pos = mul(UNITY_MATRIX_VP, posWorld);
	vo.depth = vo.pos.zw;

	if (length(_ViewportDown.xy) < 0.5 && _ViewportDown.z < 0.3)
	{
		vo.pos.w = abs(vo.pos.w);
		vo.pos.z = 0.5 * vo.pos.w;
	}

	vo.pos.xy += _ViewportDown.xy * float2(-30.0, -30.0) * p * vo.pos.w;
	vo.screenPos = ComputeScreenPos(vo.pos);

	return vo;
}

fixed4 maskFrag(VertexOutput vo) : SV_Target
{
	float depthWithWater = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, vo.screenPos).r);
	float depthWithoutWater = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_WaterlessDepthTexture, vo.screenPos).r);
	float maskDepth = LinearEyeDepth(vo.depth.x / vo.depth.y);

	float diff = depthWithoutWater - depthWithWater;
	return fixed4(abs(diff) < 0.001 || maskDepth / depthWithWater < 1.2 ? 0 : 1, 0, 0, 0);
}
