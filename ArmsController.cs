using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmsController : MonoBehaviour {

	public Weapon weapon;
	private Animator anim;
	private Transform shoulder;
	private Transform neckBone;
	private PlayerController player;

	private Vector3 accOffsetPosition, accOffsetEulerAngles;
	private Vector3 lastShoulderPosition, lastShoulderEulerAngles;

	private bool hadWeapon;

	// Converts [0,360] angle to [-180,180]
	float angle_0_180 (float angle){
		angle = angle % 360;
		if (angle <= -180) {angle += 360;}
		else if (angle >= 180) {angle -= 360;}
		return angle;
	}

	// Converts [0,360] vector to [-180,180]
	Vector3 euler_0_180 (Vector3 euler) {
		euler.x = angle_0_180 (euler.x);
		euler.y = angle_0_180 (euler.y);
		euler.z = angle_0_180 (euler.z);
		return euler;
	}

	Vector3 LerpEulerAngles (Vector3 a, Vector3 b, float t) {
		Vector3 result = Vector3.zero;
		result.x = Mathf.LerpAngle (a.x, b.x, t);
		result.y = Mathf.LerpAngle (a.y, b.y, t);
		result.z = Mathf.LerpAngle (a.z, b.z, t);
		return result;
	}

	void Start () {
		player = transform.root.GetComponent<PlayerController> ();
		anim = GetComponent<Animator> ();
		shoulder = anim.GetBoneTransform (HumanBodyBones.RightShoulder);
		neckBone = anim.GetBoneTransform (HumanBodyBones.Neck);
	}

	void Update () {
		string state = player.state;
		float speed = anim.GetFloat ("Speed");
		anim.SetFloat ("Speed", Mathf.Lerp (speed, (state.Contains ("idle")) ? (0) : ((state.Contains ("run"))?(1):(0.5f)), 5 * Time.deltaTime));

		Transform neckParent = neckBone.parent;
		neckBone.SetParent (transform);
		Vector3 neckOffset = neckBone.localPosition;
		neckBone.SetParent (neckParent);

		Vector3 armDisplacement = -Vector3.forward * player.armOffset;
		transform.localPosition = -neckOffset + armDisplacement;

		if (weapon) {
			Vector3 pos, euler;
			if (state.Contains ("run") && weapon.state.Contains ("Idle")) {
				pos = weapon.running.position;
				euler = weapon.running.eulerAngles;
			} else if (player.aiming) {
				pos = weapon.aiming.position;
				euler = weapon.aiming.eulerAngles;
			} else if (player.nearWall && weapon.state.Contains ("Idle")) {
				pos = weapon.nearWall.position;
				euler = weapon.nearWall.eulerAngles;
			} else {
				pos = weapon.idle.position;
				euler = weapon.idle.eulerAngles;
			}

			bool stopPositionOffset = false;
			bool stopAngleOffset = false;

			if (player.aiming) {
				if (accOffsetPosition.magnitude > 0.02f)
					accOffsetPosition = Vector3.zero;
				if (accOffsetEulerAngles.magnitude > 5)
					accOffsetEulerAngles = Vector3.zero;
			} else {
				if (accOffsetPosition.magnitude > 0.04f)
					accOffsetPosition = Vector3.zero;
				if (accOffsetEulerAngles.magnitude > 10)
					accOffsetEulerAngles = Vector3.zero;
			}

			if (weapon) {
				Transform shoulderParent = shoulder.parent;
				shoulder.SetParent (transform.parent);

				Vector3 offsetPosition = shoulder.localPosition - lastShoulderPosition;
				accOffsetPosition += (offsetPosition.magnitude > 0.01f) ? (Vector3.zero) : (offsetPosition);
				weapon.transform.localPosition = Vector3.Lerp (weapon.transform.localPosition, pos + accOffsetPosition / 2, 5 * Time.deltaTime);

				Vector3 offsetEulerAngles = euler_0_180 (shoulder.localEulerAngles) - euler_0_180 (lastShoulderEulerAngles);
				accOffsetEulerAngles += (offsetEulerAngles.magnitude > 1.0f) ? (Vector3.zero) : (offsetEulerAngles);
				weapon.transform.localEulerAngles = LerpEulerAngles (weapon.transform.localEulerAngles, euler, 5 * Time.deltaTime);

				lastShoulderPosition = shoulder.localPosition;
				lastShoulderEulerAngles = shoulder.localEulerAngles;

				shoulder.SetParent (shoulderParent);
			}
		}
	}

	void LateUpdate () {
		if (weapon)
			hadWeapon = weapon.gameObject.activeSelf;
	}
		
	void OnAnimatorIK () {
		if (weapon) {
			anim.SetIKPositionWeight (AvatarIKGoal.RightHand, 1);
			anim.SetIKRotationWeight (AvatarIKGoal.RightHand, 1);  
			anim.SetIKPosition (AvatarIKGoal.RightHand, weapon.right.position);
			anim.SetIKRotation (AvatarIKGoal.RightHand, weapon.right.rotation);
			anim.SetIKPositionWeight (AvatarIKGoal.LeftHand, 1);
			anim.SetIKRotationWeight (AvatarIKGoal.LeftHand, 1);
			anim.SetIKPosition (AvatarIKGoal.LeftHand, weapon.left.position);
			anim.SetIKRotation (AvatarIKGoal.LeftHand, weapon.left.rotation);
			for (int x = 0; x < transform.childCount; x++)
				if (hadWeapon != weapon.gameObject.activeSelf)
					transform.GetChild (x).gameObject.SetActive (false);
				else
					transform.GetChild (x).gameObject.SetActive (true);
		} else {
			anim.SetIKPositionWeight (AvatarIKGoal.RightHand, 0);
			anim.SetIKRotationWeight (AvatarIKGoal.RightHand, 0);
			anim.SetIKPositionWeight (AvatarIKGoal.LeftHand, 0);
			anim.SetIKRotationWeight (AvatarIKGoal.LeftHand, 0);
		}
	}
}
