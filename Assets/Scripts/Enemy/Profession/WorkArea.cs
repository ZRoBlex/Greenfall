using UnityEngine;

public class WorkArea : MonoBehaviour
{
    [Header("Configuración")]
    public WorkAreaData data;

    [Header("Estado")]
    public Transform workPoint;   // dónde se para el NPC/jugador
    public bool isOccupied;
    public GameObject currentUser;

    public bool CanBeUsedBy(Profession profession)
    {
        if (profession == null || data == null) return false;

        return data.allowedProfessions.Contains(profession.professionType);
    }

    public bool CanPlayerUse()
    {
        return data != null && data.usableByPlayer && !isOccupied;
    }

    public bool CanNPCUse(Profession profession)
    {
        if (isOccupied || !data.usableByNPCs) return false;
        return CanBeUsedBy(profession);
    }

    public void AssignUser(GameObject user)
    {
        currentUser = user;
        isOccupied = true;
    }

    public void Release()
    {
        currentUser = null;
        isOccupied = false;
    }
}
