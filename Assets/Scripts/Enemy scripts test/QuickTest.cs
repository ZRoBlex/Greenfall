using UnityEngine;

public class QuickTest : MonoBehaviour
{
    public GameObject enemyPrefab;
    public int spawnCount = 10;

    void Start()
    {
        // Spawn 10 enemigos de prueba
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 pos = Random.insideUnitCircle * 10f;
            pos = new Vector3(pos.x, 0, pos.y);

            Instantiate(enemyPrefab, pos, Quaternion.identity);
        }
    }
}
