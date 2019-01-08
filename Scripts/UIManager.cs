using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Lookout;
using Prototype.NetworkLobby;


public class UIManager : MonoBehaviour {


	private BoardManager boardManager;
	private LevelManager levelManager;
	private HexGrid hexGrid;



	public Button endTurnButton;
	public Text controlText;
	public GameObject unitHUD;
	public GameObject minimapHUD;
	public GameObject turnHUD;
	public GameObject topRightHUD;
	public GameObject menuPanel;




	#region yesOrNo
	public GameObject promptUserChoiceHUD;
	public Text promptUserText;
	public Button optionA;
	public Button optionB;

	private HexSelect hexSelect;
	#endregion

	public GameObject basicInfoPopup;

	#region Listeners
	private UnityAction<System.Object> changeTurnNotificationAction;
		

	void Awake(){
		changeTurnNotificationAction = new UnityAction<System.Object> (ChangeUIAfterTurnChange);

	}

	void OnEnable(){
		if (FindObjectOfType<BoardManager> ()) {
			EventManager.StartListening (Game.DidChangeTurnNotification, changeTurnNotificationAction);
		}
	}

	void OnDisable(){
		if(FindObjectOfType<BoardManager>()){
		EventManager.StopListening (Game.DidChangeTurnNotification, changeTurnNotificationAction);
		}
	}

	#endregion
	// Use this for initialization
	void Start () {
		boardManager = FindObjectOfType<BoardManager> ();	
		hexSelect = FindObjectOfType<HexSelect> ();
		levelManager = FindObjectOfType<LevelManager> ();
		hexGrid = FindObjectOfType<HexGrid> ();
		hexSelect.AllowInput ();

		if (boardManager) {
			if (boardManager.playerController && endTurnButton) {
				string changeTurnButtonText = (boardManager.game.control == boardManager.playerController.playerAllegiance) ? "Enemy Turn..." : "End Turn";
				endTurnButton.GetComponentInChildren<Text> ().text = changeTurnButtonText;

			}
		}
	
	}
	
	void Update () {
		if (boardManager) {
			controlText.text = boardManager.game.control.ToString ();
		}
	}

	/// <summary>
	/// Triggers an event to request ending the turn. PlayerController sends this request to all clients, so that they all change turn, if the player is taking their turn.
	/// </summary>
	public void RequestEndTurn(){
		Debug.Log ("requeset end turn");

		if (boardManager.game.control == boardManager.playerController.playerAllegiance && endTurnButton.GetComponentInChildren<Text>().text != "Showing Enemy Moves") {
			boardManager.ProcessEndOfTurnActionsAndEndTurn ();
		} else {
			StartCoroutine(MakeTextFlashRed(endTurnButton.GetComponentInChildren<Text>())); 
		}

	}

	/// <summary>
	/// Changes the user interface after turn change (i.e. shows who's turn it is etc);
	/// </summary>
	/// <param name="obj">Object.</param>
	void ChangeUIAfterTurnChange(object obj){
		//change the change turn text;
		string changeTurnButtonText = (boardManager.game.control == boardManager.playerController.playerAllegiance) ? "Showing Enemy Moves" : "Processing Enemy Turn...";
		endTurnButton.GetComponentInChildren<Text> ().text = changeTurnButtonText;
	}


	/// <summary>
	/// Changes the end turn button text.
	/// </summary>
	/// <param name="textToChangeTo">Text to change to.</param>
	public void ChangeEndTurnButton(string textToChangeTo){
		endTurnButton.GetComponentInChildren<Text> ().text = textToChangeTo;
	}


	/// <summary>
	/// Used to make certain text flast red for a secnd.
	/// </summary>
	/// <returns>ienumerator</returns>
	/// <param name="text">Text to flash.</param>
	public IEnumerator MakeTextFlashRed(Text text){
		text.color = Color.red;
		yield return new WaitForSeconds (.3f);
		text.color = Color.black;
	}

	/// <summary>
	/// Toggles whether the HUD is active or not.
	/// </summary>
	/// <param name="hudToActivateOrDeactivate">Hud to activate or deactivate.</param>
	public void ToggleHUDActive(GameObject hudToActivateOrDeactivate){
		bool trueOrFalse = (hudToActivateOrDeactivate.activeInHierarchy) ? false : true;

		hudToActivateOrDeactivate.SetActive (trueOrFalse);

	}

	#region Testing

	/// <summary>
	/// Allows you to prompt the user for a choice betwen two options
	/// </summary>
	/// <param name="question">The question or choice you want to offer them.</param>
	/// <param name="optionAAction">Option A action.</param>
	/// <param name="optionBAction">Option B action.</param>
	public void PromptUserChoice(string question, UnityAction optionAAction, UnityAction optionBAction, string optionAText, string optionBText){

		promptUserText.text = question;
		hexSelect.PreventInput ();
		optionA.GetComponentInChildren<Text> ().text = optionAText;
		optionB.GetComponentInChildren<Text> ().text = optionBText;

		ToggleHUDActive (promptUserChoiceHUD);

		optionA.onClick.RemoveAllListeners ();
		optionB.onClick.RemoveAllListeners ();

		optionA.onClick.AddListener (optionAAction);
		optionB.onClick.AddListener (optionBAction);
	}



