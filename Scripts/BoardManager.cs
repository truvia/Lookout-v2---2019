using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Lookout;
using System.IO;
public class BoardManager : MonoBehaviour {

	//Changeable in the Inspector


	public Game game = new Game ();
	public MyPlayerController playerController;


	Spawner spawner;
	HexGrid hexGrid;
	EnqueueManager enqueueManager;

	City[] allCities;




	#region TestingVars
	List<GameObject> allUnits = new List<GameObject>();
	public List<BattleEvent> allBattles = new List<BattleEvent>();
	public Queue<LookoutEvent> allEvents = new Queue<LookoutEvent>();


	public GameObject dot;


	#endregion

	#region Listeners
	UnityAction<System.Object> didBeginGameNotificationAction;
	UnityAction<System.Object> didStartLocalPlayerNotificationAction;


	void Awake(){
		didBeginGameNotificationAction = new UnityAction<System.Object> (NewGameRequested); //defines what action that this object should take when the event is triggered
		didStartLocalPlayerNotificationAction = new UnityAction<System.Object>(LocalPlayerStarted);
	}


	void OnEnable(){
		EventManager.StartListening (Game.DidBeginGameNotification, didBeginGameNotificationAction);
		EventManager.StartListening (MyPlayerController.DidStartLocalPlayer, didStartLocalPlayerNotificationAction);
	}

	void OnDisable(){
		EventManager.StopListening (Game.DidBeginGameNotification, didBeginGameNotificationAction);
		EventManager.StopListening (MyPlayerController.DidStartLocalPlayer, didStartLocalPlayerNotificationAction);
	}


	#endregion

	void Start(){
		spawner = FindObjectOfType<Spawner> ();
		hexGrid = FindObjectOfType<HexGrid> ();
		enqueueManager = FindObjectOfType<EnqueueManager> ();
		game.boardHeight = hexGrid.cellCountZ;
		game.boardWidth = hexGrid.cellCountX;

		//LoadMap ();
		game.ResetGame ();


	}



	void LocalPlayerStarted(object obj){
		playerController = (MyPlayerController)obj;

	}



	#region GameSetup

	void NewGameRequested(object obj){
	Debug.Log ("BoardManager has been asked to begin a new game");
		//myNetworkManagerUI.ToggleNetworkHUDHide ();

	}


	public void InitialGameSetup(){
		Debug.Log ("initialgameSetup called");
		SpawnableObject[] spawnableObjects = FindObjectsOfType<SpawnableObject> ();

		foreach (SpawnableObject spawnableObject in spawnableObjects) {
			ConstructNewUnit (spawnableObject.cellLocationID, spawnableObject.allegiance, spawnableObject.unitType, 5000);
			Destroy (spawnableObject.gameObject);
		}

		allCities = FindObjectsOfType<City> ();
////		//create CON armies
//		foreach (int startTile in conStartLocations) {
//			if (startTile > 0) {
//
//				ConstructNewUnit (startTile, Allegiance.CON, Unit.UnitType.Army, 5000);
//				}
//			}
//
//		//Create USA armies
//		foreach (int startTile in usStartLocations) {
//			if (startTile > 0) {
//				ConstructNewUnit (startTile, Allegiance.USA, Unit.UnitType.Army, 5000);
//			}
//		}
//
//		//Create Fortresses
//		ConstructNewUnit(game.CONFortressStartTile, Allegiance.CON, Unit.UnitType.Fortress, 8000);
//		ConstructNewUnit (game.USAFOrtressStartTIle, Allegiance.USA, Unit.UnitType.Fortress, 8000);
//
//		//create Cities
//		foreach(int cellID in game.cities){
//			ConstructNewCity (cellID, Allegiance.None);
//		}
//			
//		allCities = FindObjectsOfType<City> ();

	}

//	/ <summary>
//	/ Function to randomly chose which tiles are started. These values are called by the PlayerController and sent to the other machine. 
//	/ </summary>
//	/ <returns>A list of CellIDs that tell which cells to .</returns>
//	/ <param name="allegiance">allegiance (called once for each team).</param>
//	public int[] DefineStartPositions(Allegiance allegiance){
//		//Because these values need to be passed over the network, we can't use a list of integers, however it is much easier to add and remove values as it can be a dynamic size. Therefore, we simply pass all the values to the list, then add them back into the array.
//
//		int[] possibleStartSquaresArray = game.USPossibleStartTiles; //the tiles which units are able to start on.
//			
//
//		if (allegiance == Allegiance.CON) {
//			possibleStartSquaresArray = game.CONPossibleStartTiles; //i.e. get con start tilesif we're now calling this method on CON. 
//		}
//
//		int randomNumber = Mathf.RoundToInt(Random.Range(0f, possibleStartSquaresArray.Length));
//		
//		possibleStartSquaresArray [randomNumber] = -1;
//
//		return possibleStartSquaresArray;
//	}
//

