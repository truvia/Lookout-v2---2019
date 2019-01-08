using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Lookout{

	public enum Allegiance{
		None,
		CON,
		USA
	}


	public class Game {
		public Dictionary<int, Allegiance> victoryPoints = new Dictionary<int, Allegiance> (); //[HexID], [Who owns VP] -  set by board manager when creating cities
		public int conArmiesLeft; //set by boardmanager when creating units
		public int usArmiesLeft; //set by boardmanager when creating units
		public Allegiance winner;
		public Allegiance control; 
		public float turnNumber = 1f;
		private Allegiance startPlayer = Allegiance.CON;

		public int boardWidth; //boardwidth is set in the inspector by the meshgrid
		public int boardHeight; //boardheight is set in the inspector by the meshgrid

//		public int[][] CONPossibleStartTiles = {
//			new int[] { 0, 5 }, //40
//			new int[] { 0, 6 }, //48
//			new int[] { 0, 7 }, //56
//			new int[] { 1, 5 }, //41
//			new int[] { 1, 7 }, //57
//			new int[] { 2, 5 }, //42 
//			new int[] { 2, 6 }, //50
//			new int[] { 2, 7 } //58
//
//		}; 
//
//		public int[][] USPossibleStartTiles = new int[][] { //N.B. this is defined by the ID of the tileCoordinate, not by the coordinate itself
//			new int[] { 5, 0 }, //5
//			new int[] { 6, 0 }, //6
//			new int[] { 7, 0 }, //7
//			new int[] { 5, 1 }, //13
//			new int[] { 7, 1 }, //15
//			new int[] { 5, 2 }, //21 
//			new int[] { 6, 2 }, //22
//			new int[] { 7, 2 } //23
//		};
//
//		public int[] CONFortressStartTile = new int[] {-2, 6};
//		public int[] USAFortressStartTile = new int[] {6, 1};


		public int CONFortressStartTile = 81;
		public int USAFOrtressStartTIle = 18;

		
		public int[] USPossibleStartTiles = new int[] {
			8, 9, 17, 19, 28, 29
		};

		public int[] CONPossibleStartTiles = new int[]{
			90, 91, 80, 82, 70, 71	
		};



		public int[] cities = {
			12, 87
		};
			

		#region EventNotifications
		public const string DidBeginGameNotification = "GameTest.DidBeginGameNotification";
		public const string DidChangeTurnNotification = "GameTest.DidChangeTurnNotification";
		public const string DidEndGameNotification = "GameTest.DidEndGameNotification";

		#endregion


		void Start(){
			Debug.Log ("start called");
			ResetGame ();
		}



		#region GameSetup


//		public List<int> DefineStartLocations(){ //Called by PlayerController
//			List<int> randomNumbers = new List<int>();
//
//			for(int i = 0; i<2; i++){
//				int randNum = Mathf.RoundToInt(Random.Range(0f, 7f));
//					if(randomNumbers.Contains(randNum)){
//						i--;
//					}	
//			}
//			return randomNumbers;
//		}

		#endregion

		#region BasicGameFunctions
		/*Basic Game functions - */
		//e.g. resetting the game, or checking for win 

		public void ResetGame(){
			Debug.Log ("Reset Game is called");

			victoryPoints.Clear ();
			winner = Allegiance.None; //when the winner is not none, the game should end;
			control = startPlayer; //control sets who is taking their turn.
			EventManager.TriggerEvent(DidBeginGameNotification);
		}

		public void ChangeTurn(){
			Debug.Log("ChangeTurn called by " + control);
			turnNumber = turnNumber + 0.5f;
			control = (control == Allegiance.CON) ? Allegiance.USA : Allegiance.CON;

			EventManager.TriggerEvent (DidChangeTurnNotification);
		
		}


		public void CheckForGameOver(){
			if (CheckForWin ()) {
				Debug.Log ("Game Over.");
				EventManager.TriggerEvent (DidEndGameNotification, control); //second parameter is the winner.

			} else if (CheckForStaleMate ()) {	
				Debug.Log ("Game Over.");
				EventManager.TriggerEvent (DidEndGameNotification, Allegiance.None); //second parameter shows that nobody won.
			} else{
				Debug.Log ("Game is not over yet.");
			}
		}

		private bool CheckForWin(){
			Debug.Log ("Checking for win...");
			bool winStatus = false;

			int CONVictoryPointssHeld = 0;
			int USVictoryPointssHeld = 0;
			int totalVictoryPoints = victoryPoints.Count;

			foreach (KeyValuePair<int, Allegiance> keyValue in victoryPoints) {
				int tileID = keyValue.Key;
				Allegiance victoryPointHolder = keyValue.Value;

				if (victoryPointHolder == Allegiance.CON) {
					CONVictoryPointssHeld++;
				} else if (victoryPointHolder == Allegiance.USA) {
					USVictoryPointssHeld++;
				}

				if (CONVictoryPointssHeld == totalVictoryPoints || USVictoryPointssHeld == totalVictoryPoints) {
					winStatus = true;
				}
			}
				
			Debug.Log ("Check for win is " + winStatus + ".");
			return winStatus;
		}

		private bool CheckForStaleMate(){
			Debug.Log ("Checking for Stalemate...");
			bool stalemateStatus = false;

			if (conArmiesLeft == 0 && usArmiesLeft == 0) {
				stalemateStatus = true;
			}
				
			Debug.Log ("Check for Stalemate is " + stalemateStatus + ".");
			return stalemateStatus;
		}
			



		#endregion




		#region DebugArea
		//DEBUG AREA


		#endregion




	}
		
}