Shader "PlayWay Water/Standard Volume"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}

	_DepthColor("Depth Color", Color) = (0.0, 0.012, 0.05)

		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
		_SpecColor("Specular", Color) = (0.2,0.2,0.2)
		//_SpecGlossMap("Specular", 2D) = "white" {}

		_BumpScale("Bump Scale", Vector) = (1.0, 1.0, 0.0, 0.0)
		_BumpMap("Normal Map", 2D) = "bump" {}

	_DisplacementNormalsIntensity("Displacement Normals Intensity", Float) = 1.4
		_GlobalHeightMap("", 2D) = "grey" {}
	_GlobalNormalMap("", 2D) = "black" {}

	_GlobalDisplacementMap("", 2D) = "black" {}

		_DisplacementsScale("Horizontal Displacement Scale", Float) = 1.0

		_Parallax("Height Scale", Range(0.005, 0.08)) = 0.02
		_ParallaxMap("Height Map", 2D) = "black" {}

	_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
		_OcclusionMap("Occlusion", 2D) = "white" {}

	_EmissionColor("Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}

	_WaterMask("", 2D) = "black" {}

	_DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
	_DetailNormalMapScale("Scale", Float) = 1.0
		_DetailNormalMap("Normal Map", 2D) = "bump" {}

	_NormalsFadeParams("Normals Fade Params (bias, inv fade, unused, unused)", Vector) = (0.95, 0.01, 0, 0)

		_PlanarReflectionTex("Planar Reflection", 2D) = "black" {}
	_PlanarReflectionPack("Planar reflection (distortion, intensity, offset Y, unused)", Vector) = (1.0, 0.45, -0.3, 0.0)

		_SpecularFresnelBias("Specular Fresnel Bias", Float) = 0.02041
		_RefractionDistortion("Refraction Distortion", Float) = 0.55

		_WaterTileSize("Heightmap Tile Size", Float) = 100.0

		_Foam("Foam (intensity, cutoff)", Vector) = (0.1, 0.375, 0.0, 0.0)
		_FoamTex("Foam texture ", 2D) = "black" {}
	_FoamNormalMap("Foam Normal Map", 2D) = "bump" {}
	_FoamNormalScale("Foam Normal Scale", Float) = 2.2
		_FoamSpecularColor("Foam Specular Color", Color) = (1, 1, 1, 1)
		_InvFadeParemeter("Auto blend parameter (Edge, Shore, Distance scale)", Vector) = (0.15 ,0.15, 0.5, 1.0)

	_FoamMapWS("", 2D) = "black" {}
	_AbsorptionColor("", Color) = (0.35, 0.04, 0.001, 1.0)

	_SubsurfaceScatteringPack("Subsurface Scattering", Vector) = (1.0, 0.15, 1.65, 0)

		_SlopeVariance("", 3D) = "black" {}

	_TesselationFactor("Tesselation Factor", Float) = 14
		_MaxDisplacement("", Float) = 10

		// -- gerstner
		_GerstnerOrigin("", Vector) = (0, 0, 0, 0)

		_GrAmp0("", Vector) = (0, 0, 0, 0)
		_GrOff0("", Vector) = (0, 0, 0, 0)
		_GrFrq0("", Vector) = (0, 0, 0, 0)
		_GrAB0("", Vector) = (0, 0, 0, 0)
		_GrCD0("", Vector) = (0, 0, 0, 0)

		_GrAmp1("", Vector) = (0, 0, 0, 0)
		_GrOff1("", Vector) = (0, 0, 0, 0)
		_GrFrq1("", Vector) = (0, 0, 0, 0)
		_GrAB1("", Vector) = (0, 0, 0, 0)
		_GrCD1("", Vector) = (0, 0, 0, 0)

		_GrAmp2("", Vector) = (0, 0, 0, 0)
		_GrOff2("", Vector) = (0, 0, 0, 0)
		_GrFrq2("", Vector) = (0, 0, 0, 0)
		_GrAB2("", Vector) = (0, 0, 0, 0)
		_GrCD2("", Vector) = (0, 0, 0, 0)

		_GrAmp3("", Vector) = (0, 0, 0, 0)
		_GrOff3("", Vector) = (0, 0, 0, 0)
		_GrFrq3("", Vector) = (0, 0, 0, 0)
		_GrAB3("", Vector) = (0, 0, 0, 0)
		_GrCD3("", Vector) = (0, 0, 0, 0)

		_GrAmp4("", Vector) = (0, 0, 0, 0)
		_GrOff4("", Vector) = (0, 0, 0, 0)
		_GrFrq4("", Vector) = (0, 0, 0, 0)
		_GrAB4("", Vector) = (0, 0, 0, 0)
		_GrCD4("", Vector) = (0, 0, 0, 0)
		// --


		[Enum(UV0,0,UV1,1)] _UVSec("UV Set for secondary textures", Float) = 0

		// UI-only data
		[HideInInspector] _EmissionScaleUI("Scale", Float) = 0.0
		[HideInInspector] _EmissionColorUI("Color", Color) = (1,1,1)

		// Blending state
		[HideInInspector] _Mode("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
		[HideInInspector] _ZWrite("__zw", Float) = 1.0
	}

	CGINCLUDE
		#define UNITY_SETUP_BRDF_INPUT SpecularSetup
	ENDCG


	/*
	 * HIGH-QUALITY + TESSELATION
	 */
	/*SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" "Queue"="Transparent" "CustomType"="Water" }
		LOD 300
		
		GrabPass { "_RefractionTex2" }

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			//Stencil 
			//{
			//	Ref 0 
			//	ReadMask 64 
			//	Comp Equal 
			//	Pass Keep
			//}
			
			Blend [_SrcBlend] [_DstBlend]
			ZWrite On
			Cull Front
			ZTest LEqual

			CGPROGRAM
			#pragma target 3.0
			#pragma only_renderers d3d11
			
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			//#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			//#pragma shader_feature _PARALLAXMAP
			#pragma shader_feature _PLANAR_REFLECTIONS
			#pragma shader_feature _CUBEMAP_REFLECTIONS
			#pragma shader_feature _WATER_FOAM_WS
			#pragma shader_feature _WATER_REFRACTION
			#pragma shader_feature _INCLUDE_SLOPE_VARIANCE
			#pragma shader_feature _PROJECTION_GRID
			#pragma shader_feature _DISPLACED_VOLUME
			//#define _QUADS 1

			#define WATER_VOLUME 1

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			
			#if UNITY_CAN_COMPILE_TESSELLATION
				#pragma vertex tessvert_surf
				#pragma fragment fragForwardBase

				#pragma hull hs_surf
				#pragma domain ds_surf
			#endif

			#define POST_TESS_VERT vertForwardBase
			#define TESS_OUTPUT VertexOutputForwardBase

			#include "UnityStandardCore.cginc"
			#include "WaterTessellation.cginc"
			

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual

			//Stencil 
			//{
			//	Ref 0 
			//	ReadMask 64 
			//	Comp Equal 
			//	Pass Keep
			//}

			CGPROGRAM
			#pragma target 3.0
			#pragma only_renderers d3d11
			
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			//#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _INCLUDE_SLOPE_VARIANCE
			//#pragma shader_feature _PARALLAXMAP
			#pragma shader_feature _PROJECTION_GRID
			#pragma shader_feature _DISPLACED_VOLUME
			//#define _QUADS 1

			#define WATER_VOLUME 1
			
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			
			//#pragma vertex vertForwardAdd
			//#pragma fragment fragForwardAdd

			#if UNITY_CAN_COMPILE_TESSELLATION
				#pragma vertex tessvert_surf
				#pragma fragment fragForwardAdd

				#pragma hull hs_surf
				#pragma domain ds_surf
			#endif

			#define POST_TESS_VERT vertForwardAdd
			#define TESS_OUTPUT VertexOutputForwardAdd

			#include "UnityStandardCore.cginc"
			#include "WaterTessellation.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 3.0
			#pragma only_renderers d3d11

			//#pragma shader_feature _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _PROJECTION_GRID
			#pragma shader_feature _DISPLACED_VOLUME
			//#define _QUADS 1
			#pragma multi_compile_shadowcaster

			#if UNITY_CAN_COMPILE_TESSELLATION
				#pragma vertex tessvert_surf
				#pragma fragment fragShadowCaster

				#pragma hull hs_surf
				#pragma domain ds_surf
			#endif

			#define POST_TESS_VERT vertShadowCaster
			#define TESS_OUTPUT VertexOutputShadowCaster
			#define _SHADOWS_PASS 1

			#define WATER_VOLUME 1

			#include "UnityStandardShadow.cginc"
			#include "WaterTessellation.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Depth rendering pass
		Pass {
			Name "Depth"
			Tags { "LightMode" = "VertexLMRGBM" }
			
			ZWrite On
			ZTest LEqual
			Cull Front

			CGPROGRAM
			#pragma target 3.0
			#pragma only_renderers d3d11

			//#pragma shader_feature _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _FFT_WAVES
			#pragma shader_feature _GERSTNER_WAVES
			#pragma shader_feature _PROJECTION_GRID
			#pragma shader_feature _DISPLACED_VOLUME
			//#define _QUADS 1
			#define SHADOWS_DEPTH 1

			#if UNITY_CAN_COMPILE_TESSELLATION
				#pragma vertex tessvert_surf
				#pragma fragment fragDepth

				#pragma hull hs_surf
				#pragma domain ds_surf
			#endif

			#define POST_TESS_VERT vertDepth
			#define TESS_OUTPUT VertexOutputDepth
			#define _SHADOWS_PASS 1

			#define WATER_VOLUME 1

			#include "UnityStandardShadow.cginc"
			#include "WaterTessellation.cginc"

			ENDCG
		}

		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		/ *Pass
		{
			Name "META" 
			Tags { "LightMode"="Meta" }

			Cull Front

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			//#pragma shader_feature _EMISSION
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2

			#include "UnityStandardMeta.cginc"
			ENDCG
		}* /
	}*/

	/*
	 * HIGH-QUALITY
	 */
	SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" "Queue"="Transparent" "CustomType"="Water" }
		LOD 300
		
		GrabPass { "_RefractionTex2" }

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			//Blend [_SrcBlend] [_DstBlend]
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite On
			Cull Front
			ZTest LEqual

			/*Stencil 
			{
				Ref 0 
				ReadMask 64 
				Comp Equal 
				Pass Keep
			}*/

			CGPROGRAM
			#pragma target 3.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles
			
			// -------------------------------------
					
			//#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			//#pragma shader_feature _EMISSION
			//#pragma shader_feature _SPECGLOSSMAP
			//#pragma shader_feature ___ _DETAIL_MULX2
			//#pragma shader_feature _PARALLAXMAP
			//#pragma shader_feature _PLANAR_REFLECTIONS
			#pragma shader_feature _CUBEMAP_REFLECTIONS
			#pragma shader_feature _WATER_FOAM_WS
			#pragma shader_feature _WATER_REFRACTION
			#pragma shader_feature _INCLUDE_SLOPE_VARIANCE
			#pragma shader_feature _PROJECTION_GRID
			#pragma shader_feature _DISPLACED_VOLUME
			#pragma shader_feature _VOLUMETRIC_LIGHTING

			#define WATER_VOLUME 1

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			
			#pragma vertex vertForwardBase
			#pragma fragment fragForwardBase

			#include "UnityStandardCore.cginc"
			

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual

			/*Stencil 
			{
				Ref 0 
				ReadMask 64 
				Comp Equal 
				Pass Keep
			}*/

			CGPROGRAM
			#pragma target 3.0
			// GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles

			// -------------------------------------

			
			#pragma shader_feature _NORMALMAP
			//#pragma shader_feature _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			//#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _INCLUDE_SLOPE_VARIANCE
			//#pragma shader_feature _PARALLAXMAP
			#pragma shader_feature _PROJECTION_GRID
			#pragma shader_feature _DISPLACED_VOLUME
			#pragma shader_feature _VOLUMETRIC_LIGHTING

			#define WATER_VOLUME 1
			
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			
			#pragma vertex vertForwardAdd
			#pragma fragment fragForwardAdd

			#include "UnityStandardCore.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual
			Cull Front

			CGPROGRAM
			#pragma target 3.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles
			
			// -------------------------------------


			//#pragma shader_feature _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _PROJECTION_GRID
			#pragma shader_feature _DISPLACED_VOLUME
			#pragma multi_compile_shadowcaster

			#define WATER_VOLUME 1

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}

			// ------------------------------------------------------------------
			//  Depth rendering pass
			Pass{
			Name "Depth"
			Tags{ "LightMode" = "VertexLMRGBM" }

			ZWrite On
			ZTest LEqual
			Cull Front

			CGPROGRAM
			#pragma target 3.0
			#pragma only_renderers d3d11

			//#pragma shader_feature _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _PROJECTION_GRID
			#pragma shader_feature _DISPLACED_VOLUME
			#define SHADOWS_DEPTH 1
			#define WATER_VOLUME 1

			#pragma vertex vertDepth
			#pragma fragment fragDepth

			#define _SHADOWS_PASS 1

			#include "UnityStandardShadow.cginc"

			ENDCG
		}

		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		/*Pass
		{
			Name "META" 
			Tags { "LightMode"="Meta" }

			Cull Front

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_meta

			//#pragma shader_feature _EMISSION
			#pragma shader_feature _SPECGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2

			#include "UnityStandardMeta.cginc"
			ENDCG
		}*/
	}

	/*
	 * MEDIUM-QUALITY
	 */
	/*SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" "Queue"="Geometry" "CustomType"="Water" }
		LOD 50
		
		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			Blend One Zero
			ZWrite On
			Cull Front
			ZTest LEqual

			CGPROGRAM
			#pragma target 2.0
			
			#pragma shader_feature _NORMALMAP
			//#pragma shader_feature _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			//#pragma shader_feature _SPECGLOSSMAP
			//#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PLANAR_REFLECTIONS
			//#pragma shader_feature _CUBEMAP_REFLECTIONS
			//#pragma shader_feature _WATER_FOAM_WS
			#pragma shader_feature _PROJECTION_GRID
			#pragma shader_feature _DISPLACED_VOLUME

			#define WATER_VOLUME 1

			#pragma skip_variants SHADOWS_SOFT DYNAMICLIGHTMAP_ON DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE SHADOWS_SCREEN SHADOWS_NATIVE SHADOWS_NONATIVE SHADOWS_CUBE
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
	
			#pragma vertex vertForwardBase
			#pragma fragment fragForwardBase

			#include "UnityStandardCore.cginc"
			

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual

			CGPROGRAM
			#pragma target 2.0

			
			#pragma shader_feature _NORMALMAP
			//#pragma shader_feature _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			//#pragma shader_feature _SPECGLOSSMAP
			//#pragma shader_feature ___ _DETAIL_MULX2
			//#pragma shader_feature _PARALLAXMAP
			#pragma shader_feature _DISPLACED_VOLUME

			#define WATER_VOLUME 1
			
			//#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			
			#pragma vertex vertForwardAdd
			#pragma fragment fragForwardAdd

			#include "UnityStandardCore.cginc"

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 2.0
			
			//#pragma shader_feature _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _DISPLACED_VOLUME
			#pragma multi_compile_shadowcaster

			#define WATER_VOLUME 1

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#include "UnityStandardShadow.cginc"

			ENDCG
		}

		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		//Pass
		//{
		//	Name "META" 
		//	Tags { "LightMode"="Meta" }

		//	Cull Front

		//	CGPROGRAM
		//	#pragma vertex vert_meta
		//	#pragma fragment frag_meta

		//	//#pragma shader_feature _EMISSION
		//	#pragma shader_feature _SPECGLOSSMAP
		//	#pragma shader_feature ___ _DETAIL_MULX2

		//	#include "UnityStandardMeta.cginc"
		//	ENDCG
		//}
	}*/

	//FallFront "VertexLit"
	//CustomEditor "StandardShaderGUI"
}
