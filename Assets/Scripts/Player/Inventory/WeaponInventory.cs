using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponInventory : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] int maxSlots = 5;
    [SerializeField] Transform weaponHolder;

    [Header("Drop")]
    [SerializeField] float dropForce = 5f;

    [SerializeField] Vector3 randomRotationMin = new Vector3(-60f, 0f, -60f);
    [SerializeField] Vector3 randomRotationMax = new Vector3(60f, 360f, 60f);

    [SerializeField] float randomAngularForce = 4f;

    [SerializeField] PlayerInput playerInput;
    [SerializeField] PlayerWeaponContext playerContext;

    [Header("Player Ammo Inventory")]
    [SerializeField] AmmoInventory playerAmmoInventory;

    [Header("UI")]
    [SerializeField] AmmoUIController ammoUI;

    [Header("Default Weapon")]
    [SerializeField] Weapon defaultWeaponPrefab;
    [SerializeField] bool giveDefaultWeaponOnStart = true;

    [Header("Drop Warning UI")]
    [SerializeField] TextMeshProUGUI dropWarningText;
    [SerializeField] float warningDuration = 2f;

    [Header("Fade Settings")]
    [SerializeField] float fadeInTime = 0.15f;
    [SerializeField] float fadeOutTime = 0.25f;

    [Header("Scale Pop Settings")]
    [SerializeField] float popScale = 1.25f;
    [SerializeField] float popTime = 0.15f;

    [Header("Shake Settings")]
    [SerializeField] float shakeAmount = 6f;


    [Header("Inventory UI")]
    [SerializeField] WeaponInventoryUI inventoryUI;

    float lastScrollTime;
    [SerializeField] float scrollCooldown = 0.1f;

    InputAction nextSlotAction;

    Coroutine warningRoutine;


    Vector3 warningOriginalScale;
    Vector2 warningOriginalPos;
    bool warningInitialized;



    InputAction reloadAction;


    readonly List<Weapon> slots = new();
    int currentIndex = -1;

    public bool IsFull => slots.Count >= maxSlots;

    void Awake()
    {
        if (playerInput != null)
        {
            // Reload
            reloadAction = playerInput.actions["Reload"];
            reloadAction.performed += OnReload;

            // Inventory / NextSlot
            var inventoryMap = playerInput.actions.FindActionMap("Inventory", true);
            nextSlotAction = inventoryMap.FindAction("NextSlot", true);

            nextSlotAction.performed += OnNextSlot;
            nextSlotAction.Enable();
        }

        if (giveDefaultWeaponOnStart && defaultWeaponPrefab != null)
        {
            SpawnAndAddDefaultWeapon();
        }
    }





    // -----------------------------
    public void AddWeapon(Weapon weapon)
    {
        if (IsFull || weapon == null) return;

        PrepareAsEquipped(weapon);

        // Asignar el inventario de munición del jugador
        if (playerAmmoInventory != null)
            weapon.AssignAmmoInventory(playerAmmoInventory);


        // 🔹 Transferir balas al inventario
        int transferred = weapon.TransferAmmoToInventory();
        Debug.Log($"{weapon.name} dio {transferred} balas al inventario");



        weapon.transform.SetParent(weaponHolder);
        ApplyWeaponOffset(weapon);

        weapon.gameObject.SetActive(false);
        slots.Add(weapon);

        if (currentIndex == -1)
            Equip(0);

        if (inventoryUI != null)
            inventoryUI.Refresh();



    }

    // -----------------------------
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
        if (aim)
            aim.InjectContext(playerContext);

        w.gameObject.SetActive(true);

        if (playerAmmoInventory != null && w.stats != null)
            playerAmmoInventory.SetCurrentAmmoType(w.stats.ammoType);

        if (ammoUI != null)
            ammoUI.SetCurrentWeapon(w);

        // 🔥 ESTA ES LA LÍNEA QUE TE FALTA
        if (inventoryUI != null)
            inventoryUI.Refresh();

    }




    // -----------------------------
    public void DropCurrent()
    {
        if (slots.Count == 0) return;
        if (currentIndex < 0 || currentIndex >= slots.Count) return;

        Weapon w = slots[currentIndex];

        // 🔒 BLOQUEAR DROP SI ES ARMA DEFAULT
        if (w.isDefaultWeapon)
        {
            Debug.Log("🟡 No puedes soltar el arma default");
            ShowDropDefaultWarning();
            return;
        }


        inventoryUI?.Refresh();


        // -------------------------
        // flujo normal de drop
        // -------------------------

        slots.RemoveAt(currentIndex);

        var aim = w.GetComponent<WeaponAimController>();
        if (aim)
            aim.ForceStopAim();

        PrepareAsDropped(w);

        w.transform.SetParent(null);
        w.transform.position = transform.position + transform.forward;

        // 🎲 Rotación aleatoria inicial
        Vector3 randomEuler = new Vector3(
            Random.Range(randomRotationMin.x, randomRotationMax.x),
            Random.Range(randomRotationMin.y, randomRotationMax.y),
            Random.Range(randomRotationMin.z, randomRotationMax.z)
        );

        w.transform.rotation = Quaternion.Euler(randomEuler);

        Rigidbody rb = w.GetComponent<Rigidbody>();
        rb.AddForce(transform.forward * dropForce, ForceMode.Impulse);

        rb.AddTorque(
            Random.insideUnitSphere * randomAngularForce,
            ForceMode.Impulse
        );

        // -------------------------
        // re-equip
        // -------------------------

        if (slots.Count == 0)
        {
            currentIndex = -1;

            if (ammoUI != null)
                ammoUI.SetCurrentWeapon(null);

            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, slots.Count - 1);
        Equip(currentIndex);
    }


    // -----------------------------
    void ApplyWeaponOffset(Weapon weapon)
    {
        weapon.transform.localPosition = weapon.GetInventoryPositionOffset();
        weapon.ApplyInventoryRotation(
    weapon.GetInventoryRotationOffset()
);

    }

    // -----------------------------
    void PrepareAsEquipped(Weapon w)
    {
        if (w.TryGetComponent(out Rigidbody rb))
            rb.isKinematic = true;

        foreach (Collider c in w.GetComponentsInChildren<Collider>())
            c.enabled = false;

        // 🔥 ACTIVAR SISTEMAS DEL ARMA
        w.enabled = true;

        if (w.TryGetComponent(out WeaponAudio audio))
            audio.enabled = true;

        if (w.TryGetComponent(out AudioSource source))
            source.enabled = true;

        if (w.TryGetComponent(out WeaponAimController aim))
            aim.enabled = true;

        WeaponSpawner spawner = GetComponentInParent<WeaponSpawner>();
        //if (spawner)
        //    spawner.NotifyWeaponPicked();

        //if (w.TryGetComponent(out WeaponSwayController sway))
        //    sway.enabled = true;
    }


    // -----------------------------
    void PrepareAsDropped(Weapon w)
    {
        if (w.TryGetComponent(out Rigidbody rb))
            rb.isKinematic = false;

        foreach (Collider c in w.GetComponentsInChildren<Collider>())
            c.enabled = true;

        // ❌ DESACTIVAR SISTEMAS DEL ARMA
        w.enabled = false;

        if (w.TryGetComponent(out WeaponAudio audio))
            audio.enabled = false;

        if (w.TryGetComponent(out AudioSource source))
            source.enabled = false;

        if (w.TryGetComponent(out WeaponAimController aim))
            aim.enabled = false;

        //if (w.TryGetComponent(out WeaponSwayController sway))
        //    sway.enabled = false;
    }


    // -----------------------------
    public void EquipNext()
    {
        if (!TryCancelAim()) return;
        if (slots.Count == 0) return;
        Equip((currentIndex + 1) % slots.Count);
    }

    public void EquipPrevious()
    {
        if (!TryCancelAim()) return;
        if (slots.Count == 0) return;
        Equip((currentIndex - 1 + slots.Count) % slots.Count);
    }


    public Weapon CurrentWeapon
    {
        get
        {
            if (currentIndex < 0 || currentIndex >= slots.Count)
                return null;
            return slots[currentIndex];
        }
    }

    bool TryCancelAim()
    {
        Weapon current = CurrentWeapon;
        if (!current) return true;

        var aim = current.GetComponent<WeaponAimController>();
        if (aim && aim.IsAiming)
        {
            aim.ForceStopAim();
            return false; // 🚫 NO CAMBIES DE ARMA AÚN
        }

        return true;
    }

    void OnDestroy()
    {
        if (reloadAction != null)
            reloadAction.performed -= OnReload;

        if (nextSlotAction != null)
            nextSlotAction.performed -= OnNextSlot;
    }



    void OnReload(InputAction.CallbackContext ctx)
    {
        Weapon w = CurrentWeapon;
        if (w == null)
            return;

        w.ReloadFromInventory();
    }

    void SpawnDefaultWeapon()
    {
        if (defaultWeaponPrefab == null)
        {
            Debug.LogError("❌ No default weapon assigned in WeaponInventory");
            return;
        }

        // Instanciar arma base
        Weapon weaponInstance = Instantiate(defaultWeaponPrefab, weaponHolder);

        // Reset local transform
        weaponInstance.transform.localPosition = Vector3.zero;
        weaponInstance.transform.localRotation = Quaternion.identity;

        // Prepararla como equipada
        PrepareAsEquipped(weaponInstance);

        // Asignar inventario de munición
        if (playerAmmoInventory != null)
            weaponInstance.AssignAmmoInventory(playerAmmoInventory);

        // ❗ NO transferimos ammo al inventario global
        // porque esta arma es la base y debe quedarse con su cargador
        // weaponInstance.TransferAmmoToInventory(); ❌

        weaponInstance.gameObject.SetActive(false);

        slots.Add(weaponInstance);
        currentIndex = -1;

        Equip(0);

        // Marcarla como no dropeable
        weaponInstance.isDefaultWeapon = true;
    }

    void SpawnAndAddDefaultWeapon()
    {
        Weapon w = Instantiate(defaultWeaponPrefab);

        // 🔥 Forzar que sea default
        w.isDefaultWeapon = true;

        // 🔥 Forzar que se inicialice llena
        if (w.magazine != null)
        {
            w.magazine.currentBullets = w.magazine.maxBullets;
        }

        // 🔥 Evitar que luego vuelva a randomizar
        var weaponScript = w.GetComponent<Weapon>();
        if (weaponScript != null)
        {
            weaponScript.SendMessage("MarkAmmoInitialized", SendMessageOptions.DontRequireReceiver);
        }

        // 👉 Meterla al inventario como cualquier otra
        AddWeapon(w);

        Debug.Log("🟢 Arma default instanciada y añadida al inventario");
    }

    void ShowDropDefaultWarning()
    {
        if (dropWarningText == null)
            return;

        RectTransform rect = dropWarningText.rectTransform;

        if (!warningInitialized)
        {
            warningOriginalScale = rect.localScale;
            warningOriginalPos = rect.anchoredPosition;
            warningInitialized = true;
        }

        // 🔥 RESET VISUAL DURO
        rect.localScale = warningOriginalScale;
        rect.anchoredPosition = warningOriginalPos;

        Color c = dropWarningText.color;
        c.a = 0f;
        dropWarningText.color = c;

        dropWarningText.enabled = true;

        if (warningRoutine != null)
            StopCoroutine(warningRoutine);

        warningRoutine = StartCoroutine(DropWarningRoutine());
    }




    IEnumerator DropWarningRoutine()
    {
        dropWarningText.text = "NO SE PUEDE DROPEAR EL ARMA DEFAULT";
        dropWarningText.enabled = true;

        RectTransform rect = dropWarningText.rectTransform;

        Vector2 originalPos = warningOriginalPos;
        Vector3 originalScale = warningOriginalScale;


        Color baseColor = dropWarningText.color;
        baseColor.a = 0f;
        dropWarningText.color = baseColor;

        // -------------------------
        // 🔹 FADE IN + SCALE POP
        // -------------------------
        float t = 0f;

        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            float p = t / fadeInTime;

            float alpha = Mathf.Lerp(0f, 1f, p);
            float scale = Mathf.Lerp(1f, popScale, p);

            dropWarningText.color = new Color(
                baseColor.r,
                baseColor.g,
                baseColor.b,
                alpha
            );

            rect.localScale = originalScale * scale;

            yield return null;
        }

        // volver a escala normal suavemente
        t = 0f;
        while (t < popTime)
        {
            t += Time.deltaTime;
            float p = t / popTime;

            float scale = Mathf.Lerp(popScale, 1f, p);
            rect.localScale = originalScale * scale;

            yield return null;
        }

        // -------------------------
        // 🔹 HOLD + SHAKE
        // -------------------------
        float timer = 0f;

        while (timer < warningDuration)
        {
            timer += Time.deltaTime;

            float offsetX = Random.Range(-1f, 1f) * shakeAmount;
            float offsetY = Random.Range(-1f, 1f) * shakeAmount;

            rect.anchoredPosition = originalPos + new Vector2(offsetX, offsetY);

            yield return null;
        }

        // restaurar posición
        rect.anchoredPosition = originalPos;

        // -------------------------
        // 🔹 FADE OUT
        // -------------------------
        t = 0f;

        Color currentColor = dropWarningText.color;

        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            float p = t / fadeOutTime;

            float alpha = Mathf.Lerp(currentColor.a, 0f, p);

            dropWarningText.color = new Color(
                currentColor.r,
                currentColor.g,
                currentColor.b,
                alpha
            );

            yield return null;
        }

        dropWarningText.enabled = false;
        dropWarningText.text = "";
        rect.localScale = originalScale;
        dropWarningText.color = new Color(
            baseColor.r,
            baseColor.g,
            baseColor.b,
            1f
        );

        // 🔒 RESTORE HARD STATE
        rect.localScale = warningOriginalScale;
        rect.anchoredPosition = warningOriginalPos;

        Color finalColor = dropWarningText.color;
        finalColor.a = 1f;
        dropWarningText.color = finalColor;

        dropWarningText.enabled = false;
        dropWarningText.text = "";

    }

    // -----------------------------
    // UI READ-ONLY
    // -----------------------------
    public int CurrentCount => slots.Count;
    public int CurrentIndex => currentIndex;

    public Weapon GetWeaponAtUI(int index)
    {
        if (index < 0 || index >= slots.Count)
            return null;

        return slots[index];
    }

    public void PickupWeapon(Weapon newWeapon)
    {
        if (newWeapon == null)
            return;

        // 🔒 Si el inventario NO está lleno → flujo normal
        if (!IsFull)
        {
            AddWeapon(newWeapon);
            Equip(slots.Count - 1);
            return;
        }

        // 🔥 INVENTARIO LLENO → HACER SWAP
        SwapCurrentWeapon(newWeapon);
    }

    void SwapCurrentWeapon(Weapon newWeapon)
    {
        if (currentIndex < 0 || currentIndex >= slots.Count)
            return;

        Weapon oldWeapon = slots[currentIndex];

        // ❌ NO permitir swap si el arma actual es default
        if (oldWeapon.isDefaultWeapon)
        {
            Debug.Log("🟡 No puedes reemplazar el arma default");
            ShowDropDefaultWarning();
            return;
        }

        // -------------------------
        // SOLTAR ARMA ACTUAL
        // -------------------------
        slots.RemoveAt(currentIndex);

        var aim = oldWeapon.GetComponent<WeaponAimController>();
        if (aim)
            aim.ForceStopAim();

        PrepareAsDropped(oldWeapon);

        oldWeapon.transform.SetParent(null);
        oldWeapon.transform.position = transform.position + transform.forward;

        oldWeapon.transform.rotation = Quaternion.Euler(
            Random.Range(randomRotationMin.x, randomRotationMax.x),
            Random.Range(randomRotationMin.y, randomRotationMax.y),
            Random.Range(randomRotationMin.z, randomRotationMax.z)
        );

        Rigidbody rb = oldWeapon.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.AddForce(transform.forward * dropForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * randomAngularForce, ForceMode.Impulse);
        }

        // -------------------------
        // AÑADIR NUEVA ARMA
        // -------------------------
        PrepareAsEquipped(newWeapon);

        if (playerAmmoInventory != null)
            newWeapon.AssignAmmoInventory(playerAmmoInventory);

        newWeapon.TransferAmmoToInventory();

        newWeapon.transform.SetParent(weaponHolder);
        ApplyWeaponOffset(newWeapon);

        newWeapon.gameObject.SetActive(false);

        // 👉 insertar en el MISMO slot
        slots.Insert(currentIndex, newWeapon);

        // -------------------------
        // EQUIPAR
        // -------------------------
        Equip(currentIndex);

        inventoryUI?.Refresh();

        Debug.Log($"🔁 Swap: {oldWeapon.name} → {newWeapon.name}");
    }


    void OnNextSlot(InputAction.CallbackContext ctx)
    {
        if (Time.time - lastScrollTime < scrollCooldown)
            return;

        Vector2 scroll = ctx.ReadValue<Vector2>();

        if (scroll.y < 0f)
        {
            EquipNext();
            lastScrollTime = Time.time;
        }
        else if (scroll.y > 0f)
        {
            EquipPrevious();
            lastScrollTime = Time.time;
        }

        Debug.Log("SCROLL: " + scroll);
    }

}
