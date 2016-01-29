using UnityEngine;
using System.Collections;

public class PlayerDetection : MonoBehaviour {

	[HideInInspector]
	public PatrolOrAttack enemyPatrolScript;	

	void Start () {
		enemyPatrolScript = transform.parent.GetComponent<PatrolOrAttack> ();
	}

	public virtual void OnTriggerEnter () {
		enemyPatrolScript.StopPatrolling ();
	}
}