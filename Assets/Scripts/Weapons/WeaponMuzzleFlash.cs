using UnityEngine;
using System.Collections;

public class WeaponMuzzleFlash : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject flashPrefab;
    [SerializeField] Transform firePoint;

    [Header("Scale")]
    [SerializeField] float startScale = 0.05f;
    [SerializeField] float maxScale = 1.4f;
    [SerializeField] float growTime = 0.03f;
    [SerializeField] float shrinkTime = 0.05f;

    GameObject flash;
    Coroutine routine;

    void Awake()
    {
        flash = Instantiate(flashPrefab, firePoint);
        flash.transform.localPosition = Vector3.zero;
        flash.transform.localRotation = Quaternion.identity;
        flash.transform.localScale = Vector3.one * startScale;
        flash.SetActive(false);
    }

    public void Play()
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        // 👉 POSICIÓN Y ROTACIÓN CORRECTAS
        flash.transform.position = firePoint.position;
        flash.transform.rotation = firePoint.rotation;

        // 👉 APUNTAR FLASH HACIA DELANTE
        flash.transform.forward = firePoint.forward;

        // 👉 PARTÍCULAS HACIA ATRÁS
        RotateParticlesBackward();

        flash.transform.localScale = Vector3.one * startScale;
        flash.SetActive(true);

        // 🔼 CRECER
        float t = 0f;
        while (t < growTime)
        {
            t += Time.deltaTime;
            float k = t / growTime;
            flash.transform.localScale =
                Vector3.one * Mathf.Lerp(startScale, maxScale, k);
            yield return null;
        }

        // 🔽 ENCOGER
        t = 0f;
        while (t < shrinkTime)
        {
            t += Time.deltaTime;
            float k = t / shrinkTime;
            flash.transform.localScale =
                Vector3.one * Mathf.Lerp(maxScale, startScale, k);
            yield return null;
        }

        flash.SetActive(false);
    }

    void RotateParticlesBackward()
    {
        ParticleSystem[] particles =
            flash.GetComponentsInChildren<ParticleSystem>();

        foreach (var ps in particles)
        {
            var main = ps.main;
            main.startRotation3D = true;

            // 🔄 ROTAR 180° PARA IR HACIA ATRÁS
            ps.transform.rotation =
                Quaternion.LookRotation(-firePoint.forward);

            ps.Play(true);
        }
    }
}
