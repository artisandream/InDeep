using UnityEngine;

namespace PlayWay.Water
{
	public class WaterProjectSettings : ScriptableObjectSingleton
	{
		[SerializeField]
		private int waterVolumesLayer = 20;

		[SerializeField]
		private bool waterMasksEnabled = false;

		[SerializeField]
		private int waterMasksLayer = 21;

		static private WaterProjectSettings instance;

		static public WaterProjectSettings Instance
		{
			get
			{
				if(instance == null)
					instance = LoadSingleton<WaterProjectSettings>();

				return instance;
			}
		}

		public int WaterVolumesLayer
		{
			get { return waterVolumesLayer; }
		}

		public bool WaterMasksEnabled
		{
			get { return waterMasksEnabled; }
		}

		public int WaterMasksLayer
		{
			get { return waterMasksLayer; }
		}
	}
}
