
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Lookout;


public class MyPlayerController : NetworkBehaviour {

	public const string DidStartLocalPlayer = "PlayerController.DidStartLocalPlayer";

	public Allegiance playerAllegiance;

	public string MapName{
		get{ 
			return mapName;
		}

		set{ 
			if (value != mapName) {
				mapName = value;
			}
		}
	}
	string mapName;

	BoardManager boardManager;
	EnqueueManager enqueueManager;
	HexGrid hexGrid;
	MyNetworkManagerUI myNetworkManagerUI;

	#region Listeners
	UnityAction<System.Object> requestEndTurnNotificationAction;
	UnityAction<System.Object> didEndGameNotificationAction;

	void Awake(){
		didEndGameNotificationAction = new UnityAction<System.Object> (EndGame);
	}

	void OnEnable(){
		EventManager.StartListening (Game.DidEndGameNotification, didEndGameNotificationAction);
	}

	void OnDisable(){
		EventManager.StopListening (Game.DidEndGameNotification, didEndGameNotificationAction);
	}

	#endregion

	void Start(){
		boardManager = FindObjectOfType<BoardManager> ();
		hexGrid = FindObjectOfType<HexGrid> ();
		enqueueManager = FindObjectOfType<EnqueueManager> ();
	}

	public void SetAllegiance(){

		if (isServer) {
			Debug.Log ("playerController is local player");
			playerAllegiance = Allegiance.CON;

		} else {
			Debug.Log ("playerController is not local player");
			playerAllegiance = Allegiance.USA;
		} 
	}


	public override void OnStartLocalPlayer(){

		boardManager = FindObjectOfType<BoardManager> ();
		hexGrid = FindObjectOfType<HexGrid> ();
		enqueueManager = FindObjectOfType<EnqueueManager> ();
		base.OnStartLocalPlayer ();
		SetAllegiance ();
		Debug.Log ("on start local player called and allegiance is " + playerAllegiance);

		if (!isServer) {
			Debug.Log ("and i'm not the server");
			CmdDefineStartPositions ();
		}	

		EventManager.TriggerEvent (DidStartLocalPlayer, this); //triggers didstart local player

	}


	/// <summary>
	/// Ends the turn and send all data across to both players. 
	/// </summary>
	public void EndTurn(){

		if (isLocalPlayer && boardManager.game.control == playerAllegiance) {

			boardManager.BoostAllCityUnits ();

			while (boardManager.allEvents.Count > 0) {
				//Debug.LogError ("endturn is called");


				LookoutEvent thisEvent = boardManager.allEvents.Dequeue ();

				if (thisEvent is MovementEvent) {
					//	Debug.LogError ("this event is movements");

					MovementEvent newMovement = (MovementEvent)thisEvent;
					int cellToMoveToID = newMovement.GetIDOfCellToMoveTo();
					int idOfUnitToMove = newMovement.GetIDOfUnitToMove();

					CmdSendMovementData (idOfUnitToMove, cellToMoveToID);	

				} else if (thisEvent is BattleEvent) {
					//Debug.LogError("this event is a battle");
					BattleEvent battle = (BattleEvent)thisEvent;
					int idOfAttackingUnit = battle.GetAttackerID ();
					int idOfDefendingUnit = battle.GetDefenderID ();
					int battleLocationID = battle.GetBattleLocationID ();
					int loserLosses = battle.GetLoserLosses ();
					int winnerLosses = battle.GetWinnerLosses ();
					Allegiance winner = battle.GetWinner ();
					float turn = battle.GetTurn ();
					BattleEvent.LossType lossType = battle.GetLossType ();
					BattleEvent.VictoryType victoryType = battle.GetVictoryType ();

					CmdSendBattleData (idOfAttackingUnit, idOfDefendingUnit, winner, battleLocationID, winnerLosses, loserLosses, turn, lossType, victoryType);
				} else if (thisEvent is MergeEvent) {
					MergeEvent thisMergeEvent = (MergeEvent)thisEvent;
					int idOfUnitThatOrderedTheMerge = thisMergeEvent.GetIDOfUnitThatOrderedTheMerge ();
					int idOfUnitToMergeWith = thisMergeEvent.GetUnitToMergeWith ();
					int newStrength = thisMergeEvent.GetNewStrength ();

					CmdSendMergeData (idOfUnitThatOrderedTheMerge, idOfUnitToMergeWith, newStrength);
				} else if(thisEvent is BoostEvent){
					BoostEvent thisBoostEvent = (BoostEvent)thisEvent;
					int idOfUnitToBoost = thisEvent.GetActionUnitID ();
					int newStrength = thisBoostEvent.GetNewStrength ();
					CmdSendBoostData (idOfUnitToBoost, newStrength);
				}


			}



			CmdEndTurn ();
		}
	}

