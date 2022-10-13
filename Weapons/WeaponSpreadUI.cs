using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSpreadUI : MonoBehaviour {
	public enum spreadTypeEnum {crosshair, circular};
	public spreadTypeEnum spreadType = spreadTypeEnum.crosshair;
	public Color color = Color.white;
	[Range(0,1)]public float spread = 0.2f;
	[Range(0.01f,0.2f)]public float sizePercent = 0.1f;
	private RectTransform crosshair, circular;
	private RectTransform left, right, up, down, area;

	void Start () {
		crosshair = transform.Find ("Crosshair").GetComponent<RectTransform> ();
		left = crosshair.Find ("Left").GetComponent<RectTransform> ();
		right = crosshair.Find ("Right").GetComponent<RectTransform> ();
		up = crosshair.Find ("Up").GetComponent<RectTransform> ();
		down = crosshair.Find ("Down").GetComponent<RectTransform> ();

		circular = transform.Find ("Circular").GetComponent<RectTransform> ();
		area = circular.GetChild (0).GetComponent<RectTransform> ();
	}

	void Update () {
		if (spreadType == spreadTypeEnum.crosshair)
			crosshairSpread ();
		else
			circularSpread ();
	}

	void crosshairSpread () {
		int h = Screen.height;
		float offset = sizePercent / 2;

		if (circular.gameObject.activeSelf)
			circular.gameObject.SetActive (false);
		if (!crosshair.gameObject.activeSelf)
			crosshair.gameObject.SetActive (true);

		if (up.gameObject.activeSelf != spread > 0) {
			up.gameObject.SetActive (spread > 0);
			down.gameObject.SetActive (spread > 0);
			left.gameObject.SetActive (spread > 0);
			right.gameObject.SetActive (spread > 0);
		}

		up.sizeDelta = new Vector2 (offset / 10, offset) * h;
		down.sizeDelta = new Vector2 (offset / 10, offset) * h;
		left.sizeDelta = new Vector2 (offset, offset / 10) * h;
		right.sizeDelta = new Vector2 (offset, offset / 10) * h;

		up.localPosition = new Vector3 (0, spread + offset, 0) * h / 2;
		down.localPosition = new Vector3 (0, -spread - offset, 0) * h / 2;
		left.localPosition = new Vector3 (-spread - offset, 0, 0) * h / 2;
		right.localPosition = new Vector3 (spread + offset, 0, 0) * h / 2;

		up.GetComponent <Image> ().color = color;
		down.GetComponent <Image> ().color = color;
		left.GetComponent <Image> ().color = color;
		right.GetComponent <Image> ().color = color;
	}

	void circularSpread () {
		if (!circular.gameObject.activeSelf)
			circular.gameObject.SetActive (true);
		if (crosshair.gameObject.activeSelf)
			crosshair.gameObject.SetActive (false);

		if (area.gameObject.activeSelf != spread > 0)
			area.gameObject.SetActive (spread > 0);

		area.sizeDelta = Vector2.one * spread * Screen.height;
	}
}
