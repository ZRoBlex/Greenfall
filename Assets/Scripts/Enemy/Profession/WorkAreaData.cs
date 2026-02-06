using UnityEngine;
using System.Collections.Generic;

public enum WorkAreaType
{
    Table,
    Zone,
    Building
}

[CreateAssetMenu(menuName = "Work/Work Area", fileName = "NewWorkArea")]
public class WorkAreaData : ScriptableObject
{
    [Header("Identidad")]
    public string displayName;
    public WorkAreaType areaType;

    [Header("Profesiones Permitidas")]
    public List<ProfessionType> allowedProfessions;

    [Header("Uso")]
    public bool usableByPlayer = true;
    public bool usableByNPCs = true;

    [Header("Tiempo")]
    public float workDuration = 5f;

    [Header("Animaciones")]
    public string npcWorkAnim = "Work";
    public string playerWorkAnim = "Use";

    [Header("Opcional - Producción futura")]
    public bool producesSomething = false;
}
