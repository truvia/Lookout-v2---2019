using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lookout;
using UnityEngine.Events;

public class Unit : MonoBehaviour {

	public enum UnitType{
		Army,
		Fortress,
		Spy,
		None
	}


	public int id; //set by boardmanager
	public Allegiance allegiance; 
	public int strength;
	public UnitType unitType; 
	public HexCell unitLocation; //This is the cell that the unit is sitting in. Cell coordinates can be found through the cell (unitLocation.coordinates).

	//Movement section
	public int numberOfMoves; //units should usually have only one go per turn (promoted units might be given two?)
	public int movementRange; //how many cells the unit can traverse in a single turn;
	public float movementPointsRemaining; //how many cells the unit has left to traverse - goes down with each cell moved
	public List<HexCell> CellPossibleRoute = new List<HexCell> (); //dynamic list of cells that the unit could move to, which changes with the mouse move, once a unit is selected, set by HexSelect. 
	public float movementSpeed; // the speed at which the piece moves across the board (set to public for easy changing in the inspector).
	public GameObject battleSmoke;

	private List<HexCell> movementQueue = new List<HexCell> ();
	private int sightRange = 1;//number of cells unit can see


	private BoardManager boardManager;
	private EnqueueManager enqueueManager;
	//private AudioSource movementAudio;
	private HexSelect hexSelect;
	private HexGrid hexGrid;
	private FogOfWarManager fogOfWarManager;
	private UIManager uiManager;



	// Use this for initialization
	void Start () {
		boardManager = FindObjectOfType<BoardManager> ();
		enqueueManager = FindObjectOfType<EnqueueManager> ();
		//selectAudio = GetComponent<AudioSource> ();
		hexSelect = FindObjectOfType<HexSelect> ();
		hexGrid = FindObjectOfType<HexGrid> ();
		fogOfWarManager = FindObjectOfType <FogOfWarManager> ();
		uiManager = FindObjectOfType<UIManager> ();

		UpdateUnitLocation ();
		ResetMovementPointsToDefaultValue ();
	}
		
	/// <summary>
	/// Resets the number of movement points that the unit has left this turn back to default values.
	/// </summary>
	public void ResetMovementPointsToDefaultValue(){
		movementPointsRemaining = movementRange * numberOfMoves;

	}


	/// <summary>
	/// Moves the along the route set by HexSelect pathfinding algorithm .
	/// </summary>
	/// <returns>ienumerator (to allow yield returns</returns>
	/// <param name="cellPath">The cell path to move along (a list of cells defined by HexSelect).</param>
	public IEnumerator MoveAlongRoute(List<HexCell> cellPath, Dictionary<HexCell, float> costOfEachCellInRoute){
		hexSelect.DeselectUnit (); //to prevent unwanted behaviour if clicking elsewhere
		hexSelect.PreventInput (); //secondary protection to prevent unwanted clicks from confusing things
		movementQueue = cellPath; //saves the movement cell path to the individual unit (in case we want to cancel movement at a future date

		if (movementPointsRemaining > 0) {
			//int movementPointsUsedSoFar = 0; 
				
			List<HexCell> cellsToRemoveFrommMovementQueue = new List<HexCell> ();

			foreach (HexCell cell in movementQueue) {
				float cellCost = costOfEachCellInRoute[cell];

				if (movementPointsRemaining <= 0 || movementPointsRemaining - cellCost < 0) {
					break;
				}


				Allegiance unitOnThisCell = TestIfUnitPresent (cell);

				if (unitOnThisCell != Allegiance.None) {
					//There is definitely a unit on the next cell
				
					if (unitOnThisCell != allegiance) {
						//if there is an enemy unit on the next cell

						HexCell cellCameFrom = unitLocation; //if the battle is a stalemate, the unit will need to go back along the path, and it is easier to store here than mess about with list positions
						GameObject enemyUnit = boardManager.FindUnitByCell (cell); //get the enemy unit

						//Stop moving, ask if we want to do battle, continue moving if yes, interrupt movement if no;
						string question = "The enemy lies ahead, sir! Are your orders to attack?";
						UnityAction yesAction = new UnityAction (() => {
							RequestAttackUnit(cell, cellCameFrom, enemyUnit);
						});

						UnityAction noAction = new UnityAction (() => {
							CancelMergeOrAttack();
						});
							
						uiManager.PromptUserChoice (question, yesAction, noAction, "yes", "no");	

						break; //stops moving the unit along the path by breaking the foreach loop.

					} else {
						//if there is a unit in the path ahead, but it is a friendly unit

						if (boardManager.FindUnitByCell (cell).GetComponent<Unit> ().movementPointsRemaining > 0) {
							//it is a friendly unit and it has enough movement points to merge left
							//stop moving, ask if we want to merge, continue moving if yes, interrupt movement if no;


							string question = "Do you want to merge this Unit?";
							UnityAction yesAction = new UnityAction (() => {

								RequestMergeUnit(boardManager.FindUnitByCell(cell));
								});

							UnityAction noAction = new UnityAction (() => {
								CancelMergeOrAttack();
							});


							uiManager.PromptUserChoice (question, yesAction, noAction, "Yes", "No");	


						//	MergeWithUnit (boardManager.FindUnitByCell (cell));

						//	yield return StartCoroutine (MoveTo (cell));
						} else {
							//there is a friendly unit on the path ahead, but the other unit doesn't have enough units left to merge, so inform the player and break the loop, thereby stopping movevment along the cellPath.
							uiManager.SetBasicInfoText("The unit ahead has used all its movement points, so it cannot merge with this unit", "okay");
							uiManager.ToggleHUDActive (uiManager.basicInfoPopup);
							break;
						}
					}

				} else {
					//The square ahead is not occupied, so move the unit along as normal

					yield return StartCoroutine (MoveTo (cell));

				}
				cellsToRemoveFrommMovementQueue.Add (cell);
				//movementPointsUsedSoFar += cell.CalculateMovementCostOfCell();
				//movementPointsRemaining -= cell.CalculateMovementCostOfCell();


				movementPointsRemaining -= cellCost;

				//movementPointsUsedSoFar += (int)cellCost;

//				if (movementPointsUsedSoFar > movementPointsRemaining) {
//				//	Debug.Log ("break about to be called");
//					break;
//				}
			}

			foreach (HexCell cell in cellsToRemoveFrommMovementQueue) {
				movementQueue.Remove (cell);
			}

			hexSelect.AllowInput (); //allows user input

		} else {
			//flash red on inspector;
			UnitHUDUIManager unitHUDUIManager = FindObjectOfType<UnitHUDUIManager>();
			uiManager.MakeTextFlashRed(unitHUDUIManager.unitMovesLeftLabel);

		}
	}


