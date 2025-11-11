using Microsoft.Xna.Framework;

namespace RacingGame.GameLogic
{
	public class Player
	{
		/// <summary>
		/// This will be elevated above the car position to let our camera
		/// look at the roof of our car and not at the street.
		/// </summary>
		protected const float CarHeight = 2.0f;

		/// <summary>
		/// Convert our meter per sec to mph for display.
		/// 1 mile = 1.609344 kilometers
		/// Each hour has 3600 seconds (60 min * 60 sec).
		/// 1 kilometer = 1000 meter.
		/// </summary>
		public const float MeterPerSecToMph =
			1.609344f * (60.0f * 60.0f / 1000.0f),
			MphToMeterPerSec = 1.0f / MeterPerSecToMph;

		/// <summary>
		/// Max speed of our car is 275 mph.
		/// While we use mph for the display, we calculate internally with
		/// meters per sec since meter is the unit we use for everthing in the
		/// game. And it is a much nicer unit than miles or feet.
		/// </summary>
		public const float DefaultMaxSpeed =
			275.0f * MphToMeterPerSec,
			MaxPossibleSpeed =
			290.0f * MphToMeterPerSec;

		/// <summary>
		/// Max speed of the car, set from the car type (see CarSelection screen).
		/// We start with the speed 0, then it is increased based on the
		/// current acceleration value to this maxSpeed value.
		/// </summary>
		protected const float maxSpeed = DefaultMaxSpeed * 1.05f;

		/// <summary>
		/// Wheel movement speed for the animation used in the model class.
		/// </summary>
		const float WheelMovementSpeed = 1.0f;

		/// <summary>
		/// Car position, updated each frame by our current carSpeed vector.
		/// </summary>
		Vector3 carPos;

		/// <summary>
		/// Direction the car is currently heading.
		/// </summary>
		Vector3 carDir;

		/// <summary>
		/// Car up vector for orientation.
		/// </summary>
		Vector3 carUp;

		/// <summary>
		/// Current camera position.
		/// </summary>
		Vector3 cameraPos;
		/// <summary>
		/// Distance of the camera to our car.
		/// </summary>
		float cameraDistance;

		/// <summary>
		/// Look vector to the car. The car is our look at target. The up
		/// vector is the same as the one from the car, but the look vector is
		/// different because we slowly interpolate it.
		/// </summary>
		Vector3 cameraLookVector;

		Vector3 wannaCameraLookVector = Vector3.Zero;
		float wannaCameraDistance = 0.0f;

		/// <summary>
		/// Rotation matrix, used in UpdateViewMatrix.
		/// </summary>
		private Matrix rotMatrix = Matrix.Identity;
		
		/// <summary>
		/// Car render matrix we calculate each frame.
		/// </summary>
		Matrix carRenderMatrix = Matrix.Identity;

		/// <summary>
		/// Speed of our car, just in the direction of our car.
		/// Sliding is a nice feature, but it overcomplicates too much and
		/// for this game sliding would be really bad and make it much harder
		/// to drive!
		/// </summary>
		float speed;

		/// <summary>
		/// View distance, which we can change with page up/down and the mouse
		/// wheel, but it always moves back to 1. The real view distance is
		/// also changed depending on how fast we drive (see UpdateCar stuff below)
		/// </summary>
		float viewDistance = 1.0f;

		/// <summary>
		/// Wheel position, used for animating the wheels
		/// </summary>
		private float wheelPos = 0.0f;

		/// <summary>
		/// Helper variables to keep track of our car position on the current
		/// track. Always start with 0 (start pos) and update each frame!
		/// We could also check for the track position each frame by going
		/// through all the track segments, but that would be very slow because
		/// we got a few thousand track segments. Instead we only have to check
		/// the previous and next track segments until we find the right location.
		/// Usually this means we don't have to change or just change the
		/// trackSegmentNumber by 1.
		/// </summary>
		int trackSegmentNumber = 0;
		/// <summary>
		/// Track segment percent, tells us where we are on the current segment.
		/// Always between 0 and 1, for more information
		/// <see>trackSegmentNumber</see>
		/// </summary>
		float trackSegmentPercent = 0;

		/// <summary>
		/// Car right
		/// </summary>
		/// <returns>Vector 3</returns>
		public Vector3 CarRight
		{
			get
			{
				return Vector3.Cross(carDir, carUp);
			}
		}

		/// <summary>
		/// Look at position
		/// </summary>
		/// <returns>Vector 3</returns>
		public Vector3 LookAtPos
		{
			get
			{
				return carPos + carUp * CarHeight;
			}
		}

		public Matrix ViewMatrix => rotMatrix;
		private bool firstFrame = true;

		/// <summary>
		/// Car render matrix, this is the final matrix for rendering our car,
		/// which is calculated in UpdateCarMatrixAndCamera, which is called
		/// by Update each frame.
		/// </summary>
		/// <returns>Matrix</returns>
		public Matrix CarRenderMatrix
		{
			get
			{
				return carRenderMatrix;
			}
		}

		/// <summary>
		/// Create chase camera. Just sets the car position.
		/// The chase camera is set behind it.
		/// </summary>
		/// <param name="setCarPosition">Set car position</param>
		public Player(Vector3 setCarPosition)
		{
			SetCarPosition(setCarPosition,
				new Vector3(0, 1, 0), new Vector3(0, 0, 1));
			// Set camera position and calculate rotation from look pos
			SetCameraPosition(
				//setCarPosition - new Vector3(0, 0.5f, 1.0f) * carDir);
				setCarPosition + new Vector3(0, 10.0f, 25.0f));
		}


