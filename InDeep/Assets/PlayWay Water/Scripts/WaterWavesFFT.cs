using System;
using UnityEngine;

namespace PlayWay.Water
{
	/// <summary>
	/// Displays water spectrum using Fast Fourier Transform. Uses vertex shader texture fetch available on platforms with Shader Model 3.0+.
	/// </summary>
	[RequireComponent(typeof(Water))]
	[AddComponentMenu("Water/FFT Waves", 0)]
	public class WaterWavesFFT : MonoBehaviour, IWaterRenderAware
	{
		[HideInInspector]
		[SerializeField]
		private Shader fftShader;

		[HideInInspector]
		[SerializeField]
		private Shader derivativeShader;

		[HideInInspector]
		[SerializeField]
		private Shader jacobianShader;

		[SerializeField]
		private ComputeShader dx11FFT;
		
		[Tooltip("Determines if GPU partial derivatives (poor quality on DX11, very poor on everything else) or Fast Fourier Transform (high quality) should be used to compute slope map (Recommended: on). Works only if heightmap rendering is enabled.")]
		[SerializeField]
		private bool highQualitySlopeMaps = true;
		
		[SerializeField]
		private MapType renderedMaps = MapType.Height | MapType.Slope | MapType.Displacement;
		
		[Tooltip("Check this option, if your water is flat or game crashes instantly on a DX11 GPU (in editor or build). Compute shaders are very fast, so use this as a last resort.")]
		[SerializeField]
		private bool forcePixelShader = false;

		private float time = 0.0f;

		private RenderTexture heightMap;
		private RenderTexture slopeMap;
		private RenderTexture displacementMap;
		private RenderTexture displacementMapJacobian;

		private Water water;
		
		private GpuFFT heightFFT;
		private GpuFFT slopeFFT;
		private GpuFFT displacementFFT;
		
		private Material slopeMaterial;
		private Material jacobianMaterial;

		private MapType finalRenderedMaps;
		private bool finalHighQualitySlopeMaps;
        private int heightMapProperty;
		private int slopeMapProperty;
		private int displacementMapProperty;
		private int waveMapsFrame, displacementMapJacobianFrame;

		void Awake()
		{
			water = GetComponent<Water>();
		}

		void Start()
		{
			if(!enabled) return;

			water.SpectraRenderer.ResolutionChanged += OnResolutionChanged;
			
			slopeMaterial = new Material(derivativeShader);
			heightMapProperty = Shader.PropertyToID("_GlobalHeightMap");
			slopeMapProperty = Shader.PropertyToID("_GlobalNormalMap");
			displacementMapProperty = Shader.PropertyToID("_GlobalDisplacementMap");
			
			OnEnable();
			ValidateFFTs();

			WaterQualitySettings.Instance.Changed -= OnQualitySettingsChange;
			WaterQualitySettings.Instance.Changed += OnQualitySettingsChange;
		}

		public MapType RenderedMaps
		{
			get { return renderedMaps; }
		}

		public MapType FinalRenderedMaps
		{
			get { return finalRenderedMaps; }
		}

		/// <summary>
		/// Height map (vertical displacement map).
		/// </summary>
		public RenderTexture HeightMap
		{
			get { return heightMap; }
		}

		/// <summary>
		/// Horizontal displacement map.
		/// </summary>
		public RenderTexture DisplacementMap
		{
			get { return displacementMap; }
		}

		/// <summary>
		/// XY slope map.
		/// </summary>
		public RenderTexture SlopeMap
		{
			get { return slopeMap; }
		}

		public bool HasDisplacementMapJacobian
		{
			get { return displacementMapJacobian != null; }
		}

		public RenderTexture DisplacementMapJacobian
		{
			get
			{
				ValidateDisplacementMapJacobian();
				return displacementMapJacobian;
            }
		}

		public void SetTime(float time)
		{
			this.time = time;
		}

		public void ValidateNow(Water water, WaterQualityLevel qualityLevel)
		{
			bool enable = DetermineState(qualityLevel);

			water.SetKeyword("_FFT_WAVES_SLOPE", enable && finalRenderedMaps == MapType.Slope);
			water.SetKeyword("_FFT_WAVES", enable && ((finalRenderedMaps & MapType.Height) != 0 || (finalRenderedMaps & MapType.Displacement) != 0));

			if(Application.isPlaying)
				this.enabled = enable;
        }

