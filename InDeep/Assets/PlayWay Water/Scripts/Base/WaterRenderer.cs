using UnityEngine;

namespace PlayWay.Water
{
	/// <summary>
	/// Renders water.
	/// <seealso cref="Water.Renderer"/>
	/// </summary>
	[System.Serializable]
	public class WaterRenderer
	{
		private Water water;

		internal void OnEnable(Water water)
		{
			this.water = water;

			Camera.onPreCull -= OnSomeCameraPreCull;
			Camera.onPreCull += OnSomeCameraPreCull;

			Camera.onPostRender -= OnSomeCameraPostRender;
			Camera.onPostRender += OnSomeCameraPostRender;
		}

		internal void OnDisable()
		{
			Camera.onPreCull -= OnSomeCameraPreCull;
			Camera.onPostRender -= OnSomeCameraPostRender;
		}

		internal void OnValidate(Water water)
		{

		}
		
		public void Render(Camera camera, WaterGeometryType geometryType)
		{
			if(water == null || water.WaterMaterial == null || !water.isActiveAndEnabled)
				return;

			if((camera.cullingMask & (1 << water.gameObject.layer)) == 0)
				return;
			
			water.OnWaterRender(camera);

			var waterCamera = camera.GetComponent<WaterCamera>();

			Matrix4x4 matrix;
			var meshes = water.Geometry.GetTransformedMeshes(camera, out matrix, geometryType, waterCamera != null ? waterCamera.ForcedVertexCount : 0);

			for(int i = 0; i < meshes.Length; ++i)
				Graphics.DrawMesh(meshes[i], matrix, water.WaterMaterial, water.gameObject.layer, camera, 0, null, water.ShadowCastingMode, water.ReceiveShadows, water.transform);
		}

		private void OnSomeCameraPreCull(Camera camera)
		{
			var waterCamera = camera.GetComponent<WaterCamera>();

			if((waterCamera == null || !waterCamera.enabled) && !IsSceneViewCamera(camera))
				return;

			Render(camera, waterCamera != null ? waterCamera.GeometryType : WaterGeometryType.Auto);
		}

		private void OnSomeCameraPostRender(Camera camera)
		{
			var waterCamera = camera.GetComponent<WaterCamera>();

			if((waterCamera == null || !waterCamera.enabled) && !IsSceneViewCamera(camera))
				return;

			if(water != null)
				water.OnWaterPostRender(camera);
		}

		private bool IsSceneViewCamera(Camera camera)
		{
#if UNITY_EDITOR
			foreach(UnityEditor.SceneView sceneView in UnityEditor.SceneView.sceneViews)
			{
				if(sceneView.camera == camera)
				{
					Shader.SetGlobalTexture("_WaterlessDepthTexture", UnityEditor.EditorGUIUtility.whiteTexture);
					return true;
				}
			}
#endif

			return false;
		}
	}
}
