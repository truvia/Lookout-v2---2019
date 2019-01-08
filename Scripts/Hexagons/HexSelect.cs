using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PriorityQueues;

public class HexSelect : MonoBehaviour {
	#region Testing
	public BoardManager boardManager;
	HexGrid hexGrid;
	bool movementInProgress;
	public UIManager uiManager;
	public UnitHUDUIManager unitHUDUIManager;
	public FogOfWarManager fogOfWarManager;


	public GameObject unitMount; //set in the inspector - the thing that shows which unit is selected.
	public GameObject selectedUnitGameobject;
	public GameObject pointerArrow;

	bool allowInput = false;

	//Vars relating to the cell path or pathfinding
	Dictionary<HexCell, float> costSoFar = new Dictionary<HexCell, float> (); //int is cost so far, hexcell is this cell
	Dictionary<HexCell, float> costsOfEachCellInPath = new Dictionary<HexCell, float>();
	List<HexCell> cellPath = new List<HexCell> ();
	HexCell endCell;

	//Shorthand reference to HexGrid cells for easier reference
	HexCell[] cells;


	#endregion

	void Start(){
		hexGrid = GetComponent<HexGrid> ();
		cells = hexGrid.cells;

		if (!FindObjectOfType<HexMapEditor> ()) {
			hexGrid.ShowCellIndexNumbers (true);
			HideHighlightedCells ();
		}

	}

	void Update(){

		if (!FindObjectOfType<HexMapEditor> ()) {
			//ensures that this code doesn't execute if in map edit mode.

			if (Input.GetMouseButtonDown (0) && allowInput) {
				//if you left click, handle input
				HandleClickInput ();
			}

			if (Input.GetMouseButtonDown (1)) {
				//if you right click, deselect the unit
				DeselectUnit ();
			}


			if (selectedUnitGameobject && allowInput) {
				//if a unit is selected and you move the mouse, show the unit move
				ShowMovementRoute ();
			}
		}
	}


