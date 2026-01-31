using UnityEngine;

[CreateAssetMenu(menuName = "World/Lighting Preset")]
public class LightingPreset : ScriptableObject
{
    public Gradient sunColor;
    public Gradient ambientColor;

    public AnimationCurve sunIntensity;
}
