using UnityEngine;

namespace PlayWay.Water
{
	/// <summary>
	/// Extends water volume. Added automatically to all water child colliders. No need to use it manually, ever.
	/// </summary>
	public class WaterVolumeAdd : MonoBehaviour
	{
		[SerializeField]
		private Water water;

		private Collider[] colliders;

		void Awake()
		{
			water = GetComponentInParent<Water>();
			colliders = GetComponents<Collider>();
		}

		public Water Water
		{
			get { return water; }
		}

		public bool IsPointInside(Vector3 point)
		{
			foreach(var collider in colliders)
			{
				if(collider.IsPointInside(point))
					return true;
			}

			return false;
		}
	}
}
