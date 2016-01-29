Shader "PlayWay Water/Volumes/Front"
{
	Properties
	{
		_Id("Id", Float) = 1
	}
	SubShader
	{
		Tags { "CustomType" = "Water" }

		Pass
		{
			Cull Back
			ZTest Less
			ZWrite On
			ColorMask RGB
			
			CGPROGRAM
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
				float2 heightmapUv	: TEXCOORD1;
				float height		: TEXCOORD2;
			};

			sampler2D	_LocalHeightData;
			float4		_LocalMapsCoords;
			float _Id;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);

				float3 posWorld = mul(_Object2World, v.vertex);
				o.height = posWorld.y;
				o.heightmapUv = (posWorld.xz + _LocalMapsCoords.xy) * _LocalMapsCoords.zz;
				o.depth = o.vertex.zw;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float waterHeight = tex2D(_LocalHeightData, i.heightmapUv);

				return float4(i.depth.x / i.depth.y, _Id, waterHeight < i.height ? 1 : 0, 0);
			}
			ENDCG
		}
	}
}
