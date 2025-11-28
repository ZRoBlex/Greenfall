using UnityEngine;

[CreateAssetMenu(menuName = "Greenfall/Enemy/Profession")]
public class Profession : ScriptableObject
{
    public string professionName = "Worker";
    public float productivityMultiplier = 1f;
    // puedes añadir habilidades, behavior trees, anim clips, etc.
}
