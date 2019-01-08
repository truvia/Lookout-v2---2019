using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HexGrid : MonoBehaviour {
	#region largerMaps
//	public int chunkCountX = 4, chunkCountZ = 3;
	public int cellCountX = 20, cellCountZ = 15;
	int chunkCountX, chunkCountZ;
	public HexGridChunk chunkPrefab;

	public HexGridChunk[] chunks;
	#endregion

	public HexCell cellPrefab;
	public Text cellLabelPrefab;

	public Texture2D noiseSource;

	public bool showCoordinates;

	MeshCollider meshCollider;


	public 	HexCell[] cells;
	public int conBaseLocationID = -1;
	public int usBaseLocationID = -1;
	public List<int> conArmiesStartLocationList = new List<int>();
	public List<int> usArmiesStartLocationList = new List<int>();


	#region colours
	public Color highlightRouteColor;
	public Color routeOutOfRangeColor;


	#endregion

	#region testvars
	HexCell startCell;
	public	SaveLoadMapMenu saveLoadMapMenu;
	#endregion

	#region TerrainFeaturesVars
	public int seed;
	#endregion




	void Awake () {

		meshCollider = gameObject.AddComponent<MeshCollider> ();
		HexMetrics.noiseSource = noiseSource;
		HexMetrics.InitialiseHashGrid (seed); //initialise the hash grid used to generate terrain features.

		if (SceneManager.GetActiveScene ().name == "MapEditor") {
			CreateMap (cellCountX, cellCountZ);
		} else if(SceneManager.GetActiveScene().name == "MainGame") {
			string path = Path.Combine (Application.persistentDataPath, "test.map");

			LoadMap (path);

		}
	}

	public void LoadMap(string path){
		if (!File.Exists (path)) {
			Debug.LogError ("File does not exist " + path);
			return;
		}

		conArmiesStartLocationList.Clear ();
		conBaseLocationID = -1;
		usArmiesStartLocationList.Clear ();
		usBaseLocationID = -1;
		using (BinaryReader reader = new BinaryReader (File.OpenRead(path))) {
			int header = reader.ReadInt32();
			if (header <= 1) {
				Load(reader, header);
				HexMapCamera.ValidatePosition ();
			}
			else {
				Debug.LogWarning("Unknown map format " + header);
			}

		}

	}



	public bool CreateMap(int x, int z){
		Debug.Log ("Create: x is " + x + " and z is " + z);

		if (x <= 0 || x % HexMetrics.chunkSizex != 0 ||
			z <= 0 || z % HexMetrics.chunkSizeZ != 0) {
			Debug.LogWarning ("Unsupported Map Size");
			return false;
		}	

		if (chunks != null) {
			for (int i = 0; i < chunks.Length; i++) {
				Destroy (chunks [i].gameObject);
			}
		}
		cellCountX = x;
		cellCountZ = z;


		chunkCountX = cellCountX / HexMetrics.chunkSizex;
		chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;

		conArmiesStartLocationList.Clear ();
		conBaseLocationID = -1;
		usArmiesStartLocationList.Clear ();
		usBaseLocationID = -1;

		CreateChunks ();
		CreateCells ();
		return true;
	}

	void OnEnable(){
		if (!HexMetrics.noiseSource) {
			HexMetrics.noiseSource = noiseSource;
			HexMetrics.InitialiseHashGrid (seed);
		}
	}


	void CreateCells () {
		cells = new HexCell[cellCountZ * cellCountX];

		for (int z = 0, i = 0; z < cellCountZ; z++) {
			for (int x = 0; x < cellCountX; x++) {
				CreateCell(x, z, i++);
			}
		}
	}

	void CreateCell (int x, int z, int i) {
		Vector3 position;

		position.x = (x + z * 0.5f - z/2) * (HexMetrics.innerRadius * 2f);
		position.y = 0f;
		position.z = z * (HexMetrics.outerRadius * 1.5f);


		HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
		cell.transform.localPosition = position;
		cell.id = i;
		cell.coordinates = HexCoordinates.FromOffsetCoordinates (x, z);
		cell.TerrainType = TerrainType.Grass;

		cell.name = "Cell " + i;


		if (x > 0) {
			cell.SetNeighbour (HexDirection.W, cells [i - 1]);
		}

		if (z > 0) {
			if ((z & 1) == 0) {
				cell.SetNeighbour (HexDirection.SE, cells[i - cellCountX]);
					if(x > 0){
					cell.SetNeighbour(HexDirection.SW, cells[i-cellCountX - 1]);
					} 

			
			}else{
				cell.SetNeighbour (HexDirection.SW, cells [i - cellCountX]);
				if (x < cellCountX - 1) {
					cell.SetNeighbour (HexDirection.SE, cells [i - cellCountX + 1]);
				}
			}
		}
			if (showCoordinates) {
			Text label = Instantiate<Text> (cellLabelPrefab);
			label.rectTransform.anchoredPosition = new Vector2 (position.x, position.z);
			label.tag = "coordinateLabel";
			label.text = cell.id.ToString();
			cell.uiRect = label.rectTransform;
			cell.Elevation = 0;
			AddCellToChunk (x, z, cell);
		}
	}

	void AddCellToChunk(int x, int z, HexCell cell){
		int chunkX = x / HexMetrics.chunkSizex;
		int chunkz = z / HexMetrics.chunkSizeZ;
		HexGridChunk chunk = chunks [chunkX + chunkz * chunkCountX];

		int localX = x - chunkX * HexMetrics.chunkSizex;
		int localZ = z - chunkz * HexMetrics.chunkSizeZ;
		chunk.AddCell (localX + localZ * HexMetrics.chunkSizex, cell);
	}

//	public HexGridChunk GetRelevantChunk(int x, int z){
//		int chunkX = x / HexMetrics.chunkSizex;
//		int chunkz = z / HexMetrics.chunkSizeZ;
//		HexGridChunk chunk = chunks [chunkX + chunkz * chunkCountX];
//		return chunk;
//	}


	void CreateChunks(){
		chunks = new HexGridChunk[chunkCountX * chunkCountZ];
		for(int z = 0, i = 0; z < chunkCountZ; z++){
			for (int x = 0; x < chunkCountX; x++) {
				HexGridChunk chunk = chunks [i++] = Instantiate (chunkPrefab);
				chunk.transform.SetParent (transform);
			}
		}
	}
	#region TestingGround

	public void ColorCell(HexCell cell, Color color){
		Debug.Log ("HexGrid.ColorCell called");
		cell.Color = color;

	}

	public void ReturnAllCellsToOriginalColor(){
		Debug.Log ("HexGrid.return all cells to original color called");
		foreach (HexCell cell in cells) {
			ColorCell (cell, cell.defaultColour);
		}
		
	}

	public void ReturnListOfCellsToOriginalColor(List<HexCell> theseCells){
		Debug.Log ("HexGrid.Return listo f cells to original color");
		foreach (HexCell cell in theseCells) {
			ColorCell (cell, cell.defaultColour);
		}
	}


	public void HideCellCoordinateLabels(){
		Debug.Log ("HexGrid.HideCellCoordinateLabels called");
		GameObject[] coordinateLabels = GameObject.FindGameObjectsWithTag ("coordinateLabel");

		foreach (GameObject thisGameObject in coordinateLabels) {
			
				thisGameObject.SetActive (false);

		}
	}

	public void ReturnCellToDefaultColor(HexCell cell){
		Debug.Log ("HexGrid.ReturnCellToDefaultColor called");
		cell.Color = cell.defaultColour;
	}

	public void ShowCellIndexNumbers(bool visible){
		foreach (HexGridChunk chunk in chunks) {
			chunk.ShowThisChunkCellIndexes (visible);
		}
	}



	#endregion

	#region FindCells
	public int GetCellIndexFromCoordinates(HexCoordinates coordinates){
		return coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
	}




	public HexCell FindHexCellByIntCoordinates(int x, int z){

		foreach (HexCell cell in cells) {		
			if (cell.coordinates.X == x && cell.coordinates.Z == z) {
				return cell;
			}
		}

		return null;
	}




	public HexCell FindHexCellByHexCoordinate(HexCoordinates coordinate){

		int index = GetCellIndexFromCoordinates (coordinate);
		if (index < cells.Length) {
			return cells [index];
		}
		return null;
		}


	public List<int> FindAllHexCellsWithinNSteps(int numberOfMoves, HexCell cellToStartFrom){
		
		//Loops in every dimension -N to +N (where N is the number of moves availalbe to the piece), starting from a particular cell, and returns the cells that can be moved to.
		List<int> possibleMovementCells = new List <int>();

		for (int x = cellToStartFrom.coordinates.X -numberOfMoves; x <= cellToStartFrom.coordinates.X + numberOfMoves; x++) {
			for (int y = cellToStartFrom.coordinates.Y -numberOfMoves; y <= cellToStartFrom.coordinates.Y + numberOfMoves; y++) {
				for (int z = cellToStartFrom.coordinates.Z -numberOfMoves; z <= cellToStartFrom.coordinates.Z  + numberOfMoves; z++) {
					if (x + y + z == 0) { //all coordinates should add up to 0 to be a valid cell
						if (!(x == cellToStartFrom.coordinates.X && y == cellToStartFrom.coordinates.Y && z == cellToStartFrom.coordinates.Z)) {
							//If the cell is not the starting cell
							HexCell cell = FindHexCellByIntCoordinates (x, z);
							if (cell != null) {
								possibleMovementCells.Add (cell.id);
							}
						}
					}
				}
			}
		}

		return possibleMovementCells;

	}
	public HexCell GetCellByPosition(Vector3 cellPosition){
		cellPosition = transform.InverseTransformPoint (cellPosition);
		HexCoordinates coordinates = HexCoordinates.FromPosition (cellPosition);
		int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
		return cells [index];
	}


	public void RefreshAllChunkGrids(){
		Debug.Log ("HexGrid.RefreshAllChunkGrids");
		foreach (HexGridChunk hexChunk in chunks) {
			hexChunk.RefreshGrid ();
		}
	}



	#endregion

	#region SaveAndLoad

	public void Save(BinaryWriter writer){
		writer.Write (cellCountX); //ThisIsTheOne
		writer.Write (cellCountZ); //ThisIsTheOne
		foreach (HexCell cell in cells) {
			cell.Save (writer);
		}
	}

	public void Load(BinaryReader reader, int header){
		
		int x = 20, z = 15;
		if (header >= 1) {
			x = reader.ReadInt32 ();
			z = reader.ReadInt32 ();

			Debug.Log ("x is " + x + " and z is " + z);
		}

		CreateMap (x, z);

//		if(x != cellCountX || z != cellCountZ){
//			if(!CreateMap (x, z)){
//				return;	
//			} //ThisIsTheOne
//
//		}

		foreach (HexCell cell in cells) {
			cell.Load (reader);
		}

		RefreshAllChunkGrids ();
	}
	#endregion

	}
