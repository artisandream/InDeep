using UnityEngine;

namespace PlayWay.Water
{
	[RequireComponent(typeof(Camera))]
	[RequireComponent(typeof(WaterCamera))]
	public class UnderwaterIME : MonoBehaviour
	{
		[HideInInspector]
		[SerializeField]
		private Shader underwaterMaskShader;

		[HideInInspector]
		[SerializeField]
		private Shader imeShader;

		[HideInInspector]
		[SerializeField]
		private Shader noiseShader;

		[HideInInspector]
		[SerializeField]
		private Shader composeUnderwaterMaskShader;

		[SerializeField]
		private float waterDropsEffectDuration = 1.3f;

		[Range(0.0f, 0.4f)]
		[SerializeField]
		private float distortionIntensity = 0.06f;

		[SerializeField]
		private float distortionAnimationSpeed = 0.1f;

		[SerializeField]
		private Blur blur;

		[SerializeField]
		private bool underwaterAudioEffects = true;

		private Material maskMaterial;
		private Material imeMaterial;
		private Material noiseMaterial;
		private Material composeUnderwaterMaskMaterial;

		private Camera localCamera;
		private WaterCamera localWaterCamera;

		private AudioReverbFilter reverbFilter;
		private WaterDropsIME waterDropsIME;

		private float intensity = float.NaN;
		private float previousIntensity;
		private float previousVerticalDistance;
		private float startTime;
		private float duration;
		private float startValue, endValue;
		private bool renderUnderwaterMask;
		private bool effectEnabled = true;

		private WaterVolumeProbe waterProbe;

		void Awake()
		{
			waterProbe = WaterVolumeProbe.CreateProbe(transform);

			localCamera = GetComponent<Camera>();
			localWaterCamera = GetComponent<WaterCamera>();

			OnValidate();

			maskMaterial = new Material(underwaterMaskShader);
			maskMaterial.hideFlags = HideFlags.DontSave;

			imeMaterial = new Material(imeShader);
			imeMaterial.hideFlags = HideFlags.DontSave;

			noiseMaterial = new Material(noiseShader);
			noiseMaterial.hideFlags = HideFlags.DontSave;

			composeUnderwaterMaskMaterial = new Material(composeUnderwaterMaskShader);
			composeUnderwaterMaskMaterial.hideFlags = HideFlags.DontSave;

			reverbFilter = GetComponent<AudioReverbFilter>();
			waterDropsIME = GetComponent<WaterDropsIME>();

			if(reverbFilter == null && underwaterAudioEffects)
				reverbFilter = gameObject.AddComponent<AudioReverbFilter>();
		}

		public float Intensity
		{
			get { return intensity; }
		}

		public bool EffectEnabled
		{
			get { return effectEnabled; }
			set { effectEnabled = value; }
		}

		// Called by WaterCamera.cs, to update this effect when it's disabled
		public void OnWaterCameraPreCull()
		{
			if(waterProbe.CurrentWater == null || !effectEnabled)
			{
				enabled = false;
				UpdateTimers();
				return;
			}

			Vector3 position = transform.position;

			float vfovrad = localCamera.fieldOfView * Mathf.Deg2Rad;
			float nearPlaneSizeY = localCamera.nearClipPlane * Mathf.Tan(vfovrad * 0.5f);

			float waterLevel = waterProbe.CurrentWater.GetHeightAt(position.x, position.z, Mathf.Max(0.006f, 0.35f - Mathf.Abs(previousVerticalDistance) * 0.2f), 3);
			float verticalDistance = transform.position.y - waterLevel;

			if(verticalDistance - nearPlaneSizeY > 0.25f + waterProbe.CurrentWater.SpectraRenderer.MaxDisplacement)
			{
				enabled = false;
			}
			else if(verticalDistance + nearPlaneSizeY < -0.25f - waterProbe.CurrentWater.SpectraRenderer.MaxDisplacement)
			{
				enabled = true;
				renderUnderwaterMask = false;
			}
			else
			{
				enabled = true;
				renderUnderwaterMask = true;
			}

			float intensity = (-verticalDistance + nearPlaneSizeY) * 0.25f;

			SetEffectsIntensity(intensity);
			UpdateTimers();

			previousVerticalDistance = verticalDistance;
		}

		void OnDestroy()
		{
			Destroy(maskMaterial);
			Destroy(imeMaterial);
			if(blur != null) blur.Dispose();
		}

		void OnValidate()
		{
			if(underwaterMaskShader == null)
				underwaterMaskShader = Shader.Find("PlayWay Water/Underwater/Screen-Space Mask");

			if(imeShader == null)
				imeShader = Shader.Find("PlayWay Water/Underwater/Base IME");

			if(noiseShader == null)
				noiseShader = Shader.Find("PlayWay Water/Utilities/Noise");

			if(composeUnderwaterMaskShader == null)
				composeUnderwaterMaskShader = Shader.Find("PlayWay Water/Underwater/Compose Underwater Mask");

			if(blur != null)
				blur.Validate("PlayWay Water/Utilities/Blur (Underwater)");
		}

		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if(waterProbe.CurrentWater == null)
			{
				Graphics.Blit(source, destination);
				return;
			}

			source.filterMode = FilterMode.Bilinear;

			using(var underwaterMask = GetTemporaryUnderwaterMask())
			{
				RenderUnderwaterMask(underwaterMask);

				using(var temp1 = RenderTexturesCache.GetTemporary(Screen.width, Screen.height, 0, destination != null ? destination.format : source.format, true, false))
				{
					temp1.Texture.filterMode = FilterMode.Bilinear;
					temp1.Texture.wrapMode = TextureWrapMode.Clamp;

					RenderDepthScatter(underwaterMask, source, temp1);

					blur.BlurMaterial.SetTexture("_UnderwaterMask", underwaterMask);
					blur.Apply(temp1);

					RenderDistortions(temp1, destination);
				}
			}
		}