		private void ValidateFFTs()
		{
			ValidateFFT(ref heightFFT, (renderedMaps & MapType.Height) != 0, false);
			ValidateFFT(ref displacementFFT, (renderedMaps & MapType.Displacement) != 0, true);
			ValidateFFT(ref slopeFFT, (renderedMaps & MapType.Slope) != 0, true);
		}

		private void ValidateFFT(ref GpuFFT fft, bool present, bool twoChannels)
		{
			if(present)
			{
				if(fft == null)
					fft = ChooseBestFFTAlgorithm(twoChannels);
			}
			else if(fft != null)
			{
				fft.Dispose();
				fft = null;
			}
		}

		private void OnEnable()
		{
			if(water != null)
				ValidateNow(water, WaterQualitySettings.Instance.CurrentQualityLevel);
		}

		private void OnDisable()
		{
			if(water != null)
				ValidateNow(water, WaterQualitySettings.Instance.CurrentQualityLevel);
		}

		private GpuFFT ChooseBestFFTAlgorithm(bool twoChannels)
		{
			GpuFFT fft;

			int resolution = water.SpectraRenderer.FinalResolution;
			if(!forcePixelShader && dx11FFT != null && SystemInfo.supportsComputeShaders && resolution >= 256 && resolution <= 1024)
				fft = new Dx11FFT(dx11FFT, resolution, water.SpectraRenderer.HighPrecision || resolution >= 4096, twoChannels);
			else
				fft = new PixelShaderFFT(fftShader, resolution, water.SpectraRenderer.HighPrecision || resolution >= 4096, twoChannels);

			fft.SetupMaterials();

			return fft;
		}

		private void OnQualitySettingsChange()
		{
			enabled = DetermineState(WaterQualitySettings.Instance.CurrentQualityLevel);
		}

		private bool DetermineState(WaterQualityLevel qualityLevel)
		{
			if(water == null)
				water = GetComponent<Water>();

			bool allowed = water.SpectraRenderer.RenderingSupport && qualityLevel.wavesMode <= WaterWavesMode.AllowSlopeFFT;

			if(!allowed)
			{
#if UNITY_EDITOR
				if(!water.SpectraRenderer.RenderingSupport)
					Debug.LogError("Your hardware doesn't support floating point render textures. FFT water waves won't work.");
#endif

				finalRenderedMaps = 0;
				return false;
			}
			else
			{
				finalRenderedMaps = (MapType)((int)renderedMaps & 7);

				if(qualityLevel.wavesMode == WaterWavesMode.AllowSlopeFFT)
					finalRenderedMaps &= MapType.Slope;

				finalHighQualitySlopeMaps = highQualitySlopeMaps;

				if(!qualityLevel.allowHighQualitySlopeMaps)
					finalHighQualitySlopeMaps = false;

				if((finalRenderedMaps & MapType.Height) == 0)           // if heightmap is not rendered, only high-quality slope map is possible
					finalHighQualitySlopeMaps = true;

				ValidateFFTs();

				return true;
			}
		}
		
		void OnValidate()
		{
			if(fftShader == null)
				fftShader = Shader.Find("PlayWay Water/Base/FFT");

			if(derivativeShader == null)
				derivativeShader = Shader.Find("PlayWay Water/Utilities/Height2Normal");

			if(jacobianShader == null)
				jacobianShader = Shader.Find("PlayWay Water/Utility/Jacobian");

#if UNITY_EDITOR
			if(dx11FFT == null)
			{
				var guids = UnityEditor.AssetDatabase.FindAssets("\"DX11 FFT\" t:ComputeShader");

				if(guids.Length != 0)
				{
					string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
					dx11FFT = (ComputeShader)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(ComputeShader));
					UnityEditor.EditorUtility.SetDirty(this);
                }
			}
#endif

			if(Application.isPlaying && isActiveAndEnabled)
				enabled = DetermineState(WaterQualitySettings.Instance.CurrentQualityLevel);
        }

		void OnDestroy()
		{
			Dispose(true);
		}

		private void Dispose(bool total)
		{
			if(heightFFT != null)
			{
				heightFFT.Dispose();
				heightFFT = null;
			}

			if(slopeFFT != null)
			{
				slopeFFT.Dispose();
				slopeFFT = null;
			}

			if(displacementFFT != null)
			{
				displacementFFT.Dispose();
				displacementFFT = null;
			}

			if(displacementMapJacobian != null)
			{
				Destroy(displacementMapJacobian);
				displacementMapJacobian = null;
			}
			
			if(slopeMap != null)
			{
				Destroy(slopeMap);
				slopeMap = null;
			}

			if(total)
			{
				WaterQualitySettings.Instance.Changed -= OnQualitySettingsChange;

				if(slopeMaterial != null) Destroy(slopeMaterial);
				if(jacobianMaterial != null) Destroy(jacobianMaterial);
			}
		}
		
