using UnityEngine;

public class HexCell : MonoBehaviour {

	public HexCoordinates coordinates;

	public Color color;

	int elevation;

	public RectTransform uiRect;

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

	public int Elevation {
		get {
			return elevation;
		}
		set {
			elevation = value;
			Vector3 Position = transform.localPosition;
			Position.y = value * HexMetrics.elevationStep;
			transform.localPosition = Position;

			Vector3 uiPosition = uiRect.localPosition;
			uiPosition.z = elevation * -HexMetrics.elevationStep;
			uiRect.localPosition = uiPosition;
		}
	}

	public HexEdgeType GetEdgeType(HexDirection direction)
	{
		return HexMetrics.GetEdgeType(elevation, neighbors[(int)direction].elevation);
	}
}