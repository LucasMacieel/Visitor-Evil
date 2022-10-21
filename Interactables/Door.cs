using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class movablePart {
	public GameObject associatedObject;
	[Header("Position")]
	public Vector3 closedPosition;
	public Vector3 openPosition;
	[Header("Rotation")]
	public Vector3 closedEuler;
	public Vector3 openEuler;
}

public class Door : MonoBehaviour {

	public DoorLocker locker;
	private Transform player;
	public movablePart[] parts;
	public bool open = false;

	Vector3 lerpEuler (Vector3 a, Vector3 b, float t) {
		float x = Mathf.LerpAngle (a.x, b.x, t);
		float y = Mathf.LerpAngle (a.y, b.y, t);
		float z = Mathf.LerpAngle (a.z, b.z, t);
		return new Vector3 (x, y, z);
	}

	void Update () {
		for (int x = 0; x < parts.Length; x++) {
			Transform t = parts [x].associatedObject.transform;
			Vector3 pos = (open) ? (parts [x].openPosition) : (parts [x].closedPosition);
			t.localPosition = Vector3.Lerp (t.localPosition, pos, 5 * Time.deltaTime);

			Vector3 euler = (open) ? (parts [x].openEuler) : (parts [x].closedEuler);
			t.localEulerAngles = lerpEuler (t.localEulerAngles, euler, 5 * Time.deltaTime);
		}
	}

	void OnTriggerStay(Collider other) {
		if (locker)
			open = false;
		else if (other.transform.root.GetComponent<NPC> () || other.transform.root.GetComponent<PlayerController> ())
			open = true;
	}

	void OnTriggerExit(Collider other) {
		open = false;
	}
}
