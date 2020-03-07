using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour {

	public Color[] colors;

	public HexGrid hexGrid;

	private Color activeColor;
	int activeElevation;

	void Awake () {
		SelectColor(0);
	}

	void Update () {
		if (
			Input.GetMouseButton(0) &&
			!EventSystem.current.IsPointerOverGameObject()
		) {
			HandleInput();
		}
	}

	/// <summary>
	/// Handles Input of Mouse button down. If it clicks on a hexagon, it will change its color to the one currently selected
	/// </summary>
	void HandleInput () {
		Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(inputRay, out hit)) {
			EditCell(hexGrid.GetCell(hit.point));
		}
	}

	/// <summary>
	/// Edits the cells current color and elevation. Causes the mesh renderer to redraw the mesh. 
	/// </summary>
	/// <param name="cell"> Hex that is going to be changed </param>
	void EditCell(HexCell cell)
	{
		cell.color = activeColor;
		cell.Elevation = activeElevation;
		hexGrid.Refresh();
	}

	/// <summary>
	/// Used by the GUI to set the elevation that will be used.
	/// </summary>
	/// <param name="elevation"> Height for which the cell will become </param>
	public void SetElevation(float elevation)
	{
		activeElevation = (int)elevation;
	}

	/// <summary>
	/// Changes active color to that of another in its array
	/// </summary>
	/// <param name="index"> Index of color array </param>
	public void SelectColor (int index) {
		activeColor = colors[index];
	}
}