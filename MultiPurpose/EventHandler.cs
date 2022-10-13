using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventHandler : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}

	void Hurt (int damage) {
		NPC npc = transform.root.GetComponent <NPC> ();
		PlayerController player = transform.root.GetComponent <PlayerController> ();

		if (npc) {
			// I am an NPC
			damage = Mathf.CeilToInt (npc.damageMultiplier * (float)damage);
			if (npc.target && Vector3.Distance (npc.transform.position, npc.target.transform.position) <= npc.interactRadius.y) {
				NPC other_npc = npc.target.GetComponent <NPC> ();
				PlayerController other_player = transform.root.GetComponent <PlayerController> ();
				if (other_npc) {
					// Hitting another NPC
					other_npc.health -= damage;
					other_npc.hit = true;
				} else if (other_player) {
					// Hitting the player

				}
			}
		} else if (player) {
			// I am a player hitting something
			GameObject k = player.weapons1 [player.currentWeapon];
			Knife knife = (k) ? (k.GetComponent <Knife> ()) : (null);
			if (knife)
				knife.Attack ();
		}
	}

	public void Sound (AnimationEvent e) {
		//NPC npc = transform.root.GetComponent <NPC> ();
		PlayerController player = transform.root.GetComponent <PlayerController> ();
		if (player) {
			player.audio.PlayOneShot ((AudioClip)e.objectReferenceParameter, e.floatParameter);

			// I am a player making noise
			/*
			Knife knife = (k) ? (k.GetComponent <Knife> ()) : (null);
			if (knife)
				knife.Attack ();*/
		}
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
