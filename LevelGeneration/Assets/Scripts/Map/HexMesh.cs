using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour {

	Mesh hexMesh;
	static List<Vector3> vertices = new List<Vector3>();
	static List<Color> colors = new List<Color>();
	static List<int> triangles = new List<int>();

	MeshCollider meshCollider;

	void Awake () {
		GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
		meshCollider = gameObject.AddComponent<MeshCollider>();
		hexMesh.name = "Hex Mesh";

	}

	/// <summary>
	/// Clears previous mesh data to create a set of triangles to form a hexagon
	/// </summary>
	/// <param name="cells">Array of hexagons that need to be drawn</param>
	public void Triangulate (HexCell[] cells) {
		hexMesh.Clear();
		vertices.Clear();
		colors.Clear();
		triangles.Clear();
		for (int i = 0; i < cells.Length; i++) {
			Triangulate(cells[i]);
		}
		hexMesh.vertices = vertices.ToArray();
		hexMesh.colors = colors.ToArray();
		hexMesh.triangles = triangles.ToArray();
		hexMesh.RecalculateNormals();
		meshCollider.sharedMesh = hexMesh;
	}

	/// <summary>
	/// Function to start drawing triangles to form hexagon
	/// </summary>
	/// <param name="cell"> Hexagon </param>
	void Triangulate (HexCell cell) {
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
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
	void Triangulate (HexDirection direction, HexCell cell) {
		Vector3 center = cell.Position;
		EdgeVertices e = new EdgeVertices(center + HexMetrics.GetFirstSolidCorner(direction),center + HexMetrics.GetSecondSolidCorner(direction));

		TriangulateEdgeFan(center, e, cell.color);

		if (direction <= HexDirection.SE)
		{
			TriangulateConnection(direction, cell, e);
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
	void TriangulateConnection (HexDirection direction, HexCell cell, EdgeVertices e1) {
		HexCell neighbor = cell.GetNeighbor(direction);
		if (neighbor == null)
		{
			return;
		}

		Vector3 bridge = HexMetrics.GetBridge(direction);
		bridge.y = neighbor.Position.y - cell.Position.y;
		EdgeVertices e2 = new EdgeVertices(
			e1.v1 + bridge,
			e1.v4 + bridge
		);

		if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
		{
			TriangulateEdgeTerraces(e1, cell, e2, neighbor);
		}
		else
		{
			TriangulateEdgeStrip(e1, cell.color, e2, neighbor.color);
		}

		HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
		if (direction <= HexDirection.E && nextNeighbor != null)
		{
			Vector3 v5 = e1.v4 + HexMetrics.GetBridge(direction.Next());
			v5.y = nextNeighbor.Position.y;

			if (cell.Elevation <= neighbor.Elevation)
			{
				if (cell.Elevation <= nextNeighbor.Elevation)
				{
					TriangulateCorner(
						e1.v4, cell, e2.v4, neighbor, v5, nextNeighbor
					);
				}
				else
				{
					TriangulateCorner(
						v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor
					);
				}
			}
			else if (neighbor.Elevation <= nextNeighbor.Elevation)
			{
				TriangulateCorner(
					e2.v4, neighbor, v5, nextNeighbor, e1.v4, cell
				);
			}
			else
			{
				TriangulateCorner(
					v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor
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
			AddTriangle(bottom, left, right);
			AddTriangleColor(bottomCell.color, leftCell.color, rightCell.color);
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

		AddTriangle(begin, v3, v4);
		AddTriangleColor(beginCell.color, c3, c4);

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
			AddQuad(v1, v2, v3, v4);
			AddQuadColor(c1, c2, c3, c4);
		}

		AddQuad(v3, v4, left, right);
		AddQuadColor(c3, c4, leftCell.color, rightCell.color);

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
	void TriangulateCornerTerracesCliff( Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell, Vector3 right, HexCell rightCell)
	{
		float b = 1f / (rightCell.Elevation - beginCell.Elevation);
		if (b < 0)
		{
			b = -b;
		}
		Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(right), b);
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
			AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
			AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
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
		Vector3 boundary = Vector3.Lerp(Perturb(begin), Perturb(left), b);
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
			AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
			AddTriangleColor(leftCell.color, rightCell.color, boundaryColor);
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
		Vector3 v2 = Perturb(HexMetrics.TerraceLerp(begin, left, 1));
		Color c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, 1);

		AddTriangleUnperturbed(Perturb(begin), v2, boundary);
		AddTriangleColor(beginCell.color, c2, boundaryColor);

		for (int i = 2; i < HexMetrics.terraceSteps; i++)
		{
			Vector3 v1 = v2;
			Color c1 = c2;
			v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i));
			c2 = HexMetrics.TerraceLerp(beginCell.color, leftCell.color, i);
			AddTriangleUnperturbed(v1, v2, boundary);
			AddTriangleColor(c1, c2, boundaryColor);
		}

		AddTriangleUnperturbed(v2, Perturb(left), boundary);
		AddTriangleColor(c2, leftCell.color, boundaryColor);
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
	void TriangulateEdgeTerraces(EdgeVertices begin, HexCell beginCell, EdgeVertices end, HexCell endCell) {

		EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
		Color c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, 1);

		TriangulateEdgeStrip(begin, beginCell.color, e2, c2);

		for (int i = 2; i < HexMetrics.terraceSteps; i++)
		{
			EdgeVertices e1 = e2;
			Color c1 = c2;
			e2 = EdgeVertices.TerraceLerp(begin, end, i);
			c2 = HexMetrics.TerraceLerp(beginCell.color, endCell.color, i);
			TriangulateEdgeStrip(e1, c1, e2, c2);
		}

		TriangulateEdgeStrip(e2, c2, end, endCell.color);

	}

	/// <summary>
	/// Adds vertex information and triangle information to their lists
	/// </summary>
	/// <param name="v1">First Vector of triangle</param>
	/// <param name="v2">Second Vector of triangle</param>
	/// <param name="v3">Third Vector of triangle</param>
	void AddTriangle (Vector3 v1, Vector3 v2, Vector3 v3) {
		int vertexIndex = vertices.Count;
		vertices.Add(Perturb(v1));
		vertices.Add(Perturb(v2));
		vertices.Add(Perturb(v3));
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
	}

	/// <summary>
	/// Adds singular color to hexagon
	/// </summary>
	/// <param name="color"> Color for inside Hexagon </param>
	void AddTriangleColor (Color color) {
		colors.Add(color);
		colors.Add(color);
		colors.Add(color);
	}

	/// <summary>
	/// Blended color based on surrounding neighbor hexagon colors
	/// </summary>
	/// <param name="c1"> Main Hexagon color </param>
	/// <param name="c2"> Neighbor color </param>
	/// <param name="c3"> Neighbor color </param>
	void AddTriangleColor (Color c1, Color c2, Color c3) {
		colors.Add(c1);
		colors.Add(c2);
		colors.Add(c3);
	}

	/// <summary>
	/// Adds the quad vertices's and triangles that create it for the connection between hexagons to their respective lists
	/// </summary>
	/// <param name="v1"> Inside corner of hexagon in the direction </param>
	/// <param name="v2"> Second inside corner of hexagon in the direction </param>
	/// <param name="v3"> Vector difference between the bridge and v1 </param>
	/// <param name="v4"> Vector difference between the bridge and v2 </param>
	void AddQuad (Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) {
		int vertexIndex = vertices.Count;
		vertices.Add(Perturb(v1));
		vertices.Add(Perturb(v2));
		vertices.Add(Perturb(v3));
		vertices.Add(Perturb(v4));
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 3);
	}

	/// <summary>
	/// Add color information of quad to color list
	/// </summary>
	/// <param name="c1"> Hex Color </param>
	/// <param name="c2"> Neighbor Color </param>
	void AddQuadColor (Color c1, Color c2) {
		colors.Add(c1);
		colors.Add(c1);
		colors.Add(c2);
		colors.Add(c2);
	}

	/// <summary>
	/// Add color information of quad to color list
	/// </summary>
	/// <param name="c1">Hex color</param>
	/// <param name="c2">Neighbor Color </param>
	/// <param name="c3">Neighbor Color </param>
	/// <param name="c4">Neighbor Color </param>
	void AddQuadColor (Color c1, Color c2, Color c3, Color c4) {
		colors.Add(c1);
		colors.Add(c2);
		colors.Add(c3);
		colors.Add(c4);
	}

	/// <summary>
	/// Samples the noise texture to determine how much to modify the x and z values on a vector.
	/// Used for modifying triangles and bridge positions.
	/// </summary>
	/// <param name="position"> Vector3 position for point where a triangle will be drawn</param>
	/// <returns> new vector3 with perturbed x and z values </returns>
	Vector3 Perturb(Vector3 position)
	{
		Vector4 sample = HexMetrics.SampleNoise(position);
		position.x += (sample.x * 2f - 1f) * HexMetrics.cellPerturbStrength;
		position.z += (sample.z * 2f - 1f) * HexMetrics.cellPerturbStrength;
		return position;
	}

	/// <summary>
	/// Creates a triangle fan which goes from its center to draw three triangles in a fan like fashion
	/// </summary>
	/// <param name="center"> Center vector which triangles will start to be drawn from.</param>
	/// <param name="edge"> Group of edge vertices's which define vector3 coordinates of where to draw the triangle. </param>
	/// <param name="color"> Color for the triangle </param>
	void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
	{
		AddTriangle(center, edge.v1, edge.v2);
		AddTriangleColor(color);
		AddTriangle(center, edge.v2, edge.v3);
		AddTriangleColor(color);
		AddTriangle(center, edge.v3, edge.v4);
		AddTriangleColor(color);
	}

	/// <summary>
	/// Creates a a strip of quads between two edges. Made up of multiple triangles
	/// </summary>
	/// <param name="e1"> Group of edge vertices's which define vector3 coordinates of where to draw the triangle. </param>
	/// <param name="c1"> Color for first set of edges </param>
	/// <param name="e2"> Group of edge vertices's which define vector3 coordinates of where to draw the triangle. </param>
	/// <param name="c2"> Color for second set of edges</param>
	void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2)
	{
		AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
		AddQuadColor(c1, c2);
		AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
		AddQuadColor(c1, c2);
		AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
		AddQuadColor(c1, c2);
	}

	/// <summary>
	/// Creates a triangle without any modification to its vertices's
	/// </summary>
	/// <param name="v1"> Vector 3 position for start of triangle </param>
	/// <param name="v2"> Vector 3 position for second point of triangle</param>
	/// <param name="v3"> Vector 3 position for third point of triangle</param>
	void AddTriangleUnperturbed( Vector3 v1, Vector3 v2, Vector3 v3)
	{
		int vertexIndex = vertices.Count;
		vertices.Add(v1);
		vertices.Add(v2);
		vertices.Add(v3);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);

	}
}