	#region ClickInput
	/// <summary>
	/// Uses the current mouseposition to call ClickCell
	/// </summary>
	void HandleClickInput(){
		Ray inputRay = Camera.main.ScreenPointToRay (Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast (inputRay, out hit)) {

			if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject ()) {

				ClickCell (hit.point);
			}
		}
	}

	/// <summary>
	/// Uses the current hover mouseposition to pick a destination cell, then calls the PathfindUsingAStar to identify the shortest path, and the DefinePath method to  define and show the route between two cells.
	/// </summary>
	void ShowMovementRoute(){
		Ray inputRay = Camera.main.ScreenPointToRay (Input.mousePosition);
		RaycastHit hit;

		if (Physics.Raycast (inputRay, out hit)) {
			

			if(!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()){
			Vector3 position;
			position = transform.InverseTransformPoint (hit.point);
			HexCoordinates coordinates = HexCoordinates.FromPosition (position);
			int index = hexGrid.GetCellIndexFromCoordinates (coordinates);
			HexCell thisCell = cells [index];


			if (thisCell != endCell && thisCell != selectedUnitGameobject.GetComponent<Unit>().unitLocation) {
			
				HexCell startCell = selectedUnitGameobject.GetComponent<Unit> ().unitLocation;
				Dictionary<HexCell, HexCell> cameFrom = PathfindUsingAStar (thisCell);

				if(cameFrom.ContainsKey(thisCell)){
					DefinePathToCell(startCell, thisCell, cameFrom);

				}else{
						DestroyNumTurnsToReachDestinationIndicators ();	
						//fogOfWarManager.ShowFogOfWar ();
						HideHighlightedCells();
						thisCell.EnableHighlight(Color.red, "");

				}
									
			}
			}
		}
	}


	/// <summary>
	/// Triages what action should be taken when a cell is clicked (e.g. Select, attack, move etc.)
	/// </summary>
	/// <param name="position">
	/// Vector3 mouseposition.
	/// </param>
	void ClickCell(Vector3 position){
		Debug.Log ("touched");
		position = transform.InverseTransformPoint (position);
		HexCoordinates coordinates = HexCoordinates.FromPosition (position);
		int index = hexGrid.GetCellIndexFromCoordinates (coordinates);

		HexCell thisCell = cells [index];

		//Check if there is a unit in the cell being clicked on
		if (boardManager.FindUnitByCell (thisCell)) {



			Unit thisUnit = boardManager.FindUnitByCell (thisCell).GetComponent<Unit> ();

			//Check if the unit is my unit and if it is my turn to play.
			if (boardManager.playerController.playerAllegiance == boardManager.game.control && thisUnit.allegiance == boardManager.playerController.playerAllegiance) {


			//	Debug.Log ("this unit is one of mine");
				if (!selectedUnitGameobject && thisUnit.unitType != Unit.UnitType.Fortress) {
					//if there is no selected unit, select the unit	
					SelectUnit (thisUnit.gameObject);
				} else if (thisUnit.unitType != Unit.UnitType.Fortress && selectedUnitGameobject.GetComponent<Unit>().unitLocation != thisCell ) {
					//Prompt mergee
				//	Debug.Log ("On a path to merge...");
					StartCoroutine(selectedUnitGameobject.GetComponent<Unit>().MoveAlongRoute(cellPath, costsOfEachCellInPath));


				}
			} else {
			//	Debug.Log ("enemy unit");
				//if the unit is an enemy unit
				if (!selectedUnitGameobject) {
					//if no unit is selected, show basic known unit details
			//		Debug.Log ("This is an enemy unit. You don't have control of it");
				} else {

					if (boardManager.game.control == boardManager.playerController.playerAllegiance) {
						//Prompt attack. 
					//	Debug.Log ("this is an enemy unit and a unit is selected. On a path to attack it.");
						if (selectedUnitGameobject.GetComponent<Unit> ().movementPointsRemaining > 0) {
							StartCoroutine (selectedUnitGameobject.GetComponent<Unit> ().MoveAlongRoute (cellPath, costsOfEachCellInPath));
						}
					}
				}
			}
		} else {
			Debug.Log ("no unit found control is " + boardManager.game.control + "and my allegiance is " + boardManager.playerController.playerAllegiance);
			//no unit found at this cell;
			if (selectedUnitGameobject && selectedUnitGameobject.GetComponent<Unit>().movementPointsRemaining > 0) {
			//	Debug.Log ("but a unit is selected");
				//if a unit is selected
				StartCoroutine(selectedUnitGameobject.GetComponent<Unit>().MoveAlongRoute(cellPath, costsOfEachCellInPath));
			//	Debug.Log ("startedCoroutine");
			}
		}
	}


	#endregion

	#region Selection

	/// <summary>
	/// Selects the given unit, clears the previous path found and calls InstantiateUnitMount to show which unit is selected
	/// </summary>
	/// <param name="unitToSelect">
	/// The gameobject of the unit which we are being asked to select
	/// </param>
	void SelectUnit(GameObject unitToSelect){
		//cameFrom.Clear ();
		selectedUnitGameobject = unitToSelect;
		InstantiateUnitMount ();
		uiManager.ToggleHUDActive (uiManager.unitHUD);
		unitHUDUIManager.SetUnitHUDValues (unitToSelect.GetComponent<Unit> ());


	}


	/// <summary>
	/// Deselects the currently selectedUnitGameObject (does not require a parameter as it is a public var. Calls DestroyNumMovementINdicators to hide movement indicators and clears the selection 
	/// </summary>
	public void DeselectUnit(){

		if (selectedUnitGameobject) {
			//HidePathWithoutDeselecting ();
	
			uiManager.ToggleHUDActive (uiManager.unitHUD);
			DestroyNumTurnsToReachDestinationIndicators ();
			HideHighlightedCells ();
			selectedUnitGameobject.GetComponent<Unit> ().ClearCellRoute ();
			//fogOfWarManager.ShowFogOfWar();
			selectedUnitGameobject = null;
		}
	}



	/// <summary>
	/// Destroys and reinstantiates the unit mount (which indicates to the player which unit is selected) using a pre-defined tag  
	/// </summary>
	public void InstantiateUnitMount(){
		Debug.Log ("called");
		GameObject existingMount = GameObject.FindGameObjectWithTag ("unitMount");
		if (existingMount) {
			Destroy (existingMount);
		}

		GameObject newMount = Instantiate (unitMount, selectedUnitGameobject.transform.position, Quaternion.identity, this.transform);
		newMount.transform.parent = selectedUnitGameobject.transform;
		newMount.tag = "unitMount";

	}
		
	#endregion


	#region Defining And Showing Unit's path
	/// <summary>
	/// Pathfinds the using A* algorithm to a specific "goal" cell. Removes cells that contain a unit and river or mountain squares. StartCell is selected unit cell.
	/// </summary>
	/// <param name="goalCell"> Goal Cell</param>
	Dictionary<HexCell, HexCell> PathfindUsingAStar(HexCell goalCell){ 
		Dictionary<HexCell, HexCell> cameFrom = new Dictionary<HexCell, HexCell> ();
		costSoFar.Clear ();
		costsOfEachCellInPath.Clear ();

		if (goalCell != endCell) {
			endCell = goalCell;

			List<HexCoordinates> cellsWithUnitsIn = boardManager.FindAllCoordinatesWithUnitsIn (); //find where all the units are at the start so we dont waste processing power in the loops
			cameFrom.Clear ();

			HexCell startCell = selectedUnitGameobject.GetComponent<Unit> ().unitLocation;

			PriorityQueue<HexCellComparer> frontier = new PriorityQueue<HexCellComparer> ();
			HexCellComparer startCellComparer = new HexCellComparer (startCell.id, 0);
			frontier.Enqueue (startCellComparer);

			costSoFar.Add (startCell, 0); //0

			while (frontier.Count () > 0) {
				HexCellComparer currentCellComparer = frontier.Dequeue ();
				HexCell currentCell = hexGrid.cells [currentCellComparer.hexCellID];

				if (currentCell == goalCell) {
					break;
				}

				for (int l = 0; l < 6; l++) {
					
					HexDirection directionRelativeToCurrentCell = (HexDirection)l;

					if(currentCell.GetHexCellNeighbour((HexDirection)l)){
						
						HexCell neighbourCell = currentCell.GetHexCellNeighbour((HexDirection)l);

						float neighbourCost = neighbourCell.CalculateCostToMoveToThisCell (currentCell, directionRelativeToCurrentCell);
						float newCost = costSoFar [currentCell] + neighbourCost;
						int neighbourIndex = neighbourCell.id;
						int priority = (int)newCost + GetDistanceBetweenTwoCells (goalCell, currentCell);

						HexCellComparer neighbourComparer = new HexCellComparer (neighbourIndex, priority);

						if(IsItPossibleToMoveToThisCell(neighbourCell, neighbourCost, cellsWithUnitsIn, endCell)){				


									if (!costSoFar.ContainsKey (neighbourCell)) {
										costSoFar.Add (neighbourCell, newCost);
										costsOfEachCellInPath.Add (neighbourCell, neighbourCost);
										frontier.Enqueue (neighbourComparer);
										cameFrom.Add (neighbourCell, currentCell);
						
									} else if (costSoFar.ContainsKey (neighbourCell)) {
										if (newCost < costSoFar [neighbourCell]) {
											costSoFar [neighbourCell] = newCost;
												costsOfEachCellInPath [neighbourCell] = neighbourCost;
											frontier.Enqueue (neighbourComparer);
											if (cameFrom.ContainsKey (neighbourCell)) {
												cameFrom [neighbourCell] = currentCell;
											} else {
												cameFrom.Add (neighbourCell, currentCell);
											}
										
										}

									}
								
						}
						
					}
				} 

			}
		}
		return cameFrom;
	}
		

	/// <summary>
	/// Translates the set of cells defined by the pathfinding algorithm into a clear list of cells, then calls ShowPath to show the path on the board
	/// </summary>
	/// <param name="startCell">
	/// The cell the pathfinding algorithm started from (usually the cell that the selected unit is in).
	/// </param>
	/// <param name="goalCell">
	/// The cell the pathfinding algorithm was navigating to (usually the cell at the current mouse position).
	/// </param>
	/// <param name="cameFrom">
	/// A key value pair dictionary of two cells, matching cells along the pathfinding route to the previous cell that was explored.
	/// </param>
	void DefinePathToCell(HexCell startCell, HexCell goalCell, Dictionary<HexCell, HexCell> cameFrom){
		cellPath.Clear ();
		DestroyNumTurnsToReachDestinationIndicators ();
		HexCell currentCell = goalCell;

		while (currentCell != startCell) {
			cellPath.Add (currentCell);
			currentCell = cameFrom [currentCell];
		}

		ShowPath(cellPath);
	}

	void HideHighlightedCells(){
		foreach (HexCell cell in hexGrid.cells) {
			cell.DisableHighlight ();
		}

	}