		private TemporaryRenderTexture GetTemporaryUnderwaterMask()
		{
			if(renderUnderwaterMask || WaterProjectSettings.Instance.WaterMasksEnabled)
				return RenderTexturesCache.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.R8, true, false);
			else
				return RenderTexturesCache.GetTemporary(4, 4, 0, RenderTextureFormat.R8, true, false);
		}

		private void RenderDistortionMap(RenderTexture target)
		{
			noiseMaterial.SetVector("_Offset", new Vector4(0.0f, 0.0f, Time.time * distortionAnimationSpeed, 0.0f));
			noiseMaterial.SetVector("_Period", new Vector4(4, 4, 4, 4));
			Graphics.Blit(null, target, noiseMaterial, 1);
		}

		private void RenderUnderwaterMask(RenderTexture underwaterMask)
		{
			var camera = localCamera;

			Graphics.SetRenderTarget(underwaterMask);

			if(renderUnderwaterMask)
			{
				maskMaterial.CopyPropertiesFromMaterial(waterProbe.CurrentWater.WaterMaterial);
				maskMaterial.SetVector("_ViewportDown", camera.worldToCameraMatrix.MultiplyVector(Vector3.down));
				maskMaterial.SetPass(0);

				GL.Clear(false, true, Color.white);

				Matrix4x4 matrix;
				var meshes = waterProbe.CurrentWater.Geometry.GetTransformedMeshes(camera, out matrix, WaterGeometryType.RadialGrid);

				// render meshes manually to avoid culling
				foreach(var mesh in meshes)
					Graphics.DrawMeshNow(mesh, matrix);
			}
			else
				GL.Clear(false, true, Color.black);

			if(WaterProjectSettings.Instance.WaterMasksEnabled)
				Graphics.Blit(null, composeUnderwaterMaskMaterial, 0);

			Graphics.SetRenderTarget(null);
		}

		private void RenderDepthScatter(RenderTexture underwaterMask, RenderTexture source, RenderTexture target)
		{
			imeMaterial.CopyPropertiesFromMaterial(waterProbe.CurrentWater.WaterMaterial);

			imeMaterial.SetTexture("_UnderwaterMask", underwaterMask);
			imeMaterial.SetColor("_AbsorptionColor", waterProbe.CurrentWater.UnderwaterAbsorptionColor);
			imeMaterial.SetMatrix("UNITY_MATRIX_VP_INVERSE", Matrix4x4.Inverse(localCamera.projectionMatrix * localCamera.worldToCameraMatrix));

			var sss = imeMaterial.GetVector("_SubsurfaceScatteringPack");
			sss.y = 1.0f;
			sss.z = 2.0f;
			imeMaterial.SetVector("_SubsurfaceScatteringPack", sss);

			Graphics.Blit(source, target, imeMaterial, 2);
		}

		private void RenderDistortions(RenderTexture source, RenderTexture target)
		{
			if(distortionIntensity > 0.0f)
			{
				var distortionTex = RenderTexture.GetTemporary(Screen.width / 4, Screen.height / 4, 0, RenderTextureFormat.ARGB32);
				RenderDistortionMap(distortionTex);

				imeMaterial.SetTexture("_DistortionTex", distortionTex);
				imeMaterial.SetFloat("_DistortionIntensity", distortionIntensity);
				Graphics.Blit(source, target, imeMaterial, 3);

				RenderTexture.ReleaseTemporary(distortionTex);
			}
			else
				Graphics.Blit(source, target);
		}

		private void SetEffectsIntensity(float intensity)
		{
			if(localCamera == null)          // start wasn't called yet
				return;

			intensity = Mathf.Clamp01(intensity);

			if(this.intensity == intensity)
				return;

			this.previousIntensity = this.intensity;
			this.intensity = intensity;

			if(reverbFilter != null && underwaterAudioEffects)
			{
				float reverbIntensity = intensity > 0.05f ? Mathf.Clamp01(intensity + 0.7f) : intensity;

				reverbFilter.dryLevel = -2000 * reverbIntensity;
				reverbFilter.room = -10000 * (1.0f - reverbIntensity);
				reverbFilter.roomHF = Mathf.Lerp(-10000, -4000, reverbIntensity);
				reverbFilter.decayTime = 1.6f * reverbIntensity;
				reverbFilter.decayHFRatio = 0.1f * reverbIntensity;
				reverbFilter.reflectionsLevel = -449.0f * reverbIntensity;
				reverbFilter.reverbLevel = 1500.0f * reverbIntensity;
				reverbFilter.reverbDelay = 0.0259f * reverbIntensity;
			}

			if(intensity <= 0.01f && previousIntensity > 0.01f)
			{
				startTime = Time.time;
				duration = 0.6f;

				if(waterDropsIME != null)
					startValue = waterDropsIME.Intensity;

				endValue = 0.06f;
			}
		}

		private void UpdateTimers()
		{
			float t = Mathf.Clamp01((Time.time - startTime) / duration);

			if(waterDropsIME != null)
				waterDropsIME.Intensity = Mathf.Lerp(startValue, endValue, t);

			if(t == 1.0f && endValue == 0.06f)
			{
				startTime = Time.time + 1.0f;
				duration = waterDropsEffectDuration;

				if(waterDropsIME != null)
					startValue = waterDropsIME.Intensity;

				endValue = 0.0f;
			}
		}
	}
}
