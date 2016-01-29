using UnityEngine;

namespace PlayWay.Water
{
	/// <summary>
	/// Computes FFT on shader model 2.0 pixel shaders. The only considerable requirement is the support for at least half precision fp render textures.
	/// </summary>
	sealed public class PixelShaderFFT : GpuFFT
	{
		private Material material;

		private int butterflyTexProperty;
		private int butterflyPassProperty;

		private TemporaryRenderTexture rt1;
		private TemporaryRenderTexture rt2;

		public PixelShaderFFT(Shader fftShader, int resolution, bool highPrecision, bool twoChannels) : base(resolution, highPrecision, twoChannels, false)
		{
			material = new Material(fftShader);
			material.hideFlags = HideFlags.DontSave;

			butterflyTexProperty = Shader.PropertyToID("_ButterflyTex");
			butterflyPassProperty = Shader.PropertyToID("_ButterflyPass");
		}

		public override void Dispose()
		{
			base.Dispose();

			if(material == null)
				Object.Destroy(material);
		}

		public override void SetupMaterials()
		{
			material.SetTexture(butterflyTexProperty, Butterfly);
		}

		override public RenderTexture ComputeFFT(Texture tex)
		{
			using(rt1 = renderTexturesSet.GetTemporary())
			using(rt2 = renderTexturesSet.GetTemporary())
			{
				ComputeFFT(tex, twoChannels ? 2 : 0, false);
				ComputeFFT(rt1, twoChannels ? 3 : 1, true);
			}

			return realOutput;
		}

		private void ComputeFFT(Texture tex, int passIndex, bool invertLastPass)
		{
			material.mainTexture = tex;
			material.SetFloat(butterflyPassProperty, 0.5f / (float)numButterfliesPow2);
			Graphics.Blit(null, rt2, material, passIndex);

			SwapRT();

			for(int i = 1; i < numButterflies; ++i)
			{
				if(invertLastPass && i == numButterflies - 1)
				{
					material.mainTexture = rt1;
					material.SetFloat(butterflyPassProperty, (i + 0.5f) / (float)numButterfliesPow2);
					Graphics.Blit(null, realOutput, material, passIndex == 1 ? 4 : 5);
				}
				else
				{
					material.mainTexture = rt1;
					material.SetFloat(butterflyPassProperty, (i + 0.5f) / (float)numButterfliesPow2);
					Graphics.Blit(null, rt2, material, passIndex);
				}

				SwapRT();
			}
		}

		private void SwapRT()
		{
			var t = rt1;
			rt1 = rt2;
			rt2 = t;
		}
	}
}
