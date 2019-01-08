using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.UI;


public class MyNetworkManager : NetworkManager  {

	private float nextRefreshTime;
	private bool searchForMatches = true;
	private LevelManager levelManager;

	private int playerCount;


	private void Update(){

		if(Time.time >= nextRefreshTime && searchForMatches){
			RefreshMatches ();
		}
	}

	public override void OnStartHost(){
		base.OnStartHost ();
	//	Debug.LogError ("Host Started at " + Time.timeSinceLevelLoad);

	}


//
//
//	public void MyStartClient(){
//		Debug.Log ("starting Host at " + Time.timeSinceLevelLoad);
//		StartClient ();
//
//	}
//
	public override void OnStartClient(NetworkClient myClient){
		base.OnStartClient (myClient);
		//Debug.Log ("Client start requested at " + Time.timeSinceLevelLoad);
		//InvokeRepeating ("PrintDots", 0f, 1f);
	}

	public override void OnClientConnect(NetworkConnection con){
		base.OnClientConnect (con);
		Debug.Log ("Client is connected to " + con.address + " at "  + Time.timeSinceLevelLoad);


		//CancelInvoke ();
		searchForMatches = false;

	}

//
	void PrintDots(){
		Debug.Log (".");
	 	
	}	


	#region matchregion

	public void MyStartHost(){
		Debug.Log ("Starting Host at " + Time.timeSinceLevelLoad);

		StartMatchMaker ();
		matchMaker.CreateMatch ("Charlie's Match", 2, true, "", "", "", 0, 0, OnMatchCreated); 
		searchForMatches = false;

		//myNetworkManagerUI.ToggleNetworkHUDHide ();
	}

	public void OnMatchCreated(bool success, string extendedInfo, MatchInfo responseData){
		base.StartHost (responseData);

		levelManager = FindObjectOfType<LevelManager> ();
		levelManager.LoadNextLevel ();
		//RefreshMatches ();
	}

	public void RefreshMatches(){
		nextRefreshTime = Time.time + 2f;
		if (matchMaker == null) {
			StartMatchMaker ();
		}

		//matchMaker.ListMatches (0, 10, "", true, 0, 0, HandleListMatchesComplete);
		matchMaker.ListMatches (0, 10, "", false, 0, 0, HandleListMatchesComplete);
	}

	private void HandleListMatchesComplete(bool success, string extendedInfo, List<MatchInfoSnapshot> responseData){
		AvailableMatchesList.HandleNewMatchList (responseData);
	}

	public void JoinMatch(MatchInfoSnapshot match, string password){
		if (matchMaker == null) {
			StartMatchMaker ();
		}

		//matchMaker.JoinMatch (match.networkId, "", "", "", 0, 0, HandleJoinedMatch);
		matchMaker.JoinMatch (match.networkId, password, "", "", 0, 0, HandleJoinedMatch);
		ServerChangeScene ("TestScene");


//		levelManager = FindObjectOfType<LevelManager> ();
//		levelManager.LoadLevel	 ("TestScene");
//

	}

	private void HandleJoinedMatch(bool success, string extendedinfo, MatchInfo responseData){
		StartClient (responseData);
	}

	public void DisconnectFromNetwork(){
		client.Disconnect ();
	}

	public override void OnServerAddPlayer (NetworkConnection conn, short playerControllerId)
	{
		base.OnServerAddPlayer (conn, playerControllerId);
		searchForMatches = false;
		if (playerCount > 0) {

			//then it is the client started.
			levelManager = FindObjectOfType<LevelManager> ();
			levelManager.LoadNextLevel ();

		}
		playerCount++;


	}




	#endregion




}
