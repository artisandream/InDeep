using UnityEngine;
using UnityEditor;
using PlayWay.Water;

namespace PlayWay.WaterEditor
{
	[CustomEditor(typeof(WaterProjectSettings))]
	public class WaterProjectSettingsEditor : WaterEditorBase
	{
		public override void OnInspectorGUI()
		{
			var waterVolumesLayerProp = serializedObject.FindProperty("waterVolumesLayer");
			waterVolumesLayerProp.intValue = EditorGUILayout.LayerField(new GUIContent(waterVolumesLayerProp.displayName, waterVolumesLayerProp.tooltip), waterVolumesLayerProp.intValue);

			PropertyField("waterMasksEnabled");

			var waterMasksLayerProp = serializedObject.FindProperty("waterMasksLayer");
			waterMasksLayerProp.intValue = EditorGUILayout.LayerField(new GUIContent(waterMasksLayerProp.displayName, waterMasksLayerProp.tooltip), waterMasksLayerProp.intValue);

			serializedObject.ApplyModifiedProperties();
		}

		[MenuItem("Edit/Project Settings/Water")]
		static void OpenSettings()
		{
			var instance = WaterProjectSettings.Instance;

			Selection.activeObject = instance;
		}
	}
}
