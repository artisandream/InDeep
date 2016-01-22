using UnityEngine;
using System.Collections;

public class SetDestination : MonoBehaviour {
	

	public NavMeshAgent thisAgent;
	public Transform thisDestintion;


	// Use this for initialization
	void Start () {GetComponent<NavMeshAgent>();
		thisAgent = GetComponent<NavMeshAgent>();
	}
	
	// Update is called once per frame
	void Update () {
		thisAgent.destination = thisDestintion.position;
	}
}
