using System.Collections.Generic;
using UnityEngine;

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
        w.gameObject.SetActive(true);
    }

    // -----------------------------
    public void DropCurrent()
    {
        if (slots.Count == 0) return;
        if (currentIndex < 0 || currentIndex >= slots.Count) return;

        Weapon w = slots[currentIndex];
        slots.RemoveAt(currentIndex);

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

        w.enabled = true;
    }

    // -----------------------------
    void PrepareAsDropped(Weapon w)
    {
        if (w.TryGetComponent(out Rigidbody rb))
            rb.isKinematic = false;

        foreach (Collider c in w.GetComponentsInChildren<Collider>())
            c.enabled = true;

        w.enabled = false;
    }

    // -----------------------------
    public void EquipNext()
    {
        if (slots.Count == 0) return;
        Equip((currentIndex + 1) % slots.Count);
    }

    public void EquipPrevious()
    {
        if (slots.Count == 0) return;
        Equip((currentIndex - 1 + slots.Count) % slots.Count);
    }
}
