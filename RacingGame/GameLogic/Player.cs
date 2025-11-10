#region File Description
//-----------------------------------------------------------------------------
// Player.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using directives
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using RacingGame.GameScreens;
using RacingGame.Graphics;
using RacingGame.Helpers;
using RacingGame.Landscapes;
using RacingGame.Properties;
using RacingGame.Sounds;
using RacingGame.Tracks;
using Texture = RacingGame.Graphics.Texture;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace RacingGame.GameLogic
{
	/// <summary>
	/// Player helper class, holds all the current game properties:
	/// Fuel, Health, Speed, Lifes and Score.
	/// Note: This class is just used in RacingGame and we only have
	/// 1 instance of it for the current player for the current game.
	/// If we want to have more than 1 player (e.g. in multiplayer mode)
	/// you should add a multiplayer class and have all player instances there.
	/// </summary>
	public partial class Player
	{
		private readonly Landscape _landscape;

		#region Global game parameters (game time, game over, etc.)
		/// <summary>
		/// Current game time in ms. Used for time display in game. Also used to
		/// update the sun position and for the highscores.
		/// Will be stopped if we die or if we are still zooming in.
		/// </summary>
		protected float currentGameTimeMilliseconds = 0;

		/// <summary>
		/// Current lap. Increases and when we reach 3, the game is won.
		/// </summary>
		protected int lap;

		/// <summary>
		/// Current lap
		/// </summary>
		public int CurrentLap
		{
			get
			{
				return lap;
			}
		}

		/// <summary>
		/// Remember best lap time, unused until we complete the first lap.
		/// Then it is set every lap, always using the best and fastest lap time.
		/// </summary>
		private float bestLapTimeMilliseconds = 0;

		/// <summary>
		/// Best lap time we have archived in this game
		/// </summary>
		public float BestTimeMilliseconds
		{
			get
			{
				return bestLapTimeMilliseconds;
			}
		}

		/// <summary>
		/// Start new lap, will reset all lap variables and the game time.
		/// If all laps are done the game is over.
		/// </summary>
		protected void StartNewLap()
		{
			lap++;

			_landscape.StartNewLap();

			// Got new best time?
			if (bestLapTimeMilliseconds == 0 ||
				currentGameTimeMilliseconds < bestLapTimeMilliseconds)
				bestLapTimeMilliseconds = currentGameTimeMilliseconds;

			// Start at 0:00.00 again
			currentGameTimeMilliseconds = zoomInTime;
		}

		/// <summary>
		/// Game time ms, will return negative values if currently zooming in!
		/// </summary>
		/// <returns>Int</returns>
		public float GameTimeMilliseconds
		{
			get
			{
				return currentGameTimeMilliseconds - zoomInTime;
			}
		}

		/// <summary>
		/// How long do we zoom in.
		/// </summary>
		public const int StartGameZoomTimeMilliseconds = 5000;

		/// <summary>
		/// Zoom in time
		/// </summary>
		private float zoomInTime = StartGameZoomTimeMilliseconds;

		/// <summary>
		/// Zoom in time
		/// </summary>
		/// <returns>Float</returns>
		protected float ZoomInTime
		{
			get
			{
				return zoomInTime;
			}
			set
			{
				zoomInTime = value;
			}
		}

		/// <summary>
		/// The amount of time to remain fully zoomed in waiting for start light;
		/// </summary>
		public const int StartGameZoomedInTime = 3000;

		/// <summary>
		/// Won or lost?
		/// </summary>
		protected bool victory;

		/// <summary>
		/// Property for Victory
		/// </summary>
		public bool Victory
		{
			get
			{
				return victory;
			}
		}

		/// <summary>
		/// Level num, set when starting game!
		/// </summary>
		protected int levelNum;

		public int LevelNum
		{
			get
			{
				return levelNum;
			}
		}

		/// <summary>
		/// Game over?
		/// </summary>
		protected bool isGameOver;

		/// <summary>
		/// Is game over?
		/// </summary>
		/// <returns>Bool</returns>
		public bool GameOver
		{
			get
			{
				return isGameOver;
			}
		}

		/// <summary>
		/// Did the player win the game? Makes only sense if GameOver is true!
		/// </summary>
		public bool WonGame
		{
			get
			{
				return victory;
			}
		}

		/// <summary>
		/// Remember if we already uploaded our highscore for this game.
		/// Don't do this twice (e.g. when pressing esc).
		/// </summary>
		private bool alreadyUploadedHighscore = false;

		/// <summary>
		/// Set game over and upload highscore
		/// </summary>
		public void SetGameOverAndUploadHighscore()
		{
			// Set gameOver to true to mark this game as ended.
			isGameOver = true;

			// Upload highscore
			if (alreadyUploadedHighscore == false)
			{
				alreadyUploadedHighscore = true;
				Highscores.SubmitHighscore(levelNum,
					(int)currentGameTimeMilliseconds);
			}
		}

		/// <summary>
		/// Helper to determinate if user can control the car.
		/// If game just started we still zoom into the chase camera.
		/// </summary>
		/// <returns>Bool</returns>
		public bool CanControlCar
		{
			get
			{
				return zoomInTime <= 0 &&
					GameOver == false;
			}
		}

		private bool firstFrame = true;
		#endregion

		#region Variables
		/// <summary>
		/// Remember all lap times for the victory screen.
		/// </summary>
		private List<float> lapTimes = new List<float>();

		/// <summary>
		/// The number of laps in each race
		/// </summary>
		private const int LapCount = 3;

		/// <summary>
		/// Add lap time
		/// </summary>
		/// <param name="setLapTime">Lap time</param>
		public void AddLapTime(float setLapTime)
		{
			lapTimes.Add(setLapTime);
		}

		/// <summary>
		/// The amount of time (in milliseconds) the car has
		/// been in the air since last touching the ground
		/// If the car is in the air and does not reach the
		/// ground again for too long, its game over!
		/// </summary>
		private float inAirTimeMilliseconds = 0.0f;

		/// <summary>
		/// The amount of time (in milliseconds) the car must be
		/// in the air before game over occurs
		/// </summary>
		private const float InAirTimeoutMilliseconds = 3000.0f;
		#endregion

		#region Constructor

		/// <summary>
		/// Create chase camera. Sets the car position and the camera position,
		/// which is then used to rotate around the car.
		/// </summary>
		/// <param name="setCarPosition">Set car position</param>
		/// <param name="setDirection">Set direction</param>
		/// <param name="setUp">Set up</param>
		/// <param name="setCameraPos">Set camera pos</param>
		public Player(Landscape landscape, Vector3 setCarPosition, Vector3 setDirection, Vector3 setUp, Vector3 setCameraPos)
		{
			_landscape = landscape ?? throw new ArgumentNullException(nameof(landscape));

			SetCarPosition(setCarPosition, setDirection, setUp);
			// Set camera position and calculate rotation from look pos
			SetCameraPosition(setCameraPos);
		}

		/// <summary>
		/// Create chase camera. Sets the car position and the camera position,
		/// which is then used to rotate around the car.
		/// </summary>
		/// <param name="setCarPosition">Set car position</param>
		/// <param name="setCameraPos">Set camera pos</param>
		public Player(Landscape landscape, Vector3 setCarPosition, Vector3 setCameraPos)
		{
			_landscape = landscape ?? throw new ArgumentNullException(nameof(landscape));

			SetCarPosition(setCarPosition, new Vector3(0, 1, 0), new Vector3(0, 0, 1));
			// Set camera position and calculate rotation from look pos
			SetCameraPosition(setCameraPos);
		}

		/// <summary>
		/// Create chase camera. Just sets the car position.
		/// The chase camera is set behind it.
		/// </summary>
		/// <param name="setCarPosition">Set car position</param>
		public Player(Landscape landscape, Vector3 setCarPosition)
		{
			_landscape = landscape ?? throw new ArgumentNullException(nameof(landscape));

			SetCarPosition(setCarPosition,
				new Vector3(0, 1, 0), new Vector3(0, 0, 1));
			// Set camera position and calculate rotation from look pos
			SetCameraPosition(
				//setCarPosition - new Vector3(0, 0.5f, 1.0f) * carDir);
				setCarPosition + new Vector3(0, 10.0f, 25.0f));
		}

		#endregion

		#region Reset
		/// <summary>
		/// Reset player values.
		/// </summary>
		public void Reset()
		{
			levelNum = TrackSelection.SelectedTrackNumber;
			isGameOver = false;
			alreadyUploadedHighscore = false;
			currentGameTimeMilliseconds = 0;
			bestLapTimeMilliseconds = 0;
			lap = 0;
			victory = false;
			zoomInTime = StartGameZoomTimeMilliseconds;
			firstFrame = true;

			ResetPlayer();
			ResetChaseCamera();

			lapTimes.Clear();
		}
		#endregion

		#region Handle game logic
		/// <summary>
		/// Update game logic, called every frame.
		/// </summary>
		public void Update()
		{
			// Don't handle any more game logic if game is over.
			if (RG.InGame && ZoomInTime <= 0)
			{
				// Game over? Then show end screen!
				if (isGameOver)
				{
					// Just rotate around, don't use camera class!
					cameraPos = CarPosition + new Vector3(0, -5, +20) +
						Vector3.TransformNormal(new Vector3(30, 0, 0),
						Matrix.CreateRotationZ(RG.TotalMs / 2593.0f));
					rotMatrix = Matrix.CreateLookAt(
						cameraPos, CarPosition, CarUpVector);
					int rank = Highscores.GetRankFromCurrentTime(
						this.levelNum, (int)this.BestTimeMilliseconds);
					this.currentGameTimeMilliseconds = this.BestTimeMilliseconds;

					if (victory)
					{
						// Display Victory message
						TextureFont.WriteTextCentered(
							BaseGame.Width / 2, BaseGame.Height / 7,
							"Victory! You won.",
							Color.LightGreen, 1.25f);
					}
					else
					{
						// Display game over message
						TextureFont.WriteTextCentered(
							BaseGame.Width / 2, BaseGame.Height / 7,
							"Game Over! You lost.",
							Color.Red, 1.25f);
					}

					for (int num = 0; num < lapTimes.Count; num++)
						TextureFont.WriteTextCentered(
							BaseGame.Width / 2,
							BaseGame.Height / 7 + BaseGame.YToRes(35) * (1 + num),
							"Lap " + (num + 1) + " Time: " +
							(((int)lapTimes[num]) / 60).ToString("00") + ":" +
							(((int)lapTimes[num]) % 60).ToString("00") + "." +
							(((int)(lapTimes[num] * 100)) % 100).ToString("00"),
							Color.White, 1.25f);
					TextureFont.WriteTextCentered(
						BaseGame.Width / 2,
						BaseGame.Height / 7 + BaseGame.YToRes(35) * (1 + lapTimes.Count),
						"Rank: " + (1 + rank),
						Color.White, 1.25f);

					// Don't continue processing game logic
					return;
				}

				// Check if car is in the air,
				// used to check if the player died.
				if (this.isCarOnGround == false)
					inAirTimeMilliseconds += RG.ElapsedMs;
				else
					// Back on ground, reset
					inAirTimeMilliseconds = 0;

				// Game not over yet, check if we lost or won.
				// Check if we have fallen from the track
				float trackDistance = Vector3.Distance(CarPosition, groundPlanePos);
				if (trackDistance > 20 ||
					inAirTimeMilliseconds > InAirTimeoutMilliseconds)
				{
					// Reset player variables (stop car, etc.)
					ClearVariablesForGameOver();

					// And indicate that game is over and we lost!
					isGameOver = true;
					victory = false;
					Sound.Play(Sound.Sounds.CarLose);

					// Also stop engine sound
					Sound.StopGearSound();
				}

				// Finished all laps? Then we won!
				if (CurrentLap >= LapCount)
				{
					// Reset player variables (stop car, etc.)
					ClearVariablesForGameOver();

					// When you win, you start an extra lap we don't want to show
					this.lap--;

					// Then game is over and we won!
					isGameOver = true;
					victory = true;
					Sound.Play(Sound.Sounds.Victory);

					// Also stop engine sound
					Sound.StopGearSound();
				}
			}

			// Since there is no loading screen, we need to skip the first frame because
			// the loading will cause ElapsedTimeThisFrameInMilliseconds to be too high
			if (firstFrame)
			{
				firstFrame = false;
			}
			else
			{
				// Handle zoomInTime at the beginning of a game
				if (RG.InGame && zoomInTime > 0)
				{
					float lastZoomInTime = zoomInTime;
					zoomInTime -= RG.ElapsedMs;

					if (zoomInTime < 2000 &&
						(int)((lastZoomInTime + 1000) / 1000) != (int)((zoomInTime + 1000) / 1000))
					{
						// Handle start traffic light object (red, yellow, green!)
						_landscape.ReplaceStartLightObject(
							2 - (int)((zoomInTime + 1000) / 1000));
					}
				}
			}

			// Don't handle any more game logic if game is over or still zooming in.
			if (CanControlCar)
			{
				// Increase game time
				currentGameTimeMilliseconds += RG.ElapsedMs;
			}

			UpdatePlayer();
			UpdateChaseCamera();
		}
		#endregion
	}
}
