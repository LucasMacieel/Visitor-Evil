using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Range(0,60)]public float minTime, maxTime;
    private float currentTime;
    public GameObject[] npc;

    void Start () {
        currentTime = UnityEngine.Random.Range (minTime, maxTime);
    }
    // Update is called once per frame
    void Update() {
        if (currentTime <= 0 && npc.Length > 0) {
            currentTime = UnityEngine.Random.Range (minTime, maxTime);
            GameObject o = Instantiate (npc [UnityEngine.Random.Range (0, npc.Length)], transform.position, transform.rotation);
            NPC n = o.GetComponent <NPC> ();
            if (n && GameObject.FindGameObjectWithTag ("Player"))
                n.target = GameObject.FindGameObjectWithTag ("Player").GetComponent <Transform> ();
        }
        currentTime -= Time.deltaTime;
    }
}
