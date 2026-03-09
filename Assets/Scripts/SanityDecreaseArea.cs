using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SanityDecreaseArea : MonoBehaviour
{
    [Header("References")]

    [SerializeField] SanityManager sanityManager;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            sanityManager.enableRegeneration = false;
            sanityManager.StartDecreasing();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            sanityManager.enableRegeneration = true;
            sanityManager.StopDecreasing();
        }
    }
}
