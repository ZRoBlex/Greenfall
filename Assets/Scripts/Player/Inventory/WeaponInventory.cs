// WeaponInventory.cs — BUGS CORREGIDOS
// BUG 1: playerContext restaurado como [SerializeField]
// BUG 2: PrepareAsDropped() llama w.gameObject.SetActive(true)

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

    // FIX BUG 1: restaurado. Arrastra el GO con PlayerWeaponContext en el Inspector.
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

    // Eventos para UnifiedInventory
    public event Action<int, Weapon> OnWeaponAdded;
    public event Action<int>         OnWeaponRemoved;
    public event Action<int>         OnWeaponEquipped;
    public event Action              OnWeaponsRebuilt;

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

    void Awake()
    {
        if (giveDefaultWeaponOnStart && defaultWeaponPrefab != null)
            SpawnAndAddDefaultWeapon();
    }

    // ─── ADD WEAPON ───────────────────────────────────────────
    public void AddWeapon(Weapon weapon)
    {
        if (IsFull || weapon == null) return;

        PrepareAsEquipped(weapon);

        if (playerAmmoInventory != null)
            weapon.AssignAmmoInventory(playerAmmoInventory);

        weapon.TransferAmmoToInventory();
        weapon.transform.SetParent(weaponHolder);
        ApplyWeaponOffset(weapon);
        weapon.gameObject.SetActive(false);

        int slotIndex = slots.Count;
        slots.Add(weapon);
        OnWeaponAdded?.Invoke(slotIndex, weapon);

        if (currentIndex == -1) Equip(0);
    }

    // ─── EQUIP ────────────────────────────────────────────────
    public void Equip(int index)
    {
        if (slots.Count == 0) return;
        index = Mathf.Clamp(index, 0, slots.Count - 1);

        if (currentIndex >= 0 && currentIndex < slots.Count)
            slots[currentIndex].gameObject.SetActive(false);

        currentIndex = index;
        Weapon w = slots[currentIndex];
        ApplyWeaponOffset(w);

        // FIX BUG 1: campo serializado, no TryGetComponent
        var aim = w.GetComponent<WeaponAimController>();
        if (aim != null && playerContext != null)
            aim.InjectContext(playerContext);

        w.gameObject.SetActive(true);

        if (playerAmmoInventory != null && w.stats != null)
            playerAmmoInventory.SetCurrentAmmoType(w.stats.ammoType);

        if (ammoUI != null) ammoUI.SetCurrentWeapon(w);

        OnWeaponEquipped?.Invoke(currentIndex);
    }

    // ─── DROP CURRENT ─────────────────────────────────────────
    public void DropCurrent()
    {
        if (slots.Count == 0 || currentIndex < 0 || currentIndex >= slots.Count) return;

        Weapon w = slots[currentIndex];
        if (w.isDefaultWeapon) { ShowDropDefaultWarning(); return; }

        int droppedIndex = currentIndex;
        slots.RemoveAt(currentIndex);

        var aim = w.GetComponent<WeaponAimController>();
        if (aim) aim.ForceStopAim();

        PrepareAsDropped(w);   // FIX BUG 2: reactiva el GO aquí

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

        OnWeaponRemoved?.Invoke(droppedIndex);

        if (slots.Count == 0)
        {
            currentIndex = -1;
            if (ammoUI) ammoUI.SetCurrentWeapon(null);
            OnWeaponEquipped?.Invoke(-1);
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, slots.Count - 1);
        Equip(currentIndex);
    }

    // ─── SET WEAPONS FROM INVENTORY ───────────────────────────
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

    // ─── LECTURA ──────────────────────────────────────────────
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

    // ─── HELPERS ──────────────────────────────────────────────
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

        // ── FIX BUG 2 ─────────────────────────────────────────
        // El arma fue desactivada en AddWeapon() para no mostrarse
        // mientras está en el inventario sin estar equipada.
        // Al soltar, DEBE reactivarse para aparecer en el mundo.
        // Sin esta línea: invisible, slot ocupado, sin poder recoger más.
        w.gameObject.SetActive(true);
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

    // ─── DROP WARNING ──────────────────────────────────────────
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
        rect.localScale       = warningOriginalScale;
        rect.anchoredPosition = warningOriginalPos;
        dropWarningText.color = new Color(dropWarningText.color.r, dropWarningText.color.g,
                                          dropWarningText.color.b, 0f);
        dropWarningText.enabled = true;
        if (warningRoutine != null) StopCoroutine(warningRoutine);
        warningRoutine = StartCoroutine(DropWarningRoutine());
    }

    IEnumerator DropWarningRoutine()
    {
        dropWarningText.text    = "NO SE PUEDE DROPEAR EL ARMA DEFAULT";
        dropWarningText.enabled = true;
        RectTransform rect  = dropWarningText.rectTransform;
        Vector2 origPos     = warningOriginalPos;
        Vector3 origScale   = warningOriginalScale;
        Color   baseColor   = new Color(dropWarningText.color.r, dropWarningText.color.g,
                                         dropWarningText.color.b, 0f);
        dropWarningText.color = baseColor;

        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.deltaTime; float p = t / fadeInTime;
            dropWarningText.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(0f, 1f, p));
            rect.localScale = origScale * Mathf.Lerp(1f, popScale, p);
            yield return null;
        }
        t = 0f;
        while (t < popTime) { t += Time.deltaTime; rect.localScale = origScale * Mathf.Lerp(popScale, 1f, t / popTime); yield return null; }

        float timer = 0f;
        while (timer < warningDuration)
        {
            timer += Time.deltaTime;
            rect.anchoredPosition = origPos + new Vector2(Random.Range(-1f, 1f) * shakeAmount, Random.Range(-1f, 1f) * shakeAmount);
            yield return null;
        }
        rect.anchoredPosition = origPos;
        t = 0f; Color cur = dropWarningText.color;
        while (t < fadeOutTime) { t += Time.deltaTime; dropWarningText.color = new Color(cur.r, cur.g, cur.b, Mathf.Lerp(cur.a, 0f, t / fadeOutTime)); yield return null; }
        dropWarningText.enabled = false; dropWarningText.text = "";
        rect.localScale = warningOriginalScale; rect.anchoredPosition = warningOriginalPos;
    }
}
