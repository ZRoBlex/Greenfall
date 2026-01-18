using UnityEngine;

public class DynamicCrosshair : MonoBehaviour
{
    [Header("Crosshair Parts")]
    [SerializeField] RectTransform top;
    [SerializeField] RectTransform bottom;
    [SerializeField] RectTransform left;
    [SerializeField] RectTransform right;

    [Header("Base Settings")]
    [SerializeField] float baseGap = 8f;
    [SerializeField] float maxGap = 35f;

    [Header("Recoil")]
    [SerializeField] float shootIncrease = 6f;
    [SerializeField] float recoverSpeed = 12f;

    [Header("Movement")]
    [SerializeField] float moveIncrease = 8f;

    float currentGap;
    float targetGap;


    public static DynamicCrosshair Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    // =========================
    void Start()
    {
        currentGap = baseGap;
        targetGap = baseGap;
        ApplyGap(currentGap);
    }

    // =========================
    void Update()
    {
        currentGap = Mathf.Lerp(
            currentGap,
            targetGap,
            Time.deltaTime * recoverSpeed
        );

        ApplyGap(currentGap);
    }

    // =========================
    void ApplyGap(float gap)
    {
        top.anchoredPosition = new Vector2(0, gap);
        bottom.anchoredPosition = new Vector2(0, -gap);
        left.anchoredPosition = new Vector2(-gap, 0);
        right.anchoredPosition = new Vector2(gap, 0);
    }

    // =========================
    public void OnShoot()
    {
        targetGap = Mathf.Clamp(
            targetGap + shootIncrease,
            baseGap,
            maxGap
        );
    }

    // =========================
    public void SetMovementSpread(float normalizedSpeed)
    {
        float moveGap = normalizedSpeed * moveIncrease;
        targetGap = Mathf.Clamp(
            baseGap + moveGap,
            baseGap,
            maxGap
        );
    }

    // =========================
    public void ResetSpread()
    {
        targetGap = baseGap;
    }
}
