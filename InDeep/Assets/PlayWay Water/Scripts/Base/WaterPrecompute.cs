using UnityEngine;

namespace PlayWay.Water
{
	/// <summary>
	/// Precomputes some data for water shader:
	/// - Spectra variance used by shader microfacet model (sm 3.0+)
	/// - Fresnel (sm 2.0)
	/// <seealso cref="Water.WaterPrecompute"/>
	/// </summary>
	[System.Serializable]
	public class WaterPrecompute
	{
		[SerializeField]
		private ComputeShader varianceShader;

		[Tooltip("Incorporates tiny waves on the screen into Unity's shader micro-facet model. Makes water look realistic at all view distances. Recommended.\nWorks only on DX11.")]
		[SerializeField]
		private bool computeSlopeVariance = true;
		
		// variance
		private RenderTexture varianceTexture;
		private RenderTexture varianceBufferX, varianceBufferY;         // UAV reads work only for single-component buffers
		private RenderTexture varianceBufferPreviousX, varianceBufferPreviousY;
		private int previousLength;
		private int currentStartRow;
		private int currentNextRow;
		private int currentLength;
		private bool currentFinished;
		private bool initialized;
		private bool supported;

		private Water water;
		
		public void Start(Water water)
		{
			this.water = water;
			this.supported = CheckSupport();
        }

		public bool Enabled
		{
			get { return computeSlopeVariance && supported; }
		}

		public Texture VarianceTexture
		{
			get { return varianceTexture; }
		}

		public bool CheckSupport()
		{
			return SystemInfo.supportsComputeShaders && SystemInfo.supports3DTextures;
		}

		private float PreviousWeight
		{
			get
			{
				if(previousLength != 0)
				{
					int resolution = water.SpectraRenderer.FinalResolution;
					int numOverwrites = currentLength - (resolution - previousLength);

					if(numOverwrites < 0) numOverwrites = 0;
					if(numOverwrites > previousLength) numOverwrites = previousLength;

					return 1.0f - (float)numOverwrites / previousLength;
				}
				else
					return 0;
            }
		}
		
		public void Update()
		{
			if(!computeSlopeVariance || !supported) return;

			if(!initialized) InitializeVariance();

			ValidateVarianceTextures();

			if(!currentFinished)
			{
				RenderNextRow();
				UpdateTotalVariance();
            }
		}

		private void InitializeVariance()
		{
			initialized = true;

			varianceTexture = CreateVarianceTexture(RenderTextureFormat.RGHalf);
			varianceBufferX = CreateVarianceTexture(RenderTextureFormat.RHalf);
			varianceBufferY = CreateVarianceTexture(RenderTextureFormat.RHalf);
			varianceBufferPreviousX = CreateVarianceTexture(RenderTextureFormat.RHalf);
			varianceBufferPreviousY = CreateVarianceTexture(RenderTextureFormat.RHalf);

			water.ProfilesChanged.AddListener(OnProfilesChanged);
			water.WindDirectionChanged.AddListener(OnWindDirectionChanged);
		}

		private void ValidateVarianceTextures()
		{
			if(!varianceTexture.IsCreated())
			{
				varianceTexture.Create();
				varianceBufferX.Create();
				varianceBufferY.Create();
				varianceBufferPreviousX.Create();
				varianceBufferPreviousY.Create();

				water.WaterMaterial.SetTexture("_SlopeVariance", varianceTexture);

				varianceShader.SetTexture(2, "_Variance", varianceTexture);

				for(int i = 0; i < 4; ++i)
				{
					varianceShader.SetTexture(i, "_VarianceX", varianceBufferX);
					varianceShader.SetTexture(i, "_VarianceY", varianceBufferY);
				}

				for(int i = 2; i < 4; ++i)
				{
					varianceShader.SetTexture(i, "_PreviousVarianceX", varianceBufferPreviousX);
					varianceShader.SetTexture(i, "_PreviousVarianceY", varianceBufferPreviousY);
				}

				previousLength = 0;
				currentStartRow = 0;
				currentNextRow = 0;
				currentLength = 0;
				currentFinished = false;
			}
		}

		private void RenderNextRow()
		{
			varianceShader.SetFloat("_FFTSize", water.SpectraRenderer.FinalResolution);
			varianceShader.SetFloat("_VariancesSize", varianceTexture.width);
			varianceShader.SetFloat("_TileSize", water.TileSize);
			varianceShader.SetVector("_Coordinates", new Vector4(currentNextRow, currentNextRow + 4));
			varianceShader.SetTexture(1, "_Spectrum", water.SpectraRenderer.RawDirectionalSpectrum);
			varianceShader.Dispatch(1, 1, 1, 1);

			currentNextRow += 4;
			currentLength += 4;

			if(currentNextRow >= water.SpectraRenderer.FinalResolution)
				currentNextRow = 0;

			if(currentNextRow == currentStartRow)
				currentFinished = true;
        }

		private void UpdateTotalVariance()
		{
			varianceShader.SetFloat("_MixWeight", PreviousWeight);
            varianceShader.Dispatch(2, 1, 1, 1);
		}

		private void ResetComputations()
		{
			varianceShader.SetFloat("_MixWeight", PreviousWeight);
			varianceShader.Dispatch(3, 1, 1, 1);

			varianceShader.Dispatch(0, 1, 1, 1);

			previousLength = Mathf.Min(water.SpectraRenderer.FinalResolution, previousLength + currentLength);
			currentStartRow = currentNextRow;
			currentNextRow = currentStartRow;
			currentLength = 0;
			currentFinished = false;
		}

		internal void OnValidate(Water water)
		{
#if UNITY_EDITOR
			if(varianceShader == null)
			{
				var guids = UnityEditor.AssetDatabase.FindAssets("\"Spectral Variances\" t:ComputeShader");

				if(guids.Length != 0)
				{
					string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
					varianceShader = (ComputeShader)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(ComputeShader));
					UnityEditor.EditorUtility.SetDirty(water);
				}
			}
#endif
		}

		private RenderTexture CreateVarianceTexture(RenderTextureFormat format)
		{
			var variancesTexture = new RenderTexture(4, 4, 0, format, RenderTextureReadWrite.Linear);
			variancesTexture.hideFlags = HideFlags.DontSave;
			variancesTexture.volumeDepth = 4;
			variancesTexture.isVolume = true;
			variancesTexture.enableRandomWrite = true;
			variancesTexture.wrapMode = TextureWrapMode.Clamp;
			variancesTexture.filterMode = FilterMode.Bilinear;

			return variancesTexture;
		}
		
		private void OnProfilesChanged(Water water)
		{
			ResetComputations();
		}

		private void OnWindDirectionChanged(Water water)
		{
			ResetComputations();
		}
    }
}
