using UnityEngine;

namespace PlayWay.Water
{
	/// <summary>
	/// Performs FFT with compute shaders (fast). The in/out resolution cannot exceed 1024.
	/// </summary>
	public class Dx11FFT : GpuFFT
	{
		private ComputeShader shader;
		private int kernelIndex;

		public Dx11FFT(ComputeShader shader, int resolution, bool highPrecision, bool twoChannels) : base(resolution, highPrecision, twoChannels, true)
		{
			this.shader = shader;

			kernelIndex = (numButterflies - 8) << 1;

			if(twoChannels)
				kernelIndex += 6;
		}

		public override void SetupMaterials()
		{
			// nothing to do
		}

		override public RenderTexture ComputeFFT(Texture tex)
		{
			using(var rt1 = renderTexturesSet.GetTemporary())
			using(var rt2 = renderTexturesSet.GetTemporary())
			{
				if(!realOutput.IsCreated())
					realOutput.Create();

				shader.SetTexture(kernelIndex, "_ButterflyTex", Butterfly);
				shader.SetTexture(kernelIndex, "_SourceTex", tex);
				shader.SetTexture(kernelIndex, "_TargetTex", rt1);
				shader.Dispatch(kernelIndex, 1, tex.height, 1);

				shader.SetTexture(kernelIndex + 1, "_ButterflyTex", Butterfly);
				shader.SetTexture(kernelIndex + 1, "_SourceTex", rt1);
				shader.SetTexture(kernelIndex + 1, "_TargetTex", rt2);
				shader.Dispatch(kernelIndex + 1, 1, tex.height, 1);

				Graphics.Blit(rt2, realOutput);
			}

			return realOutput;
		}

		override protected void FillButterflyTexture(Texture2D butterfly, int[][] indices, Vector2[][] weights)
		{
			for(int row = 0; row < numButterflies; row++)
			{
				for(int col = 0; col < resolution; col++)
				{
					Color c;

					int indexX = numButterflies - row - 1;
					int indexY = (col << 1);

					c.r = indices[indexX][indexY];
					c.g = indices[indexX][indexY + 1];

					c.b = weights[row][col].x;
					c.a = weights[row][col].y;

					butterfly.SetPixel(col, row, c);
				}
			}
		}
	}
}
