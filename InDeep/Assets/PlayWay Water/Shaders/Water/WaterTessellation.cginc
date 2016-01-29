#include "Tessellation.cginc"
#include "Lighting.cginc"

float _TesselationFactor;

#ifndef TESS_FACTOR
	#define TESS_FACTOR UnityEdgeLengthBasedTessCull (v0.vertex, v1.vertex, v2.vertex, _TesselationFactor, _MaxDisplacement)
#endif

struct TesselatorVertexInput
{
    float4 vertex	: INTERNALTESSPOS;
//#if _PROJECTION_GRID
//	float distance : TEXCOORD0;
//#endif
	half3 normal	: NORMAL;
    #if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
		half2 uv2		: TEXCOORD1;
    #endif
};

#if _PROJECTION_GRID || _QUADS

	/*
	 * QUAD TESSELATION
	 */
	[UNITY_domain("quad")]
	[UNITY_partitioning("integer")]
	[UNITY_outputtopology("triangle_cw")]
	[UNITY_patchconstantfunc("hsconst")]
	[UNITY_outputcontrolpoints(4)]
	TesselatorVertexInput hs_surf (InputPatch<TesselatorVertexInput, 4> v, uint id : SV_OutputControlPointID)
	{
		return v[id];
	}

	struct HS_CONSTANT_OUTPUT
	{
		float edge[4]  : SV_TessFactor;
		float inside[2] : SV_InsideTessFactor;
	};

	float edgeTessFactor(float3 pos)
	{
		float normalFactor = 1.0 - abs(dot(normalize(pos - _WorldSpaceCameraPos.xyz), float3(0, 1, 0)));
		
		return normalFactor * normalFactor * _TesselationFactor;
	}

	HS_CONSTANT_OUTPUT hsconst (InputPatch<TesselatorVertexInput,4> v)
	{
		HS_CONSTANT_OUTPUT o;

		//if (v[0].distance > 0 && v[1].distance > 0 && v[2].distance > 0 && v[3].distance > 0)
		//{
			/*o.edge[0] = sqrt(v[0].distance + v[3].distance) * 15 / _TesselationFactor;
			o.edge[1] = sqrt(v[0].distance + v[1].distance) * 15 / _TesselationFactor;
			o.edge[2] = sqrt(v[1].distance + v[2].distance) * 15 / _TesselationFactor;
			o.edge[3] = sqrt(v[2].distance + v[3].distance) * 15 / _TesselationFactor;*/

			/*o.edge[0] = edgeTessFactor((v[0].vertex + v[3].vertex) * 0.5);
			o.edge[1] = edgeTessFactor((v[0].vertex + v[1].vertex) * 0.5);
			o.edge[2] = edgeTessFactor((v[1].vertex + v[2].vertex) * 0.5);
			o.edge[3] = edgeTessFactor((v[2].vertex + v[3].vertex) * 0.5);*/

			/*o.edge[0] = (v[0].distance + v[3].distance) * 0.5 / _TesselationFactor;
			o.edge[1] = (v[0].distance + v[1].distance) * 0.5 / _TesselationFactor;
			o.edge[2] = (v[1].distance + v[2].distance) * 0.5 / _TesselationFactor;
			o.edge[3] = (v[2].distance + v[3].distance) * 0.5 / _TesselationFactor;*/

			o.edge[0] = UnityCalcEdgeTessFactor(v[0].vertex, v[3].vertex, _TesselationFactor);
			o.edge[1] = UnityCalcEdgeTessFactor(v[0].vertex, v[1].vertex, _TesselationFactor);
			o.edge[2] = UnityCalcEdgeTessFactor(v[1].vertex, v[2].vertex, _TesselationFactor);
			o.edge[3] = UnityCalcEdgeTessFactor(v[2].vertex, v[3].vertex, _TesselationFactor);

			//o.inside[0] = (o.edge[0] + o.edge[1] + o.edge[2]) / 3.0;
			//o.inside[1] = (o.edge[0] + o.edge[2] + o.edge[3]) / 3.0;
			o.inside[0] = (o.edge[0] + o.edge[1] + o.edge[2] + o.edge[3]) / 4.0;
			o.inside[1] = (o.edge[0] + o.edge[1] + o.edge[2] + o.edge[3]) / 4.0;
			//o.inside[0] = 1.0;
			//o.inside[1] = 1.0;

			/*o.edge[0] = _TesselationFactor - 5.9f;
			o.edge[1] = _TesselationFactor - 5.9f;
			o.edge[2] = _TesselationFactor - 5.9f;
			o.edge[3] = _TesselationFactor - 5.9f;

			o.inside[0] = _TesselationFactor - 5.9f;
			o.inside[1] = _TesselationFactor - 5.9f;*/

			/*o.edge[0] = 3.0;
			o.edge[1] = 3.0;
			o.edge[2] = 3.0;
			o.edge[3] = 3.0;

			o.inside[0] = 3.0;
			o.inside[1] = 3.0;*/
		//}
		/*else
		{
			o.edge[0] = 0;
			o.edge[1] = 0;
			o.edge[2] = 0;
			o.edge[3] = 0;

			o.inside[0] = 0;
			o.inside[1] = 0;
		}*/

		return o;
	}

	#ifndef BASIC_INPUTS
	[UNITY_domain("quad")]
	TESS_OUTPUT ds_surf (HS_CONSTANT_OUTPUT tessFactors, const OutputPatch<TesselatorVertexInput, 4> patch, float2 UV : SV_DomainLocation)
	{
		VertexInput v;

		v.vertex = lerp(
			lerp(patch[0].vertex, patch[1].vertex, UV.x),
			lerp(patch[3].vertex, patch[2].vertex, UV.x),
			UV.y
		);

		v.normal = lerp(
			lerp(patch[0].normal, patch[1].normal, UV.x),
			lerp(patch[3].normal, patch[2].normal, UV.x),
			UV.y
		);

	#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
		v.uv2 = lerp(
			lerp(patch[0].uv2, patch[1].uv2, UV.x),
			lerp(patch[3].uv2, patch[2].uv2, UV.x),
			UV.y
		);
	#endif

		TESS_OUTPUT o = POST_TESS_VERT (v);
		return o;
	}
	#else
	[UNITY_domain("quad")]
	TESS_OUTPUT ds_surf(HS_CONSTANT_OUTPUT tessFactors, const OutputPatch<TesselatorVertexInput, 4> patch, float2 UV : SV_DomainLocation)
	{
		VertexInput2 v;

		v.vertex = lerp(
			lerp(patch[0].vertex, patch[1].vertex, UV.x),
			lerp(patch[3].vertex, patch[2].vertex, UV.x),
			UV.y
			);

		TESS_OUTPUT o = POST_TESS_VERT(v);
		return o;
	}
	#endif

