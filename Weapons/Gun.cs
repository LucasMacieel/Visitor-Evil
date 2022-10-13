using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour {
	public GameObject worldWeapon;
	[Header("Ammo")]
	public Ammo.ammoTypeEnum ammoType = Ammo.ammoTypeEnum.pistol;
	[HideInInspector]public int extraAmmo;
	[Range(0,100)]public int currentMagazine = 20;
	[Range(1,100)]public int maxMagazine = 21;
	private Animator anim;

	public enum firemodeEnum {manual, automatic};
	public enum reloadModeEnum {unit, complete};
	public enum spreadTypeEnum {crosshair, circular};
	[Header("Specific settings")]
	[Range(10,60)]public float fov = 40;
	public firemodeEnum firemode = firemodeEnum.automatic;
	public reloadModeEnum reloadMode = reloadModeEnum.complete;
	public spreadTypeEnum spreadType = spreadTypeEnum.crosshair;
	public Vector2 reloadUnitInterval = Vector2.zero;
	[Range(1,3)]public int shotsPerClick = 1;
	public bool chamberBullet = true;

	[Header("Projectile")]
	[Range(1,100)]public int damage = 10;
	public GameObject projectile, muzzle;
	private Transform spawner;
	[Range(1,10)]public int projectileAnmount = 1;
	[Range(0,1f)]public float spread = 0.2f;
	[Range(0,0.1f)]public float recoilPerShot = 0.02f;
	[HideInInspector]public float currentSpread;

	public string playerState;
	[HideInInspector]public bool aiming, hide, available, hiddenMeshes;
	private bool wasAiming, wasDepleted, reloading, shooting, unitReloadAdjust;
	private int currentShotsPerClick, hiddenTime;
	private float timeWithoutShots, maxTimeWithoutShots = 3;
	private bool interruptUnitReload;

	void Start () {
		anim = GetComponentInChildren<WeaponAnimatorAux> ().transform.GetComponent<Animator> ();
		spawner = findDeepChild (transform, "spawner");
		ResetProperties ();

		if (currentMagazine > maxMagazine)
			currentMagazine = maxMagazine;
	}

	void lerpLayerWeight(string layer, float value, float speed) {
		int i = anim.GetLayerIndex (layer);
		anim.SetLayerWeight (i, Mathf.Lerp (anim.GetLayerWeight (i), value, speed * Time.deltaTime));
	}

	void setLayerWeight(string layer, float value) {
		int i = anim.GetLayerIndex (layer);
		anim.SetLayerWeight (i, value);
	}

	bool between(int value, int min, int max) {
		return value >= min && value <= max;
	}

	Transform findDeepChild (Transform parent, string name) {
		for (int x = 0; x < parent.childCount; x++) {
			if (parent.GetChild (x).name.ToLower () == name.ToLower ())
				return parent.GetChild (x);
			else {
				Transform otherChild = findDeepChild (parent.GetChild (x), name);
				if (otherChild != null)
					return otherChild;
			}
		}
		return null;
	}

	void Update () {
		extraAmmo = 0;
		PlayerController player = transform.root.GetComponent <PlayerController> ();
		// Pega os valores de munição
		if (player) {
			switch (ammoType) {
			case Ammo.ammoTypeEnum.pistol:
				extraAmmo = player.pistolAmmo;
				break;
			case Ammo.ammoTypeEnum.rifle:
				extraAmmo = player.rifleAmmo;
				break;
			case Ammo.ammoTypeEnum.shotgun:
				extraAmmo = player.shotgunAmmo;
				break;
			case Ammo.ammoTypeEnum.arrow:
				extraAmmo = player.arrows;
				break;
			}
		}

		int i = anim.GetLayerIndex ("Move");
		string state = anim.GetCurrentAnimatorClipInfo (i) [0].clip.name;
		available = state != "Draw" && state != "Stock" && !hide;
		firearm ();

		if (hiddenMeshes) {
			if (hiddenTime > 0) {
				hiddenMeshes = false;
				setMeshes (transform, true);
				hiddenTime = 0;
			} else
				hiddenTime++;
		}

		if (hide)
			Hide ();
		
		// Reatribui os valores
		if (player) {
			switch (ammoType) {
			case Ammo.ammoTypeEnum.pistol:
				 player.pistolAmmo = extraAmmo;
				break;
			case Ammo.ammoTypeEnum.rifle:
				player.rifleAmmo = extraAmmo;
				break;
			case Ammo.ammoTypeEnum.shotgun:
				player.shotgunAmmo = extraAmmo;
				break;
			case Ammo.ammoTypeEnum.arrow:
				player.arrows = extraAmmo;
				break;
			}
		}
	}

	void setMeshes (Transform parent, bool value) {
		for (int x = 0; x < parent.childCount; x++) {
			MeshRenderer mr = parent.GetChild (x).GetComponent<MeshRenderer> ();
			SkinnedMeshRenderer smr = parent.GetChild (x).GetComponent<SkinnedMeshRenderer> ();
			if (mr != null)
				mr.enabled = value;
			if (smr != null)
				smr.enabled = value;
			setMeshes (parent.GetChild (x), value);
		}
	}

	void firearm () {
		bool r = Input.GetKeyDown (KeyCode.R) && available;
		bool mouse0 = ((firemode == firemodeEnum.automatic) ? (Input.GetKey (KeyCode.Mouse0)) : (Input.GetKeyDown (KeyCode.Mouse0))) && available;
		bool mouse1 = Input.GetKeyDown (KeyCode.Mouse1) && available;

		if (mouse0 && currentShotsPerClick == 0)
			currentShotsPerClick++;
		else if (between (currentShotsPerClick, 1, shotsPerClick - 1) && !shooting) {
			mouse0 = true;
			currentShotsPerClick++;
		} else if (currentShotsPerClick == shotsPerClick)
			currentShotsPerClick = 0;


		int j = anim.GetLayerIndex ("Shoot");
		AnimatorClipInfo[] clip = anim.GetCurrentAnimatorClipInfo (j);
		string clipName = clip [0].clip.name;
		bool triggerReload = (currentMagazine == 0 || (r && currentMagazine < maxMagazine) || reloading) && extraAmmo > 0 && clipName.Contains ("Idle");

		if (triggerReload)//alfa
			Reload ();
		else if ((mouse0 && currentMagazine > 0) || shooting)
			Shoot ();
		else
			NoAction ();

		motion ();

		int i = anim.GetLayerIndex ("Move");
		string state = anim.GetCurrentAnimatorClipInfo (i) [0].clip.name;
		if (playerState.Contains ("run") || playerState == "on air") {
			aiming = false;
			wasAiming = false;
		} else if (reloading)
			aiming = false;
		else if (mouse1)
			aiming = !aiming;

		firearmPosture ();

		calculateSpread ();
	}

	void calculateSpread () {
		float spreadMultiplier = 0;
		if (playerState.Contains ("idle") && aiming)
			spreadMultiplier = 0.2f;
		else if (playerState.Contains ("walk") && aiming)
			spreadMultiplier = 0.4f;
		else if (playerState.Contains ("idle"))
			spreadMultiplier = 0.35f;
		else if (playerState.Contains ("walk"))
			spreadMultiplier = 0.67f;
		else
			spreadMultiplier = 0.8f;
		currentSpread = Mathf.Lerp (currentSpread, spread * spreadMultiplier, 5 * Time.deltaTime);
		currentSpread = Mathf.Clamp (currentSpread, 0, 1);
	}

	void motion () {
		if (playerState.Contains ("idle") || playerState == "on air")
			anim.SetInteger ("Motion", 0);
		else if (playerState.Contains ("walk"))
			anim.SetInteger ("Motion", 1);
		else
			anim.SetInteger ("Motion", 2);
	}

	void firearmPosture () {
		if (aiming)
			anim.SetInteger ("Posture", 2);
		else if (timeWithoutShots < maxTimeWithoutShots || reloading)
			anim.SetInteger ("Posture", 1);
		else
			anim.SetInteger ("Posture", 0);
	}

	Vector3 closestPoint (RaycastHit[] hits) {
		float minDistance = Mathf.Infinity;
		Vector3 point = new Vector3 (Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
		for (int x = 0; x < hits.Length; x++) {
			if (hits [x].distance < minDistance && !hits [x].transform.root.GetComponent<PlayerController> () && !hits [x].collider.isTrigger) {
				point = hits [x].point;
				minDistance = hits [x].distance;
			}
		}
		return point;
	}

	float eulerDifference (Vector3 a, Vector3 b) {
		float x = Mathf.DeltaAngle (a.x, b.x);
		float y = Mathf.DeltaAngle (a.y, b.y);
		float z = Mathf.DeltaAngle (a.z, b.z);
		return new Vector3 (x, y, z).magnitude;
	}

	void lookAt(Transform t, Vector3 point) {
		Transform c = t.GetChild (0);
		Vector3 lastPos = c.localPosition;
		Vector3 lastEuler = c.localEulerAngles;
		c.position = point;
		c.SetParent (null);
		t.transform.LookAt (c, Vector3.up);
		c.SetParent (t.transform);
		c.localPosition = lastPos;
		c.localEulerAngles = lastEuler;
	}

	void shootProjectile () {
		Transform head = (transform.GetComponentInParent <Camera> ()) ? (transform.parent) : (null);
		if (head) {
			float targetDistange = Vector3.Distance (closestPoint (Physics.RaycastAll (head.position, head.forward, 2000)), head.position);
			targetDistange = Mathf.Clamp (targetDistange, 0, 2000);
			for (int x = 0; x < projectileAnmount; x++) {
				float randAngle = UnityEngine.Random.Range (0, 360);
				float randDistance = UnityEngine.Random.Range (0, currentSpread) / 2;
				Vector3 shotOffset = new Vector3 (Mathf.Cos (randAngle * Mathf.Deg2Rad), Mathf.Sin (randAngle * Mathf.Deg2Rad), 0) * randDistance;
				shotOffset = (head.up * shotOffset.y) + (head.right * shotOffset.x);

				Vector3 point = closestPoint (Physics.RaycastAll (head.position + shotOffset * targetDistange, transform.parent.forward, 2000));
				if (point.magnitude == Mathf.Infinity)
					point = transform.parent.position + transform.parent.forward * 2000 + shotOffset * targetDistange;
				GameObject projectileInstance = Instantiate (projectile, spawner.position, spawner.rotation);
				projectileInstance.GetComponent <Projectile> ().damage = damage;

				Transform model = projectileInstance.transform.GetChild (0);
				Vector3 lastPos = model.localPosition;
				Vector3 lastEuler = model.localEulerAngles;
				model.position = point;
				model.SetParent (null);
				projectileInstance.transform.LookAt (model, Vector3.up);
				model.SetParent (projectileInstance.transform);
				model.localPosition = lastPos;
				model.localEulerAngles = lastEuler;

				lookAt (projectileInstance.transform, point);
				// A arma atravessou o alvo, o projétil deve então ser instanciado a partir da câmera
				if (eulerDifference (head.eulerAngles, projectileInstance.transform.eulerAngles) > 45) {
					projectileInstance.transform.position = head.position;
					lookAt (projectileInstance.transform, point);
				}
			}
			if (muzzle) {
				Transform muzzleInstance = Instantiate (muzzle, spawner.position, transform.parent.rotation).transform;
				muzzleInstance.SetParent (spawner);
			}
			currentSpread += recoilPerShot;
		}
	}

	void Hide () {
		available = false;
		anim.SetInteger ("Hide", 1);
		lerpLayerWeight ("Shoot", 0, 5);
		lerpLayerWeight ("Reload", 0, 5);
		interruptUnitReload = true;
		int i = anim.GetLayerIndex ("Move");
		AnimatorClipInfo[] clip = anim.GetCurrentAnimatorClipInfo (i);
		if (clip [0].clip.name.Contains ("Draw")) {
			ResetProperties ();
			anim.Play ("Draw", i);
			gameObject.SetActive (false);
		}
	}

	void Reload () {
		int i = anim.GetLayerIndex ("Reload");
		lerpLayerWeight ("Shoot", 0, 5);
		currentShotsPerClick = 0;
		if (!reloading) {
			wasAiming = aiming;
			reloading = true;
			wasDepleted = currentMagazine == 0;
			anim.Play ("Reload" + ((wasDepleted) ? (" full") : ("")), i);
		} else {
			if (reloadMode == reloadModeEnum.complete)
				completeReload ();
			else
				unitReload ();
		}
	}

	void completeReload () {
		int i = anim.GetLayerIndex ("Reload");
		AnimatorClipInfo[] clip = anim.GetCurrentAnimatorClipInfo (i);
		if (clip [0].clip.name.Contains ("Reload")) {
			lerpLayerWeight ("Reload", 1, 5);
		} else {
			// Add ammo to mag
			int neededAmmo = maxMagazine - currentMagazine;
			if (wasDepleted && chamberBullet)
				neededAmmo--;
			if (neededAmmo > extraAmmo)
				neededAmmo = extraAmmo;
			extraAmmo -= neededAmmo;
			currentMagazine += neededAmmo;
			// Reset reload
			reloading = false;
			wasDepleted = false;
			// Resume aiming
			aiming = wasAiming;
			wasAiming = false;
			if (timeWithoutShots < maxTimeWithoutShots)
				timeWithoutShots = 0;
		}
	}

	void unitReload () {
		int i = anim.GetLayerIndex ("Reload");
		AnimatorClipInfo[] clip = anim.GetCurrentAnimatorClipInfo (i);
		string clipName = clip [0].clip.name;
		if (clip [0].clip.name.Contains ("Reload")) {
			// Player pode interromper o reload em algumas partes
			if (Input.GetKeyDown (KeyCode.Mouse0) && currentMagazine > 0)
				interruptUnitReload = true;

			lerpLayerWeight ("Reload", 1, 5);
			AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo (i);
			float framerate = clip [0].clip.frameRate;
			float currentFrame = state.length * state.normalizedTime * framerate;
			// Se chegar no frame final da animação de reload, replay no momento
			if (currentFrame >= reloadUnitInterval.y && !unitReloadAdjust && !interruptUnitReload) {
				currentMagazine++;
				extraAmmo--;
				int neededAmmo = maxMagazine - currentMagazine;
				if (wasDepleted && chamberBullet)
					neededAmmo--;
				if (neededAmmo > 0 && extraAmmo > 0) {
					float totalFrames = state.length * framerate;
					anim.Play ("Reload" + ((wasDepleted) ? (" full") : ("")), i, reloadUnitInterval.x / totalFrames);
				} else {
					unitReloadAdjust = true;
				}
			}
		} else {
			// Reset reload
			reloading = false;
			wasDepleted = false;
			unitReloadAdjust = false;
			interruptUnitReload = false;
			// Resume aiming
			aiming = wasAiming;
			wasAiming = false;
			if (timeWithoutShots < maxTimeWithoutShots)
				timeWithoutShots = 0;
		}
	}

	void Shoot () {
		int i = anim.GetLayerIndex ("Shoot");
		lerpLayerWeight ("Reload", 0, 5);
		if (!shooting) {
			shootProjectile ();
			anim.Play ("Shoot", i);
			currentMagazine--;
			timeWithoutShots = 0;
			shooting = true;
		} else {
			AnimatorClipInfo[] clip = anim.GetCurrentAnimatorClipInfo (i);
			if (clip [0].clip.name.Contains ("Shoot")) {
				lerpLayerWeight ("Shoot", 1, 5);
			} else {
				shooting = false;
			}
		}
	}

	void NoAction () {
		timeWithoutShots += Time.deltaTime;
		lerpLayerWeight ("Shoot", 0, 5);
		lerpLayerWeight ("Reload", 0, 5);
		currentShotsPerClick = 0;
	}

	void ResetProperties () {
		currentSpread = 0;
		aiming = false;
		wasAiming = false;
		wasDepleted = false;
		hide = false;
		hiddenMeshes = true;
		reloading = false;
		shooting = false;
		available = false;
		unitReloadAdjust = false;
		currentShotsPerClick = 0;
		interruptUnitReload = false;
		timeWithoutShots = 0;
		anim.Rebind ();
		setLayerWeight ("Shoot", 0);
		setLayerWeight ("Reload", 0);
		anim.SetInteger ("Posture", 1);
		setMeshes (transform, false);
		hiddenTime = 0;
		if (shotsPerClick > 1)
			firemode = firemodeEnum.manual;
	}
}
