#include "UnityCG.cginc"
#include "UnityStandardCore.cginc"

struct v2f
{
	float4 pos			: SV_POSITION;
	float2 depth		: TEXCOORD0;
	//float4 screenPos	: TEXCOORD1;
};

struct VertexInput2
{
	float4 vertex	: POSITION;
};

v2f vert(VertexInput2 vi)
{
	v2f o;

	float4 posWorld = GET_WORLD_POS(vi.vertex);

	half2 normal;
	float2 fftUV;
	float3 displacement;
	half mask;
	TransformVertex(posWorld, normal, fftUV, displacement, mask);

	o.pos = mul(UNITY_MATRIX_VP, posWorld);
	//o.screenPos = ComputeScreenPos(o.pos);
	o.depth = o.pos.zw;

	return o;
}

float4 frag(v2f i) : SV_Target
{
	//float currentDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos));
	float depth = i.depth.x / i.depth.y;

	//clip(currentDepth - depth);

	//UNITY_OUTPUT_DEPTH(i);
	return depth;
}