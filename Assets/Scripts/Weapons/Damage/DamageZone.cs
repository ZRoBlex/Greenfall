using UnityEngine;

public class DamageZone : MonoBehaviour
{
    public enum ZoneType
    {
        Head,
        Torso,
        Arm,
        Leg
    }

    public ZoneType zone;
    public float damageMultiplier = 1f;
}
