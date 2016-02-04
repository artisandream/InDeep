using UnityEngine;

namespace PlayWay.Water
{
	public class TerrainShoreline : MonoBehaviour, IWaterShore
	{
		[SerializeField]
		private WindWaves water;

		[SerializeField]
		private Transform center;

		[SerializeField]
		private int spawnPointsCount = 50;

		[Range(0.0f, 4.0f)]
		[SerializeField]
		private float extendShoreline = 1.5f;

		[SerializeField]
		private Blur blur;

		//[SerializeField]
		//private int resolution = 2048;

		[SerializeField]
		private Shader maskGenerateShader;

		[SerializeField]
		private Shader maskDisplayShader;

		private SpawnPoint[] spawnPoints;
		private WaterWavesParticleSystem waterParticleSystem;
		private RenderTexture intensityMask;
		private int resolutionSqr;

		private float[] intensityMaskData;
		private float offsetX, offsetZ, scaleX, scaleZ;
		private int width, height;

		static private Mesh quadMesh;

		void Start()
		{
			if(quadMesh == null)
				CreateQuadMesh();

			waterParticleSystem = water.GetComponent<WaterWavesParticleSystem>();

			if(waterParticleSystem == null)
				throw new System.Exception("TerrainShoreline requires WaterWavesParticleSystem component on target water.");

			var shorelinesRenderer = water.GetComponent<ShorelinesRenderer>();

			if(shorelinesRenderer == null)
			{
				water.gameObject.AddComponent<ShorelinesRenderer>();

				var overlays = water.GetComponent<WaterWaveOverlays>();
				overlays.ValidateWaterComponents();
			}

			//CreateSpawnPoints();
			CreateSpawnPointsFromSpectrum();
			RenderShorelineIntensityMask();
			CreateMaskRenderer();
        }

		public Texture IntensityMask
		{
			get { return intensityMask; }
		}

		void OnValidate()
		{
			if(maskGenerateShader == null)
				maskGenerateShader = Shader.Find("PlayWay Water/Utility/ShorelineMaskGenerate");

			if(maskDisplayShader == null)
				maskDisplayShader = Shader.Find("PlayWay Water/Utility/ShorelineMaskRender");
		}

		void Update()
		{
			float deltaTime = Time.deltaTime;

			foreach(var spawnPoint in spawnPoints)
			{
				spawnPoint.timeLeft -= deltaTime;

				if(spawnPoint.timeLeft < 0)
				{
					spawnPoint.timeLeft += spawnPoint.timeInterval;
					waterParticleSystem.Spawn(new WaterWavesParticleSystem.LinearParticle(spawnPoint.position, spawnPoint.direction, spawnPoint.frequency, spawnPoint.amplitude, Random.Range(190.0f, 581.0f), this), 4);
				}
			}

			/*if(Random.value < 0.08f)
			{
				var terrainCollider = GetComponent<TerrainCollider>();

				var spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length - 1)];
				waterParticleSystem.Spawn(new WaterWavesParticleSystem.LinearParticle(spawnPoint.position, spawnPoint.direction, spawnPoint.frequency, Random.Range(0.05f, 0.1f), Random.Range(90.0f, 281.0f), terrainCollider));
			}*/
		}

		/*private void CreateSpawnPoints()
		{
			var terrain = GetComponent<Terrain>();
			var terrainData = terrain.terrainData;
			var terrainCollider = GetComponent<TerrainCollider>();

			Vector2 terrainMin = new Vector2(terrain.transform.position.x - terrainData.size.x * 1.65f, terrain.transform.position.z - terrainData.size.z * 1.65f);
			Vector2 terrainMax = new Vector2(terrain.transform.position.x + terrainData.size.x * 1.65f, terrain.transform.position.z + terrainData.size.z * 1.65f);
			float waterY = water.transform.position.y;
			RaycastHit hitInfo;

			spawnPoints = new SpawnPoint[spawnPointsCount];

			for(int i=0; i< spawnPointsCount; ++i)
			{
				for(int ii = 0; ii < 40; ++ii)
				{
					Vector3 point = new Vector3(Random.Range(terrainMin.x, terrainMax.x), waterY + 1000.0f, Random.Range(terrainMin.y, terrainMax.y));

					if(!terrainCollider.Raycast(new Ray(point, Vector3.down), out hitInfo, 1000.0f) || ii == 19)
					{
						point.y = waterY;
						Vector2 closestBeachDir = FindClosestBeachDirection(terrainCollider, point);

						if(!float.IsNaN(closestBeachDir.x))
						{
							spawnPoints[i] = new SpawnPoint(new Vector2(point.x, point.z), closestBeachDir, Random.Range(0.0005f, 0.00125f));
							break;
						}
						else if(ii == 19)
						{
							spawnPoints[i] = new SpawnPoint(new Vector2(point.x, point.z), (terrain.transform.position - point).normalized, Random.Range(0.0005f, 0.00125f));
							break;
						}
					}
				}
			}
		}*/
		
