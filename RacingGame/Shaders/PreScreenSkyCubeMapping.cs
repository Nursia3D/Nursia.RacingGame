#region File Description
//-----------------------------------------------------------------------------
// PreScreenSkyCubeMapping.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using directives
using RacingGame.Graphics;
using RacingGame.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using RacingGame.GameScreens;
using XnaModel = Microsoft.Xna.Framework.Graphics.Model;
using RacingGame;
using AssetManagementBase;
using DigitalRiseModel;
using Nursia;
#endregion

namespace RacingGame.Shaders
{
	/// <summary>
	/// Pre screen sky cube mapping
	/// </summary>
	public class PreScreenSkyCubeMapping : ShaderEffect
	{
		#region Variables
		/// <summary>
		/// Shader effect filename.
		/// </summary>
		const string Filename = "PreScreenSkyCubeMapping.fx";

		/// <summary>
		/// Sky cube map texture filename.
		/// </summary>
		const string SkyCubeMapFilename = "SkyCubeMap";

		/// <summary>
		/// Default sky color
		/// </summary>
		static readonly Color DefaultSkyColor = new Color(232, 232, 232);

		/// <summary>
		/// The Cube Map texture for the sky!
		/// </summary>
		private TextureCube skyCubeMapTexture = null;

		/// <summary>
		/// Sky cube map texture
		/// </summary>
		/// <returns>Texture cube</returns>
		public TextureCube SkyCubeMapTexture
		{
			get
			{
				return skyCubeMapTexture;
			}
		}

		private DrModel cube;
		#endregion

		#region Constructor
		/// <summary>
		/// Create pre screen sky cube mapping
		/// </summary>
		public PreScreenSkyCubeMapping()
			: base(Filename)
		{
			cube = RacingGame.Assets.LoadModel(Nrs.GraphicsDevice, @"Models\Cube.glb");
		}
		#endregion

		#region Get parameters
		/// <summary>
		/// Reload
		/// </summary>
		protected override void GetParameters()
		{
			base.GetParameters();

			// Load and set cube map texture
			skyCubeMapTexture = RacingGame.Assets.LoadTextureCube(
				Nrs.GraphicsDevice,
				$"textures/{SkyCubeMapFilename}.dds");
			diffuseTexture.SetValue(skyCubeMapTexture);

			// Set sky color to nearly white
			AmbientColor = DefaultSkyColor;
		}
		#endregion

		#region Render sky
		/// <summary>
		/// Render sky with help of shader.
		/// </summary>
		public void RenderSky(Color setSkyColor)
		{
			// Can't render with shader if shader is not valid!
			if (this.Valid == false)
				return;

			// Don't use or write to the z buffer
			Nrs.GraphicsDevice.DepthStencilState = DepthStencilState.None;
			Nrs.GraphicsDevice.RasterizerState = RasterizerState.CullNone;

			// Also don't use any kind of blending.
			Nrs.GraphicsDevice.BlendState = BlendState.Opaque;

			// Set effect parameters
			AmbientColor = setSkyColor;
			effect.Parameters["view"].SetValue(BaseGame.ViewMatrix);
			ProjectionMatrix = BaseGame.ProjectionMatrix;

			// Override model's effect and render
			foreach (var pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();
				cube.Meshes[0].MeshParts[0].Draw(Nrs.GraphicsDevice);
			}

			// Reset previous render states
			Nrs.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
			Nrs.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

			Nrs.GraphicsDevice.BlendState = BlendState.AlphaBlend;
		}

		/// <summary>
		/// Render sky
		/// </summary>
		public void RenderSky()
		{
			RenderSky(lastUsedAmbientColor);
		}
		#endregion
	}
}
