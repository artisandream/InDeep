using UnityEngine;
using System.Collections;

public class FuncListener : MonoBehaviour {

	// Use this for initialization
	void Start () {
		FuncScript.myFunc += HandleFunc;
	}

	int HandleFunc (int i)
	{
		print (i);
		return i+8;
	}

}
