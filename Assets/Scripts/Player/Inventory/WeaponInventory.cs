// ============================================================
// WeaponInventory.cs  — CON EVENTOS PARA SINCRONIZACIÓN
// Carpeta: Scripts/Inventory/
// ------------------------------------------------------------
// CAMBIOS RESPECTO A TU VERSIÓN ANTERIOR:
//
// 1. Se agregan EVENTOS para que UnifiedInventory pueda
//    saber cuándo algo cambió sin acoplarse directamente.
//    Tus métodos existentes (AddWeapon, Equip, DropCurrent, etc.)
//    NO cambian su comportamiento. Solo disparan eventos al final.
//
// 2. Se agrega SetWeaponsFromInventory() para que UnifiedInventory
//    pueda forzar el estado (cuando el jugador arrastra armas en la UI).
//
// 3. Se elimina el input de scroll/hotbar keys porque ahora
//    lo maneja HotbarUI. Si lo tienes comentado, déjalo así.
//
// TODO EN UNITY:
//   Reemplaza tu WeaponInventory.cs con este archivo.
//   No necesitas cambiar el prefab ni el Inspector.
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;


// ── Nota: quitamos la dependencia de InputSystem ──
// Si aún tienes PlayerInput en el Inspector, solo deja el
// campo serializado pero no lo uses para el scroll/hotbar.

