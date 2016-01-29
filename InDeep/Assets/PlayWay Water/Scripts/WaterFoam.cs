using UnityEngine;

namespace PlayWay.Water
{
	[RequireComponent(typeof(Water))]
	[RequireComponent(typeof(WaterWavesFFT))]
	[AddComponentMenu("Water/Foam", 1)]
	public class WaterFoam : MonoBehaviour
	{
		[HideInInspector]
		[SerializeField]
		private Shader localFoamShader;

		[HideInInspector]
		[SerializeField]
		private Shader globalFoamShader;

		[HideInInspector]
		[SerializeField]
		private bool simulateLocalFoam;

		[SerializeField]
		private bool simulateGlobalFoam = true;

		[Tooltip("Foam map supersampling in relation to the waves simulator resolution. Has to be a power of two (0.25, 0.5, 1, 2, etc.)")]
		[SerializeField]
		private float supersampling = 1.0f;

		[SerializeField]
		private float foamIntensity = 0.03f;

		[Range(0.0f, 2.5f)]
		[SerializeField]
		private float foamBoost = 1.0f;

		[Tooltip("Optimized for 1 and 2.")]
		[SerializeField]
		private float foamPower = 1.0f;

		[Tooltip("Determines how fast foam will fade out.")]
		[Range(0.0f, 1.0f)]
		[SerializeField]
		private float foamFadingFactor = 0.0f;

		[SerializeField]
		private Blur foamBlur;

		private RenderTexture localFoamMapA;
		private RenderTexture localFoamMapB;

		private RenderTexture globalFoamMapA;
		private RenderTexture globalFoamMapB;

		private Material localFoamMaterial;
		private Material globalFoamMaterial;
		
		private Vector2 lastCameraPos;
		private Vector2 deltaPosition;

		private float initialBlurSize;

		private Water water;
		private WaterWavesFFT wavesSimulation;

		private int resolution;

		private bool firstFrame;

		void Start()
		{
			water = GetComponent<Water>();
			wavesSimulation = GetComponent<WaterWavesFFT>();

			water.SpectraRenderer.ResolutionChanged += OnResolutionChanged;

			resolution = Mathf.RoundToInt(water.SpectraRenderer.FinalResolution * supersampling);
			initialBlurSize = foamBlur.Size;

			localFoamMaterial = new Material(localFoamShader);
			globalFoamMaterial = new Material(globalFoamShader);

			firstFrame = true;
			
			SetupMaterials();

			water.WindDirectionChanged.AddListener(OnWindChanged);
			OnWindChanged(water);
		}

		private void OnResolutionChanged()
		{
			resolution = Mathf.RoundToInt(water.SpectraRenderer.FinalResolution * supersampling);

			Dispose(false);
		}

		public RenderTexture LocalFoamMap
		{
			get { return localFoamMapA; }
		}

		public RenderTexture GlobalFoamMap
		{
			get { return globalFoamMapA; }
		}

		void OnDisable()
		{
			water.SetKeyword("_WATER_FOAM_LOCAL", false);
			water.SetKeyword("_WATER_FOAM_WS", false);
		}

		void OnWindChanged(Water water)
		{
			Vector2 windSpeed = water.WindSpeed;
			Vector2 dir = windSpeed.normalized;
			localFoamMaterial.SetVector("_SampleDir1", new Vector4(dir.x * 0.02f, dir.y * 0.02f, windSpeed.x, windSpeed.y));
		}

		public void SetupMaterials()
		{
			water = GetComponent<Water>();
			water.SetKeyword("_WATER_FOAM_LOCAL", simulateLocalFoam);
			water.SetKeyword("_WATER_FOAM_WS", simulateGlobalFoam);
		}

		private void SetupFoamMaterials()
		{
			/*if(localFoamMaterial != null)
			{
				SetKeyword(localFoamMaterial, foamPower == 1.0f ? 0 : (foamPower == 2.0f ? 1 : 2), "FOAM_POW_1", "FOAM_POW_2", "FOAM_POW_N");

				localFoamMaterial.SetVector("_FoamParameters", new Vector4(foamIntensity * foamPower * foamPower, water.HorizontalDisplacementScale * foamBoost * resolution / 2048.0f * 220.0f / water.TileSize, foamPower, foamFadingFactor));
			}*/

			if(globalFoamMaterial != null)
			{
				SetKeyword(globalFoamMaterial, foamPower == 1.0f ? 0 : (foamPower == 2.0f ? 1 : 2), "FOAM_POW_1", "FOAM_POW_2", "FOAM_POW_N");

				globalFoamMaterial.SetVector("_FoamParameters", new Vector4(foamIntensity * foamPower * foamPower, water.HorizontalDisplacementScale * foamBoost * resolution / 2048.0f * 220.0f / water.TileSize, foamPower, foamFadingFactor));
			}
		}

		private void SetKeyword(Material material, string name, bool val)
		{
			if(val)
				material.EnableKeyword(name);
			else
				material.DisableKeyword(name);
		}

		private void SetKeyword(Material material, int index, params string[] names)
		{
			foreach(var name in names)
				material.DisableKeyword(name);

			material.EnableKeyword(names[index]);
		}

