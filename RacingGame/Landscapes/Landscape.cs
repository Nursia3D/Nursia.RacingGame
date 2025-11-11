#region File Description
//-----------------------------------------------------------------------------
// Landscape.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using directives
using Microsoft.Xna.Framework;
using Nursia.SceneGraph;
using RacingGame.GameLogic;
using RacingGame.Tracks;
using RacingGame.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;

#endregion

namespace RacingGame.Landscapes
{
	/// <summary>
	/// Level we use for our track and landscape
	/// </summary>
	public enum RacingGameLevel
	{
		Beginner,
		Advanced,
		Expert,
	}

	/// <summary>
	/// Landscape
	/// </summary>
	public class Landscape : IDisposable
	{
		private static readonly string[] starLightsModels = new string[]
		{
			"StartLight",
			"StartLight2",
			"StartLight3",
		};

		#region Objects to render on this landscape
		/// <summary>
		/// Landscape object
		/// </summary>
		public class LandscapeObject
		{
			public NursiaModelNode Model { get; private set; }

			public string Name { get; private set; }
			public float Size { get; private set; }

			public Vector3 Position => Model.GlobalTransform.Translation;

			/// <summary>
			/// Create landscape object
			/// </summary>
			/// <param name="name"></param>
			/// <param name="model"></param>
			public LandscapeObject(string name, NursiaModelNode model)
			{
				ChangeModel(name, model);
			}

			/// <summary>
			/// Change model
			/// </summary>
			/// <param name="setNewModel">Set new model</param>
			/// <param name="model"></param>
			public void ChangeModel(string name, NursiaModelNode model)
			{
				if (string.IsNullOrEmpty(name))
				{
					throw new ArgumentException(nameof(name));
				}

				Name = name;
				Model = model ?? throw new ArgumentNullException(nameof(model));
				Size = Model.Model.CalculateSize();
			}
		}

		/// <summary>
		/// List of landscape objects.
		/// </summary>
		List<LandscapeObject> landscapeObjects = new List<LandscapeObject>();

		/// <summary>
		/// Remember start light object because we will exchange it
		/// as the time goes down.
		/// </summary>
		LandscapeObject startLightObject = null;

		/// <summary>
		/// Replace start light object, 0=red, 1=yellow, 2=green.
		/// </summary>
		/// <param name="number">Number</param>
		public void ReplaceStartLightObject(int number)
		{
			// Make sure we only use 0-2
			if (number < 0 || number >= 3)
				number = 0;

			if (startLightObject != null)
			{
				var model = Assets.LoadModel(starLightsModels[number]);
				startLightObject.ChangeModel(starLightsModels[number], model);
			}
		}

		/// <summary>
		/// Kill all loaded objects
		/// </summary>
		public void KillAllLoadedObjects()
		{
			landscapeObjects.Clear();
			startLightObject = null;
		}

		/// <summary>
		/// Combos, which are used in the level file and for the automatic
		/// object generation below. Very useful. Each combo contains between
		/// 5 and 15 landscape model objects.
		/// </summary>
		TrackCombiModels[] combos = new TrackCombiModels[]
		{
			new TrackCombiModels("CombiPalms"),
			new TrackCombiModels("CombiPalms2"),
			new TrackCombiModels("CombiRuins"),
			new TrackCombiModels("CombiRuins2"),
			new TrackCombiModels("CombiStones"),
			new TrackCombiModels("CombiStones2"),
			new TrackCombiModels("CombiOilTanks"),
			new TrackCombiModels("CombiSandCastle"),
			new TrackCombiModels("CombiBuildings"),
			new TrackCombiModels("CombiHotels"),
		};

		/// <summary>
		/// Names for autogenerating stuff near the road to fill the level up.
		/// First 6 entries are used with more propability (fit better).
		/// </summary>
		internal string[] autoGenerationNames = new string[]
			{
				"CombiPalms",
				"CombiPalms2",
				"CombiRuins",
				"CombiRuins2",
				"CombiStones",
				"CombiStones2",
                //causes to much trouble and overlappings: "CombiOilTanks",
                "Kaktus",
				"Kaktus2",
				"KaktusBenny",
				"KaktusSeg",
				"AlphaDeadTree",
				"AlphaPalm",
				"AlphaPalm2",
				"AlphaPalm3",
				"AlphaPalmSmall",
				"Laterne2Sides",
				"Trashcan",
				"OilPump",
				"OilTanks",
				"RoadColumnSegment",
				"Windmill",
				"Ruin",
				"RuinHouse",
				"Sign",
				"Sign2",
				"SharpRock",
				"SharpRock2",
				"Stone4",
				"Stone5",
				"Casino01",
			};

		public static float GetMapHeight(float x, float y)
		{
			return GameClass.Terrain.GetHeight(new Vector3(x, y, 0));
		}

