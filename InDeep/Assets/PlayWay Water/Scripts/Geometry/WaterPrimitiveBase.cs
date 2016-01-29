using System.Collections.Generic;
using UnityEngine;

namespace PlayWay.Water
{
	[System.Serializable]
	abstract public class WaterPrimitiveBase
	{
		protected Water water;
		protected Dictionary<int, CachedMeshSet> cache = new Dictionary<int, CachedMeshSet>();
		private List<int> keysToRemove;

		public void Dispose()
		{
			foreach(var cachedMeshSet in cache.Values)
			{
				foreach(var mesh in cachedMeshSet.meshes)
				{
					if(Application.isPlaying)
						Object.Destroy(mesh);
					else
						Object.DestroyImmediate(mesh);
				}
			}

			cache.Clear();
		}

		virtual internal void OnEnable(Water water)
		{
			this.water = water;
        }

		virtual internal void OnDisable()
		{
			Dispose();
		}

		virtual internal void AddToMaterial(Water water)
		{
			
		}

		virtual internal void RemoveFromMaterial(Water water)
		{
			
		}

		virtual public Mesh[] GetTransformedMeshes(Camera camera, out Matrix4x4 matrix, int vertexCount)
		{
			matrix = GetMatrix(camera);

			CachedMeshSet cachedMeshSet;
			int hash = vertexCount;

			if(!cache.TryGetValue(hash, out cachedMeshSet))
				cache[hash] = cachedMeshSet = new CachedMeshSet(CreateMeshes(vertexCount));
			else
				cachedMeshSet.Update();

			return cachedMeshSet.meshes;
		}

		internal void Update()
		{
			int currentFrame = Time.frameCount;

			if(keysToRemove == null)
				keysToRemove = new List<int>();
			
			foreach(var kv in cache)
			{
				if(currentFrame - kv.Value.lastFrameUsed > 3)
				{
					keysToRemove.Add(kv.Key);

					foreach(var mesh in kv.Value.meshes)
					{
						if(Application.isPlaying)
							Object.Destroy(mesh);
						else
							Object.DestroyImmediate(mesh);
					}
				}
			}

			foreach(int key in keysToRemove)
				cache.Remove(key);

			keysToRemove.Clear();
		}

		abstract protected Matrix4x4 GetMatrix(Camera camera);
		abstract protected Mesh[] CreateMeshes(int vertexCount);

		protected Mesh CreateMesh(Vector3[] vertices, int[] indices, string name)
		{
			var mesh = new Mesh();
			mesh.hideFlags = HideFlags.DontSave;
			mesh.name = name;
			mesh.vertices = vertices;
			mesh.SetIndices(indices, MeshTopology.Quads, 0);
			mesh.RecalculateBounds();
			mesh.UploadMeshData(true);

			return mesh;
		}

		protected class CachedMeshSet
		{
			public Mesh[] meshes;
			public int lastFrameUsed;

			public CachedMeshSet(Mesh[] meshes)
			{
				this.meshes = meshes;

				Update();
			}

			public void Update()
			{
				lastFrameUsed = Time.frameCount;
			}
		}
	}
}
