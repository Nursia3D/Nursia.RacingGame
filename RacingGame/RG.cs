using AssetManagementBase;
using Microsoft.Xna.Framework;
using Nursia;
using RacingGame.GameLogic;
using RacingGame.GameScreens;
using RacingGame.Utilities;
using System.IO;

namespace RacingGame
{
	public static partial class RG
	{
		public static AssetManager Assets { get; private set; }

		public static GameTime GameTime { get; set; }

		public static float TotalMs => (float)GameTime.TotalGameTime.TotalMilliseconds;
		public static float TotalTime => TotalMs / 1000.0f;
		public static float ElapsedMs => (float)GameTime.ElapsedGameTime.TotalMilliseconds;

		public static bool InGame => false;
		public static bool IsAppActive => true;

		public static int Width => Nrs.GraphicsDevice.Viewport.Width;
		public static int Height => Nrs.GraphicsDevice.Viewport.Height;

		public static IGameScreen2 CurrentScreen { get; set; }

		public static void Initialize()
		{
			// Asset Manager
			var contentPath = Path.Combine(PathUtils.ExecutingAssemblyDirectory, "Assets");
			Assets = AssetManager.CreateFileAssetManager(contentPath);

			Resources.Initialize();
			Highscores.Initialize();

			// Initial screen is splash
			CurrentScreen = new SplashScreen2();
		}

		public static void Update(GameTime gameTime)
		{
			GameTime = gameTime;

			Input.Update();

			CurrentScreen?.Update();
		}

		public static void Render(GameTime gameTime)
		{
			GameTime = gameTime;
			CurrentScreen?.Render();
		}
	}
}
