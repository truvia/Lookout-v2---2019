using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;


public enum HexDirection{
	NE,
	E,
	SE,
	SW,
	W,
	NW	
}

/// <summary>
/// Hex direction extensions.
/// </summary>
public static class HexDirectionExtensions{

	/// <summary>
	/// Return the opposite of a sepcified direction		/// </summary>
	/// <param name="direction">Direction.</param>
	public static HexDirection Opposite (this HexDirection direction){
		return (int)direction < 3 ? (direction + 3) : (direction - 3);
	}


	public static HexDirection Previous (this HexDirection direction){
		return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
	}

	public static HexDirection Next (this HexDirection direction){
		return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
	}

	public static HexDirection Previous2(this HexDirection direction){
		direction -= 2;
		return direction >= HexDirection.NE ? direction : (direction + 6);
	}

	public static HexDirection Next2(this HexDirection direction){
		direction += 2;
		return direction <= HexDirection.NW ? direction : (direction - 6);
	}
}
	

public enum TerrainType{
	Mud,
	Mountain,
	Grass,
	Desert,
	Forest,
	Hills
}


public class HexCell : MonoBehaviour {

	HexGrid hexGrid;

	public Color mudColor;
	public Color mountainColor;
	public Color grassColor;
	public Color desertColor;
	public Color forestColor;
	public Color hillsColor;

	public HexGridChunk chunk; //the chunk of  the grid that this cell belongs to

	public int id;
	public Color defaultColour; 
	Color color;
	public HexCoordinates coordinates; 
	TerrainType terrainType;



	public RectTransform uiRect;

	#region SpawnPositions

	public Lookout.Allegiance BaseAllegiance{
		get{ 
			return baseAllegiance;
		}

		set{ 
			if (value != baseAllegiance) {


				if (value == Lookout.Allegiance.CON) {
					if (id != hexGrid.conBaseLocationID && hexGrid.conBaseLocationID >= 0) {
						//if there is already a base of teh same allegiance in the game, remove it
						hexGrid.cells [hexGrid.conBaseLocationID].BaseAllegiance = Lookout.Allegiance.None;
					}

					if (id == hexGrid.usBaseLocationID) {
						// if the enemy base is already on this square, remove it.
						hexGrid.cells [hexGrid.usBaseLocationID].BaseAllegiance = Lookout.Allegiance.None;
					}

					hexGrid.conBaseLocationID = id;

				} else if (value == Lookout.Allegiance.USA) {
					if (id != hexGrid.usBaseLocationID && hexGrid.usBaseLocationID >= 0) {
						//if there is already a base of the same allegiance in the game, remove it.
						hexGrid.cells [hexGrid.usBaseLocationID].BaseAllegiance = Lookout.Allegiance.None;
					}

					if (id == hexGrid.conBaseLocationID) {
						//If the enemy base is already on this square, remove it 
						hexGrid.cells [hexGrid.conBaseLocationID].BaseAllegiance = Lookout.Allegiance.None;
					}

					hexGrid.usBaseLocationID = id;
				} else {
					//allegiance has been changed to none.
					if(baseAllegiance == Lookout.Allegiance.CON){
						hexGrid.conBaseLocationID = -1;
					} 

					if (baseAllegiance == Lookout.Allegiance.USA) {
						hexGrid.usBaseLocationID = -1;
					}
				}

				baseAllegiance = value;

				RefreshSelfOnly ();
			}
		}
	}

	Lookout.Allegiance baseAllegiance = Lookout.Allegiance.None;


	public Lookout.Allegiance ArmyAllegiance{
		get{ 
			return armyAllegiance;
		}
		set{ 
			if (value != ArmyAllegiance) {

				if (value == Lookout.Allegiance.CON) {
					if(!hexGrid.conArmiesStartLocationList.Contains(id)){
						if (hexGrid.conArmiesStartLocationList.Count >= 5) {
							int firstCell = hexGrid.conArmiesStartLocationList [0];

							if (firstCell == id) {
								firstCell++;
							}
							hexGrid.cells [firstCell].ArmyAllegiance = Lookout.Allegiance.None;
							hexGrid.conArmiesStartLocationList.Remove (firstCell);
						}	
					}
					hexGrid.conArmiesStartLocationList.Add (id);
				} else if (value == Lookout.Allegiance.USA) {
					if (!hexGrid.usArmiesStartLocationList.Contains (id)) {
						if (hexGrid.usArmiesStartLocationList.Count >= 5) {
							int firstCell = hexGrid.usArmiesStartLocationList [0];

							if (firstCell == id) {
								firstCell++;
							}
							hexGrid.cells [firstCell].ArmyAllegiance = Lookout.Allegiance.None;
							hexGrid.usArmiesStartLocationList.Remove (firstCell);

						}	
					}
					hexGrid.usArmiesStartLocationList.Add (id);

				} else if(value == Lookout.Allegiance.None){
					if (armyAllegiance == Lookout.Allegiance.CON) {
						hexGrid.conArmiesStartLocationList.Remove (id);
					} else if (armyAllegiance == Lookout.Allegiance.USA) {
						hexGrid.usArmiesStartLocationList.Remove (id);
					}
				}

				armyAllegiance = value;
				RefreshSelfOnly ();
			}
		}
	}

