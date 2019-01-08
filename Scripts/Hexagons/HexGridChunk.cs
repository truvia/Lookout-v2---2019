using UnityEngine.UI;
using UnityEngine;

public class HexGridChunk : MonoBehaviour {

	HexCell[] cells;
	//HexMesh hexMesh;
	public HexMesh terrain, rivers, roads, water, waterShore, estuaries;
	public Canvas gridCanvas;
	public HexFeatureManager features;

	void Awake(){
		gridCanvas = GetComponentInChildren<Canvas> ();
		//hexMesh = GetComponentInChildren<HexMesh> ();
		cells = new HexCell[HexMetrics.chunkSizex * HexMetrics.chunkSizeZ]; 
		ShowThisChunkCellIndexes (false);
	}




	public void AddCell(int index, HexCell cell){
		cells [index] = cell;
		cell.transform.SetParent (transform, false);
		cell.uiRect.SetParent (gridCanvas.transform, false);
		cell.chunk = this;
	}

	public void RefreshGrid(){
		enabled = true;
	}

	void LateUpdate(){
		TriangulateAllCells ();
		enabled = false;
	}

	public void ShowThisChunkCellIndexes(bool visible){
		
		gridCanvas.gameObject.SetActive (visible);
	}



	#region TriangulateRegion
	public void TriangulateAllCells(){
		terrain.Clear ();
		rivers.Clear ();
		roads.Clear ();
		water.Clear ();
		waterShore.Clear ();
		estuaries.Clear ();
		features.Clear ();
		foreach (HexCell cell in cells) {
			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				Triangulate (d, cell);
			}	

			//spawn terrain features
			if (!cell.IsUnderWater && !cell.HasRiver) {
				if (!cell.HasRoads) {
					features.AddFeature (cell, cell.Position);
				}

				if (cell.BaseAllegiance != Lookout.Allegiance.None) {
					features.AddBase (cell, cell.Position);
				}

				if (cell.ArmyAllegiance != Lookout.Allegiance.None) {
					features.AddArmy (cell, cell.Position);
				}

				if (cell.HasCity) {
					features.AddCity (cell, cell.Position);
				}
			}
		}

