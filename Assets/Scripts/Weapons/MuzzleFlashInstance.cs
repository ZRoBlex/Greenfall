using UnityEngine;
using System.Collections;

public class MuzzleFlashInstance : MonoBehaviour
{
    [Header("Scale")]
    public float startScale = 0.05f;
    public float maxScale = 1.4f;
    public float growTime = 0.025f;
    public float shrinkTime = 0.045f;

    Transform cam;
    Coroutine routine;

    void Awake()
    {
        cam = Camera.main.transform;
        gameObject.SetActive(false);
    }

    public void Play(Vector3 position, Vector3 fireDirection)
    {
        transform.position = position;

        // 👉 MIRAR A LA CÁMARA
        Vector3 lookDir = transform.position - cam.position;
        transform.rotation = Quaternion.LookRotation(lookDir);

        RotateParticlesBackward(fireDirection);

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        transform.localScale = Vector3.one * startScale;
        gameObject.SetActive(true);

        float t = 0f;

        // 🔼 CRECER
        while (t < growTime)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.one *
                Mathf.Lerp(startScale, maxScale, t / growTime);
            yield return null;
        }

        // 🔽 ENCOGER
        t = 0f;
        while (t < shrinkTime)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.one *
                Mathf.Lerp(maxScale, startScale, t / shrinkTime);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    void RotateParticlesBackward(Vector3 fireDir)
    {
        ParticleSystem[] psList =
            GetComponentsInChildren<ParticleSystem>(true);

        foreach (var ps in psList)
        {
            ps.transform.rotation =
                Quaternion.LookRotation(-fireDir);

            ps.Play(true);
        }
    }
}
