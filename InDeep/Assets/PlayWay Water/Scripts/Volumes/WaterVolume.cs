using System.Collections.Generic;
using UnityEngine;

namespace PlayWay.Water
{
	[System.Serializable]
	public class WaterVolume
	{
		[Tooltip("Makes water volume be infinite in horizontal directions and infinitely deep. It is still reduced by substractive colliders tho. Check that if this is an ocean, sea or if this water spans through most of the scene.")]
		[SerializeField]
		private bool boundless = true;
		
		private Water water;
		private List<WaterVolumeAdd> volumes;
		private List<WaterVolumeSubtract> subtractors;
		private Camera volumesCamera;
		private bool collidersAdded;

		public WaterVolume()
		{
			volumes = new List<WaterVolumeAdd>();
			subtractors = new List<WaterVolumeSubtract>();
		}

		public bool Boundless
		{
			get { return boundless; }
		}

		public void Dispose()
		{
			if(volumesCamera != null)
			{
				if(Application.isPlaying)
					Object.Destroy(volumesCamera.gameObject);
				else
					Object.DestroyImmediate(volumesCamera.gameObject);

				volumesCamera = null;
			}
        }

		internal void OnEnable(Water water)
		{
			this.water = water;

			if(!collidersAdded && Application.isPlaying)
			{
				var colliders = water.GetComponentsInChildren<Collider>(true);

				foreach(var collider in colliders)
				{
					var volumeSubtract = collider.GetComponent<WaterVolumeSubtract>();

					if(volumeSubtract == null)
					{
						var volumeAdd = collider.GetComponent<WaterVolumeAdd>();

						if(volumeAdd == null)
							volumeAdd = collider.gameObject.AddComponent<WaterVolumeAdd>();

						AddVolume(volumeAdd);
					}
				}

				collidersAdded = true;
            }
		}

		internal void OnDisable()
		{
			Dispose();
		}

		internal void AddVolume(WaterVolumeAdd volume)
		{
			volumes.Add(volume);
		}

		internal void RemoveVolume(WaterVolumeAdd volume)
		{
			volumes.Remove(volume);
		}

		internal void AddSubtractor(WaterVolumeSubtract volume)
		{
			subtractors.Add(volume);
		}

		internal void RemoveSubtractor(WaterVolumeSubtract volume)
		{
			subtractors.Remove(volume);
		}

		public bool IsPointInside(Vector3 point, WaterVolumeSubtract[] exclusions)
		{
            foreach(var volume in subtractors)
			{
				if(volume.IsPointInside(point) && !Contains(exclusions, volume))
					return false;
			}

			if(boundless)
				return point.y <= water.transform.position.y + water.SpectraRenderer.MaxHeight;

			foreach(var volume in volumes)
			{
				if(volume.IsPointInside(point))
					return true;
			}

			return false;
		}

		private bool Contains(WaterVolumeSubtract[] array, WaterVolumeSubtract element)
		{
			if(array == null) return false;

			for(int i = 0; i < array.Length; ++i)
			{
				if(array[i] == element)
					return true;
			}

			return false;
		}

		internal bool IsPointInsideMainVolume(Vector3 point)
		{
			if(boundless)
				return point.y <= water.transform.position.y + water.SpectraRenderer.MaxHeight;
			else
				return false;
		}
		
		private void CreateVolumesCamera()
		{
			var volumesCameraGo = new GameObject();
			volumesCameraGo.hideFlags = HideFlags.HideAndDontSave;

			volumesCamera = volumesCameraGo.AddComponent<Camera>();
			volumesCamera.enabled = false;
        }
	}
}
