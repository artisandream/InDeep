Shader "PlayWay Water/Particles/Particles"
{
	Properties
	{

	}

	SubShader
	{
		Cull Off ZWrite Off ZTest Always
		Blend One One

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex		: POSITION;
				float2 uv			: TEXCOORD0;
				float4 tangent		: TANGENT;
			};

			struct v2f
			{
				float4 vertex		: SV_POSITION;
				half4 uv			: TEXCOORD0;
				half amplitude		: TEXCOORD1;
				half4 dir			: TEXCOORD2;
				half k				: TEXCOORD3;
			};

			struct PsOutput
			{
				float4 displacement	: SV_Target0;
				float4 slope		: SV_Target1;
			};

			sampler2D _MainTex;
			float3 _ParticleFieldCoords;

			v2f vert (appdata vi)
			{
				v2f vo;

				float2 forward = vi.tangent.xy * _ParticleFieldCoords.z;
				float2 right = float2(forward.y, -forward.x);
				//float width = vi.tangent.z;
				float width = 1.0;

				vi.vertex.xy = (vi.vertex.xy + _ParticleFieldCoords.xy) * _ParticleFieldCoords.zz * 2.0 - 1.0;
				vo.vertex = half4(vi.vertex.xy + forward * (vi.uv.y - 0.5) + right * (vi.uv.x - 0.5), 0, 1);
				vo.vertex.y = -vo.vertex.y;
				vo.uv = half4(vi.uv * 3.14159, vo.vertex.xy);
				vo.amplitude = vi.vertex.z;
				vo.dir = half4(normalize(vi.tangent.xy), vi.tangent.xy);
				return vo;
			}

			PsOutput frag (v2f vo)
			{
				half2 s, c;
				sincos(vo.uv.xy, s, c);

				half fade = max(0, 1.0 - pow(max(abs(vo.uv.z), abs(vo.uv.w)), 2));

				/*half height = s.x * s.y * vo.amplitude * fade;
				//half displacement = c.y * height;
				half2 slope = vo.dir.xy * vo.amplitude * vo.k * s.xy;
				half2 displacement = vo.amplitude * c.y * s.x;

				PsOutput po;
				//po.displacement = half4(vo.dir.x * displacement, height, vo.dir.y * displacement, 0);
				//po.slope = half4(vo.dir.z * displacement, vo.dir.w * displacement, 0, 0) * 0.0005;
				po.displacement = half4(displacement.x, height, displacement.y, 0);
				po.slope = half4(slope, 0, 0);*/

				half2 s2, c2;
				sincos(vo.uv.xy * 2, s2, c2);

				half height = s.x * s.y * vo.amplitude;
				half2 displacement = s.x * s2.y * vo.dir.xy * vo.amplitude;
				half2 slope = s.x * s2.y * vo.dir.xy * vo.amplitude * vo.k;

				PsOutput po;
				po.displacement = half4(displacement.x, height, displacement.y, 0) * fade;
				po.slope = half4(slope.xy, max(0, (s.x * s.y * s.y - 0.7) * 0.5), 0) * fade;

				return po;
			}
			ENDCG
		}
	}
}
