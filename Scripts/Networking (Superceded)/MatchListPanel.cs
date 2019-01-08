using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking.Match;

public class MatchListPanel : MonoBehaviour {

	public GameObject contentPrefab;

	private void Awake(){
		AvailableMatchesList.OnAvailableMatchesChanged += AvailableMatchesList_OnAvailableMatchesChanged;
	}

	void AvailableMatchesList_OnAvailableMatchesChanged (List<MatchInfoSnapshot> matches)
	{

			//ClearExistingButtons ();
		CreateNewJoinGameButtons (matches);
	}

	void ClearExistingButtons(){
		Debug.Log ("clear existing buttons called");

		Transform[] children = GetComponentsInChildren<Transform> ();

		foreach (Transform child in children) {
			if (child != transform) {
				Destroy (child.gameObject);
			}
		}
//
//		JoinButton[] buttons = GetComponentsInChildren<JoinButton> ();
//
//		foreach (JoinButton button in buttons) {
//			Destroy (button.gameObject);
//		}

	}

	void CreateNewJoinGameButtons(List<MatchInfoSnapshot> matches){

		foreach (MatchInfoSnapshot match in matches) {
			GameObject button = Instantiate (contentPrefab);
			button.transform.SetParent (transform);
			button.transform.localScale = Vector3.one;
			button.transform.localRotation = Quaternion.identity;
			button.transform.localPosition = Vector3.zero;
			button.GetComponentInChildren<JoinButton>().Initialize (match, this.gameObject.transform);
		}
	}
}
