using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour
{
    public WeaponStats stats;
    public Transform firePoint;
    public Camera shootCamera;

    float lastFire;
    bool isBursting;
    Vector3 recoilRotation;

    Quaternion baseLocalRotation;
    [Header("Camera Recoil")]
    [SerializeField] CameraRecoilController cameraRecoil;

    [Header("Default Weapon")]
    public bool isDefaultWeapon = false;


    public WeaponMagazine magazine;



    [Header("Damage Systems")]
    //[SerializeField] DistanceDamageScaler distanceScaler;


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

    [SerializeField] DistanceDamageScalerSO distanceScalerSO;


    [Header("Inventory Offset Override")]
    [SerializeField] bool overrideInventoryOffset = false;

    [SerializeField] bool liveEditOffset = false; // 👈 ESTE ES EL NUEVO

    [SerializeField] Vector3 inventoryPositionOffset;
    [SerializeField] Vector3 inventoryRotationOffset;

    [SerializeField] DynamicCrosshair crosshair;

    [Header("Crosshair")]
    [SerializeField] CrosshairProfile crosshairProfile;

    float recoilAccumulated;


    // Agregar arriba del Start()
    [Header("Ammo (Placeholder)")]
    [SerializeField] int currentAmmo = 30; // Por ahora solo contador interno
    [SerializeField] int maxAmmo = 30;

    [Header("Ammo On Pickup")]
    [SerializeField] int minAmmoOnPickup = 5;
    [SerializeField] int maxAmmoOnPickup = 30;

    // ------------------------------------------------
    // AMMO TRANSFER FLAG
    // ------------------------------------------------
    private bool hasTransferredAmmo = false;

    private void Awake()
    {
        if (magazine == null)
            magazine = GetComponent<WeaponMagazine>();
    }

    void OnEnable()
    {
        if (DynamicCrosshair.Instance)
            DynamicCrosshair.Instance.SetProfile(crosshairProfile);

        if (magazine != null && !ammoInitialized)
        {
            if (isDefaultWeapon)
            {
                // 🔥 SOLO el arma base empieza llena
                magazine.currentBullets = magazine.maxBullets;
                Debug.Log($"🟢 {name} (default) inicia con cargador lleno");
            }
            else
            {
                // 🎲 Las demás siguen siendo aleatorias
                int startAmmo = Random.Range(minAmmoOnPickup, maxAmmoOnPickup + 1);
                magazine.currentBullets = Mathf.Min(startAmmo, magazine.maxBullets);
                Debug.Log($"🟡 {name} inicia con {magazine.currentBullets} balas");
            }

            ammoInitialized = true;
        }
    }





    void Start()
    {
        shootCamera = shootCamera ? shootCamera : Camera.main;

        if (!cameraRecoil && shootCamera)
            cameraRecoil = shootCamera.GetComponentInParent<CameraRecoilController>();
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
    // FIRE LOGIC (modificación mínima)
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
            //if (!ConsumeAmmo()) break; // 🔹 no dispara si no hay munición
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
        if (magazine == null)
            return;

        if (!magazine.ConsumeBullet())
        {
            Debug.Log("🔴 Cargador vacío");
            return;
        }


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
        ApplyCameraRecoil(); // 👈 ESTO ES NUEVO

        float recoilAmount =
    stats.recoilKick +
    Random.Range(-stats.recoilRandom, stats.recoilRandom);


        if (DynamicCrosshair.Instance)
        {
            DynamicCrosshair.Instance.ApplyWeaponSpread(stats.spreadAngle);
        }

        //DynamicCrosshair.Instance?.OnShoot();

        //DynamicCrosshair.Instance?.AddRecoil(recoilAmount);




        //if (cameraRecoil)
        //{
        //    cameraRecoil.AddRecoil(
        //        stats.cameraRecoilVertical,
        //        stats.cameraRecoilHorizontal
        //    );
        //}

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
            if (distanceScalerSO != null && shootCamera != null)
            {
                float distance = Vector3.Distance(
                    shootCamera.transform.position,
                    hit.point
                );

                dmg *= distanceScalerSO.GetMultiplier(distance);
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

        //transform.localRotation = Quaternion.Euler(recoilRotation);
        transform.localRotation =
    baseLocalRotation * Quaternion.Euler(recoilRotation);

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

    void LateUpdate()
    {
        if (!liveEditOffset) return;
        if (transform.parent == null) return;

        ApplyInventoryOffset();
    }

    public void ApplyInventoryOffset()
    {
        transform.localPosition = GetInventoryPositionOffset();
        transform.localRotation = Quaternion.Euler(GetInventoryRotationOffset());
    }


    public Vector3 GetInventoryPositionOffset()
    {
        return overrideInventoryOffset
            ? inventoryPositionOffset
            : stats.inventoryPositionOffset;
    }

    public Vector3 GetInventoryRotationOffset()
    {
        return overrideInventoryOffset
            ? inventoryRotationOffset
            : stats.inventoryRotationOffset;
    }

    public void ApplyInventoryRotation(Vector3 euler)
    {
        baseLocalRotation = Quaternion.Euler(euler);
        transform.localRotation = baseLocalRotation;
    }


    void ApplyCameraRecoil()
    {
        if (!cameraRecoil || stats == null) return;

        cameraRecoil.AddRecoil(
            stats.cameraRecoilVertical,
            stats.cameraRecoilHorizontal
        );
    }



    // ------------------------------------------------
    // AMMO MANAGEMENT
    // ------------------------------------------------
    [SerializeField] AmmoInventory ammoInventory;
    private bool ammoInitialized = false;

    //bool ConsumeAmmo()
    //{
    //    if (ammoInventory == null || ammoInventory.ConsumeAmmo(stats.ammoType, 1) == false)
    //    {
    //        Debug.Log("Sin munición!");
    //        return false;
    //    }

    //    return true;
    //}



    // 🔹 Método público para recargar
    public void Reload(int amount)
    {
        currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);
    }

    // Asigna el inventario del jugador al arma
    public void AssignAmmoInventory(AmmoInventory inventory)
    {
        ammoInventory = inventory;

        // ❌ Ya no generamos aleatorio aquí
        // if (!ammoInitialized)
        // {
        //     currentAmmo = Random.Range(minAmmoOnPickup, maxAmmoOnPickup + 1);
        //     ammoInitialized = true;
        // }

        // Asegurarse de que exista el slot
        if (stats.ammoType != null)
        {
            ammoInventory.AddAmmo(stats.ammoType, 0);
        }
    }


    public int TransferAmmoToInventory()
    {
        if (stats.ammoType == null || ammoInventory == null)
            return 0;

        AmmoSlot slot = ammoInventory.GetSlot(stats.ammoType);
        if (slot == null) return 0;

        int spaceLeft = slot.maxAmount - slot.currentAmount;
        if (spaceLeft <= 0) return 0;

        int ammoToGive = Mathf.Min(currentAmmo, spaceLeft);

        ammoInventory.AddAmmo(stats.ammoType, ammoToGive);
        currentAmmo -= ammoToGive; // lo que sobra queda en el arma

        // 🔥 UI FEEDBACK
        if (AmmoPickupUIManager.Instance)
        {
            Sprite icon = null;

            // si luego tu ammoType tiene icon:
            // icon = stats.ammoType.icon;

            AmmoPickupUIManager.Instance.ShowAmmoPickup(
                ammoToGive,
                stats.ammoType.ammoName, // o .name
                icon
            );
        }

        return ammoToGive;
    }


    public void InitializeAmmo()
    {
        if (!ammoInitialized)
        {
            currentAmmo = Random.Range(minAmmoOnPickup, maxAmmoOnPickup + 1);
            ammoInitialized = true;
        }
    }

    public void ReloadFromInventory()
    {
        if (magazine == null || stats == null || stats.ammoType == null)
            return;

        if (magazine.IsFull)
        {
            Debug.Log("🟡 Cargador ya lleno");
            return;
        }

        if (ammoInventory == null)
        {
            Debug.Log("❌ No hay inventario asignado al arma");
            return;
        }

        int needed = magazine.maxBullets - magazine.currentBullets;

        int taken = ammoInventory.RemoveAmmo(stats.ammoType, needed);

        if (taken > 0)
        {
            magazine.AddBullets(taken);
            Debug.Log($"🔄 Recargadas {taken} balas");
        }
        else
        {
            Debug.Log("🔴 No hay balas en el inventario");
        }
    }

    public void MarkAmmoInitialized()
    {
        ammoInitialized = true;
    }

}
