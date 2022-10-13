using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knife : MonoBehaviour {
	public GameObject worldWeapon;
	private Animator anim;
	[HideInInspector]public string playerState;
	[Range(1,100)]public int damage = 25;
	[Range(0.5f,1.5f)]public float range = 0.8f;
	public KnifeHurtbox hurtbox;
	[HideInInspector]public bool hide, available, hiddenMeshes;
	private bool shooting;
    private int hiddenTime;

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

	public void Attack () {
		Vector3 pos = transform.position + (transform.forward * range / 2);
		KnifeHurtbox hb = Instantiate (hurtbox.gameObject, pos, transform.rotation).GetComponent <KnifeHurtbox> ();
		SphereCollider col = hb.gameObject.GetComponent <SphereCollider> ();
		col.radius = range / 2;
		hb.damage = damage;
		
	}

	void Update () {
		int i = anim.GetLayerIndex ("Move");
		string state = anim.GetCurrentAnimatorClipInfo (i) [0].clip.name;
		available = state != "Draw" && state != "Stock" && !hide;
		melee ();

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

	void melee () {
		bool mouse0 = Input.GetKeyDown (KeyCode.Mouse0) && available;

		if (mouse0 || shooting)
			Shoot ();
		else
			NoAction ();

		motion ();
	}

	void motion () {
		if (playerState.Contains ("idle") || playerState == "on air")
			anim.SetInteger ("Motion", 0);
		else if (playerState.Contains ("walk"))
			anim.SetInteger ("Motion", 1);
		else
			anim.SetInteger ("Motion", 2);
	}

	void Hide () {
		available = false;
		anim.SetInteger ("Hide", 1);
		lerpLayerWeight ("Shoot", 0, 5);
		int i = anim.GetLayerIndex ("Move");
		AnimatorClipInfo[] clip = anim.GetCurrentAnimatorClipInfo (i);
		if (clip [0].clip.name.Contains ("Draw")) {
			ResetProperties ();
			anim.Play ("Draw", i);
			gameObject.SetActive (false);
		}
	}

	void Shoot () {
		int i = anim.GetLayerIndex ("Shoot");
		if (!shooting) {
			string animation = "Attack" + UnityEngine.Random.Range (1, 4).ToString ();
			anim.Play (animation, i);
			shooting = true;
		} else {
			AnimatorClipInfo[] clip = anim.GetCurrentAnimatorClipInfo (i);
			if (clip [0].clip.name.Contains ("Attack")) {
				lerpLayerWeight ("Shoot", 1, 5);
			} else {
				shooting = false;
			}
		}
	}

	void NoAction () {
		lerpLayerWeight ("Shoot", 0, 5);
	}

	void ResetProperties () {
		hide = false;
		available = false;
		shooting = false;
		hiddenMeshes = true;
		anim.Rebind ();
		setLayerWeight ("Shoot", 0);
		setMeshes (transform, false);
        hiddenTime = 0;
	}
}