/// <summary>
/// Invoked by DefinePathToCell. Highlights the cell path and how many turns it will take for the unit to reach this cell. 
/// </summary>
/// <param name="cellPath">Cell path.</param>
	void ShowPath(List<HexCell> cellPath){
		HideHighlightedCells ();
		Unit unit = selectedUnitGameobject.GetComponent<Unit> ();
		cellPath.Reverse ();

		int cellPathCount = cellPath.Count -1;
		HexCell endCell = cellPath [cellPathCount];
		float baseCost = 0f;

		if (unit.movementPointsRemaining < unit.movementRange) {
			baseCost += unit.movementRange - unit.movementPointsRemaining;
		}

		int i = 0;

		foreach (HexCell cell in cellPath) {
			string labelText = "";
			float cost = (costSoFar [cell] + baseCost) / unit.movementRange;

			if (cell == endCell) {

				cost = Mathf.Ceil (cost);
				labelText = cost.ToString ();
			} else{
				cost = Mathf.Ceil (cost);

				if ((i + 1) <= cellPath.Count) {
					HexCell nextCell = cellPath [i + 1];
					if (Mathf.Ceil ((costSoFar [nextCell] + baseCost) / unit.movementRange) == cost) {
						//do nothing
					} else{
						labelText = cost.ToString ();
					}

				}
			}


			cell.EnableHighlight (Color.green, labelText);

			i++;
		}


	}

	bool IsItPossibleToMoveToThisCell(HexCell neighbourCell, float neighbourCost, List<HexCoordinates> cellsWithUnitsIn, HexCell goalCell){

		//conditions wehre it is always impossible to move to this cell
		if (neighbourCell.IsUnderWater
		    || neighbourCell.TerrainType == TerrainType.Mountain
			|| neighbourCost > selectedUnitGameobject.GetComponent<Unit>().movementRange
		    )
		{
			return false;
		}
		if (!neighbourCell.HasRiver
		   || neighbourCell.HasBridge
		   || neighbourCell.HasRiverBeginOrEnd) 
		{

			if (!cellsWithUnitsIn.Contains (neighbourCell.coordinates) || neighbourCell == goalCell) {
				return true;
			}
		
		}

		return false;
	}


	/*END TESTING */

	/// <summary>
	/// Shows the path of cells between a start and end position (typically called by DefinePathToCell). Also shows the number of turns it will take to get to that position.
	/// </summary>
	/// <param name="cellPath">Cell path.</param>
