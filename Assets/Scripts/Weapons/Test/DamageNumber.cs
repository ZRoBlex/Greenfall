using UnityEngine;
using TMPro;

public class DamageNumber : MonoBehaviour
{
    public TextMeshPro text;

    [Header("Motion")]
    public Vector3 moveDirection = new Vector3(0, 1, 0);
    public float moveSpeed = 1.5f;

    [Header("Scale")]
    public float startScale = 0.2f;
    public float popScale = 1.2f;
    public float scaleSpeed = 10f;

    [Header("Fade")]
    public float lifetime = 1.2f;
    public float fadeDuration = 0.4f;

    Camera cam;
    float timer;
    Color baseColor;

    void Awake()
    {
        cam = Camera.main;
        baseColor = text.color;
        transform.localScale = Vector3.one * startScale;
    }

    void OnEnable()
    {
        timer = 0f;
        text.color = baseColor;
        transform.localScale = Vector3.one * startScale;
    }

    void Update()
    {
        // Movimiento
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // Mirar a cámara
        if (cam)
        {
            transform.rotation = Quaternion.LookRotation(
                transform.position - cam.transform.position
            );
        }

        // POP IN
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            Vector3.one * popScale,
            Time.deltaTime * scaleSpeed
        );

        timer += Time.deltaTime;

        // FADE OUT
        if (timer > lifetime - fadeDuration)
        {
            float t = (timer - (lifetime - fadeDuration)) / fadeDuration;
            Color c = text.color;
            c.a = Mathf.Lerp(baseColor.a, 0f, t);
            text.color = c;
        }

        if (timer >= lifetime)
        {
            Destroy(gameObject); // luego lo cambiamos por pool
        }
    }

    public void SetValue(float value)
    {
        text.text = Mathf.RoundToInt(value).ToString();
    }
}
