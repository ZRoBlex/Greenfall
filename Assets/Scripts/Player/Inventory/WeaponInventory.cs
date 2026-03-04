// ============================================================
// WeaponInventory.cs — FIXES DE SINCRONIZACIÓN
// ============================================================
// CAMBIOS:
//
// 1. DropCurrent() y SwapCurrentWeapon() ahora disparan
//    OnWeaponsRebuilt al FINAL (después del Equip).
//    Esto le dice a UnifiedInventory "rehaz todo desde cero".
//    Es más confiable que enviar índices individuales que
//    se invalidan cuando la lista se comprime al quitar items.
//
// 2. PrepareAsDropped() llama WeaponPickup.ResetForDrop().
//    Sin esto, el arma dropeada tiene _hasBeenPickedUp=true
//    y no se puede volver a recoger (daba "inventario lleno").
//
// 3. playerContext restaurado como [SerializeField].
//    (ya estaba en la versión anterior, se mantiene)
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class WeaponInventory : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] int maxSlots = 5;
    [SerializeField] Transform weaponHolder;

    [Header("Contexto del Jugador")]
    [SerializeField] PlayerWeaponContext playerContext;

    [Header("Drop")]
    [SerializeField] float dropForce = 5f;
    [SerializeField] Vector3 randomRotationMin = new Vector3(-60f, 0f, -60f);
    [SerializeField] Vector3 randomRotationMax = new Vector3(60f, 360f, 60f);
    [SerializeField] float randomAngularForce  = 4f;

    [Header("Player Ammo Inventory")]
    [SerializeField] AmmoInventory playerAmmoInventory;

    [Header("UI")]
    [SerializeField] AmmoUIController ammoUI;

    [Header("Default Weapon")]
    [SerializeField] Weapon defaultWeaponPrefab;
    [SerializeField] bool   giveDefaultWeaponOnStart = true;

    [Header("Drop Warning UI")]
    [SerializeField] TextMeshProUGUI dropWarningText;
    [SerializeField] float warningDuration = 2f;
    [SerializeField] float fadeInTime  = 0.15f;
    [SerializeField] float fadeOutTime = 0.25f;
    [SerializeField] float popScale    = 1.25f;
    [SerializeField] float popTime     = 0.15f;
    [SerializeField] float shakeAmount = 6f;

    // ── Eventos ───────────────────────────────────────────────
    public event Action<int, Weapon> OnWeaponAdded;
    public event Action<int>         OnWeaponRemoved;
    public event Action<int>         OnWeaponEquipped;

    // FIX: este evento ahora se dispara después de un drop completo.
    // UnifiedInventory lo usa para hacer un rebuild total del hotbar.
    public event Action OnWeaponsRebuilt;

    readonly List<Weapon> slots = new();
    int currentIndex = -1;

    Coroutine warningRoutine;
    Vector3 warningOriginalScale;
    Vector2 warningOriginalPos;
    bool warningInitialized;

    public bool   IsFull       => slots.Count >= maxSlots;
    public int    CurrentIndex => currentIndex;
    public int    SlotCount    => slots.Count;
    public int    MaxSlots     => maxSlots;
    public Weapon CurrentWeapon =>
        (currentIndex >= 0 && currentIndex < slots.Count) ? slots[currentIndex] : null;

    // ── Awake ─────────────────────────────────────────────────

    void Awake()
    {
        if (giveDefaultWeaponOnStart && defaultWeaponPrefab != null)
            SpawnAndAddDefaultWeapon();
    }

    // ── Add Weapon ────────────────────────────────────────────

    public void AddWeapon(Weapon weapon)
    {
        if (IsFull || weapon == null) return;

        PrepareAsEquipped(weapon);
        if (playerAmmoInventory != null) weapon.AssignAmmoInventory(playerAmmoInventory);
        weapon.TransferAmmoToInventory();
        weapon.transform.SetParent(weaponHolder);
        ApplyWeaponOffset(weapon);
        weapon.gameObject.SetActive(false);

        int slotIndex = slots.Count;
        slots.Add(weapon);
        OnWeaponAdded?.Invoke(slotIndex, weapon);

        if (currentIndex == -1) Equip(0);
    }

    // ── Equip ─────────────────────────────────────────────────

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
        if (aim != null && playerContext != null)
            aim.InjectContext(playerContext);

        w.gameObject.SetActive(true);

        if (playerAmmoInventory != null && w.stats != null)
            playerAmmoInventory.SetCurrentAmmoType(w.stats.ammoType);

        if (ammoUI != null) ammoUI.SetCurrentWeapon(w);

        OnWeaponEquipped?.Invoke(currentIndex);
    }

    // ── Drop Current ──────────────────────────────────────────

    public void DropCurrent()
    {
        if (slots.Count == 0 || currentIndex < 0 || currentIndex >= slots.Count) return;

        Weapon w = slots[currentIndex];
        if (w.isDefaultWeapon) { ShowDropDefaultWarning(); return; }

        slots.RemoveAt(currentIndex);

        var aim = w.GetComponent<WeaponAimController>();
        if (aim) aim.ForceStopAim();

        PrepareAsDropped(w); // reactiva GO + resetea WeaponPickup

        w.transform.SetParent(null);
        w.transform.position = transform.position + transform.forward;
        w.transform.rotation = Quaternic(randomRotationMin, randomRotationMax);

        if (w.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.AddForce(transform.forward * dropForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * randomAngularForce, ForceMode.Impulse);
        }

        if (slots.Count == 0)
        {
            currentIndex = -1;
            if (ammoUI) ammoUI.SetCurrentWeapon(null);
            OnWeaponEquipped?.Invoke(-1);
            // FIX: rebuild aunque no haya armas (limpiar UI)
            OnWeaponsRebuilt?.Invoke();
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, slots.Count - 1);
        Equip(currentIndex);

        // FIX: rebuild DESPUÉS del equip para que UnifiedInventory
        // lea el estado final correcto (lista ya comprimida + índice correcto)
        OnWeaponsRebuilt?.Invoke();
    }

    // ── Set Weapons From Inventory ────────────────────────────

    public void SetWeaponsFromInventory(Weapon[] weapons, int activeIndex)
    {
        foreach (var w in slots)
            if (w != null) w.gameObject.SetActive(false);

        slots.Clear();
        currentIndex = -1;

        foreach (var w in weapons)
        {
            if (w == null) continue;
            PrepareAsEquipped(w);
            if (playerAmmoInventory != null) w.AssignAmmoInventory(playerAmmoInventory);
            w.transform.SetParent(weaponHolder);
            ApplyWeaponOffset(w);
            w.gameObject.SetActive(false);
            slots.Add(w);
        }

        OnWeaponsRebuilt?.Invoke();

        if (slots.Count > 0)
            Equip(Mathf.Clamp(activeIndex, 0, slots.Count - 1));
    }

    // ── Read ──────────────────────────────────────────────────

    public Weapon GetWeaponAtUI(int index) =>
        (index >= 0 && index < slots.Count) ? slots[index] : null;

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

    // ── Helpers ───────────────────────────────────────────────

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
        if (w.TryGetComponent<Rigidbody>(out var rb))        rb.isKinematic = true;
        foreach (var c in w.GetComponentsInChildren<Collider>()) c.enabled = false;
        w.enabled = true;
        if (w.TryGetComponent<WeaponAudio>(out var audio))       audio.enabled = true;
        if (w.TryGetComponent<AudioSource>(out var src))         src.enabled   = true;
        if (w.TryGetComponent<WeaponAimController>(out var aim)) aim.enabled   = true;
    }

    void PrepareAsDropped(Weapon w)
    {
        if (w.TryGetComponent<Rigidbody>(out var rb))        rb.isKinematic = false;
        foreach (var c in w.GetComponentsInChildren<Collider>()) c.enabled = true;
        w.enabled = false;
        if (w.TryGetComponent<WeaponAudio>(out var audio))       audio.enabled = false;
        if (w.TryGetComponent<AudioSource>(out var src))         src.enabled   = false;
        if (w.TryGetComponent<WeaponAimController>(out var aim)) aim.enabled   = false;

        // FIX: reactivar GO para que aparezca en el mundo
        w.gameObject.SetActive(true);

        // FIX: resetear el pickup para que se pueda recoger de nuevo
        var pickup = w.GetComponent<WeaponPickup>();
        if (pickup != null) pickup.ResetForDrop();
    }

    // Shorthand para rotación aleatoria
    Quaternion Quaternic(Vector3 min, Vector3 max) => Quaternion.Euler(
        Random.Range(min.x, max.x),
        Random.Range(min.y, max.y),
        Random.Range(min.z, max.z));

    void SwapCurrentWeapon(Weapon newWeapon)
    {
        if (currentIndex < 0 || currentIndex >= slots.Count) return;
        Weapon old = slots[currentIndex];
        if (old.isDefaultWeapon) { ShowDropDefaultWarning(); return; }

        slots.RemoveAt(currentIndex);

        var aim = old.GetComponent<WeaponAimController>();
        if (aim) aim.ForceStopAim();

        PrepareAsDropped(old);
        old.transform.SetParent(null);
        old.transform.position = transform.position + transform.forward;
        old.transform.rotation = Quaternic(randomRotationMin, randomRotationMax);

        if (old.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.AddForce(transform.forward * dropForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * randomAngularForce, ForceMode.Impulse);
        }

        PrepareAsEquipped(newWeapon);
        if (playerAmmoInventory != null) newWeapon.AssignAmmoInventory(playerAmmoInventory);
        newWeapon.TransferAmmoToInventory();
        newWeapon.transform.SetParent(weaponHolder);
        ApplyWeaponOffset(newWeapon);
        newWeapon.gameObject.SetActive(false);
        slots.Insert(currentIndex, newWeapon);

        Equip(currentIndex);
        OnWeaponsRebuilt?.Invoke();
    }

    void SpawnAndAddDefaultWeapon()
    {
        Weapon w = Instantiate(defaultWeaponPrefab);
        w.isDefaultWeapon = true;
        if (w.magazine != null) w.magazine.currentBullets = w.magazine.maxBullets;
        w.SendMessage("MarkAmmoInitialized", SendMessageOptions.DontRequireReceiver);
        AddWeapon(w);
    }

    // ── Drop Warning UI ───────────────────────────────────────

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
        rect.localScale        = warningOriginalScale;
        rect.anchoredPosition  = warningOriginalPos;
        dropWarningText.color  = new Color(dropWarningText.color.r, dropWarningText.color.g,
                                           dropWarningText.color.b, 0f);
        dropWarningText.enabled = true;
        if (warningRoutine != null) StopCoroutine(warningRoutine);
        warningRoutine = StartCoroutine(DropWarningRoutine());
    }

    IEnumerator DropWarningRoutine()
    {
        dropWarningText.text    = "NO SE PUEDE DROPEAR EL ARMA DEFAULT";
        dropWarningText.enabled = true;
        RectTransform rect = dropWarningText.rectTransform;
        Color baseColor = new Color(dropWarningText.color.r, dropWarningText.color.g,
                                     dropWarningText.color.b, 0f);
        dropWarningText.color = baseColor;

        float t = 0f;
        while (t < fadeInTime) { t += Time.deltaTime; float p = t / fadeInTime;
            dropWarningText.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(0f, 1f, p));
            rect.localScale = warningOriginalScale * Mathf.Lerp(1f, popScale, p); yield return null; }

        t = 0f;
        while (t < popTime) { t += Time.deltaTime;
            rect.localScale = warningOriginalScale * Mathf.Lerp(popScale, 1f, t / popTime); yield return null; }

        float timer = 0f;
        while (timer < warningDuration) { timer += Time.deltaTime;
            rect.anchoredPosition = warningOriginalPos + new Vector2(
                Random.Range(-1f, 1f) * shakeAmount, Random.Range(-1f, 1f) * shakeAmount);
            yield return null; }

        rect.anchoredPosition = warningOriginalPos;
        t = 0f; Color cur = dropWarningText.color;
        while (t < fadeOutTime) { t += Time.deltaTime;
            dropWarningText.color = new Color(cur.r, cur.g, cur.b, Mathf.Lerp(cur.a, 0f, t / fadeOutTime));
            yield return null; }

        dropWarningText.enabled = false; dropWarningText.text = "";
        rect.localScale = warningOriginalScale; rect.anchoredPosition = warningOriginalPos;
    }
}
