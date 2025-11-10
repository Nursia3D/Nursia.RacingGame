using AssetManagementBase;
using Microsoft.Xna.Framework;
using Nursia;
using Nursia.Materials;
using Nursia.SceneGraph;
using RacingGame.Graphics;
using System.Linq;

namespace RacingGame
{
	public static class CarFactory
	{
		private static readonly string[] _textures = new string[]
		{
			"RacerCar", "RacerCar2", "RacerCar3"
		};

		/// <summary>
		/// Number of car texture types
		/// </summary>
		/// <returns>Int</returns>
		public static int NumberOfCars => _textures.Length;

		public static NursiaModelNode CreateCar(int number)
		{
			var textureName = _textures[number];
			var texture = BaseGame.Content.LoadTexture2D(Nrs.GraphicsDevice, $"Textures/{textureName}.tga");

			var result = (NursiaModelNode)BaseGame.Content.LoadSceneNode("Scenes/Car.scene");
			var wheelsBoneIndices = (from b in result.Model.Bones where b.Mesh != null && b.Mesh.MeshParts.Count == 2 select b.Index).ToArray();

			// Add wheels' turning
			result.PreRender += () =>
			{
				var wheelNumber = 0;

				for (var i = 0; i < wheelsBoneIndices.Length; ++i)
				{
					var idx = wheelsBoneIndices[i];
					wheelNumber++;

					var rotationMatrix = Matrix.CreateRotationX(
						// Rotate left 2 wheels forward, the other 2 backward!
						(wheelNumber == 2 || wheelNumber == 4 ? 1 : -1) *
						RacingGameManager.Player.CarWheelPos);

					result.ModelInstance.SetBoneLocalTransform(idx, rotationMatrix);
				}
			};

			// Set texture
			for (var i = 0; i < result.Materials.Length; ++i)
			{
				for (var j = 0; j < result.Materials[i].Length; ++j)
				{
					var mat = (LitSolidMaterial)result.Materials[i][j];

					mat.DiffuseTexture = texture;
				}
			}

			return result;
		}
	}
}
