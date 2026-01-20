using UnityEngine;

public class AmmoBox : MonoBehaviour
{
    [Header("Random Ammo")]
    public int minAmmo = 5;
    public int maxAmmo = 30;

    private int currentAmount;

    void OnEnable()
    {
        // Generar SOLO cuando se activa
        currentAmount = Random.Range(minAmmo, maxAmmo + 1);
        Debug.Log($"[AmmoBox] Generada con {currentAmount} balas");
    }

    // Esto lo llamará el jugador al interactuar
    public void Interact()
    {
        Debug.Log($"[AmmoBox] Jugador recogió {currentAmount} balas");

        // Aquí después conectarás al inventario

        // Desactivar para pooling
        gameObject.SetActive(false);
    }
}
