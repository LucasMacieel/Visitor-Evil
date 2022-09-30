using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandsTargeter : MonoBehaviour {
	private Animator anim;
	public Transform left, right;
	private WeaponAnimatorAux weaponAux;

	void Start () {
		anim = GetComponent<Animator> ();
		anim.SetLayerWeight (anim.GetLayerIndex ("Right hand"), 1);
		anim.SetLayerWeight (anim.GetLayerIndex ("Left hand"), 1);
		weaponAux = transform.parent.GetComponentInChildren<WeaponAnimatorAux> ();
	}

	void OnAnimatorIK () {
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
