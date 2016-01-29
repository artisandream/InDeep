Shader "PlayWay Water/Underwater/Base IME"
{
	Properties
	{
		_MainTex ("", 2D) = "" {}
	}

	CGINCLUDE
	
	#define _VOLUMETRIC_LIGHTING 1

	#include "UnityCG.cginc"
	#include "WaterLib.cginc"

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

	sampler2D _MainTex;
	sampler2D _UnderwaterMask;
	sampler2D _VerticalDepthTex;
	float _Offset;
	half _DistortionIntensity;
	float4x4 UNITY_MATRIX_VP_INVERSE;

	sampler2D _DistortionTex;

	VertexOutput vert (VertexInput vi)
	{
		VertexOutput vo;

		vo.pos = mul(UNITY_MATRIX_MVP, vi.vertex);
		vo.uv = vi.uv0;

		return vo;
	}

	fixed4 forward(VertexOutput vo)
	{
#if UNITY_UV_STARTS_AT_TOP
		vo.uv.y -= 0.04;
#else
		vo.uv.y += 0.04;
#endif

		return tex2D(_MainTex, vo.uv).r;
	}

	fixed4 PropagateMask (VertexOutput vo) : SV_Target
	{
		fixed2 c = tex2D(_MainTex, vo.uv).rg;

#if UNITY_UV_STARTS_AT_TOP
		vo.uv.y -= _Offset;
#else
		vo.uv.y += _Offset;
#endif

		c.r += forward(vo);
		c.r += forward(vo);
		c.r += forward(vo);

		return fixed4(c, 0, 1);
	}

	fixed4 FinishMask (VertexOutput vo) : SV_Target
	{
		fixed2 c = tex2D(_MainTex, vo.uv).rg;

		return fixed4(c.r - c.g, 0, 0, 1);
	}

	half4 ime (VertexOutput vo) : SV_Target
	{
		fixed mask = 1.0 - tex2D(_UnderwaterMask, vo.uv);
		half verticalDepth = tex2D(_VerticalDepthTex, vo.uv);

		half4 color = tex2D(_MainTex, vo.uv);

		half4 screenPos = half4(vo.uv * 2 - 1, 0, 1);

		half4 pixelWorldSpacePos = mul(UNITY_MATRIX_VP_INVERSE, screenPos);
		pixelWorldSpacePos.xyz /= pixelWorldSpacePos.w;

		half3 ray = pixelWorldSpacePos - _WorldSpaceCameraPos;
		ray.xyz = normalize(ray.xyz) * 3;

#if UNITY_UV_STARTS_AT_TOP
		ray.y = -ray.y;
#endif

		float depth = tex2D(_CameraDepthTexture, vo.uv);
		depth = LinearEyeDepth(depth);

		half3 depthColor = ComputeDepthColor(pixelWorldSpacePos, ray.xyz, half3(0, 1, 0), half3(1, 1, 1));

		return half4(lerp(color.rgb, depthColor, mask * (1.0 - exp(-_AbsorptionColor * depth))), 1);
	}

	half4 ime2 (VertexOutput vo) : SV_Target
	{
		fixed mask = 1.0 - tex2D(_UnderwaterMask, vo.uv);

		half2 distortion = tex2D(_DistortionTex, vo.uv).xy - 0.75;
		vo.uv += distortion * mask * _DistortionIntensity;

		return tex2D(_MainTex, vo.uv);
	}

	ENDCG

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			
			#pragma target 3.0

			#pragma vertex vert
			#pragma fragment PropagateMask

			ENDCG
		}

		Pass
		{
			CGPROGRAM
			
			#pragma target 3.0

			#pragma vertex vert
			#pragma fragment FinishMask

			ENDCG
		}

		Pass
		{
			CGPROGRAM
			
			#pragma target 3.0

			#pragma vertex vert
			#pragma fragment ime

			ENDCG
		}

		Pass
		{
			CGPROGRAM
			
			#pragma target 3.0

			#pragma vertex vert
			#pragma fragment ime2

			ENDCG
		}
	}
}