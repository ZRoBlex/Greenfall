// NonLethalWeaponStats.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Greenfall/Weapons/NonLethal Weapon")]
public class NonLethalWeaponStats : ScriptableObject
{
    [Header("General")]
    public string weaponName = "Dart Gun";

    [Header("Dart")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 25f;
    public float projectileLife = 5f;

    [Header("Taser (short)")]
    public float taserRange = 3f;
    public float taserStun = 3f;
    public float taserCaptureTick = 10f;

    [Header("Common")]
    public float stunAmountOnHit = 15f;    // cantidad que agrega al stun meter
    public float captureTickOnHit = 20f;  // progreso de captura por impacto
    public float cooldown = 0.5f;
}