//	void ShowPath(List<HexCell> cellPath){
//		HideHighlightedCells ();
//		string labelText = "";
//		Unit unit = selectedUnitGameobject.GetComponent<Unit> ();
//		int numTurns = 1;
////		fogOfWarManager.ShowFogOfWar();
//
//		cellPath.Reverse ();
//
//		int turncost = 1;
//
//		int i = 1;
//
//		foreach (HexCell cell in cellPath) {
//			float unitMovementPoints = unit.movementRange;
//
//			if (unit.movementPointsRemaining < unitMovementPoints) {
//				if (unit.movementPointsRemaining > 0) {
//					unitMovementPoints = unit.movementPointsRemaining;
//						
//				} else if(numTurns == 1) {
//					numTurns++;
//				}
//			}
//				
//
//			if (i == cellPath.Count || cell.CalculateMovementCostOfCell() < unitMovementPoints && turncost >= unitMovementPoints || cell.CalculateMovementCostOfCell() >= unitMovementPoints) {
//				Debug.Log ("cell labels should show");
//
//
//				//Text label = Instantiate<Text> (hexGrid.cellLabelPrefab);
//			//	label.rectTransform.SetParent (cell.chunk.gridCanvas.transform, false);
//			//	label.rectTransform.anchoredPosition = new Vector2 (cell.transform.position.x, cell.transform.position.z);
//				labelText = numTurns.ToString ();
//				//label.tag = "MovementLabel";
//				turncost = 1;
//				numTurns++;
//
//
//			} else if (cell.CalculateMovementCostOfCell() < unitMovementPoints && turncost < unitMovementPoints) {
//				turncost += cell.CalculateMovementCostOfCell();
//				labelText = "";
//
//			}
//
//			cell.EnableHighlight (Color.green, labelText);
//		
//			//hexGrid.ColorCell (cell, hexGrid.highlightRouteColor);
//
//			i++;
//
//		}
//
//		//hexGrid.RefreshAllChunkGrids ();
//		//hexGrid.hexMesh.TriangulateAllCells (hexGrid.cells);
//	}

	/// <summary>
	/// Destroies the Indicators that show the number of turns it takes to reach a particular destination
	/// </summary>
	void DestroyNumTurnsToReachDestinationIndicators(){
		GameObject[] allMovementTaggedObjects = GameObject.FindGameObjectsWithTag ("MovementLabel");

		if (allMovementTaggedObjects.Length > 0) {

			foreach (GameObject taggedObject in allMovementTaggedObjects) {
				Destroy (taggedObject);
			}

		}
	}


	private int GetDistanceBetweenTwoCells(HexCell a, HexCell b){
		return (Mathf.Abs(a.coordinates.X - b.coordinates.X) + Mathf.Abs(a.coordinates.Y - b.coordinates.Y) + Mathf.Abs(a.coordinates.Z - b.coordinates.Z)) / 2;
	}


	#endregion

	#region Directional Functions

	public void PreventInput(){
		allowInput = false;
	}

	public void AllowInput(){
		allowInput = true;
	}

	public bool CheckIfCellEmpty(int cellIndex){
		HexCell cell = hexGrid.cells [cellIndex];
		List<HexCell> cellsWithUnitsIn = boardManager.FindAllCellsWithUnitsIn ();

		if (cell.TerrainType == TerrainType.Mountain || cell.TerrainType == TerrainType.Mud
		    || cellsWithUnitsIn.Contains (cell)) {

			return false;
		} else {  

			return true;
		}
	}




	#endregion

	#region Superflous

	//
	//	/// <summary>
	//	/// Gets the direction of cell relative to this cell.
	//	/// </summary>
	//	/// <returns>The direction of cell relative to this cell.</returns>
	//	/// <param name="thisCell">This cell.</param>
	//	/// <param name="cellToCompareTo">Cell to compare to.</param>
	//	Directions GetDirectionOfCellRelativeToThisCell(HexCell thisCell, HexCell cellToCompareTo){
	//		foreach (KeyValuePair<Directions, int[]> keyValue in hexDirectionDictionary) {
	//			Directions compassDirection = keyValue.Key;
	//			int[] intDirection = keyValue.Value;
	//
	//			int x = intDirection [0];
	//			int y = intDirection [1];
	//			int z = intDirection [2];
	//
	//			if (cellToCompareTo.coordinates.X + x == thisCell.coordinates.X && cellToCompareTo.coordinates.Y + y == thisCell.coordinates.Y && cellToCompareTo.coordinates.Z + z == thisCell.coordinates.Z) {
	//				return compassDirection;
	//			}
	//		}
	//
	//		return Directions.None;
	//	}
	//
	//	/// <summary>
	//	/// Returns the angle in which an arrow should point.
	//	/// </summary>
	//	/// <returns>The arrow angle.</returns>
	//	/// <param name="direction">Direction the arrow should point.</param>
	//	float ReturnArrowAngle(Directions direction){
	//		float degree = 0f;
	//
	//		if (direction == Directions.NE) {
	//			degree = 225f;	
	//		}else if(direction == Directions.E){
	//			degree = 270f;
	//		}else if(direction == Directions.SE){
	//			degree = 315f;
	//		}else if(direction == Directions.SW){
	//
	//			degree = 45f;
	//		}else if(direction == Directions.W){
	//
	//			degree = 90f;
	//		}else if(direction == Directions.NW){
	//
	//			degree = 135f;
	//		}
	//
	//		//		if (direction == Directions.NE) {
	//		//			degree = 45f;	
	//		//		}else if(direction == Directions.E){
	//		//			degree = 90f;
	//		//		}else if(direction == Directions.SE){
	//		//			degree = 135f;
	//		//		}else if(direction == Directions.SW){
	//		//
	//		//			degree = 225f;
	//		//		}else if(direction == Directions.W){
	//		//
	//		//			degree = 270f;
	//		//		}else if(direction == Directions.NW){
	//		//
	//		//			degree = 315f;
	//		//		}
	//		//
	//		Debug.Log (degree);
	//		return degree;
	//	}
	//
	//	/// <summary>
	//	/// Gets the relative HexCell coordinate amount you need to add in order to get the cell in that relative direction
	//	/// </summary>
	//	/// <returns>array of integers (relative coordinate position).</returns>
	//	/// <param name="direction">Direction you want to move in</param>
	//	int[] GetDirection(Directions direction){
	//		return hexDirectionDictionary[direction];
	//	}



	//	/// <summary>
	//	/// Gets the neighbour of the cell in any given direction
	//	/// </summary>
	//	/// <returns>The hexcell neighbour.</returns>
	//	/// <param name="cell">Cell to start from</param>
	//	/// <param name="direction">Direction to look in (integer array) </param>
	//	HexCell GetHexCellNeighbour(HexCell startCell, int[] direction){
	//	//	Debug.Log ("direction x: " + direction [0] + ", " + direction [1] + ", " + direction [2]);
	//	//	Debug.Log ("cell new x coord" + cell.coordinates.X);
	//
	//		int x = startCell.coordinates.X + direction [0];
	//		int z = startCell.coordinates.Z + direction [2];
	//
	//		if (hexGrid.FindHexCellByIntCoordinates (x, z)) {
	//			return hexGrid.FindHexCellByIntCoordinates (x, z);
	//		} 
	//
	//		return null;
	//			
	//	}



