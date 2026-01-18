using UnityEngine;


[System.Serializable]
public class WeaponSpawnEntry
{
    public Weapon weaponPrefab;
    [Range(0f, 1f)]
    public float probability = 1f;
}
