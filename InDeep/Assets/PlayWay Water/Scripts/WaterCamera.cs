using UnityEngine;
using UnityEngine.Rendering;

namespace PlayWay.Water
{
	/// <summary>
	/// Each camera supposed to see water needs this component attached. Renders all camera-specific maps for the water:
	/// <list type="bullet">
	/// <item>Depth Maps</item>
	/// <item>Displaced water info map</item>
	/// <item>Volume maps</item>
	/// </list>
	/// </summary>
	[ExecuteInEditMode]
	public class WaterCamera : MonoBehaviour
	{
		[HideInInspector]
		[SerializeField]
		private Shader depthBlitCopyShader;

		[HideInInspector]
		[SerializeField]
		private Shader waterDepthShader;

		[HideInInspector]
		[SerializeField]
		private Shader volumeFrontShader;

		[HideInInspector]
		[SerializeField]
		private Shader volumeBackShader;

		[HideInInspector]
		[SerializeField]
		private Shader waterInfoShader;

		[SerializeField]
		private WaterGeometryType geometryType = WaterGeometryType.Auto;

		[SerializeField]
		private bool renderWaterDepth = true;

		[SerializeField]
		private bool renderVolumes = true;

		[SerializeField]
		private bool sharedCommandBuffers = false;

		[HideInInspector]
		[SerializeField]
		private int forcedVertexCount = 0;

		private RenderTexture waterDepthTexture;
		private RenderTexture waterMaskTexture;
		private RenderTexture waterInfoTexture;
        private CommandBuffer depthRenderCommands;
		private CommandBuffer cleanUpCommands;
        private Camera effectCamera;
		private Camera sceneCamera;
		private Camera thisCamera;
		private Material depthMixerMaterial;
        private RenderTextureFormat depthTexturesFormat;
		private Vector2 localMapsOrigin;
		private float localMapsSizeInv;
		private int waterDepthTextureId;
		private int waterMaskId;
		private bool isEffectCamera;
		private bool effectsEnabled;
		private UnderwaterIME underwaterIME;

		void OnEnable()
		{
			thisCamera = GetComponent<Camera>();

			waterDepthTextureId = Shader.PropertyToID("_WaterDepthTexture");
			waterMaskId = Shader.PropertyToID("_WaterMask");

			depthTexturesFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat) ? RenderTextureFormat.RFloat : RenderTextureFormat.RHalf;
			
			underwaterIME = GetComponent<UnderwaterIME>();
		}

		void OnDisable()
		{
			if(effectCamera != null)
			{
				effectCamera.gameObject.Destroy();
				effectCamera = null;
			}

			if(sceneCamera != null)
			{
				sceneCamera.gameObject.Destroy();
				sceneCamera = null;
            }

			if(depthMixerMaterial != null)
			{
				depthMixerMaterial.Destroy();
				depthMixerMaterial = null;
			}

			DisableEffects();
		}

		public bool IsEffectCamera
		{
			get { return isEffectCamera; }
		}

		public WaterGeometryType GeometryType
		{
			get { return geometryType; }
			set { geometryType = value; }
		}

		public Vector2 LocalMapsOrigin
		{
			get { return localMapsOrigin; }
		}
		
		public float LocalMapsSizeInv
		{
			get { return localMapsSizeInv; }
		}

		public int ForcedVertexCount
		{
			get { return forcedVertexCount; }
		}

		/// <summary>
		/// Ready to render alternative camera for effects.
		/// </summary>
		public Camera EffectsCamera
		{
			get
			{
				if(!isEffectCamera && effectCamera == null)
					CreateEffectsCamera();

				return effectCamera;
			}
		}

		void Update()
		{
			if(!effectsEnabled)
			{
				if(IsWaterPossiblyVisible())
					EnableEffects();
			}
			else if(!IsWaterPossiblyVisible())
				DisableEffects();
		}
		
