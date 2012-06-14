using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using HockeySlam.Class.GameState;
using HockeySlam.Class.GameEntities.Models;
using HockeySlam.Interface;

namespace HockeySlam.Class.GameEntities.Agents
{
	public enum Desire
	{
		SCORE,
		TACKLE
	}

	public enum Intention
	{
		PASS,
		SHOOT,
		JINK,
		HEAD_TO_GOAL,
		HEAD_TO_GOAL_NO_DISK,
		SEARCH_DISK,
		SEARCH_PLAYER_WITH_DISK,
		GRAB_DISK,
	}

	public class BDIAgent : Agent
	{
		public struct Beliefs
		{
			public Dictionary<Agent, Vector3> sameTeamPositions;
			public Dictionary<Agent, Vector3> otherTeamPositions;
			public List<Agent> sawAgents;
			public bool hasFoundDisk;
			public bool canSeeDisk;
			public bool canSeeTeamGoal;
			public Agent agentNearGoal;
			public Vector3 diskPosition;
			public Vector2 teamGoalPosition;
			public Agent playerWithDisk;
			public Vector3 positionToPass;
		}

		AgentsManager agentsManager;

		Beliefs agentBeliefs;
		Desire desire;
		Intention intention;

		public BDIAgent(GameManager gameManager, Game game, Camera camera, int team)
			: base(gameManager, game, camera, team)
		{
			agentBeliefs.hasFoundDisk = false;
			if (team == 1)
				agentBeliefs.teamGoalPosition = _court.getTeam1GoalPosition();
			else if (team == 2)
				agentBeliefs.teamGoalPosition = _court.getTeam2GoalPosition();

			agentsManager = (AgentsManager)gameManager.getGameEntity("agentsManager");

			agentBeliefs.sameTeamPositions = new Dictionary<Agent, Vector3>();
			agentBeliefs.otherTeamPositions = new Dictionary<Agent, Vector3>();
			agentBeliefs.sawAgents = new List<Agent>();

			intention = Intention.SEARCH_DISK;
		}

		protected override void generateKeys()
		{
			updateBeliefs();
			updateDesires();
			updateIntentions();

			//if (intention == Intention.SCORE)
		}

		private void updateIntentions()
		{
			if (desire == Desire.SCORE) {
				if (intention == Intention.SEARCH_DISK && agentBeliefs.canSeeDisk && agentBeliefs.playerWithDisk == null)
					intention = Intention.GRAB_DISK;
				else if (intention == Intention.SEARCH_DISK && agentBeliefs.canSeeDisk && sameTeam(agentBeliefs.playerWithDisk))
					intention = Intention.HEAD_TO_GOAL_NO_DISK;
				else if (intention == Intention.SEARCH_DISK && agentBeliefs.canSeeDisk && !sameTeam(agentBeliefs.playerWithDisk))
					intention = Intention.JINK;
				else if (intention == Intention.HEAD_TO_GOAL_NO_DISK && agentBeliefs.canSeeDisk && !sameTeam(agentBeliefs.playerWithDisk))
					intention = Intention.JINK;
				else if (intention == Intention.HEAD_TO_GOAL_NO_DISK && agentBeliefs.canSeeDisk && agentBeliefs.playerWithDisk == null)
					intention = Intention.GRAB_DISK;
				else if (intention == Intention.GRAB_DISK && _hasDisk && agentBeliefs.agentNearGoal != null)
					intention = Intention.PASS;
				else if (intention == Intention.GRAB_DISK && _hasDisk && !agentBeliefs.canSeeTeamGoal)
					intention = Intention.HEAD_TO_GOAL;
				else if (intention == Intention.GRAB_DISK && _hasDisk && agentBeliefs.canSeeTeamGoal)
					intention = Intention.SHOOT;
				else if (!_hasDisk)
					intention = Intention.SEARCH_DISK;
			} else {
				if ((intention == Intention.SEARCH_PLAYER_WITH_DISK || intention == Intention.JINK) && sameTeam(agentBeliefs.playerWithDisk))
					intention = Intention.HEAD_TO_GOAL_NO_DISK;
				else if ((intention == Intention.SEARCH_PLAYER_WITH_DISK || intention == Intention.JINK) && agentBeliefs.canSeeDisk && agentBeliefs.playerWithDisk == null)
					intention = Intention.GRAB_DISK;
				else if (intention == Intention.JINK && agentBeliefs.sawAgents.Contains(agentBeliefs.playerWithDisk))
					intention = Intention.SEARCH_PLAYER_WITH_DISK;
				else if (!_hasDisk)
					intention = Intention.SEARCH_DISK;
			}
		}

		private void updateDesires()
		{
			if (!sameTeam(agentBeliefs.playerWithDisk) && agentBeliefs.playerWithDisk != null)
				desire = Desire.TACKLE;
			else desire = Desire.SCORE;

		}

		private bool isPlayerSameTeamNearGoal(out Agent agentPlayer)
		{
			double distance = 100000;
			agentPlayer = null;

			foreach (KeyValuePair<Agent, Vector3> pair in agentBeliefs.sameTeamPositions) {
				Vector2 playerPos = new Vector2(pair.Value.X, pair.Value.Z);
				Vector2 playerVector = agentBeliefs.teamGoalPosition - playerPos;
				double playerDistance = Math.Sqrt(Math.Pow(playerVector.X, 2) + Math.Pow(playerVector.Y, 2));
				if (playerDistance < distance) {
					distance = playerDistance;
					agentPlayer = pair.Key;
				}
			}

			Vector3 pos = _player.getPositionVector();
			Vector2 thisPlayerPos = new Vector2(pos.X, pos.Z);
			Vector2 thisPlayerVector = agentBeliefs.teamGoalPosition - thisPlayerPos;
			double thisPlayerDistance = Math.Sqrt(Math.Pow(thisPlayerVector.X, 2) + Math.Pow(thisPlayerVector.Y, 2));

			if (distance < thisPlayerDistance)
				return true;
			else {
				agentPlayer = null;
				return false;
			}
		}

		private void updateBeliefs()
		{
			if (isDiskAhead()) {
				agentBeliefs.hasFoundDisk = true;
				agentBeliefs.diskPosition = _disk.getPosition();
				agentBeliefs.canSeeDisk = true;
				agentBeliefs.playerWithDisk = _disk.getPlayerWithDisk();
			} else agentBeliefs.canSeeDisk = false;

			List<Agent> agents = agentsManager.getAgents();
			agentBeliefs.sawAgents.Clear();
			foreach (Agent agent in agents) {
				if (canSeePlayer(agent)) {
					agentBeliefs.sawAgents.Add(agent);
					Player agentPlayer = agent.getPlayer();
					if (sameTeam(agent)) {
						if (agentBeliefs.sameTeamPositions.ContainsKey(agent))
							agentBeliefs.sameTeamPositions[agent] = agentPlayer.getPositionVector();
						else
							agentBeliefs.sameTeamPositions.Add(agent, agentPlayer.getPositionVector());
					} else {
						if(agentBeliefs.otherTeamPositions.ContainsKey(agent))
							agentBeliefs.otherTeamPositions[agent] = agentPlayer.getPositionVector();
						else agentBeliefs.otherTeamPositions.Add(agent, agentPlayer.getPositionVector());
					}
				}
			}

			isPlayerSameTeamNearGoal(out agentBeliefs.agentNearGoal);

			agentBeliefs.canSeeTeamGoal = canSeeGoal();
		}
	}
}
