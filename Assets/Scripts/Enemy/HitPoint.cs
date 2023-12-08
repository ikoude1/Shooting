using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitPoint : MonoBehaviour
{
    public int MAX_Damage;
    public int MIN_Damage;

    private void OnTriggerEnter(Collider other)
    {
        //Íæ¼ÒÊÜµ½ÉËº¦¿ÛÑª
        if (other.CompareTag("Player")) {
            
            other.GetComponent<PlayerController>().PlayerHealth( Random.Range(MIN_Damage,MAX_Damage) );

        }

    }

}
