using UnityEngine;
using System.Collections.Generic;

public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance;

    [Header("Prefab")]
    public EnemyController enemyPrefab;

    [Header("Pool Settings")]
    public int initialSize = 10;

    readonly Queue<EnemyController> pool = new();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        for (int i = 0; i < initialSize; i++)
            CreateNew();
    }

    EnemyController CreateNew()
    {
        EnemyController e = Instantiate(enemyPrefab, transform);
        e.gameObject.SetActive(false);
        pool.Enqueue(e);
        return e;
    }

    public EnemyController Get()
    {
        if (pool.Count == 0)
            CreateNew();

        EnemyController e = pool.Dequeue();
        ResetEnemy(e);
        return e;
    }

    public void Return(EnemyController e)
    {
        if (e == null)
            return;

        e.gameObject.SetActive(false);
        pool.Enqueue(e);
    }

    void ResetEnemy(EnemyController e)
    {
        // 🔹 FSM
        if (e.FSM != null)
            e.FSM.ChangeState(new WanderState());

        // 🔹 Salud letal
        var h = e.GetComponent<Health>();
        if (h != null)
            h.ResetHealth();

        // 🔹 Salud no letal (stun)
        var nl = e.GetComponent<NonLethalHealth>();
        if (nl != null)
            nl.ResetHealth();

        // 🔹 LOD
        e.SetLOD(EnemyLOD.Active);

        // 🔹 Motor
        if (e.Motor != null)
            e.Motor.rotateTowardsMovement = true;

        // 🔹 Perception
        if (e.Perception != null)
            e.Perception.SetExternalTarget(null);
    }

    public void Release(EnemyController e)
    {
        ResetEnemy(e);
        e.gameObject.SetActive(false);
        pool.Enqueue(e);
    }


}
