using UnityEngine;

namespace PlayWay.Water
{
	static public class WaterDebug
	{
		static public void WriteAllMaps(Water water)
		{
#if DEBUG && WATER_DEBUG
			var wavesFFT = water.GetComponent<WaterWavesFFT>();
			SaveTexture(wavesFFT.HeightMap, "PlayWay Water - FFT Height Map.png");
			SaveTexture(wavesFFT.SlopeMap, "PlayWay Water - FFT Slope Map.png");
			SaveTexture(wavesFFT.DisplacementMap, "PlayWay Water - FFT Displacement Map.png");

			SaveTexture(water.SpectraRenderer.GetSpectrum(SpectraRenderer.SpectrumType.RawOmnidirectional), "PlayWay Water - Spectrum Raw Omnidirectional.png");
			SaveTexture(water.SpectraRenderer.GetSpectrum(SpectraRenderer.SpectrumType.RawDirectional), "PlayWay Water - Spectrum Raw Directional.png");
			SaveTexture(water.SpectraRenderer.GetSpectrum(SpectraRenderer.SpectrumType.Height), "PlayWay Water - Spectrum Height.png");
			SaveTexture(water.SpectraRenderer.GetSpectrum(SpectraRenderer.SpectrumType.Slope), "PlayWay Water - Spectrum Slope.png");
			SaveTexture(water.SpectraRenderer.GetSpectrum(SpectraRenderer.SpectrumType.Displacement), "PlayWay Water - Spectrum Displacement.png");
#endif
		}

		static public void SaveTexture(Texture tex, string name)
		{
#if DEBUG && WATER_DEBUG
			if(tex == null)
				return;

			var tempRT = new RenderTexture(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			Graphics.Blit(tex, tempRT);

			RenderTexture.active = tempRT;

			var tex2d = new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, false);
			tex2d.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
			tex2d.Apply();

			RenderTexture.active = null;

			System.IO.File.WriteAllBytes(name, tex2d.EncodeToPNG());

			tex2d.Destroy();
			tempRT.Destroy();
#endif
		}
	}
}
