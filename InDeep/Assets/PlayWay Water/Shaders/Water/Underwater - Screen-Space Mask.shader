Shader "PlayWay Water/Underwater/Screen-Space Mask"
{
	SubShader
	{
		Tags { "CustomType" = "Water" }

		Pass
		{
			ZTest Always
			Cull Front
			ColorMask R
			BlendOp Min

			CGPROGRAM
			
			#pragma target 5.0
			#pragma only_renderers d3d11

			#pragma multi_compile __ _FFT_WAVES
			#pragma multi_compile ____ _GERSTNER_WAVES
			#define _QUADS 1

			#if UNITY_CAN_COMPILE_TESSELLATION
				#pragma vertex tessvert_surf
				#pragma fragment maskFrag

				#pragma hull hs_surf
				#pragma domain ds_surf
			#endif

			#define BASIC_INPUTS 1
			#define POST_TESS_VERT vert
			#define TESS_OUTPUT VertexOutput

			#include "Underwater - Screen-Space Mask - Code.cginc"
			#include "WaterTessellation.cginc"

			ENDCG
		}
	}

	SubShader
	{
		Tags{ "CustomType" = "Water" }

		Pass
		{
			ZTest Always
			Cull Front
			ColorMask R
			BlendOp Min

			CGPROGRAM

			#pragma target 3.0

			#pragma multi_compile __ _FFT_WAVES
			#pragma multi_compile ____ _GERSTNER_WAVES

			#pragma vertex vert
			#pragma fragment maskFrag

			#include "Underwater - Screen-Space Mask - Code.cginc"

			ENDCG
		}
	}

	SubShader
	{
		Tags{ "CustomType" = "Water" }

		Pass
		{
			ZTest Always
			Cull Front
			ColorMask R
			BlendOp Min

			CGPROGRAM

			#pragma target 2.0

			#pragma multi_compile ____ _GERSTNER_WAVES

			#pragma vertex vert
			#pragma fragment maskFrag

			#include "Underwater - Screen-Space Mask - Code.cginc"

			ENDCG
		}
	}
}