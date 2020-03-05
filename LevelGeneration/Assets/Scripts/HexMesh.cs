using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour {

	Mesh hexMesh;
	List<Vector3> vertices;
	List<Color> colors;
	List<int> triangles;

	MeshCollider meshCollider;

	void Awake () {
		GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
		meshCollider = gameObject.AddComponent<MeshCollider>();
		hexMesh.name = "Hex Mesh";
		vertices = new List<Vector3>();
		colors = new List<Color>();
		triangles = new List<int>();
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
		Vector3 center = cell.transform.localPosition;
		Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
		Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);

		AddTriangle(center, v1, v2);
		AddTriangleColor(cell.color);

		if (direction <= HexDirection.SE) {
			TriangulateConnection(direction, cell, v1, v2);
		}
	}

	/// <summary>
	/// Used to create the quad and triangle vertexes that are used to join to hexes together
	/// </summary>
	/// <param name="direction"> Direction of the hexagon vertex e.g. NE, SE etc</param>
	/// <param name="cell"> Hexagon </param>
	/// <param name="v1"> Inside corner of hexagon in the direction </param>
	/// <param name="v2"> Second inside corner of hexagon in the direction</param>
	void TriangulateConnection (
		HexDirection direction, HexCell cell, Vector3 v1, Vector3 v2
	) {
		HexCell neighbor = cell.GetNeighbor(direction);
		if (neighbor == null) {
			return;
		}

		Vector3 bridge = HexMetrics.GetBridge(direction);
		Vector3 v3 = v1 + bridge;
		Vector3 v4 = v2 + bridge;

		AddQuad(v1, v2, v3, v4);
		AddQuadColor(cell.color, neighbor.color);

		HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
		if (direction <= HexDirection.E && nextNeighbor != null) {
			AddTriangle(v2, v4, v2 + HexMetrics.GetBridge(direction.Next()));
			AddTriangleColor(cell.color, neighbor.color, nextNeighbor.color);
		}
	}

	/// <summary>
	/// Adds vertex information and triangle information to their lists
	/// </summary>
	/// <param name="v1">First Vector of triangle</param>
	/// <param name="v2">Second Vector of triangle</param>
	/// <param name="v3">Third Vector of triangle</param>
	void AddTriangle (Vector3 v1, Vector3 v2, Vector3 v3) {
		int vertexIndex = vertices.Count;
		vertices.Add(v1);
		vertices.Add(v2);
		vertices.Add(v3);
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
		vertices.Add(v1);
		vertices.Add(v2);
		vertices.Add(v3);
		vertices.Add(v4);
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
}