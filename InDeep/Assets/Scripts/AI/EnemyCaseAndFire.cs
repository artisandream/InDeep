using UnityEngine;
using System.Collections;

public class EnemyCaseAndFire: MonoBehaviour {

	//added to the enemy forward trigger
	public EnemyAnimAndFiringControl enemyAnimSM;
	public EnemyController enemyController;//instance of EnemyNav script on another game Object


	void EndFiring ()
	{
		enemyAnimSM.CallFireAnim("Fire", false);
	}

	void OnTriggerEnter(Collider _c) {
		enemyAnimSM.CallFireAnim("Fire", true);
		enemyController.myTarget = _c.gameObject;//changes the navMeshAgent target to the player
	}

	void OnTriggerExit(Collider _c) {
		EndFiring();
		enemyController.myTarget = enemyController.gameObject;//changes the navMeshAgent target to itself
		enemyController.EndSwim ();
	}
	
	void OnTriggerStay (Collider _c) {
		enemyController.StartEnemyMove ();
	}

	void TurnOfComponents ()
	{
		this.GetComponent<BoxCollider>().enabled = false;
	}
	
	void Start() {

	}
}
