using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nursia;
using Nursia.SceneGraph;
using Nursia.SceneGraph.Primitives;
using RacingGame.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RacingGame.Utilities
{
	internal static class ModelUtils
	{
		public static float Radius(this BoundingBox b)
		{
			var m = (float)Math.Max(b.Max.X - b.Min.X, Math.Min(b.Max.Y - b.Min.Y, b.Max.Z - b.Min.Z));

			return m / 2;
		}

		public static Effect GetEffect(this DrMeshPart meshpart) => (Effect)meshpart.Tag;

		public static void SetEffect(this DrMeshPart meshpart, Effect effect) => meshpart.Tag = effect;

		public static Effect[] GetEffects(this DrMesh mesh)
		{
			if (mesh.Tag != null)
			{
				return (Effect[])mesh.Tag;
			}

			var result = new List<Effect>();

			foreach (var meshpart in mesh.MeshParts)
			{
				var effect = meshpart.GetEffect();
				if (effect == null)
				{
					continue;
				}

				if (!result.Contains(effect))
				{
					result.Add(effect);
				}
			}

			mesh.Tag = result.ToArray();
			return (Effect[])mesh.Tag;
		}

		public static string GetBoneMeshName(this DrModelBone bone)
		{
			if (bone.Mesh == null)
			{
				return bone.Name;
			}

			if (bone.Mesh.Name != null)
			{
				return bone.Mesh.Name;
			}

			return bone.Name;
		}

		public static void Draw(this DrMesh mesh)
		{
			var graphicsDevice = Nrs.GraphicsDevice;

			for (int i = 0; i < mesh.MeshParts.Count; i++)
			{
				var meshpart = mesh.MeshParts[i];
				if (meshpart.PrimitiveCount > 0)
				{
					var effect = meshpart.GetEffect();
					for (int j = 0; j < effect.CurrentTechnique.Passes.Count; j++)
					{
						effect.CurrentTechnique.Passes[j].Apply();

						meshpart.Draw(graphicsDevice);
					}
				}
			}
		}

		public static BoundingBox CalculateBoundingBox(this IEnumerable<TangentVertex> vertices)
		{
			return BoundingBox.CreateFromPoints(from v in vertices select v.pos);
		}

		public static float CalculateSize(this DrModel model)
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

		public static SceneNode LoadScene(string name)
		{
			var result = RacingGame.Assets.LoadSceneNode($"Scenes/{name}.scene");

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

						asModel.ModelInstance.SetBoneLocalTransform(windmillWingsBone.Index, Matrix.CreateRotationZ(BaseGame.TotalTime / 0.654f) * originalTransform);
					};
				}
			});

			return result;
		}

		public static NursiaModelNode LoadModel(string name) => (NursiaModelNode)LoadScene(name);
	}
}
