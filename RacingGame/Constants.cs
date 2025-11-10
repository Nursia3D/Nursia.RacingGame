using Microsoft.Xna.Framework;
using System;

namespace RacingGame
{
	internal static class Constants
	{
		/// <summary>
		/// Car colors for the car selection screen.
		/// </summary>
		public static readonly Color[] CarColors = new Color[]
		{
			Color.White,
			Color.Yellow,
			Color.Blue,
			Color.Purple,
			Color.Red,
			Color.Green,
			Color.Teal,
			Color.Gray,
			Color.Chocolate,
			Color.Orange,
			Color.SeaGreen,
		};

		/// <summary>
		/// Default object matrix to fix models from 3ds max to our engine!
		/// </summary>
		public static readonly Matrix objectMatrix =
			//right handed models: Matrix.CreateRotationX(MathHelper.Pi);// *
			//Matrix.CreateScale(MaxModelScaling);
			// left handed models (else everything is mirrored with x files)
			Matrix.CreateRotationX(MathHelper.Pi / 2.0f);

		/// <summary>
		/// Default color values are:
		/// 0.15f for ambient and 1.0f for diffuse and 1.0f specular.
		/// </summary>
		public static readonly Color
			DefaultAmbientColor = new Color(40, 40, 40),
			DefaultDiffuseColor = new Color(210, 210, 210),
			DefaultSpecularColor = new Color(255, 255, 255);

		public const float FieldOfViewInDegrees = 90.0f;
		public const float NearPlane = 0.5f;
		public const float FarPlane = 1750.0f;
	}
}
