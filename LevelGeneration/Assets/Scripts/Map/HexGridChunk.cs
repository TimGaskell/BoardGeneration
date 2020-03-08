using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGridChunk : MonoBehaviour
{
    HexCell[] cells;

    HexMesh hexMesh;

    Canvas gridCanvas;



    private void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();
        hexMesh = GetComponentInChildren<HexMesh>();

        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
        ShowUI(false);
    }



    private void LateUpdate()
    {
        hexMesh.Triangulate(cells);
        enabled = false;
    }


    /// <summary>
    /// Assigns a Cell into this chunk. Sets it transform and UI parent to this chuck game object
    /// </summary>
    /// <param name="index"></param>
    /// <param name="cell"></param>
    public void AddCell(int index, HexCell cell)
    {
        cells[index] = cell;
        cell.chunk = this;
        cell.transform.SetParent(transform, false);
        cell.uiRect.SetParent(gridCanvas.transform, false);
    }

    /// <summary>
    /// Used to enable the LateUpdate() method
    /// </summary>
    public void Refresh()
    {
        enabled = true;
    }

    /// <summary>
    /// Toggles the labels on the Hexes
    /// </summary>
    /// <param name="visible"> bool for switching on or off the labels </param>
    public void ShowUI(bool visible)
    {
        gridCanvas.gameObject.SetActive(visible);
    }


}