		/// <summary>
		/// Set car position
		/// </summary>
		/// <param name="setCarPosition">Set car position</param>
		/// <param name="setDirection">Set direction</param>
		/// <param name="setUp">Set up</param>
		public void SetCarPosition(
			Vector3 setNewCarPosition,
			Vector3 setDirection,
			Vector3 setUp)
		{
			// Add car height to make camera look at the roof and not at the street.
			carPos = setNewCarPosition;
			carDir = setDirection;
			carUp = setUp;
		}

		/// <summary>
		/// Set camera position
		/// </summary>
		/// <param name="setCameraPos">Set camera position</param>
		public void SetCameraPosition(Vector3 setCameraPos)
		{
			cameraPos = setCameraPos;
			cameraDistance = Vector3.Distance(LookAtPos, cameraPos);
			cameraLookVector = LookAtPos - cameraPos;
			wannaCameraDistance = cameraDistance;
			wannaCameraLookVector = cameraLookVector;

			// Build look at matrix
			rotMatrix = Matrix.CreateLookAt(cameraPos, LookAtPos, carUp);
		}

		/// <summary>
		/// Interpolate camera position
		/// </summary>
		/// <param name="setInterpolatedCameraPos">Set interpolated camera
		/// position</param>
		private void InterpolateCameraPosition(Vector3 setInterpolatedCameraPos)
		{
			if (wannaCameraDistance == 0.0f)
				SetCameraPosition(setInterpolatedCameraPos);

			wannaCameraDistance =
				Vector3.Distance(LookAtPos, setInterpolatedCameraPos);
			wannaCameraLookVector = LookAtPos - setInterpolatedCameraPos;
		}

		/// <summary>
		/// Update car matrix and camera
		/// </summary>
		private Matrix UpdateCarMatrixAndCamera()
		{
			// Get car matrix with help of the current car position, dir and up
			Matrix carMatrix = Matrix.Identity;
			carMatrix.Right = CarRight;
			carMatrix.Up = carUp;
			carMatrix.Forward = carDir;
			carMatrix.Translation = carPos;

			// Change distance based on our speed
			float chaseCamDistance =
				(4.25f + 9.75f * speed / maxSpeed) * viewDistance;

			InterpolateCameraPosition(
					carPos + carMatrix.Up * CarHeight +
					carMatrix.Forward * chaseCamDistance / 1.125f -
					carMatrix.Up * 0.8f);

			// For rendering rotate car to stay correctly on the road
			carMatrix =
				Matrix.CreateRotationX(MathHelper.Pi / 2.0f) *
				Matrix.CreateRotationZ(MathHelper.Pi) *
				carMatrix;

			return carMatrix;
		}

		private void UpdatePlayer()
		{
			// Only allow control if zommed in, use carOnGround as helper
			wheelPos += GameClass.ElapsedTime * speed / WheelMovementSpeed;

			float moveFactor = GameClass.ElapsedTime;
			// Make sure this is never below 0.001f and never above 0.5f
			// Else our formulars below might mess up or carSpeed and carForce!
			if (moveFactor < 0.001f)
				moveFactor = 0.001f;
			if (moveFactor > 0.5f)
				moveFactor = 0.5f;

			#region Update track position and handle physics

			int oldTrackSegmentNumber = trackSegmentNumber;
			
			// Find out where we currently are on the track.
			GameClass.Landscape.UpdateCarTrackPosition(
				carPos, ref trackSegmentNumber, ref trackSegmentPercent);

			// And get the TrackMatrix and track values at this location.
			float roadWidth, nextRoadWidth;
			Matrix trackMatrix = GameClass.Landscape.GetTrackPositionMatrix(
				trackSegmentNumber, trackSegmentPercent,
				out roadWidth, out nextRoadWidth);

			// Just set car up from trackMatrix, this should be done
			// better with a more accurate gravity model (see gravity calculation!)
			Vector3 remOldRightVec = CarRight;
			carUp = trackMatrix.Up;
			carDir = Vector3.Cross(carUp, remOldRightVec);

			// Set up the ground and guardrail boundings for the physics calculation.
			Vector3 trackPos = trackMatrix.Translation;
			carRenderMatrix = UpdateCarMatrixAndCamera();

			#endregion
		}

		/// <summary>
		/// Update view matrix
		/// </summary>
		private void UpdateViewMatrix()
		{
			cameraDistance = cameraDistance * 0.9f + wannaCameraDistance * 0.1f;

			// Better interpolation formula, not good for slow framerates,
			// but looks much better on high frame rates this way.
			cameraLookVector =
				cameraLookVector * 0.9f +
				wannaCameraLookVector * 0.1f;

			// Update camera pos based on the current lookPos and cameraDistance
			cameraPos = LookAtPos + cameraLookVector;

			// Build look at matrix
			rotMatrix = Matrix.CreateLookAt(cameraPos, LookAtPos, carUp);
		}

		/// <summary>
		/// Update game logic, called every frame.
		/// </summary>
		public void Update()
		{
			// Since there is no loading screen, we need to skip the first frame because
			// the loading will cause ElapsedTimeThisFrameInMilliseconds to be too high
			if (firstFrame)
			{
				firstFrame = false;
			}

			UpdatePlayer();
			UpdateViewMatrix();
		}
	}
}
