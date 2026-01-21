using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AmmoPickupUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] Image ammoIcon;
    [SerializeField] TMP_Text ammoText;

    [Header("Animation")]
    [SerializeField] float floatUpSpeed = 30f;
    [SerializeField] float lifeTime = 1.5f;
    [SerializeField] float fadeSpeed = 2f;

    CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (!canvasGroup)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Setup(int amount, string ammoName, Sprite icon = null)
    {
        // Texto: +30 9mm
        if (ammoText)
            ammoText.text = $"+{amount} {ammoName}";

        // Imagen opcional
        if (ammoIcon)
        {
            if (icon != null)
            {
                ammoIcon.sprite = icon;
                ammoIcon.enabled = true;
            }
            else
            {
                ammoIcon.enabled = false; // 👈 si no hay sprite, se oculta
            }
        }

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Flotar hacia arriba
        transform.Translate(Vector3.up * floatUpSpeed * Time.deltaTime);

        // Fade out
        if (canvasGroup)
        {
            canvasGroup.alpha = Mathf.Lerp(
                canvasGroup.alpha,
                0f,
                fadeSpeed * Time.deltaTime
            );
        }
    }
}
