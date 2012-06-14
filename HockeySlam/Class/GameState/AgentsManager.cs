using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using HockeySlam.Class.GameEntities.Models;
using HockeySlam.Class.GameState;
using HockeySlam.Class.GameEntities;
using HockeySlam.Class.GameEntities.Agents;
using HockeySlam.Interface;


namespace HockeySlam.Class.GameState
{
	class AgentsManager: IGameEntity
	{

		GameManager _gameManager;
		Game _game;
		Camera _camera;
		bool _addAgentKeyPressed;
		int _team;

		List<Agent> playerList = new List<Agent>();
		Dictionary<Agent, Thread> playerThreads = new Dictionary<Agent, Thread>();

		public AgentsManager(GameManager gameManager, Game game, Camera camera)
		{
			_gameManager = gameManager;
			_game = game;
			_camera = camera;
			_team = 0;
		}

		public void addReactiveAgent()
		{
			ReactiveAgent ra = new ReactiveAgent(_gameManager, _game, _camera, _team + 1);
			playerList.Add(ra);
			Thread playerThread = new Thread(new ParameterizedThreadStart(ra.update));
			playerThreads.Add(ra, playerThread);
			_team = (_team + 1) % 2;
		}
		public void addBDIAgent()
		{
			BDIAgent ba = new BDIAgent(_gameManager, _game, _camera, _team + 1);
			playerList.Add(ba);
			Thread playerThread = new Thread(new ParameterizedThreadStart(ba.update));
			playerThreads.Add(ba, playerThread);
			_team = (_team + 1) % 2;
		}

		public void Update(GameTime gameTime)
		{
			KeyboardState keyboard = Keyboard.GetState();

			if (keyboard.IsKeyDown(Keys.R) && !_addAgentKeyPressed) {
				addReactiveAgent();
				_addAgentKeyPressed = true;
			} else if (keyboard.IsKeyDown(Keys.T) && !_addAgentKeyPressed) {
				addBDIAgent();
				_addAgentKeyPressed = true;
			} else if ((keyboard.IsKeyUp(Keys.R) || keyboard.IsKeyUp(Keys.T)) && _addAgentKeyPressed)
				_addAgentKeyPressed = false;

			foreach (Agent agent in playerList) {
				Thread playerThread = playerThreads[agent];

				if (playerThread.ThreadState == ThreadState.Unstarted) {
					playerThread.Priority = ThreadPriority.Highest;
					playerThread.Start(gameTime);
				} else if (playerThread.ThreadState == ThreadState.Stopped) {
					playerThread.Join();
					playerThread = new Thread(new ParameterizedThreadStart(agent.update));
					playerThread.Priority = ThreadPriority.Highest;
					playerThread.Start(gameTime);
					playerThreads[agent] = playerThread;
				}
			}
		}

		public void Draw(GameTime gameTime)
		{
			foreach (Agent agent in playerList) {
				agent.draw(gameTime);
			}
		}

		public void Initialize()
		{
			_addAgentKeyPressed = false;
		}

		public void LoadContent()
		{}

		public List<Agent> getAgents()
		{
			return playerList;
		}
	}
}
