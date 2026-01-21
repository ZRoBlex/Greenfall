using UnityEngine;

public class WeaponMagazine : MonoBehaviour
{
    public int currentBullets;
    public int maxBullets = 30;

    public bool IsFull => currentBullets >= maxBullets;
    public bool IsEmpty => currentBullets <= 0;

    public void Initialize(int max)
    {
        maxBullets = max;
        currentBullets = maxBullets;
    }

    public bool ConsumeBullet()
    {
        if (currentBullets <= 0)
            return false;

        currentBullets--;
        return true;
    }

    public int AddBullets(int amount)
    {
        int spaceLeft = maxBullets - currentBullets;
        int toAdd = Mathf.Min(spaceLeft, amount);

        currentBullets += toAdd;
        return toAdd;
    }
}
