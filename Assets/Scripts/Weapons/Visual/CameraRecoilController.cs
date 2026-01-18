using UnityEngine;

public class CameraRecoilController : MonoBehaviour
{
    [Header("Recoil")]
    public float kickSpeed = 25f;
    public float returnSpeed = 18f;

    [Header("FOV Scaling")]
    public bool scaleByFOV = true;
    public float referenceFOV = 60f;

    Vector2 targetRecoil;
    Vector2 currentRecoil;

    Camera cam;

    void Awake()
    {
        cam = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        targetRecoil = Vector2.Lerp(
            targetRecoil,
            Vector2.zero,
            returnSpeed * Time.deltaTime
        );

        currentRecoil = Vector2.Lerp(
            currentRecoil,
            targetRecoil,
            kickSpeed * Time.deltaTime
        );

        transform.localRotation = Quaternion.Euler(
            -currentRecoil.y,
            currentRecoil.x,
            0f
        );
    }

    public void AddRecoil(float vertical, float horizontal)
    {
        float fovMul = 1f;

        if (scaleByFOV && cam)
            fovMul = cam.fieldOfView / referenceFOV;

        targetRecoil.y += vertical * fovMul;
        targetRecoil.x += Random.Range(-horizontal, horizontal) * fovMul;
    }
}
