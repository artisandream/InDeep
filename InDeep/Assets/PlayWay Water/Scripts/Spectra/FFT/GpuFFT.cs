using System.Collections.Generic;
using UnityEngine;

namespace PlayWay.Water
{
	abstract public class GpuFFT
	{
		private Texture2D butterfly;
		
		protected RenderTexturesCache renderTexturesSet;

		/// <summary>
		/// Real-valued output.
		/// </summary>
		protected RenderTexture realOutput;

		protected int resolution;
		protected int numButterflies;
		protected int numButterfliesPow2;
		protected bool twoChannels;

		private bool highPrecision;
		private bool usesUAV;

		public GpuFFT(int resolution, bool highPrecision, bool twoChannels, bool usesUAV)
		{
			this.resolution = resolution;
			this.highPrecision = highPrecision;
			this.numButterflies = (int)(Mathf.Log((float)resolution) / Mathf.Log(2.0f));
			this.numButterfliesPow2 = Mathf.NextPowerOfTwo(numButterflies);
			this.twoChannels = twoChannels;
			this.usesUAV = usesUAV;

			RetrieveRenderTexturesSet();
			CreateTextures();
        }

		public Texture2D Butterfly
		{
			get { return butterfly; }
		}

		public RenderTexture RealOutput
		{
			get { return realOutput; }
		}

		public int Resolution
		{
			get { return resolution; }
		}

		abstract public void SetupMaterials();
		abstract public RenderTexture ComputeFFT(Texture tex);

		virtual public void Dispose()
		{
			if(butterfly != null)
			{
				Object.Destroy(butterfly);
				butterfly = null;
			}

			if(realOutput != null)
			{
				Object.Destroy(realOutput);
				realOutput = null;
			}
		}

		private void CreateTextures()
		{
			realOutput = new RenderTexture(resolution, resolution, 0, twoChannels ? RenderTextureFormat.RGHalf : RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
			realOutput.hideFlags = HideFlags.DontSave;
			realOutput.wrapMode = TextureWrapMode.Repeat;
			realOutput.useMipMap = true;
			realOutput.generateMips = true;
			realOutput.filterMode = FilterMode.Trilinear;

			CreateButterflyTexture();
		}

		private void RetrieveRenderTexturesSet()
		{
			var format = twoChannels ?
				(highPrecision ? RenderTextureFormat.ARGBFloat : RenderTextureFormat.ARGBHalf) :
				(highPrecision ? RenderTextureFormat.RGFloat : RenderTextureFormat.RGHalf);

			renderTexturesSet = RenderTexturesCache.GetCache(resolution, resolution, 0, format, true, usesUAV);
		}

		void BitReverse(int[] indices, int N, int n)
		{
			int mask = 0x1;

			for (int j = 0; j < N; j++)
			{
				int val = 0x0;
				int temp = indices[j];

				for (int i = 0; i < n; i++)
				{
					int t = (mask & temp);
					val = (val << 1) | t;
					temp = temp >> 1;
				}

				indices[j] = val;
			}
		}

		private void ComputeWeights(int numPoints, int numButterflies, Vector2[][] weights)
		{
			int groups = numPoints >> 1;
			int numKs = 1;

			float invNumPoints = 1.0f / numPoints;

			for(int i = 0; i < numButterflies; i++)
			{
				int start = 0;
				int end = numKs;

				var weights2 = weights[i];

				for(int b = 0; b < groups; b++)
				{
					for(int k = start, K = 0; k < end; k++, K++)
					{
						float t = 2.0f * Mathf.PI * K * groups * invNumPoints;

						float real = Mathf.Cos(t);
						float im = -Mathf.Sin(t);

						weights2[k].x = real;
						weights2[k].y = im;
						weights2[k + numKs].x = -real;
						weights2[k + numKs].y = -im;
					}

					start += numKs << 1;
					end = start + numKs;
				}

				groups = groups >> 1;
				numKs = numKs << 1;
			}
		}

		private void ComputeIndices(int[][] indices, int numPoints, int numButterflies)
		{
			int numIters = 1;
			int offset, step;
			offset = numPoints;

			for(int butterflyIndex = 0; butterflyIndex < numButterflies; butterflyIndex++)
			{
				offset = offset >> 1;
				step = offset << 1;

				int p = 0;
				int start = 0;
				int end = step;

				var indices2 = indices[butterflyIndex];

				for(int i = 0; i < numIters; i++)
				{
					for(int j = start, k = p, l = 0; j < end; j += 2, l += 2, k++)
					{
						indices2[j] = k;
						indices2[j + 1] = k + offset;
						indices2[l + end] = k;
						indices2[l + end + 1] = k + offset;
					}

					start += step << 1;
					end += step << 1;
					p += step;
				}

				numIters = numIters << 1;
			}

			BitReverse(indices[numButterflies - 1], numPoints << 1, numButterflies);
		}

		virtual protected void FillButterflyTexture(Texture2D butterfly, int[][] indices, Vector2[][] weights)
		{
			float floatResolution = resolution;

			for(int row = 0; row < numButterflies; row++)
			{
				for(int col = 0; col < resolution; col++)
				{
					Color c;

					int indexX = numButterflies - row - 1;
					int indexY = (col << 1);

					c.r = (indices[indexX][indexY] + 0.5f) / floatResolution;
					c.g = (indices[indexX][indexY + 1] + 0.5f) / floatResolution;

					c.b = weights[row][col].x;
					c.a = weights[row][col].y;

					butterfly.SetPixel(col, row, c);
				}
			}
		}

		private void CreateButterflyTexture()
		{
			butterfly = new Texture2D(resolution, numButterfliesPow2, highPrecision ? TextureFormat.RGBAFloat : TextureFormat.RGBAHalf, false, true);
			butterfly.hideFlags = HideFlags.DontSave;
			butterfly.filterMode = FilterMode.Point;
			butterfly.wrapMode = TextureWrapMode.Clamp;

			int[][] indices = new int[numButterflies][];
			Vector2[][] weights = new Vector2[numButterflies][];
	
			for (int i = 0; i < (int)numButterflies; i++)
			{
				indices[i] = new int[2 * resolution];
				weights[i] = new Vector2[resolution];
			}

			ComputeIndices(indices, resolution, numButterflies);
			ComputeWeights(resolution, numButterflies, weights);
			FillButterflyTexture(butterfly, indices, weights);

			butterfly.Apply();
		}
	}
}
