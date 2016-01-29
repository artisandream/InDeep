Shader "PlayWay Water/Depth/CopyMix" {

	CGINCLUDE
		#include "UnityCG.cginc"

		struct appdata_t
		{
			float4 vertex : POSITION;
			float2 texcoord : TEXCOORD0;
		};

		struct v2f
		{
			float4 vertex : SV_POSITION;
			float2 texcoord : TEXCOORD0;
		};

		sampler2D_float _CameraDepthTexture;
		sampler2D_float _WaterDepthTexture;
		sampler2D _WaterMask;

		v2f vert(appdata_t v)
		{
			v2f o;
			o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
			o.texcoord = v.texcoord.xy;
			return o;
		}

		float4 frag(v2f i) : SV_Target
		{
			return tex2D(_CameraDepthTexture, i.texcoord);
		}

		float4 fragMix(v2f i) : SV_Target
		{
			float d1 = tex2D(_CameraDepthTexture, i.texcoord);
			float d2 = tex2D(_WaterDepthTexture, i.texcoord);
			float mask = tex2D(_WaterMask, i.texcoord).x;

			//if (LinearEyeDepth(d2) < mask)
			//	return d1;
			 
			d2 *= max(0, (mask - LinearEyeDepth(d2)) * 1000000.0) + 1;

			return min(d1, d2);
		}
	ENDCG

	SubShader
	{ 
		Pass
		{
 			ZTest Always Cull Off ZWrite Off

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			ENDCG
		}

		Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment fragMix

			ENDCG
		}
	}
	Fallback Off 
}
