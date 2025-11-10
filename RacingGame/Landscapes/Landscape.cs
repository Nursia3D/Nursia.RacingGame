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
using Microsoft.Xna.Framework.Graphics;
using Nursia;
using Nursia.Materials;
using Nursia.SceneGraph;
using Nursia.SceneGraph.Landscape;
using Nursia.SceneGraph.Lights;
using RacingGame.GameLogic;
using RacingGame.GameScreens;
using RacingGame.Graphics;
using RacingGame.Sounds;
using RacingGame.Tracks;
using RacingGame.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;

#endregion

namespace RacingGame.Landscapes
{
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
				if (number == 2)
					Sound.Play(Sound.Sounds.Bleep);
				else
					Sound.Play(Sound.Sounds.Beep);

				var model = ModelUtils.LoadModel(starLightsModels[number]);
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

		public float GetMapHeight(float x, float y)
		{
			return _terrainNode.GetHeight(new Vector3(x, y, 0));
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

			var model = ModelUtils.LoadModel(modelName);
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
				var model = ModelUtils.LoadModel(modelName);
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

		private readonly DirectLight _directLight = new DirectLight
		{
			MaxShadowDistance = 500
		};

		private readonly RenderCallbackNode _brakesNode;
		private readonly TerrainNode _terrainNode;
		private readonly SceneNode _scene = new SceneNode();

		/// <summary>
		/// Currently loaded level
		/// </summary>
		RacingGameManager.Level level = RacingGameManager.Level.Beginner;

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
					_scene.Children.Add(_directLight);

					_scene.Children.Add(_terrainNode);

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

					// Render all brake tracks
					_scene.Children.Add(_brakesNode);
				}

