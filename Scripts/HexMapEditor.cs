using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.IO;
using Lookout;


public class HexMapEditor : MonoBehaviour {

	enum OptionalToggle{
		Ignore, Yes, No
	}

	enum SpawnOptionalToggle{
		Ignore, Base, Army, Remove
	}

	private TerrainType terrainType;
	public HexGrid hexGrid;
	public int activeElevation;
	int activeWaterLevel;
	int activeUrbanLevel, activeFarmLevel, activePlantLevel;
	int brushSize;


	bool applyTerrain;
	bool applyElevation;
	bool applyWaterLevel;
	bool applyUrbanLevel, applyFarmLevel, applyPlantLevel;

	Allegiance activeAllegiance = Allegiance.None;

	#region riverVars
	OptionalToggle riverMode, roadMode, walledMode, cityMode;
	SpawnOptionalToggle spawnMode = SpawnOptionalToggle.Ignore;
	bool isDrag;
	HexDirection dragDirection;
	HexCell previousCell;

	#endregion


	void Awake () {
		hexGrid = FindObjectOfType<HexGrid> ();
	}

	void Update(){
		if (Input.GetMouseButton (0) &&
		    !EventSystem.current.IsPointerOverGameObject ()) {
			HandleInput ();
		} else {
			previousCell = null;
		}

	}


	void HandleInput () {
		Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast (inputRay, out hit)) {
			HexCell currentCell = hexGrid.GetCellByPosition (hit.point);

			//Check to see if we are draggging the mouse
			if (previousCell && previousCell != currentCell) {
				ValidateDrag (currentCell);
			} else {
				isDrag = false;
			}



			EditCells (currentCell);
			previousCell = currentCell;
		} else {
			previousCell = null;
		}
	}

	void EditCells(HexCell centerCell){

		BoardManager boardManager = FindObjectOfType<BoardManager> ();
		List<int> allCellsToEdit = hexGrid.FindAllHexCellsWithinNSteps (brushSize, centerCell);

		EditCell (centerCell);
		foreach (int i in allCellsToEdit) {
			HexCell cellToEdit = hexGrid.cells [i];

			EditCell (cellToEdit);
		}



	}

	void EditCell (HexCell cell) {
		if (applyTerrain) {
			cell.TerrainType = terrainType;
		}

		if (applyElevation) {
			cell.Elevation = activeElevation;
		}

		if (applyWaterLevel) {
			cell.WaterLevel = activeWaterLevel;
		}
		//Drawing rivers or removing rivers
		if (riverMode == OptionalToggle.No) {
			cell.RemoveRiver ();
		}

		if (roadMode == OptionalToggle.No) {
			cell.RemoveRoads ();
		}

		if (spawnMode == SpawnOptionalToggle.Base) {
	
			cell.BaseAllegiance = activeAllegiance;
		} else if (spawnMode == SpawnOptionalToggle.Army) {
			cell.ArmyAllegiance = activeAllegiance;
		}



	
		if (applyUrbanLevel) {
			cell.UrbanLevel = activeUrbanLevel;
		}

		if (applyFarmLevel) {
			cell.FarmLevel = activeFarmLevel;
		}

		if (applyPlantLevel) {
			cell.PlantLevel = activePlantLevel;
		}

		if (walledMode != OptionalToggle.Ignore) {
			cell.Walled = walledMode == OptionalToggle.Yes;
		}

		if (cityMode != OptionalToggle.Ignore) {
			cell.HasCity = cityMode == OptionalToggle.Yes;
		}

		if (isDrag ) {


			HexCell otherCell = cell.GetHexCellNeighbour (dragDirection.Opposite ());


			if (otherCell) {
				if (riverMode == OptionalToggle.Yes) {
					otherCell.SetOutgoingRiver (dragDirection);
				}
				if (roadMode == OptionalToggle.Yes) {
					otherCell.AddRoad (dragDirection);
				}
			}

		}


	}
	public void SetElevation(float sliderValue){
		activeElevation = (int)sliderValue;
	}

	public void SelectTerrain(int i){
		applyTerrain = i >= 0;
		if (applyTerrain) {
			terrainType = (TerrainType)i;
		}
	}

	public void SetElevationToggle(bool b){
		applyElevation = b;
	}

	public void SetBrushSize(float size){
		brushSize = (int)size;
	}


	#region rivers

	public void SetRiverMode(int mode){
		riverMode = (OptionalToggle)mode;
	}

	public void ValidateDrag(HexCell currentCell){
		for (dragDirection = HexDirection.NE;
			dragDirection <= HexDirection.NW;
			dragDirection++) {

			if (previousCell.GetHexCellNeighbour (dragDirection) == currentCell) {
				isDrag = true;
				return;
			}
		}
		isDrag = false;
	}

	#endregion

	#region roads
	public void SetRoadMode(int mode){
		roadMode = (OptionalToggle)mode;
	}
	#endregion

	#region Water
	public void SetApplyWaterLevel(bool toggle){
		applyWaterLevel = toggle;
	}

	public void SetWaterLevel (float level){
		activeWaterLevel = (int)level;
	}

	#endregion

	#region StartPositions


	public void SetSpawnMode(int mode){
		spawnMode = (SpawnOptionalToggle)mode;
	}

	#endregion


	#region TerrainFeatures
	/// <summary>
	/// Toggles whether we should apply the urban level;
	/// </summary>
	/// <param name="toggle">If set to <c>true</c> toggle.</param>
	public void SetApplyUrbanLevel(bool toggle){
		applyUrbanLevel = toggle;
	}

	public void SetUrbanLevel (float level){
		activeUrbanLevel = (int)level;
	}

	public void SetApplyFarmLevel(bool toggle){
		applyFarmLevel = toggle;
	}

	public void SetFarmLevel(float level){
		activeFarmLevel = (int)level;
	}

	public void SetApplyPlantLevel(bool toggle){
		applyPlantLevel = toggle;
	}

	public void SetPlantLevel(float level){
		activePlantLevel = (int)level;
	}

	public void SetWalledMode(int mode){
		walledMode = (OptionalToggle)mode;
	}

	public void SetAllegiance(float allegiance){
		activeAllegiance = (Allegiance)allegiance;
	}

	public void SetCityMode(int mode){
		cityMode = (OptionalToggle)mode;
	}

	#endregion



}
