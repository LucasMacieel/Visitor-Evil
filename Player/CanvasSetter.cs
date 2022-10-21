using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CanvasSetter : MonoBehaviour
{
    public Text weaponInfo, collectableInfo;
	public Image healthInfo;
	public WeaponSpreadUI spreadUI;

    public Text[] txt;
    public GameObject mainMenu, settings, credits;

    void Awake() {
        if (GameObject.FindGameObjectWithTag ("Player")) {
            PlayerController p = GameObject.FindGameObjectWithTag ("Player").GetComponent <PlayerController> ();
            weaponInfo.fontSize = Screen.height / 15;
            collectableInfo.fontSize = Screen.height / 25;
            p.weaponInfo = weaponInfo;
            p.collectableInfo = collectableInfo;
            p.healthInfo = healthInfo;
            p.spreadUI = spreadUI;
            Destroy (this);
        }
        for (int x = 0; x < txt.Length; x++)
            txt [x].fontSize = Screen.height / 20;
        Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
    }

    public void gotoScreen (string destination) {
        mainMenu.SetActive (false);
        settings.SetActive (false);
        credits.SetActive (false);
        if (destination == "menu")
            mainMenu.SetActive (true);
        else if (destination == "credits")
            credits.SetActive (true);
        else if (destination == "settings")
            settings.SetActive (true);
    }

    public void loadGame () {
        SceneManager.LoadScene("Game");
    }
}
