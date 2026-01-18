using UnityEngine;
using UnityEngine.UI;

public class DynamicCrosshair : MonoBehaviour
{
    [Header("Parts")]
    [SerializeField] RectTransform top;
    [SerializeField] RectTransform bottom;
    [SerializeField] RectTransform left;
    [SerializeField] RectTransform right;
    [SerializeField] Image centerDot;

    [Header("Images")]
    [SerializeField] Image topImg;
    [SerializeField] Image bottomImg;
    [SerializeField] Image leftImg;
    [SerializeField] Image rightImg;

    CrosshairProfile profile;

    float currentGap;
    float targetGap;

    float recoilAccumulated;


    public static DynamicCrosshair Instance { get; private set; }

    // =========================
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
    void Update()
    {
        if (profile == null) return;

        currentGap = Mathf.Lerp(
            currentGap,
            targetGap,
            Time.deltaTime * profile.recoverSpeed
        );

        ApplyGap(currentGap);
    }

    // =========================
    public void SetProfile(CrosshairProfile newProfile)
    {
        profile = newProfile;

        if (profile == null || !profile.showCrosshair)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        ApplyVisuals();

        currentGap = profile.baseGap;
        targetGap = profile.baseGap;

        ApplyGap(currentGap);
    }

    // =========================
    void ApplyVisuals()
    {
        Color c = profile.crosshairColor;
        c.a = profile.alpha;

        SetImage(topImg, c, profile.lineSprite);
        SetImage(bottomImg, c, profile.lineSprite);
        SetImage(leftImg, c, profile.lineSprite);
        SetImage(rightImg, c, profile.lineSprite);

        ResizeLine(top, true);
        ResizeLine(bottom, true);
        ResizeLine(left, false);
        ResizeLine(right, false);

        if (centerDot)
        {
            centerDot.gameObject.SetActive(profile.showCenterDot);
            centerDot.color = c;
            centerDot.sprite = profile.centerDotSprite;
            centerDot.rectTransform.sizeDelta =
                Vector2.one * profile.centerDotSize;
        }
    }

    void SetImage(Image img, Color color, Sprite sprite)
    {
        if (!img) return;
        img.color = color;
        if (sprite) img.sprite = sprite;
    }

    void ResizeLine(RectTransform rt, bool vertical)
    {
        if (!rt) return;

        rt.sizeDelta = vertical
            ? new Vector2(profile.lineThickness, profile.lineLength)
            : new Vector2(profile.lineLength, profile.lineThickness);
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
        if (profile == null) return;

        targetGap = Mathf.Clamp(
            targetGap + profile.shootGapIncrease,
            profile.baseGap,
            profile.maxGap
        );
    }

    // =========================
    public void SetMovementSpread(float normalizedSpeed)
    {
        if (profile == null) return;

        float moveGap = normalizedSpeed * profile.moveGapIncrease;

        targetGap = Mathf.Clamp(
            profile.baseGap + moveGap,
            profile.baseGap,
            profile.maxGap
        );
    }

    // =========================
    public void ResetSpread()
    {
        if (profile == null) return;
        targetGap = profile.baseGap;
    }

    public void AddRecoil(float recoilAmount)
    {
        if (profile == null) return;

        recoilAccumulated += recoilAmount;

        targetGap = Mathf.Clamp(
            profile.baseGap + recoilAccumulated * profile.shootGapIncrease,
            profile.baseGap,
            profile.maxGap
        );
    }

    //void OnGUI()
    //{
    //    GUI.Label(new Rect(10, 10, 300, 20),
    //        $"Recoil Accumulated: {recoilAccumulated:F2}");
    //}

}
