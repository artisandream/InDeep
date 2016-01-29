﻿Shader "PlayWay Water/Depth/Water Depth"
{
	SubShader
	{
		Tags { "CustomType"="Water" }
		Cull Back

		Pass
		{
			Fog { Mode Off }

			CGPROGRAM

			#pragma target 5.0
			#pragma only_renderers d3d11

			#if UNITY_CAN_COMPILE_TESSELLATION
				#pragma vertex tessvert_surf
				#pragma fragment frag

				#pragma hull hs_surf
				#pragma domain ds_surf
			#endif

			#pragma multi_compile __ _FFT_WAVES
			#pragma multi_compile ____ _GERSTNER_WAVES
			#define _QUADS 1

			#define BASIC_INPUTS 1
			#define POST_TESS_VERT vert
			#define TESS_OUTPUT v2f

			#include "Depth - Code.cginc"
			#include "WaterTessellation.cginc"

			ENDCG
		}
	}

	SubShader
	{
		Tags { "CustomType"="Water" }
		Cull Back

		Pass
		{
			Fog { Mode Off }

			CGPROGRAM

			#pragma multi_compile __ _FFT_WAVES
			#pragma multi_compile ____ _GERSTNER_WAVES

			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			#include "Depth - Code.cginc"

			ENDCG
		}
	}

	SubShader
	{
		Tags{ "CustomType" = "Water" }
		Cull Back

		Pass
		{
			Fog{ Mode Off }

			CGPROGRAM

			#pragma multi_compile ____ _GERSTNER_WAVES

			#pragma target 2.0
			#pragma vertex vert
			#pragma fragment frag

			#include "Depth - Code.cginc"

			ENDCG
		}
	}
}