using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    public TextMeshPro text;
    public float floatSpeed = 1.5f;
    public float lifetime = 1f;

    Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        // Flotar hacia arriba
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // Mirar siempre a la cámara
        transform.LookAt(
            transform.position + cam.transform.forward
        );
    }

    public void SetValue(float dmg)
    {
        text.text = Mathf.RoundToInt(dmg).ToString();
        Destroy(gameObject, lifetime);
    }
}
