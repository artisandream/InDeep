using UnityEngine;
using System.Collections;
using System;

public class CharacterHealthKiller : MonoBehaviour {

	public float ammoPower = 0.01f;

	public static Action<float> UpdateHealth;
	public static Action HealthHit;
	
	public void OnHit () {
		OnTriggerEnter();
	}
	
	void OnTriggerEnter ()
	{
		if(UpdateHealth != null){
			UpdateHealth(ammoPower);
			HealthHit();
		}
	}
}
