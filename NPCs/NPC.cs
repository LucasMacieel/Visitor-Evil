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
	[Range(0.3f,1.0f)]public float attackInterval = 1;
	private float timeWithoutAttacks = 0;

	private NavMeshAgent nav;
	private Vector3 destination;
	private Animator anim;

	public bool aggressive;
	public AudioClip[] hurt;
	[HideInInspector]public bool hit;
	[HideInInspector]public string state;


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

	public float DistanceFromTarget () {
		if (target) {
			Vector3 headPos = transform.position + Vector3.up * nav.height;
			Vector3 feetPos = transform.position;
			CharacterController c = target.root.GetComponent <CharacterController> ();
			NavMeshAgent n = target.root.GetComponent <NavMeshAgent> ();
			float distance = Vector3.Distance (headPos, target.root.transform.position);
			float distance1 = Vector3.Distance (feetPos, target.root.transform.position);
			float distance2 = Mathf.Infinity;
			float distance3 = Mathf.Infinity;
			if (c) {
				distance2 = Vector3.Distance (headPos, target.root.transform.position + Vector3.up * c.height);
				distance3 = Vector3.Distance (feetPos, target.root.transform.position + Vector3.up * c.height);
			} else if (n) {
				distance2 = Vector3.Distance (headPos, target.root.transform.position + Vector3.up * n.height);
				distance3 = Vector3.Distance (feetPos, target.root.transform.position + Vector3.up * n.height);
			}
			return Mathf.Min(distance, Mathf.Min(distance1, Mathf.Min(distance2, distance3)));
		} else
			return Mathf.Infinity;
	}

	void FollowTarget () {
		if (health > 0) {
			if (!nav.enabled)
				nav.enabled = true;
			if (target) {
				// Near the target position
				float distanceFromTarget = DistanceFromTarget ();
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
		if (health > 0 && state != "hit" && aggressive && target) {
			float distanceFromTarget = DistanceFromTarget ();
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
		if (health > 0 && hit) {
			if (aggressive) {
				GameObject p = GameObject.FindGameObjectWithTag ("Player");
				if (p && target != p.transform)
					target = p.transform;
			}

			if (state != "hit") {
				state = "hit";
				anim.Play ("Hit", anim.GetLayerIndex ("Hit"), 0);
				if (hurt.Length > 0) {
					int randClip = UnityEngine.Random.Range (0, hurt.Length);
					transform.root.GetComponent <AudioSource> ().PlayOneShot (hurt[randClip], 1);
				}
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
				if (anim.GetCurrentAnimatorStateInfo (i).normalizedTime >= 1) {
					anim.transform.SetParent (null);
					Destroy (anim);
					Destroy (gameObject);
				}
			}
		}
	}
}
