using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlayWay.Water
{
	public class WaterProfile : ScriptableObject
	{
		[HideInInspector]
		[SerializeField]
		private Shader spectrumShader;

		[SerializeField]
		private WaterSpectrumType spectrumType = WaterSpectrumType.Unified;

		[SerializeField]
		private float windSpeed = 22.0f;

		[Tooltip("Tile size in world units of all water maps including heightmap. High values lower overall quality, but low values make the water pattern noticeable.")]
		[SerializeField]
		private float tileSize = 180.0f;

		[SerializeField]
		private float tileScale = 1.0f;

		[Tooltip("Setting it to something else than 1.0 will make the spectrum less physical, but still may be useful at times.")]
		[SerializeField]
		private float wavesAmplitude = 1.0f;

		[SerializeField]
		private float horizontalDisplacementScale = 1.0f;

		[SerializeField]
		private float phillipsCutoffFactor = 2000.0f;

		[SerializeField]
		private float gravity = -9.81f;

		[Tooltip("It is the length of water in meters over which a wind has blown. Usually a distance to the closest land in the direction opposite to the wind.")]
		[SerializeField]
		private float fetch = 100000.0f;

		[Tooltip("Eliminates waves moving against the wind.")]
		[Range(0.0f, 1.0f)]
		[SerializeField]
		private float directionality = 0.0f;

		[ColorUsage(false, true, 0.0f, 1.0f, 0.0f, 1.0f)]
		[SerializeField]
		private Color absorptionColor = new Color(0.35f, 0.04f, 0.001f, 1.0f);

		[Tooltip("Used by the underwater camera image-effect.")]
		[ColorUsage(false, true, 0.0f, 1.0f, 0.0f, 1.0f)]
		[SerializeField]
		private Color underwaterAbsorptionColor = new Color(0.35f, 0.04f, 0.001f, 1.0f);

		[ColorUsage(false)]
		[SerializeField]
		private Color diffuseColor = new Color(0.1176f, 0.2196f, 0.2666f);

		[ColorUsage(false)]
		[SerializeField]
		private Color specularColor = new Color(0.0353f, 0.0471f, 0.0549f);

		[ColorUsage(false)]
		[SerializeField]
		private Color depthColor = new Color(0.0f, 0.0f, 0.0f);

		[ColorUsage(false)]
		[SerializeField]
		private Color emissionColor = new Color(0.0f, 0.0f, 0.0f);

		[Range(0.0f, 1.0f)]
		[SerializeField]
		private float smoothness = 0.94f;
		
		[Range(0.0f, 4.0f)]
		[SerializeField]
		private float subsurfaceScattering = 1.0f;

		[Range(0.0f, 1.0f)]
		[SerializeField]
		private float refractionDistortion = 0.55f;
		
		[SerializeField]
		private float fresnelBias = 0.02040781f;

		[Tooltip("Water gets really noisy at a distance and SMAA or FXAA won't handle that. This parameter will let you fade water's normals to avoid this problem.")]
		[SerializeField]
		private float normalsFadeBias = 10.0f;

		[Tooltip("Water gets really noisy at a distance and SMAA or FXAA won't handle that. This parameter will let you fade water's normals to avoid this problem.")]
		[SerializeField]
		private float normalsFadeDistance = 90.0f;

		[SerializeField]
		private float detailFadeDistance = 2.5f;

		[Range(0.1f, 10.0f)]
		[SerializeField]
		private float displacementNormalsIntensity = 2.0f;

		[Tooltip("Planar reflections are very good solution for calm weather, but you should fade them out for profiles with big waves (storms etc.) as they get completely incorrect then.")]
		[Range(0.0f, 1.0f)]
		[SerializeField]
		private float planarReflectionIntensity = 0.6f;

		[SerializeField]
		private float planarReflectionDistortion = 18.0f;

		[Range(-0.5f, 0.1f)]
		[SerializeField]
		private float planarReflectionOffset = -0.3f;

		[SerializeField]
		private float edgeBlendFactor = 0.15f;

		[SerializeField]
		private float directionalWrapSSS = 0.2f;

		[SerializeField]
		private float pointWrapSSS = 0.5f;

		[Tooltip("Used by the physics.")]
		[SerializeField]
		private float density = 998.6f;

		[SerializeField]
		private NormalMapAnimation normalMapAnimation1 = new NormalMapAnimation(1.0f, -10.0f, 1.0f, new Vector2(1.0f, 1.0f));

		[SerializeField]
		private NormalMapAnimation normalMapAnimation2 = new NormalMapAnimation(-0.55f, 20.0f, 0.74f, new Vector2(1.5f, 1.5f));

		[SerializeField]
		private Texture2D normalMap;

		//[Tooltip("Used for parallax mapping.")]
		//[SerializeField]
		//private Texture2D heightMap;

		[SerializeField]
		private Texture2D foamDiffuseMap;

		[SerializeField]
		private Texture2D foamNormalMap;

		[SerializeField]
		private Vector2 foamTiling = new Vector2(5.4f, 5.4f);
		
		private WaterWavesSpectrum spectrum;

		public WaterSpectrumType SpectrumType
		{
			get { return spectrumType; }
		}

		public WaterWavesSpectrum Spectrum
		{
			get
			{
				if(spectrum == null)
					CreateSpectrum();

                return spectrum;
			}
		}

		public float WindSpeed
		{
			get { return windSpeed; }
		}

		public float TileSize
		{
			get { return tileSize; }
		}

		public float TileScale
		{
			get { return tileScale; }
		}

		public float HorizontalDisplacementScale
		{
			get { return horizontalDisplacementScale; }
		}

		public float Gravity
		{
			get { return gravity; }
		}

		public float Directionality
		{
			get { return directionality; }
		}

		public Color AbsorptionColor
		{
			get { return absorptionColor; }
		}

		public Color UnderwaterAbsorptionColor
		{
			get { return underwaterAbsorptionColor; }
		}

		public Color DiffuseColor
		{
			get { return diffuseColor; }
		}

		public Color SpecularColor
		{
			get { return specularColor; }
		}

		public Color DepthColor
		{
			get { return depthColor; }
		}

		public Color EmissionColor
		{
			get { return emissionColor; }
		}

		public float Smoothness
		{
			get { return smoothness; }
		}
		
		public float SubsurfaceScattering
		{
			get { return subsurfaceScattering; }
		}

		public float RefractionDistortion
		{
			get { return refractionDistortion; }
		}

		public float FresnelBias
		{
			get { return fresnelBias; }
		}

		public float NormalsFadeBias
		{
			get { return normalsFadeBias; }
		}

		public float NormalsFadeDistance
		{
			get { return normalsFadeDistance; }
		}

		public float DetailFadeDistance
		{
			get { return detailFadeDistance; }
		}

		public float DisplacementNormalsIntensity
		{
			get { return displacementNormalsIntensity; }
		}

		public float PlanarReflectionIntensity
		{
			get { return planarReflectionIntensity; }
		}

		public float PlanarReflectionDistortion
		{
			get { return planarReflectionDistortion; }
		}

		public float PlanarReflectionOffset
		{
			get { return planarReflectionOffset; }
		}

		public float EdgeBlendFactor
		{
			get { return edgeBlendFactor; }
		}

		public float DirectionalWrapSSS
		{
			get { return directionalWrapSSS; }
		}

		public float PointWrapSSS
		{
			get { return pointWrapSSS; }
		}

		public float Density
		{
			get { return density; }
		}

		public NormalMapAnimation NormalMapAnimation1
		{
			get { return normalMapAnimation1; }
		}

		public NormalMapAnimation NormalMapAnimation2
		{
			get { return normalMapAnimation2; }
		}

		public Texture2D NormalMap
		{
			get { return normalMap; }
		}

		public Texture2D FoamDiffuseMap
		{
			get { return foamDiffuseMap; }
		}

		public Texture2D FoamNormalMap
		{
			get { return foamNormalMap; }
		}

		public Vector2 FoamTiling
		{
			get { return foamTiling; }
		}

		public void CacheSpectrum()
		{
			if(spectrum == null)
				CreateSpectrum();
		}

		void OnEnable()
		{
			if(spectrum == null)
				CreateSpectrum();
		}

		private void CreateSpectrum()
		{
			switch(spectrumType)
			{
				case WaterSpectrumType.Unified:
				{
					spectrum = new UnifiedSpectrum(tileSize, -gravity, windSpeed, wavesAmplitude, fetch);
					break;
				}

				case WaterSpectrumType.Phillips:
				{
					spectrum = new PhillipsSpectrum(tileSize, -gravity, windSpeed, wavesAmplitude, phillipsCutoffFactor);
					break;
				}
			}
		}

#if UNITY_EDITOR
		[MenuItem("Assets/Create/PlayWay Water Profile")]
		static public void CreateProfile()
		{
			string path = AssetDatabase.GetAssetPath(Selection.activeObject);

			if(path == "")
				path = "Assets";
			else if(System.IO.Path.GetExtension(path) != "")
				path = path.Replace(System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");

			var bundle = ScriptableObject.CreateInstance<WaterProfile>();

			AssetDatabase.CreateAsset(bundle, AssetDatabase.GenerateUniqueAssetPath(path + "/New Water Profile.asset"));
			AssetDatabase.SaveAssets();

			Selection.activeObject = bundle;
		}
#endif

		public enum WaterSpectrumType
		{
			Phillips,
			Unified
		}
	}
}
