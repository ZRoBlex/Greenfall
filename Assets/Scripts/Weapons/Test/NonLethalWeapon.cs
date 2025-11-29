using UnityEngine;
using System.Collections;

public class NonLethalWeapon : MonoBehaviour
{
    public NonLethalWeaponStats stats;
    public Transform firePoint;

    float lastFire;
    bool isBursting = false;

    void Update()
    {
        switch (stats.fireMode)
        {
            case FireMode.SemiAuto:
                if (Input.GetButtonDown("Fire1"))
                    TryFire();
                break;

            case FireMode.FullAuto:
                if (Input.GetButton("Fire1"))
                    TryFire();
                break;

            case FireMode.Burst:
                if (Input.GetButtonDown("Fire1"))
                    TryBurst();
                break;
        }

        if (Input.GetButtonDown("Fire2"))
            TryTase();
    }

    // -----------------------------------
    // FIRE NORMAL
    // -----------------------------------
    void TryFire()
    {
        if (Time.time - lastFire < stats.cooldown) return;
        lastFire = Time.time;

        SpawnProjectile();
    }

    // -----------------------------------
    // BURST MODE
    // -----------------------------------
    void TryBurst()
    {
        if (isBursting) return;
        StartCoroutine(BurstRoutine());
    }

    IEnumerator BurstRoutine()
    {
        isBursting = true;

        for (int i = 0; i < stats.burstCount; i++)
        {
            TryFire();
            yield return new WaitForSeconds(stats.burstDelay);
        }

        isBursting = false;
    }

    // -----------------------------------
    // TASER
    // -----------------------------------
    void TryTase()
    {
        if (Time.time - lastFire < stats.cooldown) return;
        lastFire = Time.time;

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

    // -----------------------------------
    // SPAWN PROJECTILE
    // -----------------------------------
    void SpawnProjectile()
    {
        if (stats.projectilePrefab == null || firePoint == null) return;

        Instantiate(stats.projectilePrefab, firePoint.position, firePoint.rotation);
    }
}
