using System.Collections;
using UnityEngine;
using Weapons;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Data")]
    public WeaponDefinition def;

    [Header("References")]
    public Transform aimTransform;       // dirección donde dispara el arma
    public Transform firePoint;          // punto de origen

    [Header("Runtime")]
    public int ammoInMag;
    public int ammoReserve;
    bool isReloading = false;
    bool isFiring = false;

    float lastShot = -999f;

    void Start()
    {
        ammoInMag = def.magazineSize;
        ammoReserve = def.ammoReserve;
    }

    // ---------------------------------------------------------
    // CONTROL PÚBLICO (TORRETA, NPC, TRAMPA, SISTEMA AUTOMÁTICO)
    // ---------------------------------------------------------
    public void StartFiring()
    {
        if (isFiring) return;
        isFiring = true;

        if (def.fireMode == FireMode.Auto)
            StartCoroutine(AutoRoutine());
        else if (def.fireMode == FireMode.Semi)
            ShootOnce();
        else if (def.fireMode == FireMode.Burst)
            StartCoroutine(BurstRoutine());
    }

    public void StopFiring()
    {
        isFiring = false;
    }

    public void Reload()
    {
        if (!isReloading)
            StartCoroutine(ReloadRoutine());
    }

    // ---------------------------------------------------------
    // DISPARO
    // ---------------------------------------------------------
    IEnumerator AutoRoutine()
    {
        while (isFiring)
        {
            ShootOnce();
            yield return new WaitForSeconds(def.TimeBetweenShots());
        }
    }

    IEnumerator BurstRoutine()
    {
        for (int i = 0; i < def.burstCount; i++)
        {
            ShootOnce();
            yield return new WaitForSeconds(def.burstSpacing);
        }
    }

    void ShootOnce()
    {
        if (isReloading) return;
        if (ammoInMag <= 0) return;

        if (Time.time - lastShot < def.TimeBetweenShots()) return;

        lastShot = Time.time;
        ammoInMag--;

        SpawnProjectile();

        // recoil, sonido, efectos secundarios… (independientes)
    }

    // ---------------------------------------------------------
    // PROYECTIL (PARTÍCULA)
    // ---------------------------------------------------------
    void SpawnProjectile()
    {
        if (firePoint == null || def.particlePrefab == null) return;

        // Pool
        ParticleSystem ps = BulletParticlePool.Instance.Get(def.particlePrefab);

        // Posición y orientación
        ps.transform.position = firePoint.position;
        ps.transform.rotation = Quaternion.LookRotation(aimTransform.forward);

        // Configurar daño
        var dmg = ps.GetComponent<BulletParticleDamage>();
        if (dmg != null)
            dmg.Configure(def.isLethal, def.lethalDamage, def.nonLethalTick, def.stunDuration);

        // correr el sistema
        ps.Play();

        StartCoroutine(ReturnToPool(ps));
    }

    IEnumerator ReturnToPool(ParticleSystem ps)
    {
        yield return new WaitForSeconds(def.projectileLifetime);
        BulletParticlePool.Instance.Return(def.particlePrefab, ps);
    }

    // ---------------------------------------------------------
    // RECARGA
    // ---------------------------------------------------------
    IEnumerator ReloadRoutine()
    {
        isReloading = true;

        yield return new WaitForSeconds(def.reloadTime);

        int needed = def.magazineSize - ammoInMag;

        int used = Mathf.Min(needed, ammoReserve);
        ammoInMag += used;
        ammoReserve -= used;

        isReloading = false;
    }
}
