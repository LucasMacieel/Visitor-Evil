using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMaster : MonoBehaviour {

	void Start () {
		LoadGame ();
	}

	void Update () {
		if (Input.GetKeyDown (KeyCode.Q))
			SaveGame ();
	}

	void SetVector3 (string key, Vector3 value) {
		PlayerPrefs.SetFloat (key + "_x", value.x);
		PlayerPrefs.SetFloat (key + "_y", value.y);
		PlayerPrefs.SetFloat (key + "_z", value.z);
	}

	Vector3 GetVector3 (string key, Vector3 defaultValue) {
		float x = PlayerPrefs.GetFloat (key + "_x", defaultValue.x);
		float y = PlayerPrefs.GetFloat (key + "_y", defaultValue.y);
		float z = PlayerPrefs.GetFloat (key + "_z", defaultValue.z);
		return new Vector3 (x, y, z);
	}

	void SaveGame () {
		GameObject player = GameObject.FindGameObjectWithTag ("Player");
		SetVector3 ("player", player.transform.position);
		PlayerPrefs.SetFloat ("player_horizontal", player.transform.eulerAngles.y);
		PlayerPrefs.SetFloat ("player_vertical", player.GetComponentInChildren <Camera> ().transform.localEulerAngles.x);
		PlayerPrefs.Save ();
	}

	void LoadGame () {
		GameObject player = GameObject.FindGameObjectWithTag ("Player");
		player.transform.position = GetVector3 ("player", Vector3.zero);
		float horizontal = PlayerPrefs.GetFloat ("player_horizontal", 0);
		float vertical = PlayerPrefs.GetFloat ("player_vertical", 0);
		player.transform.eulerAngles = new Vector3 (0, horizontal, 0);
		player.GetComponentInChildren <Camera> ().transform.localEulerAngles = new Vector3 (vertical, 0, 0);
	}
}
