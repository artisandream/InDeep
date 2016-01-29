#include "../Utility/NoiseLib.cginc"

#ifndef _GERSTNER_WAVES_COUNT
	#if SHADER_TARGET >= 50
		#define _GERSTNER_WAVES_COUNT 20
	#elif SHADER_TARGET == 30
		#define _GERSTNER_WAVES_COUNT 20
	#else
		#define _GERSTNER_WAVES_COUNT 12
	#endif
#endif

sampler2D	_GlobalHeightMap;
sampler2D	_GlobalDisplacementMap;
half		_DisplacementsScale;

sampler2D	_LocalDisplacementMap;
sampler2D	_LocalNormalMap;

float4		_LocalMapsCoords;
float3		_DistantFadeFactors;
float		_WaterTileSize;

half2		_GerstnerOrigin;
half4		_GrAmp[5];
half4		_GrFrq[5];
half4		_GrOff[5];
half4		_GrAB[5];
half4		_GrCD[5];

void Gerstner(float2 vertex, half4 amplitudes, half4 k, half4 offset, half4 dirAB, half4 dirCD, half t, inout half3 displacement, inout half2 normal)
{
	half4 dp = k.xyzw * half4(dot(dirAB.xy, vertex), dot(dirAB.zw, vertex), dot(dirCD.xy, vertex), dot(dirCD.zw, vertex));

	half4 c, s;
	sincos(dp + offset, s, c);

	// displacement
	half4 ab = amplitudes.xxyy * dirAB.xyzw;
	half4 cd = amplitudes.zzww * dirCD.xyzw;
	displacement.x += dot(c, half4(ab.xz, cd.xz));
	displacement.z += dot(c, half4(ab.yw, cd.yw));
	displacement.y += dot(s, amplitudes);

	// normal
	ab *= k.xxyy;
	cd *= k.zzww;

	normal.x += dot(c, half4(ab.xz, cd.xz));
	normal.y += dot(c, half4(ab.yw, cd.yw));
}

inline half4 GetOcclusionDir(half3 partialDir)
{
	return half4(partialDir.xyz, 1.0 - dot(partialDir, half3(1, 1, 1)));
}

inline void DistanceMask(float4 posWorld, inout float2 fftUV, out half mask)
{
#if SHADER_TARGET >= 30
	float maskIntensity = (length(_WorldSpaceCameraPos.xz - posWorld.xz) - _WaterTileSize) / (_WaterTileSize * 3.5);
	maskIntensity = sqrt(saturate(maskIntensity));
	half2 dualMask = half2(Perlin3D(half3(fftUV.xy * 2, _Time.x)), Perlin3D(half3(fftUV.xy * 2, 2 + _Time.x)));
	half w = maskIntensity;
	fftUV += dualMask * 0.02 * w * maskIntensity;
	mask = lerp(1, dualMask.x * 0.5 + 0.5, w) * (1.0 - w * w);
#else
	mask = 1;
#endif
}

inline half4 approxTanh(half4 x)
{
	return x / sqrt(1.0 + x * x);
}

inline void TransformVertex(inout float4 posWorld, out half2 normal, out float2 fftUV, out float3 totalDisplacement, out half mask)
{
	totalDisplacement = float3(0, 0, 0);
	normal = half2(0, 0);
	fftUV = float2(posWorld.xz) / _WaterTileSize;

	#if _GERSTNER_WAVES
		float2 samplePos = _GerstnerOrigin - posWorld.xz;

		for (int i = 0; i < (_GERSTNER_WAVES_COUNT / 4); ++i)
			Gerstner(samplePos, _GrAmp[i], _GrFrq[i], _GrOff[i], _GrAB[i], _GrCD[i], _Time.y, /*out*/ totalDisplacement, /*out*/ normal);

		totalDisplacement.xz *= -_DisplacementsScale;
	#endif

	DistanceMask(posWorld, fftUV, mask);

	#if _FFT_WAVES
		float lod = length(_WorldSpaceCameraPos.xyz - posWorld.xyz) * _DistantFadeFactors.z;
		lod = log(lod + 1.0);

		float displacement = (tex2Dlod(_GlobalHeightMap, float4(fftUV, 0, lod)).r);
		float2 horizontalDisplacement = tex2Dlod(_GlobalDisplacementMap, float4(fftUV, 0, lod)).xy * _DisplacementsScale;
		
		float3 mergedDisplacement = float3(horizontalDisplacement.x, displacement, horizontalDisplacement.y);

		totalDisplacement += mergedDisplacement;
	#endif

	#ifdef _WATER_OVERLAYS
		half4 localMapsUv = half4((posWorld.xz + _LocalMapsCoords.xy) * _LocalMapsCoords.zz, 0, 0);
		half3 overlayDisplacement = tex2Dlod(_LocalDisplacementMap, localMapsUv);

		totalDisplacement += overlayDisplacement * half3(_DisplacementsScale, 1.0, _DisplacementsScale);
	#endif

	posWorld.xyz += totalDisplacement;
}
