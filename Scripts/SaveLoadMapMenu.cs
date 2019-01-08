using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using Prototype.NetworkLobby;

public class SaveLoadMapMenu : MonoBehaviour {

	public RectTransform listContent;
	public SaveLoadItem itemPrefab;

	public Text menuLabel, actionButtonLabel;
	public HexGrid hexGrid;
	public InputField nameInput;
	public Button deleteButton;
	public string mapNamePath;
	bool saveMode;



	public void Open(bool saveModeBool){
		saveMode = saveModeBool;

		if (saveMode) {
			menuLabel.text = "Save Map";
			actionButtonLabel.text = "Save";
		} else {
			menuLabel.text = "Load Map";
			actionButtonLabel.text = "Load";

			if (SceneManager.GetActiveScene ().name == "LobbyScene") {
				deleteButton.gameObject.SetActive (false);
			}
		}

		FillList ();
		gameObject.SetActive (true);
		if (FindObjectOfType<HexMapCamera>()) {
			HexMapCamera.Locked = true;
		}
	}

	public void Close(){
		gameObject.SetActive (false);
		if (FindObjectOfType<HexMapCamera>()) {
			HexMapCamera.Locked = false;
		}
	}

	string GetSelectedPath(){
	
		string mapName = nameInput.text;
		if (mapName.Length == 0) {
			return null;
		}
		return Path.Combine (Application.persistentDataPath, mapName + ".map");

	}


	#region SaveAndLoadMap

	public void SaveMap(string path){
		UIManager uiManager = FindObjectOfType<UIManager> ();
		if (hexGrid.usBaseLocationID < 0 || hexGrid.conBaseLocationID < 0) {

			uiManager.SetBasicInfoText ("You have to have at least one base per side on the map to save it.", "Sorry!");
			uiManager.ToggleHUDActive (uiManager.basicInfoPopup);

		} else if(hexGrid.conArmiesStartLocationList.Count < 5) {
			uiManager.SetBasicInfoText("You have to have at least 5 Confederate army spawn points on the map to save it.", "I'm on it");
			uiManager.ToggleHUDActive(uiManager.basicInfoPopup);

		}else if(hexGrid.usArmiesStartLocationList.Count < 5){
			uiManager.SetBasicInfoText("You have to have at least 5 USA army spawn points on the map to save it.", "Okie-dokie");
			uiManager.ToggleHUDActive(uiManager.basicInfoPopup);

		} else{
			Debug.Log ("Con armies count is " + hexGrid.conArmiesStartLocationList.Count + " us armies count is " + hexGrid.usArmiesStartLocationList.Count);

			//string path = Path.Combine (Application.persistentDataPath, "test.map");

			using (BinaryWriter writer = new BinaryWriter (File.Open (path, FileMode.Create))) {
				writer.Write (1);
				hexGrid.Save (writer);
			}

		}

		Close ();

	}

	public void LoadMap(string path){
		if (!File.Exists (path)) {
			Debug.LogError ("File does not exist " + path);
			return;
		}

 		hexGrid.conArmiesStartLocationList.Clear ();
		hexGrid.conBaseLocationID = -1;
		hexGrid.usArmiesStartLocationList.Clear ();
		hexGrid.usBaseLocationID = -1;
		//string path = Path.Combine (Application.persistentDataPath, "test.map");
		using (BinaryReader reader = new BinaryReader (File.OpenRead(path))) {
			int header = reader.ReadInt32();
			if (header <= 1) {
				hexGrid.Load(reader, header);
				HexMapCamera.ValidatePosition ();
			}
			else {
				Debug.LogWarning("Unknown map format " + header);
			}

		}

		Close ();

	}

	public void Action(){
		string path = GetSelectedPath ();
		if (path == null) {
			return;
		}

		if (saveMode) {
			SaveMap (path);
		} else {
			if (SceneManager.GetActiveScene ().name == "LobbyScene") {
					LobbyPlayer[] lobbyPlayers = FindObjectsOfType<LobbyPlayer> ();

				foreach (LobbyPlayer lobbyPlayer in lobbyPlayers) {
					if (lobbyPlayer.isServer) {
						lobbyPlayer.mapNameInput.text = Path.GetFileNameWithoutExtension(path);
					}
				}

				Close ();

			} else {
				LoadMap (path);
			}
		}
	}


	public void SelectItem(string name){
		nameInput.text = name;
	}

	void FillList(){
		for (int i = 0; i < listContent.childCount; i++) {
			Destroy (listContent.GetChild (i).gameObject);
		}

		string[] paths = Directory.GetFiles (Application.persistentDataPath, "*.map");
		Array.Sort (paths);

		foreach (string path in paths) {
			SaveLoadItem item = Instantiate (itemPrefab);
			item.menu = this;
			item.MapName = Path.GetFileNameWithoutExtension(path);
			item.transform.SetParent (listContent, false);
		}
	}


	public void Delete(){
		string path = GetSelectedPath ();
		if (path == null) {
			return;
		}

		if (File.Exists (path)) {
			File.Delete (path);
		}
		nameInput.text = "";
		FillList ();
	}
	#endregion

}
