using UnityEngine;

public enum FireMode
{
    SemiAuto,
    FullAuto,
    Burst
}

[CreateAssetMenu(menuName = "Greenfall/Weapons/NonLethal Weapon")]
public class WeaponStats : ScriptableObject
{
    [Header("Fire Mode")]
    public FireMode fireMode = FireMode.SemiAuto;
    public float cooldown = 0.1f;

    [Header("Burst")]
    public int burstCount = 3;
    public float burstDelay = 0.08f;

    [Header("Raycast")]
    public float range = 150f;
    public int pellets = 1;
    public float spreadAngle = 1.5f;
    public LayerMask hittableLayers;
    public string[] damageTags;

    //[Header("Damage")]
    //public bool useCaptureDamage = true;
    //public float lethalDamage = 10f;
    //public float stunAmount = 15f;
    //public float captureTick = 20f;
    [Header("Damage Range")]
    public bool useCaptureDamage = true;

    // 🔴 LETHAL
    public float minLethalDamage = 8f;
    public float maxLethalDamage = 12f;

    // 🟡 CAPTURE
    public float minCaptureDamage = 15f;
    public float maxCaptureDamage = 25f;

    // ⭐ CRITICAL
    public bool allowCritical = true;
    [Range(0f, 1f)]
    public float criticalChance = 0.15f; // 15%
    [Range(0f, 0.5f)]
    public float criticalBonusPercent = 0.5f; // +50% máx


    [Header("Line Renderer")]
    public LineRenderer linePrefab;
    public Material normalLine;
    public Material hitLine;
    public float lineDuration = 0.05f;

    //[Header("Impact FX")]
    //public GameObject metalImpact;
    //public GameObject dirtImpact;
    //public GameObject fleshImpact;

    [Header("Recoil")]
    public float recoilKick = 1.2f;
    public float recoilRandom = 0.5f;
    public float recoilReturnSpeed = 12f;

    [Header("Shoot FX")]
    public ParticleSystem muzzleFlash;

    [Header("Impact FX")]
    public ParticleSystem metalImpactVFX;
    public ParticleSystem dirtImpactVFX;
    public ParticleSystem fleshImpactVFX;
    public ParticleSystem critImpact; // ⭐ opcional


    [Header("Decals")]
    public float decalLifeTime = 6f;
    public float decalFadeTime = 1.5f;
    public BulletDecal defaultDecalPrefab;

    [Header("Inventory View")]
    public Vector3 inventoryPositionOffset;
    public Vector3 inventoryRotationOffset;


    [Header("Camera Recoil")]
    public float cameraRecoilVertical = 1.5f;
    public float cameraRecoilHorizontal = 0.8f;


    [Header("Crosshair")]
    public CrosshairProfile crosshairProfile;

}
