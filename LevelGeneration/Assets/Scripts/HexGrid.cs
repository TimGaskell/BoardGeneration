﻿using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour {

	public int width = 6;
	public int height = 6;

	public Color defaultColor = Color.white;

	public HexCell cellPrefab;
	public Text cellLabelPrefab;

	HexCell[] cells;

	Canvas gridCanvas;
	HexMesh hexMesh;
	public Texture2D noiseSource;

	void Awake () {
		gridCanvas = GetComponentInChildren<Canvas>();
		hexMesh = GetComponentInChildren<HexMesh>();
		HexMetrics.noiseSource = noiseSource;

		cells = new HexCell[height * width];

		for (int z = 0, i = 0; z < height; z++) {
			for (int x = 0; x < width; x++) {
				CreateCell(x, z, i++);
			}
		}
	}

	void Start () {
		hexMesh.Triangulate(cells);
	}

	private void OnEnable()
	{
		HexMetrics.noiseSource = noiseSource;
	}

	/// <summary>
	/// Redraws the triangles needed for the mesh.
	/// </summary>
	public void Refresh()
	{
		hexMesh.Triangulate(cells);
	}

	/// <summary>
	/// Redraws the Hex with the respective color now added onto it
	/// </summary>
	/// <param name="position"> Position in world space </param>
	/// <param name="color"> Color value for hex </param>
	public HexCell GetCell (Vector3 position) {
		position = transform.InverseTransformPoint(position);
		HexCoordinates coordinates = HexCoordinates.FromPosition(position);
		int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
		return cells[index];
	}

	/// <summary>
	/// Instantiates the Hex prefabs, assigning them their positions in relation to each other.
	/// Assigns the neighbors of each hex in relation to each side (E.g. NE,SW, W, E, SE, NW).
	/// Also responsible for attaching a Text label on top of each Hex displaying its location.
	/// </summary>
	/// <param name="x"> X Coordinate of Hex</param>
	/// <param name="z"> Z Coordinate of Hex</param>
	/// <param name="i"> Index for cells </param>
	void CreateCell (int x, int z, int i) {
		Vector3 position;
		position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
		position.y = 0f;
		position.z = z * (HexMetrics.outerRadius * 1.5f);

		HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
		cell.transform.SetParent(transform, false);
		cell.transform.localPosition = position;
		cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
		cell.color = defaultColor;

		if (x > 0) {
			cell.SetNeighbor(HexDirection.W, cells[i - 1]);
		}
		if (z > 0) {
			if ((z & 1) == 0) {
				cell.SetNeighbor(HexDirection.SE, cells[i - width]);
				if (x > 0) {
					cell.SetNeighbor(HexDirection.SW, cells[i - width - 1]);
				}
			}
			else {
				cell.SetNeighbor(HexDirection.SW, cells[i - width]);
				if (x < width - 1) {
					cell.SetNeighbor(HexDirection.SE, cells[i - width + 1]);
				}
			}
		}

		Text label = Instantiate<Text>(cellLabelPrefab);
		label.rectTransform.SetParent(gridCanvas.transform, false);
		label.rectTransform.anchoredPosition =
			new Vector2(position.x, position.z);
		label.text = cell.coordinates.ToStringOnSeparateLines();
		cell.uiRect = label.rectTransform;

		cell.Elevation = 0;
	}
}