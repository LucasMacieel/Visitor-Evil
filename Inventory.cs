using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Inventory : MonoBehaviour {

	public Color backgroundColor = Color.black;
	public enum orientationEnum {left, right, center}
	public orientationEnum orientation = orientationEnum.right;
	[Range(1,10)]public int slotsX = 8, slotsY = 8;
	public Button slotPrefab;
	public List<ItemSlot> instancedSlots;

	public ItemSlot selected;
	private Vector3 startMousePos = Vector3.zero;
	private Vector3 startSlotPos = Vector3.zero;

	void Start () {
		int slotSize = Screen.height / slotsY;
		int h = Screen.height, w = Screen.width;
		int orient = 0;
		switch (orientation) {
		case orientationEnum.center: orient = -(slotSize * slotsX / 2); break;
		case orientationEnum.left: orient = -w / 2; break;
		case orientationEnum.right: orient = (w / 2) - (slotSize * slotsX); break;
		}
		for (int y = 0; y < slotsY; y++) {
			for (int x = 0; x < slotsX; x++) {
				int num = x + y * slotsX;
				RectTransform rect = Instantiate (slotPrefab, transform).GetComponent<RectTransform> ();
				rect.localPosition = new Vector3 ((h * x / slotsY) + orient + (slotSize / 2), (h * (slotsY - y) / slotsY) - (slotSize / 2) - (h / 2), 0);
				rect.sizeDelta = Vector2.one * slotSize;
				rect.gameObject.name = "Slot " + num.ToString ();
				ItemSlot slot = rect.GetComponent<ItemSlot> ();
				instancedSlots.Add (slot);
				rect.SetParent (null);

				Image background = Instantiate (rect.gameObject, transform).GetComponent<Image> ();
				Destroy (background.GetComponent<Button> ());
				Destroy (background.GetComponent<ItemSlot> ());
				Destroy (background.GetComponent<EventTrigger> ());
				for (int i = 0; i < background.transform.childCount; i++)
					Destroy (background.transform.GetChild (i).gameObject);
				background.gameObject.name = "Background " + num.ToString ();
				background.color = backgroundColor;
				rect.SetParent (transform);

				// Click events
				EventTrigger trigger = rect.GetComponent<EventTrigger> ();
				// Pointer down
				EventTrigger.Entry pointerDown = new EventTrigger.Entry ();
				pointerDown.eventID = EventTriggerType.PointerDown;
				pointerDown.callback.AddListener((data) => {slotDown(slot);});
				// Pointer over
				EventTrigger.Entry pointerEnter = new EventTrigger.Entry ();
				pointerEnter.eventID = EventTriggerType.PointerEnter;
				pointerEnter.callback.AddListener((data) => {slotEnter(num);});

				trigger.triggers.Add (pointerDown);
				trigger.triggers.Add (pointerEnter);
			}
		}
	}

	public void slotDown (ItemSlot slot) {
		selected = slot;
		startMousePos = Input.mousePosition;
		startSlotPos = slot.GetComponent<RectTransform> ().localPosition;
	}

	public void slotEnter (int i) {
		Debug.Log (i);
	}

	void Update () {
		bool mouse0Down = Input.GetKeyDown (KeyCode.Mouse0);
		bool mouse0Up = Input.GetKeyUp (KeyCode.Mouse0);
		bool mouse0On = Input.GetKey (KeyCode.Mouse0);

		if (selected) {
			Vector3 newPos = startSlotPos + Input.mousePosition - startMousePos;
			newPos.z = 0;
			selected.GetComponent<RectTransform> ().localPosition = newPos;
		}
	}
}
