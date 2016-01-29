using UnityEngine;
using System.Linq;

namespace PlayWay.Water
{
	/// <summary>
	/// Manages water primitives.
	/// </summary>
	[System.Serializable]
	public class WaterGeometry
	{
		[Tooltip("Geometry type used for display.")]
		[SerializeField]
		private Type type = Type.RadialGrid;

		[Tooltip("Water geometry vertex count at 1920x1080.")]
		[SerializeField]
		private int baseVertexCount = 118000;

		[Tooltip("Water geometry vertex count at 1920x1080 on systems with tesselation support. Set it a bit lower as tesselation will place additional, better distributed vertices in shader.")]
		[SerializeField]
		private int tesselatedBaseVertexCount = 12400;

		[SerializeField]
		private bool adaptToResolution = true;

		// sub-classes managing their primitive types

		[SerializeField]
		private WaterRadialGrid radialGrid;

		[SerializeField]
		private WaterProjectionGrid projectionGrid;

		[SerializeField]
		private WaterUniformGrid uniformGrid;

		[SerializeField]
		private Mesh[] customMeshes;

		private Water water;
		private Mesh[] customMeshesFiltered;
		private Type previousType;
		private int previousTargetVertexCount;
		private int thisSystemVertexCount;

		internal void OnEnable(Water water)
		{
			this.water = water;

			FilterCustomMeshes();
			UpdateVertexCount();

			if(radialGrid != null) radialGrid.OnEnable(water);
			if(projectionGrid != null) projectionGrid.OnEnable(water);
			if(uniformGrid != null) uniformGrid.OnEnable(water);
		}

		internal void OnDisable()
		{
			if(radialGrid != null) radialGrid.OnDisable();
			if(projectionGrid != null) projectionGrid.OnDisable();
			if(uniformGrid != null) uniformGrid.OnDisable();
		}

		public Type GeometryType
		{
			get { return type; }
		}

		public int VertexCount
		{
			get { return baseVertexCount; }
		}

		public bool AdaptToResolution
		{
			get { return adaptToResolution; }
		}

		public bool Triangular
		{
			get
			{
				if(type == Type.CustomMeshes)
				{
					if(customMeshesFiltered != null && customMeshesFiltered.Length != 0 && customMeshesFiltered[0].subMeshCount != 0)
						return customMeshesFiltered[0].GetTopology(0) == MeshTopology.Triangles;
					else
						return true;
				}
				else
					return false;
			}
		}

		public Mesh[] GetCustomMeshesDirect()
		{
			return customMeshes;
		}

		public void SetCustomMeshes(Mesh[] meshes)
		{
			customMeshes = meshes;
			FilterCustomMeshes();
        }

		internal void OnValidate(Water water)
		{
			if(radialGrid == null) radialGrid = new WaterRadialGrid();
			if(projectionGrid == null) projectionGrid = new WaterProjectionGrid();
			if(uniformGrid == null) uniformGrid = new WaterUniformGrid();

			// if geometry type changed
			if(previousType != type)
			{
				if(previousType == Type.RadialGrid) radialGrid.RemoveFromMaterial(water);
				if(previousType == Type.ProjectionGrid) projectionGrid.RemoveFromMaterial(water);
				if(previousType == Type.UniformGrid) uniformGrid.RemoveFromMaterial(water);

				if(type == Type.RadialGrid) radialGrid.AddToMaterial(water);
				if(type == Type.ProjectionGrid) projectionGrid.AddToMaterial(water);
				if(type == Type.UniformGrid) uniformGrid.AddToMaterial(water);

				previousType = type;
			}

			FilterCustomMeshes();
			UpdateVertexCount();

			if(previousTargetVertexCount != thisSystemVertexCount)
			{
				radialGrid.Dispose();
				uniformGrid.Dispose();
				projectionGrid.Dispose();
				previousTargetVertexCount = thisSystemVertexCount;
			}
		}

		internal void Update()
		{
			radialGrid.Update();
			projectionGrid.Update();
			uniformGrid.Update();
		}

		public Mesh[] GetTransformedMeshes(Camera camera, out Matrix4x4 matrix, WaterGeometryType geometryType, int vertexCount = 0)
		{
			if(vertexCount == 0)
			{
				if(adaptToResolution)
					vertexCount = Mathf.RoundToInt(thisSystemVertexCount * ((float)(camera.pixelWidth * camera.pixelHeight) / (1920 * 1080)));
				else
					vertexCount = thisSystemVertexCount;
			}

			switch(geometryType)
			{
				case WaterGeometryType.Auto:
				{
					switch(type)
					{
						case Type.RadialGrid: return radialGrid.GetTransformedMeshes(camera, out matrix, vertexCount);
						case Type.ProjectionGrid: return projectionGrid.GetTransformedMeshes(camera, out matrix, vertexCount);
						case Type.UniformGrid: return uniformGrid.GetTransformedMeshes(camera, out matrix, vertexCount);
						case Type.CustomMeshes: return GetTransformedCustomMeshes(camera, out matrix);
						default: throw new System.InvalidOperationException("Unknown water geometry type.");
					}
				}

				case WaterGeometryType.RadialGrid: return radialGrid.GetTransformedMeshes(camera, out matrix, vertexCount);
				case WaterGeometryType.ProjectionGrid: return projectionGrid.GetTransformedMeshes(camera, out matrix, vertexCount);
				case WaterGeometryType.UniformGrid: return uniformGrid.GetTransformedMeshes(camera, out matrix, vertexCount);
				default: throw new System.InvalidOperationException("Unknown water geometry type.");
			}
		}

		private Mesh[] GetTransformedCustomMeshes(Camera camera, out Matrix4x4 matrix)
		{
			matrix = water.transform.localToWorldMatrix;
			return customMeshesFiltered;
		}

		private void UpdateVertexCount()
		{
			thisSystemVertexCount = SystemInfo.supportsComputeShaders ?
				Mathf.Min(tesselatedBaseVertexCount, WaterQualitySettings.Instance.MaxTesselatedVertexCount) :
				Mathf.Min(baseVertexCount, WaterQualitySettings.Instance.MaxVertexCount);
		}

		private void FilterCustomMeshes()
		{
			customMeshesFiltered = customMeshes.Where(m => m != null).ToArray();
        }

		public enum Type
		{
			RadialGrid,
			ProjectionGrid,
			UniformGrid,
            CustomMeshes
		}
	}
}
