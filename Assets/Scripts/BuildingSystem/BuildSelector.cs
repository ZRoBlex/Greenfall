using UnityEngine;

public class BuildSelector : MonoBehaviour
{
    public StructureConfig[] availableStructures;
    public int currentIndex;

    public StructureConfig Current =>
        availableStructures[currentIndex];

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            Next();
        if (Input.GetKeyDown(KeyCode.Q))
            Previous();
    }

    void Next()
    {
        currentIndex = (currentIndex + 1) % availableStructures.Length;
    }

    void Previous()
    {
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = availableStructures.Length - 1;
    }
}
