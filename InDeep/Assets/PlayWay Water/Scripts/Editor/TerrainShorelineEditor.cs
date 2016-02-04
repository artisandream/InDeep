using UnityEngine;
using UnityEditor;

namespace PlayWay.Water
{
	[CustomEditor(typeof(TerrainShoreline))]
	public class TerrainShorelineEditor : WaterEditorBase
	{
		public override void OnInspectorGUI()
		{
			PropertyField("water");
			PropertyField("center");
			PropertyField("spawnPointsCount");
			PropertyField("extendShoreline");
			PropertyField("blur");

			DrawIntensityMask();

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawIntensityMask()
		{
			var target = (TerrainShoreline)this.target;

			GUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();
				GUILayout.Box(target.IntensityMask != null ? "" : "NOT AVAILABLE", GUILayout.Width(Screen.width * 0.85f), GUILayout.Height(Screen.width * 0.85f));
				Rect texRect = GUILayoutUtility.GetLastRect();

				if(target.IntensityMask != null && Event.current.type == EventType.Repaint)
				{
					Graphics.DrawTexture(texRect, target.IntensityMask);
					Repaint();
				}

				GUILayout.FlexibleSpace();
			}

			GUILayout.EndHorizontal();
		}
	}
}
