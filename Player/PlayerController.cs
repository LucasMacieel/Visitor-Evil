using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour {

	[HideInInspector]public Text weaponInfo, collectableInfo;
	[HideInInspector]public Image healthInfo;
	[HideInInspector]public WeaponSpreadUI spreadUI;

	public GameObject[] armParts;
	private CharacterController controller;
	private Transform head, body;
	private Animator anim;
	private Camera cam;
	private float stepOffset = 0.3f, slopeLimit = 45, animationAngle = 90;

	public string state = "upright idle";

	[Header("Camera")]
	[Range(1,100)]public int sensitivity = 20;
	[Range(60,90)]public float clampAngle = 80;
	[Range(0,0.5f)]public float bodyOffset = 0.3f, armOffset = 0.1f;
	[Range(30.0f,80.0f)]public float walkFOV = 60, runFOV = 70;
	[Range(1,3)]public float grabDistance = 2.0f;
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

	private float uprightHeight;
	private bool crouching;
	private bool lockMouse;

	[Header("Weapons")]
	public int currentWeapon = -1;
	private int scrollDir = 0;
	public GameObject[] weapons1;
	[Range(0,400)]public int pistolAmmo = 90, maxPistolAmmo = 120;
	[Range(0,400)]public int arrows = 0, maxArrows = 45;
	[Range(0,400)]public int rifleAmmo = 60, maxRifleAmmo = 90;
	[Range(0,400)]public int shotgunAmmo = 0, maxShotgunAmmo = 80;
	[Range(0.5f,2)]public float interactRange = 1f;

	[Header("Inventory")]
	public List<string> keys;
	[Range(0,10)]public int healers = 0, maxHealers = 5;

	[Header("Health")]
	[Range(0,100)]public int health = 100;
	[Range(1,100)]public int maxHealth = 100;
	public GameObject healerPrefab, deathPrefab;
	private GameObject healerInstance;
	private bool healing, interruptWeapons;

	public AudioClip[] hurt;
	[HideInInspector]public AudioSource audio;

	void Start () {
		audio = GetComponent <AudioSource> ();
		controller = GetComponent<CharacterController> ();
		stepOffset = controller.stepOffset;
		slopeLimit = controller.slopeLimit;
		cam = GetComponentInChildren<Camera> ();
		head = cam.transform;

		anim = GetComponentInChildren <Animator> ();
		body = anim.transform;

		anim.SetFloat ("Posture", 0.5f);

		adjustHeight ();
		head.localEulerAngles = Vector3.zero;

		uprightHeight = anim.GetBoneTransform (HumanBodyBones.Head).position.y - transform.position.y + 0.1f;

		// Mantém só a primeira arma visível
		bool foundWeapon = false;
		for (int x = 0; x < weapons1.Length; x++) {
			if (weapons1 [x]) {
				if (foundWeapon) {
					weapons1 [x].SetActive (false);
				} else {
					currentWeapon = x;
					weapons1 [x].SetActive (true);
					foundWeapon = true;
				}
			}
		}
	}

	bool canFitArray (int i, int len) {
		return i >= 0 && i < len;
	}

	public bool hasSomeWeapon () {
		for (int x = 0; x < weapons1.Length; x++)
			if (weapons1 [x]) {
				if (currentWeapon < 0)
					currentWeapon = x;
				return true;
			}
		return false;
	}

	void Update () {
		checkLockMouse ();
		looking ();
		Movement ();
		Jump ();
		crouch ();
		Animate ();
		adjustHeight ();
		weaponHandler ();
		HUD ();
		Interact ();
		CheckHealing ();
		CheckDeath ();

		if (transform.position.y <= -50)
			SceneManager.LoadScene("Menu");
	}

	void CheckDeath () {
		if (health <= 0){
			DeathLerper deathLerper = Instantiate(deathPrefab, transform.position, transform.rotation).GetComponent <DeathLerper> ();
			head.SetParent (null);
			deathLerper.head = head;
			bool fittable = canFitArray(currentWeapon, weapons1.Length) && weapons1 [currentWeapon];
			Gun g = (fittable) ? (weapons1 [currentWeapon].GetComponent<Gun> ()) : (null);
			Knife k = (fittable) ? (weapons1 [currentWeapon].GetComponent<Knife> ()) : (null);
			bool available = (g && g.available) || (k && k.available);
			if (available)
				if (g)
					g.hide = true;
				else if (k)
					k.hide = true;
			Destroy (gameObject);
		}
	}

	void CheckHealing () {
		if (healing && !healerInstance) {
			interruptWeapons = false;
			healing = false;
		} else if (!healing && Input.GetKeyDown (KeyCode.H) && healers > 0 && health < maxHealth) {
			healers--;
			healerInstance = Instantiate(healerPrefab, head.position, head.rotation, head);
			healing = true;
			interruptWeapons = true;
		}
	}

	bool isInteractable (GameObject o) {
		return o.GetComponent <Key> () || o.GetComponent <CollectableWeapon> () || o.GetComponent <DoorLocker> () || o.GetComponent <Ammo> () || o.GetComponent <CollectableHealer> ();
	}

	GameObject closestInteractable (RaycastHit[] hits) {
		float minDistance = Mathf.Infinity;
		Transform t = null;
		for (int x = 0; x < hits.Length; x++) {
			Transform other = hits [x].collider.transform;
			if (hits [x].distance < minDistance && !other.gameObject.GetComponent <NPC> () && (!hits [x].collider.isTrigger || isInteractable (other.gameObject))) {
				t = other;
				minDistance = hits [x].distance;
			}
		}
		return (t && isInteractable (t.root.gameObject)) ? (t.root.gameObject) : (null);
	}

	void Interact () {
		RaycastHit[] hits = Physics.RaycastAll (head.position, head.forward, interactRange);
		GameObject interactable = closestInteractable (hits);
		if (interactable) {
			Ammo ammo = interactable.GetComponent <Ammo> ();
			Key key = interactable.GetComponent <Key> ();
			CollectableHealer healer = interactable.GetComponent <CollectableHealer> ();
			CollectableWeapon weapon = interactable.GetComponent <CollectableWeapon> ();
			DoorLocker door = interactable.GetComponent <DoorLocker> ();
			if (Input.GetKeyDown (KeyCode.E)) {
				bool canDestroy = false;
				if (ammo) {
					switch (ammo.ammoType) {
					case Ammo.ammoTypeEnum.arrow:
						if (arrows < maxArrows) {
							canDestroy = true;
							arrows += ammo.amount;
						}
						break;
					case Ammo.ammoTypeEnum.pistol:
						if (pistolAmmo < maxPistolAmmo) {
							canDestroy = true;
							pistolAmmo += ammo.amount;
						}
						break;
					case Ammo.ammoTypeEnum.rifle:
						if (rifleAmmo < maxRifleAmmo) {
							canDestroy = true;
							rifleAmmo += ammo.amount;
						}
						break;
					case Ammo.ammoTypeEnum.shotgun:
						if (shotgunAmmo < maxShotgunAmmo) {
							canDestroy = true;
							shotgunAmmo += ammo.amount;
						}
						break;
					}
					if (arrows > maxArrows)
						arrows = maxArrows;
					if (pistolAmmo > maxPistolAmmo)
						pistolAmmo = maxPistolAmmo;
					if (rifleAmmo > maxRifleAmmo)
						rifleAmmo = maxRifleAmmo;
					if (shotgunAmmo > maxShotgunAmmo)
						shotgunAmmo = maxShotgunAmmo;
				} else if (key) {
					if (!keys.Contains (key.id))
						keys.Add (key.id);
					canDestroy = true;
				} else if (healer) {
					if (healers < maxHealers) {
						healers++;
						canDestroy = true;
					}
				} else if (weapon) {
					// give player associated weapon prefab on script position
					// instantiate in this position the prefab for the player gun
					GameObject newWeapon = Instantiate (weapon.playerWeapon, head.position, head.rotation, head.transform);
					newWeapon.name = weapon.playerWeapon.name;
					Gun g = newWeapon.GetComponent <Gun> ();
					if (g)
						g.currentMagazine = weapon.currentMagazine;
					if (hasSomeWeapon ()) {
						// Copies the player weapon to where the collected is
						Gun dropG = weapons1 [currentWeapon].GetComponent <Gun> ();
						Knife dropK = weapons1 [currentWeapon].GetComponent <Knife> ();
						GameObject droppedWeapon = null;
						Vector3 droppedPos = weapon.transform.position + (Vector3.up * 0.1f);

						if (dropG) {
							droppedWeapon = Instantiate (dropG.worldWeapon, droppedPos, weapon.transform.rotation);
							droppedWeapon.GetComponent <CollectableWeapon> ().currentMagazine = dropG.currentMagazine;
						} else if (dropK) {
							droppedWeapon = Instantiate (dropK.worldWeapon, droppedPos, weapon.transform.rotation);
						}
						if (droppedWeapon) {
							droppedWeapon.transform.eulerAngles = new Vector3 (0, head.eulerAngles.y, 0);
							droppedWeapon.name = weapons1 [currentWeapon].name;
							Destroy (weapons1 [currentWeapon]);
						}
							
						weapons1 [currentWeapon] = newWeapon;
					} else {
						weapons1 [0] = newWeapon;
						currentWeapon = 0;
					}
					canDestroy = true;
				} else if (door) {
					if (keys.Contains (door.id))
						canDestroy = true;
				}
				if (canDestroy)
					Destroy (interactable.transform.root.gameObject);
			} else {
				if (ammo) {
					switch (ammo.ammoType) {
					case Ammo.ammoTypeEnum.arrow:
						collectableInfo.text = "Pacote de flechas";
						break;
					case Ammo.ammoTypeEnum.pistol:
						collectableInfo.text = "Munição de pistola";
						break;
					case Ammo.ammoTypeEnum.rifle:
						collectableInfo.text = "Munição de rifle";
						break;
					case Ammo.ammoTypeEnum.shotgun:
						collectableInfo.text = "Munição de shotgun";
						break;
					}
				} else if (key)
					collectableInfo.text = "Chave magnética";
				else if (healer)
					collectableInfo.text = "Kit médico";
				else if (weapon)
					collectableInfo.text = weapon.gameObject.name;
				else if (door)
					if (keys.Contains (door.id))
						collectableInfo.text = "Usar chave magnética";
					else
						collectableInfo.text = "Não tenho a chave magnética";
			}
		} else {
			collectableInfo.text = "";
		}
	}

	void HUD () {
		healthInfo.fillAmount = (float)health / (float)maxHealth;
		if (hasSomeWeapon () && weapons1 [currentWeapon].activeSelf) {
			Gun g = weapons1 [currentWeapon].GetComponent<Gun> ();
			Knife k = weapons1 [currentWeapon].GetComponent<Knife> ();

			if (g)
				if (g.ammoType == Ammo.ammoTypeEnum.nothing)
					weaponInfo.text = $"{weapons1 [currentWeapon].name}\n{g.currentMagazine}";
				else
					weaponInfo.text = $"{weapons1 [currentWeapon].name}\n{g.currentMagazine} / {g.extraAmmo}";
			else if (k)
				weaponInfo.text = $"{weapons1 [currentWeapon].name}\n- / -";

			spreadUI.spread = (!g || g.aiming) ? (0) : (g.currentSpread);

			if (g && g.spreadType == Gun.spreadTypeEnum.circular)
				spreadUI.spreadType = WeaponSpreadUI.spreadTypeEnum.circular;
			else
				spreadUI.spreadType = WeaponSpreadUI.spreadTypeEnum.crosshair;
		} else {
			spreadUI.spread = 0;
			weaponInfo.text = "";
		}
	}

	void weaponHandler() {
		float weaponFOV = -1;
		if (hasSomeWeapon ()) {
			// Esconde braços do personagem
			for (int x = 0; x < armParts.Length; x++)
				if (armParts [x].activeSelf)
					armParts [x].SetActive (false);

			int weaponCount = 0;
			int visibleWeapons = 0;
			for (int x = 0; x < weapons1.Length; x++)
				if (weapons1 [x]) {
					weaponCount++;
					if (weapons1 [x].activeSelf)
						visibleWeapons++;
				}

			if (weaponCount < 2)
				scrollDir = 0;

			bool fittable = canFitArray(currentWeapon, weapons1.Length) && weapons1 [currentWeapon];
			Gun g = (fittable) ? (weapons1 [currentWeapon].GetComponent<Gun> ()) : (null);
			Knife k = (fittable) ? (weapons1 [currentWeapon].GetComponent<Knife> ()) : (null);
			bool available = (g && g.available) || (k && k.available);

			if (interruptWeapons) {
				if (g)
					g.hide = true;
				else if (k)
					k.hide = true;
			} else {
				if (scrollDir == 0 && available) {
					float scroll = Input.mouseScrollDelta.y;
					if (Mathf.Abs (scroll) >= 1 && weaponCount > 1) {
						if (g)
							g.hide = true;
						else if (k)
							k.hide = true;
						if (scroll >= 1)
							scrollDir = 1;
						else if (scroll <= -1)
							scrollDir = -1;
					}

					// Ajustando o FOV e estado das armas
					if (g) {
						g.playerState = state;
						if (g.aiming)
							weaponFOV = g.fov;
					} else if (k)
						k.playerState = state;

				} else if (scrollDir != 0) {
					currentWeapon += scrollDir;
					bool validSwap = false;
					while (!validSwap) {
						currentWeapon = (currentWeapon < 0) ? (weapons1.Length - 1) : ((currentWeapon >= weapons1.Length) ? (0) : (currentWeapon));
						validSwap = weapons1 [currentWeapon];
						if (!validSwap)
							currentWeapon += scrollDir;
					}
					scrollDir = 0;
				}
				if (visibleWeapons == 0 && fittable)
					weapons1 [currentWeapon].SetActive (true);
			}
		} else {
			// Mostra braços do personagem
			for (int x = 0; x < armParts.Length; x++)
				if (!armParts [x].activeSelf)
					armParts [x].SetActive (true);
		}

		// Adjusting FOV
		float newFOV = (weaponFOV > 0) ? (weaponFOV) : ((state.Contains("run")) ? (runFOV) : (walkFOV));
		cam.fieldOfView = Mathf.Lerp (cam.fieldOfView, newFOV, 5 * Time.deltaTime);
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

	// Animation
	void Animate () {
		float posture = anim.GetFloat ("Posture"), speed = anim.GetFloat ("Speed"), multiplier = 5 * Time.deltaTime;
		anim.SetFloat ("Posture", Mathf.Lerp (posture, (state.Contains ("crouch")) ? (1) : ((state == "on air")?(0):(0.5f)), multiplier));
		if (state.Contains ("crouch"))
			anim.SetFloat ("Speed", Mathf.Lerp (speed, (state.Contains ("idle")) ? (0) : (1), multiplier));
		else
			anim.SetFloat ("Speed", Mathf.Lerp (speed, (state.Contains ("idle")) ? (0) : ((state.Contains ("run"))?(1):(0.5f)), multiplier));
	}

	void adjustHeight () {
		body.localPosition = Vector3.zero;
		controller.height = head.position.y - transform.position.y + 0.2f;
		controller.center = new Vector3 (0, controller.height * 0.5f, 0);

		Transform neckBone = anim.GetBoneTransform(HumanBodyBones.Neck);
		Transform neckParent = neckBone.parent;
		neckBone.SetParent (transform);
		Vector3 neckOffset = neckBone.localPosition;
		neckBone.SetParent (neckParent);

		body.localPosition = new Vector3 (-neckOffset.x, -controller.skinWidth, -neckOffset.z - bodyOffset);

		// Keeps the camera on its axis
		float headY = anim.GetBoneTransform (HumanBodyBones.Head).position.y;
		head.localPosition = Vector3.up * (headY - transform.position.y + 0.1f);
	}

	// Converts [0,360] angle to [-180,180]
	float angle_0_180(float angle){
		angle = angle % 360;
		if (angle <= -180)
			angle += 360;
		else if (angle >= 180)
			angle -= 360;
		return angle;
	}

	float angle_0_360(float angle){
		angle = angle % 360;
		if (angle < 0)
			angle += 360;
		return angle;
	}

	void looking () {
		// Mouse input
		Vector2 mouseInput = new Vector2 (Input.GetAxis ("Mouse X"), Input.GetAxis ("Mouse Y")) * 5 * sensitivity * Time.deltaTime;
		transform.eulerAngles += new Vector3 (0, mouseInput.x, 0);
		head.localEulerAngles = new Vector3 (Mathf.Clamp (angle_0_180(head.localEulerAngles.x) - mouseInput.y, -clampAngle, clampAngle), 0, 0);
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
		if (anim.GetFloat ("Speed") == 0)
			animationAngle = theta;
		else if (!state.Contains ("idle"))
			animationAngle = Mathf.LerpAngle (animationAngle, theta, 5 * Time.deltaTime);
		anim.SetFloat ("Angle", angle_0_360 (animationAngle));
	}

	bool isGrounded () {
		if (controller.isGrounded)
			return true;
		else {
			float randAngle = UnityEngine.Random.Range (0, 45);
			for (float d = 0.5f; d <= 1; d += 0.5f) {
				for (int theta = 0; theta < 360; theta += 45) {
					Vector3 rayLocation = transform.position + new Vector3 (Mathf.Cos ((theta + randAngle) * Mathf.Deg2Rad), 0, Mathf.Sin ((theta + randAngle) * Mathf.Deg2Rad)) * d * controller.radius;
					RaycastHit[] hits = Physics.RaycastAll (rayLocation, Vector3.down, controller.skinWidth + 0.05f + controller.stepOffset);
					for (int i = 0; i < hits.Length; i++)
						if (hits [i].transform.root != transform)
							return true;
				}
			}
			return false;
		}
	}

	// Checks if there is a distance smaller than the height above head
	bool canUncrouch () {
		float randAngle = UnityEngine.Random.Range (0, 45);
		for (float d = 0.5f; d <= 1; d += 0.5f) {
			for (int theta = 0; theta < 360; theta += 45) {
				float newAngle = theta + randAngle;
				Vector3 rayOffset = new Vector3 (Mathf.Cos (newAngle * Mathf.Deg2Rad), 0, Mathf.Sin (newAngle * Mathf.Deg2Rad)) * d * controller.radius;
				Vector3 rayLocation = head.position + rayOffset;
				RaycastHit[] hits = Physics.RaycastAll (rayLocation, Vector3.up, uprightHeight - head.position.y);
				for (int i = 0; i < hits.Length; i++)
					if (hits [i].transform.root != transform && hits [i].distance < uprightHeight - head.position.y)
						return false;
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
			if (wasGrounded && !jumping)
				currentJumps++;
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
			if (canUncrouch ())
				crouching = false;
		} else if (Input.GetKeyDown (KeyCode.C)) {
			if (!crouching)
				crouching = true;
			else if (canUncrouch ())
				crouching = false;
		}
	}
}
