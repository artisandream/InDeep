using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlayWay.Water
{
	/// <summary>
	/// Ensures that shader collection rebuilding will be called when it's allowed by Unity.
	/// </summary>
	public class ShaderCollectionRebuilder
	{
		private Queue<ShaderCollection> collections = new Queue<ShaderCollection>();

		static private ShaderCollectionRebuilder instance;
		static public ShaderCollectionRebuilder Instance
		{
			get { return instance; }
		}

		public void Rebuild(ShaderCollection collection)
		{
			collections.Enqueue(collection);
		}

#if UNITY_EDITOR
		[InitializeOnLoadMethod]
		static public void StartRunning()
		{
			instance = new ShaderCollectionRebuilder();
			EditorApplication.update -= instance.OnUpdate;
			EditorApplication.update += instance.OnUpdate;
		}
		
		void OnUpdate()
		{
			while(collections.Count != 0)
			{
				var collection = collections.Dequeue();
				RebuildCollection(collection);
            }
		}

		private void RebuildCollection(ShaderCollection shaderCollection)
		{
			if(Application.isPlaying)
				return;

			shaderCollection.Clear();

			var transforms = Object.FindObjectsOfType<Transform>();

			foreach(var root in transforms)
			{
				if(root.parent == null)     // if that's really a root
				{
					var writers = root.GetComponentsInChildren<IShaderCollectionBuilder>(true);

					foreach(var writer in writers)
						writer.Write(shaderCollection);
				}
			}
		}
#endif
	}
}