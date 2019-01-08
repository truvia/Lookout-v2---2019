using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class HexCellComparer : IComparable <HexCellComparer> {


	public int hexCellID;
	public double priority; // Smaller values are higher priority


	public HexCellComparer( int hexCellID, double priority){
		this.hexCellID = hexCellID;
		this.priority = priority;

	}

	public override string ToString ()
	{
		return "(" + hexCellID + ", " + priority.ToString ("F1") + ")";
	}

	public int CompareTo(HexCellComparer other){
		if (this.priority < other.priority)
			return -1;
		else if (this.priority > other.priority)
			return 1;
		else
			return 0;
	}
}
