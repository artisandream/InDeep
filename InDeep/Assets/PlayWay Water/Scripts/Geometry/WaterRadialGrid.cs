﻿using System.Collections.Generic;
using UnityEngine;

namespace PlayWay.Water
{
	[System.Serializable]
	public class WaterRadialGrid : WaterPrimitiveBase
	{
		private float previousTargetVertexCount;

		override protected Mesh[] CreateMeshes(int vertexCount)
		{
			int dim = Mathf.RoundToInt(Mathf.Sqrt(vertexCount));
			int verticesX = Mathf.RoundToInt(dim * 0.78f);
			int verticesY = Mathf.RoundToInt((float)vertexCount / verticesX);

			List<Mesh> meshes = new List<Mesh>();

			List<Vector3> vertices = new List<Vector3>();
			List<int> indices = new List<int>();
			int vertexIndex = 0;
			int meshIndex = 0;

			Vector2[] vectors = new Vector2[verticesX];

			for(int x = 0; x < verticesX; ++x)
			{
				float fx = (float)x / (verticesX - 1) * 2.0f - 1.0f;
				fx *= Mathf.PI * 0.25f;

				vectors[x] = new Vector2(
						Mathf.Sin(fx),
						Mathf.Cos(fx)
					).normalized;
			}

			for(int y = 0; y < verticesY; ++y)
			{
				float fy = (float)y / (verticesY - 1);
				fy = 1.0f - Mathf.Cos(fy * Mathf.PI * 0.5f);

				for(int x = 0; x < verticesX; ++x)
				{
					Vector2 vector = vectors[x] * fy;
					
					vertices.Add(new Vector3(vector.x, 0.0f, vector.y));

					if(x != 0 && y != 0 && vertexIndex > verticesX)
					{
						indices.Add(vertexIndex);
						indices.Add(vertexIndex - verticesX);
						indices.Add(vertexIndex - verticesX - 1);
						indices.Add(vertexIndex - 1);

						indices.Add(vertexIndex - 1);
						indices.Add(vertexIndex - verticesX - 1);
						indices.Add(vertexIndex - verticesX);
						indices.Add(vertexIndex);
					}

					++vertexIndex;

					if(vertexIndex == 65000)
					{
						var mesh = CreateMesh(vertices.ToArray(), indices.ToArray(), string.Format("Radial Grid {0}x{1} - {2}", verticesX, verticesY, meshIndex.ToString("00")));
						meshes.Add(mesh);

						--x; --y;

						fy = (float)y / (verticesY - 1);
						fy = 1.0f - Mathf.Cos(fy * Mathf.PI * 0.5f);

						vertexIndex = 0;
						vertices.Clear();
						indices.Clear();

						++meshIndex;
					}
				}
			}

			if(vertexIndex != 0)
			{
				var mesh = CreateMesh(vertices.ToArray(), indices.ToArray(), string.Format("Projection Grid {0}x{1} - {2}", verticesX, verticesY, meshIndex.ToString("00")));
				meshes.Add(mesh);
			}

			return meshes.ToArray();
		}

		protected override Matrix4x4 GetMatrix(Camera camera)
		{
			Vector3 down = WaterUtilities.ViewportWaterPerpendicular(camera);
			Vector3 right = WaterUtilities.ViewportWaterRight(camera);
			
			Vector3 ld = WaterUtilities.RaycastPlane(camera, water.transform.position.y, (down - right));
			Vector3 rd = WaterUtilities.RaycastPlane(camera, water.transform.position.y, (down + right));
			
			Vector3 position = camera.transform.position;
			Vector3 scale;

			if(camera.orthographic)
				scale = new Vector3(camera.orthographicSize, 1.0f, camera.orthographicSize);
			else
				scale = new Vector3(camera.farClipPlane * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2.0f, 1.0f, camera.farClipPlane);

			float width = Mathf.Abs(rd.x - ld.x);
			float offset = Mathf.Min(ld.z, rd.z) - (width + water.SpectraRenderer.MaxHeight * 2) * scale.z / scale.x;

			Vector3 backward = camera.transform.forward;
			backward.y = 0.0f;
			backward.Normalize();

			float dp = Vector3.Dot(Vector3.down, camera.transform.forward);
			if(dp < -0.94f || dp > 0.94f)
			{
				backward = -camera.transform.up;
				backward.y = 0.0f;
				backward.Normalize();
				offset *= 2.0f;
			}

			return Matrix4x4.TRS(new Vector3(position.x, water.transform.position.y, position.z) + backward * offset, Quaternion.AngleAxis(Mathf.Atan2(backward.x, backward.z) * Mathf.Rad2Deg, Vector3.up), scale);
		}
	}
}