	[Command]
	void CmdEndTurn(){
		RpcEndTurn();
	}

	[ClientRpc]
	void RpcEndTurn(){
		boardManager.ProcessStartOfTurnActions ();


	}

	void EndGame(object obj){
		Allegiance winner = (Allegiance)obj;
		CmdEndGame (winner);
	}

	[Command]
	void CmdEndGame(Allegiance winnerAllegiance){
		RpcEndGame (winnerAllegiance);
	}

	[ClientRpc]

	void RpcEndGame(Allegiance winnerAllegiance){
		UIManager uiManager = FindObjectOfType<UIManager> ();
		uiManager.RequestEndGame (winnerAllegiance);
	}


	[Command]
	void CmdDefineStartPositions(){
		
			RpcDefineStartPositions ();
	}

	[ClientRpc]
	void RpcDefineStartPositions(){
		boardManager.InitialGameSetup ();

	}

	#region Testing

	[Command]
	void CmdSendMovementData(int unitToMoveID, int cellToMoveToID){
		RpcSendMovementData (unitToMoveID, cellToMoveToID);
	}

	[ClientRpc] 
	void RpcSendMovementData(int unitToMoveID, int cellToMoveToID){
		if (boardManager.game.control != playerAllegiance) {
			//Debug.LogError ("please enqueue");
			enqueueManager.EnqueueMovement (unitToMoveID, cellToMoveToID);

		}
	}

	[Command]
	void CmdSendBattleData(int idOfAttackingUnit, int idOfDefendingUnit, Allegiance winner, int battleLocationID, int winnerLosses, int loserLosses, float turn, BattleEvent.LossType lossType, BattleEvent.VictoryType victoryType){
		RpcSendBattleData (idOfAttackingUnit, idOfDefendingUnit, winner, battleLocationID, winnerLosses, loserLosses, turn, lossType, victoryType);
	}

	[ClientRpc]
	void RpcSendBattleData(int idOfAttackingUnit, int idOfDefendingUnit,  Allegiance winner, int battleLocationID, int winnerLosses, int loserLosses, float turn, BattleEvent.LossType lossType, BattleEvent.VictoryType victoryType){		
		if (boardManager.game.control != playerAllegiance) {
			//Debug.LogError ("please enqueue");
			BattleEvent newBattle = new BattleEvent();
			newBattle.SetAttackerID(idOfAttackingUnit);
			newBattle.SetDefenderID(idOfDefendingUnit);
			newBattle.SetWinner (winner);
			newBattle.SetBattleLocation (hexGrid.cells [battleLocationID]);
			newBattle.SetLosses (winnerLosses, loserLosses);
			newBattle.SetTurn (turn);
			newBattle.SetLossType(lossType);
			newBattle.SetVictoryType(victoryType);
			enqueueManager.EnqueueBattle (newBattle);
		}
	}

	[Command]
	void CmdSendMergeData (int idOfUnitThatOrderedTheMerge, int idOfUnitToMergeWith, int newStrength){
		RpcSendMergeData (idOfUnitThatOrderedTheMerge, idOfUnitToMergeWith, newStrength);
	}

	[ClientRpc]
	void RpcSendMergeData (int idOfUnitThatOrderedTheMerge, int idOfUnitToMergeWith, int newStrength){
		if (boardManager.game.control != playerAllegiance) {
			enqueueManager.EnqueueMerge (idOfUnitThatOrderedTheMerge, idOfUnitToMergeWith, newStrength, 0);
		}
	}

	[Command]
	void CmdSendBoostData(int idOfUnitToBoost, int newStrength){
		RpcSendBoostData (idOfUnitToBoost, newStrength);
	}

	[ClientRpc]
	void RpcSendBoostData(int idOfUnitToBoost, int newStrength){
		if (boardManager.game.control != playerAllegiance) {
			enqueueManager.EnqueueBoost (idOfUnitToBoost, newStrength);
		}
	}
	#endregion

}
