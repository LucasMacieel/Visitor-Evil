using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnifeHurtbox : MonoBehaviour
{
    private int frames = 0;
    [HideInInspector]public int damage = 0;
    private bool used = false;

    // Update is called once per frame
    void Update()
    {
        if (frames >= 5 || used)
            Destroy (gameObject);
        frames++;
    }

    void OnTriggerStay(Collider other) {
        if (!used) {
            NPC npc = other.transform.root.GetComponent <NPC> ();
            if (other.transform.root.tag == "Enemy" && npc) {
                npc.hit = true;
                npc.health -= damage;
                used = true;
            }
        }
    }
}
