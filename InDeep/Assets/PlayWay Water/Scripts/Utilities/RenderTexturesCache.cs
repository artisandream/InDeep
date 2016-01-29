using System.Collections.Generic;
using UnityEngine;

namespace PlayWay.Water
{
	/// <summary>
	/// Alternative for RenderTexture.GetTemporary with UAV textures support and no allocations.
	/// </summary>
	public class RenderTexturesCache
	{
		static private Dictionary<ulong, RenderTexturesCache> cache = new Dictionary<ulong, RenderTexturesCache>();

		private Queue<RenderTexture> renderTextures;
		private int lastFrameAllUsed;

		private ulong hash;
		private int width, height, depthBuffer;
		private RenderTextureFormat format;
		private bool linear, uav;

		public RenderTexturesCache(ulong hash, int width, int height, int depthBuffer, RenderTextureFormat format, bool linear, bool uav)
		{
			this.hash = hash;
			this.width = width;
			this.height = height;
			this.depthBuffer = depthBuffer;
			this.format = format;
			this.linear = linear;
			this.uav = uav;
			this.renderTextures = new Queue<RenderTexture>();
		}

		static public RenderTexturesCache GetCache(int width, int height, int depthBuffer, RenderTextureFormat format, bool linear, bool uav)
		{
			RenderTexturesUpdater.EnsureInstance();

			ulong hash = 0;

			hash |= (uint)width;
			hash |= ((uint)height << 16);
			hash |= ((uint)depthBuffer << 29);        // >> 3 << 32
			hash |= ((linear ? 1UL : 0UL) << 32);
			hash |= ((uav ? 1UL : 0UL) << 33);
			hash |= ((ulong)format << 34);
			
			RenderTexturesCache renderTexturesCache;

			if(!cache.TryGetValue(hash, out renderTexturesCache))
				cache[hash] = renderTexturesCache = new RenderTexturesCache(hash, (int)width, (int)height, (int)depthBuffer, format, linear, uav);

			return renderTexturesCache;
		}

		static public TemporaryRenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format, bool linear, bool uav)
		{
			return GetCache(width, height, depthBuffer, format, linear, uav).GetTemporary();
		}

		public TemporaryRenderTexture GetTemporary()
		{
			return new TemporaryRenderTexture(this);
		}

		public RenderTexture GetTemporaryDirect()
		{
			RenderTexture renderTexture;

			if(renderTextures.Count == 0)
			{
				renderTexture = new RenderTexture(width, height, depthBuffer, format, linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
				renderTexture.hideFlags = HideFlags.DontSave;
				renderTexture.name = "Temporary#" + hash;
				renderTexture.filterMode = FilterMode.Point;
				renderTexture.anisoLevel = 1;
				renderTexture.wrapMode = TextureWrapMode.Repeat;

				if(uav)
					renderTexture.enableRandomWrite = true;
			}
			else
				renderTexture = renderTextures.Dequeue();

			if(uav && !renderTexture.IsCreated())
				renderTexture.Create();

			if(renderTextures.Count == 0)
				lastFrameAllUsed = Time.frameCount;

			return renderTexture;
		}

		public void ReleaseTemporaryDirect(RenderTexture renderTexture)
		{
			renderTextures.Enqueue(renderTexture);
		}

		internal void Update(int frame)
		{
			if(frame - lastFrameAllUsed > 3 && renderTextures.Count != 0)
			{
				var renderTexture = renderTextures.Dequeue();
				Object.Destroy(renderTexture);
			}
		}

		public class RenderTexturesUpdater : MonoBehaviour
		{
			static private RenderTexturesUpdater instance;

			static public void EnsureInstance()
			{
				if(instance == null)
				{
					var go = new GameObject("Water.RenderTexturesCache");
					go.hideFlags = HideFlags.HideAndDontSave;
					DontDestroyOnLoad(go);
					instance = go.AddComponent<RenderTexturesUpdater>();
				}
			}

			public RenderTexture GetTemporary(int width, int height, int depthBuffer, RenderTextureFormat format, bool linear, bool uav)
			{
				ulong hash = 0;

				hash |= (uint)width;
				hash |= ((uint)height << 16);
				hash |= ((uint)depthBuffer << 29);        // >> 3 << 32
				hash |= ((linear ? 1UL : 0UL) << 32);
				hash |= ((uav ? 1UL : 0UL) << 33);
				hash |= ((ulong)format << 34);

				RenderTexturesCache renderTexturesCache;

				if(!cache.TryGetValue(hash, out renderTexturesCache))
					cache[hash] = renderTexturesCache = new RenderTexturesCache(hash, (int)width, (int)height, (int)depthBuffer, format, linear, uav);

				return renderTexturesCache.GetTemporary();
			}

			public void ReleaseTemporary(RenderTexture renderTexture)
			{
				ulong hash = 0;

				hash |= (uint)renderTexture.width;
				hash |= ((uint)renderTexture.height << 16);
				hash |= ((uint)renderTexture.depth << 29);        // >> 3 << 32
				hash |= ((renderTexture.sRGB ? 0UL : 1UL) << 32);
				hash |= ((renderTexture.enableRandomWrite ? 1UL : 0UL) << 33);
				hash |= ((ulong)renderTexture.format << 34);

				RenderTexturesCache renderTexturesCache;

				if(!cache.TryGetValue(hash, out renderTexturesCache))
					cache[hash] = renderTexturesCache = new RenderTexturesCache(hash, renderTexture.width, renderTexture.height, renderTexture.depth, renderTexture.format, !renderTexture.sRGB, renderTexture.enableRandomWrite);

				renderTexturesCache.ReleaseTemporaryDirect(renderTexture);
			}

			void Update()
			{
				int frame = Time.frameCount;

				foreach(var textures in cache.Values)
					textures.Update(frame);
			}
		}
	}
}