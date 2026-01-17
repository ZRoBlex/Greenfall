using UnityEngine;

public class MuzzleFlash : MonoBehaviour
{
    public float startScale = 0.05f;
    public float maxScale = 1.3f;
    public float duration = 0.06f;

    float timer;
    bool active;
    Transform cam;

    void Awake()
    {
        cam = Camera.main ? Camera.main.transform : null;
        gameObject.SetActive(false);
    }

    public void Play(Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        transform.rotation = rot;
        transform.localScale = Vector3.one * startScale;

        timer = 0f;
        active = true;
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (!active) return;

        if (cam)
        {
            transform.rotation =
                Quaternion.LookRotation(transform.position - cam.position);
        }

        timer += Time.deltaTime;
        float t = timer / duration;

        if (t < 0.5f)
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, maxScale, t * 2f);
        else
            transform.localScale = Vector3.one * Mathf.Lerp(maxScale, startScale, (t - 0.5f) * 2f);

        if (timer >= duration)
        {
            active = false;
            gameObject.SetActive(false);
        }
    }
}
