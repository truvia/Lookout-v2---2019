using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnqueueManager : MonoBehaviour {

	private BoardManager boardManager;
	private HexSelect hexSelect;
	private HexGrid hexGrid;
	private UIManager uiManager;

	// Use this for initialization
	void Start () {
		boardManager = FindObjectOfType<BoardManager> ();
		hexSelect = FindObjectOfType<HexSelect> ();
		hexGrid = FindObjectOfType<HexGrid> ();
		uiManager = FindObjectOfType<UIManager> ();
	}
	
	/// <summary>
	/// Enqueues the instruction to MOVE a unit from its current location to the new location. This system is used to transfer player A's moves across the network to player B in sequence (N.B. seperate instruction for battles or merges etc.)
	/// </summary>
	/// <param name="unitToMoveID">ID of the unit to move</param>
	/// <param name="cellToMoveToID">ID of the cell to move to.</param>
	public void EnqueueMovement(int unitToMoveID, int cellToMoveToID){
		//Debug.LogError ("Enqueue: Unit To move: " + unitToMoveID + " , and cell to move to: " + cellToMoveToID);


		MovementEvent newMovement = new MovementEvent ();

		newMovement.SetIDOfUnitToMove(unitToMoveID);
		newMovement.SetIDOfCellToMoveTo(cellToMoveToID);

		boardManager.allEvents.Enqueue (newMovement);

	}


	public void EnqueueBattle(BattleEvent battle ){
		//Debug.Log ("Enqueue: Battle between unit " + battle.GetAttackerID () + " and defender " + battle.GetDefenderID () + " at location: " + battle.GetBattleLocationID () + ". winner is " + battle.GetWinner());
		boardManager.allEvents.Enqueue (battle);
	}

	public void EnqueueMerge(int unitThatOrderedMergeID, int unitToMergeWithID, int unitThatOrderedMergeStrength, int unitToMergeWithStrength){
		//Debug.Log ("Enqueue: Merge units " + unitThatOrderedMergeID + " and unit " + unitToMergeWithID);
		MergeEvent newMergeEvent = new MergeEvent ();
		newMergeEvent.SetTheIDOfUnitThatOrderedTheMerge (unitThatOrderedMergeID);
		newMergeEvent.SetUnitToMergeWith (unitToMergeWithID);
		newMergeEvent.SetNewStrength (unitThatOrderedMergeStrength, unitToMergeWithStrength);

		boardManager.allEvents.Enqueue (newMergeEvent);
	}


	public IEnumerator DequeueLookoutEvents(){

		if (boardManager.game.control == boardManager.playerController.playerAllegiance) {
			//Debug.LogError ("dequeue is called");
			hexSelect.PreventInput ();

			while (boardManager.allEvents.Count > 0) {
				LookoutEvent thisEvent = boardManager.allEvents.Dequeue ();

				if (thisEvent is MovementEvent) {
					//	Debug.Log (allEvents.Count);
					MovementEvent thisMovement = (MovementEvent)thisEvent;

					HexCell destinationCell = hexGrid.cells [thisMovement.GetIDOfCellToMoveTo ()];
					GameObject unitToMove = boardManager.FindUnitByID (thisMovement.GetIDOfUnitToMove ());
					//Debug.Log ("Dequeue: unit to move is " + thisMovement.actionUnitID + " and to move to is: " + thisMovement.cellToMoveToID);

					if (unitToMove) {
						yield return StartCoroutine ((unitToMove.GetComponent<Unit> ().MoveTo (destinationCell)));
					}

				} else if (thisEvent is BattleEvent) {
					BattleEvent thisBattle = (BattleEvent)thisEvent;
					Unit attacker = boardManager.FindUnitByID (thisBattle.GetAttackerID ()).GetComponent<Unit> ();
					//HexCell battleLocation = hexGrid.cells [thisBattle.GetBattleLocationID()];
					Unit defender = boardManager.FindUnitByID (thisBattle.GetDefenderID ()).GetComponent<Unit> ();



					if (thisBattle.GetWinner () == attacker.allegiance) {
						//attacker won, so destroy loser
						attacker.ChangeStrength (attacker.strength - thisBattle.GetWinnerLosses ());
						Instantiate (attacker.battleSmoke, attacker.gameObject.transform.position, Quaternion.identity);
						Destroy (defender.gameObject);

					} else if (thisBattle.GetWinner () == defender.allegiance) {
						defender.ChangeStrength (defender.strength - thisBattle.GetWinnerLosses ());
						Instantiate (defender.battleSmoke, defender.gameObject.transform.position, Quaternion.identity);
						Destroy (attacker.gameObject);

					} else {
						//Neutral victory - this code below is wrong 
						attacker.ChangeStrength (attacker.strength - thisBattle.GetWinnerLosses ());
						defender.ChangeStrength (defender.strength - thisBattle.GetLoserLosses ());
						Instantiate (defender.battleSmoke, defender.gameObject.transform.position, Quaternion.identity);
					}

				} else if (thisEvent is MergeEvent) {
					MergeEvent thisMergeEvent = (MergeEvent)thisEvent;

					//Debug.LogError ("Unit that called the merge is " + thisMergeEvent.GetIDOfUnitThatOrderedTheMerge ());
					//Debug.LogError ("Unit to merge with is called " + thisMergeEvent.GetUnitToMergeWith ());

					Unit unitThatCalledTheMerge = boardManager.FindUnitByID (thisMergeEvent.GetIDOfUnitThatOrderedTheMerge ()).GetComponent<Unit> ();

					//Unit mergingUnit = boardManager.FindUnitByID (thisMergeEvent.GetIDOfUnitThatOrderedTheMerge()).GetComponent<Unit>();
					GameObject unitToMergeWith = boardManager.FindUnitByID (thisMergeEvent.GetUnitToMergeWith ());
					unitThatCalledTheMerge.MergeWithUnit (unitToMergeWith);

				} else if (thisEvent is BoostEvent) {
					BoostEvent boostEvent = (BoostEvent)thisEvent;

					Unit unitToBoost = boardManager.FindUnitByID (thisEvent.GetActionUnitID ()).GetComponent<Unit>();
					unitToBoost.ChangeStrength (boostEvent.GetNewStrength ());

				}

			}

			uiManager.ChangeEndTurnButton ("End Turn");
			hexSelect.AllowInput ();
		}

	}

	public void EnqueueBoost(int idOfUnitToBoost, int newStrength){
		BoostEvent newBoostEvent = new BoostEvent ();

		newBoostEvent.SetNewStrength (newStrength);
		newBoostEvent.SetActionUnitID (idOfUnitToBoost);

		boardManager.allEvents.Enqueue (newBoostEvent);
	}

}
