using UnityEngine;

public class BuildPreviewCollision : MonoBehaviour
{
    public int blockingCount = 0;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("BuildBlocker"))
            blockingCount++;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("BuildBlocker"))
            blockingCount--;
    }

    public bool IsBlocked()
    {
        return blockingCount > 0;
    }
}
