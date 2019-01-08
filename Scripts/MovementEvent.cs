using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementEvent: LookoutEvent {

	private int cellToMoveToID;

	public void SetIDOfUnitToMove(int id){
		SetActionUnitID(id);
	}

	public int GetIDOfUnitToMove(){
		return GetActionUnitID();
	}

	public void SetIDOfCellToMoveTo(int id){
		cellToMoveToID = id;
	}

	public int GetIDOfCellToMoveTo(){
		return cellToMoveToID;
	}



}
