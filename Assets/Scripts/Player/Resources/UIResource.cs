using UnityEngine;
using UnityEngine.UI;

public class UIResource : MonoBehaviour
{
    [Header("Referencia de la barra")]
    public Image fillImage; // La barra que se llena
    [SerializeField] private float lerpSpeed = 5f;

    private float targetAmount = 0f;
    private float maxAmount = 100f;

    void Update()
    {
        if (fillImage == null) return;

        float targetFill = targetAmount / maxAmount;
        fillImage.fillAmount = Mathf.MoveTowards(
            fillImage.fillAmount,
            targetFill,
            Time.deltaTime * lerpSpeed
        );
    }


    /// <summary>
    /// Se llama desde PlayerStats para actualizar la barra
    /// </summary>
    public void SetAmount(float amount, float max)
    {
        targetAmount = Mathf.Clamp(amount, 0, max);
        maxAmount = max;
    }
}
