using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleSmoke : MonoBehaviour {

	// Use this for initialization
	void Start () {
		StartCoroutine(DestroyAfterSecs(this.gameObject, 2.0f));
	}
	
	public IEnumerator DestroyAfterSecs(GameObject objectToDestroy, float waitforTime){
		Debug.Log ("Destroy after called" + objectToDestroy);

		yield return new WaitForSeconds (2);
		Debug.Log ("destroy after waitforseconds");

		Destroy (objectToDestroy);

	}

}
