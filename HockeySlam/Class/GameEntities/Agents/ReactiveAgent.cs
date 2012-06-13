using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using HockeySlam.Class.GameState;
using HockeySlam.Class.GameEntities.Models;
using HockeySlam.Interface;

namespace HockeySlam.Class.GameEntities.Agents
{
	public class ReactiveAgent : Agent
	{
		public ReactiveAgent(GameManager gameManager, Game game, Camera camera, int team)
			: base(gameManager, game, camera, team)
		{ }

		protected override void generateKeys()
		{
			bool isDiskInRange;
			if (isDiskAhead() && !_hasDisk && !sameTeam(_disk.getPlayerWithDisk())) {
				isDiskInRange = moveTowardsDisk();
				if (isDiskInRange && !_hasShoot && _player.getPositionVector() != _lastPositionWithDisk) {
					grabDisk();
				}
			} else if (_hasDisk)
				findGoal();
			else if (!_hasDisk)
				moveRandomly();
		}

	}
}
