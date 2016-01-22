using UnityEngine;
using System.Collections;

public class RotateOnCall : MonoBehaviour {

	int score = 0;

	IEnumerator StartThisRatation ()
	{
		score++;
		Debug.Log (score);

		if (score >= 10)
			myEvent.MyEvent -= StartRotate;

		yield return new WaitForSeconds (1);
		int i = 0;
		while (i<10) {
			Debug.Log ("I am rotating");
			transform.Rotate (20, 0, 0);
			i++;
			yield return null;
		}
	}

	void StartRotate ()
	{
		StartCoroutine (StartThisRatation ());
	}

	// Use this for initialization
	void Start () {
		myEvent.MyEvent += StartRotate;
	}
}
