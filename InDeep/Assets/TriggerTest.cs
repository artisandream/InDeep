using UnityEngine;
using System.Collections;

public class TriggerTest : MonoBehaviour {

	void OnTriggerEnter (Collider _c) {
		print ("Hit "+ _c.name);
	}
}
