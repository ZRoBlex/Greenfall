using UnityEngine;
using System.Collections.Generic;

public class WeaponMuzzleFlashPool : MonoBehaviour
{
    public static WeaponMuzzleFlashPool Instance;

    public MuzzleFlash prefab;
    public int poolSize = 10;

    Queue<MuzzleFlash> pool = new Queue<MuzzleFlash>();

    void Awake()
    {
        Instance = this;

        for (int i = 0; i < poolSize; i++)
        {
            MuzzleFlash flash = Instantiate(prefab, transform);
            flash.gameObject.SetActive(false);
            pool.Enqueue(flash);
        }
    }

    public void PlayFlash(Vector3 pos, Quaternion rot)
    {
        if (pool.Count == 0)
            return; // 🔒 NO crecer, NO crashear

        MuzzleFlash flash = pool.Dequeue();
        flash.Play(pos, rot);
        pool.Enqueue(flash);
    }
}
