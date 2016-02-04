using System.Collections;
using System.Collections.Generic;
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

		[SerializeField]
		private WaterGeometryType geometryType = WaterGeometryType.Auto;

		[SerializeField]
		private bool renderWaterDepth = true;

		[Tooltip("Water has a pretty smooth shape so it's often safe to render it's depth in a lower resolution than the rest of the scene. Although the default value is 1.0, you may probably safely use 0.5 and gain some minor performance boost. If you will encounter any artifacts in masking or image effects, set it back to 1.0.")]
		[Range(0.2f, 1.0f)]
		[SerializeField]
		private float baseEffectsQuality = 1.0f;

		[SerializeField]
		private bool renderVolumes = true;

		[SerializeField]
		private bool sharedCommandBuffers = false;

		[HideInInspector]
		[SerializeField]
		private int forcedVertexCount = 0;

		private RenderTexture waterDepthTexture;
		private RenderTexture subtractiveMaskTexture, additiveMaskTexture;
        private CommandBuffer depthRenderCommands;
		private CommandBuffer cleanUpCommands;
		private WaterCamera baseCamera;
        private Camera effectCamera;
		private Camera mainCamera;
		private Camera thisCamera;
		private Material depthMixerMaterial;
        private RenderTextureFormat waterDepthTextureFormat;
		private RenderTextureFormat blendedDepthTexturesFormat;
		private Vector2 localMapsOrigin;
		private float localMapsSizeInv;
		private int waterDepthTextureId;
		private int underwaterMaskId;
		private int additiveMaskId;
		private int subtractiveMaskId;
        private bool isEffectCamera;
		private bool effectsEnabled;
		private WaterVolumeProbe waterProbe;
		private IWaterImageEffect[] imageEffects;
		private Texture2D underwaterWhiteMask;

		static private Dictionary<Camera, WaterCamera> waterCamerasCache = new Dictionary<Camera, WaterCamera>();

		void Awake()
		{
			waterDepthTextureId = Shader.PropertyToID("_WaterDepthTexture");
			underwaterMaskId = Shader.PropertyToID("_UnderwaterMask");
			additiveMaskId = Shader.PropertyToID("_AdditiveMask");
			subtractiveMaskId = Shader.PropertyToID("_SubtractiveMask");

			if(SystemInfo.graphicsShaderLevel >= 40 && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth))
			{
				waterDepthTextureFormat = RenderTextureFormat.Depth;			// only > 4.0 shader targets can copy depth textures
				blendedDepthTexturesFormat = RenderTextureFormat.Depth;
			}
			else
			{
				if(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RFloat) && baseEffectsQuality > 0.2f)
					blendedDepthTexturesFormat = RenderTextureFormat.RFloat;
				else if(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RHalf))
					blendedDepthTexturesFormat = RenderTextureFormat.RHalf;
				else
					blendedDepthTexturesFormat = RenderTextureFormat.R8;

				if(SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth))
					waterDepthTextureFormat = RenderTextureFormat.Depth;
				else
					waterDepthTextureFormat = blendedDepthTexturesFormat;
			}
			
			OnValidate();
		}

		void OnEnable()
		{
			thisCamera = GetComponent<Camera>();

			if(!isEffectCamera)
			{
				float vfovrad = thisCamera.fieldOfView * Mathf.Deg2Rad;
				float nearPlaneSizeY = thisCamera.nearClipPlane * Mathf.Tan(vfovrad * 0.5f);
				waterProbe = WaterVolumeProbe.CreateProbe(transform, nearPlaneSizeY * 3.0f);

				imageEffects = GetComponents<IWaterImageEffect>();

				foreach(var imageEffect in imageEffects)
					imageEffect.OnWaterCameraEnabled();
			}
        }

		void OnDisable()
		{
			if(waterProbe != null)
			{
				waterProbe.gameObject.Destroy();
				waterProbe = null;
			}

			if(effectCamera != null)
			{
				effectCamera.gameObject.Destroy();
				effectCamera = null;
			}

			if(depthMixerMaterial != null)
			{
				depthMixerMaterial.Destroy();
				depthMixerMaterial = null;
			}
			
			DisableEffects();
		}

		void OnDestroy()
		{
			waterCamerasCache.Clear();
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

		public bool RenderVolumes
		{
			get { return renderVolumes; }
		}

		public Water ContainingWater
		{
			get { return baseCamera == null ? waterProbe.CurrentWater : baseCamera.ContainingWater; }
		}

		public WaterVolumeProbe WaterVolumeProbe
		{
			get { return waterProbe; }
		}

		public Camera MainCamera
		{
			get { return mainCamera; }
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

		public RenderTexture SubtractiveMask
		{
			get { return subtractiveMaskTexture; }
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
        }
		
		void OnPreCull()
		{
			SetFallbackUnderwaterMask();
			RenderWater();

			if(!effectsEnabled) return;

			SetLocalMapCoordinates();

			if(renderVolumes)
				RenderWaterMasks();

			if(renderWaterDepth)
				RenderWaterDepth();
			
			if(imageEffects != null && Application.isPlaying)
			{
				foreach(var imageEffect in imageEffects)
					imageEffect.OnWaterCameraPreCull();
			}
        }

		void OnPostRender()
		{
			if(waterDepthTexture != null)
			{
				RenderTexture.ReleaseTemporary(waterDepthTexture);
				waterDepthTexture = null;
			}
			
			if(subtractiveMaskTexture != null)
			{
				RenderTexture.ReleaseTemporary(subtractiveMaskTexture);
				subtractiveMaskTexture = null;
			}

			if(additiveMaskTexture != null)
			{
				RenderTexture.ReleaseTemporary(additiveMaskTexture);
				additiveMaskTexture = null;
            }

			var waters = WaterGlobals.Instance.Waters;
			int numWaterInstances = waters.Count;

			for(int waterIndex = 0; waterIndex < numWaterInstances; ++waterIndex)
				waters[waterIndex].Renderer.PostRender(thisCamera);
		}

		/// <summary>
		/// Fast and allocation free way to get a WaterCamera component attached to camera.
		/// </summary>
		/// <param name="camera"></param>
		/// <returns></returns>
		static public WaterCamera GetWaterCamera(Camera camera)
		{
			WaterCamera waterCamera;

			if(!waterCamerasCache.TryGetValue(camera, out waterCamera))
			{
				waterCamera = camera.GetComponent<WaterCamera>();

				if(waterCamera != null)
					waterCamerasCache[camera] = waterCamera;
				else
					waterCamerasCache[camera] = waterCamera = null;         // force null reference (Unity uses custom null operator)
			}

			return waterCamera;
        }

		private void RenderWater()
		{
			var waters = WaterGlobals.Instance.Waters;
			int numWaterInstances = waters.Count;
			
			for(int waterIndex=0; waterIndex<numWaterInstances; ++waterIndex)
				waters[waterIndex].Renderer.Render(thisCamera, geometryType);
		}

		private void RenderWaterDepth()
		{
			if(waterDepthTexture == null)
			{
				waterDepthTexture = RenderTexture.GetTemporary(Mathf.RoundToInt(thisCamera.pixelWidth * baseEffectsQuality), Mathf.RoundToInt(thisCamera.pixelHeight * baseEffectsQuality), waterDepthTextureFormat == RenderTextureFormat.Depth ? 32 : 16, waterDepthTextureFormat, RenderTextureReadWrite.Linear);
				waterDepthTexture.filterMode = baseEffectsQuality > 0.98f ? FilterMode.Point : FilterMode.Bilinear;			// no need to filter it, if it's of screen size
				waterDepthTexture.wrapMode = TextureWrapMode.Clamp;
			}

			var effectCamera = EffectsCamera;
			effectCamera.CopyFrom(thisCamera);
			effectCamera.GetComponent<WaterCamera>().enabled = true;
			effectCamera.renderingPath = RenderingPath.Forward;
			effectCamera.clearFlags = CameraClearFlags.SolidColor;
			effectCamera.depthTextureMode = DepthTextureMode.None;
			effectCamera.backgroundColor = Color.white;
			effectCamera.targetTexture = waterDepthTexture;
			effectCamera.cullingMask = (1 << WaterProjectSettings.Instance.WaterLayer);
			effectCamera.RenderWithShader(waterDepthShader, "CustomType");
			effectCamera.targetTexture = null;

			Shader.SetGlobalTexture(waterDepthTextureId, waterDepthTexture);
		}

		private void RenderWaterMasks()
		{
			var waters = WaterGlobals.Instance.Waters;
			int numWaters = waters.Count;

			bool hasSubtractiveVolumes = false;
			bool hasBoundingVolumes = false;
			bool hasFlatMasks = false;

			for(int i = 0; i < numWaters; ++i)
				waters[i].Renderer.OnSharedSubtractiveMaskRender(ref hasSubtractiveVolumes, ref hasBoundingVolumes, ref hasFlatMasks);

			var effectCamera = EffectsCamera;
			effectCamera.CopyFrom(thisCamera);
			effectCamera.GetComponent<WaterCamera>().enabled = false;
			effectCamera.renderingPath = RenderingPath.Forward;
			effectCamera.depthTextureMode = DepthTextureMode.None;

			if(hasSubtractiveVolumes || hasFlatMasks)
			{
				if(subtractiveMaskTexture == null)
				{
					subtractiveMaskTexture = RenderTexture.GetTemporary(thisCamera.pixelWidth, thisCamera.pixelHeight, 16, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
					subtractiveMaskTexture.filterMode = FilterMode.Point;
					subtractiveMaskTexture.wrapMode = TextureWrapMode.Clamp;
				}

				Graphics.SetRenderTarget(subtractiveMaskTexture);
				GL.Clear(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));

				effectCamera.targetTexture = subtractiveMaskTexture;

				if(hasSubtractiveVolumes)
				{
					effectCamera.clearFlags = CameraClearFlags.SolidColor;
					effectCamera.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);           // R = water key, G = far, B = near, A = unused
					effectCamera.cullingMask = (1 << WaterProjectSettings.Instance.WaterLayer);
					effectCamera.RenderWithShader(volumeBackShader, "");

					effectCamera.clearFlags = CameraClearFlags.Nothing;
					effectCamera.RenderWithShader(volumeFrontShader, "");
				}

				if(hasFlatMasks)
				{
					effectCamera.clearFlags = CameraClearFlags.Nothing;
					effectCamera.cullingMask = (1 << WaterProjectSettings.Instance.WaterTempLayer);
					effectCamera.Render();                  // may be merged with effectCamera.RenderWithShader(volumeFrontShader, "");
				}

				for(int i = 0; i < numWaters; ++i)
				{
					waters[i].WaterMaterial.SetTexture(subtractiveMaskId, subtractiveMaskTexture);
					waters[i].WaterBackMaterial.SetTexture(subtractiveMaskId, subtractiveMaskTexture);
				}
			}

			if(hasBoundingVolumes)
			{
				for(int i = 0; i < numWaters; ++i)
					waters[i].Renderer.OnSharedMaskAdditiveRender();

				if(additiveMaskTexture == null)
				{
					additiveMaskTexture = RenderTexture.GetTemporary(thisCamera.pixelWidth, thisCamera.pixelHeight, 16, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
					additiveMaskTexture.filterMode = FilterMode.Point;
					additiveMaskTexture.wrapMode = TextureWrapMode.Clamp;
				}

				Graphics.SetRenderTarget(additiveMaskTexture);
				GL.Clear(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));

				effectCamera.clearFlags = CameraClearFlags.SolidColor;
				effectCamera.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);           // R = water key, G = far, B = near, A = unused
				effectCamera.targetTexture = additiveMaskTexture;
				effectCamera.cullingMask = (1 << WaterProjectSettings.Instance.WaterLayer);
				effectCamera.RenderWithShader(volumeBackShader, "");

				effectCamera.clearFlags = CameraClearFlags.Nothing;
				effectCamera.RenderWithShader(volumeFrontShader, "");

				for(int i = 0; i < numWaters; ++i)
				{
					waters[i].WaterMaterial.SetTexture(additiveMaskId, additiveMaskTexture);
					waters[i].WaterBackMaterial.SetTexture(additiveMaskId, additiveMaskTexture);
				}
			}

			effectCamera.targetTexture = null;

			for(int i = 0; i < numWaters; ++i)
				waters[i].Renderer.OnSharedMaskPostRender();

			//Shader.SetGlobalTexture("_WaterMask", waterMasksTexture);
		}
		
		private void AddDepthRenderingCommands()
		{
			if(depthMixerMaterial == null)
			{
				depthMixerMaterial = new Material(depthBlitCopyShader);
				depthMixerMaterial.hideFlags = HideFlags.DontSave;
			}

			var camera = GetComponent<Camera>();

			if(((camera.depthTextureMode | DepthTextureMode.Depth) != 0 && renderWaterDepth) || renderVolumes)
			{
				int depthRT = Shader.PropertyToID("_CameraDepthTexture2");
				int waterlessDepthRT = Shader.PropertyToID("_WaterlessDepthTexture");

				depthRenderCommands = new CommandBuffer();
				depthRenderCommands.name = "Apply Water Depth";
				depthRenderCommands.GetTemporaryRT(waterlessDepthRT, camera.pixelWidth, camera.pixelHeight, blendedDepthTexturesFormat == RenderTextureFormat.Depth ? 32 : 0, FilterMode.Point, blendedDepthTexturesFormat, RenderTextureReadWrite.Linear);
				depthRenderCommands.Blit(BuiltinRenderTextureType.None, waterlessDepthRT, depthMixerMaterial, 0);

				depthRenderCommands.GetTemporaryRT(depthRT, camera.pixelWidth, camera.pixelHeight, blendedDepthTexturesFormat == RenderTextureFormat.Depth ? 32 : 0, FilterMode.Point, blendedDepthTexturesFormat, RenderTextureReadWrite.Linear);
				depthRenderCommands.SetRenderTarget(depthRT);
				depthRenderCommands.ClearRenderTarget(true, true, Color.white);
				depthRenderCommands.Blit(BuiltinRenderTextureType.None, depthRT, depthMixerMaterial, 1);
				depthRenderCommands.SetGlobalTexture("_CameraDepthTexture", depthRT);

				cleanUpCommands = new CommandBuffer();
				cleanUpCommands.name = "Clean Water Buffers";
				cleanUpCommands.ReleaseTemporaryRT(depthRT);
				cleanUpCommands.ReleaseTemporaryRT(waterlessDepthRT);

				camera.depthTextureMode |= DepthTextureMode.Depth;

				camera.AddCommandBuffer(camera.actualRenderingPath == RenderingPath.Forward ? CameraEvent.AfterDepthTexture : CameraEvent.BeforeLighting, depthRenderCommands);
				camera.AddCommandBuffer(CameraEvent.AfterEverything, cleanUpCommands);
			}
		}
		
		private void RemoveDepthRenderingCommands()
		{
			if(depthRenderCommands != null)
			{
				thisCamera.RemoveCommandBuffer(CameraEvent.AfterDepthTexture, depthRenderCommands);
				thisCamera.RemoveCommandBuffer(CameraEvent.BeforeLighting, depthRenderCommands);
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
			var depthCameraGo = new GameObject(name + " Water Effects Camera");
			depthCameraGo.hideFlags = HideFlags.HideAndDontSave;

			effectCamera = depthCameraGo.AddComponent<Camera>();
			effectCamera.enabled = false;

			var depthWaterCamera = depthCameraGo.AddComponent<WaterCamera>();
			depthWaterCamera.isEffectCamera = true;
			depthWaterCamera.mainCamera = thisCamera;
            depthWaterCamera.baseCamera = this;
            depthWaterCamera.waterDepthShader = waterDepthShader;
        }
		
		private void SetFallbackUnderwaterMask()
		{
			if(underwaterWhiteMask == null)
			{
				underwaterWhiteMask = new Texture2D(2, 2, TextureFormat.ARGB32, false);
				underwaterWhiteMask.hideFlags = HideFlags.DontSave;
				underwaterWhiteMask.SetPixel(0, 0, Color.black);
				underwaterWhiteMask.SetPixel(1, 0, Color.black);
				underwaterWhiteMask.SetPixel(0, 1, Color.black);
				underwaterWhiteMask.SetPixel(1, 1, Color.black);
				underwaterWhiteMask.Apply(false, true);
			}

			Shader.SetGlobalTexture(underwaterMaskId, underwaterWhiteMask);
		}

		private void SetLocalMapCoordinates()
		{
			int resolution = Mathf.NextPowerOfTwo(Mathf.Min(thisCamera.pixelWidth, thisCamera.pixelHeight)) >> 2;
			float maxHeight = 0.0f;
			float maxWaterLevel = 0.0f;

			var waters = WaterGlobals.Instance.Waters;
			int numWaterInstances = waters.Count;

			for(int waterIndex = 0; waterIndex < numWaterInstances; ++waterIndex)
			{
				var water = waters[waterIndex];
				maxHeight += water.MaxVerticalDisplacement;

				float posY = water.transform.position.y;
				if(maxWaterLevel < posY)
					maxWaterLevel = posY;
			}

			// place camera
			Vector3 thisCameraPosition = thisCamera.transform.position;
			Vector3 screenSpaceDown = WaterUtilities.ViewportWaterPerpendicular(thisCamera);
			Vector3 worldSpaceDown = thisCamera.transform.localToWorldMatrix * WaterUtilities.RaycastPlane(thisCamera, maxWaterLevel, screenSpaceDown);
			
			Vector3 effectCameraPosition = new Vector3(thisCameraPosition.x + worldSpaceDown.x * 2.0f, 0.0f, thisCameraPosition.z + worldSpaceDown.z * 2.0f);

			float size1 = thisCameraPosition.y * 6.0f;
			float size2 = maxHeight * 10.0f;
			float size3 = Vector3.Distance(effectCameraPosition, thisCameraPosition);
			float size = size1 > size2 ? (size1 > size3 ? size1 : size3) : (size2 > size3 ? size2 : size3);

			float halfPixelSize = size / resolution;
			localMapsOrigin = new Vector2((effectCameraPosition.x - size) + halfPixelSize, (effectCameraPosition.z - size) + halfPixelSize);
			localMapsSizeInv = 0.5f / size;

			Shader.SetGlobalVector("_LocalMapsCoords", new Vector4(-localMapsOrigin.x, -localMapsOrigin.y, localMapsSizeInv, 0.0f));
		}
    }
}