		void OnValidate()
		{
			if(localFoamShader == null)
				localFoamShader = Shader.Find("PlayWay Water/Foam/Local");

			if(globalFoamShader == null)
				globalFoamShader = Shader.Find("PlayWay Water/Foam/Global");

			foamBlur.Validate("PlayWay Water/Utilities/Blur");

			supersampling = Mathf.ClosestPowerOfTwo(Mathf.RoundToInt(supersampling * 4096)) / 4096.0f;

			water = GetComponent<Water>();
			wavesSimulation = GetComponent<WaterWavesFFT>();
			SetupMaterials();
        }
		
		private void Dispose(bool completely)
		{
			if(localFoamMapA != null)
			{
				Destroy(localFoamMapA);
				Destroy(localFoamMapB);

				localFoamMapA = null;
				localFoamMapB = null;
			}

			if(globalFoamMapA != null)
			{
				Destroy(globalFoamMapA);
				Destroy(globalFoamMapB);

				globalFoamMapA = null;
				globalFoamMapB = null;
			}

			if(completely && foamBlur != null)
				foamBlur.Dispose();
		}

		void OnDestroy()
		{
			Dispose(true);
		}

		void LateUpdate()
		{
			if(!firstFrame)
				UpdateFoamMap();
			else
				firstFrame = false;

			SwapRenderTargets();
		}

		private void CheckResources()
		{
			/*if(simulateLocalFoam && localFoamMapA == null)
			{
				localFoamMapA = CreateRT(0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear, FilterMode.Trilinear, TextureWrapMode.Clamp);
				localFoamMapB = CreateRT(0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear, FilterMode.Trilinear, TextureWrapMode.Clamp);

				ClearTexture(localFoamMapA, Color.black);
				ClearTexture(localFoamMapB, Color.black);
			}*/

			if(simulateGlobalFoam && globalFoamMapA == null)
			{
				globalFoamMapA = CreateRT(0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear, FilterMode.Trilinear, TextureWrapMode.Repeat);
				globalFoamMapB = CreateRT(0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear, FilterMode.Trilinear, TextureWrapMode.Repeat);
				
				ClearTexture(globalFoamMapA, Color.black);
				ClearTexture(globalFoamMapB, Color.black);
			}
		}

		private void ClearTexture(RenderTexture tex, Color color)
		{
			RenderTexture.active = tex;
			GL.Clear(false, true, color);
			RenderTexture.active = null;
		}

		private RenderTexture CreateRT(int depth, RenderTextureFormat format, RenderTextureReadWrite readWrite, FilterMode filterMode, TextureWrapMode wrapMode)
		{
			var renderTexture = new RenderTexture(resolution, resolution, depth, format, readWrite);
			renderTexture.hideFlags = HideFlags.DontSave;
			renderTexture.filterMode = filterMode;
			renderTexture.wrapMode = wrapMode;
			renderTexture.useMipMap = true;
			renderTexture.generateMips = true;
			
			return renderTexture;
		}
		
		private void UpdateFoamMap()
		{
			CheckResources();
			SetupFoamMaterials();

			/*if(simulateLocalFoam)
			{
				Vector3 centerPos = Camera.main.transform.position;
				Vector2 minPos = new Vector2(
					(centerPos.x - realSize * 0.5f),
					(centerPos.z - realSize * 0.5f)
				);

				localFoamMaterial.SetTexture("_DistortionMapB", wavesSimulation.DisplacementMap);
				localFoamMaterial.SetVector("_DistortionMapCoords", new Vector4(minPos.x, minPos.y, 1.0f / water.TileSize, realSize));

				localFoamMaterial.SetFloat("_DisplacementsScale", water.WaterMaterial.GetFloat("_DisplacementsScale"));

				localFoamMaterial.SetVector("_DeltaPosition", new Vector2(-deltaPosition.x / realSize, deltaPosition.y / realSize));
				Graphics.Blit(localFoamMapA, localFoamMapB, localFoamMaterial, 0);

				foamBlur.Size = initialBlurSize * Time.deltaTime;
				foamBlur.Apply(localFoamMapB);

				water.WaterMaterial.SetTexture("_FoamMap", localFoamMapB);
				water.WaterMaterial.SetVector("_FoamMapDimensions", new Vector4(-(lastCameraPos.x - realSize * 0.5f), -(lastCameraPos.y - realSize * 0.5f), 1.0f / realSize, 1.0f / realSize));
			}*/

			if(simulateGlobalFoam)
			{
				globalFoamMaterial.SetTexture("_DistortionMapB", wavesSimulation.DisplacementMap);
				globalFoamMaterial.SetFloat("_DisplacementsScale", water.WaterMaterial.GetFloat("_DisplacementsScale"));
				Graphics.Blit(globalFoamMapA, globalFoamMapB, globalFoamMaterial, 0);

				foamBlur.Size = initialBlurSize * Time.deltaTime;
				foamBlur.Apply(globalFoamMapB);

				water.WaterMaterial.SetTexture("_FoamMapWS", globalFoamMapB);
			}
		}

		private Vector2 RotateVector(Vector2 vec, float angle)
		{
			float s = Mathf.Sin(angle);
			float c = Mathf.Cos(angle);

			return new Vector2(c * vec.x + s * vec.y, c * vec.y + s * vec.x);
		}
		
		private void SwapRenderTargets()
		{
			var t = localFoamMapA;
			localFoamMapA = localFoamMapB;
			localFoamMapB = t;

			t = globalFoamMapA;
			globalFoamMapA = globalFoamMapB;
			globalFoamMapB = t;
		}
	}
}
