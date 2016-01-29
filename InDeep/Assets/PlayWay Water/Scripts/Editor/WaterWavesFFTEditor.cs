using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace PlayWay.Water
{
	[CustomEditor(typeof(WaterWavesFFT))]
	public class WaterWavesFFTEditor : WaterEditorBase
	{
		public override void OnInspectorGUI()
		{
			UpdateGUI();

			DrawRenderedMapsGUI();
			PropertyField("highQualitySlopeMaps");
			PropertyField("forcePixelShader");

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawRenderedMapsGUI()
		{
			SerializedProperty property = serializedObject.FindProperty("renderedMaps");

			var currentValue = (WaterWavesFFT.MapType)property.intValue;
            var newValue = (WaterWavesFFT.MapType)EditorGUILayout.EnumMaskField(new GUIContent(property.displayName, property.tooltip), currentValue);

			if(currentValue != newValue)
				property.intValue = (int)newValue;
		}
	}
}
