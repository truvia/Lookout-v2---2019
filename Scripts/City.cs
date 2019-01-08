using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lookout;

public class City : MonoBehaviour {

	private Allegiance cityAllegiance;
	public Color USAColor;
	public Color CONColor;
	public Color neutralColor;

	[SerializeField]
	private int armyBoostPerTurn;
	public int locationCellID;

	/// <summary>
	/// Sets the allegiance of this city.
	/// </summary>
	/// <param name="allegiance">Allegiance to set to.</param>
	public void SetAllegiance(Allegiance allegiance){
		cityAllegiance = allegiance;
		SetColour ();
	}

	/// <summary>
	/// Gets the allegiance of the city.
	/// </summary>
	/// <returns>The allegiance of the city.</returns>
	public Allegiance GetAllegiance(){
		return cityAllegiance;
	}

	private void SetColour(){
		Color colorToChangeTo;

		if (cityAllegiance == Allegiance.USA) {
			colorToChangeTo = USAColor;
		} else if (cityAllegiance == Allegiance.CON) {
			colorToChangeTo = CONColor;
		} else {
			colorToChangeTo = neutralColor;
		}

		for (int i = 0; i < transform.childCount; i++) {
			transform.GetChild (i).GetComponent<MeshRenderer> ().material.color = colorToChangeTo;
		}
	}



	public void BoostArmyStrengthEachTurn(Unit armyUnit){
		if (armyUnit.unitType == Unit.UnitType.Army) {
			armyUnit.ChangeStrength (armyUnit.GetStrength () + armyBoostPerTurn);
			BoostEvent newBoostEvent = new BoostEvent ();
			newBoostEvent.SetActionUnitID (armyUnit.id);
			newBoostEvent.SetNewStrength (armyUnit.strength);

		}
	}

	public int GetLocationCellID(){
		return locationCellID;
	}


	public void SetLocationCellID(int cellLocationID){
		locationCellID = cellLocationID;
	}



}
