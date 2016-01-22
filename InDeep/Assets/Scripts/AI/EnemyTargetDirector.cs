using UnityEngine;
using System.Collections;

public class EnemyTargetDirector : MonoBehaviour
{
	//locates and moves a target to the player

	public float smoothing = 1.0F;//the rate as wich it follows the player
	public Vector3 startPosition;//the start postion of the target

	void StartToNextPosition(Transform _playerTarget)
	{
		StartCoroutine(SetNewPosition(_playerTarget));//updates without an update invocation
	}


	IEnumerator SetNewPosition(Transform _playerTarget)//updates the position while active
	{
		while (Vector3.Distance(transform.position, _playerTarget.position) > 0.1f) {
			var step = smoothing * Time.deltaTime;
			transform.position = Vector3.Lerp(transform.position, _playerTarget.position, step);
			yield return null;
		}
	}
}
