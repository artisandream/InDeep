Shader "PlayWay Water/Spray/Generator"
{
	Properties
	{
		_MainTex ("", 2D) = "" {}
		_Lambda("", Float) = 1
	}

	CGINCLUDE
	
	#include "UnityCG.cginc"

	struct VertexInput
	{
		float4 vertex	: POSITION;
		float2 uv0		: TEXCOORD0;
	};

	struct VertexOutput
	{
		float4 pos	: SV_POSITION;
		half2 uv	: TEXCOORD0;
	};

	struct ParticleData
	{
		float3 position;
		float3 velocity;
		float2 lifetime;
		float offset;
		float maxIntensity;
	};

	sampler2D _MainTex;
	sampler2D _HeightMap;
	half3 _Params;			// x - lambda, y - spawn rate, z - horizontal displacement scale / tile size

	AppendStructuredBuffer<ParticleData> particles : register(u1);

	VertexOutput vert (VertexInput vi)
	{
		VertexOutput vo;

		vo.pos = mul(UNITY_MATRIX_MVP, vi.vertex);
		vo.uv = vi.uv0;

		return vo;
	}

	float random(float2 p)
	{
		float2 r = float2(23.14069263277926, 2.665144142690225);
		return frac(cos(dot(p, r)) * 123456.0);
	}

	float gauss(float2 p)
	{
		return sqrt(-2.0f * log(random(p))) * sin(3.14159 * 2.0 * random(p * -0.3241241));
	}

	float halfGauss(float2 p)
	{
		return abs(sqrt(-2.0f * log(random(p))) * sin(3.14159 * 2.0 * random(p * -0.3241241)));
	}

	fixed4 frag(VertexOutput vo) : SV_Target
	{
		half2 displacement = tex2D(_MainTex, vo.uv);
		half3 j = half3(ddx(displacement.x), ddy(displacement.y), ddx(displacement.y)) * _Params.x;
		j.xy += 1.0;

		half jacobian = -(j.x * j.y - j.z * j.z);
		half spawnRate = jacobian;
		half r = random(displacement.xy);

		[branch]
		if( spawnRate > 0 && r > _Params.y)
		{
			spawnRate += 2.0;
			half intensity = log(spawnRate + 1) * halfGauss(displacement.yx);

			half height = tex2D(_HeightMap, vo.uv);

			half2 uvDisplacement = displacement * _Params.z;

			ParticleData particle;
			particle.position = float3(vo.uv.x + uvDisplacement.x, height - 0.2, vo.uv.y + uvDisplacement.y);
			particle.velocity = intensity * half3(displacement.x, 440, displacement.y) * 0.0033;
			particle.velocity.y = clamp(particle.velocity.y, 2.1, 8.0);
			//particle.velocity.y = 2.6;
		
			float ff = gauss(displacement.xy * -0.381241) * 3.14159 * 0.3;
			particle.velocity.xz = particle.velocity.xz * cos(ff) + particle.velocity.zx * sin(ff) * float2(-1, 1);

			//particle.lifetime = spawnRate * 10.0;
			particle.lifetime = 2.0 * intensity;
			particle.offset = r * 2;
			particle.maxIntensity = saturate(intensity);
			particles.Append(particle);
		}

		return 0;
	}

	ENDCG

	SubShader
	{
		Pass
		{
			Name "Blur"
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			
			#pragma target 5.0

			#pragma vertex vert
			#pragma fragment frag

			ENDCG
		}
	}
}