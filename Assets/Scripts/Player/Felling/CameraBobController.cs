using UnityEngine;

public class CameraBobController : MonoBehaviour
{
    [Header("General")]
    [SerializeField] bool enableBob = true;

    [Header("Idle")]
    [SerializeField] float idleAmplitude = 0.01f;
    [SerializeField] float idleSpeed = 0.5f;

    [Header("Walk")]
    [SerializeField] float walkAmplitude = 0.05f;
    [SerializeField] float walkSpeed = 1.5f;

    [Header("Sprint")]
    [SerializeField] float sprintMultiplier = 1.6f;

    [Header("Smoothing")]
    [SerializeField] float smoothSpeed = 8f;

    Vector2 movementInput;
    bool isSprinting;

    Vector3 defaultLocalPos;
    float noiseTime;

    void Awake()
    {
        defaultLocalPos = transform.localPosition;
        noiseTime = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (!enableBob)
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                defaultLocalPos,
                Time.deltaTime * smoothSpeed
            );
            return;
        }

        ApplyBob();
    }

    void ApplyBob()
    {
        float intensity;
        float speed;

        if (movementInput.sqrMagnitude < 0.01f)
        {
            intensity = idleAmplitude;
            speed = idleSpeed;
        }
        else
        {
            intensity = walkAmplitude;
            speed = walkSpeed;

            if (isSprinting)
                intensity *= sprintMultiplier;
        }

        noiseTime += Time.deltaTime * speed;

        float x = (Mathf.PerlinNoise(noiseTime, 0f) - 0.5f) * intensity;
        float y = (Mathf.PerlinNoise(0f, noiseTime) - 0.5f) * intensity * 1.5f;

        Vector3 target = defaultLocalPos + new Vector3(x, y, 0f);

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            target,
            Time.deltaTime * smoothSpeed
        );
    }

    // 🔌 API LIMPIA
    public void SetMovementInput(Vector2 input)
    {
        movementInput = input;
    }

    public void SetSprint(bool sprint)
    {
        isSprinting = sprint;
    }
}
