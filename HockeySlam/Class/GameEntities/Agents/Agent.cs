﻿using System;
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
	public class Agent : IDebugEntity
	{
		protected BoundingFrustum _fov;
		protected Player _player;
		protected float _viewDistance = 30;
		protected Game _game;
		protected Camera _camera;
		protected Court _court;
		protected Disk _disk;
		protected BoundingSphere _boundingSphere;
		protected Random _randomGenerator;
		protected Vector2 _direction;
		protected Vector3 _lastPositionWithDisk;
		protected float _fovRotation;
		protected float _farPlane;

		protected bool _hasDisk;
		protected int _team;
		protected bool _hasShoot;

		protected Matrix view
		{
			get;
			set;
		}

		protected Matrix projection
		{
			get;
			set;
		}

		public Agent(GameManager gameManager, Game game, Camera camera, int team)
		{
			_player = new Player(gameManager, game, camera, team, true);
			_game = game;
			_camera = camera;
			_fovRotation = 0;
			float x, y, z;
			x = (float)Math.Cos(_fovRotation) * _viewDistance;
			z = (float)Math.Sin(_fovRotation) * _viewDistance;
			y = _player.getPositionVector().Y;
			view = Matrix.CreateLookAt(_player.getPositionVector(), new Vector3(x, y, z), Vector3.Up);

			_farPlane = _viewDistance;

			projection = Matrix.CreatePerspectiveFieldOfView(
			    MathHelper.PiOver4,
			    (float)game.Window.ClientBounds.Width /
			    (float)game.Window.ClientBounds.Height,
			    1, _farPlane);

			_fov = new BoundingFrustum(view * projection);

			DebugManager dm = (DebugManager)gameManager.getGameEntity("debugManager");
			dm.registerDebugEntities(this);

			_court = (Court)gameManager.getGameEntity("court");
			_disk = (Disk)gameManager.getGameEntity("disk");

			_boundingSphere = new BoundingSphere(_player.getPositionVector(), 3f);

			_randomGenerator = new Random();
			_direction = Vector2.Zero;
			_direction.Y = -1;

			_player.Initialize();
			_player.LoadContent();

			_team = team;
			_hasShoot = false;

			_lastPositionWithDisk = Vector3.Zero;
		}

		public void DrawDebug()
		{
			BoundingSphereRender.Render(_fov, _game.GraphicsDevice, _camera.view, _camera.projection, Color.Red);
			BoundingSphereRender.Render(_boundingSphere, _game.GraphicsDevice, _camera.view, _camera.projection, Color.Blue);
		}

		protected bool moveTowardsDisk()
		{
			Vector2 newPositionInput = Vector2.Zero;
			List<BoundingSphere> listBounding = _disk.getBoundingSpheres();

			if (listBounding[0].Intersects(_boundingSphere)) {
				_player.PositionInput = Vector2.Zero;
				return true;
			}

			if (_player.getPositionVector().X < _disk.getPosition().X)
				newPositionInput.Y = 2;
			if (_player.getPositionVector().X > _disk.getPosition().X)
				newPositionInput.Y = 1;
			if (_player.getPositionVector().X == _disk.getPosition().X)
				newPositionInput.Y = 0;
			if (_player.getPositionVector().Z < _disk.getPosition().Z)
				newPositionInput.X = 1;
			if (_player.getPositionVector().Z > _disk.getPosition().Z)
				newPositionInput.X = 2;
			if (_player.getPositionVector().Z == _disk.getPosition().Z)
				newPositionInput.X = 0;

			_player.PositionInput = newPositionInput;

			return false;
		}

		protected bool canSeeGoal()
		{
			if (_team == 1 && _fov.Intersects(_court.getTeam1Goal()))
				return true;
			else if (_team == 2 && _fov.Intersects(_court.getTeam2Goal()))
				return true;

			return false;
		}

		public void update(Object gameTimeObject)
		{
			GameTime gameTime = (GameTime)gameTimeObject;
			if(this != _disk.getPlayerWithDisk())
				_hasDisk = false;

			KeyboardState keyboard = Keyboard.GetState();
			Vector2 pos = Vector2.Zero;
				if (keyboard.IsKeyDown(Keys.A))
					pos.X = 1;
				else if (keyboard.IsKeyDown(Keys.D))
					pos.X = 2;
				else
					pos.X = 0;
			
				if (keyboard.IsKeyDown(Keys.W))
					pos.Y = 1;
				else if (keyboard.IsKeyDown(Keys.S))
					pos.Y = 2;
				else
					pos.Y = 0;

				_player.PositionInput = pos;

			List<BoundingSphere> diskBoundingSpheres = _disk.getBoundingSpheres();
			if (_hasShoot && !_boundingSphere.Intersects(diskBoundingSpheres[0]))
				_hasShoot = false;

			if(pos.X == 0 && pos.Y == 0)
				generateKeys();

			_player.agentsUpdate(gameTime);

			Vector3 playerPosition = _player.getPositionVector();
			_boundingSphere.Center = playerPosition;
			float x, y, z;

			if(isDiskAhead() && !_hasDisk) {
				x = _disk.getPosition().X;
				y = _disk.getPosition().Y;
				z = _disk.getPosition().Z;
			} else {
				x = (float)Math.Cos(_fovRotation) * _viewDistance + playerPosition.X;
				z = (float)Math.Sin(_fovRotation) * _viewDistance + playerPosition.Z;
				y = _player.getPositionVector().Y;
			}
			view = Matrix.CreateLookAt(_player.getPositionVector(), new Vector3(x, y, z), Vector3.Up);
			_fov.Matrix = view * projection;
		}

		public void draw(GameTime gameTime)
		{
			_player.Draw(gameTime);
		}

		protected virtual void generateKeys()
		{ }

		protected void grabDisk()
		{
			_disk.newPlayerWithDisk(this);
			_hasDisk = true;
			_lastPositionWithDisk = _player.getPositionVector();
		}

		protected void moveTowardsDirection(Vector2 direction)
		{
			Vector2 newPositionInput = Vector2.Zero;
			if (direction.X > 0)
				newPositionInput.X = 1;
			if (direction.X < 0)
				newPositionInput.X = 2;
			if (direction.Y > 0)
				newPositionInput.Y = 2;
			if (direction.Y < 0)
				newPositionInput.Y = 1;

			_player.PositionInput = newPositionInput;
		}

		protected void rotate()
		{
			if (_randomGenerator.Next(2) == 0) {
				rotateClockwise();
			} else {
				rotateCounterclockwise();
			}
		}

		protected void rotateCounterclockwise()
		{
			_fovRotation = (_fovRotation - 0.2f) % MathHelper.TwoPi;
			if (_fovRotation < 0)
				_fovRotation = MathHelper.TwoPi - _fovRotation;
			_direction.Y = (float)Math.Cos(_fovRotation);
		}

		protected void rotateClockwise()
		{
			_fovRotation = (_fovRotation + 0.2f) % MathHelper.TwoPi;
			_direction.X = (float)Math.Sin(_fovRotation);
		}


		protected void moveRandomly()
		{
			if (_randomGenerator.Next(2) == 0)
				moveTowardsDirection(_direction);
			else
				rotate();
		}	

		protected void shootToPosition(Vector2 positionToShoot)
		{
			Vector2 shotDirection = Vector2.Zero;

			shotDirection.Y = positionToShoot.X - _disk.getPosition().X;
			shotDirection.X = positionToShoot.Y - _disk.getPosition().Z;

			shotDirection = Vector2.Normalize(shotDirection);

			_hasDisk = false;
			_hasShoot = true;
			_disk.shoot(shotDirection);
		}

		protected bool isDiskAhead()
		{
			List<BoundingSphere> boundingList = _disk.getBoundingSpheres();
			foreach(BoundingSphere bs in boundingList) {
				if (bs.Intersects(_fov)) {
					return true;
				}
			}
			return false;
		}

		protected bool canSeePlayer(Agent agent)
		{
			Player agentPlayer = agent.getPlayer();
			List<BoundingSphere> boundingList = agentPlayer.getBoundingSpheres();
			foreach (BoundingSphere bs in boundingList) {
				if (bs.Intersects(_fov))
					return true;
			}
			return false;
		}

		protected bool isWallAhead()
		{
			List<BoundingBox> boundingList = _court.getBoundingBoxes();
			foreach (BoundingBox bb in boundingList) {
				if (bb.Intersects(_fov)) {
					
					return true;
				}
			}
			return false;
		}

		public void removePlayerDisk()
		{
			_hasDisk = false;
		}

		public Player getPlayer()
		{
			return _player;
		}

		public int getTeam()
		{
			return _team;
		}

		protected bool sameTeam(Agent agent)
		{
			if(agent != null) 
				return agent.getTeam() == _team;
			return false;
		}
	}
}