	/// <summary>
	/// Tests if a unit is present at this cell. Used in order to determine which action to take if a unit is in the cellPath ahead of the unit.
	/// </summary>
	/// <returns>The allegiance of the unit in this cell (returns Allegiance.None if no unit is present).</returns>
	/// <param name="cell">The HexCell location to check if a unit is present.</param>
	Allegiance TestIfUnitPresent(HexCell cell){

		if(boardManager.FindUnitByCell(cell)){
			GameObject thisUnitObject = boardManager.FindUnitByCell (cell);

			return thisUnitObject.GetComponent<Unit>().allegiance;
		}
		return Allegiance.None;
	}


	/// <summary>
	/// Basic method that calls the MoveTo method, used when you need to call move unit from a function that is not an IEnumerator. Does not do the actual moving. 
	/// </summary>
	/// <returns>The move enumerator.</returns>
	/// <param name="cell">The hexCell to move to.</param>
	private IEnumerator callMoveEnumerator(HexCell cell){
		Debug.Log("moving to cell number " + cell.id);
		yield return StartCoroutine (MoveTo (cell));
		Debug.Log("arrived a cell number " + cell.id);
	}


	/// <summary>
	/// Acts on this gameobject and askis it to move to the next cell along the cell route. Usually called by MoveAlongRoute which acts on a list of cells (or "cellPath")
	/// </summary>
	/// <returns>the move enumerator.</returns>
	/// <param name="cell">HexCell to move to.</param>
	public IEnumerator MoveTo(HexCell cell){
		

//		Debug.Log ("move to called unit id is " + id + " and cell to move to is " + cell.id);

		float waitTime = 0.04f;
		while (true) {
			yield return new WaitForSeconds (waitTime);
				float step = movementSpeed * waitTime;
			//Debug.Log("moveing to " + cell.gameObject.transform.position);
				transform.position = Vector3.MoveTowards(transform.position, cell.gameObject.transform.position, step);
			CheckIfCityOnPreviousCellOrInNextCell (cell.id, unitLocation.id);
			UpdateUnitLocation ();
			if(transform.position == cell.gameObject.transform.position){
				break;
			}
		}
		if (boardManager.game.control == allegiance) {
			enqueueManager.EnqueueMovement (id, cell.id);

		}



		fogOfWarManager.ShowFogOfWar ();

		yield return new WaitForSeconds (0.3f);

	}

	void CheckIfCityOnPreviousCellOrInNextCell(int cellGoingToId, int cellCameFromID){
		if (boardManager.FindCityByCellID (cellGoingToId) ){
			//there is a city at this cell;
			City thisCity = boardManager.FindCityByCellID (cellGoingToId);
			thisCity.SetAllegiance (allegiance);
		} else {
			//there is no cicty on this cell

			if(boardManager.FindCityByCellID(cellCameFromID)){
				//this unit has just come from a city
				City cityCameFrom = boardManager.FindCityByCellID(cellCameFromID
				);

				cityCameFrom.SetAllegiance(Allegiance.None);
			}
		}
	}


