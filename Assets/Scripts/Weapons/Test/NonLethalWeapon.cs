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

    [Header("Damage Systems")]
    [SerializeField] DistanceDamageScaler distanceScaler;


    // 🔥 Muzzle flash instance
    ParticleSystem muzzleFlashInstance;

    //[Header("Material Layers")]
    //public LayerMask metalLayer;
    //public LayerMask leatherMask;
    //public LayerMask dirtLayer; 
    [Header("Impact Tags")]
    [SerializeField] string[] metalTags;
    [SerializeField] string[] dirtTags;
    [SerializeField] string[] fleshTags;

    [Header("Audio")]
    [SerializeField] WeaponAudio weaponAudio;





    void Start()
    {
        // Instanciar UNA SOLA VEZ el muzzle flash
        if (stats.muzzleFlash && firePoint)
        {
            muzzleFlashInstance = Instantiate(
                stats.muzzleFlash,
                firePoint.position,
                firePoint.rotation,
                firePoint
            );

            muzzleFlashInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

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
        if (weaponAudio != null)
            weaponAudio.PlayShoot();


        // 🔥 MUZZLE FLASH (PARTÍCULAS)
        if (muzzleFlashInstance)
        {
            muzzleFlashInstance.transform.position = firePoint.position;
            muzzleFlashInstance.transform.rotation = firePoint.rotation;
            muzzleFlashInstance.Play();
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
        Health health = hit.collider.GetComponentInParent<Health>();
        NonLethalHealth nonLethal = hit.collider.GetComponentInParent<NonLethalHealth>();

        DamageHitRelay relay = hit.collider.GetComponentInParent<DamageHitRelay>();
        if (relay)
            relay.RegisterHit(hit.point);

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

        bool isCritical = false;

        if (stats.useCaptureDamage && nonLethal != null)
        {
            float dmg = GenerateDamage(stats.minCaptureDamage, stats.maxCaptureDamage, out isCritical);

            var popup = hit.collider.GetComponentInParent<DamagePopupReceiver>();
            if (popup) popup.SetLastHitCritical(isCritical);

            nonLethal.ApplyCaptureTick(dmg);
        }
        else if (health != null)
        {
            float dmg = GenerateDamage(
                stats.minLethalDamage,
                stats.maxLethalDamage,
                out isCritical
            );

            // 📏 ESCALADO POR DISTANCIA
            if (distanceScaler != null && shootCamera != null)
            {
                float distance = Vector3.Distance(
                    shootCamera.transform.position,
                    hit.point
                );

                dmg *= distanceScaler.GetMultiplier(distance);
            }

            // 🎯 ZONA DEL CUERPO
            DamageZone zone = hit.collider.GetComponent<DamageZone>();
            if (zone != null)
                dmg *= zone.damageMultiplier;

            var popup = hit.collider.GetComponentInParent<DamagePopupReceiver>();
            if (popup) popup.SetLastHitCritical(isCritical);

            health.ApplyDamage(dmg);
        }

        if (BulletDecalPool.Instance != null)
        {
            BulletDecalPool.Instance.Spawn(hit);
        }







        SpawnImpact(hit, isCritical);
    }

    // ------------------------------------------------
    // IMPACT FX
    // ------------------------------------------------
    void SpawnImpact(RaycastHit hit, bool isCritical)
    {
        ParticleSystem fx = null;
        Collider col = hit.collider;

        if (isCritical && stats.critImpact)
        {
            fx = stats.critImpact;
        }
        else if (HasAnyTag(col, metalTags))
        {
            fx = stats.metalImpactVFX;
        }
        else if (HasAnyTag(col, dirtTags))
        {
            fx = stats.dirtImpactVFX;
        }
        else if (HasAnyTag(col, fleshTags))
        {
            fx = stats.fleshImpactVFX;
        }

        if (!fx) return;

        Quaternion rot = Quaternion.LookRotation(-hit.normal);
        if (ParticlePool.Instance != null)
        {
            ParticlePool.Instance.Spawn(fx, hit.point, rot);
        }

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

    bool HasAnyTag(Collider col, string[] tags)
    {
        if (tags == null || tags.Length == 0)
            return false;

        foreach (string tag in tags)
        {
            if (!string.IsNullOrEmpty(tag) && col.CompareTag(tag))
                return true;
        }

        return false;
    }

}
