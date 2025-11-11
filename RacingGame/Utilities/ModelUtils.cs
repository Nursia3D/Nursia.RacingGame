using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Nursia;
using Nursia.SceneGraph;
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
	}
}
