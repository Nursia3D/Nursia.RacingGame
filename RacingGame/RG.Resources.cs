using AssetManagementBase;
using Microsoft.Xna.Framework;
using Nursia;
using Nursia.Env;
using Nursia.Env.Sky;
using Nursia.Materials;
using Nursia.SceneGraph;
using Nursia.SceneGraph.Landscape;
using Nursia.SceneGraph.Lights;
using RacingGame.Graphics;
using System.Linq;

namespace RacingGame
{
	partial class RG
	{
		public static class Resources
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

			/// <summary>
			/// We have one direct light for all scenes
			/// </summary>
			public static DirectLight DirectLight { get; } = new DirectLight
			{
				MaxShadowDistance = 500,
				Direction = -LensFlare.DefaultLightPos
			};

			/// <summary>
			/// And one render environment
			/// </summary>
			internal static RenderEnvironment RenderEnvironment { get; private set; }

			/// <summary>
			/// And one terrain
			/// </summary>
			public static TerrainNode Terrain { get; private set; }

			internal static void Initialize()
			{
				// Render environment with skybox
				RenderEnvironment = RenderEnvironment.Default.Clone();

				var sky = new Skybox()
				{
					LocalTransform = Constants.objectMatrix,
					DiffuseColor = new Color(232, 232, 232),
					DiffuseTexturePath = "Textures/SkyCubeMap.dds",
				};

				sky.Load(Assets);

				// RenderEnvironment.Sky = sky;
				RenderEnvironment.FogEnabled = true;

				// Terrain Node
				Terrain = (TerrainNode)Assets.LoadSceneNode("Scenes/Landscape.scene");
				Terrain.Translation = new Vector3(1280, 1280, 0);
				Terrain.Rotation = new Vector3(90, 0, 0);
			}

			public static NursiaModelNode CreateCar(int number)
			{
				var textureName = _textures[number];
				var texture = Assets.LoadTexture2D(Nrs.GraphicsDevice, $"Textures/{textureName}.tga");

				var result = (NursiaModelNode)Assets.LoadSceneNode("Scenes/Car.scene");
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
										RacingGameManager.Player.CarWheelPos);

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
				var result = Assets.LoadSceneNode($"Scenes/{name}.scene");

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

							asModel.ModelInstance.SetBoneLocalTransform(windmillWingsBone.Index, Matrix.CreateRotationZ(TotalTime / 0.654f) * originalTransform);
						};
					}
				});

				return result;
			}

			public static NursiaModelNode LoadModel(string name) => (NursiaModelNode)LoadScene(name);
		}
	}
}
