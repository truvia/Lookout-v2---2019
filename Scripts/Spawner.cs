using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lookout;

public class Spawner : MonoBehaviour {

	public GameObject CONArmyPrefab;
	public GameObject CONFortressPrefab;
	public GameObject CONSpyPrefab;
	public GameObject USAArmyPrefab;
	public GameObject USAFortressPrefab;
	public GameObject USASpyPrefab;
	public GameObject CityPrefab;


	/// <summary>
	/// Spawns (instantiates) units and sets the parent as the spawner .
	/// </summary>
	/// <returns>The unit gameobject (already instantiated in scene)</returns>
	/// <param name="location">Location that the unit will be spawned.</param>
	/// <param name="allegiance">Allegiance of the unit.</param>
	/// <param name="unitType">Unit type (fortress, city, spy).</param>
	public GameObject SpawnUnit(Vector3 location, Allegiance allegiance, Unit.UnitType unitType){

		GameObject newGameObject;

		if (unitType == Unit.UnitType.Army) {
			newGameObject = (allegiance == Allegiance.CON) ? CONArmyPrefab : USAArmyPrefab;
		} else if (unitType == Unit.UnitType.Fortress) {
			newGameObject = (allegiance == Allegiance.CON) ? CONFortressPrefab : USAFortressPrefab;
		} else if (unitType == Unit.UnitType.Spy) {
			newGameObject = (allegiance == Allegiance.CON) ? CONSpyPrefab : USASpyPrefab;
		} else {
			Debug.LogError ("No programmed unit type to spawn.");
			newGameObject = new GameObject ();
		}

		Transform parent = this.transform;

		GameObject newUnit = Instantiate (newGameObject, location, Quaternion.identity, parent);

		return newUnit;
	
	}

	/// <summary>
	/// Spawns a city.
	/// </summary>
	/// <returns>The city gameObject (already instantiated in scene).</returns>
	/// <param name="location">Location of the city</param>
	public GameObject SpawnCity(Vector3 location){
		
		Transform parent = this.transform;
		GameObject newGameObject = Instantiate(CityPrefab, location, Quaternion.identity, parent);

		return newGameObject;
			
	}
}