	Lookout.Allegiance armyAllegiance = Lookout.Allegiance.None;

	public bool HasCity{
		get{ 
			return hasCity;
		}
		set{ 
			if (value != hasCity) {
				hasCity = value;
				RefreshSelfOnly ();
			}
		}
	}
	bool hasCity = false;

	#endregion

	#region TerrainFeaturesVars
	public int UrbanLevel{
		get{ 
			return urbanLevel;
		} set{ 
			if (urbanLevel != value) {
				urbanLevel = value;
				RefreshSelfOnly ();
			}
		}
	}
	int urbanLevel;

	public int FarmLevel{
		get{ 
			return farmLevel;
		}
		set{ 
			if (farmLevel != value) {
				farmLevel = value;
				RefreshSelfOnly ();
			}
		}
	}
	int farmLevel;

	public int PlantLevel{
		get{ 
			return plantLevel;
		}
		set{ 
			if (plantLevel != value) {
				plantLevel = value;
				RefreshSelfOnly ();
			}
		}
	}
	int plantLevel;

	public bool Walled{
		get{ 
			return walled;
		}

		set{ 
			if (walled != value) {
				walled = value;
				Refresh ();
			}
		}
	}
	bool walled;
	#endregion

	#region rivers
	bool hasIncomingRiver, hasOutgoingRiver;
	HexDirection incomingRiver, outGoingRiver; 

	public bool HasIncomingRiver{
		get{ 
			return hasIncomingRiver;
		}
	}

	public bool HasOutgoingRiver{
		get{ 
			return hasOutgoingRiver;
		}
	}

	public HexDirection IncomingRiver{
		get{ 
			return incomingRiver;
		}
	}

	public HexDirection OutGoingRiver{
		get{ 
			return outGoingRiver;
		}
	}

	public bool HasRiver{
		get{ 
			return hasIncomingRiver || hasOutgoingRiver;
		}
	}

	public bool HasRiverBeginOrEnd{
		get{ 
			return hasIncomingRiver != hasOutgoingRiver;
		}
	}

	public bool HasRiverThroughEdge(HexDirection direction){
		return hasIncomingRiver && incomingRiver == direction ||
			hasOutgoingRiver && outGoingRiver == direction;
	}

