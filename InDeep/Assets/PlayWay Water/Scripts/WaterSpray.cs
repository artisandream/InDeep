using UnityEngine;

namespace PlayWay.Water
{
	[RequireComponent(typeof(Water))]
	[RequireComponent(typeof(WaterWavesFFT))]
	[AddComponentMenu("Water/Spray")]
	public class WaterSpray : MonoBehaviour
	{
		[HideInInspector]
		[SerializeField]
		private Shader sprayGeneratorShader;

		[HideInInspector]
		[SerializeField]
		private ComputeShader sprayControllerShader;

		[SerializeField]
		private Material sprayMaterial;

		[Range(16, 65535)]
		[SerializeField]
		private int maxParticles = 65535;
		
		[Range(0.0f, 4.0f)]
		[SerializeField]
		private float spawnBoost = 1.0f;

		[Range(0.0f, 0.999f)]
		[SerializeField]
		private float spawnSkipRatio = 0.975f;
		
		private Water water;
		private WaterWavesFFT wavesSimulation;
		private Material sprayGeneratorMaterial;
		private Transform probeAnchor;

		private RenderTexture blankOutput;
		private ComputeBuffer particlesA;
		private ComputeBuffer particlesB;
		private ComputeBuffer particlesBInfo;
		private int resolution;
		private Mesh mesh;
		private bool supported;
		private bool resourcesReady;
		private int[] countBuffer = new int[4];
		private float finalSpawnSkipRatio;
		private Vector2 paramsPrecomp;

		void Start()
		{
			water = GetComponent<Water>();
			wavesSimulation = GetComponent<WaterWavesFFT>();

			water.SpectraRenderer.ResolutionChanged += OnResolutionChanged;
			
			supported = CheckSupport();

			if(!supported)
			{
				enabled = false;
				return;
			}
		}

		public int CurrentParticleCount
		{
			get
			{
				ComputeBuffer.CopyCount(particlesA, particlesBInfo, 0);
				particlesBInfo.GetData(countBuffer);
				return countBuffer[0];
			}
		}
		
		private bool CheckSupport()
		{
			return SystemInfo.supportsComputeShaders && sprayGeneratorShader != null && sprayGeneratorShader.isSupported;
		}

		private void CheckResources()
		{
			if(sprayGeneratorMaterial == null)
			{
				sprayGeneratorMaterial = new Material(sprayGeneratorShader);
				sprayGeneratorMaterial.hideFlags = HideFlags.DontSave;
			}

			if(blankOutput == null)
			{
				UpdatePrecomputedParams();

				blankOutput = new RenderTexture(resolution, resolution, 0, SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8) ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
				blankOutput.filterMode = FilterMode.Point;
				blankOutput.Create();
			}

			if(probeAnchor == null)
			{
				var probeAnchorGo = new GameObject("Spray Probe Anchor");
				probeAnchorGo.hideFlags = HideFlags.HideAndDontSave;
				probeAnchor = probeAnchorGo.transform;
			}

			if(mesh == null)
			{
				mesh = new Mesh();
				mesh.name = "Spray";
				mesh.hideFlags = HideFlags.DontSave;
				mesh.vertices = new Vector3[maxParticles];

				int[] indices = new int[maxParticles];

				for(int i = 0; i < maxParticles; ++i)
					indices[i] = i;

				float size = water.TileSize * 1.6f;

				mesh.SetIndices(indices, MeshTopology.Points, 0);
				mesh.bounds = new Bounds(Vector3.zero, new Vector3(size, size, size));
			}

			if(particlesA == null)
				particlesA = new ComputeBuffer(maxParticles, 40, ComputeBufferType.Append);

			if(particlesB == null)
				particlesB = new ComputeBuffer(maxParticles, 40, ComputeBufferType.Append);

			if(particlesBInfo == null)
			{
				particlesBInfo = new ComputeBuffer(1, 16, ComputeBufferType.DrawIndirect);
				var args = new int[4];
				args[0] = 0;
				args[1] = 1;
				args[2] = 0;
				args[3] = 0;
				particlesBInfo.SetData(args);
			}

			resourcesReady = true;
        }

		private void Dispose()
		{
			if(blankOutput != null)
			{
				Destroy(blankOutput);
				blankOutput = null;
			}

			if(particlesA != null)
			{
				particlesA.Dispose();
				particlesA = null;
			}

			if(particlesB != null)
			{
				particlesB.Dispose();
				particlesB = null;
			}

			if(particlesBInfo != null)
			{
				particlesBInfo.Release();
				particlesBInfo = null;
			}

			if(mesh != null)
			{
				Destroy(mesh);
				mesh = null;
			}

			resourcesReady = false;
		}

		void OnEnable()
		{
			Camera.onPreCull -= OnSomeCameraPreCull;
			Camera.onPreCull += OnSomeCameraPreCull;
		}

		void OnDisable()
		{
			Camera.onPreCull -= OnSomeCameraPreCull;

			Dispose();
		}

		void OnValidate()
		{
			if(sprayGeneratorShader == null)
				sprayGeneratorShader = Shader.Find("PlayWay Water/Spray/Generator");
			
#if UNITY_EDITOR
			if(sprayControllerShader == null)
			{
				var guids = UnityEditor.AssetDatabase.FindAssets("\"SprayController\" t:ComputeShader");

				if(guids.Length != 0)
				{
					string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
					sprayControllerShader = (ComputeShader)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(ComputeShader));
					UnityEditor.EditorUtility.SetDirty(this);
				}
			}

