using UnityEngine;
using PlayWay.Water;
using System.Collections.Generic;

public class WaterPhysics : MonoBehaviour
{
	[Space]
	[Tooltip("Controls precision of the simulation. Keep it low (1 - 2) for small and not important objects. Prefer high values (15 - 30) for ships etc.")]
	[Range(1, 30)]
	[SerializeField]
	private int sampleCount = 20;

	[Range(0.0f, 3.0f)]
	[Tooltip("Controls drag force. Determined experimentally in wind tunnels. Example values:\n https://en.wikipedia.org/wiki/Drag_coefficient#General")]
	[SerializeField]
	private float dragCoefficient = 0.9f;

	[Space]
	[Range(0.125f, 1.0f)]
	[Tooltip("Determines how many waves will be used in computations. Set it low for big objects, larger than most of the waves. Set it high for smaller objects of size comparable to many waves.")]
	[SerializeField]
	private float precision = 0.2f;

	[Tooltip("Don't modify unless you are working on objects with extremely low density (like beach balls or pontoons) or colliders larger than the real object. Lowering this may fix some weird behaviour caused by too long physics time steps.")]
	[SerializeField]
	private float buoyancyIntensity = 1.0f;

	private Vector3[] cachedSamplePositions;
	private int cachedSampleIndex;
	private int cachedSampleCount;

	private new Collider collider;
	private Rigidbody rigidBody;

	private float volume;
	private float area;

	private SpectrumSample[] samples;

	// precomputed stuff
	private float numSamplesInv;
	private Vector3 buoyancyPart;
	private float dragPart;
	private WaterVolumeProbe waterProbe;

	void Awake()
	{
		collider = GetComponent<Collider>();
		rigidBody = GetComponentInParent<Rigidbody>();

		if(collider == null || rigidBody == null)
		{
			Debug.LogError("WaterPhysics are attached to an object without any Collider and/or RigidBody.");
			enabled = false;
			return;
		}

		waterProbe = WaterVolumeProbe.CreateProbe(transform);
		waterProbe.Enter.AddListener(OnWaterEnter);
		waterProbe.Leave.AddListener(OnWaterLeave);

		OnValidate();

		PrecomputeSamples();
	}

	public float BuoyancyIntensity
	{
		get { return buoyancyIntensity; }
		set
		{
			buoyancyIntensity = value;

			if(waterProbe.CurrentWater != null)
				PrecomputeBuoyancy();
		}
	}

	public float DragCoefficient
	{
		get { return dragCoefficient; }
		set
		{
			dragCoefficient = value;

			if(waterProbe.CurrentWater != null)
				PrecomputeDrag();
		}
	}

	void OnValidate()
	{
		numSamplesInv = 1.0f / sampleCount;

		if(collider != null)
		{
			volume = collider.ComputeVolume();
			area = collider.ComputeArea();
		}
	}