				return _scene;
			}
		}


		/// <summary>
		/// Compare checkpoint time to the bestReplay times.
		/// </summary>
		/// <param name="checkpointNum">Checkpoint num</param>
		/// <returns>Time in milliseconds we improved</returns>
		public int CompareCheckpointTime(int checkpointNum)
		{
			// Invalid data?
			if (bestReplay == null ||
				checkpointNum >= bestReplay.CheckpointTimes.Count)
				// Then we can't return anything
				return 0;

			// Else just return difference
			float differenceMs =
				RacingGameManager.Player.GameTimeMilliseconds -
				bestReplay.CheckpointTimes[checkpointNum] * 1000.0f;

			return (int)differenceMs;
		}

		/// <summary>
		/// Start new lap, checks if the newReplay is good and
		/// can be stored as best replay :)
		/// </summary>
		public void StartNewLap()
		{
			float thisLapTime =
				RacingGameManager.Player.GameTimeMilliseconds / 1000.0f;

			// Upload new highscore (as we currently are in game,
			// no bonus or anything will be added, this score is low!)
			Highscores.SubmitHighscore((int)level,
				(int)RacingGameManager.Player.GameTimeMilliseconds);

			RacingGameManager.Player.AddLapTime(thisLapTime);

			if (thisLapTime < bestReplay.LapTime)
			{
				// Add final checkpoint
				RacingGameManager.Landscape.NewReplay.CheckpointTimes.Add(
					thisLapTime);

				// Record lap time
				newReplay.LapTime = thisLapTime;

				// Save this replay to load it everytime in the future
				// Do this on a worker thread to prevent the game from skipping frames
				ThreadPool.QueueUserWorkItem(new WaitCallback(SaveReplay),
											(Replay)newReplay.Clone());

				// Set it as the current best replay
				bestReplay = newReplay;
			}

			// And start a new replay for this round
			newReplay = new Replay((int)level, true, track);
		}

		/// <summary>
		/// Callback used for saving a replay from a worker thread
		/// </summary>
		/// <param name="replay">Replay to be saved</param>
		private void SaveReplay(object replay)
		{
#if FNA
			((Replay)replay).Save();
#endif
		}

		/// <summary>
		/// New replay
		/// </summary>
		public Replay NewReplay
		{
			get
			{
				return newReplay;
			}
		}

		/// <summary>
		/// Remember a list of brack tracks, which will be generated if we brake.
		/// </summary>
		List<TangentVertex> brakeTracksVertices = new List<TangentVertex>();

		/// <summary>
		/// Little helper to avoid creating a new array each frame for rendering
		/// </summary>
		TangentVertex[] brakeTracksVerticesArray = null;
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
		public Landscape(RacingGameManager.Level setLevel)
		{
			_terrainNode = (TerrainNode)RacingGame.Assets.LoadSceneNode("Scenes/Landscape.scene");

			_terrainNode.Translation = new Vector3(1280, 1280, 0);
			_terrainNode.Rotation = new Vector3(90, 0, 0);

			#region Load track (and replay inside ReloadLevel method)
			// Load track based on the level selection and set car pos with
			// help of the ReloadLevel method.
			ReloadLevel(setLevel);
			#endregion

			var material = new LitSolidMaterial
			{
				DiffuseColor = Constants.DefaultDiffuseColor,
				SpecularColor = Constants.DefaultSpecularColor,
				AmbientColor = Constants.DefaultAmbientColor,
				DiffuseTexturePath = "Textures/track.tga",
				CastsShadows = false
			};

			material.Load(RacingGame.Assets);

			_brakesNode = new RenderCallbackNode
			{
				Material = material,
				RenderCallback = RenderBrakeTracks
			};
		}

		#region Reload level
		/// <summary>
		/// Reload level
		/// </summary>
		/// <param name="setLevel">Level</param>
		internal void ReloadLevel(RacingGameManager.Level setLevel)
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

			// Kill brake tracks
			brakeTracksVertices.Clear();
			brakeTracksVerticesArray = null;

			// Begin game with red start light
			startLightObject.ChangeModel(starLightsModels[0], ModelUtils.LoadModel(starLightsModels[0]));
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
			player.SetCarPosition(
				track.StartPosition, track.StartDirection, track.StartUpVector);
			// Camera is set in zooming in method of the Player class.
		}
		#endregion

		#region Render
		/// <summary>
		/// Render landscape (just at the origin)
		/// </summary>
		public void Update()
		{
			_directLight.Direction = -BaseGame.LightDirection;
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

		#region Add and render brake tracks
		/// <summary>
		/// Helper to skip track generation if it is near the last generated pos.
		/// </summary>
		Vector3 lastAddedTrackPos = new Vector3(-1000, -1000, -1000);
		/// <summary>
		/// Render a maximum of 140 brake tracks.
		/// </summary>
		const int MaxBrakeTrackVertices = 6 * 140;

		/// <summary>
		/// The amount to raise the break tracks decal off the road surface
		/// </summary>
		const float RaiseBreakTracksAmount = 0.2f;

		/// <summary>
		/// Add brake track
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="dir">Dir vector</param>
		/// <param name="right">Right vector</param>
		public void AddBrakeTrack(Player car)
		{
			Vector3 position = car.CarPosition + car.CarDirection * 1.25f;

			// Just skip if we setting to a similar location again.
			// This check is much faster and accurate for tracks on top of each
			// other than the foreach loop below, which is only useful to
			// put multiple tracks correctly behind each other!
			if (Vector3.DistanceSquared(position, lastAddedTrackPos) < 0.024f ||
				// Limit number of tracks to keep rendering fast.
				brakeTracksVertices.Count > MaxBrakeTrackVertices)
				return;

			lastAddedTrackPos = position;

			const float width = 2.4f; // car is 2.6m width, we use 2.4m for tires
			const float length = 4.5f; // Length of break tracks
			float maxDist =
				(float)Math.Sqrt(width * width + length * length) / 2 - 0.35f;

			// Check if there is any track already set here or nearby?
			for (int num = 0; num < brakeTracksVertices.Count; num++)
				if (Vector3.DistanceSquared(brakeTracksVertices[num].pos, position) <
					maxDist * maxDist)
					// Then skip this brake track, don't put that much stuff on
					// top of each other.
					return;

			// Move position a little bit up (above the road)
			position += Vector3.Normalize(car.CarUpVector) * RaiseBreakTracksAmount;

			// Just add 6 new vertices to render (2 triangles)
			TangentVertex[] newVertices = new TangentVertex[]
			{
                // First triangle
                new TangentVertex(
					position -car.CarRight*width/2 -car.CarDirection*length/2, 0, 0,
					car.CarUpVector, car.CarRight),
				new TangentVertex(
					position -car.CarRight*width/2 +car.CarDirection*length/2, 0, 5,
					car.CarUpVector, car.CarRight),
				new TangentVertex(
					position +car.CarRight*width/2 +car.CarDirection*length/2, 1, 5,
					car.CarUpVector, car.CarRight),
                // Second triangle
                new TangentVertex(
					position -car.CarRight*width/2 -car.CarDirection*length/2, 0, 0,
					car.CarUpVector, car.CarRight),
				new TangentVertex(
					position +car.CarRight*width/2 +car.CarDirection*length/2, 1, 5,
					car.CarUpVector, car.CarRight),
				new TangentVertex(
					position +car.CarRight*width/2 -car.CarDirection*length/2, 1, 0,
					car.CarUpVector, car.CarRight),
			};

			brakeTracksVertices.AddRange(newVertices);
			brakeTracksVerticesArray = brakeTracksVertices.ToArray();
		}

		/// <summary>
		/// Render brake tracks
		/// </summary>
		public void RenderBrakeTracks()
		{
			// Nothing to render?
			if (brakeTracksVerticesArray == null)
				return;

			// Draw the vertices
			Nrs.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList,
				brakeTracksVerticesArray, 0, brakeTracksVerticesArray.Length / 3);
		}
		#endregion
	}
}
