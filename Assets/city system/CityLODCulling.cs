// ============================================================
//  CityLODCulling.cs — v2  OPTIMIZADO
//  Parte del BSP City Generator
// ============================================================
//
//  QUÉ HACE:
//  Oculta los renderers de un edificio cuando está demasiado lejos
//  de la cámara principal. Mejora el rendimiento en ciudades grandes.
//
//  MEJORA v2:
//  Bug de rendimiento anterior: Camera.main se llamaba en Update() cada
//  8 frames. Camera.main realiza un FindObjectOfType<Camera> internamente,
//  que es una búsqueda por toda la escena — costosa si hay muchos edificios.
//
//  Fix: Cacheamos Camera.main en Start() y la actualizamos solo si es null
//  (por si la cámara se destruye y se recrea, como en transiciones de escena).
// ============================================================

using UnityEngine;

// public class CityLODCulling : MonoBehaviour
// {
//     // ── Configuración ─────────────────────────────────────────────────────

//     [Tooltip("Distancia en metros a la que el edificio se oculta. " +
//              "Se asigna desde CityGenerator vía CullingDistance property.")]
//     public float CullingDistance { get; set; } = 500f;

//     // ── Estado interno ────────────────────────────────────────────────────

//     // Todos los renderers de este edificio y sus hijos
//     private Renderer[] _renderers;

//     // Distancia al cuadrado (evita sqrt en comparación, mucho más barato)
//     private float _cullingDistanceSq;

//     // MEJORA: Cacheamos la cámara principal aquí en vez de buscarla cada frame
//     private Camera _mainCamera;

//     // Offset de frame para que no todos los edificios chequen en el mismo frame
//     // Distribuye la carga entre frames
//     private int _frameOffset;

//     // ─────────────────────────────────────────────────────────────────────
//     private void Awake()
//     {
//         // Recolectamos todos los renderers de este edificio (incluyendo hijos)
//         _renderers = GetComponentsInChildren<Renderer>(true);

//         // Guardamos un offset aleatorio para distribuir las comprobaciones
//         // entre diferentes frames (sin esto, 1000 edificios todos evalúan
//         // en el frame 0, 8, 16... creando picos de CPU)
//         _frameOffset = Random.Range(0, 8);
//     }

//     // ─────────────────────────────────────────────────────────────────────
//     private void Start()
//     {
//         // MEJORA: Calculamos la distancia al cuadrado UNA VEZ aquí
//         // en vez de recalcularla cada 8 frames
//         _cullingDistanceSq = CullingDistance * CullingDistance;

//         // MEJORA: Cacheamos la cámara en Start() (no en Awake, porque
//         // la cámara podría no estar inicializada aún en Awake)
//         CacheCamera();
//     }

//     // ─────────────────────────────────────────────────────────────────────
//     private void Update()
//     {
//         // Solo ejecutamos la lógica cada 8 frames para este edificio
//         // (el offset garantiza que no todos checan en el mismo frame)
//         if ((Time.frameCount + _frameOffset) % 8 != 0) return;

//         // Validamos que tenemos renderers que gestionar
//         if (_renderers == null || _renderers.Length == 0) return;

//         // MEJORA: Si la cámara fue destruida/recargada, intentamos recuperarla
//         // Esto cubre transiciones de escena sin crashear
//         if (_mainCamera == null)
//         {
//             CacheCamera();
//             if (_mainCamera == null) return; // Sin cámara, no hacemos nada
//         }

//         // Comparación de distancia al cuadrado (evita la costosa raíz cuadrada)
//         float distanceSq = (transform.position - _mainCamera.transform.position).sqrMagnitude;
//         bool  visible    = distanceSq <= _cullingDistanceSq;

//         // Solo cambiamos el estado si realmente cambió (evita llamadas redundantes)
//         foreach (var r in _renderers)
//         {
//             if (r != null && r.enabled != visible)
//                 r.enabled = visible;
//         }
//     }

//     // ─────────────────────────────────────────────────────────────────────
//     //  HELPERS
//     // ─────────────────────────────────────────────────────────────────────

//     private void CacheCamera()
//     {
//         _mainCamera = Camera.main;
//     }

//     // ─────────────────────────────────────────────────────────────────────
//     //  LIMPIEZA (necesario si el edificio se destruye/regenera)
//     // ─────────────────────────────────────────────────────────────────────

//     private void OnDestroy()
//     {
//         _renderers  = null;
//         _mainCamera = null;
//     }
// }