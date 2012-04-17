﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using HockeySlam.Class.GameEntities;
using HockeySlam.Class.GameEntities.Models;
using HockeySlam.Interface;
namespace HockeySlam.Class.GameState
{
	class GameManager
	{
		#region Fields

		Dictionary<string, IGameEntity> activeEntities = new Dictionary<string, IGameEntity>();
		Dictionary<string, IGameEntity> allEntities = new Dictionary<string, IGameEntity>();
		Camera camera;
		Game game;
		

		#endregion

		#region Initialization

		public GameManager(Game game)
		{
			this.game = game;
			camera = new Camera(game, new Vector3(85, 85, 0), Vector3.Zero, new Vector3(0, 1, 0));
			AddEntity("camera1", camera);
			AddEntity("debugManager", new DebugManager());
			AddEntity("collisionManager", new CollisionManager());
			//AddEntity("court", new Court(game, camera));
			//AddEntity("multiplayerManager", new MultiplayerManager(game));
			AddEntity("player1", new Player(this, game, camera));
			AddEntity("disk", new Disk(this, game, camera));
			AddEntity("ice", new Ice(game, camera));
			ActivateAllEntities();
			Initialize();
			LoadContent();
		}

		public void Initialize()
		{
			foreach (KeyValuePair<string, IGameEntity> pair in allEntities)
				pair.Value.Initialize();
		}

		protected void LoadContent()
		{
			foreach (KeyValuePair<string, IGameEntity> pair in allEntities)
				pair.Value.LoadContent();
		}

		#endregion

		#region Methods

		protected void AddEntity(string name, IGameEntity entity)
		{
			allEntities.Add(name, entity);
		}

		public IGameEntity getGameEntity(string name)
		{
			if (!allEntities.ContainsKey(name))
				return null;

			return allEntities[name];
		}

		protected void ActivateAllEntities()
		{
			activeEntities.Clear();

			foreach (KeyValuePair<string, IGameEntity> pair in allEntities)
				activeEntities.Add(pair.Key, pair.Value);
		}

		protected void ActivateEntity(string name)
		{
			if (!activeEntities.ContainsKey(name))
				activeEntities.Add(name, allEntities[name]);
		}

		protected void DeactivateEntity(string name)
		{
			if (activeEntities.ContainsKey(name))
				activeEntities.Remove(name);
		}

		#endregion

		#region Update & Draw

		public void Update(GameTime gameTime)
		{
			foreach (KeyValuePair<string, IGameEntity> pair in activeEntities)
				pair.Value.Update(gameTime);
		}

		public void Draw(GameTime gameTime)
		{
			BlendState lastBlend = game.GraphicsDevice.BlendState;
			DepthStencilState lastDepth = game.GraphicsDevice.DepthStencilState;

			game.GraphicsDevice.BlendState = BlendState.Opaque;
			game.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

			Ice ice = (Ice)getGameEntity("ice");
			ice.preDraw(gameTime);

			foreach (KeyValuePair<string, IGameEntity> pair in activeEntities)
				pair.Value.Draw(gameTime);

			game.GraphicsDevice.BlendState = lastBlend;
			game.GraphicsDevice.DepthStencilState = lastDepth;

			/************/

		}

		#endregion
	}
}
