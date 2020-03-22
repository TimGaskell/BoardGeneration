using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour {

	Mesh hexMesh;
	public bool useCollider, useColors, useUVCoordinates, useUV2Coordinates;
	public bool useTerrainTypes;

	[NonSerialized] List<Vector3> vertices, terrainTypes;
	[NonSerialized] List<Color> colors = new List<Color>();
	[NonSerialized] List<int> triangles = new List<int>();
	[NonSerialized] List<Vector2> uvs, uv2s;

	MeshCollider meshCollider;

	void Awake() {
		GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();

		if (useCollider)
		{
			meshCollider = gameObject.AddComponent<MeshCollider>();
		}
		hexMesh.name = "Hex Mesh";

	}

	/// <summary>
	/// Adds vertex information and triangle information to their lists
	/// </summary>
	/// <param name="v1">First Vector of triangle</param>
	/// <param name="v2">Second Vector of triangle</param>
	/// <param name="v3">Third Vector of triangle</param>
	public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3) {
		int vertexIndex = vertices.Count;
		vertices.Add(HexMetrics.Perturb(v1));
		vertices.Add(HexMetrics.Perturb(v2));
		vertices.Add(HexMetrics.Perturb(v3));
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
	}

	/// <summary>
	/// Creates a triangle without any modification to its vertices's
	/// </summary>
	/// <param name="v1"> Vector 3 position for start of triangle </param>
	/// <param name="v2"> Vector 3 position for second point of triangle</param>
	/// <param name="v3"> Vector 3 position for third point of triangle</param>
	public void AddTriangleUnPerturbed(Vector3 v1, Vector3 v2, Vector3 v3)
	{
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
	public void AddTriangleColor(Color color) {
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
	public void AddTriangleColor(Color c1, Color c2, Color c3) {
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
	public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) {
		int vertexIndex = vertices.Count;
		vertices.Add(HexMetrics.Perturb(v1));
		vertices.Add(HexMetrics.Perturb(v2));
		vertices.Add(HexMetrics.Perturb(v3));
		vertices.Add(HexMetrics.Perturb(v4));
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 3);
	}

	/// <summary>
	/// Adds the quad vertices's and triangles to their respective lists. Adds them without having their vector positions being perturbed by any noise function.
	/// </summary>
	/// <param name="v1"> Inside corner of hexagon in the direction </param>
	/// <param name="v2"> Second inside corner of hexagon in the direction </param>
	/// <param name="v3"> Vector difference between the bridge and v1 </param>
	/// <param name="v4"> Vector difference between the bridge and v2 </param>
	public void AddQuadUnperturbed(
		Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) {
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
	/// <param name="color"> Hex Color </param>
	public void AddQuadColor(Color color)
	{
		colors.Add(color);
		colors.Add(color);
		colors.Add(color);
		colors.Add(color);
	}

	/// <summary>
	/// Add color information of quad to color list
	/// </summary>
	/// <param name="c1"> Hex Color </param>
	/// <param name="c2"> Neighbor Color </param>
	public void AddQuadColor(Color c1, Color c2) {
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
	public void AddQuadColor(Color c1, Color c2, Color c3, Color c4) {
		colors.Add(c1);
		colors.Add(c2);
		colors.Add(c3);
		colors.Add(c4);
	}

	/// <summary>
	/// Adds vertex information to create a triangle in the UVS list. Describes the three points to create a triangle in UV coordinates
	/// </summary>
	/// <param name="uv1"> Vector 3 position for start of triangle</param>
	/// <param name="uv2"> Vector 3 position for second point of triangle</param>
	/// <param name="uv3"> Vector 3 position for third point of triangle</param>
	public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3)
	{
		uvs.Add(uv1);
		uvs.Add(uv2);
		uvs.Add(uv3);
	}

	/// <summary>
	/// Adds vertex information to create a triangle in the UV2S list. Describes the three points to create a triangle in UV coordinates. 
	/// Used as a second set of UVS 
	/// </summary>
	/// <param name="uv1"> Vector 3 position for start of triangle</param>
	/// <param name="uv2"> Vector 3 position for second point of triangle</param>
	/// <param name="uv3"> Vector 3 position for third point of triangle</param>
	public void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3) {
		uv2s.Add(uv1);
		uv2s.Add(uv2);
		uv2s.Add(uv3);
	}

	/// <summary>
	/// Adds the quad vertices's in the UVS list. Describes the four UV points to create a quad.
	/// </summary>
	/// <param name="uv1"> Vector 3 position for start of quad</param>
	/// <param name="uv2"> Vector 3 position for second point of quad</param>
	/// <param name="uv3"> Vector 3 position for third point of quad</param>
	/// <param name="uv4"> Vector 3 position for the fourth point of a quad </param>
	public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
	{
		uvs.Add(uv1);
		uvs.Add(uv2);
		uvs.Add(uv3);
		uvs.Add(uv4);
	}

	/// <summary>
	/// Another way of adding quad vertices's in UVS list. Describes the four UV points to create a quad in respective to the min and max values of the UV map between 0-1.
	/// </summary>
	/// <param name="uMin"> Min U value of UV map</param>
	/// <param name="uMax"> Max U value of UV map</param>
	/// <param name="vMin"> Min V value of UV map </param>
	/// <param name="vMax"> Max V value of UV map</param>
	public void AddQuadUV(float uMin, float uMax, float vMin, float vMax)
	{
		uvs.Add(new Vector2(uMin, vMin));
		uvs.Add(new Vector2(uMax, vMin));
		uvs.Add(new Vector2(uMin, vMax));
		uvs.Add(new Vector2(uMax, vMax));
	}

	/// <summary>
	/// Adds the quad vertices's in the UV2S list. Describes the four UV points to create a quad.
	/// Used as a second set of UVS 
	/// </summary>
	/// <param name="uv1"> Vector 3 position for start of quad</param>
	/// <param name="uv2"> Vector 3 position for second point of quad</param>
	/// <param name="uv3"> Vector 3 position for third point of quad</param>
	/// <param name="uv4"> Vector 3 position for the fourth point of a quad </param>
	public void AddQuadUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4) {
		uv2s.Add(uv1);
		uv2s.Add(uv2);
		uv2s.Add(uv3);
		uv2s.Add(uv4);
	}

	/// <summary>
	/// Another way of adding quad vertices's in UV2S list. Describes the four UV points to create a quad in respective to the min and max values of the UV map between 0-1.
	/// Used as a second set of UVS 
	/// </summary>
	/// <param name="uMin"> Min U value of UV map</param>
	/// <param name="uMax"> Max U value of UV map</param>
	/// <param name="vMin"> Min V value of UV map </param>
	/// <param name="vMax"> Max V value of UV map</param>
	public void AddQuadUV2(float uMin, float uMax, float vMin, float vMax) {
		uv2s.Add(new Vector2(uMin, vMin));
		uv2s.Add(new Vector2(uMax, vMin));
		uv2s.Add(new Vector2(uMin, vMax));
		uv2s.Add(new Vector2(uMax, vMax));
	}

	/// <summary>
	/// Clears all hexmesh data: Meshes, vertices, colors and triangles. Grabs new lists to be used to store new vertices, colors, uvs and triangle data
	/// </summary>
	public void Clear()
	{
		hexMesh.Clear();
		vertices = ListPool<Vector3>.Get();
		if (useColors)
		{
			colors = ListPool<Color>.Get();
		}
		if (useUVCoordinates)
		{
			uvs = ListPool<Vector2>.Get();
		}
		if (useUV2Coordinates) {
			uv2s = ListPool<Vector2>.Get();
		}
		if (useTerrainTypes) {
			terrainTypes = ListPool<Vector3>.Get();
		}
		triangles = ListPool<int>.Get();
	}

	/// <summary>
	/// Sets vertices's, colors, UVs, triangles and colliders for the mesh. Once applied their lists are added back to the stack to be reused.
	/// Determines which data is to be used whilst generating triangles.
	/// </summary>
	public void Apply()
	{
		hexMesh.SetVertices(vertices);
		ListPool<Vector3>.Add(vertices);
		
		if (useColors)
		{
			hexMesh.SetColors(colors);
			ListPool<Color>.Add(colors);
		}
		if (useUVCoordinates)
		{
			hexMesh.SetUVs(0, uvs);
			ListPool<Vector2>.Add(uvs);
		}
		if (useUV2Coordinates) {
			hexMesh.SetUVs(1, uv2s);
			ListPool<Vector2>.Add(uv2s);
		}
		if (useTerrainTypes) {
			hexMesh.SetUVs(2, terrainTypes);
			ListPool<Vector3>.Add(terrainTypes);
		}

		hexMesh.SetTriangles(triangles, 0);
		ListPool<int>.Add(triangles);
		hexMesh.RecalculateNormals();
		
		if (useCollider) { 
			meshCollider.sharedMesh = hexMesh;
		} 
	}

	/// <summary>
	/// Adds a vector 3 which contains the different terrain types for the bottom hex, left hex and right hex.
	/// X = bottom hex
	/// Y = left hex
	/// Z = right hex
	/// </summary>
	/// <param name="type"> Vector 3 containing the terrain types of the hexes </param>
	public void AddTriangleTerrainTypes(Vector3 type) {
		terrainTypes.Add(type);
		terrainTypes.Add(type);
		terrainTypes.Add(type);
	}

	/// <summary>
	/// Adds a vector 3 which contains the different terrain types for the bottom hex, left hex and right hex.
	/// X = bottom hex
	/// Y = left hex
	/// Z = right hex
	/// </summary>
	/// <param name="type"> Vector 3 containing the terrain types of the hexes </param>
	public void AddQuadTerrainTypes (Vector3 types) {
		terrainTypes.Add(types);
		terrainTypes.Add(types);
		terrainTypes.Add(types);
		terrainTypes.Add(types);

	}

}