	/// <summary>
	/// Updates the unit location based on its current position. Should be called for every cell the unit moves to.
	/// </summary>
	void UpdateUnitLocation(){
		HexCoordinates coordinates = HexCoordinates.FromPosition (transform.position);
		HexCell cell = hexGrid.cells[hexGrid.GetCellIndexFromCoordinates (coordinates)];
		unitLocation = cell;
		//Debug.Log ("new coords are " + unitLocation.coordinates);
	}

	/// <summary>
	/// Adds the cell to the cell route path (will be used to define the units planned route in future turns - not yet written).
	/// </summary>
	/// <param name="cell">Cell.</param>
	private void AddCellToRoute(HexCell cell){
		CellPossibleRoute.Add (cell);
	}

	/// <summary>
	/// Deletes all cells in the cell route path.
	/// </summary>
	public void ClearCellRoute(){
		CellPossibleRoute.Clear ();
	}

	/// <summary>
	/// Requests to start a coroutine which attacks the unit. This "process" function is necessary to ensure that the moves happen in sequence, after 
	/// </summary>
	/// <param name="cell">Cell.</param>
	/// <param name="cellCameFrom">Cell came from.</param>
	/// <param name="enemyUnit">Enemy unit.</param>
	private void RequestAttackUnit(HexCell cell, HexCell cellCameFrom, GameObject enemyUnit){
		CancelMergeOrAttack ();
		StartCoroutine (sayToAttackCell (cell, cellCameFrom, enemyUnit));

	}

	private IEnumerator sayToAttackCell(HexCell cell, HexCell cellCameFrom, GameObject enemyUnit){
		yield return StartCoroutine (callMoveEnumerator (cell));

		//yield return StartCoroutine (MoveTo (cell));

		Allegiance winner = Attack (enemyUnit);

		if (winner == Allegiance.None) {
			//No winner from the battle, so return unit to original cell;

			yield return StartCoroutine(callMoveEnumerator (cellCameFrom));
			//yield return StartCoroutine (MoveTo (cellCameFrom));
		}

		movementPointsRemaining = 0;

	}



	/// <summary>
	/// Attack the specified unit.
	/// </summary>
	/// <returns>
	/// The allegiance of the winner
	/// </returns>
	/// <param name="unitToAttack">Gameojbect of the unit to attack.</param>
	Allegiance Attack(GameObject unitToAttack){
		Allegiance battleWinner = Allegiance.None;
		Unit defender = unitToAttack.GetComponent<Unit> ();
		int attackerStrength = strength;
		int defenderStrength = defender.strength;
		BattleEvent newBattle = new BattleEvent ();
		newBattle.SetDefenderID(defender.id);
		newBattle.SetAttackerID(id);


		int attackerOriginalStrength = strength;
		int defenderOriginalStrength = defender.strength;


		//Set the advantage multipliers. By default attacker is set to 1 and defender is set to 1.333 to simulate defensive bonus
		// the attackers multiplier could be changed by other factors (e.g. leadership etc) in future.
		float attackerAdvantageMultiplier = 1f;
		float defenderAdvantageMultiplier = 4f / 3f;
		Debug.Log ("attacker advantage  is " + attackerAdvantageMultiplier + " and defender advantage is " + defenderAdvantageMultiplier);

		// randomizers give a random advantage to either defender or attacker and are separated in case we wish to further give more advantages
		float attackerRandomizer = Random.Range (0.1f, 10f);
		float defenderRandomizer = Random.Range (0.1f, 10f);
		Debug.Log ("attacker randomizer is " + attackerRandomizer + " and defender randomizer is " + defenderRandomizer);


		//Odds are the relative proporiton of strength once al the above variables are added in. 
		// For now it is based aroudn the attacker strength divided by defender strength times by multiplier and randomizer. 
		float attackerOdds = ((0.5f * (attackerStrength / defenderStrength)) * attackerAdvantageMultiplier) * attackerRandomizer;
		float defenderOdds = ((0.5f * (defenderStrength / attackerStrength)) * defenderAdvantageMultiplier) * defenderRandomizer;

		float totalOddsValue = attackerOdds + defenderOdds;
		float attackerPercent = (attackerOdds / totalOddsValue);
		float defenderPercent = (defenderOdds / totalOddsValue);

		Instantiate (battleSmoke, unitToAttack.transform.position, Quaternion.identity);

		if (attackerPercent < 0.1) {
			//The attacker has outright lost, and shold be destroyed.

			battleWinner = defender.allegiance;
			defender.ChangeStrength(Mathf.RoundToInt(defenderStrength * defenderPercent));
			newBattle.SetLosses (defenderOriginalStrength - defender.strength, attackerOriginalStrength);
			newBattle.SetWinner (battleWinner);
			newBattle.SetTurn (boardManager.game.turnNumber);
			newBattle.SetBattleLocation (unitLocation);
			boardManager.allBattles.Add (newBattle);
			boardManager.DestroyPiece (this.gameObject);


			//if the defender has lost 90% of its troops, destroy the unit and declare attacker the battle winner
		} else if (defenderPercent < 0.1) {
			//The defender has outright lost, and should be destroyed.

			boardManager.DestroyPiece (unitToAttack);
			battleWinner = allegiance;
			ChangeStrength(Mathf.RoundToInt(attackerStrength * attackerPercent));
			newBattle.SetLosses (attackerOriginalStrength - strength, defenderOriginalStrength);

		} else {
			//it was really a stalemate rather than an outright win, attacker is classed as winner for strength calculation purposes
			battleWinner = Allegiance.None;
			ChangeStrength (Mathf.RoundToInt (attackerStrength * attackerPercent));

			defender.ChangeStrength(Mathf.RoundToInt(defenderStrength * defenderPercent)); 
			newBattle.SetLosses (attackerOriginalStrength - strength, defenderOriginalStrength - defender.strength);
		}			

		newBattle.SetWinner (battleWinner);
		newBattle.SetTurn (boardManager.game.turnNumber);
		newBattle.SetBattleLocation (unitLocation);
		enqueueManager.EnqueueBattle(newBattle);
		Debug.Log ("winner is " + battleWinner);

		if (battleWinner == allegiance) {
			boardManager.game.CheckForGameOver ();
		}
	return battleWinner;

	
	}

