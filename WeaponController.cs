using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour {
	[Header("Ammo")]
	[Range(0,200)]public int extraAmmo = 60;
	[Range(0,100)]public int currentMagazine = 20;
	[Range(1,100)]public int maxMagazine = 21;
	private Animator anim;

	public enum playerStateEnum {idle, walking, running};
	public enum weaponTypeEnum {firearm, melee};
	public enum firemodeEnum {manual, automatic};
	public enum reloadModeEnum {unit, complete};
	[Header("Specific settings")]
	public playerStateEnum playerState = playerStateEnum.idle;
	public weaponTypeEnum weaponType = weaponTypeEnum.firearm;
	public firemodeEnum firemode = firemodeEnum.automatic;
	public reloadModeEnum reloadMode = reloadModeEnum.complete;
	[Range(1,3)]public int shotsPerClick = 1;
	public bool chamberBullet = true;

	[HideInInspector]public bool hide, aiming;
	private bool wasAiming, wasDepleted, reloading, shooting;
	private bool reloadAdjust;
	private int currentShotsPerClick = 0;
	private float timeWithoutShots = 0;

	void Start () {
		anim = GetComponentInChildren<WeaponAnimatorAux> ().transform.GetComponent<Animator> ();
		ResetProperties ();
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

	void Update () {
		if (weaponType == weaponTypeEnum.firearm)
			firearm ();
		else
			melee ();
	}

	void melee () {
		
	}

	void firearm () {
		bool r = Input.GetKeyDown (KeyCode.R);
		bool mouse0 = (firemode == firemodeEnum.automatic) ? (Input.GetKey (KeyCode.Mouse0)) : (Input.GetKeyDown (KeyCode.Mouse0));
		bool mouse1 = Input.GetKeyDown (KeyCode.Mouse1);

		if (mouse0 && currentShotsPerClick == 0)
			currentShotsPerClick++;
		else if (between (currentShotsPerClick, 1, shotsPerClick - 1) && !shooting) {
			mouse0 = true;
			currentShotsPerClick++;
		} else if (currentShotsPerClick == shotsPerClick)
			currentShotsPerClick = 0;

		if ((currentMagazine == 0 || (r && currentMagazine < maxMagazine) || reloading) && extraAmmo > 0)
			Reload ();
		else if ((mouse0 && currentMagazine > 0) || shooting)
			Shoot ();
		else
			NoAction ();

		if (playerState == playerStateEnum.idle)
			anim.SetInteger ("Motion", 0);
		else if (playerState == playerStateEnum.walking)
			anim.SetInteger ("Motion", 1);
		else
			anim.SetInteger ("Motion", 2);

		if (playerState == playerStateEnum.running || reloading)
			aiming = false;
		else if (mouse1)
			aiming = !aiming;

		if (aiming)
			anim.SetInteger ("Posture", 2);
		else if (timeWithoutShots < 1.0f || reloading)
			anim.SetInteger ("Posture", 1);
		else
			anim.SetInteger ("Posture", 0);

		if (hide)
			Hide ();
	}

	void Hide () {
		anim.SetInteger ("Posture", 0);
		anim.SetInteger ("Hide", 1);
		lerpLayerWeight ("Shoot", 0, 5);
		lerpLayerWeight ("Reload", 0, 5);
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
			if (reloadMode == reloadModeEnum.complete)
				anim.Play ("Reload" + ((wasDepleted) ? (" full") : ("")), i);
			else
				anim.Play ("Reload start", i);
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
			if (timeWithoutShots < 1)
				timeWithoutShots = 0;
		}
	}

	void unitReload () {
		int i = anim.GetLayerIndex ("Reload");
		AnimatorClipInfo[] clip = anim.GetCurrentAnimatorClipInfo (i);
		string clipName = clip [0].clip.name;
		if (clip [0].clip.name.Contains ("Reload")) {
			lerpLayerWeight ("Reload", 1, 5);
			int neededAmmo = maxMagazine - currentMagazine;
			if (wasDepleted && chamberBullet)
				neededAmmo--;
			if (clipName == "Reload stop" && neededAmmo > 0 && extraAmmo > 0) {
				anim.Play ("Reload unit", i);
				currentMagazine++;
				extraAmmo--;
			} else if (clipName == "Reload unit" && !reloadAdjust) {
				Debug.Log ("sim");
				anim.Play ("Reload stop", i);
				reloadAdjust = true;
			}
		} else {
			// Reset reload
			reloading = false;
			wasDepleted = false;
			reloadAdjust = false;
			// Resume aiming
			aiming = wasAiming;
			wasAiming = false;
			if (timeWithoutShots < 1)
				timeWithoutShots = 0;
		}
	}

	void Shoot () {
		int i = anim.GetLayerIndex ("Shoot");
		lerpLayerWeight ("Reload", 0, 5);
		if (!shooting) {
			// Fire bullet
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
		aiming = false;
		wasAiming = false;
		wasDepleted = false;
		hide = false;
		reloading = false;
		shooting = false;
		currentShotsPerClick = 0;
		timeWithoutShots = 0;
		anim.Rebind ();
		setLayerWeight ("Shoot", 0);
		setLayerWeight ("Reload", 0);
		anim.SetInteger ("Posture", 1);
		reloadAdjust = false;
		if (shotsPerClick > 1)
			firemode = firemodeEnum.manual;
	}
}
