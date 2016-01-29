using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace PlayWay.Water
{
	/// <summary>
	/// Main water component.
	/// </summary>
	[ExecuteInEditMode]
	[AddComponentMenu("Water/Water (Base Component)", -1)]
	public class Water : MonoBehaviour, IShaderCollectionBuilder
	{
		[SerializeField]
		private WaterProfile profile;

		[SerializeField]
		private WaterQualitySettings waterQualitySettings;

		[SerializeField]
		private Shader waterShader;

		[SerializeField]
		private Shader waterVolumeShader;

		[SerializeField]
		private bool refraction = true;

		[SerializeField]
		private bool blendEdges = true;

		[SerializeField]
		private bool volumetricLighting = true;

		[Tooltip("Affects direct light specularity and diffuse (mainly foam).")]
		[SerializeField]
		private bool receiveShadows;

		[SerializeField]
		private ShaderCollection shaderCollection;

		[SerializeField]
		private ShadowCastingMode shadowCastingMode;

		[SerializeField]
		private bool useCubemapReflections = true;

		[SerializeField]
		private Transform windDirectionPointer;

		[Tooltip("Use ambient color each frame at runtime to update water depth color (based on it's initial value). Currently, it's not supported for the \"skybox\" ambient mode.")]
		[SerializeField]
		private bool autoDepthColor = true;

		[Tooltip("Set it to anything else than 0 if your game has multiplayer functionality or you want your water to behave the same way each time your game is played (good for intro etc.).")]
		[SerializeField]
		private int seed = 0;

		[Tooltip("May hurt performance on some systems.")]
		[Range(0.0f, 1.0f)]
		[SerializeField]
		private float tesselationFactor = 0.88f;

		[SerializeField]
		private SpectraRenderer spectraRenderer;

		[SerializeField]
		private WaterPrecompute waterPrecompute;

		[SerializeField]
		private WaterUvAnimator waterUvAnimator;

		[SerializeField]
		private WaterVolume volume;

		[SerializeField]
		private WaterGeometry geometry;

		[SerializeField]
		private new WaterRenderer renderer;
		
		[SerializeField]
		private int namesHash = -1;

		[SerializeField]
		private WaterEvent windDirectionChanged;

		[SerializeField]
		private WaterEvent profilesChanged;

		[SerializeField]
		private bool inserted;

		private WeightedProfile[] profiles;
		private bool profilesDirty;

		private Material waterMaterial;
		private Material waterVolumeMaterial;

		private int instanceId = -1;
		private float tileSize;
		private float windSpeedMagnitude;
		private float horizontalDisplacementScale;
		private float gravity;
		private float directionality;
		private float density;
		private Color underwaterAbsorptionColor;
		private Color maxDepthColor;
		private Vector2 lastWindDirection;
		private IWaterRenderAware[] renderAwareComponents;

		static private string[] parameterNames = new string[] {
			"_AbsorptionColor", "_Color", "_SpecColor", "_DepthColor", "_EmissionColor", "_DisplacementsScale", "_Glossiness",
			"_SubsurfaceScatteringPack", "_WrapSubsurfaceScatteringPack", "_RefractionDistortion", "_SpecularFresnelBias", "_DistantFadeFactors",
			"_DisplacementNormalsIntensity", "_EdgeBlendFactorInv", "_PlanarReflectionPack", "_BumpScale", "_FoamTiling", "_WaterTileSize",
			"_BumpMap", "_FoamTex", "_FoamNormalMap"
		};

		private int[] parameterHashes;

		private VectorParameterOverride[] vectorOverrides;
		private ColorParameterOverride[] colorOverrides;
		private FloatParameterOverride[] floatOverrides;
		private TextureParameterOverride[] textureOverrides;

		void Awake()
		{
			if(!inserted)
			{
				AddDefaultComponents();

				inserted = true;
            }

			if(!Application.isPlaying)
				return;

			if(profile == null)
			{
				Debug.LogError("Water profile is not set. You may assign it in the inspector.");
				gameObject.SetActive(false);
				return;
			}

			try
			{
				CreateParameterHashes();

				renderAwareComponents = GetComponents<IWaterRenderAware>();
				lastWindDirection = WindDirection;

				CreateMaterials();

				if(profiles == null)
				{
					profiles = new WeightedProfile[] { new WeightedProfile(profile, 1.0f) };
					ResolveProfileData(profiles);
				}

				waterUvAnimator.Start(this);
				spectraRenderer.Start(this);
				waterPrecompute.Start(this);

				maxDepthColor = waterMaterial.GetColor("_DepthColor");

				profilesChanged.AddListener(OnProfilesChanged);
				windDirectionChanged.AddListener(OnWindDirectionChanged);
			}
			catch(System.Exception e)
			{
				Debug.LogError(e);
				gameObject.SetActive(false);
			}
		}

		void Start()
		{
			spectraRenderer.OnValidate(this);

			SetupMaterials();
		}
		
		public Material WaterMaterial
		{
			get
			{
				if(waterMaterial == null)
					CreateMaterials();

				return waterMaterial;
			}
		}

		public Material WaterVolumeMaterial
		{
			get
			{
				if(waterVolumeMaterial == null)
					CreateMaterials();

				return waterVolumeMaterial;
			}
		}

		/// <summary>
		/// Current wind speed as resolved from the currently set profiles.
		/// </summary>
		public Vector2 WindSpeed
		{
			get
			{
				if(windDirectionPointer != null)
				{
					Vector3 forward = windDirectionPointer.forward;
					return new Vector2(forward.x, forward.z).normalized * windSpeedMagnitude;
				}
				else
					return Vector2.zero;
			}
		}

		/// <summary>
		/// Current wind direction. It's controlled by the WindDirectionPointer.
		/// </summary>
		public Vector2 WindDirection
		{
			get
			{
				if(windDirectionPointer != null)
				{
					Vector3 forward = windDirectionPointer.forward;
					return new Vector2(forward.x, forward.z).normalized;
				}
				else
					return Vector2.zero;
			}
		}

		public Transform WindDirectionPointer
		{
			get { return windDirectionPointer; }
		}

		/// <summary>
		/// Currently set water profiles with their associated weights.
		/// </summary>
		public WeightedProfile[] Profiles
		{
			get { return profiles; }
		}

		public float HorizontalDisplacementScale
		{
			get { return horizontalDisplacementScale; }
		}

		public bool ReceiveShadows
		{
			get { return receiveShadows; }
		}

		public ShadowCastingMode ShadowCastingMode
		{
			get { return shadowCastingMode; }
		}

		public float TileSize
		{
			get { return tileSize; }
		}

		public float Gravity
		{
			get { return gravity; }
		}

		public float Directionality
		{
			get { return directionality; }
		}

		public Color UnderwaterAbsorptionColor
		{
			get { return underwaterAbsorptionColor; }
		}

		public bool VolumetricLighting
		{
			get { return volumetricLighting; }
		}

		public bool FinalVolumetricLighting
		{
			get { return volumetricLighting && WaterQualitySettings.Instance.AllowVolumetricLighting; }
		}

		/// <summary>
		/// Event invoked when wind direction changes.
		/// </summary>
		public WaterEvent WindDirectionChanged
		{
			get { return windDirectionChanged; }
		}

		/// <summary>
		/// Event invoked when profiles change.
		/// </summary>
		public WaterEvent ProfilesChanged
		{
			get { return profilesChanged; }
		}

		/// <summary>
		/// Retrieves a SpectraRenderer of this water. It's one of the classes providing basic water functionality.
		/// </summary>
		public SpectraRenderer SpectraRenderer
		{
			get { return spectraRenderer; }
		}

		/// <summary>
		/// Retrieves a WaterPrecompute of this water. It's one of the classes providing basic water functionality.
		/// </summary>
		public WaterPrecompute WaterPrecompute
		{
			get { return waterPrecompute; }
		}

		/// <summary>
		/// Retrieves a WaterVolume of this water. It's one of the classes providing basic water functionality.
		/// </summary>
		public WaterVolume Volume
		{
			get { return volume; }
		}

		/// <summary>
		/// Retrieves a WaterGeometry of this water. It's one of the classes providing basic water functionality.
		/// </summary>
		public WaterGeometry Geometry
		{
			get { return geometry; }
		}

		/// <summary>
		/// Retrieves a WaterRenderer of this water. It's one of the classes providing basic water functionality.
		/// </summary>
		public WaterRenderer Renderer
		{
			get { return renderer; }
		}

		public int Seed
		{
			get { return seed; }
			set { seed = value; }
		}
		
		public float Density
		{
			get { return density; }
		}

		public ShaderCollection ShaderCollection
		{
			get { return shaderCollection; }
		}

		void OnEnable()
		{
			CreateParameterHashes();

#if UNITY_EDITOR
			OnValidate();
#endif
			
			if(!IsNotCopied())
				shaderCollection = null;

			instanceId = GetInstanceID();
            CreateMaterials();

			if(profiles == null && profile != null)
			{
				profiles = new WeightedProfile[] { new WeightedProfile(profile, 1.0f) };
				ResolveProfileData(profiles);
			}

			WaterQualitySettings.Instance.Changed -= OnQualitySettingsChanged;
			WaterQualitySettings.Instance.Changed += OnQualitySettingsChanged;
			
			WaterGlobals.Instance.AddWater(this);

			if(geometry != null)
			{
				geometry.OnEnable(this);
				renderer.OnEnable(this);
				volume.OnEnable(this);
			}
		}

		void OnDisable()
		{
			WaterGlobals.Instance.RemoveWater(this);

			geometry.OnDisable();
			renderer.OnDisable();
			volume.OnDisable();
		}

		void OnDestroy()
		{
			WaterQualitySettings.Instance.Changed -= OnQualitySettingsChanged;

			if(spectraRenderer != null) spectraRenderer.OnDestroy();
		}

		public float GetHeightAt(float x, float z, float precision = 1.0f, int correctionSteps = 0)
		{
			float tx = x, tz = z;
			float correctionPrecision = precision * 0.4f;

			for(int i = 0; i < correctionSteps; ++i)
			{
				Vector2 displacement = spectraRenderer.GetHorizontalDisplacementAt(x, z, 0, correctionPrecision);

				x += (tx - (x + displacement.x)) * 0.75f;
				z += (tz - (z + displacement.y)) * 0.75f;
			}

			return spectraRenderer.GetHeightAt(x, z, 0.0f, precision);
		}

		public void CacheProfiles(params WaterProfile[] profiles)
		{
			foreach(var profile in profiles)
				spectraRenderer.CacheSpectrum(profile.Spectrum);
		}

		public void SetProfiles(params WeightedProfile[] profiles)
		{
			ValidateProfiles(profiles);

			this.profiles = profiles;
			profilesDirty = true;
		}

		private void CreateMaterials()
		{
			if(waterMaterial == null)
			{
				waterMaterial = new Material(waterShader);
				waterMaterial.hideFlags = HideFlags.DontSave;
			}

			if(waterVolumeMaterial == null)
			{
				waterVolumeMaterial = new Material(waterVolumeShader);
				waterVolumeMaterial.hideFlags = HideFlags.DontSave;
			}
		}

		private void SetupMaterials()
		{
			var waterQualitySettings = WaterQualitySettings.Instance;
            BuildShaderVariant(waterMaterial, waterQualitySettings.GetQualityLevelsDirect()[waterQualitySettings.GetQualityLevel()]);

			ValidateShaderCollection(waterMaterial);
			ValidateShaderCollection(waterVolumeMaterial);
		}

		private void ValidateShaderCollection(Material material)
		{
#if UNITY_EDITOR
			if(!Application.isPlaying && shaderCollection != null && !shaderCollection.ContainsShaderVariant(material.shader, material.shaderKeywords))
				RebuildShaderCollection();
#endif
		}

		[ContextMenu("Rebuild Shader Collection")]
		private void RebuildShaderCollection()
		{
#if UNITY_EDITOR
			if(shaderCollection == null)
			{
				Debug.LogError("You have to create a shader collection first.");
				return;
			}

			ShaderCollectionRebuilder.Instance.Rebuild(shaderCollection);
#endif
		}

		private void UpdateWaterVolumeMaterial()
		{
			if(waterVolumeMaterial != null)
			{
				waterVolumeMaterial.CopyPropertiesFromMaterial(waterMaterial);
				waterVolumeMaterial.renderQueue = (refraction || blendEdges) ? 2991 : 2001;
				waterVolumeMaterial.DisableKeyword("_FFT_WAVES");
				waterVolumeMaterial.DisableKeyword("_GERSTNER_WAVES");
				waterVolumeMaterial.EnableKeyword("_DISPLACED_VOLUME");
			}
		}

		public bool SetKeyword(string keyword, bool enable)
		{
			if(waterMaterial != null)
			{
				if(enable)
				{
					if(!waterMaterial.IsKeywordEnabled(keyword))
					{
						waterMaterial.EnableKeyword(keyword);
						return true;
					}
				}
				else
				{
					if(waterMaterial.IsKeywordEnabled(keyword))
					{
						waterMaterial.DisableKeyword(keyword);
						return true;
					}
				}
			}

			return false;
		}

		public void OnValidate()
		{
			if(waterShader == null)
				waterShader = Shader.Find("PlayWay Water/Standard");

			if(waterVolumeShader == null)
				waterVolumeShader = Shader.Find("PlayWay Water/Standard Volume");

			renderAwareComponents = GetComponents<IWaterRenderAware>();
			gameObject.layer = 4;

			if(waterMaterial == null)
				return;                 // wait for OnEnable
			
			CreateParameterHashes();

			if(profiles != null && profiles.Length != 0)
				ResolveProfileData(profiles);
			else if(profile != null)
				ResolveProfileData(new WeightedProfile[] { new WeightedProfile(profile, 1.0f) });

			geometry.OnValidate(this);
			renderer.OnValidate(this);
			spectraRenderer.OnValidate(this);
			waterPrecompute.OnValidate(this);
			
			SetupMaterials();
		}

		private void ResolveProfileData(WeightedProfile[] profiles)
		{
			WaterProfile topProfile = profiles[0].profile;
			float topWeight = 0.0f;

			foreach(var weightedProfile in profiles)
			{
				if(topProfile == null || topWeight < weightedProfile.weight)
				{
					topProfile = weightedProfile.profile;
					topWeight = weightedProfile.weight;
				}
			}

			tileSize = 0.0f;
			horizontalDisplacementScale = 0.0f;
			windSpeedMagnitude = 0.0f;
			gravity = 0.0f;
			directionality = 0.0f;
			density = 0.0f;
			underwaterAbsorptionColor = new Color(0.0f, 0.0f, 0.0f);

			Color absorptionColor = new Color(0.0f, 0.0f, 0.0f);
			Color diffuseColor = new Color(0.0f, 0.0f, 0.0f);
			Color specularColor = new Color(0.0f, 0.0f, 0.0f);
			Color depthColor = new Color(0.0f, 0.0f, 0.0f);
			Color emissionColor = new Color(0.0f, 0.0f, 0.0f);
			
			float smoothness = 0.0f;
			float subsurfaceScattering = 0.0f;
			float refractionDistortion = 0.0f;
			float fresnelBias = 0.0f;
			float normalsFadeBias = 0.0f;
			float normalsFadeDistance = 0.0f;
			float detailFadeDistance = 0.0f;
			float displacementNormalsIntensity = 0.0f;
			float edgeBlendFactor = 0.0f;
			float directionalWrapSSS = 0.0f;
			float pointWrapSSS = 0.0f;

			Vector3 planarReflectionPack = new Vector3();
			Vector2 foamTiling = new Vector2();
			var normalMapAnimation1 = new NormalMapAnimation();
			var normalMapAnimation2 = new NormalMapAnimation();

			foreach(var weightedProfile in profiles)
			{
				var profile = weightedProfile.profile;
				float weight = weightedProfile.weight;

				tileSize += profile.TileSize * profile.TileScale * weight;
				horizontalDisplacementScale += profile.HorizontalDisplacementScale * weight;
				windSpeedMagnitude += profile.WindSpeed * weight;
				gravity -= profile.Gravity * weight;
				directionality += profile.Directionality * weight;
				density += profile.Density * weight;
				underwaterAbsorptionColor += profile.UnderwaterAbsorptionColor * weight;

				absorptionColor += profile.AbsorptionColor * weight;
				diffuseColor += profile.DiffuseColor * weight;
				specularColor += profile.SpecularColor * weight;
				depthColor += profile.DepthColor * weight;
				emissionColor += profile.EmissionColor * weight;
				
				smoothness += profile.Smoothness * weight;
				subsurfaceScattering += profile.SubsurfaceScattering * weight;
				refractionDistortion += profile.RefractionDistortion * weight;
				fresnelBias += profile.FresnelBias * weight;
				normalsFadeBias += profile.NormalsFadeBias * weight;
				normalsFadeDistance += profile.NormalsFadeDistance * weight;
				detailFadeDistance += profile.DetailFadeDistance * weight;
				displacementNormalsIntensity += profile.DisplacementNormalsIntensity * weight;
				edgeBlendFactor += profile.EdgeBlendFactor * weight;
				directionalWrapSSS += profile.DirectionalWrapSSS * weight;
				pointWrapSSS += profile.PointWrapSSS * weight;

				planarReflectionPack.x -= profile.PlanarReflectionDistortion * weight;
				planarReflectionPack.y += profile.PlanarReflectionIntensity * weight;
				planarReflectionPack.z += profile.PlanarReflectionOffset * weight;

				foamTiling += profile.FoamTiling * weight;
				normalMapAnimation1 += profile.NormalMapAnimation1 * weight;
				normalMapAnimation2 += profile.NormalMapAnimation2 * weight;
			}

			// scale by quality settings
			var waterQualitySettings = WaterQualitySettings.Instance;
			tileSize *= waterQualitySettings.TileSizeScale;

			var wavesFFT = GetComponent<WaterWavesFFT>();

			if(wavesFFT != null && wavesFFT.FinalRenderedMaps == WaterWavesFFT.MapType.Slope)
				displacementNormalsIntensity *= 0.5f;

			// apply to materials
			waterMaterial.SetColor(parameterHashes[0], absorptionColor);
			waterMaterial.SetColor(parameterHashes[1], diffuseColor);
			waterMaterial.SetColor(parameterHashes[2], specularColor);
			waterMaterial.SetColor(parameterHashes[3], depthColor);
			waterMaterial.SetColor(parameterHashes[4], emissionColor);
			waterMaterial.SetFloat(parameterHashes[5], horizontalDisplacementScale);

			waterMaterial.SetFloat(parameterHashes[6], smoothness);
			waterMaterial.SetVector(parameterHashes[7], new Vector4(subsurfaceScattering, 0.15f, 1.65f, 0.0f));
			waterMaterial.SetVector(parameterHashes[8], new Vector4(directionalWrapSSS, 1.0f / (1.0f + directionalWrapSSS), pointWrapSSS, 1.0f / (1.0f + pointWrapSSS)));
			waterMaterial.SetFloat(parameterHashes[9], refractionDistortion);
			waterMaterial.SetFloat(parameterHashes[10], fresnelBias);
			waterMaterial.SetVector(parameterHashes[11], new Vector4(1.0f - normalsFadeBias / normalsFadeDistance, 1.0f / normalsFadeDistance, 1.0f / (tileSize * detailFadeDistance), 0.0f));
			waterMaterial.SetFloat(parameterHashes[12], displacementNormalsIntensity);
			waterMaterial.SetFloat(parameterHashes[13], 1.0f / edgeBlendFactor);
			waterMaterial.SetVector(parameterHashes[14], planarReflectionPack);
			waterMaterial.SetVector(parameterHashes[15], new Vector4(normalMapAnimation1.Intensity, normalMapAnimation2.Intensity, -(normalMapAnimation1.Intensity + normalMapAnimation2.Intensity) * 0.5f, 0.0f));
			waterMaterial.SetTextureScale("_BumpMap", normalMapAnimation1.Tiling);
			waterMaterial.SetTextureScale("_DetailAlbedoMap", normalMapAnimation2.Tiling);
			waterMaterial.SetVector(parameterHashes[16], new Vector2(foamTiling.x / normalMapAnimation1.Tiling.x, foamTiling.y / normalMapAnimation1.Tiling.y));
			waterMaterial.SetFloat(parameterHashes[17], tileSize);

			waterMaterial.SetTexture(parameterHashes[18], topProfile.NormalMap);
			waterMaterial.SetTexture(parameterHashes[19], topProfile.FoamDiffuseMap);
			waterMaterial.SetTexture(parameterHashes[20], topProfile.FoamNormalMap);

			waterUvAnimator.NormalMapAnimation1 = normalMapAnimation1;
			waterUvAnimator.NormalMapAnimation2 = normalMapAnimation2;

			SetKeyword("_EMISSION", emissionColor.grayscale != 0);

			UpdateWaterVolumeMaterial();
		}

		void Update()
		{
			if(!Application.isPlaying) return;

			waterPrecompute.Update();
			waterUvAnimator.Update();
			geometry.Update();

			if(autoDepthColor)
				UpdateDepthColor();

			FireEvents();

#if WATER_DEBUG
			if(Time.frameCount == 120)
				WaterDebug.WriteAllMaps(this);
#endif
		}

		public void OnWaterRender(Camera camera)
		{
			if(!isActiveAndEnabled) return;

			foreach(var component in renderAwareComponents)
			{
				if(((MonoBehaviour)component) != null && ((MonoBehaviour)component).enabled)
					component.OnWaterRender(camera);
			}
		}

		public void OnWaterPostRender(Camera camera)
		{
			foreach(var component in renderAwareComponents)
			{
				if(((MonoBehaviour)component) != null && ((MonoBehaviour)component).enabled)
					component.OnWaterPostRender(camera);
			}
		}

		private void UpdateDepthColor()
		{
			switch(RenderSettings.ambientMode)
			{
				case AmbientMode.Flat:
				{
					waterMaterial.SetColor("_DepthColor", maxDepthColor * RenderSettings.ambientLight * RenderSettings.ambientIntensity);
					break;
				}

				case AmbientMode.Trilight:
				{
					waterMaterial.SetColor("_DepthColor", maxDepthColor * (RenderSettings.ambientSkyColor + RenderSettings.ambientEquatorColor) * 0.5f * RenderSettings.ambientIntensity);
					break;
				}
			}
		}

		private void AddDefaultComponents()
		{
			if(GetComponent<WaterPlanarReflection>() == null)
				gameObject.AddComponent<WaterPlanarReflection>();
			
			if(GetComponent<WaterWavesFFT>() == null)
				gameObject.AddComponent<WaterWavesFFT>();

			if(GetComponent<WaterWavesGerstner>() == null)
			{
				var gerstnerWaves = gameObject.AddComponent<WaterWavesGerstner>();
				gerstnerWaves.enabled = false;
			}

			if(GetComponent<WaterFoam>() == null)
				gameObject.AddComponent<WaterFoam>();

			if(GetComponent<WaterSpray>() == null)
				gameObject.AddComponent<WaterSpray>();
		}

		private bool IsNotCopied()
		{
#if UNITY_EDITOR
			if(string.IsNullOrEmpty(UnityEditor.EditorApplication.currentScene))
				return true;

			var md5 = System.Security.Cryptography.MD5.Create();
			var hash = md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(UnityEditor.EditorApplication.currentScene + "#" + name));
			return instanceId == GetInstanceID() || namesHash == System.BitConverter.ToInt32(hash, 0);
#else
			return true;
#endif
		}

		private void OnQualitySettingsChanged()
		{
			OnValidate();
			profilesDirty = true;
		}

		private void FireEvents()
		{
			if(lastWindDirection != WindDirection)
			{
				lastWindDirection = WindDirection;
				windDirectionChanged.Invoke(this);
			}

			if(profilesDirty)
			{
				profilesDirty = false;
				profilesChanged.Invoke(this);
			}
		}

		void OnProfilesChanged(Water water)
		{
			spectraRenderer.OnProfilesChanged();
			waterMaterial.SetFloat("_MaxDisplacement", spectraRenderer.MaxDisplacement);

			ResolveProfileData(profiles);
		}

		void OnWindDirectionChanged(Water water)
		{
			spectraRenderer.SetWindDirection(water.WindDirection);
		}

		private void ValidateProfiles(WeightedProfile[] profiles)
		{
			if(profiles.Length == 0)
				throw new System.ArgumentException("Water has to use at least one profile.");

			float tileSize = profiles[0].profile.TileSize;

			for(int i = 1; i < profiles.Length; ++i)
			{
				if(profiles[i].profile.TileSize != tileSize)
				{
					Debug.LogError("TileSize varies between used water profiles. It is the only parameter that you should keep equal on all profiles used at a time.");
					break;
				}
			}
		}

		private void CreateParameterHashes()
		{
			if(parameterHashes != null)
				return;

			int numParameters = parameterNames.Length;
            parameterHashes = new int[numParameters];

			for(int i=0; i<numParameters; ++i)
				parameterHashes[i] = Shader.PropertyToID(parameterNames[i]);
		}

		private void BuildShaderVariant(Material material, WaterQualityLevel qualityLevel)
		{
			if(renderer == null)
				return;             // still not properly initialized

			var originalWaterMaterial = this.waterMaterial;
            this.waterMaterial = material;

			try
			{
				bool blendEdges = this.blendEdges && qualityLevel.allowAlphaBlending;
				bool refraction = this.refraction && qualityLevel.allowAlphaBlending;

				foreach(var component in renderAwareComponents)
					component.ValidateNow(this, qualityLevel);

				// clear after non-existing components; not sure if that's needed but..
				var waterFFT = GetComponent<WaterWavesFFT>();

				if(waterFFT == null)
				{
					SetKeyword("_FFT_WAVES", false);
					SetKeyword("_FFT_WAVES_SLOPE", false);
				}

				var gerstnerWaves = GetComponent<WaterWavesGerstner>();

				if(gerstnerWaves == null)
					SetKeyword("_GERSTNER_WAVES", false);

				var waterOverlays = GetComponent<WaterWaveOverlays>();

				if(waterOverlays == null)
					SetKeyword("_WATER_OVERLAYS", false);

				var planarReflections = GetComponent<WaterPlanarReflection>();

				if(planarReflections == null)
					SetKeyword("_PLANAR_REFLECTIONS", false);

				SetKeyword("_WATER_REFRACTION", refraction);
				SetKeyword("_VOLUMETRIC_LIGHTING", volumetricLighting && qualityLevel.allowVolumetricLighting);
				SetKeyword("_CUBEMAP_REFLECTIONS", useCubemapReflections);
				SetKeyword("_INCLUDE_SLOPE_VARIANCE", waterPrecompute.Enabled);
				SetKeyword("_NORMALMAP", waterMaterial.GetTexture("_BumpMap") != null);

				// clean after components not executing in edit mode
				var waterFoam = GetComponent<WaterFoam>();
				if(waterFoam == null || !waterFoam.enabled)
				{
					SetKeyword("_WATER_FOAM_LOCAL", false);
					SetKeyword("_WATER_FOAM_WS", false);
				}
				else
					waterFoam.SetupMaterials();

				bool alphaBlend = (refraction || blendEdges);

				waterMaterial.SetOverrideTag("RenderType", alphaBlend ? "Transparent" : "Opaque");
				waterMaterial.SetFloat("_Mode", alphaBlend ? 2 : 0);
				waterMaterial.SetInt("_SrcBlend", (int)(alphaBlend ? BlendMode.SrcAlpha : BlendMode.One));
				waterMaterial.SetInt("_DstBlend", (int)(alphaBlend ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
				SetKeyword("_ALPHATEST_ON", false);
				SetKeyword("_ALPHABLEND_ON", alphaBlend);
				SetKeyword("_ALPHAPREMULTIPLY_ON", !alphaBlend);
				waterMaterial.renderQueue = alphaBlend ? 2990 : 2000;       // 2000 - geometry, 3000 - transparent

				SetKeyword("_QUADS", !geometry.Triangular);

				waterMaterial.SetFloat("_TesselationFactor", Mathf.Lerp(32.0f, 6.0f, Mathf.Min(tesselationFactor, qualityLevel.maxTesselationFactor)));
				UpdateWaterVolumeMaterial();
			}
			finally
			{
				this.waterMaterial = originalWaterMaterial;
			}
		}

		private void AddShaderVariants(ShaderCollection collection)
		{
			var material = Instantiate(waterMaterial);

			foreach(var qualityLevel in WaterQualitySettings.Instance.GetQualityLevelsDirect())
			{
				BuildShaderVariant(material, qualityLevel);

				collection.AddShaderVariant(material.shader, material.shaderKeywords);
			}

			DestroyImmediate(material);
		}

		public void Write(ShaderCollection collection)
		{
			if(collection == shaderCollection)
			{
				if(waterMaterial != null)
					AddShaderVariants(collection);

				if(waterVolumeMaterial != null)
					collection.AddShaderVariant(waterVolumeMaterial.shader, waterVolumeMaterial.shaderKeywords);
			}
		}

		[System.Serializable]
		public class WaterEvent : UnityEvent<Water> { };

		public struct WeightedProfile
		{
			public WaterProfile profile;
			public float weight;

			public WeightedProfile(WaterProfile profile, float weight)
			{
				this.profile = profile;
				this.weight = weight;
			}
		}

		public struct VectorParameterOverride
		{
			public int hash;
			public Vector4 value;
		}

		public struct ColorParameterOverride
		{
			public int hash;
			public Color value;
		}

		public struct FloatParameterOverride
		{
			public int hash;
			public float value;
		}

		public struct TextureParameterOverride
		{
			public int hash;
			public Texture value;
		}

		public enum ColorParameters
		{
			AbsorptionColor = 0,
			DiffuseColor = 1,
			SpecularColor = 2,
			DepthColor = 3,
			EmissionColor = 4
		}

		public enum FloatParameters
		{
			DisplacementScale = 5,
			Glossiness = 6,
			RefractionDistortion = 9,
			SpecularFresnelBias = 10,
			DisplacementNormalsIntensity = 12,
			EdgeBlendFactorInv = 13,
			WaterTileSize = 17
		}

		public enum VectorParameters
		{
			SubsurfaceScatteringPack = 7,
			WrapSubsurfaceScatteringPack = 8,
			DistantFadeFactors = 11,
			PlanarReflectionPack = 14,
			BumpScale = 15,
			FoamTiling = 16
		}

		public enum TextureParameters
		{
			BumpMap = 18,
			FoamTex = 19,
			FoamNormalMap = 20
		}
	}
}