	#endregion

	#region CreateFunctions
	public void ConstructNewUnit(int cellID, Allegiance allegiance, Unit.UnitType unitType, int strength){

		HexCell locationCell = hexGrid.cells [cellID];
		Vector3 location = locationCell.transform.position;
		//Vector3 location = hexGrid.FindHexCellByIntCoordinates (coordinate [0], coordinate [1]).transform.position;
		GameObject newGameObject = spawner.SpawnUnit (location, allegiance, unitType);
		Unit newUnit = newGameObject.GetComponent<Unit> ();

		newUnit.allegiance = allegiance;
		newUnit.unitType = unitType;
		newUnit.unitLocation = locationCell;
		//newUnit.coords = coordinate;
		newUnit.strength = strength;
		newUnit.id = SetUnitID ();

		if (unitType == Unit.UnitType.Army) {
			newUnit.numberOfMoves = 1;
		} else if (unitType == Unit.UnitType.Fortress) {
			newUnit.numberOfMoves = 0;
		} else if (unitType == Unit.UnitType.Spy) {
			newUnit.numberOfMoves = 2;
		} else {
			Debug.Log ("RGame says that you haven't defined the number of moves for the unit type you are trying to construct in ConstructNewUnit. The unitType is " + unitType);
		}

		allUnits.Add (newGameObject);

	}

	void ConstructNewCity (int cellID, Allegiance allegiance){
		HexCell locationCell = hexGrid.cells [cellID];
		Vector3 newPos = locationCell.transform.position;

		GameObject newCity = spawner.SpawnCity (newPos);
		City thisNewCity = newCity.GetComponent<City> ();

		thisNewCity.SetAllegiance (Allegiance.None);
		thisNewCity.SetLocationCellID (cellID);

	}

	public void BoostAllCityUnits(){
		if (allCities.Length > 0) {
			foreach (City city in allCities) {

				if (FindUnitByCell (hexGrid.cells [city.GetLocationCellID ()])) {
					Unit unitAtSquare = FindUnitByCell (hexGrid.cells [city.GetLocationCellID ()]).GetComponent<Unit> ();

						city.BoostArmyStrengthEachTurn (unitAtSquare);

					city.SetAllegiance (unitAtSquare.allegiance);
				}
				
			}
		}
	}

	int SetUnitID(){
		int highestID = 0;

		foreach (GameObject unit in allUnits) {
			if (unit.GetComponent<Unit>().id > highestID) {
				highestID = unit.GetComponent<Unit>().id;
			}
		}

		return highestID + 1;
	}

	/// <summary>
	/// Processes actions at the end of the turn, then calls end turn on the playerController.
	/// </summary>
	/// <param name="obj">Object.</param>
	public void ProcessEndOfTurnActionsAndEndTurn(){
		BoostAllCityUnits ();

		playerController.EndTurn ();

	}

	public void ProcessStartOfTurnActions(){
		game.ChangeTurn ();
		StartCoroutine (enqueueManager.DequeueLookoutEvents ());
		RefreshNumMoves ();
	}


	#endregion


	#region destroyFunctions

	public void DestroyPiece(GameObject itemToDestroy){
		Destroy (itemToDestroy);
	}

	#endregion


	#region findUnits
	public GameObject FindUnitByCell(HexCell cell){
		Unit[] allUnits = FindObjectsOfType<Unit> ();
		foreach (Unit unit in allUnits) {

			if (unit.unitLocation == cell) {
				//Debug.Log ("Find unit by Cell" + unit.unitLocation);
				return unit.gameObject;
			}
		}
		return null;
	}

	public GameObject FindUnitByID(int id){
		Unit[] allUnits = FindObjectsOfType<Unit> ();

		foreach (Unit unit in allUnits) {
			if (unit.id == id) {
				return unit.gameObject;
			}
		}
		return null;
	}

