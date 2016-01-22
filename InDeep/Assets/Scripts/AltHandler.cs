using UnityEngine;
using System.Collections;

public class AltHandler : MonoBehaviour {

	void AltHandlerEvent ()
	{
		Debug.Log ("I did it too");
	}

	// Use this for initialization
	void Start () {
		myEvent.MyEvent += AltHandlerEvent;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
