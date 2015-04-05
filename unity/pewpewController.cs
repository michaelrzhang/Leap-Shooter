using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using Leap;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;

namespace _Scripts
{
	[RequireComponent(typeof (Rigidbody))]
	[RequireComponent(typeof (CapsuleCollider))]
	public class pewpewController: HandController
	{
		public bool punchDelay = false;
		public bool shootDelay = false;
		public Stopwatch punchStart = new Stopwatch();
		public Stopwatch shootStart = new Stopwatch();
		public bool gunMode = true;
		
		public class MovementSettings
		{
			public float ForwardSpeed = 8.0f;   // Speed when walking forward
			public float BackwardSpeed = 4.0f;  // Speed when walking backwards
			public float StrafeSpeed = 4.0f;    // Speed when walking sideways
			public float RunMultiplier = 2.0f;   // Speed when sprinting
			public KeyCode RunKey = KeyCode.LeftShift;
			public float JumpForce = 30f;
			public AnimationCurve SlopeCurveModifier = new AnimationCurve(new Keyframe(-90.0f, 1.0f), new Keyframe(0.0f, 1.0f), new Keyframe(90.0f, 0.0f));
			[HideInInspector] public float CurrentTargetSpeed = 8f;

			private bool m_Running;
			
			public void UpdateDesiredTargetSpeed(Vector2 input)
			{
				if (input == Vector2.zero) return;
				if (input.x > 0 || input.x < 0)
				{
					//strafe
					CurrentTargetSpeed = StrafeSpeed;
				}
				if (input.y < 0)
				{
					//backwards
					CurrentTargetSpeed = BackwardSpeed;
				}
				if (input.y > 0)
				{
					//forwards
					//handled last as if strafing and moving forward at the same time forwards speed should take precedence
					CurrentTargetSpeed = ForwardSpeed;
				}
				if (Input.GetKey(RunKey))
				{
					CurrentTargetSpeed *= RunMultiplier;
					m_Running = true;
				}
				else
				{
					m_Running = false;
				}
			}

			public bool Running
			{
				get { return m_Running; }
			}
		}
		
		
		[Serializable]
		public class AdvancedSettings
		{
			public float groundCheckDistance = 0.01f; // distance for checking if the controller is grounded ( 0.01f seems to work best for this )
			public float stickToGroundHelperDistance = 0.5f; // stops the character
			public float slowDownRate = 20f; // rate at which the controller comes to a stop when there is no input
			public bool airControl; // can the user control the direction that is being moved in the air
		}
		
		
		public Camera cam;
		public MovementSettings movementSettings = new MovementSettings();
		public AdvancedSettings advancedSettings = new AdvancedSettings();
		
		
		private Rigidbody m_RigidBody;
		private CapsuleCollider m_Capsule;
		private float m_YRotation;
		private Vector3 m_GroundContactNormal;
		private bool m_Jump, m_PreviouslyGrounded, m_Jumping, m_IsGrounded;
		
		
		public Vector3 Velocity
		{
			get { return m_RigidBody.velocity; }
		}
		
		public bool Grounded
		{
			get { return m_IsGrounded; }
		}
		
		public bool Jumping
		{
			get { return m_Jumping; }
		}
		
		public bool Running
		{
			get
			{
				return movementSettings.Running;
			}
		}
		private bool flaginitialized_ = false;
		private bool showhands_ = true;
		private long prevgraphics_id_ = 0;
		private long prevphysics_id_ = 0;
		
		void OnDrawGizmos() {
			// Draws the little Leap Motion Controller in the Editor view.
			Gizmos.matrix = Matrix4x4.Scale(GIZMO_SCALE * Vector3.one);
			Gizmos.DrawIcon(transform.position, "leap_motion.png");
		}
		
		void InitializeFlags()
		{
			// Optimize for top-down tracking if on head mounted display.
			Controller.PolicyFlag policy_flags = leap_controller_.PolicyFlags;
			if (isHeadMounted)
				policy_flags |= Controller.PolicyFlag.POLICY_OPTIMIZE_HMD;
			else
				policy_flags &= ~Controller.PolicyFlag.POLICY_OPTIMIZE_HMD;
			
			leap_controller_.SetPolicyFlags(policy_flags);
		}
		
		void Awake() {
			leap_controller_ = new Controller();
		}
		
