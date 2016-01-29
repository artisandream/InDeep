using UnityEngine;

namespace PlayWay.Water
{
	public class SpectrumSample
	{
		private Water water;
		private float priority;
		private float x;
		private float z;

		private float xDisplaced;
		private float zDisplaced;
		
		private Vector3 displaced;
		
		private int segmentIndex;

		private ComputationsPhase phase;
		private bool enqueued;

		private int numHeightGroups;
		private float numWaveGroupsInv;

		private float time;

		private DisplacementMode displacementMode;

		public SpectrumSample(Water water, DisplacementMode displacementMode = DisplacementMode.CompensatedHeight, float precision = 1.0f)
		{
			if(precision <= 0.0f || precision > 1.0f) throw new System.ArgumentException("Precision has to be between 0.0 and 1.0.");

			int avgCpuWaves = water.SpectraRenderer.AvgCpuWaves;
            int numWaveGroups = Mathf.Clamp(Mathf.RoundToInt(avgCpuWaves / 90.0f), 1, 8);

			this.numWaveGroupsInv = 1.0f / numWaveGroups;
            this.numHeightGroups = Mathf.Max(1, Mathf.RoundToInt(numWaveGroups * precision));

			this.water = water;
			this.displacementMode = displacementMode;
			
			this.segmentIndex = 1;
		}

		public Vector2 Position
		{
			get { return new Vector2(x, z); }
		}

		/// <summary>
		/// Retrieves recently computed displacement and restarts computations on a new position.
		/// </summary>
		/// <param name="x">World space coordinate.</param>
		/// <param name="z">World space coordinate.</param>
		/// <param name="forceCompletion">Determines if the computations should be completed on the current thread if necessary. May hurt performance, but setting it to false may cause 'flickering'.</param>
		/// <returns></returns>
		public Vector3 ComputeDisplaced(float x, float z, bool forceCompletion = true)
		{
			lock (this)
			{
				Vector3 displaced = this.displaced;
				StartComputations(x, z, forceCompletion);

				return displaced;
			}
		}

		private void StartComputations(float x, float z, bool forceCompletion)
		{
			if(!enqueued)
			{
				SpectrumSampler.Instance.StartComputations(this);
				enqueued = true;
			}

			if(forceCompletion)
			{
				while(phase != ComputationsPhase.Done)
					ComputationStep();
			}

			this.x = x;
			this.z = z;
			this.xDisplaced = x;
			this.zDisplaced = z;
			this.displaced = new Vector3(x, 0.0f, z);
			this.time = Time.time;
			this.segmentIndex = 0;
			
			phase = displacementMode != DisplacementMode.CompensatedHeight ? ComputationsPhase.Displacement : ComputationsPhase.Compensation;
		}

		public Vector3 Stop()
		{
			lock (this)
			{
				if(enqueued)
				{
					if(SpectrumSampler.HasInstance)
						SpectrumSampler.Instance.StopComputations(this);

					enqueued = false;
				}
				
				return displaced;
			}
		}

		internal void ComputationStep()
		{
			switch(phase)
			{
				case ComputationsPhase.Compensation:
				{
					/*displacements[displacementIndex] = water.SpectraAnimator.GetDisplacementAt(x, z, displacementIndex * numWaveGroupsInv, (displacementIndex + 1) * numWaveGroupsInv, time);

					if(++displacementIndex >= numDisplacementGroups)
					{
						phase = ComputationsPhase.Displacement;
						
						if(displacementMode == DisplacementMode.DisplacedHeight)
						{
							for(int i = 0; i < numDisplacementGroups; ++i)
							{
								xDisplaced -= displacements[i].x * 0.5f;
								zDisplaced -= displacements[i].y * 0.5f;
							}
						}
					}*/

					phase = ComputationsPhase.Displacement;

					break;
				}
				
				case ComputationsPhase.Displacement:
				{
					if(displacementMode != DisplacementMode.Displacement)
						displaced.y += water.SpectraRenderer.GetHeightAt(xDisplaced, zDisplaced, segmentIndex * numWaveGroupsInv, (segmentIndex + 1) * numWaveGroupsInv, time);
					else
						displaced += water.SpectraRenderer.GetDisplacementAt(xDisplaced, zDisplaced, segmentIndex * numWaveGroupsInv, (segmentIndex + 1) * numWaveGroupsInv, time);

					if(++segmentIndex >= numHeightGroups)
						phase = ComputationsPhase.Done;
					
					break;
				}
			}
		}

		enum ComputationsPhase
		{
			Compensation,
			Displacement,
			Done
		}

		public enum DisplacementMode
		{
			Height,
			Displacement,
			CompensatedHeight
		}
	}
}
