Shader "PlayWay Water/Underwater/Compose Underwater Mask"
{
	Properties
	{
		_Intensity ("Intensity", Float) = 1.0
	}

	CGINCLUDE
	
	#include "UnityCG.cginc"

	struct VertexInput
	{
		float4 vertex	: POSITION;
		float2 uv0		: TEXCOORD0;
	};

	struct VertexOutput
	{
		float4 pos	: SV_POSITION;
		float2 uv	: TEXCOORD0;
	};

	sampler2D _WaterMask;
	half _Intensity;

	VertexOutput vert (VertexInput vi)
	{
		VertexOutput vo;
		vo.pos = mul(UNITY_MATRIX_MVP, vi.vertex);
		vo.uv = vi.uv0;

		return vo;
	}

	fixed4 frag(VertexOutput vo) : SV_Target
	{
		half3 c = tex2D(_WaterMask, vo.uv);
		return c.x > 900000 ? c.z : 0;
	}

	ENDCG

	SubShader
	{
		Tags { "RenderType"="Transparent" "PerformanceChecks"="False" "Queue"="Transparent" }

		Pass
		{
			ZTest Always Cull Off ZWrite Off
			Blend One One
			ColorMask R

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			ENDCG
		}
	}
}