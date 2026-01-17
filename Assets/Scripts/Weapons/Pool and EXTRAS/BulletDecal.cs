using UnityEngine;
using System.Collections;

public class BulletDecal : MonoBehaviour
{
    SpriteRenderer sr;
    Coroutine lifeRoutine;
    Vector3 originalScale; // ✅
    public BulletDecal PrefabReference { get; private set; }



    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale; // ✅ guardar escala del prefab
        PrefabReference = this; // ⚠ se corrige al instanciar
        gameObject.SetActive(false);
    }

    public void Activate(RaycastHit hit, float lifeTime, float fadeTime)
    {
        if (lifeRoutine != null)
            StopCoroutine(lifeRoutine);

        // 🔒 SIEMPRE mantener el padre en el pool
        transform.SetParent(BulletDecalPool.Instance.transform, true);

        // 📍 Posición fija en el mundo
        transform.position = hit.point + hit.normal * 0.001f;

        // 🎯 Orientación correcta (pegada a la superficie)
        transform.rotation = Quaternion.LookRotation(-hit.normal);

        // 🧱 Escala fija (evita herencia)
        //transform.localScale = Vector3.one;
        transform.localScale = originalScale; // ✅ CLAVE

        SetAlpha(1f);
        gameObject.SetActive(true);

        lifeRoutine = StartCoroutine(Life(lifeTime, fadeTime));
    }


    IEnumerator Life(float life, float fade)
    {
        yield return new WaitForSeconds(life);

        float t = 0f;
        while (t < fade)
        {
            t += Time.deltaTime;
            SetAlpha(1f - t / fade);
            yield return null;
        }

        lifeRoutine = null;
        BulletDecalPool.Instance.Release(this);
    }

    void SetAlpha(float a)
    {
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }
}