//	 void DrawMovementRoute(HexCell startCell, HexCell endCell){ //Linear interpolation pathfinding
//		Unit selectedUnit = selectedUnitGameobject.GetComponent<Unit> ();
//		selectedUnit.ClearCellRoute ();
//
//		//First we calculate distance between two cells
//		int distanceBetweenCells = boardManager.GetDistanceBetweenTwoCells(startCell, endCell);
//		//using linear interpolation to plot the route between each cell, each cell will be a + (b-a * 1.0/distanceBetweenCells * i, (where i is every point between 0 and the distance bewteen cells, where a is start cell and b is endcell
//			
//		if (distanceBetweenCells > 0) {
//
//			for (int i = 0; i <= distanceBetweenCells; i++) {
//
//				float xLocation = boardManager.LinearInterpolation (startCell.coordinates.X, endCell.coordinates.X, distanceBetweenCells, i);
//				float yLocation = boardManager.LinearInterpolation (startCell.coordinates.Y, endCell.coordinates.Y, distanceBetweenCells, i);
//				float zLocation = boardManager.LinearInterpolation (startCell.coordinates.Z, endCell.coordinates.Z, distanceBetweenCells, i);
//
//				HexCell thisCell = boardManager.roundedCoords (xLocation, yLocation, zLocation);
//				hexGrid.ColorCell (thisCell, hexGrid.highlightRouteColor);
//
//				int distanceBetweenThisCellAndOriginal = boardManager.GetDistanceBetweenTwoCells (startCell, thisCell);
//				//Debug.Log ("What is the distance: " + distanceBetweenThisCellAndOriginal);
//
//				if (distanceBetweenThisCellAndOriginal > selectedUnit.numberOfCellStepsRemaining) {
//					hexGrid.ColorCell (thisCell, hexGrid.routeOutOfRangeColor);
//				}
//
//				selectedUnit.AddCellToRoute (boardManager.roundedCoords (xLocation, yLocation, zLocation));
//			}
//			hexGrid.hexMesh.Triangulate (hexGrid.cells);
//	
//
//		}
//	}

