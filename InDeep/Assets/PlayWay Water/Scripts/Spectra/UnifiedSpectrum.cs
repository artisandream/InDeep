using UnityEngine;

namespace PlayWay.Water
{
	/// <summary>
	/// Spectrum is based on the following paper:
	/// "A unified directional spectrum for long and short wind-driven waves." T. Elfouhaily, B. Chapron, and K.Katsaros Institut
	/// </summary>
	public class UnifiedSpectrum : WaterWavesSpectrum
	{
		private float fetch;

		public UnifiedSpectrum(float tileSize, float gravity, float windSpeed, float amplitude, float fetch) : base(tileSize, gravity, windSpeed, amplitude)
		{
			this.fetch = fetch;
		}

		override public void ComputeSpectrum(Vector3[,] spectrum, System.Random random)
		{
			int resolution = spectrum.GetLength(0);
			int halfResolution = resolution / 2;
			
			float frequencyScale = 2.0f * Mathf.PI / TileSize;

			float U10 = windSpeed;

			//float omegac = 0.84f;
			float omegac = 0.84f * Mathf.Pow((float)System.Math.Tanh(Mathf.Pow(fetch / 22000.0f, 0.4f)), -0.75f);

			float sqrt10 = Mathf.Sqrt(10.0f);

			// long-wave parameters
			float kp = gravity * FastMath.Pow2(omegac / U10);
			float cp = PhaseSpeed(kp);

			float omega = U10 / cp;
			float alphap = 0.006f * Mathf.Sqrt(omega);

			float sigma = 0.08f * (1.0f + 4.0f * Mathf.Pow(omegac, -3.0f));

			// short-wave parameters
			const float cm = 0.23f;
			float km = 2.0f * gravity / (cm * cm);

			float z0 = 3.7e-5f * U10 * U10 / gravity * Mathf.Pow(U10 / cp, 0.9f);
			float friction = U10 * 0.41f / Mathf.Log(10.0f / z0);           // 0.41 is the estimated 'k' from "the law of the wall"

			float a0 = Mathf.Log(2.0f) / 4.0f;
			float ap = 4.0f;
			float am = 0.13f * friction / cm;

			float alpham = 0.01f * (friction < cm ? 1.0f + Mathf.Log(friction / cm) : 1.0f + 3.0f * Mathf.Log(friction / cm));
			
			for(int x = 0; x < resolution; ++x)
			{
				float kx = frequencyScale * (x/* + 0.5f*/ - halfResolution);

				for(int y = 0; y < resolution; ++y)
				{
					float ky = frequencyScale * (y/* + 0.5f*/ - halfResolution);

					float k = Mathf.Sqrt(kx * kx + ky * ky);
					float c = PhaseSpeed(k);

					/*
					 * Long-wave spectrum (bl)
					 */
					float moskowitz = Mathf.Exp((-5.0f / 4.0f) * FastMath.Pow2(kp / k));

					float gamma = omegac <= 1.0f ? 1.7f : 1.7f + 6 * Mathf.Log(omegac);
					float r = Mathf.Exp(-FastMath.Pow2(Mathf.Sqrt(k / kp) - 1.0f) / (2.0f * sigma * sigma));
					float jonswap = Mathf.Pow(gamma, r);

					float fp = moskowitz * jonswap * Mathf.Exp(-(omega / sqrt10) * (Mathf.Sqrt(k / kp) - 1.0f));

					float bl = 0.5f * alphap * (cp / c) * fp;

					/*
					 * Short-wave spectrum (bh)
					 */
					float fm = Mathf.Exp(-0.25f * FastMath.Pow2(k / km - 1.0f));
					float bh = 0.5f * alpham * (cm / c) * fm;

					/*
					 * Directionality
					 */
					float deltak = (float)System.Math.Tanh(a0 + ap * Mathf.Pow(c / cp, 2.5f) + am * Mathf.Pow(cm / c, 2.5f));

					//float dp = windSpeed.x * kx / k + windSpeed.y * ky / k;
					//float phi = Mathf.Acos(dp);

					/*
					 * Total omni-directional spectrum
					 */
					float sk = amplitude * (bl + bh) /* (1.0f + deltak * Mathf.Cos(2.0f * phi))*/ / (k * k * k * k * 2.0f * Mathf.PI);
					sk = Mathf.Sqrt(sk) * frequencyScale;
					
					float h = FastMath.Gauss01() * sk;
					float hi = FastMath.Gauss01() * sk;

					int xCoord = (x + halfResolution) % resolution;
					int yCoord = (y + halfResolution) % resolution;

					if(x == halfResolution && y == halfResolution)
					{
						h = 0;
						hi = 0;
					}

					spectrum[xCoord, yCoord] = new Vector3(h, hi, deltak);
				}
			}
		}

		private float PhaseSpeed(float k)
		{
			return Mathf.Sqrt(gravity / k);
		}
	}
}
