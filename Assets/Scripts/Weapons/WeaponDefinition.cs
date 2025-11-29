using UnityEngine;

namespace Weapons
{
    public enum FireMode { Semi, Auto, Burst }
    public enum AmmoType { None, Bullets, Shells, Energy, Arrows }

    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Weapons/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "New Weapon";

        [Header("Firing")]
        public FireMode fireMode = FireMode.Semi;
        public float roundsPerMinute = 600f;
        public int burstCount = 3;
        public float burstSpacing = 0.08f;

        [Header("Ammo")]
        public int magazineSize = 30;
        public int ammoReserve = 120;
        public float reloadTime = 1.8f;

        [Header("Projectile")]
        public GameObject particlePrefab;
        public float projectileSpeed = 25f;
        public float projectileLifetime = 1.2f;

        [Header("Damage")]
        public bool isLethal = true;
        public float lethalDamage = 20f;
        public float nonLethalTick = 25f;
        public float stunDuration = 2f;

        [Header("Spread")]
        public float baseSpread = 1.5f;

        public float TimeBetweenShots()
        {
            return 60f / roundsPerMinute;
        }
    }
}
