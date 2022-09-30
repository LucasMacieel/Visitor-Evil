using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {
	[Range(1,1000)]public float speed = 5;
	[Range(1,30)]public float travelDuration = 10;
	[Range(1,30)]public float holeDuration = 20;
	public GameObject bullet, hole;
	private float lastTravelDuration;

	private bool hit = false;

	Vector3 closestPoint (RaycastHit[] hits) {
		float minDistance = Mathf.Infinity;
		Vector3 point = new Vector3 (Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
		for (int x = 0; x < hits.Length; x++) {
			if (hits [x].distance < minDistance && !hits [x].transform.root.GetComponent<PlayerController> () && !hits [x].collider.isTrigger) {
				point = hits [x].point;
				minDistance = hits [x].distance;
			}
		}
		return point;
	}

	Transform closestTransform (RaycastHit[] hits) {
		float minDistance = Mathf.Infinity;
		Transform t = null;
		for (int x = 0; x < hits.Length; x++) {
			if (hits [x].distance < minDistance && !hits [x].transform.root.GetComponent<PlayerController> () && !hits [x].collider.isTrigger) {
				t = hits [x].collider.transform;
				minDistance = hits [x].distance;
			}
		}
		return t;
	}

	void Start () {
		bullet.SetActive (true);
		hole.SetActive (false);
		lastTravelDuration = travelDuration;
	}

	void Update () {
		if (holeDuration * travelDuration <= 0)
			Destroy (gameObject);
		if (!hit) {
			float maxReachableDistance = speed * Time.deltaTime;
			RaycastHit[] rays = Physics.RaycastAll (transform.position, transform.forward, maxReachableDistance * 2);
			Vector3 point = closestPoint (rays);
			Vector3 dist = point - transform.position;

			if (dist.magnitude < maxReachableDistance * 2) {
				float offset = 0.005f;
				Vector3 lPos = transform.position - transform.right * offset;
				Vector3 rPos = transform.position + transform.right * offset;
				Vector3 uPos = transform.position + transform.up * offset;
				Vector3 dPos = transform.position - transform.up * offset;
				Vector3 l = closestPoint (Physics.RaycastAll (lPos, transform.forward, maxReachableDistance * 2)) - lPos;
				Vector3 r = closestPoint (Physics.RaycastAll (rPos, transform.forward, maxReachableDistance * 2)) - rPos;
				Vector3 u = closestPoint (Physics.RaycastAll (uPos, transform.forward, maxReachableDistance * 2)) - uPos;
				Vector3 d = closestPoint (Physics.RaycastAll (dPos, transform.forward, maxReachableDistance * 2)) - dPos;

				if (l.magnitude * r.magnitude * u.magnitude * d.magnitude != Mathf.Infinity) {
					float horizontal = Mathf.Atan ((l.magnitude - r.magnitude) / (2 * offset)) * Mathf.Rad2Deg;
					float vertical = Mathf.Atan ((d.magnitude - u.magnitude) / (2 * offset)) * Mathf.Rad2Deg;
					transform.eulerAngles += new Vector3 (-vertical, horizontal, 0);
					transform.position = point;
					Transform t = closestTransform (rays);
					transform.SetParent (t);
					hit = true;
					bullet.SetActive (false);
					hole.SetActive (true);
				}
			} else {
				transform.position += transform.forward * speed * Time.deltaTime;
				travelDuration -= Time.deltaTime;
			}

			if (!hit && lastTravelDuration == travelDuration)
				Destroy (gameObject);
			lastTravelDuration = travelDuration;
		} else {
			holeDuration -= Time.deltaTime;
			if (!transform.parent)
				Destroy (gameObject);
		}
	}
}
