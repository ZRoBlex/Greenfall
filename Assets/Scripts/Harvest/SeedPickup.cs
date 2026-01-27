using UnityEngine;

public class SeedPickup : MonoBehaviour
{
    public SeedItem seedData;
    public int amount = 1;

    public void Initialize(SeedItem seed, int amt)
    {
        seedData = seed;
        amount = amt;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // 👉 aquí luego conectas con inventario real
        Debug.Log($"Picked up {amount}x {seedData.seedId}");

        Destroy(gameObject);
    }
}
