using UnityEngine;
using System.Collections;
using System;

public class FollowMe : MonoBehaviour {

	public static Action<Transform> ToFollow;

	void OnTriggerEnter () {
		ToFollow (transform);
	}
}
