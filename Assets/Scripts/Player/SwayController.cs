using UnityEngine;

public class SwayController : MonoBehaviour
{
    [Header("Enable States")]
    public bool enableIdleSway = true;
    public bool enableMoveSway = true;
    public bool enableLookSway = true;

    [Header("Idle Sway")]
    [SerializeField] Vector2 idleAmplitude = new Vector2(0.2f, 0.2f);
    [SerializeField] float idleSpeed = 1.5f;

    [Header("Move Sway")]
    [SerializeField] Vector2 moveAmplitude = new Vector2(0.6f, 0.6f);
    [SerializeField] float moveSpeed = 6f;

    [Header("Look Sway")]
    [SerializeField] float lookAmount = 1.5f;
    [SerializeField] float lookSmooth = 8f;

    Quaternion baseRotation;
    Vector2 movementInput;
    Vector2 lookInput;
    bool isMoving;

    float idleTimer;
    Quaternion currentSway;

    void Awake()
    {
        baseRotation = transform.localRotation;
    }

    void Update()
    {
        Quaternion sway = Quaternion.identity;

        if (enableIdleSway && !isMoving)
            sway *= GetIdleSway();

        if (enableMoveSway && isMoving)
            sway *= GetMoveSway();

        if (enableLookSway)
            sway *= GetLookSway();

        currentSway = Quaternion.Lerp(
            currentSway,
            sway,
            Time.deltaTime * lookSmooth
        );

        transform.localRotation = baseRotation * currentSway;
    }

    // ----------------------------
    Quaternion GetIdleSway()
    {
        idleTimer += Time.deltaTime * idleSpeed;

        float x = Mathf.Sin(idleTimer) * idleAmplitude.x;
        float y = Mathf.Cos(idleTimer * 0.8f) * idleAmplitude.y;

        return Quaternion.Euler(x, y, 0f);
    }

    Quaternion GetMoveSway()
    {
        float x = -movementInput.y * moveAmplitude.x;
        float y = movementInput.x * moveAmplitude.y;

        return Quaternion.Euler(x, y, 0f);
    }

    Quaternion GetLookSway()
    {
        Vector3 lookEuler = new Vector3(
            -lookInput.y * lookAmount,
             lookInput.x * lookAmount,
            0f
        );

        return Quaternion.Euler(lookEuler);
    }

    // ----------------------------
    // INTERFACE (lo que llamas desde el player)
    // ----------------------------
    public void SetMovementInput(Vector2 input)
    {
        movementInput = input;
        isMoving = input.sqrMagnitude > 0.01f;
    }

    public void SetLookInput(Vector2 input)
    {
        lookInput = input;
    }
}
