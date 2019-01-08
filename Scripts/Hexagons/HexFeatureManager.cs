using UnityEngine;


public class HexFeatureManager : MonoBehaviour {

	public HexFeatureCollection[] urbanCollections, farmCollections, plantCollections;
	public HexMesh walls;
	Transform container;
	public Transform wallTower, bridge, cityPrefab;

	public Transform[] basePrefabs, armyPrefabs;

	public void Clear(){
		if (container) {
			Destroy (container.gameObject);
		}
		container = new GameObject ("Features Container").transform;
		container.SetParent (transform, false);
		walls.Clear ();
	}

	public void Apply(){
		walls.Apply();
	}

	public void AddFeature(HexCell cell, Vector3 position){
		if(cell.BaseAllegiance != Lookout.Allegiance.None){
			return;
		}
		HexHash hash = HexMetrics.SampleHashGrid (position);

		Transform prefab = PickPrefab (urbanCollections, cell.UrbanLevel, hash.a, hash.d);
		Transform otherPrefab = PickPrefab (farmCollections, cell.FarmLevel, hash.b, hash.d);

		float usedHash = hash.a;
		if (prefab) {
			if (otherPrefab & hash.b < hash.a) {
				prefab = otherPrefab;
				usedHash = hash.b;
			}
		} else if (otherPrefab) {
			prefab = otherPrefab;
			usedHash = hash.b;

		}
		otherPrefab = PickPrefab (plantCollections, cell.PlantLevel, hash.c, hash.d);

		if (prefab) {
			if (otherPrefab && hash.c < usedHash) {
				prefab = otherPrefab;
			}
		} else if (otherPrefab) {
			prefab = otherPrefab;
		}else{
			return;
		}


		Transform instance = Instantiate (prefab);
		instance.localPosition = HexMetrics.Perturb (position);
		instance.localRotation = Quaternion.Euler (0f, 360f * hash.e, 0f);
		instance.SetParent (container, false);
	}


	/// <summary>
	/// Picks the prefab from the list of HexFeatureCollections - so that we can add them in the inspector.
	/// </summary>
	/// <returns>The prefab.</returns>
	/// <param name="level">Level.</param>
	/// <param name="hash">Hash.</param>
	/// <param name="choice">Choice.</param>
	Transform PickPrefab(HexFeatureCollection[] collection, int level, float hash, float choice){
		if (level > 0) {
			float[] thresholds = HexMetrics.GetFeatureThresholds (level);
			for (int i = 0; i < thresholds.Length; i++) {
				if (hash < thresholds [i]) {
					return collection [i].Pick (choice);
				}
			}
		}
		return null;
	}


	#region Walls

	/// <summary>
	/// Adds the wall. Called by Triangulate Connection in HexChunk (as it sits on the cell boundary);
	/// </summary>
	/// <param name="nearEdge">Near edge.</param>
	/// <param name="nearCell">Near cell.</param>
	/// <param name="farEdge">Far edge.</param>
	/// <param name="farCell">Far cell.</param>
	public void AddWall(EdgeVertices nearEdge, HexCell nearCell, EdgeVertices farEdge, HexCell farCell, bool hasRiver, bool hasRoad){
		if(nearCell.Walled != farCell.Walled && !nearCell.IsUnderWater && !farCell.IsUnderWater && nearCell.GetEdgeTypeByCell(farCell) != HexMetrics.HexEdgeType.Cliff ){
			//Add a segment of wall for each vertices along the edge
			AddWallSegment (nearEdge.v1, farEdge.v1, nearEdge.v2, farEdge.v2);
			if (hasRoad || hasRiver) {
				//leave a gap
				AddWallCap(nearEdge.v2, farEdge.v2);
				AddWallCap (farEdge.v4, nearEdge.v4);
			} else {
				AddWallSegment (nearEdge.v2, farEdge.v2, nearEdge.v3, farEdge.v3);
				AddWallSegment (nearEdge.v3, farEdge.v3, nearEdge.v4, farEdge.v4);
			}
		AddWallSegment (nearEdge.v4, farEdge.v4, nearEdge.v5, farEdge.v5);
		}
	}