		private void Start()
		{
			m_RigidBody = GetComponent<Rigidbody>();
			m_Capsule = GetComponent<CapsuleCollider>();
			// Initialize hand lookup tables.
			hand_graphics_ = new Dictionary<int, HandModel>();
			hand_physics_ = new Dictionary<int, HandModel>();
			
			tools_ = new Dictionary<int, ToolModel>();
			
//			if (leap_controller_ == null) {
//				Debug.LogWarning(
//					"Cannot connect to controller. Make sure you have Leap Motion v2.0+ installed");
//			}
			
			if (enableRecordPlayback && recordingAsset != null)
				recorder_.Load(recordingAsset);
		}
		
		
		private void Update()
		{
			if (CrossPlatformInputManager.GetButtonDown("Jump") && !m_Jump)
			{
				m_Jump = true;
			}
			if (leap_controller_ == null)
				return;
			
			UpdateRecorder();
			Frame frame = GetFrame();
			
			if (frame != null && !flaginitialized_)
			{
				InitializeFlags();
			}
			
			if (Input.GetKeyDown(KeyCode.H))
			{
				showhands_ = !showhands_;
			}
			
			if (showhands_)
			{
				if (frame.Id != prevgraphics_id_)
				{
					UpdateActions(frame);
					RotateView(frame);
					UpdateHandModels(hand_graphics_, frame.Hands, leftGraphicsModel, rightGraphicsModel);
					prevgraphics_id_ = frame.Id;
				}
			}
			else
			{
				// Destroy all hands with defunct IDs.
				List<int> hands = new List<int>(hand_graphics_.Keys);
				for (int i = 0; i < hands.Count; ++i)
				{
					DestroyHand(hand_graphics_[hands[i]]);
					hand_graphics_.Remove(hands[i]);
				}
			}
		}
		
		
		private void FixedUpdate()
		{
			GroundCheck();
			Vector2 input = GetInput();
			
			if ((Mathf.Abs(input.x) > float.Epsilon || Mathf.Abs(input.y) > float.Epsilon) && (advancedSettings.airControl || m_IsGrounded))
			{
				// always move along the camera forward as it is the direction that it being aimed at
				Vector3 desiredMove = cam.transform.forward*input.y + cam.transform.right*input.x;
				desiredMove = Vector3.ProjectOnPlane(desiredMove, m_GroundContactNormal).normalized;
				
				desiredMove.x = desiredMove.x*movementSettings.CurrentTargetSpeed;
				desiredMove.z = desiredMove.z*movementSettings.CurrentTargetSpeed;
				desiredMove.y = desiredMove.y*movementSettings.CurrentTargetSpeed;
				if (m_RigidBody.velocity.sqrMagnitude <
				    (movementSettings.CurrentTargetSpeed*movementSettings.CurrentTargetSpeed))
				{
					m_RigidBody.AddForce(desiredMove*SlopeMultiplier(), ForceMode.Impulse);
				}
			}
			
			if (m_IsGrounded)
			{
				m_RigidBody.drag = 5f;
				
				if (m_Jump)
				{
					m_RigidBody.drag = 0f;
					m_RigidBody.velocity = new Vector3(m_RigidBody.velocity.x, 0f, m_RigidBody.velocity.z);
					m_RigidBody.AddForce(new Vector3(0f, movementSettings.JumpForce, 0f), ForceMode.Impulse);
					m_Jumping = true;
				}
				
				if (!m_Jumping && Mathf.Abs(input.x) < float.Epsilon && Mathf.Abs(input.y) < float.Epsilon && m_RigidBody.velocity.magnitude < 1f)
				{
					m_RigidBody.Sleep();
				}
			}
			else
			{
				m_RigidBody.drag = 0f;
				if (m_PreviouslyGrounded && !m_Jumping)
				{
					StickToGroundHelper();
				}
			}
			m_Jump = false;
			if (leap_controller_ == null)
				return;
			
			Frame frame = GetFrame();
			
			if (frame.Id != prevphysics_id_)
			{
				UpdateActions(frame);
				UpdateHandModels(hand_physics_, frame.Hands, leftPhysicsModel, rightPhysicsModel);
				UpdateToolModels(tools_, frame.Tools, toolModel);
				prevphysics_id_ = frame.Id;
			}
		}
		
		
		private float SlopeMultiplier()
		{
			float angle = Vector3.Angle(m_GroundContactNormal, Vector3.up);
			return movementSettings.SlopeCurveModifier.Evaluate(angle);
		}
		
		
		private void StickToGroundHelper()
		{
			RaycastHit hitInfo;
			if (Physics.SphereCast(transform.position, m_Capsule.radius, Vector3.down, out hitInfo,
			                       ((m_Capsule.height/2f) - m_Capsule.radius) +
			                       advancedSettings.stickToGroundHelperDistance))
			{
				if (Mathf.Abs(Vector3.Angle(hitInfo.normal, Vector3.up)) < 85f)
				{
					m_RigidBody.velocity = Vector3.ProjectOnPlane(m_RigidBody.velocity, hitInfo.normal);
				}
			}
		}
		
		
		private Vector2 GetInput()
		{
			
			Vector2 input = new Vector2
			{
				x = CrossPlatformInputManager.GetAxis("Horizontal"),
				y = CrossPlatformInputManager.GetAxis("Vertical")
			};
			movementSettings.UpdateDesiredTargetSpeed(input);
			return input;
		}
		
