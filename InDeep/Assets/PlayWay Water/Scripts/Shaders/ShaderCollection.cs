using UnityEngine;
using System.Linq;

/// <summary>
/// Stores references to materials with choosen keywords to include them in builds.
/// </summary>
public class ShaderCollection : ScriptableObject
{
	[SerializeField]
	private Material[] materials;

	public void AddShaderVariant(Shader shader, string[] keywords)
	{
#if UNITY_EDITOR
		if(!ContainsShaderVariant(shader, keywords))
		{
			System.Array.Resize(ref materials, materials.Length + 1);

			var material = new Material(shader);
			material.name = string.Join(" ", keywords);
			material.shaderKeywords = keywords;
			materials[materials.Length - 1] = material;

			UnityEditor.AssetDatabase.AddObjectToAsset(material, this);
			UnityEditor.EditorUtility.SetDirty(this);

			//UnityEditor.AssetDatabase.SaveAssets();
			//UnityEditor.AssetDatabase.Refresh();
		}
#endif
	}

	public void Clear()
	{
#if UNITY_EDITOR
		foreach(var material in materials)
			DestroyImmediate(material, true);

		materials = new Material[0];

		UnityEditor.EditorUtility.SetDirty(this);
#endif
	}

	public bool ContainsShaderVariant(Shader shader, string[] keywords)
	{
		if(materials == null)
			materials = new Material[0];

		foreach(var material in materials)
		{
			if(material.shader == shader && SameKeywords(material.shaderKeywords, keywords))
				return true;
		}

		return false;
	}

	private bool SameKeywords(string[] a, string[] b)
	{
		return !a.Except(b).Any() && !b.Except(a).Any();
	}
}
