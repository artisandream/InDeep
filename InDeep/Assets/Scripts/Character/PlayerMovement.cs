using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour {

	Vector3 moveDirection;
	public float speed = 10;

	CharacterController cc;
	// Use this for initialization
	void Start () {
		cc = GetComponent<CharacterController> ();
	}

	// Update is called once per frame
	void Update () {
		moveDirection.z = Input.GetAxis("Vertical");
		transform.Rotate (0, Input.GetAxis("Horizontal"), 0);
		moveDirection = transform.TransformDirection (moveDirection*Time.deltaTime);
		cc.Move (moveDirection);
	}
}
