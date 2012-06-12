﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using HockeySlam.Class.GameEntities.Models;
using Microsoft.Xna.Framework.Input;

using HockeySlam.Class.GameState;
using HockeySlam.Interface;

namespace HockeySlam.Class.GameEntities
{
	public class ReactiveAgent : IDebugEntity
	{
		BoundingFrustum _fov;
		Player _player;
		float _viewDistance=50;
		Game _game;
		Camera _camera;
		Court _court;
		Disk _disk;
		BoundingSphere _boundingSphere;
		Random _randomGenerator;
		Vector2 _direction;
		Vector3 _lastPositionWithDisk;
		float _fovRotation;
		float _farPlane;

		bool _hasDisk;
		int _team;
		bool _hasShoot;

		private Matrix view
		{
			get;
			set;
		}

		private Matrix projection
		{
			get;
			set;
		}
		public ReactiveAgent(GameManager gameManager, Game game, Camera camera, int team)
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

			_farPlane = 50;

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

		private bool moveTowardsDisk()
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

		public void update(GameTime gameTime)
		{
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

		private void generateKeys()
		{
			bool isDiskInRange;
			if (isDiskAhead() && !_hasDisk && !sameTeam(_disk.getPlayerWithDisk())) {
				isDiskInRange = moveTowardsDisk();
				if (isDiskInRange && !_hasShoot && _player.getPositionVector() != _lastPositionWithDisk) {
					grabDisk();
				}
			} else if(_hasDisk)
				findGoal();
			else if(!_hasDisk)
				moveRandomly();
		}

		private void grabDisk()
		{
			_disk.newPlayerWithDisk(this);
			_hasDisk = true;
			_lastPositionWithDisk = _player.getPositionVector();
		}

		private void findGoal()
		{
			bool seeGoal = false;
			if(_team == 1 && _fov.Intersects(_court.getTeam1Goal()))
				seeGoal = true;
			else if(_team == 2 && _fov.Intersects(_court.getTeam2Goal()))
				seeGoal = true;

			if (seeGoal)
				shoot();
			else {
				moveRandomly();
			}
		}

		private void moveTowardsDirection()
		{
			Vector2 newPositionInput = Vector2.Zero;
			if (_direction.X > 0)
				newPositionInput.X = 1;
			if (_direction.X < 0)
				newPositionInput.X = 2;
			if (_direction.Y > 0)
				newPositionInput.Y = 2;
			if (_direction.Y < 0)
				newPositionInput.Y = 1;

			_player.PositionInput = newPositionInput;
		}

		private void rotate()
		{
			if (_randomGenerator.Next(2) == 0) {
				_fovRotation += 0.2f;
				_direction.X = (float)Math.Sin(_fovRotation);
			} else {
				_fovRotation -= 0.2f;
				_direction.Y = (float)Math.Cos(_fovRotation);
			}
		}

		private void moveRandomly()
		{
			if (_randomGenerator.Next(2) == 0)
				moveTowardsDirection();
			else
				rotate();
		}

		private void shoot()
		{
			Vector2 goalPosition;
			Vector2 shotDirection = Vector2.Zero;
			if (_team == 1)
				goalPosition = _court.getTeam1GoalPosition();
			else
				goalPosition = _court.getTeam2GoalPosition();

			shotDirection.Y = goalPosition.X - _disk.getPosition().X;
			shotDirection.X = goalPosition.Y - _disk.getPosition().Z;

			shotDirection = Vector2.Normalize(shotDirection);

			_hasDisk = false;
			_hasShoot = true;
			_disk.shoot(shotDirection);
		}

		private bool isDiskAhead()
		{
			List<BoundingSphere> boundingList = _disk.getBoundingSpheres();
			foreach(BoundingSphere bs in boundingList) {
				if (bs.Intersects(_fov)) {
					return true;
				}
			}
			return false;
		}

		private bool isWallAhead()
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

		public bool sameTeam(ReactiveAgent agent)
		{
			if(agent != null) 
				return agent.getTeam() == _team;
			return false;
		}
	}
}
