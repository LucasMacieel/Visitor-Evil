using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandsTargeter : MonoBehaviour {
	private Animator anim;
	private Transform left, right;
	private WeaponAnimatorAux weaponAux;

	void Start () {
		anim = GetComponent<Animator> ();
		weaponAux = transform.parent.GetComponentInChildren<WeaponAnimatorAux> ();
		left = findDeepChild (weaponAux.transform.parent, "left");
		right = findDeepChild (weaponAux.transform.parent, "right");
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

	void OnAnimatorIK () {
		int r = anim.GetLayerIndex ("Right hand");
		int l = anim.GetLayerIndex ("Left hand");
		if (anim.GetLayerWeight (r) < 1.0f) {
			anim.SetLayerWeight (r, 1);
			anim.SetLayerWeight (l, 1);
		}

		anim.SetIKPositionWeight (AvatarIKGoal.RightHand, 1);
		anim.SetIKRotationWeight (AvatarIKGoal.RightHand, 1);  
		anim.SetIKPosition (AvatarIKGoal.RightHand, right.position);
		anim.SetIKRotation (AvatarIKGoal.RightHand, right.rotation);
		anim.SetIKPositionWeight (AvatarIKGoal.LeftHand, 1);
		anim.SetIKRotationWeight (AvatarIKGoal.LeftHand, 1);
		anim.SetIKPosition (AvatarIKGoal.LeftHand, left.position);
		anim.SetIKRotation (AvatarIKGoal.LeftHand, left.rotation);

		anim.SetFloat ("OpenLeft", weaponAux.leftHandOpen);
		anim.SetFloat ("IndexLeft", weaponAux.leftIndexOut);
		anim.SetFloat ("OpenRight", weaponAux.rightHandOpen);
		anim.SetFloat ("IndexRight", weaponAux.rightIndexOut);
	}
}
