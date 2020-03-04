using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    public int width = 6;
    public int height = 6;

    public HexCell cellPrefab;

    HexCell[] Cells;

    public Text cellLabelPrefab;

    Canvas gridCanvas;

    HexMesh hexMesh;

    public Color defaultColor = Color.white;
    public Color touchedColor = Color.magenta;

    private void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();
        hexMesh = GetComponentInChildren<HexMesh>();

        Cells = new HexCell[height * width];

        for (int z = 0, i = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    private void Start()
    {
        hexMesh.Triangulate(Cells);
    }

    /// <summary>
    /// Creates Grid of Cells which offsets them by 10 each.
    /// </summary>
    /// <param name="x">X cordinate multiple so that it increases per hex made.</param>
    /// <param name="z">Z cordinate multiple so that it increases per hex made.</param>
    /// <param name="i">Index for Cell location in array</param>
    void CreateCell(int x, int z, int i)
    {
        Vector3 Position;
        Position.x = (x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f) ;
        Position.y = 0f;
        Position.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = Cells[i] = Instantiate<HexCell>(cellPrefab);
        cell.transform.SetParent(transform, false);
        cell.transform.localPosition = Position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.color = defaultColor;

        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.SetParent(gridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(Position.x, Position.z);
        label.text = cell.coordinates.ToStringOnSeparateLines();

    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            HandleInput();
        }
    }

    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(inputRay,out hit))
        {
            TouchCell(hit.point);
        }

    }

    void TouchCell(Vector3 Position)
    {
        Position = transform.InverseTransformPoint(Position) ;
        HexCoordinates coordinates = HexCoordinates.FromPosition(Position);
        int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
        HexCell cell = Cells[index];
        cell.color = touchedColor;
        hexMesh.Triangulate(Cells);

        Debug.Log("Touched at " + coordinates.ToString());
    }

}
