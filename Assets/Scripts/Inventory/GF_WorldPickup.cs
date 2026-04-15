// ============================================================
//  GF_WorldPickup.cs
//  Greenfall — Sistema de Inventario Universal
//  Carpeta: Assets/Greenfall/Inventory/
// ============================================================
//  Reemplaza: WeaponPickup.cs, AmmoBox.cs, AmmoPickup.cs, SeedPickup.cs
//
//  Agrega este componente a CUALQUIER objeto recogible del mundo.
//  GF_Interactor lo detecta con raycast al mirar el objeto.
//
//  SETUP DE PREFAB:
//  1. Crea o selecciona el prefab del objeto
//  2. Agrega este componente
//  3. Asigna el GF_ItemDefinition correspondiente
//  4. (Opcional) Ajusta amount y overrides
//
//  PARA ARMAS: el prefab debe tener TAMBIÉN el componente Weapon.cs
// ============================================================

using UnityEngine;

public class GF_WorldPickup : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    //  CONFIG
    // ─────────────────────────────────────────────────────────────────────

    [Header("Item")]
    [Tooltip("La definición del item. Crea desde: " +
             "Click derecho → Create → Greenfall → Item Definition")]
    public GF_ItemDefinition definition;

    [Tooltip("Cuántas unidades hay en este objeto del mundo. " +
             "0 = usar el valor por defecto (1 para no-stackeables).")]
    public int amount = 1;

    [Header("Interacción")]
    [Tooltip("Texto personalizado. Vacío = generado automáticamente.")]
    public string customInteractionText = "";

    [Tooltip("Si true, el objeto se resalta al ser mirado (requiere OutlineObject).")]
    public bool highlightOnFocus = true;

    [Header("Auto-pickup")]
    [Tooltip("Si true, se recoge automáticamente al tocar al jugador (trigger).")]
    public bool autoPickup = false;

    // ─────────────────────────────────────────────────────────────────────
    //  ESTADO INTERNO
    // ─────────────────────────────────────────────────────────────────────

    // Flag para evitar doble pickup
    private bool _pickedUp;

    // Cache del Weapon component (solo para armas)
    private Weapon _cachedWeapon;

    // Posición al momento del pickup — CAPTURADA ANTES de cualquier reparenting
    // para que el VFX aparezca en el lugar correcto
    private Vector3 _worldPositionAtPickup;

    // ─────────────────────────────────────────────────────────────────────
    //  AWAKE
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (definition != null && definition.type == GF_ItemType.Weapon)
            _cachedWeapon = GetComponent<Weapon>() ?? GetComponentInChildren<Weapon>();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API DE INTERACCIÓN (GF_Interactor la llama)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Texto que aparece en la UI al mirar este objeto.</summary>
    public string GetInteractionText()
    {
        if (!string.IsNullOrEmpty(customInteractionText))
            return customInteractionText;

        if (definition == null) return "Interactuar";

        string verb = definition.type switch
        {
            GF_ItemType.Weapon     => "Recoger",
            GF_ItemType.Ammo       => "Recoger",
            GF_ItemType.Consumable => "Usar",
            GF_ItemType.QuestItem  => "Examinar",
            _                      => "Recoger"
        };

        string qty = definition.isStackable && amount > 1 ? $" x{amount}" : "";
        return $"{verb} {definition.displayName}{qty}";
    }

    /// <summary>
    /// Intenta que el jugador recoja este item.
    /// Llamado por GF_Interactor al presionar E.
    /// </summary>
    public bool TryPickup()
    {
        if (_pickedUp) return false;
        if (definition == null) return false;
        if (GF_Inventory.Instance == null) return false;

        // CAPTURAR POSICIÓN AQUÍ — antes de cualquier SetParent o SetActive
        _worldPositionAtPickup = transform.position;

        bool success;

        if (definition.type == GF_ItemType.Weapon && _cachedWeapon != null)
        {
            // Para armas, delegar al adapter para que maneje la instancia
            var adapter = FindFirstAdapter();
            if (adapter != null)
                success = adapter.PickupWeapon(definition, _cachedWeapon);
            else
                success = GF_Inventory.Instance.TryAdd(definition, 1, _cachedWeapon);
        }
        else
        {
            int actualAmount = amount > 0 ? amount : 1;
            success = GF_Inventory.Instance.TryAdd(definition, actualAmount);
        }

        if (success) OnPickupSuccess();
        return success;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  EFECTOS DE PICKUP
    // ─────────────────────────────────────────────────────────────────────

    private void OnPickupSuccess()
    {
        _pickedUp = true;

        // VFX en la posición capturada ANTES del reparenting
        SpawnVFX(_worldPositionAtPickup);

        // Sonido
        if (definition.pickupSound != null)
        {
            var go  = new GameObject("GF_PickupSound");
            go.transform.position = _worldPositionAtPickup;
            var src = go.AddComponent<AudioSource>();
            src.clip = definition.pickupSound;
            src.spatialBlend = 0.5f;
            src.volume = definition.pickupVolume;
            src.Play();
            Destroy(go, definition.pickupSound.length + 0.2f);
        }

        // Si NO es un arma (las armas las maneja GF_WeaponAdapter)
        if (definition.type != GF_ItemType.Weapon)
            gameObject.SetActive(false);
        // Si es arma, GF_WeaponAdapter llama PrepareWeaponForHolster
        // que desactiva el GO y reparenta
    }

    // ─────────────────────────────────────────────────────────────────────
    //  VFX PROCEDURAL
    //  Se genera completamente desde código — sin prefabs
    // ─────────────────────────────────────────────────────────────────────

    private void SpawnVFX(Vector3 position)
    {
        if (definition?.pickupVFX == null) return;

        var vfx = definition.pickupVFX;
        var go  = new GameObject("GF_PickupVFX");
        go.transform.position = position;

        var ps   = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop           = false;
        main.playOnAwake    = false;
        main.duration       = vfx.duration;
        main.startLifetime  = new ParticleSystem.MinMaxCurve(vfx.duration * 0.4f, vfx.duration * 0.85f);
        main.startSize      = new ParticleSystem.MinMaxCurve(vfx.size * 0.7f, vfx.size * 1.3f);
        main.startColor     = vfx.primaryColor;
        main.gravityModifier = vfx.useGravity ? 0.5f : 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        float speed = vfx.shape switch
        {
            GF_VFXShape.Rise    => vfx.radius * 1.2f,
            GF_VFXShape.Burst   => vfx.radius * 2.5f,
            GF_VFXShape.Scatter => vfx.radius * 3f,
            _                   => vfx.radius * 1.5f
        };
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);

        var emission = ps.emission;
        emission.enabled     = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, vfx.count) });

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = vfx.shape == GF_VFXShape.Rise
            ? ParticleSystemShapeType.Cone
            : ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;
        if (shape.shapeType == ParticleSystemShapeType.Cone) shape.angle = 20f;

        // Color fade
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(vfx.primaryColor, 0f), new GradientColorKey(vfx.secondaryColor, 1f) },
            new[] { new GradientAlphaKey(vfx.primaryColor.a, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // Size fade out
        var sizeLt = ps.sizeOverLifetime;
        sizeLt.enabled = true;
        var curve = new AnimationCurve();
        curve.AddKey(0f, 1f); curve.AddKey(0.7f, 1f); curve.AddKey(1f, 0f);
        sizeLt.size = new ParticleSystem.MinMaxCurve(1f, curve);

        ps.Play(true);
        Destroy(go, vfx.duration + 0.5f);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HIGHLIGHT
    // ─────────────────────────────────────────────────────────────────────

    public void SetHighlight(bool on)
    {
        if (!highlightOnFocus) return;

        // Compatible con OutlineObject si existe
        foreach (var comp in GetComponentsInChildren<MonoBehaviour>(true))
        {
            string typeName = comp.GetType().Name;
            if (typeName == "OutlineObject" || typeName == "HoverOutline")
                comp.enabled = on;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  RESET (para cuando el item vuelve al mundo tras ser dropeado)
    // ─────────────────────────────────────────────────────────────────────

    public void ResetPickup()
    {
        _pickedUp = false;
    }

    public void SetAmount(int newAmount)
    {
        amount    = newAmount;
        _pickedUp = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  AUTO-PICKUP
    // ─────────────────────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!autoPickup || !other.CompareTag("Player")) return;
        TryPickup();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  UTILIDADES
    // ─────────────────────────────────────────────────────────────────────

    private static GF_WeaponAdapter FindFirstAdapter()
    {
        return Object.FindFirstObjectByType<GF_WeaponAdapter>();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (definition == null) return;
        Gizmos.color = definition.GetRarityColor();
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.5f,
            $"[{definition.type}] {definition.displayName}");
    }
#endif
}