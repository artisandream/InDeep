using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using System.Collections.Generic;
using System.Linq;

namespace PlayWay.Water
{
	[CustomEditor(typeof(Water))]
	public class WaterEditor : WaterEditorBase
	{
		private AnimBool environmentFoldout = new AnimBool(true);
		private AnimBool surfaceFoldout = new AnimBool(false);
		private AnimBool spectrumFoldout = new AnimBool(false);
		private AnimBool geometryFoldout = new AnimBool(false);
		private AnimBool inspectFoldout = new AnimBool(false);

		static private GUIContent[] resolutionLabels = new GUIContent[] { new GUIContent("32x32 (runs on potatos)"), new GUIContent("64x64"), new GUIContent("128x128"), new GUIContent("256x256 (medium; low-end PCs)"), new GUIContent("512x512"), new GUIContent("1024x1024 (very high; strong PCs)"), new GUIContent("2048x2048 (as seen in Titanic® and Water World®; gaming PCs)"), new GUIContent("4096x4096 (use at your own responsibility)") };
		static private int[] resolutions = new int[] { 32, 64, 128, 256, 512, 1024, 2048, 4096 };
		private GUIStyle boldLabel;
		private GUIStyle textureBox;

		private int selectedMapIndex = -1;
		private float inspectMinValue = 0.0f;
		private float inspectMaxValue = 1.0f;
		private Material inspectMaterial;

		override protected void UpdateStyles()
		{
			base.UpdateStyles();

			if(boldLabel == null)
			{
				boldLabel = new GUIStyle(GUI.skin.label);
				boldLabel.fontStyle = FontStyle.Bold;
            }

			if(textureBox == null)
			{
				textureBox = new GUIStyle(GUI.skin.box);
				textureBox.alignment = TextAnchor.MiddleCenter;
				textureBox.fontStyle = FontStyle.Bold;
				textureBox.normal.textColor = EditorStyles.boldLabel.normal.textColor;
				textureBox.active.textColor = EditorStyles.boldLabel.normal.textColor;
				textureBox.focused.textColor = EditorStyles.boldLabel.normal.textColor;
			}
		}

		public override void OnInspectorGUI()
		{
			var water = target as Water;

			UpdateGUI();
			
			GUILayout.Space(4);
			
			PropertyField("profile");

			if(water.ShaderCollection == null)
			{
				if(Event.current.type == EventType.Layout)
					SearchShaderVariantCollection();

				if(water.ShaderCollection == null)
				{
					EditorGUILayout.HelpBox("Each scene with water needs one unique asset file somewhere in your project. This file will contain materials and baked data.\nYou may safely ignore this in editor, but produced builds may not contain necessary shaders.", MessageType.Warning, true);

					EditorGUILayout.BeginHorizontal();
					{
						GUILayout.FlexibleSpace();

						if(GUILayout.Button("Save Asset..."))
						{
							string path = EditorUtility.SaveFilePanelInProject("Save Water Assets...", water.name, "asset", "");

							if(!string.IsNullOrEmpty(path))
							{
								var shaderCollection = new ShaderCollection();

								AssetDatabase.CreateAsset(shaderCollection, path);

								AssetDatabase.SaveAssets();
								AssetDatabase.Refresh();

								serializedObject.FindProperty("shaderCollection").objectReferenceValue = shaderCollection;
								serializedObject.FindProperty("namesHash").intValue = GetNamesHash();
                            }
						}

						EditorGUILayout.EndHorizontal();
					}
				}
			}
			
			if(BeginGroup("Environment", environmentFoldout))
			{
				PropertyField("blendEdges");
				PropertyField("volumetricLighting");
				PropertyField("receiveShadows");
				PropertyField("shadowCastingMode");

				//DrawReflectionProbeModeGUI();
				
				PropertyField("seed");
				PropertyField("windDirectionPointer");
				SubPropertyField("volume", "boundless", "Boundless");
			}

			EndGroup();

			if(BeginGroup("Geometry", geometryFoldout))
			{
				SubPropertyField("geometry", "type", "Type");
				
				SubPropertyField("geometry", "baseVertexCount", "Vertices");
				SubPropertyField("geometry", "tesselatedBaseVertexCount", "Vertices (Tesselation)");
				SubPropertyField("geometry", "customMeshes", "Custom Meshes");
			}

			EndGroup();

			if(BeginGroup("Spectrum", spectrumFoldout))
			{
				DrawResolutionGUI();
				SubPropertyField("spectraRenderer", "highPrecision", "High Precision");
				SubPropertyField("spectraRenderer", "cpuWaveThreshold", "Wave Threshold (CPU)");
				SubPropertyField("spectraRenderer", "cpuMaxWaves", "Max Waves (CPU)");
			}

			EndGroup();

			if(BeginGroup("Shading", surfaceFoldout))
			{
				//PropertyField("autoDepthColor", "Auto Depth Color");
				SubPropertyField("waterPrecompute", "computeSlopeVariance", "Compute Slope Variance");
				
				PropertyField("refraction", "Refraction");
				PropertyField("tesselationFactor", "Tesselation Factor");
			}

			EndGroup();

			if(BeginGroup("Inspect", inspectFoldout))
			{
				var maps = GetWaterMaps();
				selectedMapIndex = EditorGUILayout.Popup("Texture", selectedMapIndex, maps.Select(m => m.name).ToArray());

				if(selectedMapIndex >= 0 && selectedMapIndex < maps.Count)
				{
					var texture = maps[selectedMapIndex].getTexture();

					GUILayout.BeginHorizontal();
					{
						GUILayout.FlexibleSpace();
						GUILayout.Box(texture != null ? "" : "NOT AVAILABLE", textureBox, GUILayout.Width(Screen.width * 0.85f), GUILayout.Height(Screen.width * 0.85f));
						Rect texRect = GUILayoutUtility.GetLastRect();

						if(texture != null && Event.current.type == EventType.Repaint)
						{
							if(inspectMaterial == null)
							{
								inspectMaterial = new Material(Shader.Find("PlayWay Water/Editor/Inspect Texture"));
								inspectMaterial.hideFlags = HideFlags.DontSave;
                            }

							inspectMaterial.SetVector("_RangeR", new Vector4(inspectMinValue, 1.0f / (inspectMaxValue - inspectMinValue)));
							inspectMaterial.SetVector("_RangeG", new Vector4(inspectMinValue, 1.0f / (inspectMaxValue - inspectMinValue)));
							inspectMaterial.SetVector("_RangeB", new Vector4(inspectMinValue, 1.0f / (inspectMaxValue - inspectMinValue)));
							Graphics.DrawTexture(texRect, texture, inspectMaterial);
							Repaint();
						}

						GUILayout.FlexibleSpace();
					}

					GUILayout.EndHorizontal();

					EditorGUILayout.MinMaxSlider(ref inspectMinValue, ref inspectMaxValue, 0.0f, 1.0f);
				}
			}

			EndGroup();

			GUILayout.Space(10);
			DrawFeatureSelector();
			GUILayout.Space(10);

			serializedObject.ApplyModifiedProperties();
		}
		
