Shader "PlayWay Water/Volumes/Back"
{
	Properties
	{
		_WaterId("", Vector) = (2, 1, 0, 0)
	}
	
	SubShader
	{
		Tags{ "CustomType" = "WaterVolume" }

		Pass
		{
			Cull Front
			ZTest Less
			ZWrite On
			ColorMask RG

			CGPROGRAM
			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			
			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex		: SV_POSITION;
				float2 depth		: TEXCOORD0;
			};

			float2 _WaterId;

			inline half LinearEyeDepthHalf(half z)
			{
				return 1.0 / (_ZBufferParams.z * z + _ZBufferParams.w);
			}

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.depth = o.vertex.zw;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				return float4(_WaterId.x, i.depth.x / i.depth.y, 0.0, 0.0);
			}
			ENDCG
		}
	}
}
