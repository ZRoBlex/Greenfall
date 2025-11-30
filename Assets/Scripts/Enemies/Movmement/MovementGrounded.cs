using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class MovementGrounded : MonoBehaviour
{
    [Header("Movement")]
    public CharacterController controller;
    public float gravity = -9.81f;
    public float stepOffset = 0.4f;

    [Header("Rotation")]
    public float turnSlerp = 8f;

    // estado
    Vector3 velocity = Vector3.zero;               // <- necesario para gravedad y cálculo vertical
    bool stunned = false;

    // info pública para debug / animaciones
    public float CurrentSpeed { get; private set; } = 0f;
    public Vector3 LastMoveDirection { get; private set; } = Vector3.zero;

    void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        controller.stepOffset = stepOffset;
    }

    // Mover hacia una posición (rota hacia la dirección)
    public void MoveTowards(Vector3 targetPosition, float speed)
    {
        if (stunned) return;

        Vector3 dir = targetPosition - transform.position;
        dir.y = 0f;

        float dist = dir.magnitude;
        if (dist <= 0.01f)
        {
            CurrentSpeed = 0f;
            LastMoveDirection = Vector3.zero;
            return;
        }

        Vector3 moveDir = dir.normalized;
        // rotación suave hacia moveDir
        Quaternion targetRot = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSlerp * Time.deltaTime);

        // aplicar movimiento
        Vector3 move = moveDir * speed * Time.deltaTime;
        LastMoveDirection = moveDir;
        CurrentSpeed = move.magnitude / Time.deltaTime;

        // aplicar gravedad vertical usando 'velocity'
        controller.Move(move + Vector3.up * velocity.y * Time.deltaTime);
        velocity.y += gravity * Time.deltaTime;
    }

    // Mover en una dirección relativa (rota hacia la dirección — uso general)
    public void MoveDirection(Vector3 direction, float speed)
    {
        if (stunned) return;

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            CurrentSpeed = 0f;
            LastMoveDirection = Vector3.zero;
            return;
        }

        Vector3 dirN = direction.normalized;

        // ROTAR hacia la dirección de movimiento
        Quaternion targetRot = Quaternion.LookRotation(dirN);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSlerp * Time.deltaTime);

        Vector3 move = dirN * speed * Time.deltaTime;
        LastMoveDirection = dirN;
        CurrentSpeed = move.magnitude / Time.deltaTime;

        controller.Move(move + Vector3.up * velocity.y * Time.deltaTime);
        velocity.y += gravity * Time.deltaTime;
    }

    // Mover en una dirección PERO SIN ROTAR el transform (útil para retroceder mirando al objetivo)
    public void MoveDirection_NoRotate(Vector3 direction, float speed)
    {
        if (stunned) return;

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            CurrentSpeed = 0f;
            LastMoveDirection = Vector3.zero;
            return;
        }

        Vector3 dirN = direction.normalized;

        Vector3 move = dirN * speed * Time.deltaTime;
        LastMoveDirection = dirN;
        CurrentSpeed = move.magnitude / Time.deltaTime;

        controller.Move(move + Vector3.up * velocity.y * Time.deltaTime);
        velocity.y += gravity * Time.deltaTime;

        // NO hacemos ninguna rotación aquí: el transform mantiene la orientación actual
    }

    // Rotar suavemente hacia un punto (no mueve)
    public void RotateTowards(Vector3 point)
    {
        Vector3 dir = point - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSlerp * Time.deltaTime);
    }

    public void ApplyStun(float seconds)
    {
        if (!stunned) StartCoroutine(StunCoroutine(seconds));
    }

    IEnumerator StunCoroutine(float s)
    {
        stunned = true;
        yield return new WaitForSeconds(s);
        stunned = false;
    }

    public void StopInstantly()
    {
        controller.Move(Vector3.zero); // ← fuerza velocidad 0 real
        CurrentSpeed = 0f;
        LastMoveDirection = Vector3.zero;
    }

}
