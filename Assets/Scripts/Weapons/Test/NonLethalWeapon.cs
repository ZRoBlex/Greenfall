//using UnityEngine;
//using System.Collections;

//public class NonLethalWeapon : MonoBehaviour
//{
//    public NonLethalWeaponStats stats;
//    public Transform firePoint;

//    float lastFire;
//    bool isBursting = false;

//    void Update()
//    {
//        switch (stats.fireMode)
//        {
//            case FireMode.SemiAuto:
//                if (Input.GetButtonDown("Fire1"))
//                    TryFire();
//                break;

//            case FireMode.FullAuto:
//                if (Input.GetButton("Fire1"))
//                    TryFire();
//                break;

//            case FireMode.Burst:
//                if (Input.GetButtonDown("Fire1"))
//                    TryBurst();
//                break;
//        }

//        if (Input.GetButtonDown("Fire2"))
//            TryTase();
//    }

//    // -----------------------------------
//    // FIRE NORMAL
//    // -----------------------------------
//    void TryFire()
//    {
//        if (Time.time - lastFire < stats.cooldown) return;
//        lastFire = Time.time;

//        SpawnProjectile();
//    }

//    // -----------------------------------
//    // BURST MODE
//    // -----------------------------------
//    void TryBurst()
//    {
//        if (isBursting) return;
//        StartCoroutine(BurstRoutine());
//    }

//    IEnumerator BurstRoutine()
//    {
//        isBursting = true;

//        for (int i = 0; i < stats.burstCount; i++)
//        {
//            TryFire();
//            yield return new WaitForSeconds(stats.burstDelay);
//        }

//        isBursting = false;
//    }

//    // -----------------------------------
//    // TASER
//    // -----------------------------------
//    void TryTase()
//    {
//        if (Time.time - lastFire < stats.cooldown) return;
//        lastFire = Time.time;

//        Ray r = new Ray(firePoint.position, firePoint.forward);

//        if (Physics.Raycast(r, out RaycastHit hit, stats.taserRange))
//        {
//            var nonLethal = hit.collider.GetComponent<NonLethalHealth>();
//            if (nonLethal != null)
//            {
//                //nonLethal.ApplyNonLethalHit(stats.taserStun, stats.taserCaptureTick);
//            }
//        }
//    }

//    // -----------------------------------
//    // SPAWN PROJECTILE
//    // -----------------------------------
//    void SpawnProjectile()
//    {
//        if (stats.projectilePrefab == null || firePoint == null) return;

//        Instantiate(stats.projectilePrefab, firePoint.position, firePoint.rotation);
//    }
//}
using UnityEngine;
using System.Collections;

public class NonLethalWeapon : MonoBehaviour
{
    public NonLethalWeaponStats stats;
    public Transform firePoint;
    public Camera shootCamera;

    float lastFire;
    bool isBursting;
    Vector3 recoilRotation;

    void Update()
    {
        HandleFireInput();
        HandleRecoil();
    }

    // ------------------------------------------------
    // INPUT
    // ------------------------------------------------
    void HandleFireInput()
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
    }

    // ------------------------------------------------
    // FIRE LOGIC
    // ------------------------------------------------
    void TryFire()
    {
        if (Time.time - lastFire < stats.cooldown) return;
        lastFire = Time.time;

        Shoot();
    }

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
            Shoot();
            yield return new WaitForSeconds(stats.burstDelay);
        }

        isBursting = false;
    }

    // ------------------------------------------------
    // SHOOT
    // ------------------------------------------------
    void Shoot()
    {
        for (int i = 0; i < stats.pellets; i++)
        {
            Vector3 dir = GetSpreadDirection();
            Ray ray = new Ray(shootCamera.transform.position, dir);

            if (Physics.Raycast(ray, out RaycastHit hit, stats.range, stats.hittableLayers))
            {
                HandleHit(hit);
                SpawnLine(firePoint.position, hit.point, true);
            }
            else
            {
                SpawnLine(
                    firePoint.position,
                    firePoint.position + dir * stats.range,
                    false
                );
            }
        }

        ApplyRecoil();
    }

    Vector3 GetSpreadDirection()
    {
        Vector3 dir = shootCamera.transform.forward;
        dir += Random.insideUnitSphere * stats.spreadAngle * 0.01f;
        return dir.normalized;
    }

    // ------------------------------------------------
    // HIT
    // ------------------------------------------------
    void HandleHit(RaycastHit hit)
    {
        // Buscar el objeto que realmente puede recibir daño
        Health health = hit.collider.GetComponentInParent<Health>();
        NonLethalHealth nonLethal = hit.collider.GetComponentInParent<NonLethalHealth>();

        DamageHitRelay relay = hit.collider.GetComponentInParent<DamageHitRelay>();
        if (relay != null)
            relay.RegisterHit(hit.point);


        // Verificar TAG (una sola vez, no foreach)
        bool validTag = false;
        foreach (string tag in stats.damageTags)
        {
            if (hit.collider.CompareTag(tag))
            {
                validTag = true;
                break;
            }
        }

        if (!validTag) return;

        // APLICAR DAÑO
        if (stats.useCaptureDamage)
        {
            if (nonLethal != null)
            {
                nonLethal.ApplyCaptureTick(
                    stats.captureTick
                );
            }
        }
        else
        {
            if (health != null)
            {
                health.ApplyDamage(stats.lethalDamage);
            }
        }

        // FX
        SpawnImpact(hit);
    }


    // ------------------------------------------------
    // IMPACT FX
    // ------------------------------------------------
    void SpawnImpact(RaycastHit hit)
    {
        GameObject fx = null;

        int layer = hit.collider.gameObject.layer;

        if (layer == LayerMask.NameToLayer("Metal"))
            fx = stats.metalImpact;
        else if (layer == LayerMask.NameToLayer("Dirt"))
            fx = stats.dirtImpact;
        else if (hit.collider.CompareTag("Enemy"))
            fx = stats.fleshImpact;

        if (fx)
            Instantiate(fx, hit.point, Quaternion.LookRotation(hit.normal));
    }

    // ------------------------------------------------
    // LINE RENDERER
    // ------------------------------------------------
    void SpawnLine(Vector3 start, Vector3 end, bool hit)
    {
        if (!stats.linePrefab) return;

        LineRenderer lr = Instantiate(stats.linePrefab);
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.material = hit ? stats.hitLine : stats.normalLine;

        Destroy(lr.gameObject, stats.lineDuration);
    }

    // ------------------------------------------------
    // RECOIL
    // ------------------------------------------------
    void ApplyRecoil()
    {
        recoilRotation.x -= stats.recoilKick + Random.Range(-stats.recoilRandom, stats.recoilRandom);
    }

    void HandleRecoil()
    {
        recoilRotation = Vector3.Lerp(
            recoilRotation,
            Vector3.zero,
            stats.recoilReturnSpeed * Time.deltaTime
        );

        transform.localRotation = Quaternion.Euler(recoilRotation);
    }
}