		private void DrawFoamToggle(Material material)
		{
			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Space(28);

				GUI.enabled = false;
				EditorGUILayout.Toggle("Enabled", material.IsKeywordEnabled("_WATER_FOAM_LOCAL") || material.IsKeywordEnabled("_WATER_FOAM_WS"));
				//PropertyField("displayFoam", "Enabled", 28);
				GUI.enabled = true;

				EditorGUILayout.EndHorizontal();
			}
		}

		private void DrawFeatureSelector()
		{
			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();

				//var components = new GUIContent[] { new GUIContent("Add feature..."), new GUIContent("Planar Reflection") };
				//EditorGUILayout.Popup(0, components, GUI.skin.button, GUILayout.Width(100));

				if(GUILayout.Button("Add feature...", GUILayout.Width(120)))
				{
					var menu = new GenericMenu();

					AddMenuItem(menu, "FFT Waves (High Quality)", typeof(WaterWavesFFT));
					AddMenuItem(menu, "Planar Reflections", typeof(WaterPlanarReflection));
					AddMenuItem(menu, "Foam", typeof(WaterFoam));
					AddMenuItem(menu, "Spray", typeof(WaterSpray));
					AddMenuItem(menu, "Normal Map Animation", typeof(WaterNormalMapAnimation));
					AddMenuItem(menu, "Network Water", typeof(NetworkWater));
					AddMenuItem(menu, "Gerstner Waves (Low Quality)", typeof(WaterWavesGerstner));
					
					menu.ShowAsContext();
				}

				GUILayout.FlexibleSpace();

				EditorGUILayout.EndHorizontal();
			}
		}

		private void AddMenuItem(GenericMenu menu, string label, System.Type type)
		{
			var water = (Water)target;
			
			if(water.GetComponent(type) == null)
			{
				menu.AddItem(new GUIContent(label), false, OnAddComponent, type);
			}
		}

		private void OnAddComponent(object componentTypeObj)
		{
			var water = (Water)target;
			water.gameObject.AddComponent((System.Type)componentTypeObj);
		}

		private void DrawReflectionProbeModeGUI()
		{
			GUI.enabled = PropertyField("useCubemapReflections").boolValue;

			var prop = serializedObject.FindProperty("reflectionProbeUsage");
			ReflectionProbeUsage val = (ReflectionProbeUsage)prop.intValue;
			val = (ReflectionProbeUsage)EditorGUILayout.EnumPopup("Reflection Probe Usage", val);
			prop.intValue = (int)val;

			GUI.enabled = true;
		}

		private void DrawResolutionGUI()
		{
			var property = serializedObject.FindProperty("spectraRenderer").FindPropertyRelative("resolution");

			DrawResolutionGUI(property);
        }

		static public void DrawResolutionGUI(SerializedProperty property, string name = null)
		{
			const string tooltip = "Higher values increase quality, but also decrease performance. Directly controls quality of waves, foam and spray.";
			
			int newResolution = IndexToResolution(EditorGUILayout.Popup(new GUIContent(name != null ? name : property.displayName, tooltip), ResolutionToIndex(property.intValue), resolutionLabels));

			if(newResolution != property.intValue)
				property.intValue = newResolution;
		}

		private List<WaterMap> GetWaterMaps()
		{
			var water = (Water)target;
			var textures = new List<WaterMap>();

			textures.Add(new WaterMap("Water.SpectraRenderer - Raw Omnidirectional Spectrum", () => water.SpectraRenderer.GetSpectrum(SpectraRenderer.SpectrumType.RawOmnidirectional)));
			textures.Add(new WaterMap("Water.SpectraRenderer - Raw Directional Spectrum", () => water.SpectraRenderer.GetSpectrum(SpectraRenderer.SpectrumType.RawDirectional)));
			textures.Add(new WaterMap("Water.SpectraRenderer - Height Spectrum", () => water.SpectraRenderer.GetSpectrum(SpectraRenderer.SpectrumType.Height)));
			textures.Add(new WaterMap("Water.SpectraRenderer - Slope Spectrum", () => water.SpectraRenderer.GetSpectrum(SpectraRenderer.SpectrumType.Slope)));
			textures.Add(new WaterMap("Water.SpectraRenderer - Horizontal Displacement Spectrum", () => water.SpectraRenderer.GetSpectrum(SpectraRenderer.SpectrumType.Displacement)));

			var wavesFFT = water.GetComponent<WaterWavesFFT>();
			textures.Add(new WaterMap("WaterWavesFFT - Height Map", () => wavesFFT != null ? wavesFFT.HeightMap : null));
			textures.Add(new WaterMap("WaterWavesFFT - Slope Map", () => wavesFFT != null ? wavesFFT.SlopeMap : null));
			textures.Add(new WaterMap("WaterWavesFFT - Horizontal Displacement Map", () => wavesFFT != null ? wavesFFT.DisplacementMap : null));
			//textures.Add(new WaterMap("WaterWavesFFT - Horizontal Displacement Map Jacobian", wavesFFT != null && wavesFFT.HasDisplacementMapJacobian ? wavesFFT.DisplacementMapJacobian : null));

			var foam = water.GetComponent<WaterFoam>();
			textures.Add(new WaterMap("WaterFoam - Global Foam Map", () => foam != null ? foam.GlobalFoamMap : null));
			textures.Add(new WaterMap("WaterFoam - Local Foam Map", () => foam != null ? foam.LocalFoamMap : null));

			return textures;
		}
		
		static int ResolutionToIndex(int resolution)
		{
			switch(resolution)
			{
				case 32: return 0;
				case 64: return 1;
				case 128: return 2;
				case 256: return 3;
				case 512: return 4;
				case 1024: return 5;
				case 2048: return 6;
				case 4096: return 7;
			}

			return 0;
		}

		static int IndexToResolution(int index)
		{
			return resolutions[index];
		}

		private void SearchShaderVariantCollection()
		{
			var editedWater = (Water)target;
			var transforms = FindObjectsOfType<Transform>();

			foreach(var root in transforms)
			{
				if(root.parent == null)		// if that's really a root
				{
					var waters = root.GetComponentsInChildren<Water>(true);
					
					foreach(var water in waters)
					{
						if(water != editedWater && water.ShaderCollection != null)
						{
							serializedObject.FindProperty("shaderCollection").objectReferenceValue = water.ShaderCollection;
							serializedObject.FindProperty("namesHash").intValue = GetNamesHash();
							serializedObject.ApplyModifiedProperties();
							return;
						}
					}
				}
			}
		}

		private int GetNamesHash()
		{
			var md5 = System.Security.Cryptography.MD5.Create();
			var hash = md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(EditorApplication.currentScene + "#" + target.name));
			return System.BitConverter.ToInt32(hash, 0);
		}

		enum ReflectionProbeUsage
		{
			Skybox = 0,
			BlendProbes = 1,
			BlendProbesAndSkybox = 2,
			Simple = 3,
		}

		struct WaterMap
		{
			public string name;
			public System.Func<Texture> getTexture;

			public WaterMap(string name, System.Func<Texture> getTexture)
			{
				this.name = name;
				this.getTexture = getTexture;
			}
		}
	}
}
