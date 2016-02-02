using UnityEngine;

namespace PlayWay.Water
{
	public class WaterSample
	{
		private Water water;
		private float x;
		private float z;
		
		private Vector3 displaced;
		private Vector3 previousResult;
		private Vector3 forces;

		private bool finished;
		private bool enqueued;
		private bool changed;

		private float precision;
		private float horizontalThreshold;

		private float time;

		private DisplacementMode displacementMode;

		public WaterSample(Water water, DisplacementMode displacementMode = DisplacementMode.Height, float precision = 1.0f)
		{
			if(precision <= 0.0f || precision > 1.0f) throw new System.ArgumentException("Precision has to be between 0.0 and 1.0.");
			
            this.precision = precision;
			this.horizontalThreshold = 0.065f / (precision * precision * precision);

			this.water = water;
			this.displacementMode = displacementMode;
			this.previousResult.x = float.NaN;
        }

		public bool Finished
		{
			get { return finished; }
		}

		public Vector2 Position
		{
			get { return new Vector2(x, z); }
		}

		/// <summary>
		/// Starts water height computations.
		/// </summary>
		/// <param name="origin"></param>
		public void Start(Vector3 origin)
		{
			GetAndReset(origin.x, origin.z, ComputationsMode.Normal);
		}

		/// <summary>
		/// Starts water height computations.
		/// </summary>
		/// <param name="origin"></param>
		public void Start(float x, float z)
		{
			GetAndReset(x, z, ComputationsMode.Normal);
		}

		/// <summary>
		/// Retrieves recently computed displacement and restarts computations on a new position.
		/// </summary>
		/// <param name="origin"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		public Vector3 GetAndReset(Vector3 origin, ComputationsMode mode = ComputationsMode.Normal)
		{
			return GetAndReset(origin.x, origin.z, mode);
		}

		/// <summary>
		/// Retrieves recently computed displacement and restarts computations on a new position.
		/// </summary>
		/// <param name="x">World space coordinate.</param>
		/// <param name="z">World space coordinate.</param>
		/// <param name="mode">Determines if the computations should be completed on the current thread if necessary. May hurt performance, but setting it to false may cause some 'flickering'.</param>
		/// <returns></returns>
		public Vector3 GetAndReset(float x, float z, ComputationsMode mode = ComputationsMode.Normal)
		{
			Vector3 forces;
			return GetAndReset(x, z, mode, out forces);
		}

		/// <summary>
		/// Retrieves recently computed displacement and restarts computations on a new position.
		/// </summary>
		/// <param name="x">World space coordinate.</param>
		/// <param name="z">World space coordinate.</param>
		/// <param name="mode">Determines if the computations should be completed on the current thread if necessary. May hurt performance, but setting it to false may cause some 'flickering'.</param>
		/// <returns></returns>
		public Vector3 GetAndReset(float x, float z, ComputationsMode mode, out Vector3 forces)
		{
			if(!enqueued)
			{
				WaterAsynchronousTasks.Instance.AddWaterSampleComputations(this);
				enqueued = true;

				water.OnSamplingStarted();
			}

			if(mode == ComputationsMode.ForceCompletion && !finished)
			{
				lock(this)
				{
					while(!finished)
						ComputationStep();
				}
			}

			bool wasFinished = finished;
			
			finished = true;
			changed = true;
			
			Vector3 result = this.displaced;
			result.y += water.transform.position.y;
			forces = this.forces;

			this.x = x;
			this.z = z;
			this.displaced.x = x;
			this.displaced.y = 0.0f;
			this.displaced.z = z;
			this.forces.x = 0.0f;
			this.forces.y = 0.0f;
			this.forces.z = 0.0f;
            this.time = water.Time;
			this.finished = false;
			
			if(mode == ComputationsMode.Stabilized && !wasFinished)
			{
				if(!float.IsNaN(previousResult.x))
					result = previousResult;
				else
					result = new Vector3(x, 0.0f, z);

				previousResult = result;
			}

			return result;
		}

		/// <summary>
		/// Faster version of GetAndReset. Assumes HeightAndForces displacement mode and that computations were started earlier with Start call. 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="z"></param>
		/// <param name="result"></param>
		/// <param name="forces"></param>
		public void GetAndResetFast(float x, float z, float time, ref Vector3 result, ref Vector3 forces)
		{
			finished = true;
			changed = true;
			
			result = this.displaced;
			result.y += water.transform.position.y;
			forces = this.forces;

			this.x = x;
			this.z = z;
			this.displaced.x = x;
			this.displaced.y = 0.0f;
			this.displaced.z = z;
			this.forces.x = 0.0f;
			this.forces.y = 0.0f;
			this.forces.z = 0.0f;
			this.time = time;
			this.finished = false;
		}

		public Vector3 Stop()
		{
			lock (this)
			{
				if(enqueued)
				{
					if(WaterAsynchronousTasks.HasInstance)
						WaterAsynchronousTasks.Instance.RemoveWaterSampleComputations(this);

					enqueued = false;

					if(water != null)
						water.OnSamplingStopped();
				}
				
				return displaced;
			}
		}
		
		static private float[] weights = new float[] { 0.85f, 0.75f, 0.83f, 0.77f, 0.85f, 0.75f, 0.85f, 0.75f, 0.83f, 0.77f };      // ~6.02f

		internal void ComputationStep(bool ignoreFinishedFlag = false)
		{
			changed = false;

			if(!finished || ignoreFinishedFlag)
			{
				if(displacementMode == DisplacementMode.Height || displacementMode == DisplacementMode.HeightAndForces)
				{
					CompensateHorizontalDisplacement();

					if(displacementMode == DisplacementMode.Height)
					{
						// compute height at resultant point
						float result = water.GetHeightAt(x, z, 0.0f, precision, time);

						if(!changed)
							displaced.y += result;
					}
					else
					{
						Vector4 result = water.GetHeightAndForcesAt(x, z, 0.0f, precision, time);

						if(!changed)
						{
							displaced.y += result.w;
							forces.x += result.x;
							forces.y += result.y;
							forces.z += result.z;
						}
					}
				}
				else
				{
					Vector3 result = water.GetDisplacementAt(x, z, 0.0f, precision, time);

					if(!changed)
						displaced += result;
				}

				if(!changed)
					finished = true;
			}
		}

		private void CompensateHorizontalDisplacement()
		{
			Vector2 offset = water.GetHorizontalDisplacementAt(x, z, 0, precision * 0.5f, time);

			if(!changed)
			{
				x -= offset.x;
				z -= offset.y;
			}

			if(offset.x > horizontalThreshold || offset.y > horizontalThreshold || offset.x < -horizontalThreshold || offset.y < -horizontalThreshold)
			{
				float dx = 0, dz = 0;

				for(int i = 0; i < 8; ++i)
				{
					offset = water.GetHorizontalDisplacementAt(x, z, 0, precision * 0.5f, time);

					if(!changed)
					{
						dx = displaced.x - (x + offset.x);
						dz = displaced.z - (z + offset.y);
						x += dx * weights[i];
						z += dz * weights[i];

						if(dx < horizontalThreshold && dz < horizontalThreshold && dx > -horizontalThreshold && dz > -horizontalThreshold)
							break;
					}
				}
			}
		}
		
		public enum DisplacementMode
		{
			Height,
			Displacement,
			HeightAndForces
		}

		public enum ComputationsMode
		{
			Normal,
			Stabilized,
			ForceCompletion
		}
	}
}
