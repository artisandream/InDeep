using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace PlayWay.Water
{
	/// <summary>
	/// Renders different types of water spectra and animates them in time.
	/// <seealso cref="Water.SpectraRenderer"/>
	/// </summary>
	[System.Serializable]
	public class SpectraRenderer
	{
		[HideInInspector]
		[SerializeField]
		private Shader spectrumShader;

		[Tooltip("Higher values increase quality, but also decrease performance. Directly controls quality of waves, foam and spray.")]
		[SerializeField]
		private int resolution = 1024;

		[Tooltip("Determines if 32-bit precision buffers should be used for computations (Default: off). Not supported on most mobile devices. This setting has impact on performance, even on PCs.\n\nTips:\n 1024 and higher - The difference is clearly visible, use this option on higher quality settings.\n 512 or lower - Keep it disabled.")]
		[SerializeField]
		private bool highPrecision = true;

		[Tooltip("Determines how small waves should be considered by CPU in ongoing computations. Higher values will increase the precision of all wave computations done on CPU (GetHeightAt etc.), but may decrease performance. Most waves in the ocean spectrum have negligible visual impact and may be safely ignored.")]
		[SerializeField]
		private float cpuWaveThreshold = 0.008f;

		[SerializeField]
		private int cpuMaxWaves = 5000;

		public event System.Action ResolutionChanged;

		private Texture omnidirectionalSpectrum;
		private RenderTexture totalOmnidirectionalSpectrum;
		private RenderTexture directionalSpectrum;
		private RenderTexture heightSpectrum, slopeSpectrum, displacementSpectrum;

		private RenderBuffer[] renderTargetsx2;
		private RenderBuffer[] renderTargetsx3;
		private Vector2 windDirection;
		private bool directionalSpectrumDirty = true;
		private bool renderingSupport;
		private bool renderingSupportChecked;
		private int renderTimeProperty;
		private int finalResolution;
		private bool finalHighPrecision;
        private float lastRenderTime;
		private float cpuOffsetX, cpuOffsetZ;
		private float totalAmplitude;
		private float maxHeight;
		private float maxDisplacement;
		private float variancesProgress;

		private Material animationMaterial;

		private Dictionary<WaterWavesSpectrum, WaterWavesSpectrumData> spectrumDataCache;

		private Water water;
		
		internal void Start(Water water)
		{
			this.water = water;
			this.spectrumDataCache = new Dictionary<WaterWavesSpectrum, WaterWavesSpectrumData>();
			
			ResolveResolution();
			SetWindDirection(water.WindDirection);
			this.renderTimeProperty = Shader.PropertyToID("_RenderTime");

			this.animationMaterial = new Material(spectrumShader);
			this.animationMaterial.SetFloat(renderTimeProperty, Time.time);
			
			OnProfilesChanged();
			OnValidate(water);
		}
		
		public RenderTexture RawDirectionalSpectrum
		{
			get
			{
				if(directionalSpectrumDirty && Application.isPlaying)
				{
					CheckResources();
					RenderDirectionalSpectrum();
				}

				return directionalSpectrum;
			}
		}
		
		public int Resolution
		{
			get { return resolution; }
			set
			{
				if(resolution == value)
					return;

				resolution = value;
				ResolveResolution();
			}
		}

		public int FinalResolution
		{
			get { return finalResolution; }
		}

		public bool FinalHighPrecision
		{
			get { return finalHighPrecision; }
		}

		public bool HighPrecision
		{
			get { return highPrecision; }
		}

		public int CpuMaxWaves
		{
			get { return cpuMaxWaves; }
		}

		public float CpuWaveThreshold
		{
			get { return cpuWaveThreshold; }
		}

		public int AvgCpuWaves
		{
			get { return Mathf.RoundToInt((float)spectrumDataCache.Values.Average(s => s.cpuWaves.Length)); }
		}

		public float TotalAmplitude
		{
			get { return totalAmplitude; }
		}

		public float MaxHeight
		{
			get { return maxHeight; }
		}

		public float MaxDisplacement
		{
			get { return maxDisplacement; }
		}

		public bool RenderingSupport
		{
			get
			{
				if(!renderingSupportChecked)
					CheckSupport();

				return renderingSupport;
			}
		}

		private RenderTexture TotalOmnidirectionalSpectrum
		{
			get
			{
				if(totalOmnidirectionalSpectrum == null)
				{
					totalOmnidirectionalSpectrum = new RenderTexture(finalResolution, finalResolution, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
					totalOmnidirectionalSpectrum.filterMode = FilterMode.Point;
					totalOmnidirectionalSpectrum.wrapMode = TextureWrapMode.Repeat;
				}

				return totalOmnidirectionalSpectrum;
			}
		}
		
		private void SetupMaterials()
		{
			animationMaterial.SetFloat("_Gravity", water.Gravity);
			animationMaterial.SetFloat("_PlaneSizeInv", 1.0f / water.TileSize);
			animationMaterial.SetVector("_TargetResolution", new Vector4(finalResolution, finalResolution, 0.0f, 0.0f));
		}

		public void CacheSpectrum(WaterWavesSpectrum spectrum)
		{
			GetSpectrumData(spectrum);
		}

		public void OnDestroy()
		{
			ResolutionChanged = null;

			if(totalOmnidirectionalSpectrum != null) Object.Destroy(totalOmnidirectionalSpectrum);
			if(directionalSpectrum != null) Object.Destroy(directionalSpectrum);
			if(heightSpectrum != null) Object.Destroy(heightSpectrum);
			if(slopeSpectrum != null) Object.Destroy(slopeSpectrum);
			if(displacementSpectrum != null) Object.Destroy(displacementSpectrum);

			if(spectrumDataCache != null)
			{
				foreach(var spectrumData in spectrumDataCache.Values)
					spectrumData.Dispose(false);

				spectrumDataCache = null;
			}
		}

		internal void OnValidate(Water water)
		{
			if(spectrumShader == null)
				spectrumShader = Shader.Find("PlayWay Water/Spectrum/Water Spectrum");

			ResolveResolution();
		}
		
		public Texture RenderHeightSpectrumAt(float time)
		{
			CheckResources();

			var directionalSpectrum = RawDirectionalSpectrum;

			lastRenderTime = time;
			animationMaterial.SetFloat(renderTimeProperty, time);
			Graphics.Blit(directionalSpectrum, heightSpectrum, animationMaterial, 0);

			return heightSpectrum;
		}

		public Texture RenderSlopeSpectrumAt(float time)
		{
			CheckResources();

			var directionalSpectrum = RawDirectionalSpectrum;

			lastRenderTime = time;
			animationMaterial.SetFloat(renderTimeProperty, time);
			Graphics.Blit(directionalSpectrum, slopeSpectrum, animationMaterial, 1);

			return slopeSpectrum;
		}

		public void RenderDisplacementsSpectraAt(float time, out Texture height, out Texture displacement)
		{
			CheckResources();

			height = heightSpectrum;
			displacement = displacementSpectrum;

			// it's necessary to set it each frame for some reason
			renderTargetsx2[0] = heightSpectrum.colorBuffer;
			renderTargetsx2[1] = displacementSpectrum.colorBuffer;

			var directionalSpectrum = RawDirectionalSpectrum;

			lastRenderTime = time;
			animationMaterial.SetFloat(renderTimeProperty, time);
			Graphics.SetRenderTarget(renderTargetsx2, heightSpectrum.depthBuffer);
			Graphics.Blit(directionalSpectrum, animationMaterial, 5);
			Graphics.SetRenderTarget(null);
		}

		public void RenderCompleteSpectraAt(float time, out Texture height, out Texture slope, out Texture displacement)
		{
			CheckResources();

			height = heightSpectrum;
			slope = slopeSpectrum;
			displacement = displacementSpectrum;
			
			// it's necessary to set it each frame for some reason
			renderTargetsx3[0] = heightSpectrum.colorBuffer;
			renderTargetsx3[1] = slopeSpectrum.colorBuffer;
			renderTargetsx3[2] = displacementSpectrum.colorBuffer;

			var directionalSpectrum = RawDirectionalSpectrum;

			lastRenderTime = time;
			animationMaterial.SetFloat(renderTimeProperty, time);
			Graphics.SetRenderTarget(renderTargetsx3, heightSpectrum.depthBuffer);
			Graphics.Blit(directionalSpectrum, animationMaterial, 2);
			Graphics.SetRenderTarget(null);
		}

		public Vector3 GetDisplacementAt(float x, float z, float spectrumStart, float spectrumEnd, float time)
		{
			lock (this)
			{
				x = cpuOffsetX - x;
				z = cpuOffsetZ - z;

				float threshold = 0.001f + FastMath.Pow2(spectrumStart);

				Vector3 result = new Vector3();

				foreach(var spectrum in spectrumDataCache.Values)
				{
					if(spectrum.weight < threshold || spectrum.cpuWaves == null) continue;

					spectrum.UpdateSpectralValues(windDirection, water.Directionality);

					Vector3 subResult = new Vector3();
					
					var cpuWaves = spectrum.cpuWaves;
					int startIndex = (int)(spectrumStart * cpuWaves.Length);
					int endIndex = (int)(spectrumEnd * cpuWaves.Length);
					
					for(int i = startIndex; i < endIndex; ++i)
						subResult += cpuWaves[i].GetDisplacementAt(x, z, time);

					result += subResult * spectrum.weight;
				}

				float scale = -water.HorizontalDisplacementScale;
                result.x = result.x * scale;
				result.z = result.z * scale;

				return result;
			}
		}

		public Vector3 GetDisplacementAt(float x, float z, float spectrumStart, float spectrumEnd)
		{
			return GetDisplacementAt(x, z, spectrumStart, spectrumEnd, lastRenderTime);
		}

		public Vector2 GetHorizontalDisplacementAt(float x, float z, float spectrumStart, float spectrumEnd, float time)
		{
			lock (this)
			{
				x = cpuOffsetX - x;
				z = cpuOffsetZ - z;

				float threshold = 0.001f + FastMath.Pow2(spectrumStart);

				Vector2 result = new Vector3();

				foreach(var spectrum in spectrumDataCache.Values)
				{
					if(spectrum.weight < threshold || spectrum.cpuWaves == null) continue;

					spectrum.UpdateSpectralValues(windDirection, water.Directionality);

					Vector2 subResult = new Vector2();

					var cpuWaves = spectrum.cpuWaves;
					int startIndex = (int)(spectrumStart * cpuWaves.Length);
					int endIndex = (int)(spectrumEnd * cpuWaves.Length);

					for(int i = startIndex; i < endIndex; ++i)
						subResult += cpuWaves[i].GetHorizontalDisplacementAt(x, z, time);

					result += subResult * spectrum.weight;
				}

				float scale = -water.HorizontalDisplacementScale;
				result.x = result.x * scale;
				result.y = result.y * scale;

				return result;
			}
		}

		public Vector2 GetHorizontalDisplacementAt(float x, float z, float spectrumStart, float spectrumEnd)
		{
			return GetHorizontalDisplacementAt(x, z, spectrumStart, spectrumEnd, lastRenderTime);
		}

		public float GetHeightAt(float x, float z, float spectrumStart, float spectrumEnd, float time)
		{
			lock (this)
			{
				x = cpuOffsetX - x;
				z = cpuOffsetZ - z;

				float threshold = 0.001f + FastMath.Pow2(spectrumStart);
				float h = 0.0f;

				foreach(var spectrum in spectrumDataCache.Values)
				{
					if(spectrum.weight < threshold || spectrum.cpuWaves == null) continue;

					spectrum.UpdateSpectralValues(windDirection, water.Directionality);

					float subResult = 0.0f;

					var cpuWaves = spectrum.cpuWaves;
					int startIndex = (int)(spectrumStart * cpuWaves.Length);
					int endIndex = (int)(spectrumEnd * cpuWaves.Length);

					for(int i = startIndex; i < endIndex; ++i)
						subResult += cpuWaves[i].GetHeightAt(x, z, time);

					h += subResult * spectrum.weight;
				}

				return h;
			}
		}

		public float GetHeightAt(float x, float z, float spectrumStart, float spectrumEnd)
		{
			return GetHeightAt(x, z, spectrumStart, spectrumEnd, lastRenderTime);
		}

		public void SetWindDirection(Vector2 windDirection)
		{
			lock(this)
			{
				this.windDirection = windDirection;
				this.directionalSpectrumDirty = true;

				SetCpuWavesDirty();
			}
		}
		
		public void OnProfilesChanged()
		{
			lock(this)
			{
				cpuOffsetX = water.TileSize + (0.5f / finalResolution) * water.TileSize;
				cpuOffsetZ = -water.TileSize + (0.5f / finalResolution) * water.TileSize;
				
				ComputeTotalSpectrum();
			}
		}

		public Texture GetSpectrum(SpectrumType type)
		{
			switch(type)
			{
				case SpectrumType.Height: return heightSpectrum;
				case SpectrumType.Slope: return slopeSpectrum;
				case SpectrumType.Displacement: return displacementSpectrum;
				case SpectrumType.RawDirectional: return directionalSpectrum;
				case SpectrumType.RawOmnidirectional: return omnidirectionalSpectrum;
				default: throw new System.InvalidOperationException();
			}
		}
		
		public Gerstner4[] FindMostMeaningfulWaves(int count, bool mask)
		{
			lock(this)
			{
				var list = new List<FoundWave>();
				
				foreach(var spectrum in spectrumDataCache.Values)
				{
					if(spectrum.weight < 0.001f)
						continue;

					spectrum.UpdateSpectralValues(windDirection, water.Directionality);

					var gerstnerWaves = spectrum.gerstnerWaves;

					foreach(var gerstnerWave in gerstnerWaves)
						list.Add(new FoundWave(spectrum, gerstnerWave));
				}
				
				list.Sort((a, b) => b.importance.CompareTo(a.importance));

				int index = 0;
				int numFours = (count >> 2);
				var result = new Gerstner4[numFours];
				
				for(int i=0; i < numFours; ++i)
				{
					var wave0 = index < list.Count ? list[index++] : new WaterWave();
					var wave1 = index < list.Count ? list[index++] : new WaterWave();
					var wave2 = index < list.Count ? list[index++] : new WaterWave();
					var wave3 = index < list.Count ? list[index++] : new WaterWave();

					result[i] = new Gerstner4(wave0, wave1, wave2, wave3);
				}
				
				//if(mask)
				//	foundWave.spectrum.texture.SetPixel(wave.u, wave.v, new Color(0.0f, 0.0f, 0.0f, 0.0f));

				/*if(mask)
				{
					foreach(var spectrum in spectra)
						spectrum.texture.Apply(false, false);

					ComputeTotalSpectrum();
					directionalSpectrumDirty = true;
				}*/
				
				return result;
			}
		}

		private void ComputeTotalSpectrum()
		{
			lock(this)
			{
				SetupMaterials();

				var profiles = water.Profiles;

				if(profiles.Length > 1)
				{
					var totalOmnidirectionalSpectrum = TotalOmnidirectionalSpectrum;

					Graphics.SetRenderTarget(totalOmnidirectionalSpectrum);
					GL.Clear(false, true, Color.black);
					Graphics.SetRenderTarget(null);

					foreach(var weightedProfile in profiles)
					{
						if(weightedProfile.weight <= 0.0001f)
							continue;

						var spectrum = weightedProfile.profile.Spectrum;

						WaterWavesSpectrumData spectrumData;

						if(!spectrumDataCache.TryGetValue(spectrum, out spectrumData))
							spectrumData = GetSpectrumData(spectrum);

						spectrumData.weight = weightedProfile.weight;

						animationMaterial.SetFloat("_Weight", spectrumData.weight);
						Graphics.Blit(spectrumData.Texture, totalOmnidirectionalSpectrum, animationMaterial, 4);
					}

					omnidirectionalSpectrum = totalOmnidirectionalSpectrum;
				}
				else
				{
					var spectrum = profiles[0].profile.Spectrum;
					WaterWavesSpectrumData spectrumData;

					if(!spectrumDataCache.TryGetValue(spectrum, out spectrumData))
						spectrumData = GetSpectrumData(spectrum);

					spectrumData.weight = 1.0f;
					omnidirectionalSpectrum = spectrumData.Texture;
				}

				totalAmplitude = spectrumDataCache.Values.Sum(s => s.totalAmplitude * s.weight);
				maxHeight = totalAmplitude * 0.12f;
				maxDisplacement = Mathf.Sqrt(FastMath.Pow2(maxHeight) + FastMath.Pow2(maxHeight * water.HorizontalDisplacementScale));

				water.WaterMaterial.SetFloat("_MaxDisplacement", maxDisplacement);

				directionalSpectrumDirty = true;
				SetCpuWavesDirty();
			}
		}

		private void ResolveResolution()
		{
			int finalResolution = Mathf.Min(resolution, WaterQualitySettings.Instance.MaxSpectrumResolution, SystemInfo.maxTextureSize);
			bool finalHighPrecision = highPrecision && WaterQualitySettings.Instance.AllowHighPrecisionTextures && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat);

			if(this.finalResolution != finalResolution)
			{
				lock(this)
				{
					this.finalResolution = finalResolution;
					this.finalHighPrecision = finalHighPrecision;
					OnMapsFormatChanged(true);
				}
			}
			else if(this.finalHighPrecision != finalHighPrecision)
			{
				lock(this)
				{
					this.finalHighPrecision = finalHighPrecision;
					OnMapsFormatChanged(false);
				}
            }
		}

		private void OnMapsFormatChanged(bool resolution)
		{
			if(totalOmnidirectionalSpectrum != null)
			{
				Object.Destroy(totalOmnidirectionalSpectrum);
				totalOmnidirectionalSpectrum = null;
			}

			if(heightSpectrum != null)
			{
				Object.Destroy(heightSpectrum);
				heightSpectrum = null;
            }

			if(slopeSpectrum != null)
			{
				Object.Destroy(slopeSpectrum);
				slopeSpectrum = null;
            }

			if(displacementSpectrum != null)
			{
				Object.Destroy(displacementSpectrum);
				displacementSpectrum = null;
            }

			if(directionalSpectrum != null)
			{
				Object.Destroy(directionalSpectrum);
				directionalSpectrum = null;
            }

			if(spectrumDataCache != null)
			{
				foreach(var spectrumData in spectrumDataCache.Values)
					spectrumData.Dispose(!resolution);
			}

			omnidirectionalSpectrum = null;
			renderTargetsx2 = null;
			renderTargetsx3 = null;

			directionalSpectrumDirty = true;

			if(ResolutionChanged != null)
				ResolutionChanged();
        }

		private WaterWavesSpectrumData GetSpectrumData(WaterWavesSpectrum spectrum)
		{
			lock(this)
			{
				WaterWavesSpectrumData spectrumData;

				if(!spectrumDataCache.TryGetValue(spectrum, out spectrumData))
				{
					spectrumDataCache[spectrum] = spectrumData = new WaterWavesSpectrumData(water, spectrum);
					spectrumData.ValidateSpectrum();
				}

				return spectrumData;
			}
		}

		private void CheckResources()
		{
			if(heightSpectrum == null)			// they are always all null or non-null
			{
				heightSpectrum = new RenderTexture(finalResolution, finalResolution, 0, finalHighPrecision ? RenderTextureFormat.RGFloat : RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
				heightSpectrum.filterMode = FilterMode.Point;

				slopeSpectrum = new RenderTexture(finalResolution, finalResolution, 0, finalHighPrecision ? RenderTextureFormat.ARGBFloat : RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
				slopeSpectrum.filterMode = FilterMode.Point;

				displacementSpectrum = new RenderTexture(finalResolution, finalResolution, 0, finalHighPrecision ? RenderTextureFormat.ARGBFloat : RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
				displacementSpectrum.filterMode = FilterMode.Point;

				directionalSpectrum = new RenderTexture(finalResolution, finalResolution, 0, finalHighPrecision ? RenderTextureFormat.RGFloat : RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
				directionalSpectrum.filterMode = FilterMode.Point;
				directionalSpectrum.wrapMode = TextureWrapMode.Repeat;

				renderTargetsx2 = new RenderBuffer[] { heightSpectrum.colorBuffer, displacementSpectrum.colorBuffer };
				renderTargetsx3 = new RenderBuffer[] { heightSpectrum.colorBuffer, slopeSpectrum.colorBuffer, displacementSpectrum.colorBuffer };
			}
		}

		private void RenderDirectionalSpectrum()
		{
			if(omnidirectionalSpectrum == null)
				ComputeTotalSpectrum();

			animationMaterial.SetFloat("_Directionality", 1.0f - water.Directionality);
			animationMaterial.SetVector("_WindDirection", windDirection);
			Graphics.Blit(omnidirectionalSpectrum, directionalSpectrum, animationMaterial, 3);
			directionalSpectrumDirty = false;
		}

		private void SetCpuWavesDirty()
		{
			foreach(var spectrum in spectrumDataCache.Values)
				spectrum.cpuWavesDirty = true;
		}

		private void CheckSupport()
		{
			renderingSupportChecked = true;

			if(highPrecision && (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGFloat) || !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat)))
				highPrecision = false;

			if(!highPrecision && (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGHalf) || !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)))
			{
				if(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGFloat))
					highPrecision = true;
				else
				{
#if UNITY_EDITOR
					Debug.LogError("Your hardware doesn't support floating point render textures. FFT water waves won't work.");
#endif
					
					renderingSupport = false;
					return;
				}
			}

			renderingSupport = true;
		}

		class FoundWave
		{
			public WaterWavesSpectrumData spectrum;
			public WaterWavesSpectrumData.WaveFrequency wave;
			public float importance;

			public FoundWave(WaterWavesSpectrumData spectrum, WaterWavesSpectrumData.WaveFrequency wave)
			{
				this.spectrum = spectrum;
				this.wave = wave;

				importance = wave.gerstnerPriority * spectrum.weight;
				//importance = wave.priority * spectrum.weight * Mathf.Pow(wave.k, 1);
				//importance = wave.priority * Mathf.Sqrt(g / wave.k) / (2.0f * wave.k);
			}

			static public implicit operator WaterWave(FoundWave foundWave)
			{
				var wave = foundWave.wave;
				float speed = wave.w;

				return new WaterWave(new Vector2(wave.nkx, wave.nky), wave.amplitude * foundWave.spectrum.weight, wave.offset, wave.k, speed);
			}
		}

		public enum SpectrumType
		{
			Height,
			Slope,
			Displacement,
			RawDirectional,
			RawOmnidirectional
		}
	}
}
