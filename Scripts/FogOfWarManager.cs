using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Lookout;

public class FogOfWarManager : MonoBehaviour {

	private HexGrid hexGrid;
	private BoardManager boardManager;

	public GameObject fogOfWar;

	[Range(-1, 1f)]
	public float hue;

	[Range(-1f, 1f)]
	public float saturation = 1f;

	[Range(-1f, 1f)]
	public float value = -0.95f;


//	private float hueNow;
//	private float saturationNow;
//	private float valueNow;

	#region Listeners
	private UnityAction<System.Object> didConstructFirstUnits;

	void Awake(){
		didConstructFirstUnits = new UnityAction<System.Object> (DidConstructFirstUnits); //defines what action that this object should take when the event is triggered
	}

	void OnEnable(){
		//EventManager.StartListening (BoardManager.DidConstructFirstUnits, didConstructFirstUnits);
	}

	void OnDisable(){
	//	EventManager.StopListening (BoardManager.DidConstructFirstUnits, didConstructFirstUnits);
	}

	void DidConstructFirstUnits(object obj){
		ShowFogOfWar ();

	}
	#endregion

	void Start () {
		hexGrid = FindObjectOfType<HexGrid> ();
		boardManager = FindObjectOfType<BoardManager> ();

	}

//	void Update(){
//		if (hue != hueNow || saturation != saturationNow || value != valueNow) {
//			DarkenCells (DefineCellsThatThisPlayerCannotSee());
//		}
//	}
//	

	/// <summary>
	/// Defines the cells that this player cannot see.
	/// </summary>
	/// <returns>The IDs of cells that this player cannot see.</returns>
	List<int> DefineCellsThatThisPlayerCannotSee(){
		List<int> cellsWeCanSee = DefineCellsThatThisPlayerCanSee ();
		List<int> cellsWeCantSee = new List<int> ();

		foreach (HexCell cell in hexGrid.cells) {
			if(!cellsWeCanSee.Contains(cell.id)){
				cellsWeCantSee.Add (cell.id);
			}
		}

		return cellsWeCantSee;
	}


	/// <summary>
	/// Defines the cells that this player can see.
	/// </summary>
	/// <returns>The IDs of cells that this player can see.</returns>
	List<int> DefineCellsThatThisPlayerCanSee(){
		Unit[] allUnits = FindObjectsOfType<Unit> ();
		List<int> cellsThatWeCanSee = new List<int> ();

		foreach (Unit unit in allUnits) {
			if (unit.allegiance == boardManager.playerController.playerAllegiance) {
				//This unit is one of my units;

				//add the square that the unit is already on to visible squares
				if (!cellsThatWeCanSee.Contains (unit.unitLocation.id)) {
					cellsThatWeCanSee.Add (unit.unitLocation.id);
				}


				int sightRange = unit.GetSightRange ();
				List<int> allCellsWithinNSteps = hexGrid.FindAllHexCellsWithinNSteps (sightRange, unit.unitLocation);

				foreach (int cellID in allCellsWithinNSteps) {
					if (!cellsThatWeCanSee.Contains (cellID)) {
						cellsThatWeCanSee.Add (cellID);
					}
				}
			}
		}
		return cellsThatWeCanSee;
	}
		
	public void ShowFogOfWar(){
		//hexGrid.ReturnAllCellsToOriginalColor ();
		//DarkenCells (DefineCellsThatThisPlayerCannotSee ());
	}

	void DarkenCells(List<int> cellsThatPlayerCannotSee){
		//originalCorrectionFactor = correctionfactor;

//		hueNow = hue;
//		saturationNow = saturation;
//		valueNow = value;

		foreach (int cellID in cellsThatPlayerCannotSee) {
			HexCell relevantCell = hexGrid.cells [cellID];


			float H, S, V;

			Color.RGBToHSV (relevantCell.defaultColour, out H, out S, out V);

			H *= 1+ hue;
			S *= 1+ saturation;
			V *= 1+ value;

			relevantCell.Color = Color.HSVToRGB (H, S, V);


		}

		Unit[] allUnits = FindObjectsOfType<Unit>();
		foreach (Unit unit in allUnits) {
			if (cellsThatPlayerCannotSee.Contains (unit.unitLocation.id)) {
				SetWhetherWeCanSeeUnitOrNot (unit, false);
			} else {
				SetWhetherWeCanSeeUnitOrNot (unit, true);
			}
		}



		//hexGrid.hexMesh.TriangulateAllCells (hexGrid.cells);
		//hexGrid.RefreshAllChunkGrids();
	}

	private void SetWhetherWeCanSeeUnitOrNot(Unit thisUnit, bool trueOrFalse){

		for (int i = 0; i < thisUnit.transform.childCount; i++) {
			thisUnit.transform.GetChild (i).gameObject.SetActive (trueOrFalse);
		}

	}



}
