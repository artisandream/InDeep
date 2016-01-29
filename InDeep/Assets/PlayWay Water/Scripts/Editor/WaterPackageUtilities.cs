using UnityEngine;
using UnityEditor;
using System.IO;

namespace PlayWay.Water
{
	/// <summary>
	/// Helps locating the PlayWay Water folder and find stuff in it.
	/// </summary>
	public class WaterPackageUtilities
	{
		static public string GetPackagePath(ScriptableObject so, string path)
		{
			var script = MonoScript.FromScriptableObject(so);
			string p = AssetDatabase.GetAssetPath(script);

			string dir = Path.GetDirectoryName(p);

			while(!Directory.Exists(dir + "/Scripts"))
				dir = Path.GetDirectoryName(dir);

			return dir + path;
		}
	}
}