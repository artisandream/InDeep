﻿Shader "PlayWay Water/Base/FFT"
{
	Properties
	{
		_MainTex ("", 2D) = "" {}
		_ButterflyTex ("", 2D) = "" {}
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
	};

	/*struct MRTOutput
	{
		half2 height					: COLOR0;
		half4 slope						: COLOR1;
		half4 horizontalDisplacement	: COLOR2;
	};*/

	sampler2D _MainTex;
	sampler2D _ButterflyTex;
	half _ButterflyPass;
	half _ScaleFactor;

	//sampler2D _TexA;
	//sampler2D _TexB;
	//sampler2D _TexC;

	VertexOutput vert (VertexInput vi)
	{
		VertexOutput vo;

		vo.pos = mul(UNITY_MATRIX_MVP, vi.vertex);
		vo.uv = vi.uv0;

		return vo;
	}

	///
	/// FFT
	///
	inline float4 FFT_1(sampler2D tex, half2 uv1, half2 uv2, float2 weights)
	{
		float2 a1 = tex2D(tex, uv1).rg;
		float2 b1 = tex2D(tex, uv2).rg;

		float2 res;
		res.r = weights.r*b1.r - weights.g*b1.g;
		//res.g = weights.g*b1.r + weights.r*b1.g;
		res.g = dot(weights.gr, b1.rg);

		return float4(a1 + res, 0, 1);
	}

	inline float4 FFT_2(sampler2D tex, half2 uv1, half2 uv2, float2 weights)
	{
		float4 a1 = tex2D(tex, uv1).rgba;
		float4 b1 = tex2D(tex, uv2).rgba;

		float4 res;
		res.rb = weights.r*b1.rb - weights.g*b1.ga;
		res.ga = weights.g*b1.rb + weights.r*b1.ga;

		return a1 + res;
	}

	///
	/// Single FFT
	///
	float4 hfft_1(VertexOutput In) : SV_Target
	{
		float4 butterfly = tex2D(_ButterflyTex, half2(In.uv.x, _ButterflyPass));
  
		half2 indices = butterfly.rg;
		float2 weights = butterfly.ba;

		return FFT_1(_MainTex, half2(indices.x, In.uv.y), half2(indices.y, In.uv.y), weights);
	}

	float4 vfft_1(VertexOutput In) : SV_Target
	{
		float4 butterfly = tex2D(_ButterflyTex, half2(In.uv.y, _ButterflyPass));

		half2 indices = butterfly.rg;
		float2 weights = butterfly.ba;

		return FFT_1(_MainTex, half2(In.uv.x, indices.x), half2(In.uv.x, indices.y), weights);
	}

	///
	/// Two FFTs at a time
	///
	float4 hfft_2(VertexOutput In) : SV_Target
	{
		float4 butterfly = tex2D(_ButterflyTex, half2(In.uv.x, _ButterflyPass));
  
		half2 indices = butterfly.rg;
		float2 weights = butterfly.ba;

		return FFT_2(_MainTex, half2(indices.x, In.uv.y), half2(indices.y, In.uv.y), weights);
	}

	float4 vfft_2(VertexOutput In) : SV_Target
	{
		float4 butterfly = tex2D(_ButterflyTex, half2(In.uv.y, _ButterflyPass));

		half2 indices = butterfly.rg;
		float2 weights = butterfly.ba;

		return FFT_2(_MainTex, half2(In.uv.x, indices.x), half2(In.uv.x, indices.y), weights);
	}

	///
	/// Real-valued output versions
	///
	float4 vfft_1r(VertexOutput In) : SV_Target
	{
		float4 butterfly = tex2D(_ButterflyTex, half2(In.uv.y, _ButterflyPass));

		half2 indices = butterfly.rg;
		float2 weights = butterfly.ba;

		return FFT_1(_MainTex, half2(In.uv.x, indices.x), half2(In.uv.x, indices.y), weights).rgba;
	}

	float4 vfft_2r(VertexOutput In) : SV_Target
	{
		float4 butterfly = tex2D(_ButterflyTex, half2(In.uv.y, _ButterflyPass));

		half2 indices = butterfly.rg;
		float2 weights = butterfly.ba;

		return FFT_2(_MainTex, half2(In.uv.x, indices.x), half2(In.uv.x, indices.y), weights).rbrb;
	}

	///
	/// Multiple Render Targets
	///
	/*MRTOutput hfft_mrt(VertexOutput In) : SV_Target
	{
		float4 butterfly = tex2D(_ButterflyTex, float2(In.uv.x, _ButterflyPass));
  
		float2 indices = butterfly.rg;
		float2 weights = butterfly.ba;

		float2 uv1 = float2(indices.x, In.uv.y);
		float2 uv2 = float2(indices.y, In.uv.y);

		MRTOutput o;
		o.height = FFT_1(_TexA, uv1, uv2, weights);
		o.slope = FFT_2(_TexB, uv1, uv2, weights);
		o.horizontalDisplacement = FFT_2(_TexC, uv1, uv2, weights);

		return o;
	}

	MRTOutput vfft_mrt(VertexOutput In) : SV_Target
	{
		float4 butterfly = tex2D(_ButterflyTex, float2(In.uv.y, _ButterflyPass));

		float2 indices = butterfly.rg;
		float2 weights = butterfly.ba;

		float2 uv1 = float2(In.uv.x, indices.x);
		float2 uv2 = float2(In.uv.x, indices.y);

		MRTOutput o;
		o.height = FFT_1(_TexA, uv1, uv2, weights);
		o.slope = FFT_2(_TexB, uv1, uv2, weights);
		o.horizontalDisplacement = FFT_2(_TexC, uv1, uv2, weights);

		return o;
	}*/

	ENDCG

	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Always
		Blend Off

		Pass
		{
			Name "hFFT1"
			ColorMask RG

			CGPROGRAM
			
			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment hfft_1

			ENDCG
		}

		Pass
		{
			Name "vFFT1"
			ColorMask RG

			CGPROGRAM
			
			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment vfft_1

			ENDCG
		}

		Pass
		{
			Name "hFFT2"
			ColorMask RGBA

			CGPROGRAM
			
			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment hfft_2

			ENDCG
		}

		Pass
		{
			Name "vFFT2"
			ColorMask RGBA

			CGPROGRAM
			
			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment vfft_2

			ENDCG
		}

		/*Pass
		{
			Name "hFFTmrt"
			ColorMask RGBA

			CGPROGRAM
			
			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment hfft_mrt

			ENDCG
		}

		Pass
		{
			Name "vFFTmrt"
			ColorMask RGBA

			CGPROGRAM
			
			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment vfft_mrt

			ENDCG
		}*/

		Pass
		{
			Name "vFFT1r"
			ColorMask R

			CGPROGRAM
			
			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment vfft_1r

			ENDCG
		}

		Pass
		{
			Name "vFFT2r"
			ColorMask RG

			CGPROGRAM
			
			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment vfft_2r

			ENDCG
		}
	}
}