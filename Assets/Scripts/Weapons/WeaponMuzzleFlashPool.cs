using UnityEngine;
using System.Collections.Generic;

public class WeaponMuzzleFlashPool : MonoBehaviour
{
    [SerializeField] MuzzleFlashInstance flashPrefab;
    [SerializeField] int poolSize = 10;

    readonly List<MuzzleFlashInstance> pool = new();
    int index;

    void Awake()
    {
        for (int i = 0; i < poolSize; i++)
        {
            MuzzleFlashInstance flash =
                Instantiate(flashPrefab, transform);

            flash.gameObject.SetActive(false);
            pool.Add(flash);
        }
    }

    public void PlayFlash(Vector3 position, Vector3 fireDirection)
    {
        MuzzleFlashInstance flash = pool[index];
        index = (index + 1) % pool.Count;

        flash.Play(position, fireDirection);
    }
}
