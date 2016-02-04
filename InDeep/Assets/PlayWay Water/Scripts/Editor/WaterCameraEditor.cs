using UnityEditor;
using PlayWay.Water;
using UnityEngine;

namespace PlayWay.WaterEditor
{
	[CustomEditor(typeof(WaterCamera))]
	public class WaterCameraEditor : WaterEditorBase
	{
		public override void OnInspectorGUI()
		{
			var waterCamera = (WaterCamera)target;
			var camera = waterCamera.GetComponent<Camera>();

			PropertyField("geometryType", "Water Geometry");

			PropertyField("renderWaterDepth", "Render Water Depth");
			PropertyField("renderVolumes", "Render Volumes");

			PropertyField("sharedCommandBuffers", "Shared Command Buffers");
			PropertyField("baseEffectsQuality", "Base Effects Quality");

			if(camera.farClipPlane < 100.0f)
				EditorGUILayout.HelpBox("Your camera farClipPlane is set below 100 units. It may be too low for the underwater effects to \"see\" the max depth and they may produce some artifacts.", MessageType.Warning, true);

			serializedObject.ApplyModifiedProperties();
		}

		/*private void DisplayTexturesInspector()
		{
			var waterCamera = (WaterCamera)target;
		}

		private List<WaterMap> GetWaterMaps()
		{
			var camera = (WaterCamera)target;
			var textures = new List<WaterMap>();

			textures.Add(new WaterMap("WaterCamera - SubtractiveMask", () => camera.SubtractiveMask));

			return textures;
		}*/
	}
}