using UnityEngine;
using UnityEngine.Networking;

namespace PlayWay.Water
{
	[AddComponentMenu("Water/Network Synchronization", 2)]
	public class NetworkWater : NetworkBehaviour
	{
		[SyncVar]
		private float time;

		private WaterWavesFFT wavesFFT;

		void Start()
		{
			wavesFFT = GetComponent<WaterWavesFFT>();
		}

		void Update()
		{
			if(isServer)
				time = Time.time;
			else
				time += Time.deltaTime;

			if(wavesFFT != null)
				wavesFFT.SetTime(time);
		}
	}
}
