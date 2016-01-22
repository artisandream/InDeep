using UnityEngine;
using System.Collections;

public class EventHandler : MonoBehaviour {

	void MyEventHandler ()
	{
		Debug.Log ("I handled the event");
	}

	// Use this for initialization
	void Start () {
		myEvent.MyEvent += MyEventHandler;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
