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
		PASS,
		SHOOT,
		JINK,
		HEAD_TO_GOAL,
		HEAD_TO_GOAL_NO_DISK,
		SEARCH_DISK,
		GRAB_DISK,
		GOTO_DISK,
		GOTO_TEAM_PLAYER,
	}

	public enum Intention
	{
		SCORE,
		TACKLE
	}

	public class BDIAgent : Agent
	{
		public struct Beliefs
		{
			public Dictionary<Agent, Vector3> sameTeamPositions;
			public Dictionary<Agent, Vector3> otherTeamPositions;
			public bool hasFoundDisk;
			public Vector3 diskPosition;
			public Vector2 playerGoalPosition;
		}

		AgentsManager agentsManager;

		Beliefs agentBeliefs;
		Desire desire;
		Intention intension;

		public BDIAgent(GameManager gameManager, Game game, Camera camera, int team)
			: base(gameManager, game, camera, team)
		{
			agentBeliefs.hasFoundDisk = false;
			if (team == 1)
				agentBeliefs.playerGoalPosition = _court.getTeam1GoalPosition();
			else if (team == 2)
				agentBeliefs.playerGoalPosition = _court.getTeam2GoalPosition();

			agentsManager = (AgentsManager)gameManager.getGameEntity("agentsManager");

			agentBeliefs.sameTeamPositions = new Dictionary<Agent, Vector3>();
			agentBeliefs.otherTeamPositions = new Dictionary<Agent, Vector3>();

			intension = Intention.SCORE;
		}

		protected override void generateKeys()
		{
			updateBeliefs();
			options();
		}

		private void options()
		{
			if (intension == Intention.SCORE && !_hasDisk)
				gotoDiskPosition();
			else if (intension == Intention.SCORE && _hasDisk)
				gotoGoal();
		}

		private void gotoGoal()
		{
			Agent agentPlayer;
			if (isPlayerSameTeamNearGoal(out agentPlayer) && canSeePlayer(agentPlayer))
				desire = Desire.PASS;
			else if (agentPlayer != null)
				desire = Desire.GOTO_TEAM_PLAYER;
			else
				desire = Desire.HEAD_TO_GOAL;
		}

		private bool isPlayerSameTeamNearGoal(out Agent agentPlayer)
		{
			throw new NotImplementedException();
		}

		private void gotoDiskPosition()
		{
			if (isDiskAhead() && _disk.getPlayerWithDisk() == null)
				desire = Desire.GRAB_DISK;
			else if (isDiskAhead() && !sameTeam(_disk.getPlayerWithDisk()))
				desire = Desire.JINK;
			else if (!agentBeliefs.hasFoundDisk)
				desire = Desire.SEARCH_DISK;
			else if (!agentBeliefs.hasFoundDisk)
				desire = Desire.GOTO_DISK;
		}

		private void updateBeliefs()
		{
			if (isDiskAhead()) {
				agentBeliefs.hasFoundDisk = true;
				agentBeliefs.diskPosition = _disk.getPosition();
			}

			List<Agent> agents = agentsManager.getAgents();
			foreach (Agent agent in agents) {
				if (canSeePlayer(agent)) {
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
		}
	}
}
