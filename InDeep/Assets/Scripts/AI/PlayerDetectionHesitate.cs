using UnityEngine;
using System.Collections;

public class PlayerDetectionHesitate : PlayerDetection {

	public float timer = 5f;

	IEnumerator HesitateAndAttack ()
	{
		yield return new WaitForSeconds (timer);
		enemyPatrolScript.StopPatrolling ();
	}

	public override void OnTriggerEnter ()
	{
		StartCoroutine (HesitateAndAttack ());
		print ("Enter");
	}

	void OnTriggerExit () {
		StopAllCoroutines ();
		print ("Exit");
	}
}
