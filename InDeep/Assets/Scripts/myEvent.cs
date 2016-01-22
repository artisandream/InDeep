using UnityEngine;
using System.Collections;
using System;

public class myEvent : MonoBehaviour {

	public static Action MyEvent;

	// Use this for initialization
	void OnTriggerEnter () {
		MyEvent ();
	}
}
