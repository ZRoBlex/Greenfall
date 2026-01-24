using UnityEngine;

public class MaterialPickup : MonoBehaviour
{
    public float materialAmount = 15f;

    private void OnTriggerEnter(Collider other)
    {
        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats != null)
        {
            //stats.AddMaterials(materialAmount);
            Destroy(gameObject);
        }
    }
}
