using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour {

	public GameObject worldInstance; // Como aparece no mapa, e.g. arma
	public GameObject playerInstance; // Como aparece com o player, e.g. arma com braços
	public enum itemTypeEnum {
		weapon,     // Arma para o jogador
		ammo,       // Munição
		healer,     // Recupera HP
		card        // Abrir portas
	}
	// Máxima quantidade que pode ser estocada em um slot
	[Range(1,200)]public int maxAmountPerSlot = 1, currentAmount;
	public itemTypeEnum itemType = itemTypeEnum.card;
	[Range(1,100)]public int value = 20;
	public Item master;

	Rigidbody rb;

	void Start () {
		rb = GetComponent<Rigidbody> ();
		onPlayerDrop ();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	// Se o player ainda tiver espaço
	bool canBePicked () {
		return false;
	}

	void onPlayerDrop () {
		if (transform.parent != null) {
			Transform p = playerInstance.transform.root;
			transform.position = p.position + p.forward + (Vector3.up * 0.3f);
			transform.eulerAngles = playerInstance.transform.eulerAngles;
			transform.SetParent (null);
		}
		if (rb) {
			rb.detectCollisions = true;
		}
		if (worldInstance != null) {
			worldInstance.transform.localPosition = Vector3.zero;
			worldInstance.transform.localEulerAngles = Vector3.zero;
			worldInstance.gameObject.SetActive (true);
		}
		if (playerInstance != null) {
			playerInstance.transform.localPosition = Vector3.zero;
			playerInstance.transform.localEulerAngles = Vector3.zero;
			playerInstance.gameObject.SetActive (false);
		}
	}

	void onPlayerPick () {
		PlayerControllerI p = transform.root.GetComponent <PlayerControllerI> ();
		/*if (p) {
			bool added = false;
			switch (itemType) {
			// Munição
			case itemTypeEnum.ammo:
				for (int i = 0; i < p.inventory.Count; i++) {
					if (p.inventory [i].text == text && p.inventory [i].currentAmount < maxAmountPerSlot) {
						if (p.inventory [i].currentAmount + currentAmount > maxAmountPerSlot)
							p.inventory [i].currentAmount = maxAmountPerSlot;
						else
							p.inventory [i].currentAmount += currentAmount;
						added = true;
						break;
					}
				}
				break;
			// Fragmento
			case itemTypeEnum.fragment:
				for (int i = 0; i < p.inventory.Count; i++) {
					if (p.inventory [i].text == text && p.inventory [i].currentAmount < maxAmountPerSlot) {
						if (p.inventory [i].currentAmount + currentAmount > maxAmountPerSlot) {
							Destroy (p.inventory [i].gameObject);
							GameObject m = Instantiate (master.gameObject, p.transform.position, p.transform.rotation, p.transform);
							m.GetComponent <Item> ().onPlayerPick ();
							p.inventory [i] = master;
						} else
							p.inventory [i].currentAmount += currentAmount;
						added = true;
						break;
					}
				}
				break;
			default:
				added = true;
			}

			if (added)
				Destroy (gameObject);
			else
				p.inventory.Add (this);
		}*/
	}
	void hide () {
		for (int i = 0; i < transform.childCount; i++)
			transform.GetChild (i).gameObject.SetActive (false);
	}
}
