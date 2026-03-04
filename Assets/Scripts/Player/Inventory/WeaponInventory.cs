// ============================================================
// WeaponInventory.cs — INTEGRACIÓN CON WeaponSwitchAnimator
// ============================================================
// CAMBIOS PRINCIPALES:
//
// 1. CAMBIO DE ARMA CON ANIMACIÓN Y COOLDOWN:
//    EquipNext(), EquipPrevious(), y SetActiveHotbar() (desde
//    HotbarUI con teclas numéricas/scroll) ahora pasan por
//    RequestSwitch() en WeaponSwitchAnimator.
//    Esto ejecuta: bajar arma → swap → subir arma.
//    Si está en cooldown o animación, el input se ignora.
//
// 2. DROP = INSTANTÁNEO:
//    DropCurrent() usa ForceInstantSwitch() para que al soltar
//    el arma no haya animación de delay (se siente más natural
//    porque el arma sale volando de la mano).
//
// 3. COOLDOWN VISUAL EN EL INSPECTOR:
//    WeaponSwitchAnimator.IsBusy bloquea inputs repetidos.
//    El cooldown se configura en WeaponSwitchAnimator (campo
//    switchCooldown, default 0.25s).
//
// JERARQUÍA DE LLAMADAS:
//   Input (HotbarUI / scroll) →
//     RequestEquip(index) →
//       WeaponSwitchAnimator.RequestSwitch(callback) →
//         [animación holster] →
//           callback: DoEquip(index) →
//             [animación draw]
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class WeaponInventory : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // CONFIGURACIÓN
    // ─────────────────────────────────────────────────────────

    [Header("Slots")]
    [SerializeField] int maxSlots = 5;
    [SerializeField] Transform weaponHolder;

    [Header("Contexto del Jugador")]
    [SerializeField] PlayerWeaponContext playerContext;

    [Header("Animación de Cambio")]
    [Tooltip("WeaponSwitchAnimator. Si está vacío se busca en el mismo GO.")]
    [SerializeField] WeaponSwitchAnimator switchAnimator;

    [Header("Drop")]
    [SerializeField] float   dropForce         = 5f;
    [SerializeField] Vector3 randomRotationMin = new Vector3(-60f, 0f, -60f);
    [SerializeField] Vector3 randomRotationMax = new Vector3(60f, 360f, 60f);
    [SerializeField] float   randomAngularForce = 4f;

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
    [SerializeField] float fadeInTime      = 0.15f;
    [SerializeField] float fadeOutTime     = 0.25f;
    [SerializeField] float popScale        = 1.25f;
    [SerializeField] float popTime         = 0.15f;
    [SerializeField] float shakeAmount     = 6f;

    // ─────────────────────────────────────────────────────────
    // EVENTOS
    // ─────────────────────────────────────────────────────────

    public event Action<int, Weapon> OnWeaponAdded;
    public event Action<int>         OnWeaponRemoved;
    public event Action<int>         OnWeaponEquipped;
    public event Action              OnWeaponsRebuilt;

    // ─────────────────────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────────────────────

    readonly List<Weapon> slots = new();
    int currentIndex = -1;

    Coroutine warningRoutine;
    Vector3   warningOriginalScale;
    Vector2   warningOriginalPos;
    bool      warningInitialized;

    // ─────────────────────────────────────────────────────────
    // PROPIEDADES
    // ─────────────────────────────────────────────────────────

    public bool   IsFull       => slots.Count >= maxSlots;
    public int    CurrentIndex => currentIndex;
    public int    SlotCount    => slots.Count;
    public int    MaxSlots     => maxSlots;

    /// <summary>True si hay una animación de switch en curso o en cooldown.</summary>
    public bool IsSwitching    => switchAnimator != null && switchAnimator.IsBusy;

    public Weapon CurrentWeapon =>
        (currentIndex >= 0 && currentIndex < slots.Count) ? slots[currentIndex] : null;

    // ─────────────────────────────────────────────────────────
    // AWAKE
    // ─────────────────────────────────────────────────────────

    void Awake()
    {
        // Auto-detectar WeaponSwitchAnimator
        if (switchAnimator == null)
            switchAnimator = GetComponent<WeaponSwitchAnimator>();

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
        if (playerAmmoInventory != null) weapon.AssignAmmoInventory(playerAmmoInventory);
        weapon.TransferAmmoToInventory();
        weapon.transform.SetParent(weaponHolder);
        ApplyWeaponOffset(weapon);
        weapon.gameObject.SetActive(false);

        int slotIndex = slots.Count;
        slots.Add(weapon);
        OnWeaponAdded?.Invoke(slotIndex, weapon);

        // Primera arma: equipar instantáneo (sin animación, el juego acaba de iniciar)
        if (currentIndex == -1)
            DoEquip(0);
    }

    // ─────────────────────────────────────────────────────────
    // REQUEST EQUIP — con animación y cooldown
    // El punto de entrada para cambios de arma iniciados por el jugador.
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Pide cambiar al arma en el índice dado.
    /// Pasa por WeaponSwitchAnimator: holster → swap → draw.
    /// Ignorado si ya hay una animación en curso.
    /// </summary>
    public void RequestEquip(int index)
    {
        if (slots.Count == 0) return;
        index = Mathf.Clamp(index, 0, slots.Count - 1);
        if (index == currentIndex) return;         // ya equipada
        if (!TryCancelAim()) return;               // en medio de zoom

        if (switchAnimator != null)
        {
            // Con animación
            int captured = index;
            switchAnimator.RequestSwitch(() => DoEquip(captured));
        }
        else
        {
            // Sin animador: equip instantáneo
            DoEquip(index);
        }
    }

    // ─────────────────────────────────────────────────────────
    // DO EQUIP — swap real de arma (llamado como callback)
    // ─────────────────────────────────────────────────────────

    void DoEquip(int index)
    {
        if (slots.Count == 0) return;
        index = Mathf.Clamp(index, 0, slots.Count - 1);

        // Ocultar arma anterior
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

    // ─────────────────────────────────────────────────────────
    // EQUIP — llamado desde SetWeaponsFromInventory y AddWeapon
    //         (usos internos que no pasan por animación)
    // ─────────────────────────────────────────────────────────

    public void Equip(int index) => DoEquip(index);

    // ─────────────────────────────────────────────────────────
    // DROP CURRENT — instantáneo, sin animación
    // ─────────────────────────────────────────────────────────

    public void DropCurrent()
    {
        if (slots.Count == 0 || currentIndex < 0 || currentIndex >= slots.Count) return;

        Weapon w = slots[currentIndex];
        if (w.isDefaultWeapon) { ShowDropDefaultWarning(); return; }

        slots.RemoveAt(currentIndex);

        var aim = w.GetComponent<WeaponAimController>();
        if (aim) aim.ForceStopAim();

        PrepareAsDropped(w);

        w.transform.SetParent(null);
        w.transform.position = transform.position + transform.forward;
        w.transform.rotation = RandomRot();

        if (w.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.AddForce(transform.forward * dropForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * randomAngularForce, ForceMode.Impulse);
        }

        OnWeaponRemoved?.Invoke(currentIndex);

        if (slots.Count == 0)
        {
            currentIndex = -1;
            if (ammoUI) ammoUI.SetCurrentWeapon(null);
            OnWeaponEquipped?.Invoke(-1);
            OnWeaponsRebuilt?.Invoke();
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, slots.Count - 1);

        // Drop usa ForceInstantSwitch para no tener delay raro
        if (switchAnimator != null)
        {
            int captured = currentIndex;
            switchAnimator.ForceInstantSwitch(() => DoEquip(captured));
        }
        else
        {
            DoEquip(currentIndex);
        }

        OnWeaponsRebuilt?.Invoke();
    }

    // ─────────────────────────────────────────────────────────
    // SET WEAPONS FROM INVENTORY (drag & drop de UI)
    // ─────────────────────────────────────────────────────────

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
        if (slots.Count > 0) DoEquip(Mathf.Clamp(activeIndex, 0, slots.Count - 1));
    }

    // ─────────────────────────────────────────────────────────
    // EQUIP NEXT / PREV — pasan por RequestEquip (con animación)
    // ─────────────────────────────────────────────────────────

    public void EquipNext()
    {
        if (slots.Count == 0) return;
        RequestEquip((currentIndex + 1) % slots.Count);
    }

    public void EquipPrevious()
    {
        if (slots.Count == 0) return;
        RequestEquip((currentIndex - 1 + slots.Count) % slots.Count);
    }

    public void PickupWeapon(Weapon newWeapon)
    {
        if (newWeapon == null) return;
        if (!IsFull) { AddWeapon(newWeapon); return; }
        SwapCurrentWeapon(newWeapon);
    }

    // ─────────────────────────────────────────────────────────
    // LECTURA
    // ─────────────────────────────────────────────────────────

    public Weapon GetWeaponAtUI(int index) =>
        (index >= 0 && index < slots.Count) ? slots[index] : null;

    // ─────────────────────────────────────────────────────────
    // HELPERS
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

        // FIX: reactivar GO + resetear pickup para poder recoger de nuevo
        w.gameObject.SetActive(true);
        var pickup = w.GetComponent<WeaponPickup>();
        if (pickup != null) pickup.ResetForDrop();
    }

    Quaternion RandomRot() => Quaternion.Euler(
        Random.Range(randomRotationMin.x, randomRotationMax.x),
        Random.Range(randomRotationMin.y, randomRotationMax.y),
        Random.Range(randomRotationMin.z, randomRotationMax.z));

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
        old.transform.rotation = RandomRot();

        if (old.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.AddForce(transform.forward * dropForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * randomAngularForce, ForceMode.Impulse);
        }

        OnWeaponRemoved?.Invoke(currentIndex);

        PrepareAsEquipped(newWeapon);
        if (playerAmmoInventory != null) newWeapon.AssignAmmoInventory(playerAmmoInventory);
        newWeapon.TransferAmmoToInventory();
        newWeapon.transform.SetParent(weaponHolder);
        ApplyWeaponOffset(newWeapon);
        newWeapon.gameObject.SetActive(false);
        slots.Insert(currentIndex, newWeapon);
        OnWeaponAdded?.Invoke(currentIndex, newWeapon);

        DoEquip(currentIndex);
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

    // ─────────────────────────────────────────────────────────
    // DROP WARNING UI
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
        rect.localScale        = warningOriginalScale;
        rect.anchoredPosition  = warningOriginalPos;
        dropWarningText.color  = new Color(dropWarningText.color.r,
                                           dropWarningText.color.g,
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
        Color baseColor = new Color(dropWarningText.color.r,
                                     dropWarningText.color.g,
                                     dropWarningText.color.b, 0f);
        dropWarningText.color = baseColor;

        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.deltaTime; float p = t / fadeInTime;
            dropWarningText.color = new Color(baseColor.r, baseColor.g, baseColor.b,
                                              Mathf.Lerp(0f, 1f, p));
            rect.localScale = warningOriginalScale * Mathf.Lerp(1f, popScale, p);
            yield return null;
        }
        t = 0f;
        while (t < popTime)
        {
            t += Time.deltaTime;
            rect.localScale = warningOriginalScale * Mathf.Lerp(popScale, 1f, t / popTime);
            yield return null;
        }

        float timer = 0f;
        while (timer < warningDuration)
        {
            timer += Time.deltaTime;
            rect.anchoredPosition = warningOriginalPos + new Vector2(
                Random.Range(-1f, 1f) * shakeAmount,
                Random.Range(-1f, 1f) * shakeAmount);
            yield return null;
        }

        rect.anchoredPosition = warningOriginalPos;
        t = 0f; Color cur = dropWarningText.color;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            dropWarningText.color = new Color(cur.r, cur.g, cur.b,
                                              Mathf.Lerp(cur.a, 0f, t / fadeOutTime));
            yield return null;
        }
        dropWarningText.enabled = false; dropWarningText.text = "";
        rect.localScale = warningOriginalScale; rect.anchoredPosition = warningOriginalPos;
    }
}
