using UnityEngine;
using System.Collections;
using System;

public class MoveUsingButtons : MonoBehaviour {

	public static Action<float> Forward;
	public static Action<float> Rotate;

	public enum directionOptions {
		Forward,
		Backward,
		Left,
		Right
	}

	public directionOptions direction;

	public void OnMouseDrag () {
		switch (direction) {
		case directionOptions.Forward:
			Forward(1);
			break;

		case directionOptions.Backward:
			Forward(-1);
			break;

		case directionOptions.Left:
			Rotate(-1);
			break;

		case directionOptions.Right:
			Rotate (1);
			break;
		}
	}
}