		public void OnWaterRender(Camera camera)
		{
			if(!Application.isPlaying || !enabled) return;

			ValidateWaveMaps();
		}

		public void OnWaterPostRender(Camera camera)
		{

		}
		
		private void OnResolutionChanged()
		{
			Dispose(false);
			ValidateFFTs();
        }

		private void ValidateWaveMaps()
		{
			int frameCount = Time.frameCount;

			if(waveMapsFrame == frameCount || !Application.isPlaying)
				return;         // it's already done

			waveMapsFrame = frameCount;

			// render needed spectra
			Texture heightSpectrum, slopeSpectrum, displacementSpectrum;
			RenderSpectra(out heightSpectrum, out slopeSpectrum, out displacementSpectrum);

			// transform spectra to final maps
			if((finalRenderedMaps & MapType.Height) != 0)
			{
				heightMap = heightFFT.ComputeFFT(heightSpectrum);
				water.WaterMaterial.SetTexture(heightMapProperty, heightMap);
			}

			if((finalRenderedMaps & MapType.Displacement) != 0)
			{
				displacementMap = displacementFFT.ComputeFFT(displacementSpectrum);
				water.WaterMaterial.SetTexture(displacementMapProperty, displacementMap);
			}

			if((finalRenderedMaps & MapType.Slope) != 0)
			{
				if(!finalHighQualitySlopeMaps)
				{
					int resolution = water.SpectraRenderer.FinalResolution;

					if(slopeMap == null)
					{
						slopeMap = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
						slopeMap.hideFlags = HideFlags.DontSave;
						slopeMap.wrapMode = TextureWrapMode.Repeat;
						slopeMap.useMipMap = true;
						slopeMap.generateMips = true;
						slopeMap.filterMode = FilterMode.Trilinear;
					}

					slopeMaterial.SetFloat("_Intensity", Mathf.Lerp(1.6f, 2.1f, resolution / 1024.0f));
					slopeMaterial.SetTexture("_GlobalHeightMap", displacementMap);
					Graphics.Blit(heightMap, slopeMap, slopeMaterial, 0);
				}
				else
					slopeMap = slopeFFT.ComputeFFT(slopeSpectrum);

				water.WaterMaterial.SetTexture(slopeMapProperty, slopeMap);
			}
		}

		private void RenderSpectra(out Texture heightSpectrum, out Texture slopeSpectrum, out Texture displacementSpectrum)
		{
			if(finalRenderedMaps == MapType.Slope)
			{
				heightSpectrum = null;
				displacementSpectrum = null;
				slopeSpectrum = water.SpectraRenderer.RenderSlopeSpectrumAt(time != 0.0f ? time : Time.time);
			}
			else if((finalRenderedMaps & MapType.Slope) == 0 || !finalHighQualitySlopeMaps)
			{
				slopeSpectrum = null;
				water.SpectraRenderer.RenderDisplacementsSpectraAt(time != 0.0f ? time : Time.time, out heightSpectrum, out displacementSpectrum);
			}
			else
				water.SpectraRenderer.RenderCompleteSpectraAt(time != 0.0f ? time : Time.time, out heightSpectrum, out slopeSpectrum, out displacementSpectrum);
		}

		private void ValidateDisplacementMapJacobian()
		{
			if(displacementMapJacobianFrame == Time.frameCount || !Application.isPlaying)
				return;			// it's already done

			displacementMapJacobianFrame = Time.frameCount;

			int resolution = water.SpectraRenderer.FinalResolution;

			if(displacementMapJacobian == null)
			{
				displacementMapJacobian = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
				displacementMapJacobian.filterMode = FilterMode.Bilinear;
				displacementMapJacobian.wrapMode = TextureWrapMode.Repeat;
			}

			if(jacobianMaterial == null)
			{
				jacobianMaterial = new Material(jacobianShader);
				jacobianMaterial.hideFlags = HideFlags.DontSave;
            }

			jacobianMaterial.SetFloat("_Scale", water.HorizontalDisplacementScale);
			Graphics.Blit(displacementMap, displacementMapJacobian, jacobianMaterial, 0);
		}
		
		public enum SpectrumType
		{
			Phillips,
			Unified
		}

		[System.Flags]
		public enum MapType
		{
			Height = 1,
			Slope = 2,
			Displacement = 4
		}
	}
}
