#region File Description
//-----------------------------------------------------------------------------
// RacingGameManager.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using directives
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Threading;
using RacingGame.GameLogic;
using RacingGame.GameScreens;
using RacingGame.Graphics;
using RacingGame.Landscapes;
using RacingGame.Sounds;
using Texture = RacingGame.Graphics.Texture;
using RacingGame.Properties;
using RacingGame.Shaders;
using Nursia;

#endregion

namespace RacingGame
{
	/// <summary>
	/// This is the main entry class our game. Handles all game screens,
	/// which themself handle all the game logic.
	/// As you can see this class is very simple, which is really cool.
	/// </summary>
	public class RacingGameManager : BaseGame
	{
		#region Variables
		/// <summary>
		/// Game screens stack. We can easily add and remove game screens
		/// and they follow the game logic automatically. Very cool.
		/// </summary>
		private static Stack<IGameScreen> gameScreens = new Stack<IGameScreen>();

		/// <summary>
		/// Player for the game, also allows us to control the car and contains
		/// all the required code for the car physics, chase camera and basic
		/// player values and the game time because this is the top class
		/// of many derived classes. Player, car and camera position is set
		/// when the game starts depending on the selected level.
		/// </summary>
		private static Player player = new Player(null, new Vector3(0, 0, 0));

		/// <summary>
		/// Helper texture for color selection
		/// </summary>
		public static Texture colorSelectionTexture = null;

		/// <summary>
		/// Landscape we are currently using.
		/// </summary>
		private static Landscape landscape = null;

		/// <summary>
		/// Level we use for our track and landscape
		/// </summary>
		public enum Level
		{
			Beginner,
			Advanced,
			Expert,
		}

		/// <summary>
		/// Load level
		/// </summary>
		/// <param name="setNewLevel">Set new level</param>
		public static void LoadLevel(Level setNewLevel)
		{
			landscape.ReloadLevel(setNewLevel);
		}

		/// <summary>
		/// The thread that will load most of the content for this game.
		/// </summary>
		private static Thread loadingThread;

		public static event EventHandler<EventArgs> LoadEvent;
		#endregion

		#region Properties
		/// <summary>
		/// In menu
		/// </summary>
		/// <returns>Bool</returns>
		public static bool InMenu
		{
			get
			{
				return gameScreens.Count > 0 &&
					gameScreens.Peek().GetType() != typeof(GameScreen);
			}
		}

		/// <summary>
		/// In game?
		/// </summary>
		public static bool InGame
		{
			get
			{
				return gameScreens.Count > 0 &&
					gameScreens.Peek().GetType() == typeof(GameScreen);
			}
		}

		/// <summary>
		/// ShowMouseCursor
		/// </summary>
		/// <returns>Bool</returns>
		public static bool ShowMouseCursor
		{
			get
			{
				// Only if not in Game, not in splash screen!
				return gameScreens.Count > 0 &&
					gameScreens.Peek().GetType() != typeof(GameScreen) &&
					gameScreens.Peek().GetType() != typeof(SplashScreen) &&
					gameScreens.Peek().GetType() != typeof(LoadingScreen);
			}
		}

		/// <summary>
		/// In car selection screen
		/// </summary>
		/// <returns>Bool</returns>
		public static bool InCarSelectionScreen
		{
			get
			{
				return gameScreens.Count > 0 &&
					gameScreens.Peek().GetType() == typeof(CarSelection);
			}
		}

		/// <summary>
		/// Player for the game, also allows us to control the car and contains
		/// all the required code for the car physics, chase camera and basic
		/// player values and the game time because this is the top class
		/// of many derived classes.
		/// Easy access here with a static property in case we need the player
		/// somewhere in the game.
		/// </summary>
		/// <returns>Player</returns>
		public static Player Player
		{
			get
			{
				return player;
			}
		}

		/// <summary>
		/// Landscape we are currently using, used for several things (menu
		/// background, the game, some other classes outside the landscape class).
		/// </summary>
		/// <returns>Landscape</returns>
		public static Landscape Landscape
		{
			get
			{
				return landscape;
			}
		}

		public static Thread LoadingThread
		{
			get
			{
				return loadingThread;
			}
		}

