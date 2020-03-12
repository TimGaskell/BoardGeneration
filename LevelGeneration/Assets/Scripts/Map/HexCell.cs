﻿using UnityEngine;

public class HexCell : MonoBehaviour {

	public HexCoordinates coordinates;

	Color Color;

	bool hasIncomingRiver, hasOutgoingRiver;
	HexDirection incomingRiver, outgoingRiver;

	int elevation = int.MinValue;

	public RectTransform uiRect;

	[SerializeField]
	HexCell[] neighbors;

	[SerializeField]
	bool[] roads;

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

	void RefreshSelfOnly()
	{
		chunk.Refresh();
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

			if(hasOutgoingRiver && elevation < GetNeighbor(outgoingRiver).elevation)
			{
				RemoveOutgoingRiver();
			}
			if(hasIncomingRiver && elevation > GetNeighbor(incomingRiver).elevation)
			{
				RemoveIncomingRiver();
			}

			for (int i = 0; i<roads.Length; i++)
			{
				if(roads[i] && GetElevationDifference((HexDirection)i) > 1){
					SetRoad(i, false);
				}
			}

			Refresh();
		}
	}

	/// <summary>
	/// Either returns the Color value of this Hex or when its color is changed, set the new color value and redraw the Hexcell
	/// </summary>
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

	/// <summary>
	/// Returns the local position of this hex cell
	/// </summary>
	public Vector3 Position {
		get {
			return transform.localPosition;
		}
	}

	/// <summary>
	/// Returns Bool value of if current cell has an incoming river
	/// </summary>
	public bool HasIncomingRiver {
		get {
			return hasIncomingRiver;
		}
	}

	/// <summary>
	/// Returns Bool value of if current cell has an out going river
	/// </summary>
	public bool HasOutgoingRiver {
		get {
			return hasOutgoingRiver;
		}
	}

	/// <summary>
	/// Returns the direction from the which the cell is receiving the incoming river
	/// </summary>
	public HexDirection IncomingRiver {
		get {
			return incomingRiver;
		}
	}

	/// <summary>
	/// Returns the direction from the which the cell is sending its river
	/// </summary>
	public HexDirection OutgoingRiver {
		get {
			return outgoingRiver;
		}
	}

	/// <summary>
	/// returns true if their is a river inside of this hex
	/// </summary>
	public bool HasRiver {
		get {
			return hasIncomingRiver || hasOutgoingRiver;
		}
	}

	/// <summary>
	/// returns true if their is it only has an incoming river or outgoing river
	/// </summary>
	public bool HasRiverBeginOrEnd {
		get {
			return hasIncomingRiver != hasOutgoingRiver;
		}
	}

	/// <summary>
	/// Returns a Bool of whether there is a river flowing through an certain edge of the Hex
	/// </summary>
	/// <param name="direction">  Direction of the hexagon vertex e.g. NE, SE etc </param>
	/// <returns> true if there its incoming river or outgoing river are coming from the same direction as the inputed direction </returns>
	public bool HasRiverThroughEdge(HexDirection direction)
	{
		return hasIncomingRiver && incomingRiver == direction || hasOutgoingRiver && outgoingRiver == direction;
	}

	/// <summary>
	/// Resets the Hex to remove outgoing river information for this hex and its river neighbor. Redraws these hexes in their chunks
	/// </summary>
	public void RemoveOutgoingRiver()
	{
		if (!hasOutgoingRiver)
		{
			return;
		}
		hasOutgoingRiver = false;
		RefreshSelfOnly();

		HexCell neighbor = GetNeighbor(outgoingRiver);
		neighbor.hasIncomingRiver = false;
		neighbor.RefreshSelfOnly();
	}

	/// <summary>
	/// Resets the Hex to remove incoming river information for this hex and its river neighbor. Redraws these hexes in their chunks
	/// </summary>
	public void RemoveIncomingRiver()
	{
		if (!hasIncomingRiver)
		{
			return;
		}
		hasIncomingRiver = false;
		RefreshSelfOnly();

		HexCell neighbor = GetNeighbor(incomingRiver);
		neighbor.hasOutgoingRiver = false;
		neighbor.RefreshSelfOnly();

	}

	/// <summary>
	/// Removes all river information for this hex and the hexes that are neighbored with this hex with river information.
	/// </summary>
	public void RemoveRiver()
	{
		RemoveIncomingRiver();
		RemoveOutgoingRiver();
	}

	/// <summary>
	/// Used to set the hex to having an outgoing river through it. Checks if the hex is allowed to have a river flowing through it and to its neighbor in a specific direction.
	/// Redraws the hex in its respective chunk along with its neighbor which the river is moving to.
	/// </summary>
	/// <param name="direction"> Direction of the hexagon vertex e.g. NE, SE etc</param>
	public void SetOutgoingRiver(HexDirection direction)
	{
		if(hasOutgoingRiver && outgoingRiver == direction)
		{
			return;
		}

		HexCell neighbor = GetNeighbor(direction);
		if (!neighbor || elevation < neighbor.elevation)
		{
			return;
		}

		RemoveOutgoingRiver();
		if(hasIncomingRiver && incomingRiver == direction)
		{
			RemoveIncomingRiver();
		}

		hasOutgoingRiver = true;
		outgoingRiver = direction;
	

		neighbor.RemoveIncomingRiver();
		neighbor.hasIncomingRiver = true;
		neighbor.incomingRiver = direction.Opposite();

		SetRoad((int)direction, false);
	}

	/// <summary>
	/// Bottom Y position of the river bed
	/// </summary>
	public float StreamBedY {
		get {
			return (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep;
		}
	}

	/// <summary>
	/// Elevation of which the river surface on the Y value will be places.
	/// </summary>
	public float RiverSurfaceY {
		get {
			return (elevation + HexMetrics.riverSurfaceElevationOffset) * HexMetrics.elevationStep;
		}
	}

	/// <summary>
	/// Direction from which the river is ending or beginning from.
	/// </summary>
	public HexDirection RiverBeginOrEndDirection {
		get {
			return hasIncomingRiver ? IncomingRiver : outgoingRiver;
		}
	}

	/// <summary>
	/// Checks if there is a road that passes through this hexes edge in a certain direction
	/// </summary>
	/// <param name="direction">  Direction of the hexagon vertex e.g. NE, SE etc</param>
	/// <returns> true or false if there is a road passing through the specified edge in that direction </returns>
	public bool HasRoadThroughEdge (HexDirection direction)
	{
		return roads[(int)direction];
	}

	/// <summary>
	/// Determines an elevation difference between this hex and its neighbor in the specified direction
	/// </summary>
	/// <param name="direction">  Direction of the hexagon vertex e.g. NE, SE etc </param>
	/// <returns> Elevation difference between hex neighbor in direction </returns>
	public int GetElevationDifference(HexDirection direction)
	{
		int difference = elevation - GetNeighbor(direction).elevation;
		return difference >= 0 ? difference : -difference;
	}

	/// <summary>
	/// Checks all directions of this hex cell to see if it contains any roads.
	/// Returns true or false
	/// </summary>
	public bool HasRoads {
		get {
			for(int i = 0; i< roads.Length; i++)
			{
				if (roads[i])
				{
					return true;
				}
			}
			return false;
		}
	}

	/// <summary>
	/// Adds a road to this specific direction if the cell currently doesn't have:
	/// - a road already in that direction
	/// - a river running through the specified edge
	///  - the difference in elevation is not greater than 1
	/// </summary>
	/// <param name="direction">  Direction of the hexagon vertex e.g. NE, SE etc </param>
	public void AddRoad(HexDirection direction)
	{
		if(!roads[(int)direction] && !HasRiverThroughEdge(direction) && GetElevationDifference(direction) <= 1)
		{
			SetRoad((int)direction, true);
		}
	}

	/// <summary>
	/// Removes all roads in the hex
	/// </summary>
	public void RemoveRoads()
	{
		for (int i = 0; i< roads.Length; i++)
		{
			if (roads[i])
			{
				SetRoad(i, false);
			
			}
		}
	}

	/// <summary>
	/// Used for adding or removing roads to a hex based on the state inputed.
	/// Is used to also update the neighbors road information in the opposite direction as the connection affects how its drawn.
	/// Refreshes and redraws the neighbor hex chunk and current hex chunk
	/// </summary>
	/// <param name="index"></param>
	/// <param name="state"></param>
	void SetRoad(int index, bool state)
	{
		roads[index] = state;
		neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
		neighbors[index].RefreshSelfOnly();
		RefreshSelfOnly();
	}
}