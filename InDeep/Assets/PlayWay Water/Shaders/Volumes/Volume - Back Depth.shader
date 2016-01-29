Shader "PlayWay Water/Volumes/Back"
{
	Properties
	{
		_Id("Id", Float) = 1
	}
		SubShader
	{
		Tags{ "CustomType" = "Water" }

		Pass
		{
			Cull Front
			ZTest Greater
			ZWrite On
			ColorMask RGB

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			sampler2D	_VolumesFrontDepth;
			sampler2D	_WaterDepthTexture;
			sampler2D	_LocalHeightData;
			float4		_LocalMapsCoords;
			float4x4	UNITY_MATRIX_VP_INVERSE;
			float		_Id;
			
			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex		: SV_POSITION;
				float4 screenPos	: TEXCOORD0;
				float4 heightmapUv	: TEXCOORD1;
				float height		: TEXCOORD2;
			};

			inline float2 LinearEyeDepth2(float2 z)
			{
				return 1.0 / (_ZBufferParams.z * z + _ZBufferParams.w);
			}

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				float3 posWorld = mul(_Object2World, v.vertex);
				o.height.x = posWorld.y;
				o.heightmapUv.xy = (posWorld.xz + _LocalMapsCoords.xy) * _LocalMapsCoords.zz;
				o.heightmapUv.zw = (_WorldSpaceCameraPos.xz + _LocalMapsCoords.xy) * _LocalMapsCoords.zz;
				o.screenPos = ComputeScreenPos(o.vertex);
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float3 frontDepthPack = tex2Dproj(_VolumesFrontDepth, i.screenPos);
				float frontDepth = frontDepthPack.x;
				float frontAboveWater = frontDepthPack.z;
				float waterHeight = tex2D(_LocalHeightData, i.heightmapUv.xy);

				//clip(-abs(frontDepthPack.y - _Id) + 0.05);

				float2 depths = LinearEyeDepth2(float2(frontDepth, i.screenPos.z / i.screenPos.w));
				float cameraDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_WaterDepthTexture, i.screenPos));



				/*float4 nearScreenPos = float4(i.screenPos.xy / i.screenPos.w, 0, 1);
				//float4 nearScreenPos = i.screenPos;
				//nearScreenPos.z = 0;
				float4 nearWorldPos = mul(UNITY_MATRIX_VP_INVERSE, nearScreenPos);
				nearWorldPos.xyz /= nearWorldPos.w;
				float2 nearTexUv = (nearWorldPos.xz + _LocalMapsCoords.xy) * _LocalMapsCoords.zz;*/
				float3 nearWorldPos = _WorldSpaceCameraPos.xyz;
				float2 nearTexUv = i.heightmapUv.zw;

				float cameraWaterHeight = tex2D(_LocalHeightData, nearTexUv);

				if (abs(frontAboveWater - 0.5) < 0.35 || depths.x > depths.y)
					frontAboveWater = (cameraWaterHeight < nearWorldPos.y ? 1 : 0);



				float surfaceMask = depths.x <= cameraDepth && depths.y >= cameraDepth ? 1 : 0;
				float backfillMask = surfaceMask;

				if (cameraWaterHeight > nearWorldPos.y)
					backfillMask = 1.0 - backfillMask;

				if (i.height <= waterHeight)
				{
					if (cameraWaterHeight < nearWorldPos.y && frontAboveWater > 0.5)
						surfaceMask = 1.0;

					backfillMask = 1.0;
				}

				if (cameraWaterHeight < nearWorldPos.y && frontAboveWater < 0.5)
					backfillMask = 0;

				//float backfillMask = /*frontAboveWater > 0.5 && */i.height <= waterHeight ? 1 : 0;
				float frontfillMask = frontAboveWater < 0.5 && i.height > waterHeight ? 1 : 0;
				float surfaceMaskFinal = surfaceMask > 0.5 ? depths.y * 1.05 : 0;

				return float4(surfaceMaskFinal, backfillMask, surfaceMask > 0.5 ? 1 : 0, 0);
				//return float4(0, i.depth.x / i.depth.y, 0, 0);
			}
			ENDCG
		}
	}
}