		public static bool ContentLoaded
		{
			get
			{
				return loadingThread.ThreadState == ThreadState.Stopped;
			}
		}

		#endregion

		#region Constructor
		/// <summary>
		/// Create Racing game
		/// </summary>
		public RacingGameManager()
			: base("RacingGame")
		{
			Sound.Initialize();
			// Start playing the menu music
			//Sound.Play(Sound.Sounds.MenuMusic);

			// Create main menu at our main entry point
			gameScreens.Push(new MainMenu());

			// But start with splash screen, if user clicks or presses Start,
			// we are back in the main menu.
			gameScreens.Push(new SplashScreen());

			//We want to initially show the loading screen while things start.
			gameScreens.Push(new LoadingScreen());

			loadingThread = new Thread(LoadResources);
			loadingThread.Priority = ThreadPriority.BelowNormal;
		}

		/// <summary>
		/// Create Racing game for unit tests, not used for anything else.
		/// </summary>
		public RacingGameManager(string unitTestName)
			: base(unitTestName)
		{
			// Don't add game screens here
		}

		/// <summary>
		/// Load car stuff
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();
		}

		/// <summary>
		/// Initializes and loads some content, previously referred to as the
		/// "car stuff".
		/// </summary>
		private void LoadResources()
		{
			// Nursia Initialization
			Nrs.Game = this;


			LoadEvent("Models...", null);

			LoadEvent("Landscape...", null);
			// Load landscape
			landscape = new Landscape(Level.Beginner);

			LoadEvent("Textures...", null);
			// Load textures, first one is grabbed from the imported one through
			// the car.x model, the other two are loaded seperately.
			colorSelectionTexture = new Texture("ColorSelection.png");

			LoadEvent("All systems go!", null);
			Thread.Sleep(1000);
		}

		#endregion

		#region Add game screen
		/// <summary>
		/// Add game screen
		/// </summary>
		/// <param name="gameScreen">Game screen</param>
		public static void AddGameScreen(IGameScreen gameScreen)
		{
			// Play sound for screen click
			Sound.Play(Sound.Sounds.ScreenClick);

			// Add the game screen
			gameScreens.Push(gameScreen);
		}
		#endregion

		#region Update
		/// <summary>
		/// Update
		/// </summary>
		protected override void Update(GameTime gameTime)
		{
			// Update game engine
			base.Update(gameTime);

			if (gameScreens.Count > 0)
			{
				if (gameScreens.Peek().GetType() != typeof(LoadingScreen))
				{
					// Update player and game logic
					// player.Update();
				}

				//Update the game screen
				gameScreens.Peek().Update(gameTime);
			}
		}
		#endregion

		#region Render
		/// <summary>
		/// Render
		/// </summary>
		protected override void Render()
		{
			// No more game screens?
			if (gameScreens.Count == 0)
			{
				// Before quiting, stop music and play crash sound :)
				Sound.PlayCrashSound(true);
				Sound.StopMusic();

				// Then quit
				Exit();
				return;
			}

			// Handle current screen
			if (gameScreens.Peek().Render())
			{
				// If this was the options screen and the resolution has changed,
				// apply the changes
				if (gameScreens.Peek().GetType() == typeof(Options) &&
					(BaseGame.Width != GameSettings.Default.ResolutionWidth ||
					BaseGame.Height != GameSettings.Default.ResolutionHeight ||
					BaseGame.Fullscreen != GameSettings.Default.Fullscreen))
				{
					BaseGame.ApplyResolutionChange();
				}

				// Play sound for screen back
				Sound.Play(Sound.Sounds.ScreenBack);

				gameScreens.Pop();
			}
		}

		/// <summary>
		/// Post user interface rendering, in case we need it.
		/// Used for rendering the car selection 3d stuff after the UI.
		/// </summary>
		protected override void PostUIRender()
		{
			// Enable depth buffer again
			Nrs.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

			// Currently in car selection screen?
			if (gameScreens.Count > 0 &&
				gameScreens.Peek().GetType() == typeof(CarSelection))
				((CarSelection)gameScreens.Peek()).PostUIRender();

			// Do menu shader after everything
			if (PostScreenMenu.Started)
				UI.PostScreenMenuShader.Show();
		}
		#endregion
	}
}
