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

    [Header("Spread")]
    public float virtualDistance = 15000f;

    CrosshairProfile profile;
    public CrosshairProfile Profile
    {
        get => profile;
        set => profile = value;
    }


    float currentGap;

    // 🔹 fuentes independientes
    float movementGap;
    float weaponGap;
    float airborneGap;
    float crouchMultiplier = 1f;



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

    void Update()
    {
        if (profile == null) return;

        float targetGap = Mathf.Clamp(
            profile.baseGap + movementGap + weaponGap,
            profile.baseGap,
            profile.maxGap
        );

        currentGap = Mathf.Lerp(
            currentGap,
            targetGap,
            Time.deltaTime * profile.recoverSpeed
        );

        // el spread del arma se disipa SOLO
        weaponGap = Mathf.Lerp(weaponGap, 0f, Time.deltaTime * profile.recoverSpeed);

        ApplyGap(currentGap);

        float finalGap =
    profile.baseGap +
    movementGap +
    weaponGap +
    airborneGap;

        finalGap *= crouchMultiplier;

        targetGap = Mathf.Clamp(
            finalGap,
            profile.baseGap,
            profile.maxGap
        );

    }

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
        movementGap = 0f;
        weaponGap = 0f;
    }

    // ================= VISUALS =================

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

    void ApplyGap(float gap)
    {
        top.anchoredPosition = new Vector2(0, gap);
        bottom.anchoredPosition = new Vector2(0, -gap);
        left.anchoredPosition = new Vector2(-gap, 0);
        right.anchoredPosition = new Vector2(gap, 0);
    }

    // ================= MOVEMENT =================

    public void SetMovementSpread(float normalizedSpeed)
    {
        if (profile == null) return;
        movementGap = normalizedSpeed * profile.moveGapIncrease;
    }

    // ================= WEAPON =================

    public void ApplyWeaponSpread(float spreadAngle)
    {
        if (profile == null) return;

        float radians = spreadAngle * Mathf.Deg2Rad;
        float spreadRadius = Mathf.Tan(radians) * virtualDistance;

        weaponGap = Mathf.Clamp(
            spreadRadius,
            0f,
            profile.maxGap
        );
    }

    public void SetAirborne(bool airborne)
    {
        if (profile == null) return;

        airborneGap = airborne ? profile.airborneGapIncrease : 0f;
    }
    public void SetCrouch(bool crouching)
    {
        crouchMultiplier = crouching ? profile.crouchGapMultiplier : 1f;
    }

}
