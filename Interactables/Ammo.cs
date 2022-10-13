using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ammo : MonoBehaviour {
	public enum ammoTypeEnum {pistol, rifle, shotgun, arrow, nothing}
	public ammoTypeEnum ammoType = ammoTypeEnum.pistol;
	[Range(1,100)]public int amount = 20;
}
