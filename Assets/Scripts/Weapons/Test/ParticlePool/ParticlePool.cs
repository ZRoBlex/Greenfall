using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;

public class ParticlePool : MonoBehaviour
{
    public static ParticlePool Instance;

    public GameObject prefabBala;
    public int cantidadInicial = 10;

    public Transform firePoint;

    private List<GameObject> bulletPool = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Crear las balas al inicio
        for (int i = 0; i < cantidadInicial; i++)
        {
            GameObject bala = Instantiate(prefabBala);
            bala.SetActive(false);
            bulletPool.Add(bala);
        }
    }

    public GameObject ObtenerBala()
    {
        // Buscar bala inactiva
        foreach (GameObject bala in bulletPool)
        {
            if (!bala.activeInHierarchy)
            {
                bala.SetActive(true);
                //bala.GetComponent<ParticleSystem>().Play();
                return bala;
            }
        }

        // Si no hay balas disponibles, creamos otra (opcional)
        GameObject nuevaBala = Instantiate(prefabBala, firePoint.position, firePoint.rotation);
        bulletPool.Add(nuevaBala);
        return nuevaBala;

    }
}
