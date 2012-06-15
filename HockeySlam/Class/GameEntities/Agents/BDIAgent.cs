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

	public enum TypeSearch
	{
		DISK,
		PLAYER
	}

	public enum Intention
	{
		PASS,
		SHOOT_TO_GOAL,
		SHOOT_TO_PLAYER,
		JINK,
		HEAD_TO_GOAL,
		HEAD_TO_GOAL_NO_DISK,
		SEARCH_DISK,
		MOVE_TO_DISK,
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

			agentBeliefs.positionToPass = new Vector3(1000, 1000, 1000);

			intention = Intention.SEARCH_DISK;
		}

		protected override void generateKeys()
		{
			updateBeliefs();
			updateDesires();
			updateIntentions();

			Console.WriteLine(intention);

			if (intention == Intention.SEARCH_DISK)
				moveRandomly();
			else if(intention == Intention.MOVE_TO_DISK){
				Vector2 diskPos = new Vector2(agentBeliefs.diskPosition.X, agentBeliefs.diskPosition.Z);
				Vector2 newDirection = calculateDirection(ref diskPos);
				if (!rotateTowardsDirection(newDirection)) {
					Vector2 nd = new Vector2(newDirection.Y, newDirection.X);
					moveTowardsDirection(nd);
				}
			}
			else if (intention == Intention.GRAB_DISK || intention == Intention.JINK) {
				bool isDiskInRange;
				isDiskInRange = moveTowardsDisk();
				if (isDiskInRange && !_hasShoot && _player.getPositionVector() != _lastPositionWithDisk) {
					grabDisk();
				}
			} else if (intention == Intention.HEAD_TO_GOAL || intention == Intention.HEAD_TO_GOAL_NO_DISK) {
				Vector2 newDirection = calculateDirection(ref agentBeliefs.teamGoalPosition);
				if (!rotateTowardsDirection(newDirection)) {
					Vector2 tgp = new Vector2(agentBeliefs.teamGoalPosition.Y, agentBeliefs.teamGoalPosition.X);
					moveTowardsDirection(tgp);
				}
			} else if (intention == Intention.SHOOT_TO_GOAL) {
				shootToPosition(agentBeliefs.teamGoalPosition);
			} else if (intention == Intention.PASS) {
				if (isPossibleMovingInPosition(agentBeliefs.positionToPass, TypeSearch.PLAYER)) {
					Vector2 ptp = new Vector2(agentBeliefs.positionToPass.X, agentBeliefs.positionToPass.Z);
					Vector2 newDirection = calculateDirection(ref ptp);
					if (!rotateTowardsDirection(newDirection)) {
						Vector2 tgp = new Vector2(agentBeliefs.teamGoalPosition.Y, agentBeliefs.teamGoalPosition.X);
						moveTowardsDirection(tgp);
					}
				}
			} else if (intention == Intention.SHOOT_TO_PLAYER) {
				Vector3 pos = agentBeliefs.agentNearGoal.getPlayer().getPositionVector();
				Vector2 agentNearGoalPos = new Vector2(pos.X, pos.Y);
				shootToPosition(agentNearGoalPos);
			}
		}

		private bool isPossibleMovingInPosition(Vector3 pos, TypeSearch ts)
		{
			BoundingSphere bs = new BoundingSphere(pos, 1);
			if (_fov.Intersects(bs)) {
				if (ts == TypeSearch.DISK)
					agentBeliefs.hasFoundDisk = false;
				else {
					agentBeliefs.agentNearGoal = null;
				}
				return false;
			}
			return true;
		}

		private bool rotateTowardsDirection(Vector2 newDirection)
		{
			double newDirectionAngle = Math.Acos(newDirection.X);
			if (newDirection.Y < 0)
				newDirectionAngle = MathHelper.TwoPi - newDirectionAngle;

			if (_fovRotation < newDirectionAngle + 0.3 && _fovRotation > newDirectionAngle - 0.3) {
					return false;
			}

			double plusPI = (newDirectionAngle + MathHelper.Pi) % MathHelper.TwoPi;

			if (_team == 1) {
				rotateFovForTeam1(newDirectionAngle, plusPI);
			} else {
				rotateFovForTeam2(newDirectionAngle, plusPI);
			}

			return true;
		}

		private void rotateFovForTeam2(double newDirectionAngle, double plusPI)
		{
			if (plusPI > newDirectionAngle) {
				if ((_fovRotation >= 0 && _fovRotation <= newDirectionAngle) || (_fovRotation >= plusPI && _fovRotation <= MathHelper.TwoPi))
					rotateCounterclockwise();
				else rotateClockwise();
			} else {
				if ((_fovRotation >= 0 && _fovRotation <= plusPI) || (_fovRotation >= newDirectionAngle && _fovRotation <= MathHelper.TwoPi))
					rotateCounterclockwise();
				else rotateClockwise();
			}
		}

		private void rotateFovForTeam1(double newDirectionAngle, double plusPI)
		{
			if (plusPI > newDirectionAngle) {
				if ((_fovRotation >= 0 && _fovRotation <= newDirectionAngle) || (_fovRotation >= plusPI && _fovRotation <= MathHelper.TwoPi))
					rotateClockwise();
				else rotateCounterclockwise();
			} else {
				if ((_fovRotation >= 0 && _fovRotation <= plusPI) || (_fovRotation >= newDirectionAngle && _fovRotation <= MathHelper.TwoPi))
					rotateClockwise();
				else rotateCounterclockwise();
			}
		}

		private Vector2 calculateDirection(ref Vector2 destinatePosition)
		{
			Vector3 thisPlayerPosition = _player.getPositionVector();
			Vector2 newDirection = new Vector2(destinatePosition.X - thisPlayerPosition.X,
											   destinatePosition.Y - thisPlayerPosition.Z);
			newDirection = Vector2.Normalize(newDirection);
			return newDirection;
		}

		private bool isPossibleGetToPlayer(Vector3 pos)
		{
			BoundingSphere bs = new BoundingSphere(pos, 1);

			return !bs.Intersects(_fov);
		}

		private void updateIntentions()
		{
			if (desire == Desire.SCORE || _hasDisk) {
				if ((intention == Intention.SEARCH_DISK || intention == Intention.MOVE_TO_DISK) && agentBeliefs.canSeeDisk && agentBeliefs.playerWithDisk == null)
					intention = Intention.GRAB_DISK;
				else if (intention == Intention.SEARCH_DISK && agentBeliefs.canSeeDisk && sameTeam(agentBeliefs.playerWithDisk))
					intention = Intention.HEAD_TO_GOAL_NO_DISK;
				else if (intention == Intention.SEARCH_DISK && agentBeliefs.canSeeDisk && !sameTeam(agentBeliefs.playerWithDisk))
					intention = Intention.JINK;
				else if (intention == Intention.HEAD_TO_GOAL_NO_DISK && agentBeliefs.canSeeDisk && !sameTeam(agentBeliefs.playerWithDisk))
					intention = Intention.JINK;
				else if (intention == Intention.HEAD_TO_GOAL_NO_DISK && agentBeliefs.canSeeDisk && agentBeliefs.playerWithDisk == null)
					intention = Intention.GRAB_DISK;
				else if ((intention == Intention.GRAB_DISK || intention == Intention.HEAD_TO_GOAL) &&
						 _hasDisk && agentBeliefs.agentNearGoal != null)
					intention = Intention.PASS;
				else if (intention == Intention.GRAB_DISK && _hasDisk && !agentBeliefs.canSeeTeamGoal)
					intention = Intention.HEAD_TO_GOAL;
				else if (intention == Intention.GRAB_DISK && _hasDisk && agentBeliefs.canSeeTeamGoal)
					intention = Intention.SHOOT_TO_GOAL;
				else if (intention == Intention.PASS && _hasDisk && canSeePlayer(agentBeliefs.agentNearGoal))
					intention = Intention.SHOOT_TO_PLAYER;
				else if (intention == Intention.HEAD_TO_GOAL && _hasDisk && agentBeliefs.canSeeTeamGoal)
					intention = Intention.SHOOT_TO_GOAL;
				else if (!_hasDisk && intention != Intention.GRAB_DISK && agentBeliefs.hasFoundDisk)
					intention = Intention.MOVE_TO_DISK;
				else if(intention == Intention.SEARCH_PLAYER_WITH_DISK && agentBeliefs.agentNearGoal == null)
					intention = Intention.SEARCH_DISK;
				else if (!_hasDisk && intention != Intention.GRAB_DISK)
					intention = Intention.SEARCH_DISK;
			} else {
				if ((intention == Intention.SEARCH_PLAYER_WITH_DISK || intention == Intention.JINK) && sameTeam(agentBeliefs.playerWithDisk))
					intention = Intention.HEAD_TO_GOAL_NO_DISK;
				else if ((intention == Intention.SEARCH_PLAYER_WITH_DISK || intention == Intention.JINK) && agentBeliefs.canSeeDisk && agentBeliefs.playerWithDisk == null)
					intention = Intention.GRAB_DISK;
				else if (intention == Intention.JINK && agentBeliefs.sawAgents.Contains(agentBeliefs.playerWithDisk))
					intention = Intention.JINK;
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

			Vector3 pos = _player.getPositionVector();
			Vector2 thisPlayerPos = new Vector2(pos.X, pos.Z);
			Vector2 thisPlayerVector = agentBeliefs.teamGoalPosition - thisPlayerPos;
			double thisPlayerDistance = Math.Sqrt(Math.Pow(thisPlayerVector.X, 2) + Math.Pow(thisPlayerVector.Y, 2));

			foreach (KeyValuePair<Agent, Vector3> pair in agentBeliefs.sameTeamPositions) {
				Vector2 playerPos = new Vector2(pair.Value.X, pair.Value.Z);
				Vector2 playerVector = agentBeliefs.teamGoalPosition - playerPos;
				double playerDistance = Math.Sqrt(Math.Pow(playerVector.X, 2) + Math.Pow(playerVector.Y, 2));
				if (playerDistance > distance && playerDistance < thisPlayerDistance) {
					distance = playerDistance;
					agentPlayer = pair.Key;
				}
			}

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

			if (!agentBeliefs.canSeeDisk)
				isPossibleMovingInPosition(agentBeliefs.diskPosition, TypeSearch.DISK);

			List<Agent> agents = agentsManager.getAgents();
			agentBeliefs.sawAgents.Clear();
			foreach (Agent agent in agents) {
				if (agent != this && canSeePlayer(agent)) {
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
