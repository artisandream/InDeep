Shader "PlayWay Water/Foam/Global"
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
	};

	sampler2D _MainTex;
	sampler2D _DistortionMapB;

	half4 _SampleDir1;
	half2 _DeltaPosition;
	half4 _FoamParameters;		// x = intensity, y = horizonal displacement scale, z = power, w = fading factor

	VertexOutput vert (VertexInput vi)
	{
		VertexOutput vo;

		vo.pos = mul(UNITY_MATRIX_MVP, vi.vertex);
		vo.uv = vi.uv0;

		return vo;
	}

	half ComputeFoamGain5(half2 uv)
	{
		half2 displacement = tex2D(_DistortionMapB, uv);
#if SHADER_API_D3D11
		half3 j = half3(ddx_fine(displacement.x), ddy_fine(displacement.y), ddx_fine(displacement.y)) * _FoamParameters.y;
#else
		half3 j = half3(ddx(displacement.x), ddy(displacement.y), ddx(displacement.y)) * _FoamParameters.y;
#endif
		j.xy += 1.0;

		half jacobian = -(j.x * j.y - j.z * j.z);
		half gain = max(0.0, jacobian + 0.94);

#if FOAM_POW_2
		gain = gain * gain;
#elif FOAM_POW_N
		gain = pow(gain, _FoamParameters.z);
#endif

		return gain;
	}

	half ComputeFoamGain3(half2 uv)
	{
		half2 displacement = tex2D(_DistortionMapB, uv);
		half3 j = half3(ddx(displacement.x), ddy(displacement.y), ddx(displacement.y)) * _FoamParameters.y;
		j.xy += 1.0;

		half jacobian = -(j.x * j.y - j.z * j.z);
		half gain = max(0.0, jacobian + 0.94);

#if FOAM_POW_2
		gain = gain * gain;
#elif FOAM_POW_N
		gain = pow(gain, _FoamParameters.z);
#endif

		return gain;
	}

	half4 frag5(VertexOutput vo) : SV_Target
	{
		half2 foamUV = vo.uv - _SampleDir1.zw * 0.000002;
		half foam = tex2D(_MainTex, foamUV) * _FoamParameters.w;

		half gain = ComputeFoamGain5(vo.uv) * 6;
		foam += gain * _FoamParameters.x;

		return foam;
	}

	half4 frag3(VertexOutput vo) : SV_Target
	{
		half2 foamUV = vo.uv - _SampleDir1.zw * 0.000002;
		half foam = tex2D(_MainTex, foamUV) * _FoamParameters.w;

		half gain = ComputeFoamGain3(vo.uv) * 6;
		foam += gain * _FoamParameters.x;

		return foam;
	}

	ENDCG

	SubShader
	{
		Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			
			#pragma target 5.0

			#pragma vertex vert
			#pragma fragment frag5

			#pragma multi_compile FOAM_POW_1 FOAM_POW_2 FOAM_POW_N

			ENDCG
		}
	}

	SubShader
	{
		Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM

			#pragma target 3.0

			#pragma vertex vert
			#pragma fragment frag3

			#pragma multi_compile FOAM_POW_1 FOAM_POW_2 FOAM_POW_N

			ENDCG
		}
	}
}