		void OnValidate()
		{
			if(depthBlitCopyShader == null)
				depthBlitCopyShader = Shader.Find("PlayWay Water/Depth/CopyMix");

			if(waterDepthShader == null)
				waterDepthShader = Shader.Find("PlayWay Water/Depth/Water Depth");

			if(volumeFrontShader == null)
				volumeFrontShader = Shader.Find("PlayWay Water/Volumes/Front");

			if(volumeBackShader == null)
				volumeBackShader = Shader.Find("PlayWay Water/Volumes/Back");

			if(waterInfoShader == null)
				waterInfoShader = Shader.Find("PlayWay Water/Utility/Info");
        }
		
		void OnPreCull()
		{
			if(!effectsEnabled) return;

			if(renderWaterDepth)
				RenderWaterDepth();
			
			if(renderVolumes && Application.isPlaying)
			{
				RenderWaterHeightData();
				RenderWaterVolumeSubtractors();
			}

			if(underwaterIME != null && Application.isPlaying)
				underwaterIME.OnWaterCameraPreCull();
        }

		void OnPostRender()
		{
			if(waterDepthTexture != null)
			{
				RenderTexture.ReleaseTemporary(waterDepthTexture);
				waterDepthTexture = null;
			}

			if(waterMaskTexture != null)
			{
				RenderTexture.ReleaseTemporary(waterMaskTexture);
				waterMaskTexture = null;
			}

			if(waterInfoTexture != null)
			{
				RenderTexture.ReleaseTemporary(waterInfoTexture);
				waterInfoTexture = null;
            }
        }

		private void RenderWaterDepth()
		{
			waterDepthTexture = RenderTexture.GetTemporary(thisCamera.pixelWidth, thisCamera.pixelHeight, 16, depthTexturesFormat, RenderTextureReadWrite.Linear);

			if(effectCamera == null)
				CreateEffectsCamera();

			effectCamera.CopyFrom(thisCamera);
			effectCamera.GetComponent<WaterCamera>().enabled = true;
			effectCamera.renderingPath = RenderingPath.Forward;
			effectCamera.clearFlags = CameraClearFlags.SolidColor;
			effectCamera.depthTextureMode = DepthTextureMode.None;
			effectCamera.backgroundColor = Color.white;
			effectCamera.targetTexture = waterDepthTexture;
			effectCamera.cullingMask = (1 << 4);
			effectCamera.RenderWithShader(waterDepthShader, "CustomType");

			Shader.SetGlobalTexture(waterDepthTextureId, waterDepthTexture);
		}

		private void RenderWaterHeightData()
		{
			int resolution = Mathf.NextPowerOfTwo(Mathf.Min(thisCamera.pixelWidth, thisCamera.pixelHeight)) / 4;

			waterInfoTexture = RenderTexture.GetTemporary(resolution, resolution, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear, 1);
			waterInfoTexture.filterMode = FilterMode.Bilinear;
			waterInfoTexture.wrapMode = TextureWrapMode.Clamp;

			if(effectCamera == null)
				CreateEffectsCamera();

			effectCamera.CopyFrom(thisCamera);

			float maxHeight = 0.0f;
			float maxWaterLevel = 0.0f;

			foreach(var water in WaterGlobals.Instance.Waters)
			{
				maxHeight += water.SpectraRenderer.MaxHeight;

				float posY = water.transform.position.y;
				if(maxWaterLevel < posY)
					maxWaterLevel = posY;
			}

			// place camera
			Vector3 thisCameraPosition = thisCamera.transform.position;
			Vector3 screenSpaceDown = WaterUtilities.ViewportWaterPerpendicular(thisCamera);
			Vector3 worldSpaceDown = thisCamera.transform.localToWorldMatrix * WaterUtilities.RaycastPlane(thisCamera, maxWaterLevel, screenSpaceDown);
			worldSpaceDown.y = 0.0f;

			Vector3 effectCameraPosition = new Vector3(thisCameraPosition.x, 0.0f, thisCameraPosition.z) + worldSpaceDown * 2.0f;

			effectCamera.transform.position = new Vector3(effectCameraPosition.x, maxWaterLevel + effectCamera.farClipPlane * 0.5f, effectCameraPosition.z);
			effectCamera.transform.LookAt(new Vector3(effectCameraPosition.x, -1.0f, effectCameraPosition.z), Vector3.forward);
			effectCamera.orthographic = true;
			//effectCamera.orthographicSize = 400.0f;
			effectCamera.orthographicSize = Mathf.Max(thisCameraPosition.y * 2.0f, maxHeight * 10.0f, Vector3.Distance(effectCameraPosition, thisCameraPosition));

			var waterCamera = effectCamera.GetComponent<WaterCamera>();
			waterCamera.geometryType = WaterGeometryType.UniformGrid;
			waterCamera.forcedVertexCount = resolution * resolution - 20;

			// setup and render
			effectCamera.GetComponent<WaterCamera>().enabled = true;
			effectCamera.renderingPath = RenderingPath.Forward;
			effectCamera.clearFlags = CameraClearFlags.SolidColor;
			effectCamera.depthTextureMode = DepthTextureMode.None;
			effectCamera.backgroundColor = Color.white;
			effectCamera.targetTexture = waterInfoTexture;
			effectCamera.cullingMask = (1 << 4);
			effectCamera.RenderWithShader(waterInfoShader, "CustomType");
			
			waterCamera.geometryType = WaterGeometryType.Auto;
			waterCamera.forcedVertexCount = 0;

			float halfPixelSize = effectCamera.orthographicSize / resolution;
			localMapsOrigin = new Vector2((effectCameraPosition.x - effectCamera.orthographicSize) + halfPixelSize, (effectCameraPosition.z - effectCamera.orthographicSize) + halfPixelSize);
			localMapsSizeInv = 0.5f / (effectCamera.orthographicSize);

			Shader.SetGlobalTexture("_LocalHeightData", waterInfoTexture);
			Shader.SetGlobalVector("_LocalMapsCoords", new Vector4(-localMapsOrigin.x, -localMapsOrigin.y, localMapsSizeInv, 0.0f));
		}