	public void AddWall(Vector3 c1, HexCell cell1, Vector3 c2, HexCell cell2, Vector3 c3, HexCell cell3){
		if (cell1.Walled) {
			//if cell one is walled
			if (cell2.Walled) {
				//if cell 1 and two are walled 
				if (!cell3.Walled) {
					//if cell one and two are walled but cell 3 is not
					AddWallSegment (c3, cell3, c1, cell1, c2, cell2);
				}
				//otherwise all are walled, so the'yre all internal
			} else if (cell3.Walled) {
				//if cell one and cell three are walled but not cell two
				AddWallSegment (c2, cell2, c3, cell3, c1, cell1);
			} else {
				//only cell 1 is walled
				AddWallSegment (c1, cell1, c2, cell2, c3, cell3);
			}
		} else if (cell2.Walled) {
			//cell 1 isn't walled, but cell two is

			if (cell3.Walled) {
				// if cell 1 isn't walled but cells 2 and cells 3 are;
				AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
			} else {
				//only cell2 is walled
				AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
			}
		} else if (cell3.Walled) {
			//cells 2 and 3 are not walled, but cell3 is
			AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
		}
			// otherwise none of the cells are walled

	}



	void AddWallSegment(Vector3 pivotPoint, HexCell pivotCell, Vector3 leftPoint, HexCell leftCell, Vector3 rightPoint, HexCell rightCell){

		if (pivotCell.IsUnderWater) {
			return;
		}

		bool hasLeftWall = !leftCell.IsUnderWater && pivotCell.GetEdgeTypeByCell (leftCell) != HexMetrics.HexEdgeType.Cliff;
		bool hasRightWall = !rightCell.IsUnderWater && pivotCell.GetEdgeTypeByCell (rightCell) != HexMetrics.HexEdgeType.Cliff;

		if (hasLeftWall) {
			if (hasRightWall) {
				bool hasTower = false;
				if(leftCell.Elevation == rightCell.Elevation){

				HexHash hash = HexMetrics.SampleHashGrid ((pivotPoint + leftPoint + rightPoint) * (1f / 3f));

				hasTower = hash.e < HexMetrics.wallTowerThreshold; 
				}
				AddWallSegment (pivotPoint, leftPoint, pivotPoint, rightPoint, hasTower); //this will put a wall up and towers on the corner
			
			} else if(leftCell.Elevation < rightCell.Elevation){
				AddWallWedge (pivotPoint, leftPoint, rightPoint);
			}else {
				AddWallCap (pivotPoint, leftPoint);
			}
		} else if(hasRightWall) {
			if (rightCell.Elevation < leftCell.Elevation) {
				AddWallWedge (rightPoint, pivotPoint, leftPoint);
			} else {
				AddWallCap (rightPoint, pivotPoint);
			}
		}

	

	}

	/// <summary>
	/// Creates a single segment of wall along an edge, based on the four vertices at the corner of the edge
	/// </summary>
	/// <param name="nearLeft">Near left.</param>
	/// <param name="farLeft">Far left.</param>
	/// <param name="nearRight">Near right.</param>
	/// <param name="farRight">Far right.</param>
	void AddWallSegment(Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight, bool addTower = false){

		nearLeft = HexMetrics.Perturb (nearLeft);
		farLeft = HexMetrics.Perturb (farLeft);
		nearRight = HexMetrics.Perturb (nearRight);
		farRight = HexMetrics.Perturb (farRight);

		Vector3 left = HexMetrics.WallLerp(nearLeft, farLeft); //Vector3.Lerp(nearLeft, farLeft, 0.5f); // get the halfway pointbetween tthe two left points, with correct height automaticall included.;
		Vector3 right = HexMetrics.WallLerp(nearRight, farRight); //Vector3 right = Vector3.Lerp(nearRight, farRight, 0.5f);// get the halfway point between the two right points; 

		Vector3 leftThicknessOffset = HexMetrics.WallThicknessOffset (nearLeft, farLeft);
		Vector3 rightThicknessOffset = HexMetrics.WallThicknessOffset (nearRight, farRight);
		float leftTop = left.y + HexMetrics.wallHeight;
		float rightTop = right.y + HexMetrics.wallHeight;

		Vector3 v1, v2, v3, v4; //define the vectors of the wall;

		v1 = v3 = left - leftThicknessOffset;
		v2 = v4  = right - rightThicknessOffset;
		v3.y = leftTop;
		v4.y = rightTop;
		walls.AddQuadUnperturbed(v1, v2, v3, v4);


		Vector3 t1 = v3;
		Vector3 t2 = v4;
		v1 = v3 = left + leftThicknessOffset;
		v2 = v4  = right + rightThicknessOffset;
		v3.y = leftTop;
		v4.y = rightTop;
		walls.AddQuadUnperturbed(v2, v1, v4, v3);

		walls.AddQuadUnperturbed (t1, t2, v3, v4);

		if (addTower) {
			//Add towers
			Transform towerInstance = Instantiate (wallTower);
			Vector3 rightDirection = right - left;
			rightDirection.y = 0f;
			towerInstance.transform.right = rightDirection;
			towerInstance.transform.localPosition = (left + right) * 0.5f;
			towerInstance.SetParent (container, false);
		}
	}

