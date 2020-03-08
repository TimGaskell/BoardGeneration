using UnityEngine;

public class HexCell : MonoBehaviour {

	public HexCoordinates coordinates;

	Color Color;



	int elevation = int.MinValue;

	public RectTransform uiRect;

	[SerializeField]
	HexCell[] neighbors;

	public HexGridChunk chunk;

	void Refresh()
	{
		if (chunk)
		{
			chunk.Refresh();
			for(int i = 0; i < neighbors.Length; i++)
			{
				HexCell neighbor = neighbors[i];
				if(neighbor != null && neighbor.chunk != chunk)
				{
					neighbor.chunk.Refresh();
				}
			}
		}
	}

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

	/// <summary>
	/// Used to either get the current elevation of the cell or if it is set, change its y position in the world space.
	/// Also changes the UI text label.
	/// </summary>
	public int Elevation {
		get {
			return elevation;
		}
		set {
			if (elevation == value)
				return;

			elevation = value;
			Vector3 Position = transform.localPosition;
			Position.y = value * HexMetrics.elevationStep;
			Position.y += (HexMetrics.SampleNoise(Position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
			transform.localPosition = Position;

			Vector3 uiPosition = uiRect.localPosition;
			uiPosition.z = -Position.y;
			uiRect.localPosition = uiPosition;

			Refresh();
		}
	}

	public Color color {
		get {
			return Color;
		}
		set {
			if (Color == value)
				return;
			Color = value;
			Refresh();
		}
	}

	/// <summary>
	/// Returns the edge type of a hex based on its direction to this current hex
	/// </summary>
	/// <param name="direction"> direction of cell relative to current cell </param>
	/// <returns> Edge type of hex in that direction compared to this hex</returns>
	public HexEdgeType GetEdgeType(HexDirection direction)
	{
		return HexMetrics.GetEdgeType(elevation, neighbors[(int)direction].elevation);
	}

	/// <summary>
	/// Returns the edge type of a hex  compared to this current hex
	/// </summary>
	/// <param name="otherCell"> Hex that is to be compared </param>
	/// <returns> Edge type of that hex compared to this hex</returns>
	public HexEdgeType GetEdgeType(HexCell otherCell)
	{
		return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
	}

	public Vector3 Position {
		get {
			return transform.localPosition;
		}
	}
}