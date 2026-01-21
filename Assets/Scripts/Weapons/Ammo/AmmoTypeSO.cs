using UnityEngine;

public enum AmmoCategory
{
    Standard,
    Shotgun,
    Submachine,
    Sniper,
    Heavy,
    Arrow
}

// Estos son los ScriptableObjects que creas desde Unity (Assets → Create → Greenfall → Weapons → Ammo Type)

[CreateAssetMenu(menuName = "Greenfall/Weapons/Ammo Type")]
public class AmmoTypeSO : ScriptableObject
{
    public string ammoName = "Default Ammo";
    public AmmoCategory category = AmmoCategory.Standard;
    public int defaultMaxStack = 100;
    public Sprite icon; // 👈 opcional
}
