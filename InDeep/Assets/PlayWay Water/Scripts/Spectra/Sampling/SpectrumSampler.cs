using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace PlayWay.Water
{
	public class SpectrumSampler : MonoBehaviour
	{
		static private SpectrumSampler instance;

		static public SpectrumSampler Instance
		{
			get
			{
				if(instance == null)
				{
					instance = GameObject.FindObjectOfType<SpectrumSampler>();

					if(instance == null)
					{
						var go = new GameObject("PlayWay Water Spectrum Sampler");
						go.hideFlags = HideFlags.HideInHierarchy;
						instance = go.AddComponent<SpectrumSampler>();
					}
				}

				return instance;
			}
		}

		static public bool HasInstance
		{
			get { return instance != null; }
		}

		private Thread thread;
		private bool run;

		private List<SpectrumSample> computations = new List<SpectrumSample>();
		private int computationIndex;

		void Awake()
		{
			run = true;

			Thread thread = new Thread(Run);
			thread.Start();
		}

		public void StartComputations(SpectrumSample computation)
		{
			lock(computations)
			{
				computations.Add(computation);
			}
		}

		public void StopComputations(SpectrumSample computation)
		{
			lock(computations)
			{
				int index = computations.IndexOf(computation);

				if(index == -1) return;

				if(index < computationIndex)
					--computationIndex;

				computations.RemoveAt(index);
			}
		}
		
		void OnDisable()
		{
			run = false;
        }
		
		private void Run()
		{
			while(run)
			{
				SpectrumSample computation = null;

				lock(computations)
				{
					if(computations.Count != 0)
					{
						if(computationIndex >= computations.Count)
							computationIndex = 0;

						computation = computations[computationIndex++];
					}
				}

				if(computation == null)
				{
					Thread.Sleep(4);
					continue;
				}

				lock(computation)
				{
					computation.ComputationStep();
				}
			}
		}
	}
}
