using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Healer : MonoBehaviour {

	[Range(1,100)]public int health = 50;
	private Animator anim;

	void Start () {
		anim = GetComponentInChildren <Animator> ();
	}
	
	void Update () {
		if (anim.GetCurrentAnimatorStateInfo (0).normalizedTime >= 1){
			Destroy (gameObject);
			PlayerController p = transform.root.GetComponent <PlayerController> ();
			if (p)
				p.health = (p.health + health >= p.maxHealth) ? (p.maxHealth) : (p.health + health);
		}
	}
}
