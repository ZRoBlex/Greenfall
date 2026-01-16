using UnityEngine;

public class BuildSelector : MonoBehaviour
{
    public StructureData[] structures;
    int index;

    public StructureData Current => structures[index];

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            index = (index + 1) % structures.Length;

        if (Input.GetKeyDown(KeyCode.Q))
            index = (index - 1 + structures.Length) % structures.Length;
    }
}
