using UnityEngine;

[CreateAssetMenu(fileName = "NewResource", menuName = "Greenfall/Resource")]
public class ResourceSO : ScriptableObject
{
    public string resourceName;
    public Sprite icon;
    public float maxAmount = 100f;
}
