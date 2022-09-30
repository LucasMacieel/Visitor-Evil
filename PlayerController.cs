using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// TODO
// Smooth transition armed disarmed (it's taking a frame to mimic moves)
// Grab only the closest object to your character
// Create a dictionary class to store all valuable functions

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour {

	private CharacterController controller;
	private Transform head, headless, legs;
	private Camera cam;
	private Animator headlessAnim, legsAnim;
	private float stepOffset = 0.3f, slopeLimit = 45, animationAngle = 90;
	private ArmsController arms;

	public string state = "upright idle";

	[Header("Camera")]
	[Range(1,100)]public int sensitivity = 20;
	[Range(60,90)]public float clampAngle = 80;
	[Range(0,0.5f)]public float bodyOffset = 0.25f, armOffset = 0.1f;
	[Range(30.0f,80.0f)]public float aimFOV = 40, walkFOV = 60, runFOV = 70;
	[Range(1,3)]public float grabDistance = 2.0f;
	public bool aiming;
	public bool nearWall = false;

	[Header("Movement")]
	[Range(0,10)]public float walkSpeed = 2;
	[Range(0,10)]public float runSpeed = 5;
	private float moveEfficiency = 1;
	private float walkNormalizer = 0, runNormalizer = 0;
	private Vector3 moveDirection;

	[Header("Jumping")]
	[Range(1,5)]public int maxJumps = 2;
	[Range(0.5f,2)]public float jumpHeight = 1;
	private int currentJumps = 0;
	private bool jumping = false, wasGrounded = true;
	private float onAirTime = 0, gravity = 9.81f;

	[Header("Crouching")]
	[Range(1.5f,2.0f)]public float uprightHeight = 1.7f;
	[Range(0.5f,1.5f)]public float crouchHeight = 1.0f;
	public bool crouching = false;

	public List<Weapon> weapons;

	private bool lockMouse = true;
	private bool firstFrame = true;
	private int scrollDir = 0;
	private bool wasAiming = false;
	private int lastWeaponCount;

	public Text ammoText;

	void Start () {
		controller = GetComponent<CharacterController> ();
		stepOffset = controller.stepOffset;
		slopeLimit = controller.slopeLimit;
		cam = GetComponentInChildren<Camera> ();
		head = cam.transform;

		headless = transform.FindChild ("Headless");
		headlessAnim = headless.GetComponent <Animator> ();
		headlessAnim.SetFloat ("Posture", 0.5f);
		legs = transform.FindChild ("Legs");
		legsAnim = legs.GetComponent <Animator> ();
		legsAnim.SetFloat ("Posture", 0.5f);

		arms = head.GetComponentInChildren<Animator> ().transform.GetComponent<ArmsController> ();
		arms.weapon = (weapons.Count > 0) ? (weapons[0]) : (null);
		hideStockedWeapons ();
		adjustHeight ();
		head.localEulerAngles = Vector3.zero;
		controller.height = uprightHeight;
	}

	void Update () {
		chooseAnimator ();
		checkLockMouse ();
		looking ();
		Movement ();
		Jump ();
		crouch ();
		Animate ();
		adjustHeight ();
		checkCloseObjects ();
		switchWeapons ();
		updateUI ();
	}

	void LateUpdate () {
		correctAnimators ();
	}

	void checkLockMouse () {
		bool esc = Input.GetKeyDown (KeyCode.Escape);
		bool mouse0 = Input.GetKeyDown (KeyCode.Mouse0);
		bool mouse1 = Input.GetKeyDown (KeyCode.Mouse1);

		if (esc) {
			lockMouse = !lockMouse;
		} else if (mouse0 || mouse1) {
			lockMouse = true;
		}

		Cursor.visible = !lockMouse;
		Cursor.lockState = (lockMouse) ? (CursorLockMode.Locked) : (CursorLockMode.None);
	}

	void adjustHeight () {
		Transform body = (weapons.Count > 0) ? (legs) : (headless);
		Animator bodyAnim = (weapons.Count > 0) ? (legsAnim) : (headlessAnim);

		body.localPosition = Vector3.zero;
		controller.height = Mathf.Lerp(controller.height, (crouching) ? (crouchHeight) : (uprightHeight), 3 * Time.deltaTime);
		controller.center = new Vector3 (0, controller.height * 0.5f, 0);

		Transform neckBone = bodyAnim.GetBoneTransform(HumanBodyBones.Neck);
		Transform neckParent = neckBone.parent;
		neckBone.SetParent (transform);
		Vector3 neckOffset = neckBone.localPosition;
		neckBone.SetParent (neckParent);
		body.localPosition = new Vector3 (-neckOffset.x, -controller.skinWidth, -neckOffset.z - bodyOffset);
	}
	// Animation
	void Animate () {
		Animator bodyAnim = (weapons.Count > 0) ? (legsAnim) : (headlessAnim);
		float posture = bodyAnim.GetFloat ("Posture"), speed = bodyAnim.GetFloat ("Speed"), multiplier = 5 * Time.deltaTime;
		bodyAnim.SetFloat ("Posture", Mathf.Lerp (posture, (state.Contains ("crouch")) ? (1) : ((state == "on air")?(0):(0.5f)), multiplier));
		if (state.Contains ("crouch")) {bodyAnim.SetFloat ("Speed", Mathf.Lerp (speed, (state.Contains ("idle")) ? (0) : (1), multiplier));}
		else {bodyAnim.SetFloat ("Speed", Mathf.Lerp (speed, (state.Contains ("idle")) ? (0) : ((state.Contains ("run"))?(1):(0.5f)), multiplier));}
	}

	// Converts [0,360] angle to [-180,180]
	float angle_0_180(float angle){
		angle = angle % 360;
		if (angle <= -180) {angle += 360;}
		else if (angle >= 180) {angle -= 360;}
		return angle;
	}

	float angle_0_360(float angle){
		angle = angle % 360;
		if (angle < 0) {angle += 360;}
		return angle;
	}

	void looking () {
		// Keeps the camera on its axis
		head.localPosition = new Vector3 (0, controller.height - 0.15f - controller.skinWidth, 0);
		// Mouse input
		Vector2 mouseInput = new Vector2 (Input.GetAxis ("Mouse X"), Input.GetAxis ("Mouse Y")) * 5 * sensitivity * Time.deltaTime;
		transform.eulerAngles += new Vector3 (0, mouseInput.x, 0);
		head.localEulerAngles = new Vector3 (Mathf.Clamp (angle_0_180(head.localEulerAngles.x) - mouseInput.y, -clampAngle, clampAngle), 0, 0);
		// Adjusting FOV
		float newFOV = (aiming) ? (aimFOV) : ((state.Contains("run")) ? (runFOV) : (walkFOV));
		cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, newFOV, 5 * Time.deltaTime);
	}

	void Movement () {
		bool w = Input.GetKey (KeyCode.W) || Input.GetKey (KeyCode.UpArrow);
		bool a = Input.GetKey (KeyCode.A) || Input.GetKey (KeyCode.LeftArrow);
		bool s = Input.GetKey (KeyCode.S) || Input.GetKey (KeyCode.DownArrow);
		bool d = Input.GetKey (KeyCode.D) || Input.GetKey (KeyCode.RightArrow);
		bool shift = Input.GetKey (KeyCode.LeftShift) || Input.GetKey (KeyCode.RightShift);
		bool mouse1 = Input.GetKeyDown (KeyCode.Mouse1);

		Vector2 moveInput = Vector2.zero;
		if (w) {moveInput.y += 1;}
		if (s) {moveInput.y -= 1;}
		if (a) {moveInput.x -= 1;}
		if (d) {moveInput.x += 1;}

		float theta = angle_0_360 (Mathf.Atan2 (moveInput.y, moveInput.x) * Mathf.Rad2Deg);
		//float move

		// Moving
		if (moveInput.magnitude != 0) {
			// The player wants to walk
			moveDirection = new Vector3(moveInput.x, 0, moveInput.y);
			moveDirection = transform.forward * moveDirection.z + transform.right * moveDirection.x;
			moveDirection.Normalize ();
			walkNormalizer = Mathf.Clamp (walkNormalizer + 4 * Time.deltaTime, 0, 1);
			// Running normalizer
			if (shift && (!crouching || canUncrouch ())) {
				runNormalizer = Mathf.Clamp (runNormalizer + 4 * Time.deltaTime, 0, 1);
				state = "upright run";
				crouching = false;
			} else {
				runNormalizer = Mathf.Clamp (runNormalizer - 4 * Time.deltaTime, 0, 1);
				state = (crouching) ? ("crouch walk") : ("upright walk");
			}
		} else {
			// The player is idle
			walkNormalizer = Mathf.Clamp (walkNormalizer - 4 * Time.deltaTime, 0, 1);
			runNormalizer = Mathf.Clamp (runNormalizer - 4 * Time.deltaTime, 0, 1);
			state = (crouching) ? ("crouch idle") : ("upright idle");
		}
		if (walkNormalizer > 0) {
			Vector2 lastPosxz = new Vector2(transform.position.x, transform.position.z);
			float lasty = transform.position.y;
			float multiplier = ((walkSpeed * walkNormalizer) + ((runSpeed - walkSpeed) * runNormalizer)) * Time.deltaTime;
			controller.Move (moveDirection * multiplier);
			// If the efficiency is low then it is stuck, return to original position and remove animations
			moveEfficiency = Vector3.Distance (new Vector2(transform.position.x, transform.position.z), lastPosxz) / multiplier;
			if (moveEfficiency <= 0.1f) {
				transform.position = new Vector3 (lastPosxz.x, lasty, lastPosxz.y);
				state = (crouching) ? ("crouch idle") : ("upright idle");
			}
		}
		// Animator angle
		Animator bodyAnim = (weapons.Count > 0) ? (legsAnim) : (headlessAnim);
		if (bodyAnim.GetFloat ("Speed") == 0)
			animationAngle = theta;
		else if (!state.Contains ("idle"))
			animationAngle = Mathf.LerpAngle (animationAngle, theta, 5 * Time.deltaTime);
		bodyAnim.SetFloat ("Angle", angle_0_360 (animationAngle));

		// Aiming
		if (!aiming && wasAiming) {
			aiming = true;
			wasAiming = false;
		}
		if (onAirTime > 0 || state == "upright run") {
			wasAiming = false;
			aiming = false;
		} else if (weapons.Count == 0 || weapons [0].weaponType != Weapon.weaponTypeEnum.Gun || (!weapons [0].state.Contains ("Shot") && !weapons [0].state.Contains ("Idle"))) {
			if (weapons.Count > 0 && weapons [0].state.Contains ("Reload") && aiming)
				wasAiming = true;
			if (mouse1)
				wasAiming = false;
			aiming = false;
		} else if (mouse1) {
			aiming = !aiming;
		}
	}

	bool isGrounded () {
		if (controller.isGrounded) {
			return true;
		} else {
			float randAngle = UnityEngine.Random.Range (0, 45);
			for (float d = 0.5f; d <= 1; d += 0.5f) {
				for (int theta = 0; theta < 360; theta += 45){
					Vector3 rayLocation = transform.position + new Vector3 (Mathf.Cos ((theta + randAngle) * Mathf.Deg2Rad), 0, Mathf.Sin ((theta + randAngle) * Mathf.Deg2Rad)) * d * controller.radius;
					RaycastHit[] hits = Physics.RaycastAll (rayLocation, Vector3.down, controller.skinWidth + 0.05f + controller.stepOffset);
					for (int i = 0; i < hits.Length; i++) {if (hits [i].transform.root != transform) {return true;}}
				}
			}
			return false;
		}
	}

	// Checks if there is a distance smaller than the height above head
	bool canUncrouch () {
		float randAngle = UnityEngine.Random.Range (0, 45);
		for (float d = 0.5f; d <= 1; d += 0.5f) {
			for (int theta = 0; theta < 360; theta += 45){
				float newAngle = theta + randAngle;
				Vector3 rayOffset = new Vector3 (Mathf.Cos (newAngle * Mathf.Deg2Rad), 0, Mathf.Sin (newAngle * Mathf.Deg2Rad)) * d * controller.radius;
				Vector3 rayLocation = transform.position + (Vector3.up * crouchHeight) + rayOffset;
				RaycastHit[] hits = Physics.RaycastAll (rayLocation, Vector3.up, uprightHeight - crouchHeight);
				for (int i = 0; i < hits.Length; i++) {
					if (hits [i].transform.root != transform && hits [i].distance < uprightHeight - crouchHeight) {
						return false;
					}
				}
			}
		}
		return true;
	}

	void Jump () {
		if (isGrounded () && !jumping) {
			currentJumps = 0;
			onAirTime = 0;
			controller.Move (new Vector3(0, -gravity * Time.deltaTime, 0));
			wasGrounded = true;
			controller.stepOffset = stepOffset;
			controller.slopeLimit = slopeLimit;
		} else {
			if (wasGrounded && !jumping) {currentJumps++;}
			onAirTime += Time.deltaTime;
			float v0 = (jumping) ? (Mathf.Sqrt (2 * jumpHeight * gravity)) : (0);
			float v = v0 - gravity * onAirTime;
			controller.Move (new Vector3(0, v * Time.deltaTime, 0));
			wasGrounded = false;
			controller.stepOffset = controller.slopeLimit = 0;
			if (onAirTime >= v0 / gravity && isGrounded ())
				jumping = false;
		}
		if (onAirTime > 0)
			state = "on air";
		bool j = Input.GetKeyDown (KeyCode.Space);
		if (j && currentJumps < maxJumps && ((crouching && canUncrouch()) || !crouching)) {
			jumping = true;
			onAirTime = 0;
			currentJumps++;
		}
	}

	void crouch () {
		if (state == "on air" || state == "upright run") {
			if (canUncrouch ()) {crouching = false;}
		} else if (Input.GetKeyDown (KeyCode.C)) {
			if (!crouching) {crouching = true;}
			else if (canUncrouch ()) {crouching = false;}
		}
	}

	void checkCloseObjects () {
		nearWall = false;
		bool e = Input.GetKeyDown (KeyCode.E);
		bool x = Input.GetKeyDown (KeyCode.X);
		bool collected = false;
		float closeDistance = controller.radius * 1.5f;
		RaycastHit[] hits = Physics.RaycastAll (head.position, head.forward, grabDistance);
		for (int i = 0; i < hits.Length; i++) {
			if (hits [i].transform.root != transform) {
				if (hits [i].distance < closeDistance)
					nearWall = true;
				if (Vector3.Distance (head.position, hits [i].transform.root.position) < grabDistance && e && !collected) {
					Weapon weapon = hits [i].transform.root.GetComponent <Weapon> ();
					if (weapon) {
						weapons.Add (weapon);
						collected = true;
					}
				}
			}
		}
		if (x && weapons.Count > 0 && !weapons [0].state.Contains ("Shot")) {
			Transform w = weapons [0].transform;
			w.SetParent (null);
			w.position = transform.position;
			weapons.RemoveAt (0);
			if (weapons.Count == 1)
				arms.gameObject.SetActive (false);
		}
	}

	void chooseAnimator () {
		if (weapons.Count > 0) {
			if (!legs.gameObject.activeSelf) {
				legs.gameObject.SetActive (true);
				legsAnim.SetFloat ("Posture", headlessAnim.GetFloat ("Posture"));
				legsAnim.SetFloat ("Speed", headlessAnim.GetFloat ("Speed"));
				legsAnim.SetFloat ("Angle", headlessAnim.GetFloat ("Angle"));
			}
			if (headless.gameObject.activeSelf) 
				headless.gameObject.SetActive (false);
			if (!arms.gameObject.activeSelf)
				arms.gameObject.SetActive (true);
		} else {
			if (!headless.gameObject.activeSelf) {
				headless.gameObject.SetActive (true);
				headlessAnim.SetFloat ("Posture", legsAnim.GetFloat ("Posture"));
				headlessAnim.SetFloat ("Speed", legsAnim.GetFloat ("Speed"));
				headlessAnim.SetFloat ("Angle", legsAnim.GetFloat ("Angle"));
			}
			if (legs.gameObject.activeSelf) 
				legs.gameObject.SetActive (false);
			if (arms.gameObject.activeSelf)
				arms.gameObject.SetActive (false);
		}
		if (firstFrame) {
			arms.gameObject.SetActive (true);
			legs.gameObject.SetActive (true);
			headless.gameObject.SetActive (true);
		}
	}

	void correctAnimators () {
		if (firstFrame) {
			if (weapons.Count > 0) {
				headlessAnim.SetFloat ("Posture", legsAnim.GetFloat ("Posture"));
				headlessAnim.SetFloat ("Speed", legsAnim.GetFloat ("Speed"));
				headlessAnim.SetFloat ("Angle", legsAnim.GetFloat ("Angle"));
			} else {
				legsAnim.SetFloat ("Posture", headlessAnim.GetFloat ("Posture"));
				legsAnim.SetFloat ("Speed", headlessAnim.GetFloat ("Speed"));
				legsAnim.SetFloat ("Angle", headlessAnim.GetFloat ("Angle"));
			}
			firstFrame = false;
		}
	}

	void switchWeapons () {
		if (lastWeaponCount < weapons.Count) {
			hideStockedWeapons ();
			scrollDir = -1;
			wasAiming = false;
			if (weapons.Count > 1)
				weapons [0].stocking = true;
		} else if (lastWeaponCount > weapons.Count) {
			hideStockedWeapons ();
			wasAiming = false;
		}
		if (weapons.Count > 0) {
			if (weapons [0].gameObject.activeSelf && !weapons [0].stocking && (weapons[0].state.Contains ("Idle") || weapons[0].state.Contains ("Reload"))) {
				if (Input.mouseScrollDelta.y >= 1)
					scrollDir = 1;
				else if (Input.mouseScrollDelta.y <= -1)
					scrollDir = -1;
				else
					scrollDir = 0;
				if (weapons.Count == 1 && Mathf.Abs(Input.mouseScrollDelta.y) >= 1)
					scrollDir = 0;
				if (scrollDir != 0) {
					wasAiming = false;
					weapons [0].stocking = true;
				}
			} else if (!weapons [0].gameObject.activeSelf) {
				if (scrollDir > 0) {
					Weapon w = weapons [0];
					weapons.RemoveAt (0);
					weapons.Add (w);
				} else if (scrollDir < 0) {
					Weapon w = weapons [weapons.Count - 1];
					weapons.RemoveAt (weapons.Count - 1);
					List<Weapon> weapons2 = new List<Weapon> ();
					weapons2.Add (w);
					weapons2.AddRange (weapons);
					weapons = weapons2;
				}
				hideStockedWeapons ();
				arms.weapon = weapons [0];
				scrollDir = 0;
			}
		}
		lastWeaponCount = weapons.Count;
	}

	void hideStockedWeapons () {
		if (weapons.Count > 0) {
			weapons [0].gameObject.SetActive (true);
			weapons [0].correctOrientation ();
			for (int x = 0; x < weapons.Count; x++) {
				weapons [x].transform.SetParent (head);
				weapons [x].transform.localPosition = weapons [x].idle.position;
				weapons [x].transform.localEulerAngles = weapons [x].idle.eulerAngles;
			}
			for (int x = 1; x < weapons.Count; x++)
				weapons [x].gameObject.SetActive (false);
		}
		arms.weapon = (weapons.Count > 0) ? (weapons[0]) : (null);
	}

	void updateUI () {
		if (ammoText) {
			if (weapons.Count > 0) {
				Weapon w = weapons [0];
				ammoText.text = w.name + "\n" + w.currentMagazine.ToString () + " / " + w.currentExtra.ToString ();
			} else {
				ammoText.text = "";
			}
		}
	}
}
