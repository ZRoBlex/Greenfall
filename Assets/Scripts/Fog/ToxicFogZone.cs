using UnityEngine;

public class ToxicFogZone : MonoBehaviour
{
    public FogManager fogManager;

    void OnTriggerEnter(Collider other)
    {
        //if (other.CompareTag("Player"))
        //    fogManager.SetToxic(true);
    }

    void OnTriggerExit(Collider other)
    {
        //if (other.CompareTag("Player"))
        //    fogManager.SetToxic(false);
    }
}