		private void RenderWaterVolumeSubtractors()
		{
			var projectSettings = WaterProjectSettings.Instance;
			var volumeFrontTexture = RenderTexture.GetTemporary(thisCamera.pixelWidth, thisCamera.pixelHeight, 16, SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ? RenderTextureFormat.ARGBFloat : RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, 1);
			waterMaskTexture = RenderTexture.GetTemporary(thisCamera.pixelWidth, thisCamera.pixelHeight, 16, SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ? RenderTextureFormat.ARGBFloat : RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear, 1);
			
			if(effectCamera == null)
				CreateEffectsCamera();

			//Shader.SetGlobalMatrix("UNITY_MATRIX_VP_INVERSE", (camera.projectionMatrix * camera.worldToCameraMatrix).inverse);

			effectCamera.GetComponent<WaterCamera>().enabled = false;
			effectCamera.CopyFrom(thisCamera);
			effectCamera.renderingPath = RenderingPath.Forward;
			effectCamera.clearFlags = CameraClearFlags.SolidColor;
			effectCamera.depthTextureMode = DepthTextureMode.None;
			effectCamera.backgroundColor = new Color(0.0f, 0.0f, 0.5f, 0.0f);
			effectCamera.targetTexture = volumeFrontTexture;
			effectCamera.cullingMask = (1 << projectSettings.WaterVolumesLayer);
			effectCamera.RenderWithShader(volumeFrontShader, "CustomType");

			Graphics.SetRenderTarget(waterMaskTexture);
			GL.Clear(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f), 0.0f);

			Shader.SetGlobalTexture("_VolumesFrontDepth", volumeFrontTexture);
			effectCamera.clearFlags = CameraClearFlags.Nothing;
			effectCamera.targetTexture = waterMaskTexture;
			effectCamera.RenderWithShader(volumeBackShader, "CustomType");

			RenderTexture.ReleaseTemporary(volumeFrontTexture);

			if(projectSettings.WaterMasksEnabled)
			{
				effectCamera.cullingMask = (1 << projectSettings.WaterMasksLayer);
				effectCamera.Render();
			}

			foreach(var water in WaterGlobals.Instance.Waters)
			{
				water.WaterMaterial.SetTexture(waterMaskId, waterMaskTexture);
				water.WaterVolumeMaterial.SetTexture(waterMaskId, waterMaskTexture);
			}
			
