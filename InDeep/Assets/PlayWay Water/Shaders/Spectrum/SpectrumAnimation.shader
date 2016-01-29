Shader "PlayWay Water/Spectrum/Water Spectrum"
{
	Properties
	{
		_MainTex ("", 2D) = "" {}
		_Gravity ("", Float) = 9.81
		_PlaneSizeInv ("", Float) = 0.01
		_TargetResolution ("", Vector) = (1024, 1024, 1024, 1024)
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
		float2 uv1  : TEXCOORD1;
	};

	struct PSOutput
	{
		float4 height		: COLOR0;
		float4 displacement	: COLOR1;
	};

	struct PSOutput2
	{
		float4 height					: COLOR0;
		float4 slope					: COLOR1;
		float4 displacement				: COLOR2;
	};

	sampler2D _MainTex;
	float _RenderTime;
	float _Gravity;
	float _PlaneSizeInv;
	float2 _TargetResolution;
	float4 _MainTex_TexelSize;

	VertexOutput vert (VertexInput vi)
	{
		VertexOutput vo;

		vo.pos = mul(UNITY_MATRIX_MVP, vi.vertex);
		vo.uv = vi.uv0 - _MainTex_TexelSize.xy * 0.5;
		vo.uv1 = vi.uv0;

		return vo;
	}

	float2 Spectrum(float2 uv1, float2 uv2, float t)
	{
		float2 s1 = tex2D(_MainTex, uv1);
		float2 s2 = tex2D(_MainTex, uv2);
		
		float s, c;
		sincos(t, s, c);

		return float2((s1.x + s2.x) * c - (s1.y + s2.y) * s, (s1.x - s2.x) * s + (s1.y - s2.y) * c);
		//return float2(s1.x * c - s1.y * s, s1.x * s + s1.y * c);
	}

	float2 Spectrum(float2 uv, float2 uv1, out float2 k)
	{
		float pix2 = 6.2831853;
		float2 centeredUV = -0.5 + frac(uv + 0.5);
		k = pix2 * _TargetResolution.xy * _PlaneSizeInv * centeredUV;
		float t = _RenderTime * sqrt(_Gravity * length(k));

		return Spectrum(uv1, 1.0 - uv1 + _MainTex_TexelSize.xy, t);
	}

	float4 animate (VertexOutput vo) : SV_Target
	{
		float2 k;
		return float4(Spectrum(vo.uv, vo.uv1, k), 0, 1);
	}

	PSOutput animateDisplacements(VertexOutput vo)
	{
		float2 k;
		float2 s = Spectrum(vo.uv, vo.uv1, k);

		PSOutput po;
		po.height = float4(s, 1, 1);

		k = normalize(k);
		if (vo.uv.x == 0 && vo.uv.y == 0) k = 0.707107;

		po.displacement.x = s.y * k.x;
		po.displacement.y = -s.x * k.x;
		po.displacement.z = s.y * k.y;
		po.displacement.w = -s.x * k.y;

		return po;
	}

	PSOutput2 animatex3 (VertexOutput vo)
	{
		float2 k;
		float2 s = Spectrum(vo.uv, vo.uv1, k);

		PSOutput2 po;
		po.height = float4(s, 1, 1);
		
		po.slope.x = -s.y * k.x;
		po.slope.y = s.x * k.x;
		po.slope.z = -s.y * k.y;
		po.slope.w = s.x * k.y;

		k = normalize(k);
		if (vo.uv.x == 0 && vo.uv.y == 0) k = 0.707107;

		po.displacement.x = s.y * k.x;
		po.displacement.y = -s.x * k.x;
		po.displacement.z = s.y * k.y;
		po.displacement.w = -s.x * k.y;

		return po;
	}

	float4 animateSlope(VertexOutput vo) : SV_Target
	{
		float2 k;
		float2 s = Spectrum(vo.uv, vo.uv1, k);

		float4 slope;
		slope.x = -s.y * k.x;
		slope.y = s.x * k.x;
		slope.z = -s.y * k.y;
		slope.w = s.x * k.y;

		return slope;
	}

	float2 _WindDirection;
	float _Directionality;

	/// Converts omnidirectional spectrum to a directional one
	float4 directionalSpectrum (VertexOutput vo) : SV_Target
	{
		float3 spectrum = tex2D(_MainTex, vo.uv1.xy);

		float pix2 = 6.2831853;
		float2 centeredUV = -0.5 + frac(vo.uv.xy + 0.5);
		float2 k = pix2 * _TargetResolution.xy * _PlaneSizeInv * centeredUV;
		
		k = normalize(k);
		if (centeredUV.x == 0 && centeredUV.y == 0) k = _WindDirection.xy;

		float dp = _WindDirection.x * k.x + _WindDirection.y * k.y;
		float phi = acos(dp * 0.999);

		float directionalFactor = sqrt(1.0f + spectrum.z * cos(2.0f * phi));

		if(dp < 0)
			directionalFactor *= _Directionality;
		
		return float4(spectrum.xy * directionalFactor, 0.0, 1.0);
	}

	float _Weight;

	float4 addSpectrum (VertexOutput vo) : SV_Target
	{
		return tex2D(_MainTex, vo.uv.xy) * _Weight;
	}

	ENDCG

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			Name "Spectrum"

			CGPROGRAM
			
			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment animate

			ENDCG
		}

		Pass
		{
			Name "Animate Slope"

			CGPROGRAM

			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment animateSlope

			ENDCG
		}

		Pass
		{
			Name "Spectrumx3"

			CGPROGRAM
			
			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment animatex3

			ENDCG
		}

		Pass
		{
			Name "Directional Spectrum"

			CGPROGRAM
			
			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment directionalSpectrum

			ENDCG
		}

		Pass
		{
			Name "Add Spectrum"

			Blend One One

			CGPROGRAM

			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment addSpectrum

			ENDCG
		}

		Pass
		{
			Name "Animate Displacements"

			CGPROGRAM

			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment animateDisplacements

			ENDCG
		}
	}
}