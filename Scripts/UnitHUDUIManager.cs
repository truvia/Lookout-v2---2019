using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitHUDUIManager : MonoBehaviour {

	public Text unitTypeLabel;
	public Text unitMovesLeftLabel; 
	public Text unitStrengthLabel;
	public Sprite armySprite;
	public Sprite fortressSprite;
	public GameObject unitPortrait;


	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void SetUnitHUDValues(Unit unit){
		
		unitTypeLabel.text = unit.unitType.ToString ();
		unitStrengthLabel.text = unit.strength.ToString ();
		unitMovesLeftLabel.text = unit.movementPointsRemaining + "/" + unit.movementRange;

		Image portraitImage = unitPortrait.GetComponent<Image> ();

		if (unit.unitType == Unit.UnitType.Fortress) {
			portraitImage.sprite = fortressSprite;
		} else if (unit.unitType == Unit.UnitType.Army) {
			portraitImage.sprite = armySprite;
		} else {
			//Spy portrait
		}


	}

}
