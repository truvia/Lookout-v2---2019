using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MergeEvent : LookoutEvent {

	public int unitToMergeWithID;
	public int newStrength;



	public void SetTheIDOfUnitThatOrderedTheMerge(int unitCallingTheMerge){
		SetActionUnitID(unitCallingTheMerge);
	}

	public int GetIDOfUnitThatOrderedTheMerge(){
		return GetActionUnitID ();
	}


	public void SetUnitToMergeWith(int idOfUnitToMergeWith ){
		unitToMergeWithID = idOfUnitToMergeWith;
	}

	public int GetUnitToMergeWith(){
		return unitToMergeWithID; 
	}

	public void SetNewStrength(int unitOrderingMergeStrength, int unitBeingMergedWithStrength){
		newStrength = unitOrderingMergeStrength + unitBeingMergedWithStrength;
	}

	public int GetNewStrength(){
		return newStrength;
	}

}
