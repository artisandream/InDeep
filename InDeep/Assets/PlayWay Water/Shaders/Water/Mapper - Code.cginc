#include "UnityCG.cginc"
#include "UnityStandardCore.cginc"

struct VertexOutput
{
	float4 pos			: SV_POSITION;
	half2 heights		: TEXCOORD0;
	half2 screenPos		: TEXCOORD1;
};

VertexOutput vert (VertexInput vi)
{
	VertexOutput vo;

	float4 posWorld = GET_WORLD_POS(vi.vertex);
	half neutralY = posWorld.y;

	half2 normal;
	float2 fftUV;
	float3 displacement;
	half mask;
	TransformVertex(posWorld, normal, fftUV, displacement, mask);

	vo.pos = mul(UNITY_MATRIX_VP, posWorld);
	vo.heights = half2(neutralY, posWorld.y);
	vo.screenPos = vo.pos.xy;			// it's ortographic projection, so two components are enough
		
	return vo;
}

half4 frag(VertexOutput vo) : SV_Target
{
	// fade near edges
	half2 k = abs(vo.screenPos);
	half4 result = lerp(vo.heights.x, vo.heights.y, max(0, 1.0 - pow(max(k.x, k.y), 6)));

	return result;
}
