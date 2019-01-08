using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class MyNetworkManagerUI : MonoBehaviour {

	private MyNetworkManager myNetworkManager;
	public GameObject networkPanel;
	public Text myGameName;
	public GameObject passwordField;
	private Text passwordText;
	private bool privateGame;
	private string thisGameName;


	//public GameObject matchListPanel;
	//public GameObject awaitingPlayerScreen;



	void Start(){
		myNetworkManager = FindObjectOfType<MyNetworkManager> ();
	}


	public void RequestStartHost(){
		if (privateGame && passwordText.text == "") {
			Debug.Log ("you have to enter a password");
		} else{

			if (myGameName.text == "") {
				thisGameName = "default";
			}

			Debug.Log ("Starting Host at " + Time.timeSinceLevelLoad + "game name is " + myGameName.text);
		
			myNetworkManager.StartMatchMaker ();
			thisGameName = myGameName.text;
			//myNetworkManager.matchMaker.CreateMatch (myGameName.text, 2, privateGame, passwordText.text, "", "", 0, 0, myNetworkManager.OnMatchCreated);
			myNetworkManager.matchMaker.CreateMatch (thisGameName, 2, true, "", "", "", 0, 0, myNetworkManager.OnMatchCreated); 
//		myNetworkManager.matchMaker.CreateMatch(myGameName, 2, privateGame, 
		}
	}

	public void TogglePrivateGame(){
		privateGame = (privateGame == true) ? false : true;

		ToggleFieldActive (passwordField);
	}

	public void ToggleFieldActive(GameObject inputField){
		bool trueOrFalse = (inputField.activeInHierarchy) ? false : true;

		inputField.SetActive (trueOrFalse);
	}

	public void ToggleHUDActive(GameObject hudToActivateOrDeactivate){
		bool trueOrFalse = (hudToActivateOrDeactivate.activeInHierarchy) ? false : true;

		hudToActivateOrDeactivate.SetActive (trueOrFalse);

	}

	private void HideActiveHUD(GameObject hudToDeactivate){
		if (hudToDeactivate.activeInHierarchy) {
			hudToDeactivate.SetActive (false);
		}
	}
	private void ShowInactiveHUD(GameObject hudToActivate){
		if (!hudToActivate.activeInHierarchy) {
			hudToActivate.SetActive(true);
		}
	}


//	public void ToggleNetworkHUDHide(){
//		ShowInactiveHUD(awaitingPlayerScreen);
//		HideActiveHUD (networkPanel);
//		HideActiveHUD (matchListPanel);
//
//	}
//
//	public void ToggleNetworkHUDShow(){
//		ShowInactiveHUD (networkPanel);
//		ShowInactiveHUD (matchListPanel);
//	}





//	public void ToggleHUDChangeOnPlayerOneJoined(){
//		Debug.Log ("friendly player joined");
//		ToggleHUDActive (awaitingPlayerScreen);
//		ToggleHUDActive (networkPanel);
//		ToggleHUDActive (matchListPanel);
//	}
//
//
//	public void ToggleHUDChangeOnPlayerTwoJoined(){
//		Debug.Log ("enemy player joined");
//		ToggleHUDActive (networkPanel);
//		ToggleHUDActive (matchListPanel);
//		UIManager uiManager = FindObjectOfType<UIManager> ();
//		uiManager.ToggleHUDsOnBothPlayersJoined ();
//
//	}


}
