// NonLethalWeapon.cs
using UnityEngine;

public class NonLethalWeapon : MonoBehaviour
{
    public NonLethalWeaponStats stats;
    public Transform firePoint; // punto desde donde se dispara (cámara / muñeca)
    float lastFire;

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            TryFire();
        }
        if (Input.GetButtonDown("Fire2"))
        {
            TryTase();
        }
    }

    void TryFire()
    {
        if (Time.time - lastFire < stats.cooldown) return;
        lastFire = Time.time;

        if (stats.projectilePrefab == null || firePoint == null) return;

        var projGO = Instantiate(stats.projectilePrefab, firePoint.position, firePoint.rotation);
        //var proj = projGO.GetComponent<DartProjectile>();
        //if (proj != null)
        //{
        //    proj.Initialize(stats.projectileSpeed, stats.projectileLife, stats.stunAmountOnHit, stats.captureTickOnHit);
        //}
    }

    void TryTase()
    {
        if (Time.time - lastFire < stats.cooldown) return;
        lastFire = Time.time;

        // Raycast corto
        Ray r = new Ray(firePoint.position, firePoint.forward);
        if (Physics.Raycast(r, out RaycastHit hit, stats.taserRange))
        {
            var nonLethal = hit.collider.GetComponent<NonLethalHealth>();
            if (nonLethal != null)
            {
                //nonLethal.ApplyNonLethalHit(stats.taserStun, stats.taserCaptureTick);
            }
        }
    }
}
