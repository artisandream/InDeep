using UnityEngine;
using System.Collections;
using System;

public class FuncScript : MonoBehaviour {

	public static Func<int, int> myFunc;

	int ammo = 10;

	// Use this for initialization
	void Start () {
		ammo += myFunc (ammo);
		print (ammo);
		ammo += myFunc (ammo);
		print (ammo);
	}
}
