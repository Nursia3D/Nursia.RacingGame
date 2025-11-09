using AssetManagementBase;
using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nursia;
using Nursia.Materials;
using Nursia.SceneGraph;
using Nursia.SceneGraph.Landscape;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Transactions;

namespace Converter
{
	public class Conversion
	{
		const int GridWidth = 257, GridHeight = 257;

		static readonly Matrix objectMatrix = Matrix.CreateRotationX(MathHelper.Pi / 2.0f);
		private class MaterialData
		{
			public string Effect { get; set; }
			public string Technique { get; set; }

			public Dictionary<string, JsonElement> Parameters { get; set; }
		}

		private class MaterialData2
		{
			public Dictionary<string, MaterialData> Materials { get; set; }
			public Dictionary<string, string[]> MeshesMaterials { get; set; }
		}

		private static string UpdatePath(string path)
		{
			return path.Replace("../textures", "../Textures");
		}

		public static NursiaModelNode LoadFromFile(GraphicsDevice device, string file)
		{
			Console.WriteLine(file);

			var assetManager = AssetManager.CreateFileAssetManager(Path.GetDirectoryName(file));

			var model = assetManager.LoadModel(device, file);
			var result = new NursiaModelNode
			{
				Model = model,
				ModelPath = Utility.TryToMakePathRelativeTo(file, Utility.OutputFolder),
				Rotation = new Vector3(90, 0, 0)
			};

			var materialFile = Path.ChangeExtension(file, "material");
			var data = File.ReadAllText(materialFile);

			var materialData = JsonSerializer.Deserialize<MaterialData2>(data);

			if (materialData.Materials.Count == 0)
			{
				return result;
			}

			var materials = new Dictionary<string, LitSolidMaterial>();
			foreach (var pair in materialData.Materials)
			{
				var md = pair.Value;

				var material = new LitSolidMaterial();

				// Set parameters
				foreach (var pair2 in md.Parameters)
				{
					var val = pair2.Value;

					switch (pair2.Key)
					{
						case "diffuseTexture":
							material.DiffuseTexturePath = UpdatePath(val.GetString());
							break;

						case "normalTexture":
							material.NormalTexturePath = UpdatePath(val.GetString());
							break;

						case "ambientColor":
							material.AmbientColor = val.ToColor();
							break;

						case "diffuseColor":
							material.DiffuseColor = val.ToColor();
							break;

						case "specularColor":
							material.SpecularColor = val.ToColor();
							break;

						case "shininess":
							material.SpecularPower = val.ToFloat();
							break;

						case "lightDir":
						case "reflectionCubeTexture":
						case "NormalizeCubeTexture":
						case "shadowCarColor":
						case "carHueColorChange":
							break;

						default:
							Console.WriteLine($"Skipped parameter {pair2.Key}");
							break;

					}
				}

				materials[pair.Key] = material;
			}

			foreach (var pair in materialData.MeshesMaterials)
			{
				var found = false;
				for (var i = 0; i < model.Meshes.Length; ++i)
				{
					var mesh = model.Meshes[i];
					var name = mesh.Name ?? mesh.ParentBone.Name;

					if (pair.Key == name)
					{
						found = true;
						for (var j = 0; j < mesh.MeshParts.Count; ++j)
						{
							if (pair.Value.Length != 0)
							{
								result.Materials[i][j] = materials[pair.Value[j]];
							}
							else
							{
								result.Materials[i][j] = materials.First().Value;
							}
						}
					}
				}

				if (!found)
				{
					Console.WriteLine($"Warning: could not find mesh {pair.Key}");
				}
			}

			return result;
		}

		private static float CalculateSize(DrModel model)
		{
			var transforms = new Matrix[model.Bones.Length];
			model.CopyAbsoluteBoneTransformsTo(transforms);

			var realScaling = 1.0f;

			// Calculate scaling for this object, used for distance comparisons.
			if (model.Meshes.Length > 0)
			{
				realScaling = model.Meshes[0].BoundingBox.Radius() * transforms[0].Right.Length();
			}

			return realScaling;
		}


		public static SceneNode FromTrackData(string file)
		{
			var result = new SceneNode();

			var folder = Path.GetDirectoryName(file);
			var assetManager = AssetManager.CreateFileAssetManager(Path.Combine(folder, "Scenes"));

			// Terrain
			var terrainSubsceneNode = new SubsceneNode
			{
				NodePath = "Landscape.scene",
				Rotation = new Vector3(90, 0, 0),
				Translation = new Vector3(1280, 1280, 0),
			};

			terrainSubsceneNode.Load(assetManager);

			result.Children.Add(terrainSubsceneNode);

			var terrainNode = terrainSubsceneNode.QueryFirstByType<TerrainNode>();

			var trackData = TrackData.Load(file);

			SubsceneNode subscene;
			Matrix transform;
			foreach (var obj in trackData.NeutralsObjects)
			{
				if (obj.modelName.StartsWith("Track"))
				{
					// Skip self-reference
					continue;
				}

				if (obj.modelName.StartsWith("Combi"))
				{
					var combiPath = Path.Combine(folder, $"{obj.modelName}.CombiModel");
					var combiModels = new TrackCombiModels(combiPath);
					foreach (var combiObj in combiModels.Objects)
					{
						var subscenePath = Path.Combine(folder, $"Scenes/{combiObj.modelName}.scene");

						var modelNode = (NursiaModelNode)assetManager.LoadSceneNode(subscenePath);
						var size = CalculateSize(modelNode.Model);

						subscene = new SubsceneNode
						{
							NodePath = $"{combiObj.modelName}.scene"
						};

						transform = combiObj.matrix * obj.matrix;

						var pos = transform.Translation;

						var height = terrainNode.GetHeight(pos);
						if (pos.Z < height)
						{
							pos.Z = height;
						}
						
						transform.Translation = pos;

						subscene.SetTransform(transform);

						result.Children.Add(subscene);
					}
				}
				else
				{
					var subscenePath = Path.Combine(folder, $"Scenes/{obj.modelName}.scene");

					var modelNode = (NursiaModelNode)assetManager.LoadSceneNode(subscenePath);
					var size = CalculateSize(modelNode.Model);

					subscene = new SubsceneNode
					{
						NodePath = $"{obj.modelName}.scene"
					};

					transform = obj.matrix;

					var pos = transform.Translation;

					var height = terrainNode.GetHeight(pos);
					if (pos.Z < height)
					{
						pos.Z = height;
					}

					transform.Translation = pos;

					subscene.SetTransform(transform);

					result.Children.Add(subscene);
				}
			}

			return result;
		}
	}
}
