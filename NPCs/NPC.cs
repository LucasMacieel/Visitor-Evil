using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPC : MonoBehaviour {

	public Transform target;
	[Range(0.5f,5)]public float damageMultiplier = 1;
	// Região circular em volta do alvo que pode ficar
	public Vector2 interactRadius = new Vector2 (1, 3);
	[Range(0.5f,1.0f)]public float attackInterval = 1;
	private float timeWithoutAttacks = 0;

	private NavMeshAgent nav;
	private Vector3 destination;
	private Animator anim;

	public bool aggressive, hit;

	public string state;

	private bool wasAttacking;

	[Range(0,1000)]public int health = 100;

	void lerpLayerWeight(string layer, float value, float speed) {
		int i = anim.GetLayerIndex (layer);
		anim.SetLayerWeight (i, Mathf.Lerp (anim.GetLayerWeight (i), value, speed * Time.deltaTime));
	}

	void setLayerWeight(string layer, float value) {
		int i = anim.GetLayerIndex (layer);
		anim.SetLayerWeight (i, value);
	}

	void Start () {
		nav = GetComponent<NavMeshAgent> ();
		anim = GetComponentInChildren<Animator> ();
		timeWithoutAttacks = attackInterval;
		state = "idle";
	}

	void Update () {
		FollowTarget ();
		Attacks ();
		CheckDeath ();
		CheckHit ();

		if (state == "dead") {
			lerpLayerWeight ("Death", 1, 5);
			lerpLayerWeight ("Attack", 0, 5);
			lerpLayerWeight ("Hit", 0, 5);
		} else if (state == "attack") {
			lerpLayerWeight ("Death", 0, 5);
			lerpLayerWeight ("Attack", 1, 5);
			lerpLayerWeight ("Hit", 0, 5);
		} else if (state == "hit") {
			lerpLayerWeight ("Death", 0, 5);
			lerpLayerWeight ("Attack", 0, 5);
			lerpLayerWeight ("Hit", 1, 5);
		} else {
			lerpLayerWeight ("Death", 0, 5);
			lerpLayerWeight ("Attack", 0, 5);
			lerpLayerWeight ("Hit", 0, 5);
		}
	}

	void FollowTarget () {
		if (state != "dead") {
			if (!nav.enabled)
				nav.enabled = true;
			if (target) {
				// Near the target position
				float distanceFromTarget = Vector3.Distance (transform.position, target.transform.position);
				if (distanceFromTarget > interactRadius.y) {
					float targetDisplacement = Vector3.Distance (target.position, destination);
					if (targetDisplacement > 1.0f) {
						destination = target.position;
						nav.SetDestination (destination);
					}
				} else if (distanceFromTarget < interactRadius.x) {
					if (nav.hasPath) {
						nav.ResetPath ();
						nav.enabled = false;
					}
				}
				// Face the target
				float currentEuler = transform.eulerAngles.y;
				transform.LookAt (target, Vector3.up);
				float newEuler = transform.eulerAngles.y;
				transform.eulerAngles = new Vector3 (0, Mathf.LerpAngle (currentEuler, newEuler, 10 * Time.deltaTime), 0);
			}
			float currentSpeed = anim.GetFloat ("Speed");
			float goalSpeed = Mathf.Clamp (nav.velocity.magnitude / 5, 0, 1);
			anim.SetFloat ("Speed", Mathf.Lerp (currentSpeed, goalSpeed, 3 * Time.deltaTime));
		}
	}

	void Attacks () {
		if (state != "dead" && state != "hit" && aggressive && target) {
			float distanceFromTarget = Vector3.Distance (transform.position, target.transform.position);
			int i = anim.GetLayerIndex ("Attack");
			if (state != "attack" && distanceFromTarget <= interactRadius.y && timeWithoutAttacks >= attackInterval) {
				state = "attack";
				anim.SetInteger ("Attack", UnityEngine.Random.Range (1, 4));
			} else if (state == "attack") {
				string name = anim.GetCurrentAnimatorClipInfo (i) [0].clip.name;
				if (!name.Contains ("Idle"))
					wasAttacking = true;
				else if (wasAttacking) {
					state = "idle";
					wasAttacking = false;
					anim.SetInteger ("Attack", 0);
				}
				timeWithoutAttacks = 0;
			}
		} else if (state == "attack" && !target)
			state = "idle";
		timeWithoutAttacks += Time.deltaTime;
	}

	void CheckHit () {
		if (state != "dead" && hit) {
			if (state != "hit") {
				state = "hit";
				anim.Play ("Hit", anim.GetLayerIndex ("Hit"), 0);
			} else {
				int i = anim.GetLayerIndex ("Hit");
				if (anim.GetCurrentAnimatorStateInfo (i).normalizedTime >= 0.95f) {
					state = "idle";
					hit = false;
				}
			}
		}
	}

	void CheckDeath () {
		if (health <= 0) {
			health = 0;
			int i = anim.GetLayerIndex ("Death");
			if (state != "dead") {
				state = "dead";
				string animation = "Death" + UnityEngine.Random.Range (1, 3).ToString ();
				anim.Play (animation, i, 0);
				Destroy (gameObject.GetComponent <Collider> ());
				nav.speed = 0;
			} else {
				if (anim.GetCurrentAnimatorStateInfo (i).normalizedTime >= 0.95f) {
					anim.transform.SetParent (null);
					Destroy (anim);
					Destroy (gameObject);
				}
			}
		}
	}
}