			if(sprayMaterial == null)
			{
				var guids = UnityEditor.AssetDatabase.FindAssets("\"Spray\" t:Material");

				if(guids.Length != 0)
				{
					string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
					sprayMaterial = (Material)UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(Material));
					UnityEditor.EditorUtility.SetDirty(this);
				}
			}
#endif
			
			UpdatePrecomputedParams();
        }

		void LateUpdate()
		{
			if(Time.frameCount < 10)
				return;

			if(!resourcesReady)
				CheckResources();
			
			SwapParticleBuffers();
			ClearParticles();
			UpdateParticles();
			RenderJacobian();
		}

		void OnSomeCameraPreCull(Camera camera)
		{
			if(!resourcesReady) return;

			if(camera.GetComponent<WaterCamera>() != null)
			{
				float tileSize = water.TileSize;

				sprayMaterial.SetFloat("_TileSize", tileSize);
				sprayMaterial.SetBuffer("_Particles", particlesA);
				sprayMaterial.SetVector("_CameraUp", camera.transform.up);

				Vector3 pos = camera.transform.position;
				pos.x = (Mathf.Round(pos.x / tileSize) - 1) * tileSize;
				pos.y = 0.0f;
				pos.z = (Mathf.Round(pos.z / tileSize) - 1) * tileSize;

				Matrix4x4 matrix = Matrix4x4.identity;
				matrix.m03 = pos.x;
				matrix.m23 = pos.z;

				probeAnchor.position = pos;

				Graphics.DrawMesh(mesh, matrix, sprayMaterial, 0, camera, 0, null, UnityEngine.Rendering.ShadowCastingMode.Off, false, probeAnchor);

				matrix.m03 += tileSize;
				Graphics.DrawMesh(mesh, matrix, sprayMaterial, 0, camera, 0, null, UnityEngine.Rendering.ShadowCastingMode.Off, false, probeAnchor);

				matrix.m23 += tileSize;
				Graphics.DrawMesh(mesh, matrix, sprayMaterial, 0, camera, 0, null, UnityEngine.Rendering.ShadowCastingMode.Off, false, probeAnchor);

				matrix.m03 -= tileSize;
				Graphics.DrawMesh(mesh, matrix, sprayMaterial, 0, camera, 0, null, UnityEngine.Rendering.ShadowCastingMode.Off, false, probeAnchor);
			}
		}

		private void RenderJacobian()
		{
			sprayGeneratorMaterial.SetTexture("_HeightMap", wavesSimulation.HeightMap);
			sprayGeneratorMaterial.SetVector("_Params", new Vector4(paramsPrecomp.x * water.HorizontalDisplacementScale / water.TileSize, paramsPrecomp.y, water.HorizontalDisplacementScale / water.TileSize, 0.0f));
			Graphics.SetRandomWriteTarget(1, particlesA);
			Graphics.Blit(wavesSimulation.DisplacementMap, blankOutput, sprayGeneratorMaterial, 0);
			Graphics.ClearRandomWriteTargets();
        }

		private void UpdateParticles()
		{
			Vector2 windSpeed = water.WindSpeed * 0.0008f;
			Vector3 gravity = Physics.gravity;
			float deltaTime = Time.deltaTime;
			
            sprayControllerShader.SetFloat("deltaTime", deltaTime);
			sprayControllerShader.SetVector("externalForces", new Vector3((windSpeed.x + gravity.x) * deltaTime, gravity.y * deltaTime, (windSpeed.y + gravity.z) * deltaTime));
			//sprayControllerShader.SetTexture(0, "FoamMap", GetComponent<WaterMapEffects>().FoamMap);
			//sprayControllerShader.SetTexture(0, "DisplacementMap", GetComponent<WaterWavesFFT>().DistortionMap);
			sprayControllerShader.SetBuffer(0, "SourceParticles", particlesB);
			//sprayControllerShader.SetBuffer(0, "SourceParticlesInfo", particlesBInfo); 
			sprayControllerShader.SetBuffer(0, "TargetParticles", particlesA);
			sprayControllerShader.Dispatch(0, maxParticles / 128, 1, 1);
		}
		
		private void ClearParticles()
		{
			sprayControllerShader.SetBuffer(1, "TargetParticlesFlat", particlesA);
			sprayControllerShader.Dispatch(1, maxParticles / 128, 1, 1);
		}

		private void SwapParticleBuffers()
		{
			var t = particlesB;
			particlesB = particlesA;
			particlesA = t;
		}

		private void OnResolutionChanged()
		{
			if(blankOutput != null)
			{
				Destroy(blankOutput);
				blankOutput = null;
			}
			
			resourcesReady = false;
        }

		private void UpdatePrecomputedParams()
		{
			if(water != null)
				resolution = water.SpectraRenderer.FinalResolution;

			paramsPrecomp.x = spawnBoost * resolution / 2048.0f * 220.0f * 6.5f;
			paramsPrecomp.y = Mathf.Pow(spawnSkipRatio, 1024.0f / resolution);
        }
	}
}
