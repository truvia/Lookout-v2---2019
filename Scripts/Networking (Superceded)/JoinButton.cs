using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking.Match;

public class JoinButton : MonoBehaviour {

	public Text roomNameText;
	private MyNetworkManager myNetworkManager;
	private MatchInfoSnapshot match;

//	void Awake(){
//		roomNameText = GetComponentInChildren<Text> ();
//	}

	void Start(){
		myNetworkManager = FindObjectOfType<MyNetworkManager> ();	
	}

	public void Initialize(MatchInfoSnapshot match, Transform matchListPanelTransform){
		this.match = match;
		roomNameText.text = match.name;

	}

	public void JoinMatch(){
		myNetworkManager.JoinMatch (match, "");
	}
}
