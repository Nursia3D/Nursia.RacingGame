using AssetManagementBase;
using Microsoft.Xna.Framework;
using Nursia;
using Nursia.Materials;
using Nursia.SceneGraph;
using RacingGame.Utilities;
using System.IO;
using System.Linq;

namespace RacingGame
{
	internal static class Assets
	{
		private static readonly string[] _textures = new string[]
		{
				"RacerCar", "RacerCar2", "RacerCar3"
		};

		public static AssetManager Manager { get; private set; }

		/// <summary>
		/// Number of car texture types
		/// </summary>
		/// <returns>Int</returns>
		public static int NumberOfCars => _textures.Length;

		public static void Initialize()
		{
			var contentPath = Path.Combine(PathUtils.ExecutingAssemblyDirectory, "Assets");
			Manager = AssetManager.CreateFileAssetManager(contentPath);
		}

		public static NursiaModelNode CreateCar(int number)
		{
			var textureName = _textures[number];
			var texture = Manager.LoadTexture2D(Nrs.GraphicsDevice, $"Textures/{textureName}.tga");

			var result = (NursiaModelNode)Manager.LoadSceneNode("Scenes/Car.scene");
			var wheelsBoneIndices = (from b in result.Model.Bones where b.Mesh != null && b.Mesh.MeshParts.Count == 2 select b.Index).ToArray();

			// Add wheels' turning
			/*			result.PreRender += () =>
						{
							var wheelNumber = 0;

							for (var i = 0; i < wheelsBoneIndices.Length; ++i)
							{
								var idx = wheelsBoneIndices[i];
								wheelNumber++;

								var rotationMatrix = Matrix.CreateRotationX(
									// Rotate left 2 wheels forward, the other 2 backward!
									(wheelNumber == 2 || wheelNumber == 4 ? 1 : -1) *
									GameClass.Player.CarWheelPos);

								result.ModelInstance.SetBoneLocalTransform(idx, rotationMatrix);
							}
						};
			*/
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

		public static SceneNode LoadScene(string name)
		{
			var result = Manager.LoadSceneNode($"Scenes/{name}.scene");

			// Setup animations
			result.Iterate(n =>
			{
				var asModel = n as NursiaModelNode;
				if (asModel == null)
				{
					return;
				}

				var windmillWingsBone = (from bone in asModel.Model.Bones where bone.Name != null && bone.Name.ToLower().StartsWith("windmill_wings") select bone).FirstOrDefault();
				if (windmillWingsBone != null)
				{
					asModel.PreRender += () =>
					{
						var originalTransform = windmillWingsBone.CalculateDefaultLocalTransform();

						asModel.ModelInstance.SetBoneLocalTransform(windmillWingsBone.Index, Matrix.CreateRotationZ(GameClass.TotalTime / 0.654f) * originalTransform);
					};
				}
			});

			return result;
		}

		public static NursiaModelNode LoadModel(string name) => (NursiaModelNode)LoadScene(name);
	}
}
