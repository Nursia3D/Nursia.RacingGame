#region File Description
//-----------------------------------------------------------------------------
// Program.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using directives
using System;
using AssetManagementBase;

#endregion

namespace RacingGame
{
	/// <summary>
	/// Program
	/// </summary>
	static class Program
	{
		#region Main
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// <param name="args">Arguments</param>
#if !XBOX360
		[STAThread]
#endif
		static void Main()
		{
			StartGame();
		}
		#endregion

		#region StartGame
		/// <summary>
		/// Start game, is in a seperate method for 2 reasons: We want to catch
		/// any exceptions here, but not for the unit tests and we also allow
		/// the unit tests to call this method if we don't want to unit test
		/// in debug mode.
		/// </summary>
		public static void StartGame()
		{
			try
			{
				AMBConfiguration.Logger = Console.WriteLine;
				// Nrs.DebugSettings.DrawBoundingBoxes = true;
				using (var game = new RacingGame())
				{
					game.Run();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.Write(ex.StackTrace);
			}
		}
		#endregion
	}
}