//	void FloodFillCells(){ //called every time a piece is selected HexCell goalCell
//		cameFrom.Clear();
//		HexCell startCell = selectedUnitGameobject.GetComponent<Unit> ().unitLocation;
//
//		List<HexCell> frontier = new List<HexCell> ();
//		frontier.Add (startCell);
//
//		cameFrom.Add (startCell, null);
//
//		for(int i = 0; i < frontier.Count; i++){
//			HexCell currentCell = frontier [i];
//
//			//			if (currentCell == goalCell) {
//			//				break;
//			//			}
//
//			for (int l = 0; l < 6; l++) {
//				if (GetHexNeighbour (currentCell, hexDirections [l])) {
//					HexCell neighbourCell = GetHexNeighbour (currentCell, hexDirections [l]);
//					if (neighbourCell.terrainType != HexCell.TerrainType.River && neighbourCell.terrainType != HexCell.TerrainType.Mountain) {
//
//						if (!boardManager.FindUnitByCell (neighbourCell)) {
//
//							if (!cameFrom.ContainsKey (neighbourCell)) {
//
//								frontier.Add (neighbourCell);
//								cameFrom.Add (neighbourCell, currentCell);
//							}
//						}
//					}
//				}
//			}
//		}
//
//
//	}
//
//	void PathfindUsingUniformCostSearch(){ //called every time a piece is selected HexCell goalCell ( dijkstra's algorithm)
//		cameFrom.Clear();
//		HexCell startCell = selectedUnitGameobject.GetComponent<Unit> ().unitLocation;
//
//		PriorityQueue<HexCellComparer> frontier = new PriorityQueue<HexCellComparer> ();
//		HexCellComparer startCellComparer = new HexCellComparer (startCell.id, 0);
//		frontier.Enqueue (startCellComparer);
//
//		Dictionary<HexCell, int> costSoFar = new Dictionary<HexCell, int> (); //int is cost so far, hexcell is this cell
//		costSoFar.Add (startCell, 0);
//
//		while (frontier.Count() > 0) {
//			HexCellComparer currentCellComparer = frontier.Dequeue();
//
//			HexCell currentCell = hexGrid.cells [currentCellComparer.hexCellID];
//
//			for (int l = 0; l < 6; l++) {
//				if (GetHexNeighbour (currentCell, hexDirections [l])) {
//					HexCell neighbourCell = GetHexNeighbour (currentCell, hexDirections [l]);
//
//					int newCost = costSoFar [currentCell] + neighbourCell.movementCost;
//					int neighbourIndex = neighbourCell.id;
//					int priority = newCost;
//					HexCellComparer neighbourComparer = new HexCellComparer (neighbourIndex, priority);
//
//					if (neighbourCell.terrainType != HexCell.TerrainType.River && neighbourCell.terrainType != HexCell.TerrainType.Mountain) {
//
//						if (!boardManager.FindUnitByCell (neighbourCell)) {
//
//							if (!costSoFar.ContainsKey (neighbourCell)) {
//								costSoFar.Add (neighbourCell, newCost);
//
//								frontier.Enqueue (neighbourComparer);
//								cameFrom.Add (neighbourCell, currentCell);
//							} else if (costSoFar.ContainsKey (neighbourCell)) {
//								if (newCost < costSoFar [neighbourCell]) {
//
//									costSoFar [neighbourCell] = newCost;
//									frontier.Enqueue (neighbourComparer);
//									cameFrom.Add (neighbourCell, currentCell);
//
//								}
//							}
//						}
//					}
//				}
//			}
//		} 
//
//	}
//

	//		hexDirectionDictionary.Add(Directions.NE, hexDirections[0]);
	//		hexDirectionDictionary.Add (Directions.E, hexDirections [1]);
	//		hexDirectionDictionary.Add(Directions.SE, hexDirections[2]);
	//		hexDirectionDictionary.Add (Directions.SW, hexDirections [3]);
	//		hexDirectionDictionary.Add(Directions.W, hexDirections[4]);
	//		hexDirectionDictionary.Add (Directions.NW, hexDirections [5]);



	//	int[][] hexDirections = new int[][]{
	//		new int[]{0, -1, 1},		new int[]{1, -1, 0},		new int[]{1, 0, -1},
	//		new int[]{0, 1, -1},		new int[]{-1, 1, 0},		new int[]{-1, 0, 1},
	//	};
	//
	//	enum Directions{
	//		NE,
	//		E,
	//		SE,
	//		SW,
	//		W,
	//		NW,
	//		None
	//	}
	//Dictionary<Directions, int[]> hexes = new Dictionary<Directions, int[]>();

	//Dictionary<Directions, int[]> hexDirectionDictionary = new Dictionary<Directions, int[]>();


	#endregion

}
