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
		private SpriteBatch _spriteBatch;
		private readonly FramesPerSecondCounter _fpsCounter = new FramesPerSecondCounter();

		public RacingGame()
		{
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

			RG.Initialize();

			// SpriteBatch
			_spriteBatch = new SpriteBatch(GraphicsDevice);
		}

		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			RG.Update(gameTime);
		}

		protected override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			GraphicsDevice.Clear(Color.Black);

			RG.Render(gameTime);

			_spriteBatch.Begin();

			var font = Nrs.DebugFont;
			var statistics = RG.Graphics.Statistics;
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
