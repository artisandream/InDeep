using UnityEngine;

public class ScriptableObjectSingleton : ScriptableObject
{
	static protected T LoadSingleton<T>() where T : ScriptableObject
	{
		var instance = Resources.Load<T>(typeof(T).Name);

#if UNITY_EDITOR
		if(instance == null)
		{
			instance = ScriptableObject.CreateInstance<T>();

			string path = GetPackagePath(instance, "/Resources/" + typeof(T).Name + ".asset");

			UnityEditor.AssetDatabase.CreateAsset(instance, path);
		}
#endif

		return instance;
	}

#if UNITY_EDITOR
	static private string GetPackagePath(ScriptableObject so, string path)
	{
		var script = UnityEditor.MonoScript.FromScriptableObject(so);
		string p = UnityEditor.AssetDatabase.GetAssetPath(script);

		string dir = System.IO.Path.GetDirectoryName(p);

		while(!System.IO.Directory.Exists(dir + "/Scripts"))
			dir = System.IO.Path.GetDirectoryName(dir);

		return dir + path;
	}
#endif
}
