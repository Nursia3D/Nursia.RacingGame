using Microsoft.Xna.Framework;
using Nursia.SceneGraph;
using Nursia.SceneGraph.Cameras;
using RacingGame.GameLogic;
using RacingGame.Helpers;
using RacingGame.Landscapes;

namespace RacingGame.GameScreens
{
	public class SplashScreen2 : IGameScreen2
	{
		private readonly Landscape _landscape = new Landscape(RacingGameManager.Level.Advanced);
		private readonly NursiaModelNode _car;
		private PerspectiveCamera _camera = new PerspectiveCamera
		{
			ViewAngle = Constants.FieldOfViewInDegrees,
			NearPlaneDistance = Constants.NearPlane,
			FarPlaneDistance = Constants.FarPlane
		};
		private readonly Player _player;
		private readonly Replay _bestReplay;
		private float _carMenuTime;
		private Vector3 _oldCarForward = Vector3.Zero, _oldCarUp = Vector3.Zero;

		public SplashScreen2()
		{
			_bestReplay = _landscape.BestReplay;
			_player = new Player(_landscape, Vector3.Zero);
			_landscape.SetCarToStartPosition(_player);
			var randomCarNumber = RandomHelper.GetRandomInt(3);
			_car = RG.Resources.CreateCar(randomCarNumber);
		}

		public void Update()
		{
			// Advance menu car preview time
			_carMenuTime += (float)RG.GameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f;
			if (_carMenuTime > _landscape.BestReplay.LapTime)
				_carMenuTime -= _landscape.BestReplay.LapTime;

			// Use data from replay
			Matrix carMatrix = _bestReplay.GetCarMatrixAtTime(_carMenuTime);

			// Interpolate carPos a little
			var carPos = carMatrix.Translation;

			// Set carPos for camera (else the car will drive away from us ^^)
			_player.SetCarPosition(carPos, carMatrix.Forward, carMatrix.Up);

			// Put camera behind car, but make it move smoothly
			Vector3 newCarForward = carMatrix.Forward;
			Vector3 newCarUp = carMatrix.Up;
			if (_oldCarForward == Vector3.Zero)
			{
				_oldCarForward = newCarForward;
			}
			if (_oldCarUp == Vector3.Zero)
			{
				_oldCarUp = newCarUp;
			}

			_oldCarForward = _oldCarForward * 0.95f + newCarForward * 0.05f;
			_oldCarUp = _oldCarUp * 0.95f + newCarUp * 0.05f;

			// Mix camera positions, interpolate slowly, much smoother camera!
			_player.SetCameraPosition(carPos + _oldCarForward * 13 - _oldCarUp * 1.3f);
			_player.Update();

			_camera.View = _player.RotationMatrix;
			_car.GlobalTransform = Constants.objectMatrix * _player.CarRenderMatrix;
		}

		public void Render()
		{
			RG.Graphics3D.AddToRender(RG.Resources.DirectLight);
			RG.Graphics3D.AddToRender(_landscape.Scene);
			RG.Graphics3D.AddToRender(_car);
			RG.Graphics3D.DoRender(_camera);
		}
	}
}
