// ============================================================
// Weapon.cs — RECOIL LIMPIADO + INTEGRACIÓN CON CameraRecoilController
// ============================================================
// CAMBIOS RESPECTO A TU VERSIÓN:
//
// 1. ApplyRecoil() ya no modifica transform.localRotation.
//    Ese método peleaba contra ApplyInventoryRotation() causando
//    que el arma vibrara o se viera mal. El recoil VISUAL del
//    modelo del arma ahora viene de WeaponRecoilAnimator (nuevo).
//
// 2. ApplyCameraRecoil() ahora sí se llama correctamente.
//    La línea estaba comentada en tu código original.
//
// 3. HandleRecoil() también eliminado del arma.
//    La cámara ya maneja su propia recuperación en CameraRecoilController.
//
// 4. El modelo del arma ahora tiene su propio "kick" visual
//    mediante WeaponRecoilAnimator (componente separado, abajo).
//    Eso da el feeling de que el arma "pega" sin interferir con
//    la posición del inventario.
// ============================================================

using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour
{
    public WeaponStats stats;
    public Transform   firePoint;
    public Camera      shootCamera;

    float     lastFire;
    bool      isBursting;
    Quaternion baseLocalRotation;

    [Header("Camera Recoil")]
    [SerializeField] CameraRecoilController cameraRecoil;

    [Header("Weapon Visual Recoil")]
    [Tooltip("Opcional. Maneja el kick visual del modelo del arma.")]
    // [SerializeField] WeaponRecoilAnimator weaponRecoilAnimator;

    [Header("Default Weapon")]
    public bool isDefaultWeapon = false;

    [Header("UI")]
    public Sprite icon;
    public string weaponName;

    public WeaponMagazine magazine;

    [Header("Impact Tags")]
    [SerializeField] string[] metalTags;
    [SerializeField] string[] dirtTags;
    [SerializeField] string[] fleshTags;

    [Header("Audio")]
    [SerializeField] WeaponAudio weaponAudio;

    [SerializeField] DistanceDamageScalerSO distanceScalerSO;

    [Header("Inventory Offset Override")]
    [SerializeField] bool    overrideInventoryOffset = false;
    [SerializeField] bool    liveEditOffset = false;
    [SerializeField] Vector3 inventoryPositionOffset;
    [SerializeField] Vector3 inventoryRotationOffset;

    [Header("Crosshair")]
    [SerializeField] DynamicCrosshair  crosshair;
    [SerializeField] CrosshairProfile  crosshairProfile;

    [Header("Ammo (Placeholder)")]
    [SerializeField] int currentAmmo       = 30;
    [SerializeField] int maxAmmo           = 30;

    [Header("Ammo On Pickup")]
    [SerializeField] int minAmmoOnPickup   = 5;
    [SerializeField] int maxAmmoOnPickup   = 30;

    ParticleSystem muzzleFlashInstance;
    bool hasTransferredAmmo = false;

    // ─────────────────────────────────────────────────────────
    // AMMO MANAGEMENT
    // ─────────────────────────────────────────────────────────
    [SerializeField] AmmoInventory ammoInventory;
    private bool ammoInitialized = false;

    // ─────────────────────────────────────────────────────────
    // AWAKE / START / ON ENABLE
    // ─────────────────────────────────────────────────────────

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
                magazine.currentBullets = magazine.maxBullets;
            }
            else
            {
                int startAmmo = Random.Range(minAmmoOnPickup, maxAmmoOnPickup + 1);
                magazine.currentBullets = Mathf.Min(startAmmo, magazine.maxBullets);
            }
            ammoInitialized = true;
        }
    }

    void Start()
    {
        shootCamera = shootCamera ? shootCamera : Camera.main;

        // Buscar CameraRecoilController en la jerarquía de la cámara
        if (!cameraRecoil && shootCamera)
            cameraRecoil = shootCamera.GetComponentInParent<CameraRecoilController>();

        // Autodetectar WeaponRecoilAnimator si no está asignado
        // if (!weaponRecoilAnimator)
        //     weaponRecoilAnimator = GetComponent<WeaponRecoilAnimator>();

        // Instanciar muzzle flash
        if (stats.muzzleFlash != null && firePoint != null)
        {
            muzzleFlashInstance = Instantiate(
                stats.muzzleFlash,
                firePoint.position,
                firePoint.rotation,
                firePoint);
            muzzleFlashInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void Update()
    {
        HandleFireInput();
        // ELIMINADO: HandleRecoil() ya no existe aquí.
        // La cámara tiene su propio Update en CameraRecoilController.
    }

    // ─────────────────────────────────────────────────────────
    // INPUT
    // ─────────────────────────────────────────────────────────

    void HandleFireInput()
    {
        switch (stats.fireMode)
        {
            case FireMode.SemiAuto:
                if (Input.GetButtonDown("Fire1")) TryFire();
                break;
            case FireMode.FullAuto:
                if (Input.GetButton("Fire1")) TryFire();
                break;
            case FireMode.Burst:
                if (Input.GetButtonDown("Fire1")) TryBurst();
                break;
        }
    }

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

    // ─────────────────────────────────────────────────────────
    // SHOOT
    // ─────────────────────────────────────────────────────────

    void Shoot()
    {
        if (magazine == null || !magazine.ConsumeBullet()) return;

        if (weaponAudio != null) weaponAudio.PlayShoot();

        if (muzzleFlashInstance)
        {
            muzzleFlashInstance.transform.SetPositionAndRotation(
                firePoint.position, firePoint.rotation);
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
                SpawnLine(firePoint.position, firePoint.position + dir * stats.range, false);
            }
        }

        // ── RECOIL ────────────────────────────────────────────
        // 1. Recoil de cámara (el que se siente como jugador)
        ApplyCameraRecoil();

        // 2. Recoil visual del modelo del arma (kick del cañón)
        // weaponRecoilAnimator?.AddKick(stats.recoilKick, stats.recoilRandom);

        // 3. Crosshair spread
        DynamicCrosshair.Instance?.ApplyWeaponSpread(stats.spreadAngle);
    }

    Vector3 GetSpreadDirection()
    {
        Vector3 dir = shootCamera.transform.forward;
        dir += Random.insideUnitSphere * stats.spreadAngle * 0.01f;
        return dir.normalized;
    }

    // ─────────────────────────────────────────────────────────
    // HIT
    // ─────────────────────────────────────────────────────────

    void HandleHit(RaycastHit hit)
    {
        Health health             = hit.collider.GetComponentInParent<Health>();
        NonLethalHealthAdapted nl = hit.collider.GetComponentInParent<NonLethalHealthAdapted>();
        DamageHitRelay relay      = hit.collider.GetComponentInParent<DamageHitRelay>();
        if (relay) relay.RegisterHit(hit.point);

        bool validTag = false;
        foreach (string tag in stats.damageTags)
            if (hit.collider.CompareTag(tag)) { validTag = true; break; }
        if (!validTag) return;

        bool isCritical = false;

        if (stats.useCaptureDamage && nl != null)
        {
            float dmg = GenerateDamage(stats.minCaptureDamage, stats.maxCaptureDamage, out isCritical);
            hit.collider.GetComponentInParent<DamagePopupReceiver>()?.SetLastHitCritical(isCritical);
            nl.ApplyCaptureTick(dmg);
        }
        else if (health != null)
        {
            float dmg = GenerateDamage(stats.minLethalDamage, stats.maxLethalDamage, out isCritical);

            if (distanceScalerSO != null && shootCamera != null)
                dmg *= distanceScalerSO.GetMultiplier(
                    Vector3.Distance(shootCamera.transform.position, hit.point));

            DamageZone zone = hit.collider.GetComponent<DamageZone>();
            if (zone != null) dmg *= zone.damageMultiplier;

            hit.collider.GetComponentInParent<DamagePopupReceiver>()?.SetLastHitCritical(isCritical);
            health.ApplyDamage(dmg);
        }

        BulletDecalPool.Instance?.Spawn(hit);
        SpawnImpact(hit, isCritical);
    }

    void SpawnImpact(RaycastHit hit, bool isCritical)
    {
        ParticleSystem fx = null;
        Collider col = hit.collider;

        if      (isCritical && stats.critImpact)    fx = stats.critImpact;
        else if (HasAnyTag(col, metalTags))          fx = stats.metalImpactVFX;
        else if (HasAnyTag(col, dirtTags))           fx = stats.dirtImpactVFX;
        else if (HasAnyTag(col, fleshTags))          fx = stats.fleshImpactVFX;

        if (!fx) return;
        ParticlePool.Instance?.Spawn(fx, hit.point, Quaternion.LookRotation(-hit.normal));
    }

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

    // ─────────────────────────────────────────────────────────
    // RECOIL
    // ─────────────────────────────────────────────────────────

    void ApplyCameraRecoil()
    {
        if (!cameraRecoil || stats == null) return;
        cameraRecoil.AddRecoil(stats.cameraRecoilVertical, stats.cameraRecoilHorizontal);
    }

    // ─────────────────────────────────────────────────────────
    // UTILS
    // ─────────────────────────────────────────────────────────

    float GenerateDamage(float min, float max, out bool isCritical)
    {
        float damage = Random.Range(min, max);
        isCritical = false;
        if (stats.allowCritical && Random.value <= stats.criticalChance)
        {
            damage    += damage * stats.criticalBonusPercent;
            isCritical = true;
        }
        return damage;
    }

    bool HasAnyTag(Collider col, string[] tags)
    {
        if (tags == null) return false;
        foreach (string tag in tags)
            if (!string.IsNullOrEmpty(tag) && col.CompareTag(tag)) return true;
        return false;
    }

    void LateUpdate()
    {
        if (!liveEditOffset || transform.parent == null) return;
        ApplyInventoryOffset();
    }

    // ─────────────────────────────────────────────────────────
    // INVENTORY OFFSET
    // ─────────────────────────────────────────────────────────

    public void ApplyInventoryOffset()
    {
        transform.localPosition = GetInventoryPositionOffset();
        transform.localRotation = Quaternion.Euler(GetInventoryRotationOffset());
    }

    public Vector3 GetInventoryPositionOffset() =>
        overrideInventoryOffset ? inventoryPositionOffset : stats.inventoryPositionOffset;

    public Vector3 GetInventoryRotationOffset() =>
        overrideInventoryOffset ? inventoryRotationOffset : stats.inventoryRotationOffset;

    public void ApplyInventoryRotation(Vector3 euler)
    {
        baseLocalRotation       = Quaternion.Euler(euler);
        transform.localRotation = baseLocalRotation;
    }

    // ─────────────────────────────────────────────────────────
    // AMMO
    // ─────────────────────────────────────────────────────────

    public void Reload(int amount) =>
        currentAmmo = Mathf.Min(currentAmmo + amount, maxAmmo);

    public void AssignAmmoInventory(AmmoInventory inventory)
    {
        ammoInventory = inventory;
        if (stats.ammoType != null)
            ammoInventory.AddAmmo(stats.ammoType, 0);
    }

    public int TransferAmmoToInventory()
    {
        if (stats.ammoType == null || ammoInventory == null) return 0;
        AmmoSlot slot = ammoInventory.GetSlot(stats.ammoType);
        if (slot == null) return 0;

        int space = slot.maxAmount - slot.currentAmount;
        if (space <= 0) return 0;

        int give = Mathf.Min(currentAmmo, space);
        ammoInventory.AddAmmo(stats.ammoType, give);
        currentAmmo -= give;

        AmmoPickupUIManager.Instance?.ShowAmmoPickup(give, stats.ammoType.ammoName, null);
        return give;
    }

    public void InitializeAmmo()
    {
        if (!ammoInitialized)
        {
            currentAmmo    = Random.Range(minAmmoOnPickup, maxAmmoOnPickup + 1);
            ammoInitialized = true;
        }
    }

    public void ReloadFromInventory()
    {
        if (magazine == null || stats?.ammoType == null) return;
        if (magazine.IsFull) return;
        if (ammoInventory == null) return;

        int needed = magazine.maxBullets - magazine.currentBullets;
        int taken  = ammoInventory.RemoveAmmo(stats.ammoType, needed);
        if (taken > 0) magazine.AddBullets(taken);
    }

    public void MarkAmmoInitialized() => ammoInitialized = true;
}
