using System.Collections.Generic;
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



    readonly List<Weapon> slots = new();
    int currentIndex = -1;

    public bool IsFull => slots.Count >= maxSlots;

    // -----------------------------
    public void AddWeapon(Weapon weapon)
    {
        if (IsFull || weapon == null) return;

        PrepareAsEquipped(weapon);

        weapon.transform.SetParent(weaponHolder);
        ApplyWeaponOffset(weapon);

        weapon.gameObject.SetActive(false);
        slots.Add(weapon);

        if (currentIndex == -1)
            Equip(0);
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
    }

    // -----------------------------
    public void DropCurrent()
    {
        if (slots.Count == 0) return;
        if (currentIndex < 0 || currentIndex >= slots.Count) return;

        Weapon w = slots[currentIndex];
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


        if (slots.Count == 0)
        {
            currentIndex = -1;
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

        if(w.TryGetComponent(out WeaponAimController aim))
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


}