		/// <summary>
		/// Add object to render
		/// </summary>
		/// <param name="modelName">Model name</param>
		/// <param name="renderMatrix">Render matrix</param>
		public void AddObjectToRender(string modelName, Matrix renderMatrix)
		{
			// Search for combos
			for (int num = 0; num < combos.Length; num++)
			{
				TrackCombiModels combi = combos[num];
				//slower: if (StringHelper.Compare(combi.Name, modelName))
				if (combi.Name == modelName)
				{
					// Add all combi objects (calls this method for each model)
					combi.AddAllModels(this, renderMatrix);
					// Thats it.
					return;
				}
			}

			// Fix z position to be always ABOVE the landscape
			Vector3 modelPos = renderMatrix.Translation;

			// Get landscape height here
			float landscapeHeight = GetMapHeight(modelPos.X, modelPos.Y);
			// And make sure we are always above it!
			if (modelPos.Z < landscapeHeight)
			{
				modelPos.Z = landscapeHeight;
				// Fix render matrix
				renderMatrix.Translation = modelPos;
			}

			var model = Assets.LoadModel(modelName);
			var size = model.Model.CalculateSize();

			// Check if another object is nearby, then skip this one!
			// Don't skip signs or banners!
			if (modelName.StartsWith("Banner") == false &&
				modelName.StartsWith("Sign") == false &&
				modelName.StartsWith("StartLight") == false)
			{
				for (int num = 0; num < landscapeObjects.Count; num++)
					if (Vector3.DistanceSquared(landscapeObjects[num].Position, modelPos) < size * size / 4)
					{
						// Don't add
						return;
					}
			}

			// Scale all objects up a little (else world is not filled enough)
			model.GlobalTransform = Constants.objectMatrix * Matrix.CreateScale(1.2f) * renderMatrix;

			var newObject = new LandscapeObject(modelName, model);

			// Add
			landscapeObjects.Add(newObject);

			if (modelName.StartsWith("StartLight"))
				startLightObject = newObject;
		}

		/// <summary>
		/// Add object to render
		/// </summary>
		/// <param name="modelName">Model name</param>
		/// <param name="rotation">Rotation</param>
		/// <param name="trackPos">Track position</param>
		/// <param name="trackRight">Track right</param>
		/// <param name="distance">Distance</param>
		public void AddObjectToRender(string modelName,
			float rotation, Vector3 trackPos, Vector3 trackRight,
			float distance)
		{
			var objSize = 1.0f;

			// Search for combos
			var isCombi = false;
			for (int num = 0; num < combos.Length; num++)
			{
				TrackCombiModels combi = combos[num];
				//slower: if (StringHelper.Compare(combi.Name, modelName))
				if (combi.Name == modelName)
				{
					objSize = combi.Size;
					isCombi = true;
					break;
				}
			}

			if (!isCombi)
			{
				var model = Assets.LoadModel(modelName);
				objSize = model.Model.CalculateSize();
			}

			// Make sure it is away from the road.
			if (distance > 0 &&
				distance - 10 < objSize)
				distance += objSize;
			if (distance < 0 &&
				distance + 10 > -objSize)
				distance -= objSize;

			AddObjectToRender(modelName,
				Matrix.CreateRotationZ(rotation) *
				Matrix.CreateTranslation(
				trackPos + trackRight * distance + new Vector3(0, 0, -100)));
		}

		/// <summary>
		/// Add object to render
		/// </summary>
		/// <param name="modelName">Model name</param>
		/// <param name="renderPos">Render position</param>
		public void AddObjectToRender(string modelName, Vector3 renderPos)
		{
			AddObjectToRender(modelName, Matrix.CreateTranslation(renderPos));
		}
		#endregion

		#region Variables


		private readonly SceneNode _scene = new SceneNode();

		/// <summary>
		/// Currently loaded level
		/// </summary>
		RacingGameLevel level = RacingGameLevel.Beginner;

		/// <summary>
		/// Track for our landscape, can be TrackBeginner, TrackAdvanced and
		/// TrackExpert, which will be selected in the menu.
		/// </summary>
		Track track = null;

		/// <summary>
		/// Best replay for the best lap time showing the player driving.
		/// And a new replay which is recorded in case we archive a better
		/// time this time when we drive :)
		/// </summary>
		Replay bestReplay = null,
			newReplay = null;

		public SceneNode Scene
		{
			get
			{
				if (_scene.Children.Count == 0)
				{
					_scene.Children.Add(GameClass.Terrain);

					_scene.Children.Add(track.Scene);

					_scene.Children.Add(track.RoadMesh);
					_scene.Children.Add(track.RoadBackmesh);

					if (track.RoadTunnelMesh != null)
					{
						_scene.Children.Add(track.RoadTunnelMesh);
					}

					_scene.Children.Add(track.LeftRailMesh);
					_scene.Children.Add(track.RightRailMesh);
					_scene.Children.Add(track.ColumnsMesh);

					// Render all landscape objects
					foreach (var landscapeObject in landscapeObjects)
					{
						_scene.Children.Add(landscapeObject.Model);
					}
				}

				return _scene;
			}
		}

