using UnityEngine;

public enum ResourceType { Food, Energy, Material, Moral }

[CreateAssetMenu(fileName = "NewUIResource", menuName = "Greenfall/UIResource")]
public class UIResourceSO : ScriptableObject
{
    public ResourceType resourceType;
    public string resourceName;
    public Sprite icon;
    public float maxAmount = 100f;
}