		private void RenderShorelineIntensityMask()
		{
			var terrain = GetComponent<Terrain>();
			var terrainData = terrain.terrainData;

			int w = terrainData.heightmapWidth;
			int h = terrainData.heightmapHeight;
			var heightMap = new Texture2D(w, h, TextureFormat.RFloat, true, true);
			heightMap.wrapMode = TextureWrapMode.Clamp;
			var heights = terrainData.GetHeights(0, 0, w, h);
			float heightOffset = transform.position.y;
			float heightScale = terrainData.size.y;

			for(int y = 0; y < h; ++y)
			{
				for(int x = 0; x < w; ++x)
					heightMap.SetPixel(x, y, new Color(heights[y, x] * heightScale + heightOffset, 0.0f, 0.0f));
			}

			heightMap.Apply(false, true);

			if(intensityMask == null)
			{
				intensityMask = new RenderTexture(Mathf.RoundToInt(w * (1.0f + extendShoreline)), Mathf.RoundToInt(h * (1.0f + extendShoreline)), 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
				intensityMask.hideFlags = HideFlags.DontSave;
			}

			offsetX = -transform.position.x + terrainData.size.x * 0.5f * extendShoreline;
			offsetZ = -transform.position.z + terrainData.size.z * 0.5f * extendShoreline;
			scaleX = intensityMask.width / (terrainData.size.x * (1.0f + extendShoreline));
			scaleZ = intensityMask.height / (terrainData.size.z * (1.0f + extendShoreline));
			width = intensityMask.width;
			height = intensityMask.height;

			var temp = RenderTexture.GetTemporary(intensityMask.width, intensityMask.height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
			var temp2 = RenderTexture.GetTemporary(intensityMask.width, intensityMask.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
			var material = new Material(maskGenerateShader);
			material.SetFloat("_ShorelineExtendRange", extendShoreline);
			material.SetFloat("_TerrainMinPoint", -heightOffset);
			
			Graphics.Blit(heightMap, temp2, material, 0);
			Destroy(heightMap);

			blur.Apply(temp2);
			ReadBackIntensityMask(temp2);

			Graphics.Blit(temp2, temp, material, 1);

			RenderTexture.ReleaseTemporary(temp2);
			Destroy(material);
			
			Graphics.Blit(temp, intensityMask);
			RenderTexture.ReleaseTemporary(temp);
		}

		private void ReadBackIntensityMask(RenderTexture source)
		{
			int w = intensityMask.width;
			int h = intensityMask.height;

			intensityMaskData = new float[w * h + w + 1];

			RenderTexture.active = source;
			var gpuDownloadTex = new Texture2D(intensityMask.width, intensityMask.height, TextureFormat.RGBAFloat, false, true);
			gpuDownloadTex.ReadPixels(new Rect(0, 0, intensityMask.width, intensityMask.height), 0, 0);
			gpuDownloadTex.Apply();
			RenderTexture.active = null;

			int index = 0;

			for(int y = 0; y < h; ++y)
			{
				for(int x = 0; x < w; ++x)
				{
					intensityMaskData[index++] = gpuDownloadTex.GetPixel(x, y).r;
				}
			}

			Destroy(gpuDownloadTex);
		}

		private void CreateMaskRenderer()
		{
			var terrain = GetComponent<Terrain>();
			var terrainData = terrain.terrainData;

			var go = new GameObject("Shoreline Mask");
			go.hideFlags = HideFlags.DontSave;
			go.layer = 10;

			var mf = go.AddComponent<MeshFilter>();
			mf.sharedMesh = quadMesh;

			var material = new Material(maskDisplayShader);
			material.hideFlags = HideFlags.DontSave;
			material.SetTexture("_MainTex", intensityMask);

			var renderer = go.AddComponent<MeshRenderer>();
			renderer.sharedMaterial = material;

			go.transform.SetParent(transform);
			go.transform.localPosition = new Vector3(terrainData.size.x * 0.5f, 0.0f, terrainData.size.z * 0.5f);
			go.transform.localRotation = Quaternion.identity;
			go.transform.localScale = terrainData.size * (1.0f + extendShoreline);
		}

		private void CreateSpawnPointsFromSpectrum()
		{
			var terrain = GetComponent<Terrain>();
			var terrainData = terrain.terrainData;

			var gerstners = water.SpectrumResolver.SelectShorelineWaves(spawnPointsCount);

			spawnPointsCount = gerstners.Length;

			Vector2 centerPos = new Vector2(center.position.x, center.position.z);
			float terrainSize = terrainData.size.x * 0.5f;

			spawnPoints = new SpawnPoint[spawnPointsCount];

			for(int i=0; i<spawnPointsCount; ++i)
			{
				var gerstner = gerstners[i];

				Vector2 point = centerPos - gerstner.direction * terrainSize * (1.0f + extendShoreline);
				spawnPoints[i] = new SpawnPoint(point, gerstner.direction, gerstner.frequency, Mathf.Abs(gerstner.amplitude * 2.0f), gerstner.speed, water.TileSizes.x);
			}
		}

		private Vector2 FindClosestBeachDirection(TerrainCollider terrainCollider, Vector3 point)
		{
			RaycastHit hitInfo;
			Vector3 closestHit = new Vector3(float.NaN, float.NaN, float.NaN);
			float closestDistance = float.PositiveInfinity;

			for(int i=0; i<16; ++i)
			{
				float f = 2.0f * Mathf.PI * i / 16.0f;
                float s = Mathf.Sin(f);
				float c = Mathf.Cos(f);

				Ray ray = new Ray(point, new Vector3(s, 0.0f, c));

				if(terrainCollider.Raycast(ray, out hitInfo, 100000.0f))
				{
					float distance = hitInfo.distance;

					if(closestDistance > distance)
					{
						closestDistance = distance;
						closestHit = hitInfo.point;
					}
				}
			}

			if(!float.IsNaN(closestHit.x))
				return new Vector2(closestHit.x - point.x, closestHit.z - point.z).normalized;
			else
				return new Vector2(float.NaN, float.NaN);
		}

		static private void CreateQuadMesh()
		{
			quadMesh = new Mesh();
			quadMesh.name = "Shoreline Quad Mesh";
			quadMesh.hideFlags = HideFlags.DontSave;
			quadMesh.vertices = new Vector3[] { new Vector3(-0.5f, 0.0f, -0.5f), new Vector3(-0.5f, 0.0f, 0.5f), new Vector3(0.5f, 0.0f, 0.5f), new Vector3(0.5f, 0.0f, -0.5f) };
			quadMesh.uv = new Vector2[] { new Vector2(0.0f, 0.0f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 0.0f) };
			quadMesh.SetIndices(new int[] { 0, 1, 2, 3 }, MeshTopology.Quads, 0);
			quadMesh.UploadMeshData(true);
		}

		public float GetDepthAt(float x, float z)
		{
			x = (x + offsetX) * scaleX;
			z = (z + offsetZ) * scaleZ;

			int ix = Mathf.FloorToInt(x);
			int iz = Mathf.FloorToInt(z);

			if(ix >= width || ix < 0 || iz >= height || iz < 0)
				return 100.0f;

			x -= ix;
			z -= iz;

			int index = iz * width + ix;

			float a = intensityMaskData[index] * (1.0f - x) + intensityMaskData[index + 1] * x;
			float b = intensityMaskData[index + width] * (1.0f - x) + intensityMaskData[index + width + 1] * x;

			return a * (1.0f - z) + b * z;
		}

		class SpawnPoint
		{
			public Vector2 position;
			public Vector2 direction;
			public float frequency;
			public float amplitude;
			public float timeInterval;
			public float timeLeft;

			public SpawnPoint(Vector2 position, Vector2 direction, float frequency, float amplitude, float speed, float tileSize)
			{
				this.position = position;
				this.direction = direction;
				this.frequency = frequency;
				this.amplitude = amplitude;

				//this.timeInterval = 2.0f * Mathf.PI / speed;
				this.timeInterval = Mathf.PI / speed;
				//this.timeInterval = (2.0f * Mathf.PI / frequency) / speed;
				this.timeLeft = Random.Range(0.0f, timeInterval);
			}
		}
	}
}
