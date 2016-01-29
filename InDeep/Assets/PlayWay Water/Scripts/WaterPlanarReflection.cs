using UnityEngine;
using System.Collections.Generic;
using System;

namespace PlayWay.Water
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(Water))]
	[AddComponentMenu("Water/Planar Reflections", 1)]
	public class WaterPlanarReflection : MonoBehaviour, IWaterRenderAware
	{
		[SerializeField]
		private Camera reflectionCamera;

		[SerializeField]
		private bool reflectSkybox = true;

		[Range(1, 6)]
		[SerializeField]
		private int downsample = 2;

		[Range(1, 6)]
		[Tooltip("Allows you to use more rational resolution of planar reflections on screens with very high dpi. Planar reflections should be blurred anyway.")]
		[SerializeField]
		private int retinaDownsample = 3;

		[SerializeField]
		private LayerMask reflectionMask = int.MaxValue;

		[SerializeField]
		private Blur blur;

		[SerializeField]
		private Blur nearBlur;

		[Tooltip("Determines, if blur should be depth-based. Makes reflections sharp up-close and blurries them when viewed at a distance (that's what actually happens in real world). Draw-calls heavy.")]
		[SerializeField]
		private bool useDepthBlur;
		
		[SerializeField]
		private Shader depthBlurMapShader;

		[SerializeField]
		private float clipPlaneOffset = 0.07f;

		private Water water;
		private RenderTexture currentTarget;
		private bool systemSupportsHDR;
        private int finalDivider;
		private int reflectionTexProperty;

		private Dictionary<Camera, RenderTexture> temporaryTargets = new Dictionary<Camera, RenderTexture>();
		
		void Start()
		{
			reflectionTexProperty = Shader.PropertyToID("_PlanarReflectionTex");
			systemSupportsHDR = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf);

			OnValidate();
		}

		public bool ReflectSkybox
		{
			get { return reflectSkybox; }
			set { reflectSkybox = value; }
		}

		public LayerMask ReflectionMask
		{
			get { return reflectionMask; }
			set
			{
				if(reflectionMask == value)
					return;

				reflectionMask = value;

				if(reflectionCamera != null)
					reflectionCamera.cullingMask = reflectionMask;
			}
		}

		void OnDisable()
		{
			water.SetKeyword("_PLANAR_REFLECTIONS", false);
		}

		void OnEnable()
		{
			water = GetComponent<Water>();
			ValidateNow(water, WaterQualitySettings.Instance.CurrentQualityLevel);
		}

		void OnValidate()
		{
			if(depthBlurMapShader == null)
				depthBlurMapShader = Shader.Find("PlayWay Water/Utilities/PlanarReflectionBlurMap");

			if(nearBlur != null)
			{
				nearBlur.Validate("PlayWay Water/Utilities/Blur (Near)");
				blur.Validate("PlayWay Water/Utilities/Blur");
			}

			int finalDivider = Screen.dpi <= 220 ? downsample : retinaDownsample;

			if(this.finalDivider != finalDivider)
			{
				this.finalDivider = finalDivider;
				ClearRenderTextures();
			}

			if(reflectionCamera != null)
				ValidateReflectionCamera();
		}

		void OnDestroy()
		{
			if(blur != null)
				blur.Dispose();

			ClearRenderTextures();
		}

		void Update()
		{
			temporaryTargets.Clear();
		}

		public void OnWaterRender(Camera camera)
		{
			if(camera == reflectionCamera || !enabled || !camera.enabled)
				return;

			if(!temporaryTargets.TryGetValue(camera, out currentTarget))
			{
				RenderReflection(camera);

				var material = water.WaterMaterial;

				if(material != null)
					material.SetTexture(reflectionTexProperty, currentTarget);
			}
		}

		public void OnWaterPostRender(Camera camera)
		{
			RenderTexture renderTexture;

			if(temporaryTargets.TryGetValue(camera, out renderTexture))
			{
				temporaryTargets.Remove(camera);
				RenderTexture.ReleaseTemporary(renderTexture);
			}
		}

		private void RenderReflection(Camera camera)
		{
			if(!enabled)
				return;

			SetupReflectionCamera(camera);

			GL.invertCulling = true;
			reflectionCamera.Render();
			GL.invertCulling = false;

			ApplyBlur(camera);
		}

		private void ApplyBlur(Camera camera)
		{
			blur.Apply(reflectionCamera.targetTexture);
		}

		private void SetupReflectionCamera(Camera camera)
		{
			if(reflectionCamera == null)
			{
				var reflectionCameraGo = new GameObject(name + " Reflection Camera");
				reflectionCameraGo.transform.parent = transform;

				reflectionCamera = reflectionCameraGo.AddComponent<Camera>();
				ValidateReflectionCamera();
			}

			reflectionCamera.hdr = systemSupportsHDR && camera.hdr;
			reflectionCamera.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

			currentTarget = GetRenderTexture(camera.pixelWidth, camera.pixelHeight);
			reflectionCamera.targetTexture = currentTarget;
			temporaryTargets[camera] = currentTarget;

			Vector3 cameraEuler = camera.transform.eulerAngles;
			reflectionCamera.transform.eulerAngles = new Vector3(-cameraEuler.x, cameraEuler.y, cameraEuler.z);
			reflectionCamera.transform.position = camera.transform.position;

			Vector3 cameraPosition = camera.transform.position;
			cameraPosition.y = -cameraPosition.y;
			reflectionCamera.transform.position = cameraPosition;

			float d = -Vector3.Dot(Vector3.up, Vector3.zero) - clipPlaneOffset;
			Vector4 reflectionPlane = new Vector4(0, 1, 0, d);

			Matrix4x4 reflection = Matrix4x4.zero;
			reflection = CalculateReflectionMatrix(reflection, reflectionPlane);
			Vector3 newpos = reflection.MultiplyPoint(camera.transform.position);

			reflectionCamera.worldToCameraMatrix = camera.worldToCameraMatrix * reflection;

			Vector4 clipPlane = CameraSpacePlane(reflectionCamera, new Vector3(0, 0, 0), new Vector3(0, 1, 0), 1.0f);

			var matrix = camera.projectionMatrix;
			matrix = CalculateObliqueMatrix(matrix, clipPlane);
			reflectionCamera.projectionMatrix = matrix;

			reflectionCamera.transform.position = newpos;
			Vector3 cameraEulerB = camera.transform.eulerAngles;
			reflectionCamera.transform.eulerAngles = new Vector3(-cameraEulerB.x, cameraEulerB.y, cameraEulerB.z);

			reflectionCamera.clearFlags = reflectSkybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
		}

		private void ValidateReflectionCamera()
		{
			reflectionCamera.enabled = false;
			reflectionCamera.cullingMask = reflectionMask;
			reflectionCamera.renderingPath = RenderingPath.Forward;
			reflectionCamera.depthTextureMode = DepthTextureMode.None;
		}

		static Matrix4x4 CalculateReflectionMatrix(Matrix4x4 reflectionMat, Vector4 plane)
		{
			reflectionMat.m00 = (1.0f - 2.0f * plane[0] * plane[0]);
			reflectionMat.m01 = (-2.0f * plane[0] * plane[1]);
			reflectionMat.m02 = (-2.0f * plane[0] * plane[2]);
			reflectionMat.m03 = (-2.0f * plane[3] * plane[0]);

			reflectionMat.m10 = (-2.0f * plane[1] * plane[0]);
			reflectionMat.m11 = (1.0f - 2.0f * plane[1] * plane[1]);
			reflectionMat.m12 = (-2.0f * plane[1] * plane[2]);
			reflectionMat.m13 = (-2.0f * plane[3] * plane[1]);

			reflectionMat.m20 = (-2.0f * plane[2] * plane[0]);
			reflectionMat.m21 = (-2.0f * plane[2] * plane[1]);
			reflectionMat.m22 = (1.0f - 2.0f * plane[2] * plane[2]);
			reflectionMat.m23 = (-2.0f * plane[3] * plane[2]);

			reflectionMat.m30 = 0.0f;
			reflectionMat.m31 = 0.0f;
			reflectionMat.m32 = 0.0f;
			reflectionMat.m33 = 1.0f;

			return reflectionMat;
		}
		
		Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
		{
			Vector3 offsetPos = pos + normal * clipPlaneOffset;
			Matrix4x4 m = cam.worldToCameraMatrix;
			Vector3 cpos = m.MultiplyPoint(offsetPos);
			Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;

			return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
		}

		static Matrix4x4 CalculateObliqueMatrix(Matrix4x4 projection, Vector4 clipPlane)
		{
			Vector4 q = projection.inverse * new Vector4(Mathf.Sign(clipPlane.x), Mathf.Sign(clipPlane.y), 1.0f, 1.0f);

			Vector4 c = clipPlane * (2.0f / (Vector4.Dot(clipPlane, q)));
			projection[2] = c.x - projection[3];
			projection[6] = c.y - projection[7];
			projection[10] = c.z - projection[11];
			projection[14] = c.w - projection[15];

			return projection;
		}

		private RenderTexture GetRenderTexture(int width, int height)
		{
			int adaptedWidth = width / finalDivider;
			int adaptedHeight = height / finalDivider;

			var renderTexture = RenderTexture.GetTemporary(adaptedWidth, adaptedHeight, 16, reflectionCamera.hdr && systemSupportsHDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1);
			renderTexture.filterMode = FilterMode.Bilinear;
			
			return renderTexture;
		}

		private void ClearRenderTextures()
		{
			foreach(var kv in temporaryTargets)
				RenderTexture.ReleaseTemporary(kv.Value);

			temporaryTargets.Clear();
		}

		public void ValidateNow(Water water, WaterQualityLevel qualityLevel)
		{
			water.SetKeyword("_PLANAR_REFLECTIONS", enabled);
		}
	}
}