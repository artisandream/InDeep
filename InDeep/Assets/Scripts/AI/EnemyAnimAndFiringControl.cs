using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyAnimAndFiringControl : MonoBehaviour {
	
	private int i = 0;
	public Animator EnemyAnimation;
	public Transform ammoStartingPoint;
	public float firingRate = 1f;

	public void CallFireAnim (string peram, bool state)
	{
		EnemyAnimation.SetBool (peram, state);
	}


	void OnDisable () {
		StopAllCoroutines();
	}

}
