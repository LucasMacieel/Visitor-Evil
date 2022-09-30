using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class Orientation {
	public Vector3 position, eulerAngles;
}
	
public class Weapon : MonoBehaviour {
	public GameObject projectile;
	[Range(1,10)]public int projectileAnmount = 1;
	public enum weaponTypeEnum {Gun, Melee};
	public weaponTypeEnum weaponType = weaponTypeEnum.Gun;
	public Orientation idle, aiming, running, nearWall;
	[HideInInspector]public Animator anim;

	public Transform left, right;

	//
	[Range(0,1)]public float spread = 0.2f;
	[Range(0,200)]public int currentMagazine = 20;
	[Range(1,400)]public int maxMagazine = 20;
	[Space(10)]
	[Range(0,200)]public int currentExtra = 120;
	[Range(1,400)]public int maxExtra = 120;
	private bool reloading = false;
	[HideInInspector]public bool stocking;
	public string state = "none";

	private Transform lastRoot, spawner;
	private bool attached;

	public bool reloadUnit = false;
	private bool depleted = false;

	void Start () {
		anim = GetComponent<Animator> ();
		spawner = transform.GetChild (0).FindChild ("Spawner");
	}
	
	// Update is called once per frame
	void Update () {
		if (lastRoot != transform.root)
			correctOrientation ();
		if (attached && transform.parent)
			Gun ();
		lastRoot = transform.root;
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

	public void correctOrientation () {
		if (transform.root.GetComponent<PlayerController> ()) {
			attached = true;
			anim.enabled = true;
			anim.Play (name + " Draw");
			transform.localPosition = idle.position;
			transform.localEulerAngles = idle.eulerAngles;
		} else {
			transform.SetParent (null);
			anim.Play (name + " Idle");
			anim.enabled = false;
			depleted = false;
		}
	}

	void Gun () {
		bool mouse0 = Input.GetKey (KeyCode.Mouse0);
		bool r = Input.GetKeyDown (KeyCode.R);

		AnimatorClipInfo[] clipInfo = anim.GetCurrentAnimatorClipInfo (0);
		state = (clipInfo.Length > 0) ? (clipInfo [0].clip.name) : ("none");

		if (stocking) {
			if (state.Contains ("Idle") || state.Contains ("Reload")) {
				anim.Play (name + " Stock");
			} else if (state.Contains ("Draw")) {
				stocking = false;
				gameObject.SetActive (false);
			}
		} else if (state.Contains ("Idle")) {
			if ((r || currentMagazine == 0) && currentMagazine < maxMagazine && currentExtra > 0 && !reloading) {
				depleted = currentMagazine == 0;
				if (!reloadUnit)
					anim.Play (name + " Reload" + ((depleted) ? (" Full") : ("")));
				else
					anim.Play (name + " Start Reload");
			} else if (reloading && !reloadUnit) {
				int neededAmmo = maxMagazine - currentMagazine;
				currentMagazine += (neededAmmo <= currentExtra) ? (neededAmmo) : (currentExtra);
				currentExtra -= (neededAmmo <= currentExtra) ? (neededAmmo) : (currentExtra);
				reloading = false;
				depleted = false;
			} else if (currentMagazine > 0 && mouse0) {
				Shoot ();
			}
		} else if ((state.Contains ("Reload") && !reloadUnit) || (state.Contains ("Unit Reload") && reloadUnit)) {
			if (anim.GetCurrentAnimatorStateInfo (0).normalizedTime > 1 - Time.deltaTime * 2) {
				reloading = true;
				if (reloadUnit) {
					currentExtra--;
					currentMagazine++;
					if (currentMagazine < maxMagazine && currentExtra > 0) {
						anim.Play (name + " Unit Reload");
					} else {
						anim.Play (name + " Stop Reload" + ((depleted) ? (" Full") : ("")));
						depleted = false;
					}
					reloading = false;
				}
			} else if (reloadUnit && mouse0 && currentMagazine > 0)
				Shoot ();
		}
	}

	void Shoot () {
		anim.Play (name + " Shot");
		float distanceFromHead = Vector3.Distance (closestPoint (Physics.RaycastAll (transform.parent.position, transform.parent.forward, 2000)), transform.parent.position);
		distanceFromHead = Mathf.Clamp (distanceFromHead, 0, 2000);
		PlayerController p = transform.root.GetComponent <PlayerController> ();
		float currentSpread = spread;
		if (p.aiming && p.state.Contains ("idle"))
			currentSpread *= 0.25f;
		else if (p.aiming && p.state.Contains ("walk") || p.state.Contains ("idle"))
			currentSpread *= 0.5f;
		else if (p.state.Contains ("walk"))
			currentSpread *= 0.75f;

		for (int x = 0; x < projectileAnmount; x++) {
			GameObject projectileInstance = Instantiate (projectile, spawner.position, spawner.rotation);

			float randAngle = UnityEngine.Random.Range (0, 360);
			float randDistance = UnityEngine.Random.Range (0, currentSpread);

			Vector3 shotOffset = new Vector3 (Mathf.Cos (randAngle * Mathf.Deg2Rad), 0, Mathf.Sin (randAngle * Mathf.Deg2Rad)) * randDistance;
			shotOffset = (transform.parent.up * shotOffset.z) + (transform.parent.right * shotOffset.x);

			Vector3 point = closestPoint (Physics.RaycastAll (transform.parent.position + shotOffset * distanceFromHead, transform.parent.forward, 2000));
			if (point.magnitude == Mathf.Infinity)
				point = transform.parent.position + transform.parent.forward * 2000 + shotOffset * distanceFromHead;
			Transform model = projectileInstance.transform.GetChild (0);
			Vector3 lastPos = model.localPosition;
			Vector3 lastEuler = model.localEulerAngles;
			model.position = point;
			model.SetParent (null);
			projectileInstance.transform.LookAt (model, Vector3.up);
			model.SetParent (projectileInstance.transform);
			model.localPosition = lastPos;
			model.localEulerAngles = lastEuler;
		}
		currentMagazine -= 1;
	}
}