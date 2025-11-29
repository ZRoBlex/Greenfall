using UnityEngine;

public class ParticleShrinkOnHit : MonoBehaviour
{
    public float shrinkSpeed = 10f;     // qué tan rápido se hace pequeña
    public float destroyThreshold = 0.05f; // tamaño mínimo antes de destruirla
    bool shrinking = false;

    void Update()
    {
        if (!shrinking) return;

        // Escala uniforme
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            Vector3.zero,
            Time.deltaTime * shrinkSpeed
        );

        // Si ya es muy pequeña → destruir
        if (transform.localScale.x < destroyThreshold)
        {
            Destroy(gameObject);
        }
    }

    public void StartShrink()
    {
        shrinking = true;
    }
}
