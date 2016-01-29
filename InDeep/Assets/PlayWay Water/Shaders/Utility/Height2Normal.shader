Shader "PlayWay Water/Utilities/Height2Normal"
{
	Properties
	{
		_MainTex ("", 2D) = "" {}
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
		half2 uv	: TEXCOORD0;
		half2 uv1	: TEXCOORD1;
		half2 uv2	: TEXCOORD2;
		half2 uv3	: TEXCOORD3;
		half2 uv4	: TEXCOORD4;
	};

	sampler2D _MainTex;
	float4 _MainTex_TexelSize;
	half _Intensity;

	VertexOutput vert (VertexInput vi)
	{
		VertexOutput vo;

		vo.pos = mul(UNITY_MATRIX_MVP, vi.vertex);
		vo.uv = vi.uv0;
		vo.uv1 = vi.uv0 + half2(_MainTex_TexelSize.x, 0.0);
		vo.uv2 = vi.uv0 + half2(0.0, _MainTex_TexelSize.y);
		vo.uv3 = vi.uv0 - half2(_MainTex_TexelSize.x, 0.0);
		vo.uv4 = vi.uv0 - half2(0.0, _MainTex_TexelSize.y);

		return vo;
	}

	half4 frag(VertexOutput vo) : SV_Target
	{
		half h00 = tex2D(_MainTex, vo.uv);
		half h10 = tex2D(_MainTex, vo.uv1);
		half h01 = tex2D(_MainTex, vo.uv2);
		half h20 = tex2D(_MainTex, vo.uv3);
		half h02 = tex2D(_MainTex, vo.uv4);

		return half4(((h00 - h10) + (h20 - h00)), ((h00 - h01) + (h02 - h00)), 0, 0) * _Intensity;
	}

	ENDCG

	SubShader
	{
		Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			
			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment frag

			ENDCG
		}
	}
}