public class WeaponInventory : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // CONFIGURACIÓN (igual que antes)
    // ─────────────────────────────────────────────────────────

    [Header("Slots")]
    [SerializeField] int maxSlots = 5;
    [SerializeField] Transform weaponHolder;

    [Header("Drop")]
    [SerializeField] float dropForce = 5f;
    [SerializeField] Vector3 randomRotationMin = new Vector3(-60f, 0f, -60f);
    [SerializeField] Vector3 randomRotationMax = new Vector3(60f, 360f, 60f);
    [SerializeField] float randomAngularForce = 4f;

    [Header("Player Ammo Inventory")]
    [SerializeField] AmmoInventory playerAmmoInventory;

    [Header("UI Legacy")]
    [SerializeField] AmmoUIController ammoUI;

    [Header("Default Weapon")]
    [SerializeField] Weapon defaultWeaponPrefab;
    [SerializeField] bool giveDefaultWeaponOnStart = true;

    [Header("Drop Warning UI")]
    [SerializeField] TextMeshProUGUI dropWarningText;
    [SerializeField] float warningDuration = 2f;
    [SerializeField] float fadeInTime  = 0.15f;
    [SerializeField] float fadeOutTime = 0.25f;
    [SerializeField] float popScale    = 1.25f;
    [SerializeField] float popTime     = 0.15f;
    [SerializeField] float shakeAmount = 6f;

    // ─────────────────────────────────────────────────────────
    // EVENTOS — UnifiedInventory se suscribe a estos
    // ─────────────────────────────────────────────────────────

    /// <summary>Se dispara cuando una arma se agrega a un slot.</summary>
    public event Action<int, Weapon> OnWeaponAdded;

    /// <summary>Se dispara cuando una arma se quita de un slot.</summary>
    public event Action<int> OnWeaponRemoved;

    /// <summary>Se dispara cuando cambia el arma equipada. int = nuevo índice.</summary>
    public event Action<int> OnWeaponEquipped;

    /// <summary>Se dispara cuando toda la lista de armas fue reordenada.</summary>
    public event Action OnWeaponsRebuilt;

    // ─────────────────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────────────────

    readonly List<Weapon> slots = new();
    int currentIndex = -1;

    Coroutine warningRoutine;
    Vector3 warningOriginalScale;
    Vector2 warningOriginalPos;
    bool warningInitialized;

    // ─────────────────────────────────────────────────────────
    // PROPIEDADES PÚBLICAS
    // ─────────────────────────────────────────────────────────

    public bool IsFull       => slots.Count >= maxSlots;
    public int  CurrentIndex => currentIndex;
    public int  SlotCount    => slots.Count;
    public int  MaxSlots     => maxSlots;

    public Weapon CurrentWeapon
    {
        get
        {
            if (currentIndex < 0 || currentIndex >= slots.Count) return null;
            return slots[currentIndex];
        }
    }

    // ─────────────────────────────────────────────────────────
    // AWAKE
    // ─────────────────────────────────────────────────────────

    void Awake()
    {
        if (giveDefaultWeaponOnStart && defaultWeaponPrefab != null)
            SpawnAndAddDefaultWeapon();
    }

    // ─────────────────────────────────────────────────────────
    // ADD WEAPON
    // ─────────────────────────────────────────────────────────

    public void AddWeapon(Weapon weapon)
    {
        if (IsFull || weapon == null) return;

        PrepareAsEquipped(weapon);

        if (playerAmmoInventory != null)
            weapon.AssignAmmoInventory(playerAmmoInventory);

        int transferred = weapon.TransferAmmoToInventory();

        weapon.transform.SetParent(weaponHolder);
        ApplyWeaponOffset(weapon);
        weapon.gameObject.SetActive(false);

        int slotIndex = slots.Count;
        slots.Add(weapon);

        // Evento: informar que se agregó en slotIndex
        OnWeaponAdded?.Invoke(slotIndex, weapon);

        if (currentIndex == -1)
            Equip(0);
    }

    // ─────────────────────────────────────────────────────────
    // EQUIP
    // ─────────────────────────────────────────────────────────

    public void Equip(int index)
    {
        if (slots.Count == 0) return;
        index = Mathf.Clamp(index, 0, slots.Count - 1);

        if (currentIndex >= 0 && currentIndex < slots.Count)
            slots[currentIndex].gameObject.SetActive(false);

        currentIndex = index;
        Weapon w = slots[currentIndex];
        ApplyWeaponOffset(w);

        var aim = w.GetComponent<WeaponAimController>();
        if (aim && TryGetComponent<PlayerWeaponContext>(out var ctx))
            aim.InjectContext(ctx);

        w.gameObject.SetActive(true);

        if (playerAmmoInventory != null && w.stats != null)
            playerAmmoInventory.SetCurrentAmmoType(w.stats.ammoType);

        if (ammoUI != null)
            ammoUI.SetCurrentWeapon(w);

        // Evento
        OnWeaponEquipped?.Invoke(currentIndex);
    }

    // ─────────────────────────────────────────────────────────
    // DROP CURRENT
    // ─────────────────────────────────────────────────────────

    public void DropCurrent()
    {
        if (slots.Count == 0 || currentIndex < 0 || currentIndex >= slots.Count) return;

        Weapon w = slots[currentIndex];
        if (w.isDefaultWeapon) { ShowDropDefaultWarning(); return; }

        int droppedIndex = currentIndex;
        slots.RemoveAt(currentIndex);

        var aim = w.GetComponent<WeaponAimController>();
        if (aim) aim.ForceStopAim();

        PrepareAsDropped(w);
        w.transform.SetParent(null);
        w.transform.position = transform.position + transform.forward;

        w.transform.rotation = Quaternion.Euler(
            Random.Range(randomRotationMin.x, randomRotationMax.x),
            Random.Range(randomRotationMin.y, randomRotationMax.y),
            Random.Range(randomRotationMin.z, randomRotationMax.z));

        if (w.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.AddForce(transform.forward * dropForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * randomAngularForce, ForceMode.Impulse);
        }

        // Evento: arma quitada
        OnWeaponRemoved?.Invoke(droppedIndex);

        if (slots.Count == 0)
        {
            currentIndex = -1;
            if (ammoUI != null) ammoUI.SetCurrentWeapon(null);
            OnWeaponEquipped?.Invoke(-1);
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, slots.Count - 1);
        Equip(currentIndex);
    }

    // ─────────────────────────────────────────────────────────
    // NUEVO MÉTODO: SetWeaponsFromInventory
    // Llamado por UnifiedInventory cuando el jugador arrastra
    // armas en la UI. Reconstruye la lista de armas en el orden
    // que dicta UnifiedInventory.
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Reemplaza el orden de armas con la lista indicada.
    /// weapons[i] puede ser null (slot vacío en la hotbar).
    /// activeIndex: qué índice de la lista debe equiparse.
    /// </summary>
    public void SetWeaponsFromInventory(Weapon[] weapons, int activeIndex)
    {
        // Desactivar todas las armas actuales
        foreach (var w in slots)
            if (w != null) w.gameObject.SetActive(false);

        slots.Clear();
        currentIndex = -1;

        foreach (var w in weapons)
        {
            if (w == null) continue;

            PrepareAsEquipped(w);
            if (playerAmmoInventory != null)
                w.AssignAmmoInventory(playerAmmoInventory);

            w.transform.SetParent(weaponHolder);
            ApplyWeaponOffset(w);
            w.gameObject.SetActive(false);

            slots.Add(w);
        }

        // Notificar que la lista fue reconstruida
        OnWeaponsRebuilt?.Invoke();

        // Equipar el activo
        if (slots.Count > 0)
        {
            int clampedActive = Mathf.Clamp(activeIndex, 0, slots.Count - 1);
            Equip(clampedActive);
        }
    }

    // ─────────────────────────────────────────────────────────
    // LECTURA (para que UnifiedInventory inicialice sus slots)
    // ─────────────────────────────────────────────────────────

    /// <summary>Retorna el arma en el slot index (puede ser null).</summary>
    public Weapon GetWeaponAtUI(int index)
    {
        if (index < 0 || index >= slots.Count) return null;
        return slots[index];
    }

    // ─────────────────────────────────────────────────────────
    // EQUIP NEXT / PREVIOUS (ahora llamados por HotbarUI)
    // ─────────────────────────────────────────────────────────

    public void EquipNext()
    {
        if (!TryCancelAim() || slots.Count == 0) return;
        Equip((currentIndex + 1) % slots.Count);
    }

    public void EquipPrevious()
    {
        if (!TryCancelAim() || slots.Count == 0) return;
        Equip((currentIndex - 1 + slots.Count) % slots.Count);
    }

    public void PickupWeapon(Weapon newWeapon)
    {
        if (newWeapon == null) return;
        if (!IsFull) { AddWeapon(newWeapon); return; }
        SwapCurrentWeapon(newWeapon);
    }

    // ─────────────────────────────────────────────────────────
    // HELPERS (sin cambios respecto al original)
    // ─────────────────────────────────────────────────────────

    bool TryCancelAim()
    {
        var aim = CurrentWeapon?.GetComponent<WeaponAimController>();
        if (aim && aim.IsAiming) { aim.ForceStopAim(); return false; }
        return true;
    }

    void ApplyWeaponOffset(Weapon weapon)
    {
        weapon.transform.localPosition = weapon.GetInventoryPositionOffset();
        weapon.ApplyInventoryRotation(weapon.GetInventoryRotationOffset());
    }

    void PrepareAsEquipped(Weapon w)
    {
        if (w.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
        foreach (Collider c in w.GetComponentsInChildren<Collider>()) c.enabled = false;
        w.enabled = true;
        if (w.TryGetComponent<WeaponAudio>(out var audio))  audio.enabled = true;
        if (w.TryGetComponent<AudioSource>(out var src))    src.enabled = true;
        if (w.TryGetComponent<WeaponAimController>(out var aim)) aim.enabled = true;
    }

    void PrepareAsDropped(Weapon w)
    {
        if (w.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = false;
        foreach (Collider c in w.GetComponentsInChildren<Collider>()) c.enabled = true;
        w.enabled = false;
        if (w.TryGetComponent<WeaponAudio>(out var audio)) audio.enabled = false;
        if (w.TryGetComponent<AudioSource>(out var src))   src.enabled = false;
        if (w.TryGetComponent<WeaponAimController>(out var aim)) aim.enabled = false;
    }

    void SwapCurrentWeapon(Weapon newWeapon)
    {
        if (currentIndex < 0 || currentIndex >= slots.Count) return;
        Weapon old = slots[currentIndex];
        if (old.isDefaultWeapon) { ShowDropDefaultWarning(); return; }

        int swapIndex = currentIndex;
        slots.RemoveAt(currentIndex);

        var aim = old.GetComponent<WeaponAimController>();
        if (aim) aim.ForceStopAim();

        PrepareAsDropped(old);
        old.transform.SetParent(null);
        old.transform.position = transform.position + transform.forward;
        old.transform.rotation = Quaternion.Euler(
            Random.Range(randomRotationMin.x, randomRotationMax.x),
            Random.Range(randomRotationMin.y, randomRotationMax.y),
            Random.Range(randomRotationMin.z, randomRotationMax.z));

        if (old.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.AddForce(transform.forward * dropForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * randomAngularForce, ForceMode.Impulse);
        }

        OnWeaponRemoved?.Invoke(swapIndex);

        PrepareAsEquipped(newWeapon);
        if (playerAmmoInventory != null) newWeapon.AssignAmmoInventory(playerAmmoInventory);
        newWeapon.TransferAmmoToInventory();
        newWeapon.transform.SetParent(weaponHolder);
        ApplyWeaponOffset(newWeapon);
        newWeapon.gameObject.SetActive(false);

        slots.Insert(currentIndex, newWeapon);
        OnWeaponAdded?.Invoke(currentIndex, newWeapon);

        Equip(currentIndex);
    }

    void SpawnAndAddDefaultWeapon()
    {
        Weapon w = Instantiate(defaultWeaponPrefab);
        w.isDefaultWeapon = true;
        if (w.magazine != null) w.magazine.currentBullets = w.magazine.maxBullets;
        w.SendMessage("MarkAmmoInitialized", SendMessageOptions.DontRequireReceiver);
        AddWeapon(w);
    }

    // ─────────────────────────────────────────────────────────
    // DROP WARNING UI (sin cambios)
    // ─────────────────────────────────────────────────────────

    void ShowDropDefaultWarning()
    {
        if (dropWarningText == null) return;

        RectTransform rect = dropWarningText.rectTransform;
        if (!warningInitialized)
        {
            warningOriginalScale = rect.localScale;
            warningOriginalPos   = rect.anchoredPosition;
            warningInitialized   = true;
        }

        rect.localScale           = warningOriginalScale;
        rect.anchoredPosition     = warningOriginalPos;
        dropWarningText.color     = new Color(dropWarningText.color.r, dropWarningText.color.g,
                                              dropWarningText.color.b, 0f);
        dropWarningText.enabled   = true;

        if (warningRoutine != null) StopCoroutine(warningRoutine);
        warningRoutine = StartCoroutine(DropWarningRoutine());
    }

    IEnumerator DropWarningRoutine()
    {
        dropWarningText.text    = "NO SE PUEDE DROPEAR EL ARMA DEFAULT";
        dropWarningText.enabled = true;

        RectTransform rect      = dropWarningText.rectTransform;
        Vector2 origPos         = warningOriginalPos;
        Vector3 origScale       = warningOriginalScale;
        Color   baseColor       = new Color(dropWarningText.color.r, dropWarningText.color.g,
                                             dropWarningText.color.b, 0f);
        dropWarningText.color   = baseColor;

        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            float p = t / fadeInTime;
            dropWarningText.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(0f, 1f, p));
            rect.localScale = origScale * Mathf.Lerp(1f, popScale, p);
            yield return null;
        }

        t = 0f;
        while (t < popTime)
        {
            t += Time.deltaTime;
            rect.localScale = origScale * Mathf.Lerp(popScale, 1f, t / popTime);
            yield return null;
        }

        float timer = 0f;
        while (timer < warningDuration)
        {
            timer += Time.deltaTime;
            rect.anchoredPosition = origPos + new Vector2(
                Random.Range(-1f, 1f) * shakeAmount,
                Random.Range(-1f, 1f) * shakeAmount);
            yield return null;
        }

        rect.anchoredPosition = origPos;
        t = 0f;
        Color cur = dropWarningText.color;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            dropWarningText.color = new Color(cur.r, cur.g, cur.b, Mathf.Lerp(cur.a, 0f, t / fadeOutTime));
            yield return null;
        }

        dropWarningText.enabled = false;
        dropWarningText.text    = "";
        rect.localScale         = warningOriginalScale;
        rect.anchoredPosition   = warningOriginalPos;
    }
}
