// ============================================================
// RecoilPatternSO.cs
// ============================================================
// ScriptableObject que define el patrón de retroceso de un arma.
//
// CÓMO CREAR UN PATRÓN:
//   1. Click derecho en Project → Create → Greenfall → Recoil Pattern
//   2. En el Inspector verás un canvas donde DIBUJAR el patrón
//      (requiere el script Editor/RecoilPatternEditor.cs)
//   3. Dibuja con el mouse: el trazo se convierte en puntos
//   4. Asigna el asset al campo recoilPattern de WeaponStats
//
// CÓMO FUNCIONA EL PATRÓN:
//   Cada Vector2 = desplazamiento para ese número de disparo.
//   X: horizontal (+ = derecha, - = izquierda)
//   Y: vertical   (+ = sube,   - = baja)
//   Shot 1 → points[0], Shot 2 → points[1], etc.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecoilPattern", menuName = "Greenfall/Recoil Pattern")]
public class RecoilPatternSO : ScriptableObject
{
    [Tooltip("Cada Vector2 es la fuerza de un disparo: X=horizontal, Y=vertical.\n" +
             "Usa el Editor para dibujar el patrón visualmente.")]
    public List<Vector2> points = new List<Vector2>
    {
        new Vector2( 0.00f, 0.80f),
        new Vector2( 0.05f, 0.85f),
        new Vector2(-0.10f, 0.80f),
        new Vector2( 0.15f, 0.75f),
        new Vector2(-0.20f, 0.70f),
        new Vector2( 0.20f, 0.65f),
        new Vector2(-0.15f, 0.60f),
        new Vector2( 0.10f, 0.55f),
        new Vector2(-0.10f, 0.50f),
        new Vector2( 0.05f, 0.50f),
    };

    [Tooltip("Variación aleatoria aplicada a cada punto. 0 = exacto, 0.15 = natural.")]
    [Range(0f, 0.5f)]
    public float variation = 0.12f;

    [Tooltip("Si está activo, el patrón se repite en loop al disparar más balas que puntos.")]
    public bool loopPattern = true;

    // ── Datos internos del editor (no afectan el juego) ───────

    // Escala del canvas en el editor. Cuántas unidades de juego caben en el canvas.
    // Si canvasScale = 5, el canvas representa el rango [-5, 5] en X e Y.
    [HideInInspector] public float canvasScale = 5f;

    // Color de visualización en el editor
    [HideInInspector] public Color patternColor = Color.red;

    // ── API del juego ─────────────────────────────────────────

    /// <summary>
    /// Devuelve el punto para el disparo N, con variación aplicada.
    /// shotIndex: 0 = primer disparo, 1 = segundo, etc.
    /// </summary>
    public Vector2 GetPoint(int shotIndex, float globalScale, bool isADS, float adsScale)
    {
        if (points == null || points.Count == 0) return Vector2.zero;

        int count = points.Count;
        int idx   = loopPattern
            ? shotIndex % count
            : Mathf.Min(shotIndex, count - 1);

        Vector2 pt = points[idx];

        // Variación aleatoria
        pt.x += Random.Range(-variation, variation);
        pt.y += Random.Range(-variation * 0.4f, variation * 0.4f);

        float scale = globalScale * (isADS ? adsScale : 1f);
        return pt * scale;
    }
}
