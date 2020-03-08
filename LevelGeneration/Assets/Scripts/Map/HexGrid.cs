using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour {

	int cellCountX, cellCountZ;

	public int chunkCountX = 4, chunkCountZ = 3;

	public Color defaultColor = Color.white;

	public HexCell cellPrefab;
	public Text cellLabelPrefab;

	HexCell[] cells;

	public Texture2D noiseSource;

	public HexGridChunk chunkPrefab;
	HexGridChunk[] chunks;

	public void Awake () {

		HexMetrics.noiseSource = noiseSource;

		cellCountX = chunkCountX * HexMetrics.chunkSizeX;
		cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ;

		CreateChunks();
		CreateCells();
	}


	private void OnEnable()
	{
		HexMetrics.noiseSource = noiseSource;
	}

	void CreateChunks()
	{
		chunks = new HexGridChunk[chunkCountX * chunkCountZ];

		for (int z = 0, i = 0; z < chunkCountZ; z++)
		{
			for (int x = 0; x < chunkCountX; x++)
			{
				HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
				chunk.transform.SetParent(transform);
			}
		}
	}

	void CreateCells()
	{
		cells = new HexCell[cellCountZ * cellCountX];

		for (int z = 0, i = 0; z < cellCountZ; z++)
		{
			for (int x = 0; x < cellCountX; x++)
			{
				CreateCell(x, z, i++);
			}
		}

	}


	/// <summary>
	/// Redraws the Hex with the respective color now added onto it
	/// </summary>
	/// <param name="position"> Position in world space </param>
	/// <param name="color"> Color value for hex </param>
	public HexCell GetCell (Vector3 position) {
		position = transform.InverseTransformPoint(position);
		HexCoordinates coordinates = HexCoordinates.FromPosition(position);
		int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
		return cells[index];
	}

	/// <summary>
	/// Grabs a Hex Cell based off its HexCoordinates in the scene
	/// </summary>
	/// <param name="coordinates"> HexCoordinates struct for the cell </param>
	/// <returns> HexCell at that coordinate set or returns null </returns>
	public HexCell GetCell(HexCoordinates coordinates)
	{
		int z = coordinates.Z;
		if(z <0 || z >= cellCountZ)
		{
			return null;
		}
		int x = coordinates.X + z / 2;
		if(x < 0 || x >= cellCountX)
		{
			return null;
		}
		return cells[x + z * cellCountX];
	}

	/// <summary>
	/// Instantiates the Hex prefabs, assigning them their positions in relation to each other.
	/// Assigns the neighbors of each hex in relation to each side (E.g. NE,SW, W, E, SE, NW).
	/// Also responsible for attaching a Text label on top of each Hex displaying its location.
	/// </summary>
	/// <param name="x"> X Column value for the Hex </param>
	/// <param name="z"> Z Column value for the Hex </param>
	/// <param name="i"> Index for cells </param>
	void CreateCell (int x, int z, int i) {
		Vector3 position;
		position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
		position.y = 0f;
		position.z = z * (HexMetrics.outerRadius * 1.5f);

		HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
		cell.transform.localPosition = position;
		cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
		cell.color = defaultColor;

		if (x > 0) {
			cell.SetNeighbor(HexDirection.W, cells[i - 1]);
		}
		if (z > 0) {
			if ((z & 1) == 0) {
				cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
				if (x > 0) {
					cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
				}
			}
			else {
				cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
				if (x < cellCountX - 1) {
					cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
				}
			}
		}

		Text label = Instantiate<Text>(cellLabelPrefab);
	
		label.rectTransform.anchoredPosition =
			new Vector2(position.x, position.z);
		label.text = cell.coordinates.ToStringOnSeparateLines();
		cell.uiRect = label.rectTransform;

		cell.Elevation = 0;

		AddCellToChunk(x, z, cell);
	}

	/// <summary>
	/// Based on the position of the Hex, determines which chunk it will be assigned to
	/// </summary>
	/// <param name="x"> X Column value for the Hex </param>
	/// <param name="z"> Z Column value for the Hex </param>
	/// <param name="cell"> Hexcell </param>
	void AddCellToChunk(int x, int z, HexCell cell)
	{
		int chunkX = x / HexMetrics.chunkSizeX;
		int chunkZ = z / HexMetrics.chunkSizeZ;
		HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

		int localX = x - chunkX * HexMetrics.chunkSizeX;
		int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
		chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
	}

	/// <summary>
	/// Toggle for showing Label UI on Hex
	/// </summary>
	/// <param name="visible"> Bool value for being on or off </param>
	public void ShowUI(bool visible)
	{
		for(int i=0; i< chunks.Length; i++)
		{
			chunks[i].ShowUI(visible);
		}
	}

}