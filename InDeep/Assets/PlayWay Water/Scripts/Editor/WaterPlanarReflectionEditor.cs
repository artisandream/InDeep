using UnityEditor;

namespace PlayWay.Water
{
	[CustomEditor(typeof(WaterPlanarReflection))]
	public class WaterPlanarReflectionEditor : WaterEditorBase
	{
		public override void OnInspectorGUI()
		{
			UpdateGUI();
			
			PropertyField("reflectSkybox", "Reflect Skybox");
			PropertyField("reflectionMask", "Reflection Mask");
			PropertyField("downsample", "Downsample");
			PropertyField("retinaDownsample", "Downsample (Retina)");

			//PropertyField("maxBlurDistance", "Blur Distance");
			//PropertyField("depthBlur", "Depth Blur");
			//PropertyField("nearBlur", "Near Blur");
			PropertyField("blur", "Blur");

			serializedObject.ApplyModifiedProperties();
		}
	}
}
