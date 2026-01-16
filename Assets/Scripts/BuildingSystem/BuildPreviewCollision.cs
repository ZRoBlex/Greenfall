using UnityEngine;

public class BuildPreviewCollision : MonoBehaviour
{
    int hits = 0;
    BuildPreviewCollision collision;

    void Awake()
    {
        collision = GetComponent<BuildPreviewCollision>();

        foreach (var c in GetComponentsInChildren<Collider>())
            c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.transform.root == transform.root)
            return;

        if (other.isTrigger)
            return;

        hits++;
    }


    void OnTriggerExit(Collider other)
    {
        if (!other.isTrigger)
            hits--;
    }

    public bool IsBlocked() => hits > 0;
}
