using UnityEngine;

[CreateAssetMenu(menuName = "FPS/Crosshair Profile")]
public class CrosshairProfile : ScriptableObject
{
    [Header("Visibility")]
    public bool showCrosshair = true;
    public bool hideWhenAiming = false;
    public bool showCenterDot = true;

    [Header("Color")]
    public Color crosshairColor = Color.white;
    public float alpha = 1f;

    [Header("Gap")]
    public float baseGap = 8f;
    public float maxGap = 35f;

    [Header("Line Size")]
    public float lineLength = 12f;
    public float lineThickness = 2f;

    [Header("Center Dot")]
    public float centerDotSize = 3f;

    [Header("Sprites")]
    public Sprite lineSprite;
    public Sprite centerDotSprite;

    [Header("Shoot Reaction")]
    public float shootGapIncrease = 6f;

    [Header("Movement Reaction")]
    public float moveGapIncrease = 8f;

    [Header("Recovery")]
    public float recoverSpeed = 12f;

    [Header("Mode")]
    public bool isADS = false;

    [Header("ADS Options")]
    public bool hideLines = false;
    public bool hideCenterDot = false;
    public bool lockGap = false;
    public float lockedGap = 0f;

    [Header("Movement States")]
    public float airborneGapIncrease = 12f;
    public float crouchGapMultiplier = 0.65f;


}