	/// <summary>
	/// HIdes the HUD and does nothing when the button is pushed. Acts on promptUserChoiceHUD
	/// </summary>
	public void CancelInput(){
		ToggleHUDActive (promptUserChoiceHUD);
		hexSelect.AllowInput ();
	}


	/// <summary>
	/// Toggles the popup info hud (i.e. the hud that has no option but okay
	/// </summary>
	/// <returns><c>true</c> if this instance cancel basic info HU; otherwise, <c>false</c>.</returns>
	public void CancelBasicInfoHUD(){
		ToggleHUDActive (basicInfoPopup);
		hexSelect.AllowInput ();
	}



	/// <summary>
	/// Requests that a unit be split
	/// </summary>
	public void RequestSplitUnit(){
	
		Debug.Log ("uiController.RequestSplitUnit");
		Unit thisUnit = hexSelect.selectedUnitGameobject.GetComponent<Unit> ();
	
		if (thisUnit.strength < 1500) {
			SetBasicInfoText ("Your unit is too small to split", "okey-dokey");
			ToggleHUDActive (basicInfoPopup);
		} else if (thisUnit.movementPointsRemaining < 1) {
			SetBasicInfoText ("Not enough moves to split, sir", "Darn");
			ToggleHUDActive (basicInfoPopup);
		} else {
		
			List<int> cellsAroundThisUnit = hexGrid.FindAllHexCellsWithinNSteps (1, thisUnit.unitLocation);
			List<int> freeCells = new List<int> ();

			foreach (int i in cellsAroundThisUnit) {
				if (hexSelect.CheckIfCellEmpty (i)) {
					freeCells.Add (i);
				}

			}
			GameObject unitToSplit = hexSelect.selectedUnitGameobject;

			hexSelect.DeselectUnit ();

			if (freeCells.Count > 0) {
				int randCell = freeCells[Mathf.RoundToInt(Random.Range (0, freeCells.Count))];

				//there is a free cell around the unit, so ask if they want to split
				string question = "Do you want to split this unit in half?";
				UnityAction yesAction = new UnityAction (() => {
					SplitAction (randCell, unitToSplit);
				});

				UnityAction noAction = new UnityAction (() => {
					CancelSplit();
				});

				PromptUserChoice (question, yesAction, noAction, "Yes", "No");
			} else{
				SetBasicInfoText ("Sorry, there is nowhere that your unit can go", "okie-dokie");

				ToggleHUDActive (basicInfoPopup);
			}
		} 
				

	}

	/// <summary>
	/// Action that occurs when a split is requested
	/// </summary>
	/// <param name="randomCellID">ID of a cell that is randomly picked around the unit.</param>
	/// <param name="unitToSplit">Unit to split.</param>
	private void SplitAction(int randomCellID, GameObject unitToSplit){
		Debug.Log("Split unit ");
		ToggleHUDActive (promptUserChoiceHUD);
		unitToSplit.GetComponent<Unit> ().SplitUnit (randomCellID);
		hexSelect.AllowInput ();
	}


	/// <summary>
	/// Action to take when cancel splitting is called. 
	/// </summary>
	/// <returns><c>true</c> if this instance cancel split; otherwise, <c>false</c>.</returns>
	private void CancelSplit(){
		CancelInput ();
		hexSelect.AllowInput ();
	}

	/// <summary>
	/// Sets the text of a basic info popup that informs the user of something.
	/// </summary>
	/// <param name="descritionToChange">Descrition to change.</param>
	/// <param name="buttonTextToChange">Button text to change.</param>
	public void SetBasicInfoText(string descritionToChange, string buttonTextToChange){
		Text description = basicInfoPopup.GetComponentInChildren<Text> ();
		Text buttonText = basicInfoPopup.GetComponentInChildren<Button> ().GetComponentInChildren<Text> ();

		description.text = descritionToChange;
		buttonText.text = buttonTextToChange;
	}



	/// <summary>
	/// Requests the quit game.
	/// </summary>
	public void RequestQuitGame(){
		ToggleHUDActive (menuPanel);
		levelManager.QuitRequest ();
	}

	public	void RequestEndGame (Allegiance winner){

		UnityAction yesAction = new UnityAction (() => {
			RequestExitToMainMenu();
		});

		UnityAction noAction = new UnityAction (() => {
			RequestQuitGame();
		});


		PromptUserChoice ("Game over! Winner was " + winner + "! Do you want to return to Game Lobby or quit?", yesAction, noAction, "Return to lobby", "Quit Game");

	}

	/// <summary>
	/// Requests the exit to main menu.
	/// </summary>
	public void RequestExitToMainMenu(){
		ToggleHUDActive (menuPanel);
		if (LobbyManager.s_Singleton) {
			LobbyManager.s_Singleton.ServerReturnToLobby ();
			levelManager.LoadLevel ("LobbyScene");

		} else {
			levelManager.LoadLevel ("Menu");
		}
	}
		

	public void CancelExitToMainMenu(){
		ToggleHUDActive (menuPanel);
	}
		#endregion
}
