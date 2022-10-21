using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathLerper : MonoBehaviour
{
    
    [HideInInspector]public Transform head;
    private bool apply = false;
    private float returnToMenu = 5;

    Transform findDeepChild (Transform parent, string name) {
		for (int x = 0; x < parent.childCount; x++) {
			if (parent.GetChild (x).name.ToLower () == name.ToLower ())
				return parent.GetChild (x);
			else {
				Transform otherChild = findDeepChild (parent.GetChild (x), name);
				if (otherChild != null)
					return otherChild;
			}
		}
		return null;
	}

    void Update() {
        if (head) {
            if (!apply) {
                apply = true;
                head.SetParent (findDeepChild(transform, "head"));
            }
            head.transform.localPosition = Vector3.Lerp(head.transform.localPosition, Vector3.zero, 5 * Time.deltaTime);
            head.transform.localRotation = Quaternion.Lerp(head.transform.localRotation, Quaternion.identity, 5 * Time.deltaTime);
        }
        returnToMenu -= Time.deltaTime;
        if (returnToMenu <= 0)
            SceneManager.LoadScene("Menu");
    }
}
