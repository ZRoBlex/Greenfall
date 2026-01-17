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

    //[Header("Critical Settings")]
    //[SerializeField] bool enableCritical = true;
    //[SerializeField] int criticalRollMax = 14;
    //[SerializeField] float criticalMultiplier = 2f;

    [Header("Muzzle Flash")]
    [SerializeField] WeaponMuzzleFlash muzzleFlash;

    [SerializeField] WeaponMuzzleFlashPool muzzleFlashPool;




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
        // 🔥 MUZZLE FLASH
        if (stats.muzzleFlash)
        {
            ParticleSystem fx =
                Instantiate(stats.muzzleFlash, firePoint.position, firePoint.rotation);

            fx.Play();
            Destroy(fx.gameObject, fx.main.duration);
        }

        // 🔥 FLASH VISUAL
        if (muzzleFlash != null)
            muzzleFlash.Play();

        if (muzzleFlashPool != null)
        {
            muzzleFlashPool.PlayFlash(
                firePoint.position,
                shootCamera.transform.forward
            );
        }



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
        bool isCritical = false;




        if (stats.useCaptureDamage)
{
    if (nonLethal != null)
    {
        float dmg = GenerateDamage(
            stats.minCaptureDamage,
            stats.maxCaptureDamage,
            out isCritical
        );

                DamagePopupReceiver popup = hit.collider.GetComponentInParent<DamagePopupReceiver>();
                if (popup != null)
                {
                    popup.SetLastHitCritical(isCritical);
                }


                nonLethal.ApplyCaptureTick(dmg);
            SendDamagePopup(hit, dmg, isCritical);
        }
    }
else
{
    if (health != null)
    {
        float dmg = GenerateDamage(
            stats.minLethalDamage,
            stats.maxLethalDamage,
            out isCritical
        );

                DamagePopupReceiver popup = hit.collider.GetComponentInParent<DamagePopupReceiver>();
                if (popup != null)
                {
                    popup.SetLastHitCritical(isCritical);
                }


                health.ApplyDamage(dmg);
            SendDamagePopup(hit, dmg, isCritical);
        }
    }


        // FX
        SpawnImpact(hit, isCritical);
    }


    // ------------------------------------------------
    // IMPACT FX
    // ------------------------------------------------
    //void SpawnImpact(RaycastHit hit)
    //{
    //    GameObject fx = null;

    //    int layer = hit.collider.gameObject.layer;

    //    if (layer == LayerMask.NameToLayer("Metal"))
    //        fx = stats.metalImpact;
    //    else if (layer == LayerMask.NameToLayer("Dirt"))
    //        fx = stats.dirtImpact;
    //    else if (hit.collider.CompareTag("Enemy"))
    //        fx = stats.fleshImpact;

    //    if (fx)
    //        Instantiate(fx, hit.point, Quaternion.LookRotation(hit.normal));

    //}
    void SpawnImpact(RaycastHit hit, bool isCritical)
    {
        ParticleSystem fx = null;

        int layer = hit.collider.gameObject.layer;

        if (isCritical && stats.critImpact)
        {
            fx = stats.critImpact;
        }
        else if (layer == LayerMask.NameToLayer("Metal"))
        {
            fx = stats.metalImpactVFX;
        }
        else if (layer == LayerMask.NameToLayer("Dirt"))
        {
            fx = stats.dirtImpactVFX;
        }
        else if (hit.collider.CompareTag("Enemy"))
        {
            fx = stats.fleshImpactVFX;
        }

        if (!fx) return;

        ParticleSystem ps =
            Instantiate(fx, hit.point, Quaternion.LookRotation(hit.normal));

        ps.Play();
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

    float GenerateDamage(float min, float max, out bool isCritical)
    {
        float damage = Random.Range(min, max);
        isCritical = false;

        if (stats.allowCritical && Random.value <= stats.criticalChance)
        {
            damage += damage * stats.criticalBonusPercent;
            isCritical = true;
        }

        return damage;
    }




    //bool RollCritical()
    //{
    //    if (!enableCritical)
    //        return false;

    //    int roll = Random.Range(0, criticalRollMax + 1);
    //    return roll == criticalRollMax;
    //}


    void SendDamagePopup(RaycastHit hit, float damage, bool isCritical)
    {
        DamagePopupReceiver popup =
            hit.collider.GetComponentInParent<DamagePopupReceiver>();

        //if (popup != null)
            //popup.ShowExternalDamage(damage, isCritical, hit.point);
    }

}
