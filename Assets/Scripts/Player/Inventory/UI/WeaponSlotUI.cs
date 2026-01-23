using UnityEngine;
using UnityEngine.UI;

public class WeaponSlotUI : MonoBehaviour
{
    [Header("Icon")]
    [SerializeField] Image icon;

    [Header("Background")]
    [SerializeField] Image background; // ← NUEVO: la imagen de fondo

    [Header("Lift Settings")]
    [SerializeField] float liftPixels = 15f;
    [SerializeField] float liftSpeed = 10f;

    Vector2 basePosIcon;
    Vector2 basePosBg; // ← NUEVO: posición base del fondo
    bool isSelected;
    bool initialized;

    void Update()
    {
        if (!initialized || icon == null || !icon.enabled)
            return;

        Vector2 targetPos = basePosIcon + (isSelected ? Vector2.up * liftPixels : Vector2.zero);
        icon.rectTransform.anchoredPosition = Vector2.Lerp(
            icon.rectTransform.anchoredPosition,
            targetPos,
            Time.deltaTime * liftSpeed
        );

        // ----------------------------
        // MOVEMOS EL FONDO JUNTOS
        if (background != null)
        {
            Vector2 targetBgPos = basePosBg + (isSelected ? Vector2.up * liftPixels : Vector2.zero);
            background.rectTransform.anchoredPosition = Vector2.Lerp(
                background.rectTransform.anchoredPosition,
                targetBgPos,
                Time.deltaTime * liftSpeed
            );
        }
    }

    // -----------------------------
    public void Set(Weapon weapon, bool selected)
    {
        if (weapon == null || weapon.icon == null)
        {
            Clear();
            return;
        }

        icon.enabled = true;
        icon.sprite = weapon.icon;

        isSelected = selected;

        // Guardamos la posición REAL solo una vez
        if (!initialized)
        {
            basePosIcon = icon.rectTransform.anchoredPosition;

            if (background != null)
                basePosBg = background.rectTransform.anchoredPosition;

            initialized = true;
        }
    }

    public void Clear()
    {
        if (icon != null)
            icon.enabled = false;

        isSelected = false;
    }
}
