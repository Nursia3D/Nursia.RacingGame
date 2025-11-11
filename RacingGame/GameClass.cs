using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Nursia;
using Nursia.Env;
using Nursia.Env.Sky;
using Nursia.Rendering;
using Nursia.SceneGraph;
using Nursia.SceneGraph.Cameras;
using Nursia.SceneGraph.Landscape;
using Nursia.SceneGraph.Lights;
using RacingGame.GameLogic;
using RacingGame.Landscapes;
using System;

namespace RacingGame
{
	public class GameClass : Game
	{
		private static GameClass _instance;
		private readonly GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;
		private readonly FramesPerSecondCounter _fpsCounter = new FramesPerSecondCounter();
		private GameTime _gameTime;
		private ForwardRenderer _renderer;
		private SceneNode _root;
		private DirectLight _directLight;
		private Landscape _landscape;
		private NursiaModelNode _car;
		private PerspectiveCamera _camera;
		private Player _player;
		private Replay _bestReplay;
		private float _carMenuTime;
		private Vector3 _oldCarForward = Vector3.Zero, _oldCarUp = Vector3.Zero;
		private RenderEnvironment _renderEnvironment;
		private TerrainNode _terrain;
		private int _frameCount;

		public static TerrainNode Terrain => _instance._terrain;
		public static Player Player => _instance._player;
		public static Landscape Landscape => _instance._landscape;

		public static GameTime GameTime => _instance._gameTime;

		public static float TotalMs => (float)GameTime.TotalGameTime.TotalMilliseconds;
		public static float TotalTime => TotalMs / 1000.0f;
		public static float ElapsedMs => (float)GameTime.ElapsedGameTime.TotalMilliseconds;
		public static float ElapsedTime => ElapsedMs / 1000.0f;
		public static int TotalFrames => _instance._frameCount;

		public GameClass()
		{
			_instance = this;

			_graphics = new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth = 1600,
				PreferredBackBufferHeight = 900,
				GraphicsProfile = GraphicsProfile.HiDef
			};

			Window.AllowUserResizing = true;
			IsMouseVisible = false;
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			Nrs.Game = this;
			MyraEnvironment.Game = this;

			// Assets
			Assets.Initialize();

			// 3D stuff
			_renderer = new ForwardRenderer();
			_root = new SceneNode();

			// Render environment with skybox
			_renderEnvironment = RenderEnvironment.Default.Clone();

			var cube = Assets.LoadModel("Cube");

			var sky = new Skybox(cube.Model.Meshes[0].MeshParts[0])
			{
				LocalTransform = Constants.objectMatrix * Matrix.CreateScale(100.0f),
				DiffuseColor = new Color(232, 232, 232),
				DiffuseTexturePath = "Textures/SkyCubeMap.dds",
			};

			sky.Load(Assets.Manager);

			_renderEnvironment.Sky = sky;
			// _renderEnvironment.FogEnabled = true;

			// Terrain Node
			_terrain = (TerrainNode)Assets.LoadScene("Landscape");
			_terrain.Translation = new Vector3(1280, 1280, 0);
			_terrain.Rotation = new Vector3(90, 0, 0);

			// Direct Light
			_directLight = new DirectLight
			{
				MaxShadowDistance = Constants.DefaultMaxShadowDistance,
				Direction = -Constants.DefaultLightPos
			};
			_landscape = new Landscape(RacingGameLevel.Beginner);

			// Camera
			_camera = new PerspectiveCamera
			{
				ViewAngle = Constants.FieldOfViewInDegrees,
				NearPlaneDistance = Constants.NearPlane,
				FarPlaneDistance = Constants.FarPlane
			};

			_bestReplay = _landscape.BestReplay;
			_player = new Player(Vector3.Zero);
			_landscape.SetCarToStartPosition(_player);
			var randomCarNumber = new Random().Next(3);

			// Car
			_car = Assets.CreateCar(randomCarNumber);

			// Build the scene
			_root.Children.Add(_directLight);
			_root.Children.Add(_landscape.Scene);
			_root.Children.Add(_car);

			Nrs.GraphicsSettings.ShadowCascadeSize = ShadowCascadeSize.Size2048;
			Nrs.GraphicsSettings.ShadowType = ShadowType.Simple;

			// SpriteBatch
			_spriteBatch = new SpriteBatch(GraphicsDevice);
		}

		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			_gameTime = gameTime;

			++_frameCount;

			// Advance menu car preview time
			_carMenuTime += (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f;
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

			_camera.View = _player.ViewMatrix;
			_car.LocalTransform = Constants.objectMatrix * _player.CarRenderMatrix;
		}

		protected override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			_gameTime = gameTime;

			GraphicsDevice.Clear(Color.Black);

			var vp = Nrs.GraphicsDevice.Viewport;

			_camera.SetViewport(vp.Width, vp.Height);
			_renderer.Render(_root, _camera, _renderEnvironment);

			_spriteBatch.Begin();

			var font = Nrs.DebugFont;
			var statistics = _renderer.Statistics;
			_spriteBatch.DrawString(font, $"FPS: {_fpsCounter.FramesPerSecond}", new Vector2(0, 0), Color.White);
			_spriteBatch.DrawString(font, $"Effect Switches: {statistics.EffectsSwitches}", new Vector2(0, 24), Color.White);
			_spriteBatch.DrawString(font, $"Draw Calls: {statistics.DrawCalls}", new Vector2(0, 48), Color.White);
			_spriteBatch.DrawString(font, $"Vertices Drawn: {statistics.VerticesDrawn}", new Vector2(0, 72), Color.White);
			_spriteBatch.DrawString(font, $"Primitives Drawn: {statistics.PrimitivesDrawn}", new Vector2(0, 96), Color.White);
			_spriteBatch.DrawString(font, $"Passes: {statistics.Passes}", new Vector2(0, 120), Color.White);

			_spriteBatch.End();

			_fpsCounter.OnFrameDrawn();
		}
	}
}
