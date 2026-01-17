using UnityEngine;
using System.Collections;

public class BulletDecal : MonoBehaviour
{
    MeshRenderer rend;
    MaterialPropertyBlock block;

    float lifeTime;
    float fadeTime;

    Coroutine fadeRoutine;

    void Awake()
    {
        rend = GetComponent<MeshRenderer>();
        block = new MaterialPropertyBlock();
    }

    public void Init(float life, float fade)
    {
        lifeTime = life;
        fadeTime = fade;

        SetAlpha(1f);

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(LifeRoutine());
    }

    IEnumerator LifeRoutine()
    {
        yield return new WaitForSeconds(lifeTime);
        yield return FadeOut();
        BulletDecalPool.Instance.Release(this);
    }

    IEnumerator FadeOut()
    {
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(1f, 0f, t / fadeTime));
            yield return null;
        }
    }

    void SetAlpha(float a)
    {
        rend.GetPropertyBlock(block);
        block.SetFloat("_Alpha", a);
        rend.SetPropertyBlock(block);
    }

    public void ResetDecal()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        SetAlpha(1f);
    }
}
