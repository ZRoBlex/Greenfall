//// NonLethalWeaponStats.cs
//using UnityEngine;

//public enum FireMode
//{
//    SemiAuto,
//    FullAuto,
//    Burst
//}

//[CreateAssetMenu(menuName = "Greenfall/Weapons/NonLethal Weapon")]
//public class NonLethalWeaponStats : ScriptableObject
//{
//    public FireMode fireMode = FireMode.SemiAuto;

//    [Header("Burst Settings")]
//    public int burstCount = 3;
//    public float burstDelay = 0.08f;

//    [Header("General")]
//    public string weaponName = "Dart Gun";

//    [Header("Dart")]
//    public GameObject projectilePrefab;
//    public float projectileSpeed = 25f;
//    public float projectileLife = 5f;

//    [Header("Taser (short)")]
//    public float taserRange = 3f;
//    public float taserStun = 3f;
//    public float taserCaptureTick = 10f;

//    [Header("Common")]
//    public float stunAmountOnHit = 15f;    // cantidad que agrega al stun meter
//    public float captureTickOnHit = 20f;  // progreso de captura por impacto
//    public float cooldown = 0.5f;
//}

using UnityEngine;

public enum FireMode
{
    SemiAuto,
    FullAuto,
    Burst
}

[CreateAssetMenu(menuName = "Greenfall/Weapons/NonLethal Weapon")]
public class NonLethalWeaponStats : ScriptableObject
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

    [Header("Damage")]
    public bool useCaptureDamage = true;
    public float lethalDamage = 10f;
    public float stunAmount = 15f;
    public float captureTick = 20f;

    [Header("Line Renderer")]
    public LineRenderer linePrefab;
    public Material normalLine;
    public Material hitLine;
    public float lineDuration = 0.05f;

    [Header("Impact FX")]
    public GameObject metalImpact;
    public GameObject dirtImpact;
    public GameObject fleshImpact;

    [Header("Recoil")]
    public float recoilKick = 1.2f;
    public float recoilRandom = 0.5f;
    public float recoilReturnSpeed = 12f;
}