			Shader.SetGlobalTexture(waterMaskId, waterMaskTexture);
        }
		
		private void AddDepthRenderingCommands()
		{
			depthMixerMaterial = new Material(depthBlitCopyShader);
			depthMixerMaterial.hideFlags = HideFlags.DontSave;

			var camera = GetComponent<Camera>();

			if(((camera.depthTextureMode | DepthTextureMode.Depth) != 0 && renderWaterDepth) || renderVolumes)
			{
				int depthRT = Shader.PropertyToID("_CameraDepthTexture2");
				int waterlessDepthRT = Shader.PropertyToID("_WaterlessDepthTexture");

				depthRenderCommands = new CommandBuffer();
				depthRenderCommands.name = "Apply Water Depth";
				depthRenderCommands.GetTemporaryRT(waterlessDepthRT, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, depthTexturesFormat, RenderTextureReadWrite.Linear);
				depthRenderCommands.Blit(BuiltinRenderTextureType.None, waterlessDepthRT, depthMixerMaterial, 0);

				depthRenderCommands.GetTemporaryRT(depthRT, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Point, depthTexturesFormat, RenderTextureReadWrite.Linear);
				depthRenderCommands.SetRenderTarget(depthRT);
				depthRenderCommands.ClearRenderTarget(true, true, Color.white);
				depthRenderCommands.Blit(BuiltinRenderTextureType.None, depthRT, depthMixerMaterial, 1);
				depthRenderCommands.SetGlobalTexture("_CameraDepthTexture", depthRT);

				cleanUpCommands = new CommandBuffer();
				cleanUpCommands.name = "Clean Water Buffers";
				cleanUpCommands.ReleaseTemporaryRT(depthRT);
				cleanUpCommands.ReleaseTemporaryRT(waterlessDepthRT);

				camera.depthTextureMode |= DepthTextureMode.Depth;

				camera.AddCommandBuffer(camera.actualRenderingPath == RenderingPath.Forward ? CameraEvent.AfterDepthTexture : CameraEvent.AfterLighting, depthRenderCommands);
				camera.AddCommandBuffer(CameraEvent.AfterEverything, cleanUpCommands);
			}
		}

		private void RemoveDepthRenderingCommands()
		{
			if(depthRenderCommands != null)
			{
				thisCamera.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, depthRenderCommands);
				thisCamera.RemoveCommandBuffer(CameraEvent.AfterLighting, depthRenderCommands);
				depthRenderCommands.Dispose();
				depthRenderCommands = null;
            }

			if(cleanUpCommands != null)
			{
				thisCamera.RemoveCommandBuffer(CameraEvent.AfterEverything, cleanUpCommands);
				cleanUpCommands.Dispose();
				cleanUpCommands = null;
            }

			if(!sharedCommandBuffers)
				thisCamera.RemoveAllCommandBuffers();
        }

		private void EnableEffects()
		{
			if(isEffectCamera)
				return;

			effectsEnabled = true;

			AddDepthRenderingCommands();
		}

		private void DisableEffects()
		{
			effectsEnabled = false;

			RemoveDepthRenderingCommands();
		}
		
		private bool IsWaterPossiblyVisible()
		{
#if UNITY_EDITOR
			if(!Application.isPlaying)
				return true;
#endif

			var waters = WaterGlobals.Instance.Waters;

			return waters.Count != 0;
		}

		private void CreateEffectsCamera()
		{
			var depthCameraGo = new GameObject("Water Depth Camera");
			depthCameraGo.hideFlags = HideFlags.HideAndDontSave;

			effectCamera = depthCameraGo.AddComponent<Camera>();
			effectCamera.enabled = false;

			var depthWaterCamera = depthCameraGo.AddComponent<WaterCamera>();
			depthWaterCamera.isEffectCamera = true;
            depthWaterCamera.waterDepthShader = waterDepthShader;
        }

		private void CreateSceneCamera()
		{
			var depthCameraGo = new GameObject("Water Scene Camera");
			depthCameraGo.hideFlags = HideFlags.HideAndDontSave;

			sceneCamera = depthCameraGo.AddComponent<Camera>();
			sceneCamera.enabled = false;
		}
	}
}
