using UnityEngine;

namespace PlayWay.Water
{
	public class WavesGenerator : MonoBehaviour
	{
		[SerializeField]
		private WaterWavesParticleSystem wavesParticleSystem;

		[SerializeField]
		private TerrainShoreline shore;

		[SerializeField]
		private float wavelength;

		[SerializeField]
		private float amplitude;

		[SerializeField]
		private float velocity;

		[SerializeField]
		private int width = 8;

		private float nextSpawnTime;
		private float timeStep;

		void Start()
		{
			OnValidate();
		}
		
		void Update()
		{
			if(Time.time > nextSpawnTime)
			{
				Vector3 position = transform.position;
				Vector3 direction = transform.forward;

				wavesParticleSystem.Spawn(new WaterWavesParticleSystem.LinearParticle(
					new Vector2(position.x, position.z),
					new Vector2(direction.x, direction.z).normalized,
					1.0f / wavelength, amplitude, 1.0f, shore
				), width);

				nextSpawnTime += timeStep;
            }
		}

		void OnValidate()
		{
			timeStep = wavelength / velocity;
		}
	}
}
