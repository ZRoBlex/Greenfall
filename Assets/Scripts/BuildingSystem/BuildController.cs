using UnityEngine;
using System.Collections.Generic;

public class BuildController : MonoBehaviour
{
    public Camera playerCamera;
    public float buildDistance = 5f;

    public BuildSelector selector;
    public BuildPreview preview;

    public bool buildModeActive = false;

    private StructureRotation currentRotation = StructureRotation.Deg0;

    // Grid lógico (luego irá a chunks)
    private static Dictionary<Vector3Int, GameObject> occupiedCells
        = new Dictionary<Vector3Int, GameObject>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            buildModeActive = !buildModeActive;

            if (!buildModeActive)
                preview.Hide();
        }

        if (!buildModeActive)
            return;

        HandleRotationInput();

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (!Physics.Raycast(ray, out RaycastHit hit, buildDistance))
        {
            preview.Hide();
            return;
        }

        Vector3Int originCell = GridMath.WorldToCell(hit.point);

        StructureConfig structure = selector.Current;

        bool canBuild = CanBuild(originCell, structure, currentRotation);

        Vector3 worldPos = GridMath.CellToWorld(originCell);
        Quaternion rotation = GetWorldRotation();

        preview.Show(structure, worldPos, rotation, canBuild);

        if (canBuild && Input.GetMouseButtonDown(0))
        {
            Place(originCell, structure, rotation);
        }
    }


    void HandleRotationInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentRotation = (StructureRotation)(((int)currentRotation + 1) % 4);
        }
    }

    bool CanBuild(
        Vector3Int origin,
        StructureConfig config,
        StructureRotation rotation
    )
    {
        foreach (var localCell in config.occupiedCells)
        {
            Vector3Int rotatedCell =
                GridRotation.RotateCell(localCell, config.gridBounds, rotation);

            Vector3Int cell = origin + rotatedCell;

            if (occupiedCells.ContainsKey(cell))
                return false;
        }

        return true;
    }

    void Place(
        Vector3Int origin,
        StructureConfig config,
        Quaternion rotation
    )
    {
        Vector3 worldPos = GridMath.CellToWorld(origin);

        GameObject obj = Instantiate(
            config.finalPrefab,
            worldPos,
            rotation
        );

        foreach (var localCell in config.occupiedCells)
        {
            Vector3Int rotatedCell =
                GridRotation.RotateCell(localCell, config.gridBounds, currentRotation);

            occupiedCells.Add(origin + rotatedCell, obj);
        }
    }

    Quaternion GetWorldRotation()
    {
        return Quaternion.Euler(0, (int)currentRotation * 90f, 0);
    }
}
