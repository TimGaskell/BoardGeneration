using UnityEngine;

public class HexCell : MonoBehaviour {

	public HexCoordinates coordinates;

	public Color color;

	[SerializeField]
	HexCell[] neighbors;

	/// <summary>
	/// Gets the neighbor of current Hex in the direction specified 
	/// </summary>
	/// <param name="direction"> Direction of the hexagon vertex e.g. NE, SE etc </param>
	/// <returns> HexCell saved in its neighbors array </returns>
	public HexCell GetNeighbor (HexDirection direction) {
		return neighbors[(int)direction];
	}

	/// <summary>
	/// Sets the neighbor in specified direction in neighbors array for the Hex.
	/// Also sets itself as a neighbor in the opposite direction for that same Hex.
	/// </summary>
	/// <param name="direction"> Direction of the hexagon vertex e.g. NE, SE etc </param>
	/// <param name="cell"></param>
	public void SetNeighbor (HexDirection direction, HexCell cell) {
		neighbors[(int)direction] = cell;
		cell.neighbors[(int)direction.Opposite()] = this;
	}
}