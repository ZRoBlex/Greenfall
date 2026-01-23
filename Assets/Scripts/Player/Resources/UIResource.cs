using UnityEngine;
using UnityEngine.UI;

public class UIResource : MonoBehaviour
{
    [Header("Settings")]
    public UIResourceSO resourceSO;
    [SerializeField] Image fillImage; // Para barras o iconos que se llenan
    [SerializeField] float lerpSpeed = 5f;

    [Header("Optional Icon")]
    [SerializeField] Image iconImage;

    private float currentAmount;

    void Start()
    {
        currentAmount = resourceSO.maxAmount;

        if (iconImage != null)
            iconImage.sprite = resourceSO.icon;

        UpdateUIImmediate();
    }

    void Update()
    {
        if (fillImage != null)
        {
            float targetFill = currentAmount / resourceSO.maxAmount;
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.deltaTime * lerpSpeed);
        }
    }

    public void SetAmount(float amount)
    {
        currentAmount = Mathf.Clamp(amount, 0, resourceSO.maxAmount);
    }

    private void UpdateUIImmediate()
    {
        if (fillImage != null)
            fillImage.fillAmount = currentAmount / resourceSO.maxAmount;
    }
}