		terrain.Apply ();	
		rivers.Apply ();
		roads.Apply ();
		water.Apply ();
		waterShore.Apply ();
		estuaries.Apply ();
		features.Apply ();
	}


	void Triangulate(HexDirection direction, HexCell cell){
		Vector3 center = cell.Position;
		EdgeVertices e = new EdgeVertices (
			center + HexMetrics.GetFirstSolidCorner(direction),
			center + HexMetrics.GetSecondSolidCorner(direction),
			0.25f

		);

		if (cell.HasRiver) {
			if (cell.HasRiverThroughEdge(direction)) {
				e.v3.y = cell.StreamBedY;
				if (cell.HasRiverBeginOrEnd) {
					TriangulateWithRiverBeginningOrEnd(direction, cell, center, e);
				}
				else {
					TriangulateWithRiver(direction, cell, center, e);
				}
			}
			else {
				TriangulateAdjacentToRiver(direction, cell, center, e);
			}
		}
		else {
			TriangulateWithoutRiver (direction, cell, center, e);
			if (!cell.IsUnderWater && !cell.HasRoadThroughEdge (direction)) {
				features.AddFeature (cell, (center + e.v1 + e.v5) * (1f / 3f));
			}
			}
		if (direction <= HexDirection.SE) {
			TriangulateConnection (direction, cell, e);
		}

		if (cell.IsUnderWater) {
			TriangulateWater (direction, cell, center);
		}


	}


	/// <summary>
	/// Triangulates a cell with a river flowing through it (not where the river is begining or ending).
	/// </summary>
	/// <param name="direction">Direction that the river is flowing in.</param>
	/// <param name="cell">Cell.</param>
	/// <param name="center">Center</param>
	/// <param name="e">set of edge vertices (the line to the right)</param>
	void TriangulateWithRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e){
		Vector3 centerL, centerR;
		//if the cell has a river going in the direct opposite direction of the way it entered, then it is a straight river. 
		if (cell.HasRiverThroughEdge (direction.Opposite ())) {
			//For rivers that flow stright through a cell
			//firstly stretch the center into a  line;
			//assuming direction is flowing from the east to the west (along the flat line), we need the NE corner to start with. So get the north east corner of the "solid / inner" hex (direction.Previous()), times that by a quarter to get 
			centerL = center + HexMetrics.GetFirstSolidCorner (direction.Previous ()) * 0.25f;
			//assuming river is flowing east to west, the next corner y ou need dto get is the South East corner
			centerR = center + HexMetrics.GetSecondSolidCorner (direction.Next ()) * 0.25f;
		} else if (cell.HasRiverThroughEdge (direction.Next ())) {
			//then it is a very sharp turn - i.. the river flows in the east and goes out south east. 
			centerL = center;
			centerR = Vector3.Lerp (center, e.v5, 2f / 3f); //

		} else if (cell.HasRiverThroughEdge (direction.Previous ())) {
			//then it is also a very sharp turn - i.e. the river flows in form the east and goes out north east
			centerL = Vector3.Lerp (center, e.v1, 2f / 3f); //
			centerR = center;

		}else if (cell.HasRiverThroughEdge(direction.Next2())) {
			//it is a meander (i.e. the river has flown in from the east and goes out south west
			centerL = center;
			centerR = center +
				HexMetrics.GetSolidEdgeMiddle (direction.Next ()) * (0.5f * HexMetrics.innerToOuter);
		}
		else {
			//normal bend of a river 
			centerL = center +
				HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * (0.5f * HexMetrics.innerToOuter);
			centerR = center;
		}
		center = Vector3.Lerp (centerL, centerR, 0.5f);

		//the midpoints between  these (see page 111 of my blue book) are as follows:
		EdgeVertices m = new EdgeVertices (
			Vector3.Lerp (centerL, e.v1, 0.5f), //this is corner 1 ( i.e. the halfway point between the new Center line to the left and the first corner of the inner hex
			Vector3.Lerp (centerR, e.v5, 0.5f), // thhis is corner 2 (i.e. the halfway point between the new Center line to the right and the second corner of the inner hex
			1f/6f

		);
		//this results in us now have defined line between points m1, m2, m3, m4, m5 (or more accuarately, m.v1, m.v2, m.v3

		//we now need to set the height of the middle vertices so that it is lower thn the ones around in order to create a V channel. Since we've already done this for the outer hex limit, just copy this for the center and m middle vertices.  
		m.v3.y = center.y = e.v3.y;

		//since TriangulateEdgeStrip already creates a strip of quads, lets use that to fill in quads 1, 3, 5, 6.
		TriangulateEdgeStrip(m, cell.Color, e, cell.Color);

		terrain.AddTriangle (centerL, m.v1, m.v2);
		terrain.AddTriangleColor (cell.Color, cell.Color, cell.Color);

		terrain.AddQuad (centerL, center, m.v2, m.v3);
		terrain.AddQuadColor (cell.Color, cell.Color);

		terrain.AddQuad (center, centerR, m.v3, m.v4);
		terrain.AddQuadColor (cell.Color, cell.Color);
		terrain.AddTriangle (centerR, m.v4, m.v5);
		terrain.AddTriangleColor (cell.Color, cell.Color, cell.Color);
		if (!cell.IsUnderWater) {

			bool reversed = cell.IncomingRiver == direction; //incoming rivers should be reversed and as this is a cell in which water is flowing through, all we need to know if which direction the river is flowing from. If the direction we're triangulating is the same as the the direction of fthe incoming river, we can reverse
			TriangulateRiverQuad (centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY, 0.4f, reversed); // i.e. create a square that is as wide as the channel that reaches halfway between the center of the cell and the inner edge. 
			TriangulateRiverQuad (m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);
		}
	}

	/// <summary>
	/// Triangulates the with river beginning or ending in it.
	/// </summary>
	/// <param name="direction">Direction.</param>
	/// <param name="cell">Cell.</param>
	/// <param name="center">Center.</param>
	/// <param name="e">E.</param>
	void TriangulateWithRiverBeginningOrEnd(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e){


		EdgeVertices m = new EdgeVertices (Vector3.Lerp (center, e.v1, 0.4f), Vector3.Lerp (center, e.v5, 0.5f), 1 / 4);

		m.v3.y = e.v3.y;

		//This will create a cell in which the river is pinched, which is fine in this context.
		//Lets first creat the four quads using EdgeStrip
		TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
		TriangulateEdgeFan (center, m, cell.Color);


		//Now that the cell is full and chanel is created, lets create the actual water 

		if (!cell.IsUnderWater) {
			//the direction of the water depends on if this is the beigning or end - so reversed is true if it has water incoming (i.e. it is an end).
			bool reversed = cell.HasIncomingRiver;
			TriangulateRiverQuad (m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed); // this fills in the quads (but obviously not the triangles - see page 114 

			//Now we must make the triangular source or end point
			center.y = m.v2.y = m.v4.y = cell.RiverSurfaceY; //set the y height to the surface of the river;
			rivers.AddTriangle (center, m.v2, m.v4);
			if (reversed) {
				rivers.AddTriangleUV (new Vector2 (0.5f, 0.4f), new Vector2 (1f, 0.2f), new Vector2 (0f, 0.2f));
				
			} else {
				rivers.AddTriangleUV (new Vector2 (0.5f, 0.4f), new Vector2 (0f, 0.6f), new Vector2 (1f, 0.6f));
			}
		}
	}


	/// <summary>
	/// Triangulates the connection bridge between two cells (the boundary region of both cells between their solid hex centres and outer radius.
	/// </summary>
	/// <param name="direction">Direction.</param>
	/// <param name="cell">Cell.</param>
	/// <param name="e1">Edge vertices.</param>
	void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1){
		HexCell neighbour =	 cell.GetHexCellNeighbour (direction);
		if (neighbour == null) {
			return;
		}
		Vector3 bridge = HexMetrics.GetBridge (direction);
		bridge.y = neighbour.Position.y - cell.Position.y;
		EdgeVertices e2 = new EdgeVertices(e1.v1 + bridge, e1.v5 +bridge, 1f/4f);

		bool hasRiver = cell.HasRiverThroughEdge (direction);
		bool hasRoad = cell.HasRoadThroughEdge (direction);

		if (hasRiver) {
			//there is a river going through this edge
			e2.v3.y = neighbour.StreamBedY;

			if (!cell.IsUnderWater) {

				if (!neighbour.IsUnderWater) {
					//so lets create a river connection
					//firstly define the if the flow of water is reversed (is the incoming river equal to direction
					bool reversed = cell.HasIncomingRiver && cell.IncomingRiver == direction;
					TriangulateRiverQuad (e1.v2, e1.v4, e2.v2, e2.v4, cell.RiverSurfaceY, neighbour.RiverSurfaceY, 0.8f, reversed); 
				} else if(cell.Elevation > neighbour.WaterLevel) {

					TriangulateWaterFallsInWater (e1.v2, e1.v4, e2.v2, e2.v4, cell.RiverSurfaceY, neighbour.RiverSurfaceY, neighbour.WaterSurfaceY);
				} 
			}else if(!neighbour.IsUnderWater && neighbour.Elevation > cell.WaterLevel){
				TriangulateWaterFallsInWater(
					e2.v4, e2.v2, e1.v4, e1.v2,
					neighbour.RiverSurfaceY, cell.RiverSurfaceY,
					cell.WaterSurfaceY
				);
			}
		}

		if (cell.GetEdgeTypeByDirection (direction) == HexMetrics.HexEdgeType.Slope) {
			TriangulateEdgeTerraces(e1, cell, e2, neighbour, hasRoad); //if the edge between two cells is a slope,  create an "edge terrace" i.e. a terraced strip of quads. Also checks if there is a road going through as this invokes an additional method
		}else {
			TriangulateEdgeStrip(e1, cell.Color, e2, neighbour.Color, hasRoad); //create the usual strip of quads that connects two cells. Also checks to see if there is a road going through this edge as this invokes an additional method.

		}

		features.AddWall (e1, cell, e2, neighbour, hasRiver, hasRoad); // Adds walls (code in HexFeatureManager

		HexCell nextNeighbour = cell.GetHexCellNeighbour (direction.Next ());

		if (direction <= HexDirection.E && nextNeighbour != null) {
			Vector3 v5 = e1.v5 + HexMetrics.GetBridge (direction.Next ());
			v5.y = nextNeighbour.Position.y;


			if (cell.Elevation <= neighbour.Elevation) {
				if (cell.Elevation <= nextNeighbour.Elevation) {
					TriangulateCorner(e1.v5, cell, e2.v5, neighbour, v5, nextNeighbour);
				}
				else {
					TriangulateCorner(v5, nextNeighbour, e1.v5, cell, e2.v5, neighbour);
				}
			}
			else if (neighbour.Elevation <= nextNeighbour.Elevation) {
				TriangulateCorner(e2.v5, neighbour, v5, nextNeighbour, e1.v5, cell);
			}
			else {
				TriangulateCorner(v5, nextNeighbour, e1.v5, cell, e2.v5, neighbour);
			}

		}
	}

	/// <summary>
	/// Triangulates the slope of the corner between three cells.
	/// </summary>
	/// <param name="bottom">Vector3 location of the lowest indeces of the three cells. 
	/// </param>
	/// <param name="bottomCell">lowest cell.</param>
	/// <param name="left">Left most cell.</param>
	/// <param name="leftCell">Left cell.</param>
	/// <param name="right">Right.</param>
	/// <param name="rightCell">Right cell.</param>
	void TriangulateCorner(
		Vector3 bottom, HexCell bottomCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell
	){
		HexMetrics.HexEdgeType leftEdgeType = bottomCell.GetEdgeTypeByCell (leftCell); //the type of slope between the bottommost cell and the cell to its left 
		HexMetrics.HexEdgeType rightEdgeType = bottomCell.GetEdgeTypeByCell (rightCell); // the type of slope between the bottomost cel land the cell to its right


		if (leftEdgeType == HexMetrics.HexEdgeType.Slope) {
			if (rightEdgeType == HexMetrics.HexEdgeType.Slope) {
				TriangulateCornerTerraces (
					bottom, bottomCell, left, leftCell, right, rightCell
				);
			} else if (rightEdgeType == HexMetrics.HexEdgeType.Flat) {
				TriangulateCornerTerraces (
					left, leftCell, right, rightCell, bottom, bottomCell
				);

			} else {
				TriangulateCornerTerracesCliff (
					bottom, bottomCell, left, leftCell, right, rightCell
				);

			}
		} else if (rightEdgeType == HexMetrics.HexEdgeType.Slope) {
			if (leftEdgeType == HexMetrics.HexEdgeType.Flat) {
				TriangulateCornerTerraces (
					right, rightCell, bottom, bottomCell, left, leftCell
				);

			} else {

				TriangulateCornerCliffTerraces (bottom, bottomCell, left, leftCell, right, rightCell);
				return;
			}
		} else if (leftCell.GetEdgeTypeByCell (rightCell) == HexMetrics.HexEdgeType.Slope) {
			if (leftCell.Elevation < rightCell.Elevation) {
				TriangulateCornerCliffTerraces (
					right, rightCell, bottom, bottomCell, left, leftCell
				);
			} else {
				TriangulateCornerTerracesCliff (
					left, leftCell, right, rightCell, bottom, bottomCell
				);
			}

		} else {

			terrain.AddTriangle (bottom, left, right);
			terrain.AddTriangleColor (bottomCell.Color, leftCell.Color, rightCell.Color);
		}

		features.AddWall(bottom, bottomCell, left, leftCell, right, rightCell); //triangulate the corner of the walls


	}


	void TriangulateCornerTerraces(
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell

	){

		Vector3 v3 = HexMetrics.TerraceLerp (begin, left, 1);
		Vector3 v4 = HexMetrics.TerraceLerp (begin, right, 1);
		Color c3 = HexMetrics.TerraceLerp (beginCell.Color, leftCell.Color, 1);
		Color c4 = HexMetrics.TerraceLerp (beginCell.Color, rightCell.Color, 1);

		terrain.AddTriangle(begin, v3, v4);
		terrain.AddTriangleColor(beginCell.Color, c3, c4);

		for (int i = 2; i < HexMetrics.terraceSteps; i++) {
			Vector3 v1 = v3;
			Vector3 v2 = v4;
			Color c1 = c3;
			Color c2 = c4;
			v3 = HexMetrics.TerraceLerp(begin, left, i);
			v4 = HexMetrics.TerraceLerp(begin, right, i);
			c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
			c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);
			terrain.AddQuad(v1, v2, v3, v4);
			terrain.AddQuadColor(c1, c2, c3, c4);
		}

		terrain.AddQuad(v3, v4, left, right);
		terrain.AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);


	}
	/// <summary>
	/// Triangulates the corner terraces when a step terrace is adjacent to a cliff.
	/// </summary>
	void TriangulateCornerTerracesCliff (
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell
	) {
		float b = 1f / (rightCell.Elevation - beginCell.Elevation);
		if (b < 0) {
			b = -b;
		}
		Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
		Color boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, b);


		TriangulateBoundaryTriangle (begin, beginCell, left, leftCell, boundary, boundaryColor);

		if (leftCell.GetEdgeTypeByCell (rightCell) == HexMetrics.HexEdgeType.Slope) {
			TriangulateBoundaryTriangle (left, leftCell, right, rightCell, boundary, boundaryColor);
		} else {
			terrain.AddUnperturbedTriangle(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
			terrain.AddTriangleColor (leftCell.Color, rightCell.Color, boundaryColor);
		}
	}

	void TriangulateCornerCliffTerraces (
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 right, HexCell rightCell
	) {
		float b = 1f / (leftCell.Elevation - beginCell.Elevation);
		if (b < 0) {
			b = -b;
		}
		Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
		Color boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, b);


		TriangulateBoundaryTriangle (right, rightCell, begin, beginCell, boundary, boundaryColor);

		if (leftCell.GetEdgeTypeByCell (rightCell) == HexMetrics.HexEdgeType.Slope) {
			TriangulateBoundaryTriangle (left, leftCell, right, rightCell, boundary, boundaryColor);
		} else {
			terrain.AddUnperturbedTriangle(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
			terrain.AddTriangleColor (leftCell.Color, rightCell.Color, boundaryColor);
		}
	}


	void TriangulateBoundaryTriangle (
		Vector3 begin, HexCell beginCell,
		Vector3 left, HexCell leftCell,
		Vector3 boundary, Color boundaryColor
	) {
		Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
		Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

		terrain.AddUnperturbedTriangle(HexMetrics.Perturb(begin), v2, boundary);
		terrain.AddTriangleColor(beginCell.Color, c2, boundaryColor);

		for (int i = 2; i < HexMetrics.terraceSteps; i++) {
			Vector3 v1 = v2;
			Color c1 = c2;
			v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
			c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
			terrain.AddUnperturbedTriangle(v1, v2, boundary);
			terrain.AddTriangleColor(c1, c2, boundaryColor);
		}

		terrain.AddUnperturbedTriangle(v2, HexMetrics.Perturb(left), boundary);
		terrain.AddTriangleColor(c2, leftCell.Color, boundaryColor);
	}



	/// <summary>
	/// Creates a "fan" of triangles spiralling into a central point for each edge of the hex. Often use to fill in the central part of the hex. 
	/// </summary>
	/// <param name="center">Center.</param>
	/// <param name="edge">Edge.</param>
	/// <param name="color">Color.</param>
	void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color){
		terrain.AddTriangle (center, edge.v1, edge.v2);
		terrain.AddTriangleColor (color, color, color);


		terrain.AddTriangle (center, edge.v2, edge.v3);
		terrain.AddTriangleColor (color, color, color);


		terrain.AddTriangle (center, edge.v3, edge.v4);
		terrain.AddTriangleColor (color, color, color);


		terrain.AddTriangle (center, edge.v4, edge.v5);
		terrain.AddTriangleColor (color, color, color);

	}

	/// <summary>
	/// Creates a strip of four quads using two sets of vertices (the left line and the right line) - used mostly for creating a flat edge connection between two hex cells. There is an optional parameter "Has road" that by default is set to false, if the cell has a road, set to true.
	/// </summary>
	/// <param name="e1">All the points along the leftmost line.</param>
	/// <param name="c1">The color on the left.</param>
	/// <param name="e2">All the points along the rightmost line.</param>
	/// <param name="c2">The color on the right.</param>
	/// <param name="hasRoad">(Optional) bool to say whether this has a road</param>
	void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2, bool hasRoad = false){
		terrain.AddQuad (e1.v1, e1.v2, e2.v1, e2.v2);
		terrain.AddQuadColor (c1, c2);

		terrain.AddQuad (e1.v2, e1.v3, e2.v2, e2.v3);
		terrain.AddQuadColor (c1, c2);

		terrain.AddQuad (e1.v3, e1.v4, e2.v3, e2.v4);
		terrain.AddQuadColor (c1, c2);

		terrain.AddQuad (e1.v4, e1.v5, e2.v4, e2.v5);
		terrain.AddQuadColor (c1, c2);

		if (hasRoad) {
			TriangulateRoadSegment (e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4);
		}
	}


	/// <summary>
	/// Fills in the rest of the cell that is left empty by a river
	/// </summary>
	/// <param name="direction">Direction.</param>
	/// <param name="cell">Cell.</param>
	/// <param name="center">Center.</param>
	/// <param name="e">E.</param>
	void TriangulateAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e){
			
		if (cell.HasRoads) {
			TriangulateRoadAdjacentToRiver (direction, cell, center, e);
		}

		if (cell.HasRiverThroughEdge (direction.Next ())) {
			if (cell.HasRiverThroughEdge (direction.Previous ())) {
				//Then the bit that we're filling in of the cell is on the inside of the curve
				center += HexMetrics.GetSolidEdgeMiddle (direction) * (HexMetrics.innerToOuter * 0.5f);
			}else if(cell.HasRiverThroughEdge(direction.Previous2())){
				//then we must be on one side of a a straight river.
				center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
			}
		} else if(cell.HasRiverThroughEdge(direction.Previous()) && cell.HasRiverThroughEdge(direction.Next2())){
			//then we must be on the other side of a straight river - so move the center to the next solid corner 
			center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
		}





		EdgeVertices m = new EdgeVertices(
			Vector3.Lerp(center, e.v1, 0.5f),
			Vector3.Lerp(center, e.v5, 0.5f),
			1f/4f
		);

		TriangulateEdgeStrip (m, cell.Color, e, cell.Color);
		TriangulateEdgeFan (center, m, cell.Color);

		//Add features
		if (!cell.IsUnderWater && !cell.HasRoadThroughEdge (direction)) {
			features.AddFeature (cell, (center + e.v1 + e.v5) * (1f / 3f));
		}
	}

	/// <summary>
	/// Creates a terraced version of the Edge strip (used for triangulating edges of two cells where there is a height difference between two hex cells)
	/// </summary>
	/// <param name="begin">Beginning edge.</param>
	/// <param name="beginCell">Begin cell.</param>
	/// <param name="end">End edge.</param>
	/// <param name="endCell">End cell.</param>
	void TriangulateEdgeTerraces (EdgeVertices begin, HexCell beginCell, EdgeVertices end, HexCell endCell, bool hasRoad) {
		EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
		Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

		TriangulateEdgeStrip(begin, beginCell.Color, e2, c2, hasRoad);

		for (int i = 2; i < HexMetrics.terraceSteps; i++) {
			EdgeVertices e1 = e2;
			Color c1 = c2;
			e2 = EdgeVertices.TerraceLerp(begin, end, i);
			c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);
			TriangulateEdgeStrip(e1, c1, e2, c2, hasRoad);
		}

		TriangulateEdgeStrip(e2, c2, end, endCell.Color, hasRoad);
	}


	#endregion

	#region Rivers 
	/// <summary>
	/// Builds the quads that make up the river water. This one only takes in one height (for flat cells or cells of the same height) there is an identical method of the same name for cliffs etc. 
	/// </summary>
	/// <param name="v1">V1.</param>
	/// <param name="v2">V2.</param>
	/// <param name="v3">V3.</param>
	/// <param name="v4">V4.</param>
	/// <param name="y">The height of the river.</param>
	/// <param name="v"></param> 
	/// <param name="reversed">Should the river flow be reversed</param> 
	void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y, float v, bool reversed){
		TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed);
	}

	/// <summary>
	/// Builds the quads that make up the river water. This one takes in two heights, for cliffs etc. There is an identical method of the same name for celsl of the same height.
	/// </summary>
	/// <param name="v1">V1.</param>
	/// <param name="v2">V2.</param>
	/// <param name="v3">V3.</param>
	/// <param name="v4">V4.</param>
	/// <param name="y1">The height of the river in cell a.</param>
	/// <param name="y2"> the height of the river in cell b </param> 
	///  <param name="v"> v </param> 
	/// <param name="reversed">Should the river flow be reversed</param> 
	void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float v, bool reversed){
		v1.y = v2.y =  y1;
		v3.y = v4.y = y2;

		rivers.AddQuad (v1, v2, v3, v4);

		if (reversed) {
			rivers.AddQuadUV (1f, 0f, 0.8f -  v, 0.6f - v);
		} else {
			rivers.AddQuadUV (0f, 1f, v,  v + 0.25f);

		}



	}

	#endregion

	#region Roads

	void TriangulateRoadSegment( Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5, Vector3 v6){
		roads.AddQuad (v1, v2, v4, v5);
		roads.AddQuad (v2, v3, v5, v6);

		roads.AddQuadUV (0f, 1f, 0f, 0f);
		roads.AddQuadUV (1f, 0f, 0f, 0f);
	}

	void TriangulateRoad(Vector3 center, Vector3 mLeft, Vector3 mRight, EdgeVertices e, bool hasRoadThroughCellEdge){
		//To build roads we are going to two straight quads from the edge vertices and the mid point, then from the mid point to the center (in a triangle). 

		if (hasRoadThroughCellEdge) {
			Vector3 mCenter = Vector3.Lerp (mLeft, mRight, 0.5f); // get the halfway point between vertices MLeft and mRight

			TriangulateRoadSegment (mLeft, mCenter, mRight, e.v2, e.v3, e.v4);
	
			roads.AddTriangle (center, mLeft, mCenter);
			roads.AddTriangle (center, mCenter, mRight);

			roads.AddTriangleUV (new Vector2 (1f, 0f), new Vector2 (0f, 0f), new Vector2 (1f, 0f));

			roads.AddTriangleUV (new Vector2 (1f, 0f), new Vector2 (1f, 0f), new Vector2 (0f, 0f));

		} else {
			TriangulateRoadEdge (center, mLeft, mRight);
		}
	}

	void TriangulateWithoutRiver (
		HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
	) {
		TriangulateEdgeFan(center, e, cell.Color);

		if (cell.HasRoads) {
			Vector2 interpolators = GetRoadInterpolators(direction, cell);
			TriangulateRoad(
				center,
				Vector3.Lerp(center, e.v1, interpolators.x),
				Vector3.Lerp(center, e.v5, interpolators.y),
				e, cell.HasRoadThroughEdge(direction)
			);
		}
	}

	void TriangulateRoadEdge(Vector3 center, Vector3 mLeft, Vector3 mRight){
		roads.AddTriangle (center, mLeft, mRight);
		roads.AddTriangleUV (new Vector2 (1f, 0f), new Vector2 (0f, 0f), new Vector2 (0f, 0f));

	}



	Vector3 GetRoadInterpolators(HexDirection direction, HexCell cell){
		Vector2 interpolators; 
		if(cell.HasRoadThroughEdge(direction)	){

			interpolators.x = interpolators.y = 0.5f;

		}else{

			interpolators.x = cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f: 0.25f;
			interpolators.y=
			cell.HasRoadThroughEdge(direction.Next()) ? 0.5f: 0.25f; 
		}

			return interpolators;

	
	
	}

	void TriangulateRoadAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e){
		bool hasRoadThroughEdge = cell.HasRoadThroughEdge (direction);
		bool previousHasRiver = cell.HasRiverThroughEdge (direction.Previous ());
		bool nextHasRiver = cell.HasRiverThroughEdge (direction.Next ());


		Vector2 interpolators = GetRoadInterpolators (direction, cell);
		Vector3 roadCenter = center;

		if (cell.HasRiverBeginOrEnd) {
			roadCenter += HexMetrics.GetSolidEdgeMiddle (cell.RiverBeginOrEndDirection.Opposite ()) * (1f / 3f);
		} else if (cell.IncomingRiver == cell.OutGoingRiver.Opposite ()) {
			//the road is split by a river.. 
			Vector3 corner;
			if (previousHasRiver) {

				if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge (direction.Next ())) {
					return;
				}
				corner = HexMetrics.GetSecondSolidCorner (direction);
			} else {


				if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge (direction.Previous ())) {
					return;
				}

				corner = HexMetrics.GetFirstSolidCorner (direction);
			}


			roadCenter += corner * 0.5f;
			if (cell.IncomingRiver == direction.Next () && (
			        cell.HasRoadThroughEdge (direction.Next2 ()) ||
			        cell.HasRoadThroughEdge (direction.Opposite ())
			    )) {
				features.AddBridge (roadCenter, center - corner * 0.5f);
				cell.HasBridge = true;
			} else {
				cell.HasBridge = false;
			}
			center += corner * 0.25f; 

		} else if (cell.IncomingRiver == cell.OutGoingRiver.Previous ()) {
			roadCenter -= HexMetrics.GetSecondSolidCorner (cell.IncomingRiver) * 0.2f;
		} else if (cell.IncomingRiver == cell.OutGoingRiver.Next ()) {
			roadCenter -= HexMetrics.GetFirstSolidCorner (cell.IncomingRiver) * 0.2f;
		} else if (previousHasRiver && nextHasRiver) {
			if (!hasRoadThroughEdge) {
				return;
			}
			Vector3 offset = HexMetrics.GetSolidEdgeMiddle (direction) * HexMetrics.innerToOuter;
			roadCenter += offset * 0.7f;
			center += offset * 0.5f;
		} else {
			HexDirection middle;
			if (previousHasRiver) {
				middle = direction.Next ();

			} else if (nextHasRiver) {
				middle = direction.Previous ();
			} else {
				middle = direction;
			}
			if (!cell.HasRoadThroughEdge (middle) && !cell.HasRoadThroughEdge (middle.Previous ()) && !cell.HasRoadThroughEdge (middle.Next ())) {
				return;
			
			}
			Vector3 offset 	= HexMetrics.GetSolidEdgeMiddle (middle);
			center += offset * 0.25f;
			if (direction == middle &&	cell.HasRoadThroughEdge(direction.Opposite())) {
				features.AddBridge(
					roadCenter,
					center - offset * (HexMetrics.innerToOuter * 0.7f)
				);
				cell.HasBridge = true;
			}
		}

		Vector3 mLeft = Vector3.Lerp (center, e.v1, interpolators.x);
		Vector3 mRight = Vector3.Lerp (center, e.v5, interpolators.y);
		TriangulateRoad (roadCenter, mLeft, mRight, e, hasRoadThroughEdge);


		if(previousHasRiver){


			TriangulateRoadEdge(roadCenter, center, mLeft);
		}

		if(nextHasRiver){
			TriangulateRoadEdge(roadCenter, mRight, center);
		}
	}
	#endregion

	#region Water
	void TriangulateWater(HexDirection direction, HexCell cell, Vector3 center){

		//fill out the center of the hexCell 
		center.y = cell.WaterSurfaceY;

		//triangulate the shore line between this cell and the next, if the next neighbour is not underwater. 
		HexCell neighbour = cell.GetHexCellNeighbour (direction);
		if (neighbour != null && !neighbour.IsUnderWater) {
			TriangulateWaterShore (direction, cell, neighbour, center);
		
		} else {
			//otherwise triangulate normal, open water. 
			TriangulateOpenWater (direction, cell, neighbour, center);
		}

	}

	void TriangulateOpenWater(HexDirection direction, HexCell cell, HexCell neighbour, Vector3 center){
		Vector3 c1 = center + HexMetrics.GetFirstWaterCorner (direction);
		Vector3 c2 = center + HexMetrics.GetSecondWaterCorner (direction);

		water.AddTriangle (center, c1, c2);

		//connect adjacent hexcells that are water cells with a quad;
		if(direction <= HexDirection.SE && neighbour != null){
			

			Vector3 bridge = HexMetrics.GetWaterBridge (direction);
			Vector3 e1 = c1 + bridge;
			Vector3 e2 = c2 + bridge;

			water.AddQuad (c1, c2, e1, e2); //this connects the adjacent hexes, but leaves the triangle between them unharmed. we need to therefoere fill this in

			if (direction <= HexDirection.E) {
				HexCell nextNeighbour = cell.GetHexCellNeighbour (direction.Next());
				if (nextNeighbour == null || !nextNeighbour.IsUnderWater) {
					return;
				}

				water.AddTriangle (c2, e2, c2 + HexMetrics.GetWaterBridge (direction.Next ()));

			}
		}
	}


	void TriangulateWaterShore(HexDirection direction, HexCell cell, HexCell neighbour, Vector3 center){
		//the edge of this cell is perturbed, so we need to make  water triangles perturbed as well


		EdgeVertices e1 = new EdgeVertices(
			center + HexMetrics.GetFirstWaterCorner(direction),
			center + HexMetrics.GetSecondWaterCorner(direction), 1f/4f
		);


		water.AddTriangle (center, e1.v1, e1.v2);
		water.AddTriangle (center, e1.v2, e1.v3);
		water.AddTriangle (center, e1.v3, e1.v4);
		water.AddTriangle (center, e1.v4, e1.v5);


		//now we need to fill the gap between the centerHex of this cell and the centerhex of the next cell (and make the watershore froth)
		//Vector3 bridge = HexMetrics.GetWaterBridge(direction);
		Vector3 center2 = neighbour.Position;
		center2.y = center.y;

		EdgeVertices e2 = new EdgeVertices (
			                  center2 + HexMetrics.GetSecondSolidCorner (direction.Opposite ()),
			                  center2 + HexMetrics.GetFirstSolidCorner (direction.Opposite ()),
			                  1 / 4f
		                  );

		if (cell.HasRiverThroughEdge (direction)) {
			TriangulateEstuary (e1, e2, cell.IncomingRiver == direction);
		}else{
		waterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
		waterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
		waterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
		waterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
		waterShore.AddQuadUV (0f, 0f, 0f, 1f);
		waterShore.AddQuadUV (0f, 0f, 0f, 1f);
		waterShore.AddQuadUV (0f, 0f, 0f, 1f);
		waterShore.AddQuadUV (0f, 0f, 0f, 1f);
		//this leaves a litttle triangle missing between three cells, so fill that in
		}

		HexCell nextNeighbour = cell.GetHexCellNeighbour (direction.Next ());
		if (nextNeighbour != null) {
//			Vector3 center3 = nextNeighbour.Position;
//			center3.y = center.y;
			Vector3 v3 = nextNeighbour.Position + (nextNeighbour.IsUnderWater ?
				HexMetrics.GetFirstWaterCorner(direction.Previous()) :
				HexMetrics.GetFirstSolidCorner(direction.Previous()));
			v3.y = center.y;
			waterShore.AddTriangle (e1.v5, e2.v5, v3);

			waterShore.AddTriangleUV(
				new Vector2(0f, 0f),
				new Vector2(0f, 1f),
				new Vector2(0f, nextNeighbour.IsUnderWater ? 0f : 1f)
			);
		}
	}


	void TriangulateWaterFallsInWater(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float waterY){
		v1.y = v2.y = y1;
		v3.y = v4.y = y2;
		v1 = HexMetrics.Perturb (v1);
		v2 = HexMetrics.Perturb (v2);
		v3 = HexMetrics.Perturb (v3);
		v4 = HexMetrics.Perturb (v4);
		float t = (waterY - y2) / (y1 - y2);
		v3 = Vector3.Lerp (v3, v1, t);
		v4 = Vector3.Lerp (v4, v2, t);
		rivers.AddQuadUnperturbed(v1, v2, v3, v4);
		rivers.AddQuadUV(0f, 1f, 0.8f, 1f);
	}


	void TriangulateEstuary(EdgeVertices e1, EdgeVertices e2, bool incomingRiver){
		waterShore.AddTriangle (e2.v1, e1.v2, e1.v1);
		waterShore.AddTriangle (e2.v5, e1.v5, e1.v4);
		waterShore.AddTriangleUV (new Vector2 (0f, 1f), new Vector2 (0f, 0f), new Vector2 (0f, 0f));
		waterShore.AddTriangleUV(new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));

		estuaries.AddQuad (e2.v1, e1.v2, e2.v2, e1.v3);
		estuaries.AddTriangle (e1.v3, e2.v2, e2.v4);
		estuaries.AddQuad (e1.v3, e1.v4, e2.v4, e2.v5);

		estuaries.AddQuadUV (new Vector2 (0f, 1f), new Vector2 (0f, 0f), new Vector2 (1f, 1f), new Vector2 (0f, 0f));
		estuaries.AddTriangleUV (new Vector2 (0f, 0f), new Vector2 (1f, 1f), new Vector2 (1f, 1f));

		estuaries.AddQuadUV (
			new Vector2 (0f, 0f), new Vector2 (0f, 0f),
			new Vector2 (1f, 1f), new Vector2 (0f, 1f)
		);


		if (incomingRiver) {
			estuaries.AddQuadUV2 (
				new Vector2 (1.5f, 1f), new Vector2 (0.7f, 1.15f),
				new Vector2 (1f, 0.8f), new Vector2 (0.5f, 1.1f)
			);
			estuaries.AddTriangleUV2 (
				new Vector2 (0.5f, 1.1f),
				new Vector2 (1f, 0.8f),
				new Vector2 (0f, 0.8f)
			);
			estuaries.AddQuadUV2 (
				new Vector2 (0.5f, 1.1f), new Vector2 (0.3f, 1.15f),
				new Vector2 (0f, 0.8f), new Vector2 (-0.5f, 1f)
			);
		} else {
			estuaries.AddQuadUV2 (
				new Vector2 (-0.5f, -0.2f), new Vector2 (0.3f, -0.35f),
				new Vector2 (0f, 0f), new Vector2 (0.5f, -0.3f)
			);

			estuaries.AddTriangleUV2 (
				new Vector2 (0.5f, -0.3f),
				new Vector2 (0f, 0f),
				new Vector2 (1f, 0f)
			);
			estuaries.AddQuadUV2 (
				new Vector2 (0.5f, -0.3f), new Vector2 (0.7f, -0.35f),
				new Vector2 (1f, 0f), new Vector2 (1.5f, -0.2f)
			);
		}
	}

	#endregion
}
	