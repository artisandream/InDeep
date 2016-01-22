using UnityEngine;
using System.Collections;

public class EnemyActivateMelee : MonoBehaviour {

	public Animator eAnim;

	void OnTriggerEnter () {
		eAnim.SetBool ("Melee", true);
	}

	void OnTriggerExit () {
		eAnim.SetBool ("Melee", false);
	}

	void DeactivateThisCollider ()
	{
		gameObject.GetComponent<BoxCollider>().enabled = false;
		OnTriggerExit ();
	}


}