#else

	/*
	* TRIANGLE TESSELATION
	*/
	[UNITY_domain("tri")]
	[UNITY_partitioning("fractional_odd")]
	[UNITY_outputtopology("triangle_cw")]
	[UNITY_patchconstantfunc("hsconst")]
	[UNITY_outputcontrolpoints(3)]
	TesselatorVertexInput hs_surf(InputPatch<TesselatorVertexInput, 3> v, uint id : SV_OutputControlPointID)
	{
		return v[id];
	}

	float4 tessEdge(TesselatorVertexInput v0, TesselatorVertexInput v1, TesselatorVertexInput v2)
	{
		return TESS_FACTOR;			//return float4(1,1,1,1);
	}

	UnityTessellationFactors hsconst(InputPatch<TesselatorVertexInput, 3> v)
	{
		UnityTessellationFactors o;
		float4 tf;
		tf = tessEdge(v[0], v[1], v[2]);
		o.edge[0] = tf.x; o.edge[1] = tf.y; o.edge[2] = tf.z; o.inside = tf.w;
		return o;
	}

	#ifdef POST_TESS_VERT
	[UNITY_domain("tri")]
	TESS_OUTPUT ds_surf(UnityTessellationFactors tessFactors, const OutputPatch<TesselatorVertexInput, 3> vi, float3 bary : SV_DomainLocation) {
		VertexInput v;
		v.vertex = vi[0].vertex*bary.x + vi[1].vertex*bary.y + vi[2].vertex*bary.z;
		v.normal = vi[0].normal*bary.x + vi[1].normal*bary.y + vi[2].normal*bary.z;

	#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
		v.uv2 = vi[0].uv2*bary.x + vi[1].uv2*bary.y + vi[2].uv2*bary.z;
	#endif

		TESS_OUTPUT o = POST_TESS_VERT(v);
		return o;
	}
	#endif

#endif

TesselatorVertexInput tessvert_surf (VertexInput v)
{
	TesselatorVertexInput o;
#if _PROJECTION_GRID
	o.vertex = v.vertex;
	o.vertex = float4(GetProjectedPosition(o.vertex.xy), 1);
	//float4 projected = mul(UNITY_MATRIX_VP, o.vertex);
	//projected.z /= projected.w;
	//o.distance = length(_WorldSpaceCameraPos.xyz - o.vertex.xyz);// -_ProjectionParams.y;
	//o.distance = 1;
	//if (projected.z < -1.1)
	//	o.distance = -1;
#else
	o.vertex = mul(_OBJECT2WORLD, v.vertex);
#endif
	o.normal = v.normal;
#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
	o.uv2 = v.uv2;
#endif
	return o;
}
