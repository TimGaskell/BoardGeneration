using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexGridChunk : MonoBehaviour
{
	HexCell[] cells;

	public HexMesh terrain, rivers, roads, water, waterShore;

	Canvas gridCanvas;




	private void Awake()
	{
		gridCanvas = GetComponentInChildren<Canvas>();

		cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
		ShowUI(false);
	}



	private void LateUpdate()
	{
		Triangulate();
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

	/// <summary>
	/// Clears previous mesh data to create a set of triangles to form a hexagon
	/// </summary>
	/// <param name="cells">Array of hexagons that need to be drawn</param>
	public void Triangulate()
	{
		terrain.Clear();
		rivers.Clear();
		roads.Clear();
		water.Clear();
		waterShore.Clear();
		for (int i = 0; i < cells.Length; i++)
		{
			Triangulate(cells[i]);
		}
		terrain.Apply();
		rivers.Apply();
		roads.Apply();
		water.Apply();
		waterShore.Apply();
	}

	/// <summary>
	/// Function to start drawing triangles to form hexagon
	/// </summary>
	/// <param name="cell"> Hexagon </param>
	void Triangulate(HexCell cell)
	{
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
		{
			Triangulate(d, cell);
		}
	}

	/// <summary>
	/// For every direction of the hexagon create triangles with individual colors.
	/// Creates bridge vertices's and triangles to connect the hexagons. Stops it from creating overlapping bridge by 
	/// only having certain directions create them. E.g. SE,SW,W,NW
	/// </summary>
	/// <param name="direction">  Direction of the hexagon vertex e.g. NE, SE etc </param>
	/// <param name="cell"> Hexagon </param>
	void Triangulate(HexDirection direction, HexCell cell)
	{
		Vector3 center = cell.Position;
		EdgeVertices e = new EdgeVertices(center + HexMetrics.GetFirstSolidCorner(direction), center + HexMetrics.GetSecondSolidCorner(direction));

		if (cell.HasRiver)
		{
			if (cell.HasRiverThroughEdge(direction))
			{
				e.v3.y = cell.StreamBedY;
				if (cell.HasRiverBeginOrEnd)
				{
					TriangulateWithRiverBeginOrEnd(direction, cell, center, e);
				}
				else
				{
					TriangulateWithRiver(direction, cell, center, e);
				}
			}
			else
			{
				TriangulateAdjacentToRiver(direction, cell, center, e);
			}
		}
		else
		{
			TriangulateWithoutRiver(direction, cell, center, e);
		}

		if (direction <= HexDirection.SE)
		{
			TriangulateConnection(direction, cell, e);
		}

		if (cell.isUnderwater)
		{
			TriangulateWater(direction, cell, center);
		}
	}

	/// <summary>
	/// Used to create the quad and triangle vertexes that are used to join to hexes together. Takes into consideration the height differences between hexes to create
	/// either terraces, flats or cliffs connections. These combination of connections have unique corners to suite their geometry.
	/// </summary>
	/// <param name="direction"> Direction of the hexagon vertex e.g. NE, SE etc</param>
	/// <param name="cell"> Hexagon </param>
	/// <param name="v1"> Inside corner of hexagon in the direction </param>
	/// <param name="v2"> Second inside corner of hexagon in the direction</param>
	void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1)
	{
		HexCell neighbor = cell.GetNeighbor(direction);
		if (neighbor == null)
		{
			return;
		}

		Vector3 bridge = HexMetrics.GetBridge(direction);
		bridge.y = neighbor.Position.y - cell.Position.y;
		EdgeVertices e2 = new EdgeVertices(
			e1.v1 + bridge,
			e1.v5 + bridge
		);

		if (cell.HasRiverThroughEdge(direction))
		{
			e2.v3.y = neighbor.StreamBedY;
			TriangulateRiverQuad(e1.v2, e1.v4, e2.v2, e2.v4, cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f, cell.HasIncomingRiver && cell.IncomingRiver == direction);
		}

		if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
		{
			TriangulateEdgeTerraces(e1, cell, e2, neighbor, cell.HasRoadThroughEdge(direction));
		}
		else
		{
			TriangulateEdgeStrip(e1, cell.color, e2, neighbor.color, cell.HasRoadThroughEdge(direction));
		}

		HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
		if (direction <= HexDirection.E && nextNeighbor != null)
		{
			Vector3 v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
			v5.y = nextNeighbor.Position.y;

			if (cell.Elevation <= neighbor.Elevation)
			{
				if (cell.Elevation <= nextNeighbor.Elevation)
				{
					TriangulateCorner(
						e1.v5, cell, e2.v5, neighbor, v5, nextNeighbor
					);
				}
				else
				{
					TriangulateCorner(
						v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor
					);
				}
			}
			else if (neighbor.Elevation <= nextNeighbor.Elevation)
			{
				TriangulateCorner(
					e2.v5, neighbor, v5, nextNeighbor, e1.v5, cell
				);
			}
			else
			{
				TriangulateCorner(
					v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor
				);
			}
		}
	}

	/// <summary>
	/// Used to determine what type of Corner of the current hex is to be created based on the elevation types between the three hexes.
	/// Based on an upside down triangle where it has a bottom, left and right.
	/// </summary>
	/// <param name="bottom"> Bottom vector of the bottom Hex </param>
	/// <param name="bottomCell"> Bottom Hex of the three </param>
	/// <param name="left"> Left vector of the left Hex </param>
	/// <param name="leftCell"> Left Hex of the three </param>
	/// <param name="right"> Right vector of the right hex </param>
	/// <param name="rightCell"> right hex of the three </param>
	void TriangulateCorner(Vector3 bottom, HexCell bottomCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
	{
		HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
		HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

		if (leftEdgeType == HexEdgeType.Slope)
		{
			if (rightEdgeType == HexEdgeType.Slope)
			{
				TriangulateCornerTerraces(
					bottom, bottomCell, left, leftCell, right, rightCell
				);
			}
			else if (rightEdgeType == HexEdgeType.Flat)
			{
				TriangulateCornerTerraces(
					left, leftCell, right, rightCell, bottom, bottomCell
				);
			}
			else
			{
				TriangulateCornerTerracesCliff(
					bottom, bottomCell, left, leftCell, right, rightCell
				);
			}
		}
		else if (rightEdgeType == HexEdgeType.Slope)
		{
			if (leftEdgeType == HexEdgeType.Flat)
			{
				TriangulateCornerTerraces(
					right, rightCell, bottom, bottomCell, left, leftCell
				);
			}
			else
			{
				TriangulateCornerCliffTerraces(
					bottom, bottomCell, left, leftCell, right, rightCell
				);
			}
		}
		else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
		{
			if (leftCell.Elevation < rightCell.Elevation)
			{
				TriangulateCornerCliffTerraces(
					right, rightCell, bottom, bottomCell, left, leftCell
				);
			}
			else
			{
				TriangulateCornerTerracesCliff(
					left, leftCell, right, rightCell, bottom, bottomCell
				);
			}
		}
		else
		{
			terrain.AddTriangle(bottom, left, right);
			terrain.AddTriangleColor(bottomCell.color, leftCell.color, rightCell.color);
		}
	}

	/// <summary>
	/// Based on the three hexes, if two are a slope but are the same height whilst containing terraces connecting. Creates a terraced corner that connects them 
	/// </summary>
	/// <param name="begin"> Cell which is lowest </param>
	/// <param name="begin"> Cell which is lowest </param>
	/// <param name="left"> Left vector of the left Hex </param>
	/// <param name="leftCell"> Left Hex of the three </param>
	/// <param name="right"> Right vector of the right hex </param>
	/// <param name="rightCell"> right hex of the three </param>
	void TriangulateCornerTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
	{
		Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
		Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
		Color c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);
		Color c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, 1);

		terrain.AddTriangle(begin, v3, v4);
		terrain.AddTriangleColor(beginCell.color, c3, c4);

		for (int i = 2; i < HexMetrics.terraceSteps; i++)
		{
			Vector3 v1 = v3;
			Vector3 v2 = v4;
			Color c1 = c3;
			Color c2 = c4;
			v3 = HexMetrics.TerraceLerp(begin, left, i);
			v4 = HexMetrics.TerraceLerp(begin, right, i);
			c3 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
			c4 = HexMetrics.TerraceLerp(beginCell.color, rightCell.color, i);
			terrain.AddQuad(v1, v2, v3, v4);
			terrain.AddQuadColor(c1, c2, c3, c4);
		}

		terrain.AddQuad(v3, v4, left, right);
		terrain.AddQuadColor(c3, c4, leftCell.color, rightCell.color);

	}

	/// <summary>
	/// When a terrace corner is next a Cliff face. This extends the terraces of the hex so that it slowly blends into the cliff. Used for when the cliff is on the right of the cell with the terrace.
	/// </summary>
	/// <param name="begin"> Cell which is lowest </param>
	/// <param name="begin"> Cell which is lowest </param>
	/// <param name="left"> Left vector of the left Hex </param>
	/// <param name="leftCell"> Left Hex of the three </param>
	/// <param name="right"> Right vector of the right hex </param>
	/// <param name="rightCell"> right hex of the three </param>
	void TriangulateCornerTerracesCliff(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
	{
		float b = 1f / (rightCell.Elevation - beginCell.Elevation);
		if (b < 0)
		{
			b = -b;
		}
		Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
		Color boundaryColor = Color.Lerp(beginCell.color, rightCell.color, b);

		TriangulateBoundaryTriangle(
			begin, beginCell, left, leftCell, boundary, boundaryColor
		);

		if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
		{
			TriangulateBoundaryTriangle(
				left, leftCell, right, rightCell, boundary, boundaryColor
			);
		}
		else
		{
			terrain.AddTriangleUnPerturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
			terrain.AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
		}
	}

	/// <summary>
	/// This function reverses the order for "TriangulateCornerTerracesCliff". This is to be used when the cliff is on the left of the cell with the terrace.
	/// </summary>
	/// <param name="begin"> Cell which is lowest </param>
	/// <param name="begin"> Cell which is lowest </param>
	/// <param name="left"> Left vector of the left Hex </param>
	/// <param name="leftCell"> Left Hex of the three </param>
	/// <param name="right"> Right vector of the right hex </param>
	/// <param name="rightCell"> right hex of the three </param>
	void TriangulateCornerCliffTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
	{
		float b = 1f / (leftCell.Elevation - beginCell.Elevation);
		if (b < 0)
		{
			b = -b;
		}
		Vector3 boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
		Color boundaryColor = Color.Lerp(beginCell.color, leftCell.color, b);

		TriangulateBoundaryTriangle(
			right, rightCell, begin, beginCell, boundary, boundaryColor
		);

		if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
		{
			TriangulateBoundaryTriangle(
				left, leftCell, right, rightCell, boundary, boundaryColor
			);
		}
		else
		{
			terrain.AddTriangleUnPerturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
			terrain.AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
		}
	}

	/// <summary>
	/// Used by TriangulateCornerCliffTerraces and TriangulateCornerTerracesCliff. Used to define a point in the joining corner which the terraced Hex End will join into the slope. This extends the 
	/// terrace into the slope.
	/// </summary>
	/// <param name="begin"> Cell which is lowest </param>
	/// <param name="begin"> Cell which is lowest </param>
	/// <param name="left"> Left vector of the left Hex </param>
	/// <param name="leftCell"> Left Hex of the three </param>
	/// <param name="boundary"> </param>
	/// <param name="boundaryColor">  </param>
	void TriangulateBoundaryTriangle(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 boundary, Color boundaryColor)
	{
		Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
		Color c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);

		terrain.AddTriangleUnPerturbed(HexMetrics.Perturb(begin), v2, boundary);
		terrain.AddTriangleColor(beginCell.color, c2, boundaryColor);

		for (int i = 2; i < HexMetrics.terraceSteps; i++)
		{
			Vector3 v1 = v2;
			Color c1 = c2;
			v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
			c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
			terrain.AddTriangleUnPerturbed(v1, v2, boundary);
			terrain.AddTriangleColor(c1, c2, boundaryColor);
		}

		terrain.AddTriangleUnPerturbed(v2, HexMetrics.Perturb(left), boundary);
		terrain.AddTriangleColor(c2, leftCell.color, boundaryColor);
	}

	/// <summary>
	/// Used to convert the bridge connecting two hexes into a stepped terrace leading to that hexes elevation.
	/// </summary>
	/// <param name="beginleft"> starting left vector3 of  terrace connection </param>
	/// <param name="beginRight"> starting right vector3 terrace connection </param>
	/// <param name="beginCell"> the hex which the steps will start from </param>
	/// <param name="endLeft"> end left vector3 of location terrace </param>
	/// <param name="endRight"> end right vector3 of location terrace </param>
	/// <param name="endCell">the hex which the steps will end at </param>
	void TriangulateEdgeTerraces(EdgeVertices begin, HexCell beginCell, EdgeVertices end, HexCell endCell, bool hasRoad)
	{

		EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
		Color c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, 1);

		TriangulateEdgeStrip(begin, beginCell.color, e2, c2, hasRoad);

		for (int i = 2; i < HexMetrics.terraceSteps; i++)
		{
			EdgeVertices e1 = e2;
			Color c1 = c2;
			e2 = EdgeVertices.TerraceLerp(begin, end, i);
			c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, i);
			TriangulateEdgeStrip(e1, c1, e2, c2, hasRoad);
		}

		TriangulateEdgeStrip(e2, c2, end, endCell.color, hasRoad);

	}

	/// <summary>
	/// Triangulation method used to create a Hexagon with a trench for a river that runs through the middle towards the specified direction. Creates the joining channels between rivers.
	/// Adds river quads above the trench with respective UV coordinates from the middle of the hex.
	/// </summary>
	/// <param name="direction"> Direction of the river e.g. NE, SE etc </param>
	/// <param name="cell"> Current cell </param>
	/// <param name="center"> Center location of the cell </param>
	/// <param name="e"> Edge vertices's of the hex </param>
	void TriangulateWithRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
	{
		Vector3 centerL, centerR;

		if (cell.HasRiverThroughEdge(direction.Opposite()))
		{
			centerL = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
			centerR = center + HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
		}
		else if (cell.HasRiverThroughEdge(direction.Next()))
		{
			centerL = center;
			centerR = Vector3.Lerp(center, e.v5, 2f / 3f);
		}
		else if (cell.HasRiverThroughEdge(direction.Previous()))
		{
			centerL = Vector3.Lerp(center, e.v1, 2f / 3f);
			centerR = center;
		}
		else if (cell.HasRiverThroughEdge(direction.Next2()))
		{
			centerL = center;
			centerR = center + HexMetrics.GetSolidEdgeMiddle(direction.Next()) * (0.5f * HexMetrics.innerToOuter);
		}
		else
		{
			centerR = center;
			centerL = center + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * (0.5f * HexMetrics.innerToOuter);
		}
		center = Vector3.Lerp(centerL, centerR, 0.5f);
		EdgeVertices m = new EdgeVertices(Vector3.Lerp(centerL, e.v1, 0.5f), Vector3.Lerp(centerR, e.v5, 0.5f), 1f / 6f);

		m.v3.y = center.y = e.v3.y;

		TriangulateEdgeStrip(m, cell.color, e, cell.color);

		terrain.AddTriangle(centerL, m.v1, m.v2);
		terrain.AddTriangleColor(cell.color);
		terrain.AddQuad(centerL, center, m.v2, m.v3);
		terrain.AddQuadColor(cell.color);
		terrain.AddQuad(center, centerR, m.v3, m.v4);
		terrain.AddQuadColor(cell.color);
		terrain.AddTriangle(centerR, m.v4, m.v5);
		terrain.AddTriangleColor(cell.color);

		bool reversed = cell.IncomingRiver == direction;

		TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY, 0.4f, reversed);
		TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);

	}

	/// <summary>
	/// Triangulation method used to create a Hexagon with a trench for a river that runs through the middle towards the specified direction. Creates the pointed ends for beginning or end of river.
	/// Adds triangle and Quad UVs for the river mesh from the center of the hexagon
	/// </summary>
	/// <param name="direction"> Direction of the river e.g. NE, SE etc </param>
	/// <param name="cell"> Current cell </param>
	/// <param name="center"> Center location of the cell </param>
	/// <param name="e"> Edge vertices's of the hex </param>
	void TriangulateWithRiverBeginOrEnd(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
	{
		EdgeVertices m = new EdgeVertices(Vector3.Lerp(center, e.v1, 0.5f), Vector3.Lerp(center, e.v5, 0.5f));
		m.v3.y = e.v3.y;

		TriangulateEdgeStrip(m, cell.color, e, cell.color);
		TriangulateEdgeFan(center, m, cell.color);


		bool reversed = cell.HasIncomingRiver;
		TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);
		center.y = m.v2.y = m.v4.y = cell.RiverSurfaceY;
		rivers.AddTriangle(center, m.v2, m.v4);
		if (reversed)
		{
			rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f));
		}
		else
		{
			rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(0f, 0.6f), new Vector2(1f, 0.6f));
		}

	}

	/// <summary>
	/// Fills in the surrounding area of the river so that the hexagon is completely filled by land.
	/// </summary>
	/// <param name="direction"> Direction of the river  e.g. NE, SE etc</param>
	/// <param name="cell"> Current hex </param>
	/// <param name="center"> Center of Hex </param>
	/// <param name="e"> Edge vertices's of Hex </param>
	void TriangulateAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
	{

		if (cell.HasRoads)
		{
			TriangulateRoadAdjacentToRiver(direction, cell, center, e);
		}

		if (cell.HasRiverThroughEdge(direction.Next()))
		{
			if (cell.HasRiverThroughEdge(direction.Previous()))
			{
				center += HexMetrics.GetSolidEdgeMiddle(direction) * (HexMetrics.innerToOuter * 0.5f);
			}
			else if (cell.HasRiverThroughEdge(direction.Previous2()))
			{
				center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
			}
		}
		else if (cell.HasRiverThroughEdge(direction.Previous()) && cell.HasRiverThroughEdge(direction.Next2()))
		{

			center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
		}

		EdgeVertices m = new EdgeVertices(Vector3.Lerp(center, e.v1, 0.5f), Vector3.Lerp(center, e.v5, 0.5f));

		TriangulateEdgeStrip(m, cell.color, e, cell.color);
		TriangulateEdgeFan(center, m, cell.color);

	}

	/// <summary>
	/// Creates a triangle fan which goes from its center to draw three triangles in a fan like fashion
	/// </summary>
	/// <param name="center"> Center vector which triangles will start to be drawn from.</param>
	/// <param name="edge"> Group of edge vertices's which define vector3 coordinates of where to draw the triangle. </param>
	/// <param name="color"> Color for the triangle </param>
	void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
	{
		terrain.AddTriangle(center, edge.v1, edge.v2);
		terrain.AddTriangleColor(color);
		terrain.AddTriangle(center, edge.v2, edge.v3);
		terrain.AddTriangleColor(color);
		terrain.AddTriangle(center, edge.v3, edge.v4);
		terrain.AddTriangleColor(color);
		terrain.AddTriangle(center, edge.v4, edge.v5);
		terrain.AddTriangleColor(color);
	}

	/// <summary>
	/// Creates a a strip of quads between two edges. Made up of multiple triangles
	/// </summary>
	/// <param name="e1"> Group of edge vertices's which define vector3 coordinates of where to draw the triangle. </param>
	/// <param name="c1"> Color for first set of edges </param>
	/// <param name="e2"> Group of edge vertices's which define vector3 coordinates of where to draw the triangle. </param>
	/// <param name="c2"> Color for second set of edges</param>
	void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2, bool hasRoad = false)
	{
		terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
		terrain.AddQuadColor(c1, c2);
		terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
		terrain.AddQuadColor(c1, c2);
		terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
		terrain.AddQuadColor(c1, c2);
		terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
		terrain.AddQuadColor(c1, c2);

		if (hasRoad)
		{
			TriangulateRoadSegment(e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4);
		}
	}

	/// <summary>
	/// Adds UV quad vectors for where the river mesh will be drawn in the river bed.
	/// </summary>
	/// <param name="v1"> First point of the river quad</param>
	/// <param name="v2"> Second point of the river quad</param>
	/// <param name="v3"> Third point of the river quad</param>
	/// <param name="v4"> Fourth point of the river quad</param>
	/// <param name="y1"> Height of the first Hex river surface </param>
	/// <param name="y2"> Height of the second Hex river surface</param>
	/// <param name="v"> Which way to stretch the v coordinates of the UV </param>
	/// <param name="reversed"> Reverse UV coordinates if it is an incoming river quad </param>
	void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float v, bool reversed)
	{
		v1.y = v2.y = y1;
		v3.y = v4.y = y2;
		rivers.AddQuad(v1, v2, v3, v4);

		if (reversed)
		{
			rivers.AddQuadUV(1f, 0f, 0.8f - v, 0.6f - v);
		}
		else
		{
			rivers.AddQuadUV(0f, 1f, v, v + 0.2f);
		}

	}

	/// <summary>
	/// Used to fill in center area of the hex.
	/// If the hex has a road through it. It creates a road quads and triangles between the hex and the in the specified direction.
	/// </summary>
	/// <param name="direction"> Direction of the hexagon vertex e.g. NE, SE etc </param>
	/// <param name="cell"> Current Cell</param>
	/// <param name="center"> Center of current cell </param>
	/// <param name="e">Edge vertices's of Hex</param>
	void TriangulateWithoutRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
	{
		TriangulateEdgeFan(center, e, cell.color);

		if (cell.HasRoads)
		{
			Vector2 interpolators = GetRoadInterpolators(direction, cell);
			TriangulateRoad(center, Vector3.Lerp(center, e.v1, interpolators.x), Vector3.Lerp(center, e.v5, interpolators.y), e, cell.HasRoadThroughEdge(direction));
		}
	}

	/// <summary>
	/// Adds UV quad vectors for where the river mesh will be drawn in the river bed. Used if their is only one height
	/// </summary>
	/// <param name="v1"> First point of the river quad</param>
	/// <param name="v2"> Second point of the river quad</param>
	/// <param name="v3"> Third point of the river quad</param>
	/// <param name="v4"> Fourth point of the river quad</param>
	/// <param name="y"> Height of the  Hex river surface</param>
	/// <param name="v"> Which way to stretch the v coordinates of the UV </param>
	/// <param name="reversed"> Reverse UV coordinates if it is an incoming river quad </param>
	void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y, float v, bool reversed)
	{
		TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed);
	}

	/// <summary>
	/// Adds quad information to the roads list about which points the quads are to be drawn to create the road
	/// Adds quad UV information to the roads list on how its mesh texture is to be rendered. 
	/// </summary>
	/// <param name="v1"> Quad Vector 1</param>
	/// <param name="v2"> Quad Vector 2</param>
	/// <param name="v3"> Quad Vector 3</param>
	/// <param name="v4"> Quad Vector 4</param>
	/// <param name="v5"> Quad Vector 5</param>
	/// <param name="v6"> Quad Vector 6</param>
	void TriangulateRoadSegment(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5, Vector3 v6)
	{
		roads.AddQuad(v1, v2, v4, v5);
		roads.AddQuad(v2, v3, v5, v6);
		roads.AddQuadUV(0f, 1f, 0f, 0f);
		roads.AddQuadUV(1f, 0f, 0f, 0f);
	}

	/// <summary>
	/// Decides on how the road is meant to be drawn based off if the road is going through an edge or through the center
	/// </summary>
	/// <param name="center"> center of hex</param>
	/// <param name="mL"> left middle vertex </param>
	/// <param name="mR"> right middle vertex </param>
	/// <param name="e"> Edge vertices's of Hex</param>
	/// <param name="hasRoadThroughEdge"> Bool value of if road is going through an edge</param>
	void TriangulateRoad(Vector3 center, Vector3 mL, Vector3 mR, EdgeVertices e, bool hasRoadThroughEdge)
	{
		if (hasRoadThroughEdge)
		{
			Vector3 mC = Vector3.Lerp(mL, mR, 0.5f);
			TriangulateRoadSegment(mL, mC, mR, e.v2, e.v3, e.v4);
			roads.AddTriangle(center, mL, mC);
			roads.AddTriangle(center, mC, mR);

			roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f));
			roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
		}
		else
		{
			TriangulateRoadEdge(center, mL, mR);
		}
	}

	/// <summary>
	/// Adds additional triangles to the road in the center of the hex
	/// </summary>
	/// <param name="center"> center of the hex </param>
	/// <param name="mL"> left middle vertex </param>
	/// <param name="mR"> right middle vertex </param>
	void TriangulateRoadEdge(Vector3 center, Vector3 mL, Vector3 mR)
	{
		roads.AddTriangle(center, mL, mR);
		roads.AddTriangleUV(new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
	}

	/// <summary>
	/// Used for created more rounded edges towards the center of a cell. 
	/// If there is a road going through an edge in the specified direction then keep the edge triangles wider
	/// Otherwise, depending if there is a road in the adjacent hex to this one, make them smaller.
	/// </summary>
	/// <param name="direction"> Direction of the hexagon vertex e.g. NE, SE etc </param>
	/// <param name="cell"> Current Hex</param>
	/// <returns> Returns X and Y scalers for triangle size </returns>
	Vector2 GetRoadInterpolators(HexDirection direction, HexCell cell)
	{
		Vector2 interpolators;
		if (cell.HasRoadThroughEdge(direction))
		{
			interpolators.x = interpolators.y = 0.5f;
		}
		else
		{
			interpolators.x = cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
			interpolators.y = cell.HasRoadThroughEdge(direction.Next()) ? 0.5f : 0.25f;
		}
		return interpolators;
	}

	/// <summary>
	/// Handler of how roads and rivers are meant to interact with each other based on the rivers direction and directions in adjacent cells.
	/// Manipulates the roads center and center of the hex depending on the river.
	/// This cuts off split pieces of the road mesh when intersected by a river and mover its center to go around the river so they don't overlap
	/// </summary>
	/// <param name="direction"> Direction of the hexagon vertex e.g. NE, SE etc</param>
	/// <param name="cell">Current Hex</param>
	/// <param name="center"> Center of HEx</param>
	/// <param name="e">Edge vertices's of Hex</param>
	void TriangulateRoadAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
	{
		bool hasRoadThroughEdge = cell.HasRoadThroughEdge(direction);
		bool previousHasRiver = cell.HasRiverThroughEdge(direction.Previous());
		bool nextHasRiver = cell.HasRiverThroughEdge(direction.Next());
		Vector2 interpolators = GetRoadInterpolators(direction, cell);
		Vector3 roadCenter = center;

		if (cell.HasRiverBeginOrEnd)
		{
			roadCenter += HexMetrics.GetSolidEdgeMiddle(cell.RiverBeginOrEndDirection.Opposite()) * (1f / 3f);
		}
		else if (cell.IncomingRiver == cell.OutgoingRiver.Opposite())
		{
			Vector3 corner;
			if (previousHasRiver)
			{
				if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Next()))
				{
					return;
				}
				corner = HexMetrics.GetSecondSolidCorner(direction);
			}
			else
			{
				if (!hasRoadThroughEdge && !cell.HasRoadThroughEdge(direction.Previous()))
				{
					return;
				}
				corner = HexMetrics.GetFirstSolidCorner(direction);
			}
			roadCenter += corner * 0.5f;
			center += corner * 0.25f;

		}
		else if (cell.IncomingRiver == cell.OutgoingRiver.Previous())
		{
			roadCenter -= HexMetrics.GetSecondCorner(cell.IncomingRiver) * 0.2f;
		}
		else if (cell.IncomingRiver == cell.OutgoingRiver.Next())
		{
			roadCenter -= HexMetrics.GetFirstCorner(cell.IncomingRiver) * 0.2f;
		}
		else if (previousHasRiver && nextHasRiver)
		{
			if (!hasRoadThroughEdge)
			{
				return;
			}
			Vector3 offset = HexMetrics.GetSolidEdgeMiddle(direction) * HexMetrics.innerToOuter;
			roadCenter += offset * 0.7f;
			center += offset * 0.5f;
		}
		else
		{
			HexDirection middle;
			if (previousHasRiver)
			{
				middle = direction.Next();
			}
			else if (nextHasRiver)
			{
				middle = direction.Previous();
			}
			else
			{
				middle = direction;
			}
			if (!cell.HasRoadThroughEdge(middle) && !cell.HasRoadThroughEdge(middle.Previous()) && !cell.HasRoadThroughEdge(middle.Next()))
			{
				return;
			}
			roadCenter += HexMetrics.GetSolidEdgeMiddle(middle) * 0.25f;
		}

		Vector3 mL = Vector3.Lerp(roadCenter, e.v1, interpolators.x);
		Vector3 mR = Vector3.Lerp(roadCenter, e.v5, interpolators.y);
		TriangulateRoad(roadCenter, mL, mR, e, hasRoadThroughEdge);

		if (previousHasRiver)
		{
			TriangulateRoadEdge(roadCenter, center, mL);
		}
		if (nextHasRiver)
		{
			TriangulateRoadEdge(roadCenter, mR, center);
		}
	}

	void TriangulateWater(HexDirection direction, HexCell cell, Vector3 center)
	{
		center.y = cell.waterSrufaceY;

		HexCell neighbor = cell.GetNeighbor(direction);
		if (neighbor != null && !neighbor.isUnderwater)
		{
			TriangulateWaterShore(direction, cell, neighbor, center);
		}
		else
		{
			TriangulateOpenWater(direction, cell, neighbor, center);
		}

	}

	void TriangulateOpenWater(HexDirection direction, HexCell cell, HexCell Neighbor, Vector3 center)
	{
		Vector3 c1 = center + HexMetrics.GetFirstWaterCorner(direction);
		Vector3 c2 = center + HexMetrics.GetSecondWaterCorner(direction);

		water.AddTriangle(center, c1, c2);

		if (direction <= HexDirection.SE && Neighbor != null)
		{
			Vector3 bridge = HexMetrics.GetWaterBridge(direction);
			Vector3 e1 = c1 + bridge;
			Vector3 e2 = c2 + bridge;

			water.AddQuad(c1, c2, e1, e2);

			if (direction <= HexDirection.E)
			{
				HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
				if (nextNeighbor == null || !nextNeighbor.isUnderwater)
				{
					return;
				}
				water.AddTriangle(c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next()));
			}
		}
	}

	void TriangulateWaterShore(HexDirection direction, HexCell cell, HexCell Neighbour, Vector3 center)
	{

		EdgeVertices e1 = new EdgeVertices(center + HexMetrics.GetFirstWaterCorner(direction), center + HexMetrics.GetSecondWaterCorner(direction));
		water.AddTriangle(center, e1.v1, e1.v2);
		water.AddTriangle(center, e1.v2, e1.v3);
		water.AddTriangle(center, e1.v3, e1.v4);
		water.AddTriangle(center, e1.v4, e1.v5);

		Vector3 center2 = Neighbour.Position;
		center2.y = center.y;
		EdgeVertices e2 = new EdgeVertices(
			center2 + HexMetrics.GetSecondSolidCorner(direction.Opposite()),
			center2 + HexMetrics.GetFirstSolidCorner(direction.Opposite())
		);
		waterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
		waterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
		waterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
		waterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
		waterShore.AddQuadUV(0f, 0f, 0f, 1f);
		waterShore.AddQuadUV(0f, 0f, 0f, 1f);
		waterShore.AddQuadUV(0f, 0f, 0f, 1f);
		waterShore.AddQuadUV(0f, 0f, 0f, 1f);

		HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
		if (nextNeighbor != null)
		{
			Vector3 v3 = nextNeighbor.Position + (nextNeighbor.isUnderwater ? HexMetrics.GetFirstWaterCorner(direction.Previous()) :HexMetrics.GetFirstSolidCorner(direction.Previous()));
			v3.y = center.y;
			waterShore.AddTriangle(e1.v5, e2.v5, v3);
			waterShore.AddTriangleUV(new Vector2(0f, 0f),new Vector2(0f, 1f),new Vector2(0f, nextNeighbor.isUnderwater ? 0f : 1f));
		}
	}
}
