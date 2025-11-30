using UnityEngine;

public interface IWeapon
{
    void Fire(Transform firePoint, Transform aimTarget);
}