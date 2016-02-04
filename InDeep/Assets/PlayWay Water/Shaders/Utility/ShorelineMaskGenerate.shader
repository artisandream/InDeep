Shader "PlayWay Water/Utility/ShorelineMaskGenerate"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	CGINCLUDE
		#include "UnityCG.cginc"
		#include "NoiseLib.cginc"

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float4 vertex : SV_POSITION;
			float2 uv : TEXCOORD0;
		};

		float _ShorelineExtendRange;
		float _TerrainMinPoint;

		v2f vert(appdata v)
		{
			v2f o;
			o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
			o.uv = v.uv * (1.0 + _ShorelineExtendRange) - _ShorelineExtendRange * 0.5;
			return o;
		}

		v2f vertFull(appdata v)
		{
			v2f o;
			o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
			o.uv = v.uv;
			return o;
		}

		sampler2D _MainTex;

		float4 copyHeightMap(v2f i) : SV_Target
		{
			float h = -tex2D(_MainTex, i.uv).x;
			
			if(i.uv.x < 0 || i.uv.y < 0 || i.uv.x > 1 || i.uv.y > 1)
			{
				float perlin = 0.0;
				float p = 0.5;
				float f = 1;

				for (int x = 0; x < 5; ++x)
				{
					perlin += Perlin3D(float3(i.uv * f, 6.123412)) * p;
					p *= 0.5;
					f *= 2.0;
				}

				float2 d;
				d.x = i.uv.x < 0 ? -i.uv.x : (i.uv.x > 1 ? i.uv.x - 1.0 : 0.0);
				d.y = i.uv.y < 0 ? -i.uv.y : (i.uv.y > 1 ? i.uv.y - 1.0 : 0.0);

				float s = length(d) / (_ShorelineExtendRange * 0.5);

				h = lerp(_TerrainMinPoint, max(h, 90), s * s) + perlin * 40 * sqrt(s);
			}

			return h;
		}

		float4 heightMapToMask(v2f i) : SV_Target
		{
			half h = max(0, tex2D(_MainTex, i.uv).x);
			half mask = sqrt(9.81 * tanh(h * 0.01)) / sqrt(9.81);

			return saturate(mask);
		}
	ENDCG

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment copyHeightMap

			ENDCG
		}

		Pass
		{
			CGPROGRAM

			#pragma vertex vertFull
			#pragma fragment heightMapToMask

			ENDCG
		}
	}
}