		#endregion

		#region Properties
		/// <summary>
		/// Current track name
		/// </summary>
		/// <returns>String</returns>
		public string CurrentTrackName
		{
			get
			{
				return level.ToString();
			}
		}

		/// <summary>
		/// Track length
		/// </summary>
		/// <returns>Float</returns>
		public float TrackLength
		{
			get
			{
				return track.Length;
			}
		}

		/// <summary>
		/// Remember checkpoint segment positions for easier checkpoint checking.
		/// </summary>
		public List<int> CheckpointSegmentPositions
		{
			get
			{
				return track.CheckpointSegmentPositions;
			}
		}

		/// <summary>
		/// Best replay for the best lap time showing the player driving.
		/// </summary>
		public Replay BestReplay
		{
			get
			{
				return bestReplay;
			}
		}
		#endregion

		#region Constructor
		/// <summary>
		/// Create landscape.
		/// This constructor should only be called
		/// from the RacingGame main class!
		/// </summary>
		/// <param name="setLevel">Level we want to load</param>
		public Landscape(RacingGameLevel setLevel)
		{
			#region Load track (and replay inside ReloadLevel method)
			// Load track based on the level selection and set car pos with
			// help of the ReloadLevel method.
			ReloadLevel(setLevel);
			#endregion
		}

		#region Reload level
		/// <summary>
		/// Reload level
		/// </summary>
		/// <param name="setLevel">Level</param>
		internal void ReloadLevel(RacingGameLevel setLevel)
		{
			level = setLevel;

			// Load track based on the level selection, do this after
			// we got all the height data because the track might be adjusted.
			if (track == null)
				track = new Track("Track" + level.ToString(), this);
			else
				track.Reload("Track" + level.ToString(), this);

			// Load replay for this track to show best player
			bestReplay = new Replay((int)level, false, track);
			newReplay = new Replay((int)level, true, track);

			// Begin game with red start light
			startLightObject.ChangeModel(starLightsModels[0], Assets.LoadModel(starLightsModels[0]));
		}
		#endregion

		#endregion

		#region Dispose
		/// <summary>
		/// Dispose
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose
		/// </summary>
		/// <param name="disposing">Disposing</param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				track.Dispose();
			}
		}
		#endregion

		#region Set car to start pos
		/// <summary>
		/// Set car to start pos
		/// </summary>
		public void SetCarToStartPosition(Player player)
		{
			player.SetCarPosition(track.StartPosition, track.StartDirection, track.StartUpVector);
			// Camera is set in zooming in method of the Player class.
		}
		#endregion

		#region GetTrackPositionMatrix and UpdateCarTrackPosition
		/// <summary>
		/// Get track position matrix, used for the game background and unit tests.
		/// </summary>
		/// <param name="carTrackPos">Car track position</param>
		/// <param name="roadWidth">Road width</param>
		/// <param name="nextRoadWidth">Next road width</param>
		/// <returns>Matrix</returns>
		public Matrix GetTrackPositionMatrix(float carTrackPos,
			out float roadWidth, out float nextRoadWidth)
		{
			return track.GetTrackPositionMatrix(carTrackPos,
				out roadWidth, out nextRoadWidth);
		}

		/// <summary>
		/// Get track position matrix
		/// </summary>
		/// <param name="trackSegmentNum">Track segment number</param>
		/// <param name="trackSegmentPercent">Track segment percent</param>
		/// <param name="roadWidth">Road width</param>
		/// <param name="nextRoadWidth">Next road width</param>
		/// <returns>Matrix</returns>
		public Matrix GetTrackPositionMatrix(
			int trackSegmentNum, float trackSegmentPercent,
			out float roadWidth, out float nextRoadWidth)
		{
			return track.GetTrackPositionMatrix(
				trackSegmentNum, trackSegmentPercent,
				out roadWidth, out nextRoadWidth);
		}

		/// <summary>
		/// Update car track position
		/// </summary>
		/// <param name="carPos">Car position</param>
		/// <param name="trackSegmentNumber">Track segment number</param>
		/// <param name="trackPositionPercent">Track position percent</param>
		public void UpdateCarTrackPosition(
			Vector3 carPos,
			ref int trackSegmentNumber, ref float trackPositionPercent)
		{
			track.UpdateCarTrackPosition(carPos,
				ref trackSegmentNumber, ref trackPositionPercent);
		}
		#endregion
	}
}