	public float StreamBedY{
		get{ 
			return (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;
		}
	}

	public float RiverSurfaceY{
		get{ 
			return (elevation + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;
		}
	}

	public HexDirection RiverBeginOrEndDirection{
		get{ 
			return hasIncomingRiver ? incomingRiver : outGoingRiver;
		}

	}

	public bool HasBridge{
		get{ 
			return hasBridge;
		}

		set{ 
			if (value != hasBridge) {

				hasBridge = value;
			}
		}
	}
	bool hasBridge = false;

	#endregion

	#region Water

	public int WaterLevel {
		get{ 
			return waterLevel;
		}
		set{ 
			if (waterLevel == value) {
				return;
			}
			waterLevel = value;
			ValidateRivers ();
			Refresh ();
		}
	}
	int waterLevel;

	public bool IsUnderWater{
		get{ 
			return waterLevel > elevation;
		}
	}

	public float WaterSurfaceY{
		get{ 
			return(waterLevel + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep;
		}


	}
	#endregion

	#region elevation
	public int Elevation{
		get{
			return elevation;	
		}
		set{ 
			if (elevation == value) {
				return;
				}
			elevation = value;
			RefreshPosition ();
			ValidateRivers ();

			for (int i = 0; i < roads.Length; i++) {
				if(roads[i] && GetElevationDifference((HexDirection)i) > 1){
					SetRoad(i, false); //destroy the road if the elevation is increased too high for the road to make sense (e.g. up a cliff).
				}
			}


			Refresh();
		}
	}
	int elevation = int.MinValue;


	public TerrainType TerrainType{
		get{ 
			return terrainType;
		}
		set{ 
			if (terrainType == value) {
				return;
			}
			terrainType = value;
			SetTerrainColor ();
			Refresh ();
		}
	}


	#endregion

	public Color Color{
		get{ 
			return color;
		}

		set{ 
			if (color == value) {
				return;
			}
			color = value;

			Refresh();

		}
	}


	public 	Vector3 Position {
		get{ 
			return transform.localPosition;
		}
	}

	#region newVars
	[SerializeField]
	HexCell[] neighbours = new HexCell[6];
	#endregion

	#region RoadVars
	[SerializeField]
	bool[] roads;

	public bool HasRoads{
		get{ 
			for (int i = 0; i < roads.Length; i++) {
				if (roads [i]) {
					return true;
				}
			}
			return false;
		}
	}
	#endregion

	// Use this for initialization
	void Awake () {
		hexGrid = FindObjectOfType<HexGrid> ();

	}

	void RefreshPosition(){

		Vector3 position = transform.localPosition;
		position.y = elevation * HexMetrics.elevationStep;
		position.y += (HexMetrics.SampleNoise(position).y *2f - 1f) * HexMetrics.elevationPerturbStrength;
		transform.localPosition = position;

		Vector3 uiPosition = uiRect.localPosition;
		uiPosition.z = -position.y;
		uiRect.localPosition = uiPosition;

	}


	#region newCode

	/// <summary>
	/// Gets the neighbour of the cell in any given direction
	/// </summary>
	/// <returns>The hexcell neighbour.</returns>
	/// <param name="cell">Cell to start from</param>
	/// <param name="direction">Direction to look in (integer array) </param>
	public HexCell GetHexCellNeighbour(HexDirection direction){
		//	Debug.Log ("direction x: " + direction [0] + ", " + direction [1] + ", " + direction [2]);
		//	Debug.Log ("cell new x coord" + cell.coordinates.X);

		return neighbours [(int)direction];
	}


	public void SetNeighbour(HexDirection direction, HexCell cell){
		neighbours [(int)direction] = cell;
		cell.neighbours [(int)direction.Opposite ()] = this;
	}




	public void SetTerrainColor(){

			if (terrainType == TerrainType.Mud) {
				defaultColour = mudColor;
			} else if (terrainType == TerrainType.Mountain) {
				defaultColour = mountainColor;
			} else if (terrainType == TerrainType.Grass) {
				defaultColour = grassColor;
			} else if (terrainType == TerrainType.Desert) {
				defaultColour = desertColor;
			} else if (terrainType == TerrainType.Forest) {
				defaultColour = forestColor;
			} else {
				defaultColour = hillsColor;
			}
		color = defaultColour;
}

	public HexMetrics.HexEdgeType GetEdgeTypeByDirection(HexDirection direction){
		return HexMetrics.GetHexEdgeType (elevation, neighbours [(int)direction].elevation);
	}


	/// <summary>
	/// Gets the type of slope between two cells, using HexCell as the "other" cell.
	/// </summary>
	/// <returns>The edge type by cell.</returns>
	/// <param name="otherCell">Other cell.</param>
	public HexMetrics.HexEdgeType GetEdgeTypeByCell(HexCell otherCell){
		return HexMetrics.GetHexEdgeType (elevation, otherCell.elevation);
	}

	void Refresh(){
		if (chunk) {
			chunk.RefreshGrid ();
		}

		for (int i = 0; i < neighbours.Length; i++) {
			HexCell neighbourCell = neighbours [i];
			if (neighbourCell != null && neighbourCell.chunk != chunk) {
				neighbourCell.chunk.RefreshGrid ();
			}
		}
	}

	public void RefreshSelfOnly(){
		if (chunk) {
			chunk.RefreshGrid ();
		}
	}

	#endregion


	#region Rivers

	public void RemoveOutgoingRiver(){
		if (!hasOutgoingRiver) {
			return;
		}

		hasOutgoingRiver = false;
		RefreshSelfOnly ();

		HexCell neighbourcell = GetHexCellNeighbour(outGoingRiver);
		neighbourcell.hasIncomingRiver = false;
		neighbourcell.RefreshSelfOnly();
	}

	public void RemoveIncomingRiver(){
		if (!hasIncomingRiver) {
			return;
		}

		hasIncomingRiver = false;
		RefreshSelfOnly ();
		HexCell neighbourCell = GetHexCellNeighbour (incomingRiver);
		neighbourCell.hasOutgoingRiver = false;
		neighbourCell.RefreshSelfOnly ();
	}

	public void RemoveRiver(){
		RemoveOutgoingRiver ();
		RemoveIncomingRiver ();
	}

	public void SetOutgoingRiver(HexDirection direction){
		if (hasOutgoingRiver && outGoingRiver == direction) {
			return;
		}
		HexCell neighbour = GetHexCellNeighbour (direction);
		if(!IsValidRiverDestination(neighbour)){
			return;
		} 

		if (hasOutgoingRiver && incomingRiver == direction) {
			RemoveIncomingRiver ();
		}

		hasOutgoingRiver = true;
		outGoingRiver = direction;
		baseAllegiance = Lookout.Allegiance.None;
		//RefreshSelfOnly (); //removed to make way for roads

		neighbour.hasIncomingRiver = true;
		neighbour.incomingRiver = direction.Opposite ();
		neighbour.BaseAllegiance = Lookout.Allegiance.None;
		//neighbour.RefreshSelfOnly (); //removed to make way for roads

		SetRoad ((int)direction, false); //remove all roads in the way of the river. - may want to think about bridges at a later date.
	}

	//ensuring the rivers can flow in and out of lakes
	bool IsValidRiverDestination(HexCell neighbour){
		return neighbour && (elevation >= neighbour.elevation || waterLevel == neighbour.elevation);
	}

	void ValidateRivers(){
		if (hasOutgoingRiver && !IsValidRiverDestination (GetHexCellNeighbour (outGoingRiver))) {
			RemoveOutgoingRiver ();
		}

		if (hasIncomingRiver && !GetHexCellNeighbour (incomingRiver).IsValidRiverDestination (this)) {
			RemoveIncomingRiver ();
		}
	}

	#endregion

		

	#region Roads

	/// <summary>
	/// Determines whether this instance has road through edge the specified direction.
	/// </summary>
	/// <returns><c>true</c> if this instance has road through edge the specified direction; otherwise, <c>false</c>.</returns>
	/// <param name="direction">Direction.</param>
	public bool HasRoadThroughEdge(HexDirection direction){
		return roads [(int)direction];

	} 


	/// <summary>
	/// Adds a road from a particular edge direction
	/// </summary>
	/// <param name="direction">Direction.</param>
	public void AddRoad(HexDirection direction){
		if (!roads [(int)direction] && !HasRiverThroughEdge(direction) && GetElevationDifference(direction) <= 1) { //if there is not already a road in this direction, and if there isn't a river and if the elevation difference is only one
			SetRoad ((int)direction, true); // add a road

		}
	}	

	/// <summary>
	/// Removes all the roads in this cell
	/// </summary>
	public void RemoveRoads(){
		for (int i = 0; i < neighbours.Length; i++) {
			if (roads [i]) {
				SetRoad (i, false);
			}
		}
	}



	/// <summary>
	/// Adds or removes a road dependent on whether the bool is true or false	/// </summary>
	/// <param name="index">Index.</param>
	/// <param name="trueOrFalse">If set to <c>true</c> true or false.</param>
	void SetRoad(int index, bool trueOrFalse){
		roads [index] = trueOrFalse;
		neighbours [index].roads [(int)((HexDirection)index).Opposite ()] = trueOrFalse;
		neighbours [index].RefreshSelfOnly ();
		RefreshSelfOnly ();
	}


	public int GetElevationDifference(HexDirection direction){
		int difference = elevation - GetHexCellNeighbour (direction).elevation;
		return difference >= 0 ? difference : -difference;
	}
	#endregion



	#region SaveAndLoad

	public void Save(BinaryWriter writer){
		writer.Write ((int)terrainType); //index of the terrain type.
		writer.Write(elevation);
		writer.Write (waterLevel);
		writer.Write ((byte)urbanLevel);
		writer.Write ((byte)plantLevel);
		writer.Write (Walled);
		writer.Write ((int)baseAllegiance);
		writer.Write ((int)armyAllegiance);
		writer.Write(hasCity);

		if (hasIncomingRiver) {
			writer.Write ((byte)(incomingRiver + 128));
		} else {
			writer.Write ((byte)0);
		}

		if (hasOutgoingRiver) {
			writer.Write ((byte)(outGoingRiver + 128));
		} else {
			writer.Write ((byte)0);
		}
		int roadFlags = 0;
		for (int i = 0; i < roads.Length; i++) {
			//			writer.Write(roads[i]);
			if (roads[i]) {
				roadFlags |= 1 << i;
			}
		}
		writer.Write((byte)roadFlags);
	}


	public void Load(BinaryReader reader){
		TerrainType = (TerrainType)reader.ReadInt32();
		elevation = reader.ReadInt32 ();
		waterLevel = reader.ReadInt32 ();
		urbanLevel = reader.ReadByte ();
		plantLevel = reader.ReadByte ();
		walled = reader.ReadBoolean ();
		baseAllegiance = (Lookout.Allegiance)reader.ReadInt32 ();
		if (baseAllegiance == Lookout.Allegiance.CON) {
			hexGrid.conBaseLocationID = id;
		} else if (baseAllegiance == Lookout.Allegiance.USA) {
			hexGrid.usBaseLocationID = id;
		}

		armyAllegiance = (Lookout.Allegiance)reader.ReadInt32 ();
		if (armyAllegiance == Lookout.Allegiance.CON) {
			hexGrid.conArmiesStartLocationList.Add (id);
		} else if (armyAllegiance == Lookout.Allegiance.USA) {
			hexGrid.usArmiesStartLocationList.Add (id);
		}
		hasCity = reader.ReadBoolean ();

		byte riverData = reader.ReadByte ();
		if (riverData >= 128) {
			hasIncomingRiver = true;
			incomingRiver = (HexDirection)(riverData - 128);
		} else {
			hasIncomingRiver = false;
		}

		riverData = reader.ReadByte ();

		if (riverData >= 128) {
			hasOutgoingRiver = true;
			outGoingRiver = (HexDirection)(riverData - 128);
		} else {
			hasOutgoingRiver = false;
		}

		int roadflags = reader.ReadByte();
		for(int i = 0; i < roads.Length; i++){
			roads[i] = (roadflags & (1 << i)) != 0;
		}

		RefreshPosition ();
	}


	public void EnableHighlight(Color color, string labelTextToChangeTo){
		Image highlight = uiRect.GetChild (0).GetComponent<Image> ();
		highlight.enabled = true;
		highlight.color = color;

		Text labelText = uiRect.GetComponent<Text> ();
		labelText.text = labelTextToChangeTo;


	}

	public void DisableHighlight(){
		Image highlight = uiRect.GetChild (0).GetComponent<Image> ();
		highlight.enabled = false;
		Text labelText = uiRect.GetComponent<Text> ();
		labelText.text = "";
	}
	#endregion

	/// <summary>
	/// Calculates the cost to move to this cell. Usually invoked by the PathfindUsingAStar method in HexSelect, this helps to prioritise which cell should be chosen
	/// </summary>
	/// <returns>The cost to move to this cell.</returns>
	/// <param name="cellCameFrom">Cell came from.</param>
	/// <param name="costSoFar">Cost so far.</param>
	/// <param name="directionRelativeToCurrentCell">Direction relative to current cell.</param>
	public float CalculateCostToMoveToThisCell(HexCell cellCameFrom, HexDirection directionRelativeToCurrentCell){
		float totalMovementCost;

		if (terrainType == TerrainType.Grass || terrainType == TerrainType.Desert) {
			totalMovementCost = 1f;
		} else if (terrainType == TerrainType.Forest || terrainType == TerrainType.Hills) {
			totalMovementCost = 2f;
		} else {
			totalMovementCost = 1f;
		}

		if(!cellCameFrom.Walled && Walled || cellCameFrom.Walled && !Walled){
			if (!cellCameFrom.HasRoadThroughEdge (directionRelativeToCurrentCell) && !HasRoadThroughEdge(directionRelativeToCurrentCell.Opposite())) {
				totalMovementCost += 3f;	
			}

		}

		if (cellCameFrom.elevation < elevation) {

			if (elevation - cellCameFrom.elevation >= 1) {
				totalMovementCost += 1;
			}

		}



		if (HasBridge) {
			totalMovementCost += 1f;
		}


		if (cellCameFrom.HasRoadThroughEdge (directionRelativeToCurrentCell)) {
			if (HasRoadThroughEdge (directionRelativeToCurrentCell.Opposite())) {
				totalMovementCost *= 0.5f;
			}
		}


		return totalMovementCost;


	}

}