	/// <summary>
	/// Requests to Merge the unit with the stated unit (this is called after the user has inputted whether they want to merge). It calls MergeWithUnit, which actually does the merging, but also asks the piece to move
	/// </summary>
	/// <param name="unitToMergeWithObject">Unit to merge with object.</param>
	private void RequestMergeUnit(GameObject unitToMergeWithObject){
		MergeWithUnit (unitToMergeWithObject);
		StartCoroutine (callMoveEnumerator (unitToMergeWithObject.GetComponent<Unit> ().unitLocation));
		CancelMergeOrAttack ();
	}

	/// <summary>
	/// Performs the actual merge with a unit (change strength and move nums etc).
	/// </summary>
	/// <param name="unitToMergeWithObject">GameObject of the Unit to merge with.</param>
	public void MergeWithUnit(GameObject unitToMergeWithObject){
		Unit unitToMergeWith = unitToMergeWithObject.GetComponent<Unit>();	

		if (boardManager.game.control == allegiance) {
			enqueueManager.EnqueueMerge (id, unitToMergeWith.id, strength, unitToMergeWith.strength);
		}

		ChangeStrength (strength + unitToMergeWith.strength);

		SetNumberOfMovementPointsRemaining (0);

		Destroy (unitToMergeWithObject);
		hexSelect.DeselectUnit ();

	}

	/// <summary>
	/// Cancel the move that had been requested.
	/// </summary>
	/// <returns><c>true</c> if this instance cancel merge; otherwise, <c>false</c>.</returns>
	private void CancelMergeOrAttack(){
		Debug.Log ("cancel input");
		uiManager.CancelInput ();

	}

	/// <summary>
	/// Changes the strength of this unit.
	/// </summary>
	/// <param name="newStrength">New strength to change to</param>
	public void ChangeStrength(int newStrength){
		strength = newStrength;
	}

	/// <summary>
	/// Sets the number of movement points that the unit has remaining this turn to a specific value.
	/// </summary>
	/// <param name="i">The value to change.</param>
	public void SetNumberOfMovementPointsRemaining(int numberOfMPsremaining){
		movementPointsRemaining = numberOfMPsremaining;
	}

	/// <summary>
	/// Gets the sight range.
	/// </summary>
	/// <returns>The sight range of the unit.</returns>
	public int GetSightRange(){
		return sightRange;
	}


	/// <summary>
	/// Splits the unit.
	/// </summary>
	/// <param name="cellIndex">index of the cell where the unit will be</param>
	public void SplitUnit(int cellIndex){
		strength = strength / 2;
		HexCell cell = hexGrid.cells [cellIndex];

		boardManager.ConstructNewUnit (cellIndex, allegiance, unitType, strength);
		fogOfWarManager.ShowFogOfWar ();
		SetNumberOfMovementPointsRemaining (0);
		boardManager.FindUnitByCell (cell).GetComponent<Unit> ().SetNumberOfMovementPointsRemaining (0);
	}


	/// <summary>
	/// Gets the strength of this unit.
	/// </summary>
	/// <returns>The strength of this unit.</returns>
	public int GetStrength(){
		return strength;
	}





}
