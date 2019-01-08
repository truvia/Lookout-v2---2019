using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lookout;

public class BoostEvent : LookoutEvent  {

	private int newStrength;


	/// <summary>
	/// Sets the new strength to boost the unit by.
	/// </summary>
	/// <param name="strength">Strength.</param>
	public void SetNewStrength(int strength){
		newStrength = strength;
	}

	/// <summary>
	/// Gets the new strength.
	/// </summary>
	public int GetNewStrength(){
		return newStrength;
	}


}
