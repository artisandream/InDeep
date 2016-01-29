﻿using UnityEngine;
using UnityEngine.Events;

namespace PlayWay.Water
{
	/// <summary>
	/// Finds out in which water volume this GameObject is contained and raises events on enter/leave.
	/// </summary>
	public class WaterVolumeProbe : MonoBehaviour
	{
		[SerializeField]
		private UnityEvent enter;

		[SerializeField]
		private UnityEvent leave;

		private Water currentWater;
		private Transform target;
		private bool targetted;
		private WaterVolumeSubtract[] exclusions;

		public Water CurrentWater
		{
			get { return currentWater; }
		}

		public UnityEvent Enter
		{
			get
			{
				if(enter == null)
					enter = new UnityEvent();

				return enter;
			}
		}

		public UnityEvent Leave
		{
			get
			{
				if(leave == null)
					leave = new UnityEvent();

				return leave;
			}
		}

		void Start()
		{
			ScanWaters();
		}

		void FixedUpdate()
		{
			if(targetted)
			{
				if(target == null)
				{
					Destroy(gameObject);            // cleans itself if target has been destroyed
					return;
				}

				transform.position = target.position;
			}

			if(currentWater != null && currentWater.Volume.Boundless)
			{
				if(!currentWater.Volume.IsPointInsideMainVolume(transform.position) && !currentWater.Volume.IsPointInside(transform.position, exclusions))
					LeaveCurrentWater();
            }
			else if(currentWater == null)
				ScanBoundlessWaters();
		}

		public void OnTriggerEnter(Collider other)
		{
			if(currentWater != null)
			{
				var volumeSubtract = other.GetComponent<WaterVolumeSubtract>();

				if(volumeSubtract != null)
				{
					if(!currentWater.Volume.IsPointInside(transform.position, exclusions))
						LeaveCurrentWater();
				}
			}
			else
			{
				var volumeAdd = other.GetComponent<WaterVolumeAdd>();

				if(volumeAdd != null && volumeAdd.Water.Volume.IsPointInside(transform.position, exclusions))
					EnterWater(volumeAdd.Water);
			}
        }

		public void OnTriggerExit(Collider other)
		{
			if(currentWater == null)
			{
				var volumeSubtract = other.GetComponent<WaterVolumeSubtract>();

				if(volumeSubtract != null)
					ScanWaters();
			}
			else
			{
				var volumeAdd = other.GetComponent<WaterVolumeAdd>();

				if(volumeAdd != null && volumeAdd.Water == currentWater && !currentWater.Volume.IsPointInside(transform.position, exclusions))
					LeaveCurrentWater();
			}
		}

		[ContextMenu("Refresh Probe")]
		private void ScanWaters()
		{
			Vector3 position = transform.position;

			foreach(var water in WaterGlobals.Instance.Waters)
			{
				if(water.Volume.IsPointInside(position, exclusions))
				{
					EnterWater(water);
					return;
				}
			}

			LeaveCurrentWater();
		}

		private void ScanBoundlessWaters()
		{
			Vector3 position = transform.position;

			foreach(var water in WaterGlobals.Instance.BoundlessWaters)
			{
				if(water.Volume.IsPointInsideMainVolume(position) && water.Volume.IsPointInside(position, exclusions))
				{
					EnterWater(water);
					return;
				}
			}
		}

		private void EnterWater(Water water)
		{
			if(currentWater == water) return;

			if(currentWater != null)
				LeaveCurrentWater();

			currentWater = water;

			if(enter != null)
				enter.Invoke();
		}

		private void LeaveCurrentWater()
		{
			if(currentWater != null)
			{
				if(leave != null)
					leave.Invoke();

				currentWater = null;
			}
		}

		static public WaterVolumeProbe CreateProbe(Transform target)
		{
			var go = new GameObject("Water Volume Probe");
			go.hideFlags = HideFlags.HideAndDontSave;
			go.transform.position = target.position;

			var sphereCollider = go.AddComponent<SphereCollider>();
			sphereCollider.radius = 0.0f;               // make it a point
			sphereCollider.isTrigger = true;

			var rigidBody = go.AddComponent<Rigidbody>();
			rigidBody.isKinematic = true;
			rigidBody.mass = 0.0000001f;

			var probe = go.AddComponent<WaterVolumeProbe>();
			probe.target = target;
			probe.targetted = true;
			probe.exclusions = target.GetComponentsInChildren<WaterVolumeSubtract>(true);

			return probe;
		}
	}
}