	public List<HexCell> FindAllCellsWithUnitsIn(){
		List<HexCell> hexCellsWithUnitsIn = new List<HexCell> ();
		Unit[] allUnits = FindObjectsOfType<Unit> ();
		foreach (Unit unit in allUnits) {
			hexCellsWithUnitsIn.Add (unit.unitLocation);
		}

		return hexCellsWithUnitsIn;
	}



	public List<HexCoordinates> FindAllCoordinatesWithUnitsIn(){
		List<HexCoordinates> allCoordinatesWithUnitsIn = new List<HexCoordinates> ();
		Unit[] allUnits = FindObjectsOfType<Unit> ();

		foreach (Unit unit in allUnits) {
			allCoordinatesWithUnitsIn.Add (unit.unitLocation.coordinates);
		}


		return allCoordinatesWithUnitsIn;
	}
		


	public City FindCityByCellID(int cellID){
		if (allCities != null) {
			foreach (City city in allCities) {
				if (city.GetLocationCellID () == cellID) {
					return city;
				} 
			}

		}
		return null;
	}





	#endregion

	#region Movement









//	public float LinearInterpolation(int a, int b, int distanceBetweenCells, int i){
//		//Linear interpolation plots the route between different cells (it is a standard maths formulat)
//		//using linear interpolation each point will be a + (b-a * 1.0/distanceBetweenCells * i, (where i is every point between 0 and the distance bewteen cells, where a is start cell and b is endcell
//		return(a + (b - a) * (1.0f / distanceBetweenCells * i));
//	}
		
//
//	public HexCell roundedCoords(float x, float y, float z){
//		
//		//using traditional Mathf.Round doesn't always work: as for tiles 1NE and 1E, the float is exaclty between two different tiles, so when you round individual coordinates and add all together, it doesn't always add up to 0.
//		int roundedX = Mathf.RoundToInt (x);
//		int roundedY = Mathf.RoundToInt (y);
//		int roundedZ = Mathf.RoundToInt (z);
//
//		//Debug.Log ("Mathf rounding = " + roundedX + ", " + roundedY + ", " + roundedZ);
//
//		float xDifference = Mathf.Abs (roundedX - x);
//		float yDifference = Mathf.Abs (roundedY - y);
//		float zDifference = Mathf.Abs (roundedZ - z);
//
//		if (xDifference > yDifference && xDifference > zDifference) {
//			roundedX = -roundedY - roundedZ;
//
//		} else if (yDifference > zDifference) {
//			roundedY = -roundedX - roundedZ;
//		} else {
//			roundedZ = -roundedX - roundedY;
//		}
//
//		//Debug.Log ("Special Rounded:" + roundedX + ", " + roundedY + ", " + roundedZ);
//		if(hexGrid.FindHexCellByIntCoordinates(roundedX, roundedZ) == null){
//			//Debug.Log (x + ", " + y + ", " + z);
//			//Debug.Log ("mathf round: " + Mathf.RoundToInt (x) + ", " + Mathf.RoundToInt (y) + ", " + Mathf.RoundToInt (z));
//			//Debug.Log ("Special Round: " + roundedX + " , " + roundedY + ", " + roundedZ);
//
//			roundedX = Mathf.CeilToInt (x);
//			//Debug.Log ("Finalised coord: " + roundedX + ", " + roundedY + ", " + roundedZ);
//		}
//		return hexGrid.FindHexCellByIntCoordinates (roundedX, roundedZ);
//	}
//


	#endregion


	#region TestingGround




	/// <summary>
	/// Returns each unit's number of moves bacck to default.
	/// </summary>
	public void RefreshNumMoves(){
		Unit[] allUnits = FindObjectsOfType<Unit> (); 

		foreach (Unit unit in allUnits) {
			unit.ResetMovementPointsToDefaultValue ();
		}
	}


//
//
//	Vector3 GetVector3LocationFromCoordinates(int[] coordinate){
//		Vector3 location = new Vector3 ();
//		int x = coordinate [0];
//		int z = coordinate [1];
//
//		location.x = (x + z * 0.5f - z/2) * (HexMetrics.innerRadius * 2f);
//		location.z = z * (HexMetrics.outerRadius * 1.5f);
//		location.y = 0f;
//		return location;
//	}

//

	#endregion

}

