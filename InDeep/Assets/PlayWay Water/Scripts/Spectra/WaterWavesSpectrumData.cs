using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace PlayWay.Water
{
	/// <summary>
	/// Resolves detailed info on a spectrum with setup of a specific water object.
	/// </summary>
	public class WaterWavesSpectrumData
	{
		public Water water;
		public WaterWavesSpectrum spectrum;

		private Texture2D texture;
		public Vector3[,] values;
		public WaveFrequency[] gerstnerWaves;
		public WaveFrequency[] cpuWaves;
		public float weight;
		public bool cpuWavesDirty;
		public float totalAmplitude;

		public WaterWavesSpectrumData(Water water, WaterWavesSpectrum spectrum)
		{
			this.water = water;
            this.spectrum = spectrum;
		}

		public Texture2D Texture
		{
			get
			{
				if(texture == null)
					CreateSpectrumTexture();

				return texture;
			}
		}

		public void ValidateSpectrum()
		{
			if(cpuWaves != null)
				return;

			var spectraRenderer = water.SpectraRenderer;
			int resolution = spectraRenderer.FinalResolution;
			int halfResolution = resolution / 2;
			int cpuMaxWaves = spectraRenderer.CpuMaxWaves;
			float cpuWaveThreshold = spectraRenderer.CpuWaveThreshold;

			var priorityList = new List<WaveFrequency>();

			if(values == null)
				values = new Vector3[resolution, resolution];

			if(water.Seed != 0)
				Random.seed = water.Seed;

			//var random = water.Seed != 0 ? new System.Random(water.Seed) : new System.Random();

			spectrum.ComputeSpectrum(values, null);

			// debug spectrum
			/*for(int x=0; x<resolution; ++x)
			{
				for(int y = 0; y < resolution; ++y)
					values[x, y] = new Vector3(0.0f, 0.0f, values[x, y].z);
			}
			values[7, 8] = new Vector3(1.0f, 0.0f, values[7, 8].z);*/

			var gerstnerWaves = new Heap<WaveFrequency>(40);

			// write to texture and find meaningful waves
			const float pix2 = 6.2831853f;
            float frequencyScale = pix2 / spectrum.TileSize;
			float gravity = spectrum.Gravity;
			float halfk = Mathf.PI  / spectrum.TileSize;

			totalAmplitude = 0.0f;

			for(int x = 0; x < resolution; ++x)
			{
				float kx = frequencyScale * (x - halfResolution);
				int u = (x + halfResolution) % resolution;

				for(int y = 0; y < resolution; ++y)
				{
					float ky = frequencyScale * (y - halfResolution);
					int v = (y + halfResolution) % resolution;

					Vector3 s = values[u, v];
					float amplitude = Mathf.Sqrt(s.x * s.x + s.y * s.y);
					float k = Mathf.Sqrt(kx * kx + ky * ky);
					float w = Mathf.Sqrt(gravity * k);
                    float gerstnerPriority = amplitude * w;
					
					if(amplitude >= cpuWaveThreshold)
						priorityList.Add(new WaveFrequency(u, v, kx, ky, k, w, amplitude, gerstnerPriority));

					// don't consider breaking waves for gerstner
					if(amplitude * spectrum.TileSize > 0.5f * Mathf.PI / k)
						continue;
					
					if(gerstnerWaves.Count == 40)
					{
						if(gerstnerWaves.Max.gerstnerPriority < gerstnerPriority)
						{
							gerstnerWaves.ExtractMax();
							gerstnerWaves.Insert(new WaveFrequency(u, v, kx, ky, k + Random.Range(-halfk, halfk), w, amplitude, gerstnerPriority));
						}
					}
					else
						gerstnerWaves.Insert(new WaveFrequency(u, v, kx, ky, k + Random.Range(-halfk, halfk), w, amplitude, gerstnerPriority));

					totalAmplitude += amplitude;
                }
			}

			this.gerstnerWaves = gerstnerWaves.ToArray();
			cpuWaves = priorityList.ToArray();
			SortCpuWaves();

			if(cpuWaves.Length > spectraRenderer.CpuMaxWaves)
				System.Array.Resize(ref cpuWaves, cpuMaxWaves);
		}

		public void UpdateSpectralValues(Vector2 windDirection, float directionality)
		{
			ValidateSpectrum();

			if(cpuWavesDirty)
			{
				cpuWavesDirty = false;

				var cpuWaves = this.cpuWaves;
				int numCpuWaves = cpuWaves.Length;
                float directionalityInv = 1.0f - directionality;
				int resolution = water.SpectraRenderer.FinalResolution;

				for(int i = 0; i < numCpuWaves; ++i)
					cpuWaves[i].UpdateSpectralValues(values, windDirection, directionalityInv, resolution);

				int numGerstners = gerstnerWaves.Length;

				for(int i = 0; i < numGerstners; ++i)
					gerstnerWaves[i].UpdateSpectralValues(values, windDirection, directionalityInv, resolution);

				SortCpuWaves();
            }
		}

		public void SortCpuWaves()
		{
			System.Array.Sort(cpuWaves, (a, b) =>
			{
				if(a.amplitude > b.amplitude)
					return -1;
				else
					return a.amplitude == b.amplitude ? 0 : 1;
			});
		}

		public void Dispose(bool onlyTexture)
		{
			if(texture != null)
			{
				Object.Destroy(texture);
				texture = null;
			}

			if(!onlyTexture)
			{
				values = null;
				cpuWaves = null;
				cpuWavesDirty = true;
			}
		}

		private void CreateSpectrumTexture()
		{
			ValidateSpectrum();

			int resolution = water.SpectraRenderer.FinalResolution;
			int halfResolution = resolution / 2;

			// create texture
			texture = new Texture2D(resolution, resolution, TextureFormat.RGBAFloat, false, true);
			texture.filterMode = FilterMode.Point;
			texture.wrapMode = TextureWrapMode.Repeat;

			// fill texture
			for(int x = 0; x < resolution; ++x)
			{
				int u = (x + halfResolution) % resolution;

				for(int y = 0; y < resolution; ++y)
				{
					int v = (y + halfResolution) % resolution;

					Vector3 s = values[u, v];
					texture.SetPixel(u, v, new Color(s.x, s.y, s.z, 1.0f));
				}
			}

			texture.Apply(false, true);
		}

		public struct WaveFrequency : System.IComparable<WaveFrequency>
		{
			public readonly int u, v;
			public readonly float kx, ky;
			public readonly float nkx, nky;
			public readonly float w;
			public readonly float k;

			public float amplitude;
			public float gerstnerPriority;
			public float offset;

			public WaveFrequency(int u, int v, float kx, float ky, float k, float w, float amplitude, float gerstnerPriority)
			{
				this.u = u;
				this.v = v;
				this.kx = kx;
				this.ky = ky;
				this.k = k;
				float kl = Mathf.Sqrt(kx * kx + ky * ky);
				this.nkx = k != 0 ? kx / kl : 0.707107f;
				this.nky = k != 0 ? ky / kl : 0.707107f;
				this.amplitude = 2.0f * amplitude;
				this.offset = 0.0f;
                this.w = w;
				this.gerstnerPriority = gerstnerPriority;
			}

			public void UpdateSpectralValues(Vector3[,] spectrum, Vector2 windDirection, float directionalityInv, int resolution)
			{
				var s = spectrum[u, v];

				float dp = windDirection.x * nkx + windDirection.y * nky;
				float phi = Mathf.Acos(dp * 0.999f);
				float scale = Mathf.Sqrt(1.0f + s.z * Mathf.Cos(2.0f * phi));
				if(dp < 0.0f) scale *= directionalityInv;

				float sx = s.x * scale;
				float sy = s.y * scale;
				
				amplitude = 2.0f * Mathf.Sqrt(sx * sx + sy * sy);
				offset = Mathf.Atan2(Mathf.Abs(sx), Mathf.Abs(sy));

				if(sy > 0.0f)
				{
					amplitude = -amplitude;
					offset = -offset;
				}

				if(sx < 0.0f) offset = -offset;

				gerstnerPriority = amplitude * w;
            }

			public Vector2 GetHorizontalDisplacementAt(float x, float z, float t)
			{
				float dot = kx * x + ky * z;
				float c = amplitude * Mathf.Cos(dot + t * w + offset);

				return new Vector2(nkx * c, nky * c);
			}

			public Vector3 GetDisplacementAt(float x, float z, float t)
			{
				float dot = kx * x + ky * z;

				float s, c;
				FastMath.SinCos2048(dot + t * w + offset, out s, out c);

				c *= amplitude;

				return new Vector3(nkx * c, s * amplitude, nky * c);
			}
			
			public float GetHeightAt(float x, float z, float t)
			{
				float dot = kx * x + ky * z;
				return amplitude * Mathf.Sin(dot + t * w + offset);
			}

			// used by the heap to identify best waves for gerstner
			public int CompareTo(WaveFrequency other)
			{
				return other.gerstnerPriority.CompareTo(gerstnerPriority);
			}
		}
	}
}
