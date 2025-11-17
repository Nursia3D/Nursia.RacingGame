using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D.UI;
using Nursia;
using Nursia.Env;
using Nursia.Env.Sky;
using Nursia.Rendering;
using Nursia.SceneGraph;
using Nursia.SceneGraph.Landscape;
using Nursia.SceneGraph.Lights;
using Nursia.Utilities;
using RacingDemo.GameLogic;
using RacingDemo.Landscapes;
using RacingDemo.UI;
using System;

namespace RacingDemo
{
	public class RacingGame : Game
	{
		private static RacingGame _instance;
		private readonly GraphicsDeviceManager _graphics;
		private SpriteBatch _spriteBatch;
		private readonly FramesPerSecondCounter _fpsCounter = new FramesPerSecondCounter();
		private GameTime _gameTime;
		private ForwardRenderer _renderer;
		private SceneNode _root;
		private DirectLight _directLight;
		private Landscape _landscape;
		private NursiaModelNode _car;
		private Camera _camera;
		private Camera _pauseCamera;
		private Player _player;
		private Replay _bestReplay;
		private float _carMenuTime;
		private Vector3 _oldCarForward = Vector3.Zero, _oldCarUp = Vector3.Zero;
		private RenderEnvironment _renderEnvironment;
		private TerrainNode _terrain;
		private int _frameCount;
		private Desktop _desktop;
		private ToggleButton _optionsButton;
		private OptionsWindow _optionsWindow;
		private bool _isPaused;
		private CameraInputController _cameraController;

		public static TerrainNode Terrain => _instance._terrain;
		public static Player Player => _instance._player;
		public static Landscape Landscape => _instance._landscape;
		public static RenderEnvironment RenderEnvironment => _instance._renderEnvironment;

		public static GameTime GameTime => _instance._gameTime;

		public static float TotalMs => (float)GameTime.TotalGameTime.TotalMilliseconds;
		public static float TotalTime => TotalMs / 1000.0f;
		public static float ElapsedMs => (float)GameTime.ElapsedGameTime.TotalMilliseconds;
		public static float ElapsedTime => ElapsedMs / 1000.0f;
		public static int TotalFrames => _instance._frameCount;

		public RacingGame()
		{
			_instance = this;

			_graphics = new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth = 1600,
				PreferredBackBufferHeight = 900,
				GraphicsProfile = GraphicsProfile.HiDef,
				SynchronizeWithVerticalRetrace = false
			};

			Window.AllowUserResizing = true;
			IsMouseVisible = true;
			IsFixedTimeStep = false;
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
			_landscape = new Landscape(RacingDemoLevel.Beginner);

			// Camera
			_camera = new Camera
			{
				ViewAngle = Constants.FieldOfViewInDegrees,
				NearPlane = Constants.NearPlane,
				FarPlane = Constants.FarPlane
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

			Nrs.GraphicsSettings.ShadowMapSize = ShadowMapSize.Size2048;
			Nrs.GraphicsSettings.ShadowType = ShadowType.Simple;

			// Myra
			MyraEnvironment.Game = this;
			_desktop = new Desktop();

			var panel = new Panel();

			_optionsButton = new ToggleButton
			{
				Content = new Label
				{
					Text = "/c[red]Op/cdtions"
				},
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Top,
				Left = -8,
				Top = 8
			};

			_optionsButton.Click += _optionsButton_Click;
			panel.Widgets.Add(_optionsButton);
			_desktop.Root = panel;

			// SpriteBatch
			_spriteBatch = new SpriteBatch(GraphicsDevice);
		}

		private void _optionsButton_Click(object sender, EventArgs e)
		{
			if (_optionsButton.IsToggled)
			{
				_optionsWindow = new OptionsWindow();
				_optionsWindow.Closed += (s, a) => _optionsButton.IsToggled = false;
				_optionsWindow.Show(_desktop);
			}
			else if (_optionsWindow != null)
			{
				_optionsWindow.Close();
				_optionsWindow = null;
			}
		}

		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			_gameTime = gameTime;

			++_frameCount;

			if (!_isPaused)
			{
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
			else
			{
				_cameraController.Update();
			}

			KeyboardUtils.Begin();

			if (KeyboardUtils.IsPressed(Keys.O))
			{
				_optionsButton.DoClick();
			}

			if (KeyboardUtils.IsPressed(Keys.Space))
			{
				if (!_isPaused)
				{
					_isPaused = true;
					_pauseCamera = (Camera)_camera.Clone();
					_pauseCamera.View = _camera.View;
					_cameraController = new CameraInputController(_pauseCamera);
				}
				else
				{
					_isPaused = false;
				}
			}

			KeyboardUtils.End();
		}

		protected override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			_gameTime = gameTime;

			GraphicsDevice.Clear(Color.Black);

			var vp = Nrs.GraphicsDevice.Viewport;

			var camera = _isPaused ? _pauseCamera : _camera;
			camera.SetViewport(vp);
			_renderer.Render(_root, camera, _renderEnvironment);

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

			_desktop.Render();

			_fpsCounter.OnFrameDrawn();
		}
	}
}
