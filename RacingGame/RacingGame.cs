using AssetManagementBase;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nursia;
using RacingGame.GameScreens;
using RacingGame.Graphics;
using RacingGame.Utilities;
using System.IO;

namespace RacingGame
{
	public class RacingGame : Game
	{
		private readonly GraphicsDeviceManager _graphics;
		private RenderSystem _renderSystem;
		private SpriteBatch _spriteBatch;
		private readonly FramesPerSecondCounter _fpsCounter = new FramesPerSecondCounter();
		private AssetManager _assets = null;

		public IGameScreen2 CurrentScreen { get; set; }

		public static RacingGame Instance { get; private set; }
		public static AssetManager Assets => Instance._assets;
		public static bool InGame => false;

		public RacingGame()
		{
			Instance = this;

			_graphics = new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth = 1600,
				PreferredBackBufferHeight = 900,
				GraphicsProfile = GraphicsProfile.HiDef
			};

			Window.AllowUserResizing = true;
			IsMouseVisible = true;
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			// Required to work with Nursia
			Nrs.Game = this;

			var contentPath = Path.Combine(PathUtils.ExecutingAssemblyDirectory, "Assets");
			_assets = AssetManager.CreateFileAssetManager(contentPath);

			_renderSystem = new RenderSystem();

			// SpriteBatch
			_spriteBatch = new SpriteBatch(GraphicsDevice);

			Highscores.Initialize();

			CurrentScreen = new SplashScreen2();
		}

		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			CurrentScreen?.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			GraphicsDevice.Clear(Color.Black);

			CurrentScreen?.Render(_renderSystem);

			_spriteBatch.Begin();

			var font = Nrs.DebugFont;
			var statistics = _renderSystem.Renderer3D.Statistics;
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
