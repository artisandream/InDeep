using UnityEngine;

namespace PlayWay.Water
{
	public class WaterFloat : MonoBehaviour
	{
		[SerializeField]
		private float heightBonus = 0.0f;

		[Range(0.04f, 1.0f)]
		[SerializeField]
		private float precision = 0.2f;

		[SerializeField]
		private SpectrumSample.DisplacementMode displacementMode = SpectrumSample.DisplacementMode.Displacement;
		
		private SpectrumSample sample;

		private Vector3 initialPosition;

		void Start()
		{
			initialPosition = transform.position;

			var water = FindObjectOfType<Water>();
			sample = new SpectrumSample(water, displacementMode, precision);
		}

		void OnDisable()
		{
			sample.Stop();
		}

		void LateUpdate()
		{
			Vector3 displaced = sample.ComputeDisplaced(initialPosition.x, initialPosition.z);
			displaced.y += heightBonus;
            transform.position = displaced;
		}
	}
}