		private void RotateView(Frame frame)
		{
			//avoids the mouse looking if the game is effectively paused
			if (Mathf.Abs(Time.timeScale) < float.Epsilon) return;
			
			// get the rotation before it's changed
			float oldYRotation = transform.eulerAngles.y;

			
			if (m_IsGrounded || advancedSettings.airControl)
			{
				// Rotate the rigidbody velocity to match the new direction that the character is looking
				Quaternion velRotation = Quaternion.AngleAxis(transform.eulerAngles.y - oldYRotation, Vector3.up);
				m_RigidBody.velocity = velRotation*m_RigidBody.velocity;
			}
		}
		
		
		/// sphere cast down just beyond the bottom of the capsule to see if the capsule is colliding round the bottom
		private void GroundCheck()
		{
			m_PreviouslyGrounded = m_IsGrounded;
			RaycastHit hitInfo;
			if (Physics.SphereCast(transform.position, m_Capsule.radius, Vector3.down, out hitInfo,
			                       ((m_Capsule.height/2f) - m_Capsule.radius) + advancedSettings.groundCheckDistance))
			{
				m_IsGrounded = true;
				m_GroundContactNormal = hitInfo.normal;
			}
			else
			{
				m_IsGrounded = false;
				m_GroundContactNormal = Vector3.up;
			}
			if (!m_PreviouslyGrounded && m_IsGrounded && m_Jumping)
			{
				m_Jumping = false;
			}
		}

		public void UpdateActions(Frame frame) {
			punch(frame);
			shoot(frame);
			toss(frame);
		}
		
		public void punch(Frame frame) {
			HandList hands = frame.Hands;
			int num_hands = hands.Count;
			for (int h = 0; h < num_hands; ++h) {
				Hand hand = hands[h];
				double zVelocity = hand.PalmVelocity.z;
				if (hand.GrabStrength > 0.8 && zVelocity < -500) {
					// Punch something
					if (punchDelay == true && punchStart.ElapsedMilliseconds > 0.0005) {
						punchDelay = !punchDelay;
					}
					if (!punchDelay) {
						punchStart.Reset();
						punchDelay = true;
					}	
				}
			}	
		}

		public void shoot(Frame frame) {
			HandList hands = frame.Hands;
			int num_hands = hands.Count;
			for (int h = 0; h < num_hands; ++h) {
				Hand hand = hands[h];
				double xVelocity = hand.PalmVelocity.x;
				if (hand.IsRight) {
					Vector thumb = null;
					Vector index = null;
					Vector middle = null;
					FingerList fingers = hand.Fingers;
					for (int i = 0; i < 5; ++i) {
						if (i == 0) {
							thumb = fingers[i].Direction;
						}
						if (i == 1) {
							index = fingers[i].Direction;
						}
						if (i == 2) {
							middle = fingers[i].Direction;
						}
					}
					if (parseRightAngle(thumb, index, middle) && xVelocity < -500) {
						gunMode = true;
						if (shootDelay == true && shootStart.ElapsedMilliseconds > 0.0005) {
							shootDelay = !shootDelay;
						}
						if (!shootDelay) {
							// Shoot something
							shootStart.Reset();
							shootDelay = true;
						}	
					}
				}
			}
		}
		
		public void toss(Frame frame) {
			GestureList gestures = frame.Gestures();
			for (int i = 0; i < gestures.Count; ++i) {
				Gesture gesture = gestures[i];
				if (gesture.ToString().Equals("TYPE_SWIPE")) {
					SwipeGesture swipe = new SwipeGesture(gesture);
					if (!gunMode) {
						Vector unit = swipe.Direction;
						float grenadeSpeed = swipe.Speed;
						// Throw a grenade in the direction of (unit.x * grenadeSpeed, unit.y * grenadeSpeed, unit.z * grenadeSpeed)
					} else {
						if (swipe.Direction.Dot(new Vector(0, -1, 0)) > 0.8) {
							gunMode = false;
						}
					}
				}
			}
		}
		
		public bool parseRightAngle (Vector thumb, Vector index, Vector middle) {
			return (Mathf.Abs((Mathf.Acos(thumb.Dot(index)) - Mathf.PI/2)) < 0.70) && 
				(Mathf.Abs((Mathf.Acos(thumb.Dot(middle)) - Mathf.PI/2)) < 0.70) &&
					index.Dot(middle) > 0.90;
		}
	}
}
