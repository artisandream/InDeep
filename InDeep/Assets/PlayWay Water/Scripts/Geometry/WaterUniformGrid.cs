using System.Collections.Generic;
using UnityEngine;

namespace PlayWay.Water
{
	[System.Serializable]
	public class WaterUniformGrid : WaterPrimitiveBase
	{
		override protected Mesh[] CreateMeshes(int vertexCount)
		{
			int dim = Mathf.RoundToInt(Mathf.Sqrt(vertexCount));
			List<Mesh> meshes = new List<Mesh>();
			List<Vector3> vertices = new List<Vector3>();
			List<int> indices = new List<int>();
			int vertexIndex = 0;
			int meshIndex = 0;
			
			for(int y = 0; y < dim; ++y)
			{
				float fy = (float)y / (dim - 1) * 2.0f - 1.0f;

				for(int x = 0; x < dim; ++x)
				{
					float fx = (float)x / (dim - 1) * 2.0f - 1.0f;

					vertices.Add(new Vector3(fx, 0.0f, fy));

					if(x != 0 && y != 0 && vertexIndex > dim)
					{
						indices.Add(vertexIndex);
						indices.Add(vertexIndex - dim);
						indices.Add(vertexIndex - dim - 1);
						indices.Add(vertexIndex - 1);

						indices.Add(vertexIndex - 1);
						indices.Add(vertexIndex - dim - 1);
						indices.Add(vertexIndex - dim);
						indices.Add(vertexIndex);
					}

					++vertexIndex;

					if(vertexIndex == 65000)
					{
						var mesh = CreateMesh(vertices.ToArray(), indices.ToArray(), string.Format("Uniform Grid {0}x{1} - {2}", dim, dim, meshIndex.ToString("00")));
						meshes.Add(mesh);

						--x; --y;

						fy = (float)y / (dim - 1) * 2.0f - 1.0f;

						vertexIndex = 0;
						vertices.Clear();
						indices.Clear();

						++meshIndex;
					}
				}
			}

			if(vertexIndex != 0)
			{
				var mesh = CreateMesh(vertices.ToArray(), indices.ToArray(), string.Format("Uniform Grid {0}x{1} - {2}", dim, dim, meshIndex.ToString("00")));
				meshes.Add(mesh);
			}

			return meshes.ToArray();
		}

		protected override Matrix4x4 GetMatrix(Camera camera)
		{
			Vector3 position = camera.transform.position;
			Vector3 scale = camera.orthographic ? new Vector3(camera.orthographicSize + water.SpectraRenderer.MaxDisplacement, 1.0f, camera.orthographicSize + water.SpectraRenderer.MaxDisplacement) : new Vector3(camera.farClipPlane * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad), 1.0f, camera.farClipPlane);
			return Matrix4x4.TRS(new Vector3(position.x, water.transform.position.y, position.z), Quaternion.identity, scale);
		}
	}
}
