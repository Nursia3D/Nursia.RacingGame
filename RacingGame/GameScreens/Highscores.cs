#region File Description
//-----------------------------------------------------------------------------
// Highscores.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

namespace RacingGame.GameScreens
{
	/// <summary>
	/// Highscores
	/// </summary>
	/// <returns>IGame screen</returns>
	internal static class Highscores
	{
		#region Highscore helper class
		/// <summary>
		/// Highscore helper class
		/// </summary>
		private struct HighscoreInLevel
		{
			#region Variables
			/// <summary>
			/// Player name
			/// </summary>
			public string name;
			/// <summary>
			/// Highscore points 
			/// </summary>
			public int timeMilliseconds;
			#endregion

			#region Constructor
			/// <summary>
			/// Create highscore
			/// </summary>
			/// <param name="setName">Set name</param>
			/// <param name="setTimeMs">Set time ms</param>
			public HighscoreInLevel(string setName, int setTimeMs)
			{
				name = setName;
				timeMilliseconds = setTimeMs;
			}
			#endregion

			#region ToString
			/// <summary>
			/// To string
			/// </summary>
			/// <returns>String</returns>
			public override string ToString()
			{
				return name + ":" + timeMilliseconds;
			}
			#endregion
		}

		/// <summary>
		/// Number of highscores displayed in this screen.
		/// </summary>
		private const int NumOfHighscores = 10,
			NumOfHighscoreLevels = 3;

		/// <summary>
		/// List of remembered highscores.
		/// </summary>
		private static HighscoreInLevel[,] highscores = null;

		/// <summary>
		/// Create Highscores class, will basically try to load highscore list,
		/// if that fails we generate a standard highscore list!
		/// </summary>
		public static void Initialize()
		{
			// Init highscores
			highscores =
				new HighscoreInLevel[NumOfHighscoreLevels, NumOfHighscores];

			// Generate default lists
			for (int level = 0; level < NumOfHighscoreLevels; level++)
			{
				for (int rank = 0; rank < NumOfHighscores; rank++)
				{
					highscores[level, rank] =
						new HighscoreInLevel("Player " + (rank + 1).ToString(),
							(75000 + rank * 5000) * (level + 1));
				}
			}
		}
		#endregion

		#region Get top lap time
		/// <summary>
		/// Get top lap time
		/// </summary>
		/// <param name="level">Level</param>
		/// <returns>Best lap time</returns>
		public static float GetTopLapTime(int level) => 75 * (level + 1);

		#endregion
	}
}