using System;
using UnityEngine;

namespace PlayWay.Water
{
	public class ShorelinesRenderer : MonoBehaviour, IOverlaysRenderer
	{
		public void RenderOverlays(WaterOverlays overlays)
		{
			Vector2 origin = overlays.Camera.LocalMapsOrigin;
			float sizeInv = overlays.Camera.LocalMapsSizeInv;

			if(sizeInv == 0.0f)
				return;

			float size = 1.0f / sizeInv;
			
			var effectsCamera = overlays.Camera.EffectsCamera;
			effectsCamera.orthographic = true;
			effectsCamera.orthographicSize = size * 0.5f;
			effectsCamera.cullingMask = 1 << 10;
			effectsCamera.farClipPlane = 2000.0f;
			effectsCamera.clearFlags = CameraClearFlags.Nothing;
			effectsCamera.transform.position = new Vector3(origin.x + size * 0.5f, 1000.0f, origin.y + size * 0.5f);
			effectsCamera.transform.rotation = Quaternion.LookRotation(new Vector3(0.0f, -1.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f));
			effectsCamera.targetTexture = overlays.SlopeMap;
			effectsCamera.Render();
		}
	}
}
