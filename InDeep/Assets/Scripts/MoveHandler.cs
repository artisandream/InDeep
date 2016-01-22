using UnityEngine;
using System.Collections;

public class MoveHandler : MonoBehaviour {

	IEnumerator StartThisMovement ()
	{
		if (transform.position.z >= 5)
			myEvent.MyEvent -= StartMovement;

		int i = 0;
		while (i < 20) {
			transform.Translate(0,0,0.1f);
			i++;
			yield return null;
		}
	}

	void StartMovement ()
	{
		StartCoroutine (StartThisMovement ());
	}

	// Use this for initialization
	void Start () {
		myEvent.MyEvent += StartMovement;
	}
}
