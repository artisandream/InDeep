using UnityEngine;
using System.Collections;

public class PlayerDetection : MonoBehaviour {
	
	public PatrolOrAttack enemyPatrolScript;

	void OnTriggerEnter () {
		enemyPatrolScript.StopPatrolling ();
	}
}