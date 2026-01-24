using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIMaterialSlot : MonoBehaviour
{
    [Header("UI")]
    public Image iconImage;
    public Image fillImage; // opcional si quieres barra
    public TMP_Text amountText;

    private float currentAmount;
    private float maxAmount;

    public void SetIcon(Sprite icon)
    {
        if (iconImage != null)
            iconImage.sprite = icon;
    }

    public void SetAmount(float amount, float max)
    {
        currentAmount = Mathf.Clamp(amount, 0, max);
        maxAmount = max;

        // 🔢 Texto: "35 / 100"
        if (amountText != null)
            amountText.text = $"{Mathf.FloorToInt(currentAmount)}";

        // 📊 Barra opcional
        if (fillImage != null)
            fillImage.fillAmount = currentAmount / maxAmount;
    }
}
