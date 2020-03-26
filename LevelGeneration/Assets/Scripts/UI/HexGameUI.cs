using UnityEngine;
using UnityEngine.EventSystems;

public class HexGameUI : MonoBehaviour {

	public HexGrid grid;

	HexCell currentCell;
    HexUnit selectedUnit;

	public void SetEditMode(bool toggle) {
		enabled = !toggle;
		grid.ShowUI(!toggle);
		grid.ClearPath();
	}

	void Update() {
		if (!EventSystem.current.IsPointerOverGameObject()) {
			if (Input.GetMouseButtonDown(0)) {
				DoSelection();
			}
			else if (selectedUnit) {
				if (Input.GetMouseButtonDown(1)) {
					DoMove();
				}
				else {
					DoPathfinding();
				}
			}
		}
	}

	/// <summary>
	/// Section method for selecting a unit on the map. Removes any previous paths from another unit
	/// </summary>
	void DoSelection() {
		grid.ClearPath();
		UpdateCurrentCell();
		if (currentCell) {
			selectedUnit = currentCell.Unit;
		}
	}

	/// <summary>
	/// Calls the path finding method using the selected units position as its start and the hovered hex as its end.
	/// Only executes if the destination is valid e.g.( no water, no unit in space hovered).
	/// </summary>
	void DoPathfinding() {
		if (UpdateCurrentCell()) {
			if (currentCell && selectedUnit.IsValidDestination(currentCell)) {
				grid.FindPath(selectedUnit.Location, currentCell, 24);
			}
			else {
				grid.ClearPath();
			}
		}
	}

	/// <summary>
	/// If there is an active path for a unit. Move unit to that hovered cell.
	/// </summary>
	void DoMove() {
		if (grid.HasPath) {
			selectedUnit.Travel(grid.GetPath());
			grid.ClearPath();
		}
	}

	/// <summary>
	/// Gets the current cell that the mouse is hovering over. If that cell isn't already being used, it updates it.
	/// </summary>
	/// <returns> True if the mouse is hovering a new cell </returns>
	bool UpdateCurrentCell() {
		HexCell cell =grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
		if (cell != currentCell) {
			currentCell = cell;
			return true;
		}
		return false;
	}
}