using UnityEngine;

[System.Serializable]
public class EnemyInstanceStats : MonoBehaviour
{
    [Header("Movement Overrides")]
    public float? moveSpeed = null;
    public float? runSpeed = null;
    public float? turnSpeed = null;

    [Header("Wander Overrides")]
    public float? wanderRadius = null;
    public float? wanderWaitTime = null;

    [Header("Passive Overrides")]
    public float? passiveSafeDistance = null;
    public float? passiveRetreatSpeed = null;

    [Header("Combat Overrides")]
    public float? attackRange = null;
    public float? attackDamage = null;

    [Header("Team Override")]
    public int? overrideTeam = null;   // ← ← ← AGREGADO AQUÍ

    // ---------------------------------------
    // Methods to apply overrides
    // ---------------------------------------
    public float GetMoveSpeed(float baseValue) => moveSpeed ?? baseValue;
    public float GetRunSpeed(float baseValue) => runSpeed ?? baseValue;
    public float GetTurnSpeed(float baseValue) => turnSpeed ?? baseValue;

    public float GetWanderRadius(float baseValue) => wanderRadius ?? baseValue;
    public float GetWanderWait(float baseValue) => wanderWaitTime ?? baseValue;

    public float GetPassiveSafeDistance(float baseValue) => passiveSafeDistance ?? baseValue;
    public float GetPassiveRetreatSpeed(float baseValue) => passiveRetreatSpeed ?? baseValue;

    public float GetAttackRange(float baseValue) => attackRange ?? baseValue;
    public float GetAttackDamage(float baseValue) => attackDamage ?? baseValue;
}
