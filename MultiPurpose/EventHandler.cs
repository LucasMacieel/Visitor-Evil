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
			if (npc.target && npc.DistanceFromTarget () <= npc.interactRadius.y) {
				NPC other_npc = npc.target.root.GetComponent <NPC> ();
				PlayerController other_player = npc.target.root.GetComponent <PlayerController> ();
				if (other_npc) {
					// Hitting another NPC
					other_npc.health -= damage;
					other_npc.hit = true;
				} else if (other_player) {
					// Hitting the player
					if (other_player.hurt.Length > 0) {
						int randClip = UnityEngine.Random.Range (0, other_player.hurt.Length);
						other_player.audio.PlayOneShot (other_player.hurt[randClip], 1);
					}
					other_player.health -= damage;
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
		AudioSource src = transform.root.GetComponent <AudioSource> ();
		AudioClip clip = (AudioClip)e.objectReferenceParameter;
		if (!clip.name.ToLower ().Contains ("foot"))
			src.PlayOneShot (clip, e.floatParameter);
		else if (src && e.animatorClipInfo.weight > 0.5)
			src.PlayOneShot (clip, e.floatParameter);
	}
}
