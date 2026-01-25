using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    [System.Serializable]
    public class Resource
    {
        public ResourceSO resourceSO;
        public float currentAmount;
    }

    public List<Resource> resources = new List<Resource>();

    void Start()
    {
        // Inicializar recursos al máximo
        foreach (var res in resources)
        {
            res.currentAmount = res.resourceSO.maxAmount;
        }
    }

    // -------------------
    public bool ConsumeResource(ResourceSO resSO, float amount)
    {
        Resource res = resources.Find(r => r.resourceSO == resSO);
        if (res == null) return false;

        if (res.currentAmount >= amount)
        {
            res.currentAmount -= amount;
            return true;
        }

        res.currentAmount = 0;
        return false;
    }

    public void AddResource(ResourceSO resSO, float amount)
    {
        Resource res = resources.Find(r => r.resourceSO == resSO);
        if (res == null) return;

        res.currentAmount = Mathf.Min(res.currentAmount + amount, res.resourceSO.maxAmount);
    }

    public float GetResource(ResourceSO resSO)
    {
        Resource res = resources.Find(r => r.resourceSO == resSO);
        return res != null ? res.currentAmount : 0;
    }
}