	/// <summary>
	/// Adds a cap to a wall (i.e. where a a wall is split by a river, it needs a cap on the two quads.
	/// </summary>
	/// <param name="near">Near.</param>
	/// <param name="far">Far.</param>
	void AddWallCap(Vector3 near, Vector3 far){
		near = HexMetrics.Perturb(near);
		far = HexMetrics.Perturb (far);
		Vector3 center = HexMetrics.WallLerp (near, far);
		Vector3 thickness = HexMetrics.WallThicknessOffset (near, far);

		Vector3 v1, v2, v3, v4;

		v1 = v3 = center - thickness;
		v2 = v4 = center + thickness;
		v3.y = v4.y = center.y + HexMetrics.wallHeight;
		walls.AddQuadUnperturbed (v1, v2, v3, v4);
	}


	/// <summary>
	/// Adds a wall wedge segment (for when the wall connects with a clifff).
	/// </summary>
	/// <param name="near">Near.</param>
	/// <param name="far">Far.</param>
	void AddWallWedge(Vector3 near, Vector3 far, Vector3 point){
		near = HexMetrics.Perturb(near);
		far = HexMetrics.Perturb (far);
		point = HexMetrics.Perturb (point);

		Vector3 center = HexMetrics.WallLerp (near, far);
		Vector3 thickness = HexMetrics.WallThicknessOffset (near, far);


		Vector3 v1, v2, v3, v4;
		Vector3 pointTop = point;
		point.y = center.y;
		v1 = v3 = center - thickness;
		v2 = v4 = center + thickness;
		v3.y = v4.y = pointTop.y = center.y + HexMetrics.wallHeight;
		walls.AddQuadUnperturbed (v1, point, v3, pointTop);

		walls.AddQuadUnperturbed (point, v2, pointTop, v4);
		walls.AddUnperturbedTriangle (pointTop, v3, v4);
	}

	public void AddBridge(Vector3 roadCenter1, Vector3 roadCenter2){
		roadCenter1 = HexMetrics.Perturb(roadCenter1);
		roadCenter2 = HexMetrics.Perturb(roadCenter2);
		Transform instance = Instantiate(bridge);
		instance.localPosition = (roadCenter1 + roadCenter2) * 0.5f;
		instance.forward = roadCenter2 - roadCenter1;

		float length = Vector3.Distance (roadCenter1, roadCenter2);
		instance.localScale = new Vector3 (1f, 1f, length * (1f / HexMetrics.bridgeDesignLength));
		instance.SetParent(container, false);
	}

	public void AddBase(HexCell cell , Vector3 position){

		Transform instance = Instantiate(basePrefabs[(int)cell.BaseAllegiance -1]);
		instance.localPosition = HexMetrics.Perturb (position);
		instance.GetComponent<SpawnableObject> ().cellLocationID = cell.id;
		HexHash hash = HexMetrics.SampleHashGrid (position);
		instance.localRotation = Quaternion.Euler (0f, 360f * hash.e, 0f);
		instance.SetParent (container, false);
	}

	public void AddArmy(HexCell cell , Vector3 position){

		Transform instance = Instantiate(armyPrefabs[(int)cell.ArmyAllegiance -1]);
		instance.localPosition = HexMetrics.Perturb (position);
		instance.GetComponent<SpawnableObject> ().cellLocationID = cell.id;
		HexHash hash = HexMetrics.SampleHashGrid (position);
		instance.localRotation = Quaternion.Euler (0f, 360f * hash.e, 0f);
		instance.SetParent (container, false);
	}

	public void AddCity(HexCell cell, Vector3 position){

		Transform instance = Instantiate(cityPrefab);
		instance.localPosition = HexMetrics.Perturb (position);
		instance.GetComponent<City> ().locationCellID = cell.id;
		HexHash hash = HexMetrics.SampleHashGrid (position);
		instance.localRotation = Quaternion.Euler (0f, 360f * hash.e, 0f);
		instance.SetParent (container, false);
	}
	#endregion
}
