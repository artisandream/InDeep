using UnityEngine;
using System.Collections;

public class EnemyMove : MonoBehaviour {
	 
	Transform playerTarget;
	public NavMeshAgent enemyAgent;

	void AddPlayer (Transform obj)
	{
		playerTarget = obj;
	}

	void Start () {
		FollowMe.ToFollow += AddPlayer;
	}

	void OnTriggerStay () {
		enemyAgent.destination = playerTarget.position;
	}
}
