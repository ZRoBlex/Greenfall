// ============================================================
//  PickupVFXSystem.cs
//  Greenfall: The Last Harvest
//  Carpeta sugerida: Assets/Greenfall/Inventory/
// ============================================================
//
//  QUÉ HACE:
//  Genera efectos de partículas completamente desde código al recoger items.
//  NO necesitas ningún prefab de partículas — todo se crea en runtime.
//  Solo necesitas configurar el PickupVFXConfig en cada InventoryItemData.
//
//  CÓMO FUNCIONA:
//  1. Tiene un pool de ParticleSystems pre-creados en Awake()
//  2. Cuando llamas Play(config, pos), toma uno del pool,
//     lo configura según el config y lo reproduce
//  3. Cuando las partículas terminan, el PS vuelve al pool
//
//  FORMAS DISPONIBLES (PickupVFXShape):
//  Burst    → Explosión radial (para items raros/armas)
//  Rise     → Partículas suben (para semillas/plantas)
//  Spiral   → Espiral ascendente (para items mágicos/épicos)
//  Scatter  → Completamente aleatorio (para materiales/chatarra)
//  Implode  → Partículas se absorben hacia el centro (para consumibles)
//  Sparkle  → Destellos estáticos (para items legendarios)
//
//  POOL:
//  Se mantiene un pool de ParticleSystems para evitar Instantiate/Destroy.
//  El tamaño del pool es configurable pero 10 es suficiente para gameplay normal.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupVFXSystem : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    //  SINGLETON
    // ─────────────────────────────────────────────────────────────────────

    public static PickupVFXSystem Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────
    //  CONFIGURACIÓN DEL POOL
    // ─────────────────────────────────────────────────────────────────────

    [Header("Pool de Partículas")]
    [Tooltip("Número de ParticleSystems pre-creados en el pool. " +
             "10 es suficiente para gameplay normal. " +
             "Aumentar si el jugador recoge muchos items al mismo tiempo.")]
    [Range(4, 20)] [SerializeField] private int _poolSize = 10;

    // ─────────────────────────────────────────────────────────────────────
    //  ESTADO INTERNO
    // ─────────────────────────────────────────────────────────────────────

    // Queue del pool — los PS disponibles para usar
    private Queue<ParticleSystem> _availablePS;

    // PS actualmente en uso (para saber cuándo devolver al pool)
    private List<(ParticleSystem ps, float returnTime)> _activePS;

    // ─────────────────────────────────────────────────────────────────────
    //  AWAKE
    // ─────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Crear el pool de ParticleSystems
        _availablePS = new Queue<ParticleSystem>(_poolSize);
        _activePS    = new List<(ParticleSystem, float)>(_poolSize);

        for (int i = 0; i < _poolSize; i++)
        {
            var ps = CreateBaseParticleSystem($"VFX_Pool_{i}");
            _availablePS.Enqueue(ps);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  UPDATE — devolver PS al pool cuando terminan
    // ─────────────────────────────────────────────────────────────────────

    private void Update()
    {
        // Recorremos la lista de activos y devolvemos los que ya terminaron
        for (int i = _activePS.Count - 1; i >= 0; i--)
        {
            var (ps, returnTime) = _activePS[i];
            if (Time.time >= returnTime)
            {
                ReturnToPool(ps);
                _activePS.RemoveAt(i);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API PÚBLICA
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reproduce un efecto de partículas en la posición dada.
    /// Toda la configuración visual viene del PickupVFXConfig.
    ///
    /// Se llama desde InventoryItemController.OnPickupSuccess().
    /// </summary>
    public void Play(PickupVFXConfig config, Vector3 worldPosition)
    {
        if (config == null) return;

        // Obtener un PS del pool (o crear uno temporal si el pool está agotado)
        ParticleSystem ps = GetFromPool();
        if (ps == null)
        {
            // Pool agotado — crear uno temporal y destruirlo después
            ps = CreateBaseParticleSystem("VFX_Temp");
        }

        // Posicionar
        ps.transform.position = worldPosition;
        ps.gameObject.SetActive(true);

        // Configurar todos los módulos según el config
        ConfigureParticleSystem(ps, config);

        // Reproducir
        ps.Play(true);

        // Registrar para devolución al pool
        _activePS.Add((ps, Time.time + config.duration + 0.5f));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  CONFIGURAR PS SEGÚN EL CONFIG
    // ─────────────────────────────────────────────────────────────────────

    private void ConfigureParticleSystem(ParticleSystem ps, PickupVFXConfig config)
    {
        // ── Módulo MAIN ───────────────────────────────────────────────────
        // El módulo main controla el comportamiento base de cada partícula
        var main = ps.main;
        main.loop           = false;    // Un solo burst, no loop
        main.playOnAwake    = false;
        main.duration       = config.duration;
        main.startLifetime  = new ParticleSystem.MinMaxCurve(
            config.duration * 0.4f,    // Vida mínima
            config.duration * 0.9f);   // Vida máxima (variación para naturalidad)
        main.startSize      = new ParticleSystem.MinMaxCurve(
            config.particleSize * (1f - config.sizeVariation),
            config.particleSize * (1f + config.sizeVariation));
        main.startColor     = config.primaryColor;
        main.gravityModifier = config.useGravity ? 0.5f : 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // ── Módulo EMISSION ───────────────────────────────────────────────
        // Controla cuántas partículas se emiten y cómo
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f; // No emitir continuamente

        // // Emitir todas en un burst (instante inicial)
        // emission.SetBursts(new[] {
        //     new ParticleSystem.Burst(
        //         time: 0f,
        //         count: config.particleCount,
        //         cycleCount: 1,
        //         repeatInterval: 0.01f)
        // });

        
        // Emitir todas en un burst (instante inicial)
        emission.SetBursts(new[] {
            new ParticleSystem.Burst(
                0f,
                config.particleCount,
                1,
                0.01f)
        });

        // ── Módulo SHAPE (forma de emisión) ──────────────────────────────
        // Define DESDE DÓNDE salen las partículas
        var shape = ps.shape;
        shape.enabled = true;

        switch (config.shape)
        {
            case PickupVFXShape.Burst:
            case PickupVFXShape.Scatter:
                // Esfera: partículas salen desde el centro hacia afuera
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius    = 0.1f; // Punto de emisión pequeño
                break;

            case PickupVFXShape.Rise:
            case PickupVFXShape.Spiral:
                // Cono mirando hacia arriba: partículas suben
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle     = 20f;
                shape.radius    = 0.2f;
                break;

            case PickupVFXShape.Implode:
                // Cáscara esférica: partículas empiezan en el borde y van al centro
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius    = config.radius;
                break;

            case PickupVFXShape.Sparkle:
                // Punto: partículas nacen en el mismo lugar
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius    = config.radius * 0.5f;
                break;
        }

        // ── Módulo VELOCITY ────────────────────────────────────────────
        // Controla hacia dónde y a qué velocidad se mueven
        var velocityOverTime = ps.velocityOverLifetime;
        velocityOverTime.enabled = false; // Por defecto off, lo activamos si necesitamos

        // ── Módulo START SPEED ─────────────────────────────────────────
        switch (config.shape)
        {
            case PickupVFXShape.Burst:
                main.startSpeed = new ParticleSystem.MinMaxCurve(
                    config.radius * 1.5f,
                    config.radius * 3f);
                break;

            case PickupVFXShape.Rise:
                main.startSpeed = new ParticleSystem.MinMaxCurve(
                    config.radius * 0.5f,
                    config.radius * 1.5f);
                break;

            case PickupVFXShape.Spiral:
                main.startSpeed = new ParticleSystem.MinMaxCurve(
                    config.radius * 0.8f,
                    config.radius * 1.8f);
                // Rotación en Y para el efecto espiral
                var rotationOverTime = ps.rotationOverLifetime;
                rotationOverTime.enabled = true;
                rotationOverTime.z = new ParticleSystem.MinMaxCurve(
                    -360f * Mathf.Deg2Rad,
                    360f  * Mathf.Deg2Rad);
                break;

            case PickupVFXShape.Scatter:
                main.startSpeed = new ParticleSystem.MinMaxCurve(
                    config.radius * 1f,
                    config.radius * 4f);
                break;

            case PickupVFXShape.Implode:
                // Velocidad negativa para que vayan hacia el centro
                main.startSpeed = new ParticleSystem.MinMaxCurve(
                    -config.radius * 1.5f,
                    -config.radius * 0.5f);
                break;

            case PickupVFXShape.Sparkle:
                main.startSpeed = new ParticleSystem.MinMaxCurve(0f, config.radius * 0.3f);
                break;
        }

        // ── Módulo COLOR OVER LIFETIME ────────────────────────────────
        // Interpola del color primario al secundario durante la vida
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        var gradient = new Gradient();
        var colorKeys = new[]
        {
            new GradientColorKey(config.primaryColor,   0f),
            new GradientColorKey(config.secondaryColor, 1f)
        };
        var alphaKeys = new[]
        {
            new GradientAlphaKey(config.primaryColor.a,   0f),
            new GradientAlphaKey(config.secondaryColor.a, 0.8f),
            new GradientAlphaKey(0f, 1f) // Fade out al final
        };
        gradient.SetKeys(colorKeys, alphaKeys);
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        // ── Módulo SIZE OVER LIFETIME ──────────────────────────────────
        // Las partículas se encogen al final de su vida
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        var sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);    // Tamaño completo al nacer
        sizeCurve.AddKey(0.7f, 1f);  // Se mantiene hasta el 70% de su vida
        sizeCurve.AddKey(1f, 0f);    // Desaparece al final
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // ── Módulo ROTATION OVER LIFETIME ─────────────────────────────
        if (config.rotateParticles && config.shape != PickupVFXShape.Spiral)
        {
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(
                -config.rotationSpeed * Mathf.Deg2Rad,
                 config.rotationSpeed * Mathf.Deg2Rad);
        }

        // ── Módulo RENDERER ────────────────────────────────────────────
        // Configuramos el renderer para usar un material de partículas built-in
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            // Usar el material de partículas default si no tenemos uno
            if (renderer.material == null || renderer.material.shader == null)
            {
                var mat = new Material(Shader.Find("Particles/Standard Unlit"));
                if (mat.shader != null) renderer.material = mat;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  POOL MANAGEMENT
    // ─────────────────────────────────────────────────────────────────────

    private ParticleSystem GetFromPool()
    {
        if (_availablePS.Count == 0) return null;
        return _availablePS.Dequeue();
    }

    private void ReturnToPool(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.gameObject.SetActive(false);
        ps.transform.SetParent(transform);
        _availablePS.Enqueue(ps);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  CREAR PS BASE (sin configuración, listo para recibir config)
    // ─────────────────────────────────────────────────────────────────────

    private ParticleSystem CreateBaseParticleSystem(string goName)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform);
        go.SetActive(false);

        var ps   = go.AddComponent<ParticleSystem>();
        var main = ps.main;

        // Configuración base que no cambia entre efectos
        main.loop        = false;
        main.playOnAwake = false;
        main.stopAction  = ParticleSystemStopAction.None;

        return ps;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  CLEANUP
    // ─────────────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        // Limpiar todos los PS del pool
        while (_availablePS != null && _availablePS.Count > 0)
        {
            var ps = _availablePS.Dequeue();
            if (ps != null) Destroy(ps.gameObject);
        }
    }
}