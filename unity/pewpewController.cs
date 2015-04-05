using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using Leap;

{
	[RequireComponent(typeof (Rigidbody))]
	[RequireComponent(typeof (CapsuleCollider))]
	public class pewpewController : MonoBehaviour
	{
		public bool punchDelay = false;
		public bool shootDelay = false;
		public StopWatch punchStart = new StopWatch();
		public StopWatch shootStart = new StopWatch();
		public boolean gunMode = true;

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
			
			#if !MOBILE_INPUT
			private bool m_Running;
			#endif
			
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
				#if !MOBILE_INPUT
				if (Input.GetKey(RunKey))
				{
					CurrentTargetSpeed *= RunMultiplier;
					m_Running = true;
				}
				else
				{
					m_Running = false;
				}
				#endif
			}
			
			#if !MOBILE_INPUT
			public bool Running
			{
				get { return m_Running; }
			}
			#endif
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
				#if !MOBILE_INPUT
				return movementSettings.Running;
				#else
				return false;
				#endif
			}
		}

		
		// Reference distance from thumb base to pinky base in mm.
		protected const float GIZMO_SCALE = 5.0f;
		protected const float MM_TO_M = 0.001f;
		
		public bool separateLeftRight = false;
		public HandModel leftGraphicsModel;
		public HandModel leftPhysicsModel;
		public HandModel rightGraphicsModel;
		public HandModel rightPhysicsModel;
		
		public ToolModel toolModel;
		
		public bool isHeadMounted = false;
		public bool mirrorZAxis = false;
		
		// If hands are in charge of Destroying themselves, make this false.
		public bool destroyHands = true;
		
		public Vector3 handMovementScale = Vector3.one;
		
		// Recording parameters.
		public bool enableRecordPlayback = false;
		public TextAsset recordingAsset;
		public float recorderSpeed = 1.0f;
		public bool recorderLoop = true;
		
		protected LeapRecorder recorder_ = new LeapRecorder();
		
		protected Controller leap_controller_;
		
		protected Dictionary<int, HandModel> hand_graphics_;
		protected Dictionary<int, HandModel> hand_physics_;
		protected Dictionary<int, ToolModel> tools_;
		
		private bool flag_initialized_ = false;
		private bool show_hands_ = true;
		private long prev_graphics_id_ = 0;
		private long prev_physics_id_ = 0;
		
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
			
			if (leap_controller_ == null) {
				Debug.LogWarning(
					"Cannot connect to controller. Make sure you have Leap Motion v2.0+ installed");
			}
			
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
			
			if (frame != null && !flag_initialized_)
			{
				InitializeFlags();
			}
			
			if (Input.GetKeyDown(KeyCode.H))
			{
				show_hands_ = !show_hands_;
			}
			
			if (show_hands_)
			{
				if (frame.Id != prev_graphics_id_)
				{
					UpdateAction(frame);
					RotateView(frame);
					UpdateHandModels(hand_graphics_, frame.Hands, leftGraphicsModel, rightGraphicsModel);
					prev_graphics_id_ = frame.Id;
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
			
			if (frame.Id != prev_physics_id_)
			{
				UpdateActions(frame);
				UpdateHandModels(hand_physics_, frame.Hands, leftPhysicsModel, rightPhysicsModel);
				UpdateToolModels(tools_, frame.Tools, toolModel);
				prev_physics_id_ = frame.Id;
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

		protected HandModel CreateHand(HandModel model) {
			HandModel hand_model = Instantiate(model, transform.position, transform.rotation)
				as HandModel;
			hand_model.gameObject.SetActive(true);
			Leap.Utils.IgnoreCollisions(hand_model.gameObject, gameObject);
			return hand_model;
		}
		
		protected void DestroyHand(HandModel hand_model) {
			if (destroyHands)
				Destroy(hand_model.gameObject);
			else
				hand_model.SetLeapHand(null);
		}
		
		protected void UpdateHandModels(Dictionary<int, HandModel> all_hands,
		                                HandList leap_hands,
		                                HandModel left_model, HandModel right_model) {
			List<int> ids_to_check = new List<int>(all_hands.Keys);
			
			// Go through all the active hands and update them.
			int num_hands = leap_hands.Count;
			for (int h = 0; h < num_hands; ++h) {
				Hand leap_hand = leap_hands[h];
				
				HandModel model = (mirrorZAxis != leap_hand.IsLeft) ? left_model : right_model;
				
				// If we've mirrored since this hand was updated, destroy it.
				if (all_hands.ContainsKey(leap_hand.Id) &&
				    all_hands[leap_hand.Id].IsMirrored() != mirrorZAxis) {
					DestroyHand(all_hands[leap_hand.Id]);
					all_hands.Remove(leap_hand.Id);
				}
				
				// Only create or update if the hand is enabled.
				if (model != null) {
					ids_to_check.Remove(leap_hand.Id);
					
					// Create the hand and initialized it if it doesn't exist yet.
					if (!all_hands.ContainsKey(leap_hand.Id)) {
						HandModel new_hand = CreateHand(model);
						new_hand.SetLeapHand(leap_hand);
						new_hand.MirrorZAxis(mirrorZAxis);
						new_hand.SetController(this);
						
						// Set scaling based on reference hand.
						float hand_scale = MM_TO_M * leap_hand.PalmWidth / new_hand.handModelPalmWidth;
						new_hand.transform.localScale = hand_scale * transform.lossyScale;
						
						new_hand.InitHand();
						new_hand.UpdateHand();
						all_hands[leap_hand.Id] = new_hand;
					}
					else {
						// Make sure we update the Leap Hand reference.
						HandModel hand_model = all_hands[leap_hand.Id];
						hand_model.SetLeapHand(leap_hand);
						hand_model.MirrorZAxis(mirrorZAxis);
						
						// Set scaling based on reference hand.
						float hand_scale = MM_TO_M * leap_hand.PalmWidth / hand_model.handModelPalmWidth;
						hand_model.transform.localScale = hand_scale * transform.lossyScale;
						hand_model.UpdateHand();
					}
				}
			}
			
			// Destroy all hands with defunct IDs.
			for (int i = 0; i < ids_to_check.Count; ++i) {
				DestroyHand(all_hands[ids_to_check[i]]);
				all_hands.Remove(ids_to_check[i]);
			}
		}
		
		protected ToolModel CreateTool(ToolModel model) {
			ToolModel tool_model = Instantiate(model, transform.position, transform.rotation) as ToolModel;
			tool_model.gameObject.SetActive(true);
			Leap.Utils.IgnoreCollisions(tool_model.gameObject, gameObject);
			return tool_model;
		}
		
			protected void UpdateToolModels(Dictionary<int, ToolModel> all_tools,
			                                ToolList leap_tools, ToolModel model) {
				List<int> ids_to_check = new List<int>(all_tools.Keys);
				
				// Go through all the active tools and update them.
				int num_tools = leap_tools.Count;
				for (int h = 0; h < num_tools; ++h) {
					Tool leap_tool = leap_tools[h];
					
					// Only create or update if the tool is enabled.
					if (model) {
						
						ids_to_check.Remove(leap_tool.Id);
						
						// Create the tool and initialized it if it doesn't exist yet.
						if (!all_tools.ContainsKey(leap_tool.Id)) {
							ToolModel new_tool = CreateTool(model);
							new_tool.SetController(this);
							new_tool.SetLeapTool(leap_tool);
							new_tool.InitTool();
							all_tools[leap_tool.Id] = new_tool;
						}
						
						// Make sure we update the Leap Tool reference.
						ToolModel tool_model = all_tools[leap_tool.Id];
						tool_model.SetLeapTool(leap_tool);
						tool_model.MirrorZAxis(mirrorZAxis);
						
						// Set scaling.
						tool_model.transform.localScale = transform.lossyScale;
						
						tool_model.UpdateTool();
					}
				}
				
				// Destroy all tools with defunct IDs.
				for (int i = 0; i < ids_to_check.Count; ++i) {
					Destroy(all_tools[ids_to_check[i]].gameObject);
					all_tools.Remove(ids_to_check[i]);
				}
			}
			
			public Controller GetLeapController() {
				return leap_controller_;
			}
			
			public Frame GetFrame() {
				if (enableRecordPlayback && recorder_.state == RecorderState.Playing)
					return recorder_.GetCurrentFrame();
				
				return leap_controller_.Frame();
			}
			
			private void RotateView()
			{
				//avoids the mouse looking if the game is effectively paused
				if (Mathf.Abs(Time.timeScale) < float.Epsilon) return;

				// get the rotation before it's changed
				float oldYRotation = transform.eulerAngles.y;

				mouseLook.LookRotation (transform, cam.transform);

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
		public bool IsConnected() {
			return leap_controller_.IsConnected;
		}
		
		public bool IsEmbedded() {
			DeviceList devices = leap_controller_.Devices;
			if (devices.Count == 0)
				return false;
			return devices[0].IsEmbedded;
		}
		
		public HandModel[] GetAllGraphicsHands() {
			if (hand_graphics_ == null)
				return new HandModel[0];
			
			HandModel[] models = new HandModel[hand_graphics_.Count];
			hand_graphics_.Values.CopyTo(models, 0);
			return models;
		}
		
		public HandModel[] GetAllPhysicsHands() {
			if (hand_physics_ == null)
				return new HandModel[0];
			
			HandModel[] models = new HandModel[hand_physics_.Count];
			hand_physics_.Values.CopyTo(models, 0);
			return models;
		}
		
		public void DestroyAllHands() {
			if (hand_graphics_ != null) {
				foreach (HandModel model in hand_graphics_.Values)
					Destroy(model.gameObject);
				
				hand_graphics_.Clear();
			}
			if (hand_physics_ != null) {
				foreach (HandModel model in hand_physics_.Values)
					Destroy(model.gameObject);
				
				hand_physics_.Clear();
			}
		}
		
		public float GetRecordingProgress() {
			return recorder_.GetProgress();
		}
		
		public void StopRecording() {
			recorder_.Stop();
		}
		
		public void PlayRecording() {
			recorder_.Play();
		}
		
		public void PauseRecording() {
			recorder_.Pause();
		}
		
		public string FinishAndSaveRecording() {
			string path = recorder_.SaveToNewFile();
			recorder_.Play();
			return path;
		}
		
		public void ResetRecording() {
			recorder_.Reset();
		}
		
		public void Record() {
			recorder_.Record();
		}
		
		protected void UpdateRecorder() {
			if (!enableRecordPlayback)
				return;
			
			recorder_.speed = recorderSpeed;
			recorder_.loop = recorderLoop;
			
			if (recorder_.state == RecorderState.Recording) {
				recorder_.AddFrame(leap_controller_.Frame());
			}
			else {
				recorder_.NextFrame();
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
				double zVelocity = hand.palm_velocity.z;
				if (hand.grabStrength > 0.8 && zVelocity < -500) {
					if (punchDelay == true && punchStart.ElapsedMilliseconds > 0.0005) {
						punchDelay = !punchDelay;
					}
					if (!punchDelay) {
						// Punch something
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
				double xVelocity = hand.palm_velocity.x;
				if (hand.isRight) {
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
