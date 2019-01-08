using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lookout;

public class BattleEvent:LookoutEvent {

	public enum VictoryType{
		Outstanding, //<0-10%
		Effective, //<11-30%
		Slight, //<31-50%  
		Difficult, //<51-70%
		Pyrrhic//71-100
	}

	public enum LossType{
		Catastrophic, //<0-10%
		Punishing, //<11-30%
		Slight, //<31-50%  
		Minor, //<51-70%
		Scratch//71-100
	}

	private Allegiance winner;
	private float turn;
	private int battleLocationCellID;
	private int winnerLosses;
	private int loserLosses;
	private VictoryType winnerVictoryType;
	private LossType loserLossType;
	private int defenderID; //attacker ID is the actionUnitID



	public void SetWinner(Allegiance battleWinner){
		winner = battleWinner;
	}

	public Allegiance GetWinner(){
		return winner;
	}

	public void SetTurn(float turnNumber){
		turn = turnNumber;
	}

	public float GetTurn(){
		return turn;
	}

	public void SetBattleLocation(HexCell cell){
		battleLocationCellID = cell.id;
	}

	public int GetBattleLocationID(){
		return battleLocationCellID;
	}

	public void SetLosses(int winnerLoss, int loserLoss){
		winnerLosses = winnerLoss;
		loserLosses = loserLoss;
		SetTypeOfVictory ();
	}

	public void SetDefenderID(int defenderIDNum){
		defenderID = defenderIDNum;
	}

	public int GetDefenderID(){
		return defenderID;
	}

	public void SetAttackerID(int attackerID){
		SetActionUnitID (attackerID);

	}

	public int GetAttackerID(){
		return GetActionUnitID ();
	}

	public void SetWinnerLosses(int lossamount){
		winnerLosses = lossamount;
	}

	public int GetWinnerLosses(){
		return winnerLosses;
	}

	public void SetLoserLosses(int losses){
		loserLosses = losses;
	}

	public int GetLoserLosses(){
		return loserLosses;
	}


	public void SetVictoryType(VictoryType victoryType){
		winnerVictoryType = victoryType;
	}

	public VictoryType GetVictoryType(){
		return winnerVictoryType;
	}

	public void SetLossType(LossType lossType){
		loserLossType = lossType;
	}

	public LossType GetLossType(){
		return loserLossType;
	}



	private void SetTypeOfVictory(){
		if (winnerLosses / loserLosses <= 1 / 10) {
			winnerVictoryType = VictoryType.Outstanding;
			loserLossType = LossType.Catastrophic;
		} else if (winnerLosses / loserLosses <= 3 / 10) {
			winnerVictoryType = VictoryType.Effective;
			loserLossType = LossType.Punishing;
		} else if (winnerLosses / loserLosses <= 5 / 10) {
			winnerVictoryType = VictoryType.Slight;
			loserLossType = LossType.Slight;
		} else if (winnerLosses / loserLosses <= 7 / 10) {
			winnerVictoryType = VictoryType.Difficult;
			loserLossType = LossType.Minor;
		} else {
			winnerVictoryType = VictoryType.Pyrrhic;
			loserLossType = LossType.Scratch;
		}
	}


	#region code for testing
//	public float attackerStrength;
//	public float defenderStrength;
//
//	public float attackerRandomizer;
//	public float defenderRandomizer;
//
//	public float attackAdvantageMultiplier = 1f;
//	public float defenderAdvantageMultiplier = 4f / 3f ;
//
//	public float defenderOdds;
//	public float attackerOdds;
//
//
//	public void Battle(){
//
//
//		//current algorythm strongly favours defender when attacker is weaker, but doesn't sufficiently create an even battle when the defnder is outnumbered. appply different attack/defend logic based on which is stronger?
//
//		// e.g. an attacker of strength just 500 less than the defender will be obliterated. but a defender of the 1000 weaker than the attacker will face certain defeat. the randomizers are therefore having both a too great and too little effect. 
//		// at equal strength the defender always wins. 
//
//
//		int attackerWins = 0;
//		int defenderWins = 0;
//
//		for (int i = 0; i < 100; i++) {
//
//			attackerRandomizer = Random.Range (1f, 10f);
//			defenderRandomizer = Random.Range (1f, 10f);
//
//			attackerOdds = ((0.5f * (attackerStrength / defenderStrength)) * attackAdvantageMultiplier) * attackerRandomizer;
//			defenderOdds = ((0.5f * (defenderStrength / attackerStrength)) * defenderAdvantageMultiplier) * defenderRandomizer;
//
//			print ("Attacker odds are: " + attackerOdds + " but defender odds are " + defenderOdds);
//			if (attackerOdds > defenderOdds) {
//				attackerWins++;
//				print ("Attacker wins!");
//			} else {
//				defenderWins++;
//				print ("Defender wins!");
//			}
//
//			float totalOddsValue = attackerOdds + defenderOdds;
//			float attackerPercent = (attackerOdds / totalOddsValue);
//			float defenderPercent = (defenderOdds / totalOddsValue);
//
////			if (attackerPercent < 0.1) {
////				attackerStrength = 0f;
////			}
////
////			if (defenderPercent < 0.1) {
////				defenderStrength = 0f;
////			}
//
//			print ("Attacker Percentage: " + (attackerPercent * 100f) + "%; Defender Percentage: " + (defenderPercent * 100f) + "%");
//
//			float attackerNewStrength = attackerStrength * attackerPercent;
//			float defenderNewStrength = defenderStrength * defenderPercent;
//			print ("Attacker new strength: " + attackerNewStrength + "; Defender new strength: " + defenderNewStrength);
//
//
//
//
//		}
//
//		float totalWins = attackerWins + defenderWins;
//		print ("Attacker win percentage was: " + ((attackerWins / totalWins) * 100f) + "% (" + attackerWins + ")");
//		print ("defender win percentage was: " + (defenderWins / totalWins * 100f) + "% (" + defenderWins + ")");
//	}

	#endregion
}