	void FixedUpdate()
	{
		var currentWater = waterProbe.CurrentWater;

		if(currentWater == null) return;

		var bounds = collider.bounds;
		float min = bounds.min.y;
		float max = bounds.max.y;

		Vector3 force;
		float height = max - min + 80.0f;
		float drag = 0.0f;
		float fixedDeltaTime = Time.fixedDeltaTime;
		float forceToVelocity = fixedDeltaTime * (1.0f - rigidBody.drag * fixedDeltaTime) / rigidBody.mass;
		float precompMaxF = rigidBody.mass * numSamplesInv / fixedDeltaTime;
		float waterDensity = currentWater.Density;

		/*
		 * Compute new samples.
		 */
		for(int i = 0; i < sampleCount; ++i)
		{
			Vector3 point = transform.TransformPoint(cachedSamplePositions[cachedSampleIndex]);
			Vector3 displaced = samples[i].ComputeDisplaced(point.x, point.z, false);

			float waterHeight = displaced.y;
			displaced.y = min - 20.0f;

			RaycastHit hitInfo;

			if(collider.Raycast(new Ray(displaced, Vector3.up), out hitInfo, height))
			{
				float low = hitInfo.point.y;

				displaced.y = max + 20.0f;
				collider.Raycast(new Ray(displaced, Vector3.down), out hitInfo, height);

				float high = hitInfo.point.y;

				float frc = (waterHeight - low) / (high - low);

				if(frc < 0.0f) frc = 0.0f;
				if(frc > 1.0f) frc = 1.0f;

				// buoyancy
				force = buoyancyPart * waterDensity * frc;

				// hydrodynamic drag
				displaced.y = Mathf.Lerp(low, high, frc * 0.5f);

				Vector3 pointVelocity = rigidBody.GetPointVelocity(displaced);
				Vector3 velocity = pointVelocity + force * forceToVelocity;
				Vector3 sqrVelocity = new Vector3(velocity.x * velocity.x, velocity.y * velocity.y, velocity.z * velocity.z);

				if(velocity.x > 0.0f) sqrVelocity.x = -sqrVelocity.x;
				if(velocity.y > 0.0f) sqrVelocity.y = -sqrVelocity.y;
				if(velocity.z > 0.0f) sqrVelocity.z = -sqrVelocity.z;

				Vector3 dragForce = dragPart * waterDensity * sqrVelocity;

				// limit drag to prevent backward motion
				float maxF = pointVelocity.magnitude * precompMaxF + force.magnitude;
				float prop = maxF / dragForce.magnitude;
				if(prop < 1.0f) dragForce *= prop;

				force += frc * dragForce;
				drag += frc;

				// apply forces
				rigidBody.AddForceAtPosition(force, displaced, ForceMode.Force);
			}

			if(++cachedSampleIndex >= cachedSampleCount)
				cachedSampleIndex = 0;
		}

		// apply some additional angular drag as above method may not apply a full response, especially with low sample count
		drag *= numSamplesInv;
		rigidBody.angularVelocity *= (1.0f - drag * 0.04f);
	}

	private void OnWaterEnter()
	{
		CreateWaterSamplers();
		PrecomputeBuoyancy();
		PrecomputeDrag();
    }

	private void OnWaterLeave()
	{
		for(int i = 0; i < sampleCount; ++i)
		{
			samples[i].Stop();
			samples[i] = null;
		}
	}

	private void PrecomputeSamples()
	{
		var samplePositions = new List<Vector3>();

		float offset = 0.5f;
		float step = 1.0f;
		int targetPoints = sampleCount * 14;
		var transform = this.transform;

		Vector3 min, max;
		ColliderExtensions.GetLocalMinMax(collider, out min, out max);

		for(int i = 0; i < 4 && samplePositions.Count < targetPoints; ++i)
		{
			for(float x = offset; x <= 1.0f; x += step)
			{
				for(float y = offset; y <= 1.0f; y += step)
				{
					for(float z = offset; z <= 1.0f; z += step)
					{
						Vector3 p = new Vector3(Mathf.Lerp(min.x, max.x, x), Mathf.Lerp(min.y, max.y, y), Mathf.Lerp(min.z, max.z, z));

						if(collider.IsPointInside(transform.TransformPoint(p)))
							samplePositions.Add(p);
					}
				}
			}

			step = offset;
			offset *= 0.5f;
		}

		cachedSamplePositions = samplePositions.ToArray();
		cachedSampleCount = cachedSamplePositions.Length;
		Shuffle(cachedSamplePositions);
	}

	private void CreateWaterSamplers()
	{
		if(samples == null || samples.Length != sampleCount)
			samples = new SpectrumSample[sampleCount];

		for(int i = 0; i < sampleCount; ++i)
			samples[i] = new SpectrumSample(waterProbe.CurrentWater, SpectrumSample.DisplacementMode.Height, precision);
	}

	private void PrecomputeBuoyancy()
	{
		buoyancyPart = -Physics.gravity * (numSamplesInv * volume * buoyancyIntensity);
	}

	private void PrecomputeDrag()
	{
		dragPart = 0.5f * dragCoefficient * area * numSamplesInv;
	}

	private void Shuffle<T>(T[] array)
	{
		int n = array.Length;

		while(n > 1)
		{
			int k = Random.Range(0, n--);

			var t = array[n];
			array[n] = array[k];
			array[k] = t;
		}
	}
}
