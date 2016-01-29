using UnityEngine;

namespace PlayWay.Water
{
	public interface IWaterRenderAware
	{
		void OnWaterRender(Camera camera);
		void OnWaterPostRender(Camera camera);

		void ValidateNow(Water water, WaterQualityLevel qualityLevel);
	}
}
