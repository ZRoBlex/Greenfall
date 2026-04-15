// ============================================================
//  GF_WeaponSwayBinder.cs
//  Greenfall — Sistema de Inventario Universal
//  Carpeta: Assets/Greenfall/Inventory/
// ============================================================
//  Versión actualizada de WeaponSwayBinder.cs para el nuevo sistema.
//  ELIMINA el WeaponSwayBinder.cs original y usa este.
//
//  CAMBIOS vs el original:
//  - Referencia a GF_WeaponAdapter en lugar de WeaponInventory
//  - Lógica idéntica, solo cambia la fuente del CurrentWeapon
// ============================================================

using UnityEngine;

public class GF_WeaponSwayBinder : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El mismo GF_WeaponAdapter que pusiste en el jugador.")]
    [SerializeField] GF_WeaponAdapter adapter;

    [SerializeField] SwayController sway;

    Weapon _lastWeapon;

    void Update()
    {
        Weapon current = adapter != null ? adapter.CurrentWeapon : null;

        if (current == _lastWeapon) return;

        // Desactivar sway del arma anterior
        if (_lastWeapon != null && _lastWeapon.TryGetComponent(out SwayController oldSway))
            oldSway.enabled = false;

        _lastWeapon = current;

        if (current == null) return;

        // Obtener o agregar SwayController en el arma nueva
        SwayController newSway = current.GetComponent<SwayController>();
        if (!newSway)
            newSway = current.gameObject.AddComponent<SwayController>();

        sway = newSway;
        sway.enabled = true;
        sway.RefreshBaseRotation();
    }

    public void SetInputs(Vector2 move, Vector2 look)
    {
        if (!sway) return;
        sway.SetMovementInput(move);
        sway.SetLookInput(look);
    }
}