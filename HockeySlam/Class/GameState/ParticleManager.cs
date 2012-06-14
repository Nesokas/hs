using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;

using HockeySlam.Interface;
using HockeySlam.Class.GameEntities;
using HockeySlam.Class.GameEntities.Agents;
using HockeySlam.Class.GameEntities.Particles;
using HockeySlam.Class.Particles;
using HockeySlam.Class.GameEntities.Models;

namespace HockeySlam.Class.GameState
{
	class ParticleManager : IGameEntity
	{
		Game game;
		Camera camera;
		NetworkSession networkSession;
		GameManager gameManager;

		List<ParticleSystem> particles;

		public ParticleManager(GameManager gameManager, Game game, Camera camera, NetworkSession networkSession)
		{
			this.game = game;
			this.camera = camera;
			this.networkSession = networkSession;
			this.gameManager = gameManager;
		}

		public void Initialize()
		{
			particles = new List<ParticleSystem>();

			InitializeParticles();

			foreach (ParticleSystem particle in particles)
				particle.Initialize();
		}

		private void InitializeParticles()
		{
			if (networkSession == null) {
				AgentsManager rm = (AgentsManager)gameManager.getGameEntity("agentsManager");
				List<Agent> ras = rm.getAgents();

				foreach (ReactiveAgent ra in ras) {
					Player player = ra.getPlayer();
					particles.Add(new Trail(game, game.Content, player));
					particles.Add(new IceParticles(game, game.Content, player));
				}
			} else {
				foreach (NetworkGamer gamer in networkSession.AllGamers) {
					Player player = gamer.Tag as Player;
					particles.Add(new Trail(game, game.Content, player));
					particles.Add(new IceParticles(game, game.Content, player));
				}
			}
		}

		public void LoadContent()
		{
			foreach (ParticleSystem particle in particles)
				particle.LoadContent();
		}

		public void Update(GameTime gameTime)
		{
			foreach (ParticleSystem particle in particles) {
				particle.SetCamera(camera);
				particle.SpecificUpdate(gameTime);
				particle.Update(gameTime);
			}
		}

		public void Draw(GameTime gameTime)
		{
			foreach (ParticleSystem particle in particles)
				particle.Draw(gameTime);
		}
	}
}
