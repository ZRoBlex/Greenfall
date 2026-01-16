using UnityEngine;

public static class DamageIndicatorSpawner
{
    public static GameObject prefab;

    public static void Spawn(int dmg, Vector3 pos, Vector3 normal)
    {
        if (!prefab) return;

        GameObject obj = Object.Instantiate(
            prefab,
            pos,
            Quaternion.LookRotation(normal)
        );

        obj.GetComponent<DamageIndicator>().Setup(dmg);
    }
}
