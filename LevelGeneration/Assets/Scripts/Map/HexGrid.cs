using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class HexGrid : MonoBehaviour {

	public int cellCountX = 20, cellCountZ = 15;
	int chunkCountX, chunkCountZ;

	public Color defaultColor = Color.white;

	public HexCell cellPrefab;
	public Text cellLabelPrefab;

	HexCell[] cells;

	HexCell currentPathFrom, currentPathTo;
	bool currentPathExists;

	public Texture2D noiseSource;

	public HexGridChunk chunkPrefab;
	HexGridChunk[] chunks;

	HexCellPriorityQueue searchFrontier;

	public int seed;

	int searchFrontierPhase;



	/// <summary>
	/// Used to determine how many cells are in total are in the x direction and z direction.
	/// Creates the chunks then the cells
	/// </summary>
	public void Awake () {

		HexMetrics.noiseSource = noiseSource;
		HexMetrics.InitializeHashGrid(seed);
		CreateMap(cellCountX,cellCountZ);

	}

	/// <summary>
	/// Function used for creating a new map into the scene. The map size needs to be an appropriate number so that each chunk contains the same amount of cells in each.
	/// Destroys all previous chunks in the scene and creates them again along with new cells.
	/// </summary>
	/// <param name="x"> How many cells in the X direction </param>
	/// <param name="z"> How many cells in the Y direction </param>
	/// <returns></returns>
	public bool CreateMap(int x, int z) {
		
		if( x <= 0 || x % HexMetrics.chunkSizeX != 0 || 
			z <= 0 || z % HexMetrics.chunkSizeZ != 0) {
			Debug.LogError("Unsupported map size.");
			return false;
		}

		ClearPath();
		if(chunks != null) {
			for(int i = 0 ; i < chunks.Length ; i++) {
				Destroy(chunks[i].gameObject);
			}
		}
		cellCountX = x;
		cellCountZ = z;
		chunkCountX = cellCountX / HexMetrics.chunkSizeX;
		chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;
		CreateChunks();
		CreateCells();

		return true;
	}
	private void OnEnable()
	{
		if (!HexMetrics.noiseSource) {
			HexMetrics.noiseSource = noiseSource;
			HexMetrics.InitializeHashGrid(seed);
		
		}
	}

	/// <summary>
	/// Used for creating a set amount of chunks based on how many chunks are set or its X amount and Y amount. 
	/// </summary>
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

	/// <summary>
	/// Creates as many cells needed to fill the entire board based on its size.
	/// </summary>
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
	/// Adds the cell to its appropriate chunk
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
		cell.uiRect = label.rectTransform;

		cell.Elevation = 0;

		AddCellToChunk(x, z, cell);
	}

	/// <summary>
	/// Based on the position of the Hex, determines which chunk it will be assigned to
	/// </summary>
	/// <param name="x"> X Column value for the Hex </param>
	/// <param name="z"> Z Column value for the Hex </param>
	/// <param name="cell"> Hex cell </param>
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

	/// <summary>
	/// Calls every individual cell to save their data via a binary writer
	/// </summary>
	/// <param name="writer"> Binary Writer passed through </param>
	public void Save(BinaryWriter writer) {

		writer.Write(cellCountX);
		writer.Write(cellCountZ);

		for(int i = 0; i < cells.Length; i++) {
			cells[i].Save(writer);
		}
	}

	/// <summary>
	/// Each cell reads data from the binary reader
	/// </summary>
	/// <param name="reader"> Binary reader passed through </param>
	public void Load(BinaryReader reader, int header) {
		int x = 20, z = 15;
		if(header >= 1) {
			x = reader.ReadInt32();
			z = reader.ReadInt32();
		}

		if (x != cellCountX || z != cellCountZ) {
			if (!CreateMap(x, z)) {
				return;
			}
		}

		for (int i = 0; i < cells.Length; i++) {
			cells[i].Load(reader);
		}
		for(int i = 0; i < chunks.Length; i++) {
			chunks[i].Refresh();
		}
	}

	/// <summary>
	/// Starts the searching algorithm of cell distances
	/// </summary>
	/// <param name="cell"> Starting cell of search </param>
	public void FindPath(HexCell fromCell, HexCell toCell, int speed) {
		
		ClearPath();
		currentPathFrom = fromCell;
		currentPathTo = toCell;
		currentPathExists = Search(fromCell, toCell, speed);
		ShowPath(speed);
	}


	/// <summary>
	/// Used for finding the most optimal path to reaching an end point by comparing distances and hazards that lead to that point.
	/// It weights each hex based on it distance value and its position being closer to the end point.
	/// The hexes with the most progress in these categories get assessed first to minimize the amount of loops of the method.
	/// Once it finds the end hex, it works backwards with hexes remembering which way they came to work to the origin point. 
	/// Edit. Now assigns how many turns it would take to get there based on distance a unit can move.
	/// </summary>
	/// <param name="fromCell"> Origin hex </param>
	/// <param name="toCell"> Destination hex </param>
	/// <returns> Creates path towards end destination </returns>
	bool Search(HexCell fromCell, HexCell toCell,int speed) {

		searchFrontierPhase += 2;

		if(searchFrontier == null) {
			searchFrontier = new HexCellPriorityQueue();
		}
		else {
			searchFrontier.Clear();
		}

		fromCell.SearchPhase = searchFrontierPhase;
		fromCell.Distance = 0;

		searchFrontier.Enqueue(fromCell);
		while (searchFrontier.Count > 0) {
			
			HexCell current = searchFrontier.Dequeue();
			current.SearchPhase += 1;

			if(current == toCell) {
				return true;
			}

			int currentTurn = current.Distance / speed;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbour = current.GetNeighbor(d);

				if (neighbour == null || neighbour.SearchPhase > searchFrontierPhase) {
					continue;
				}

				if (neighbour.isUnderwater) {
					continue;
				}

				HexEdgeType edgeType = current.GetEdgeType(neighbour);
				if(edgeType == HexEdgeType.Cliff) {
					continue;
				}

				int moveCost;
				if(current.HasRoadThroughEdge(d)){
					moveCost = 1;
				}
				else if(current.Walled != neighbour.Walled) {
					continue;
				}
				else {
					moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
					moveCost += neighbour.UrbanLevel + neighbour.FarmLevel + neighbour.PlantLevel;
				}

				int distance = current.Distance + moveCost;
				int turn = distance / speed;

				if(turn > currentTurn) {
					distance = turn * speed + moveCost;
				}

				if(neighbour.SearchPhase < searchFrontierPhase) {
					neighbour.Distance = distance;
					neighbour.SearchPhase = searchFrontierPhase;
					neighbour.PathFrom = current;
					neighbour.SearchHeuristic = neighbour.coordinates.DistanceTo(toCell.coordinates);
					searchFrontier.Enqueue(neighbour);
				}
				else if(distance < neighbour.Distance) {
					int oldPriority = neighbour.SearchPriority;
					neighbour.Distance = distance;

					neighbour.PathFrom = current;
					searchFrontier.Change(neighbour, oldPriority);
				}
							
			}

		}
		return false;
	}

	/// <summary>
	/// Creates the path to get to a selected cell from its origin. 
	/// </summary>
	/// <param name="speed"></param>
	void ShowPath(int speed) {
		if (currentPathExists) {
			HexCell current = currentPathTo;
			while(current != currentPathFrom) {
				int turn = current.Distance / speed;
				current.SetLabel(turn.ToString());
				current.EnableHighlight(Color.white);
				current = current.PathFrom;
			}
		}
		currentPathFrom.EnableHighlight(Color.blue);
		currentPathTo.EnableHighlight(Color.red);
	}

	/// <summary>
	/// Clears any previously made path, restoring it to defaut values
	/// </summary>
	void ClearPath() {
		if (currentPathExists) {
			HexCell current = currentPathTo;
			while(current != currentPathFrom) {
				current.SetLabel(null);
				current.DisableHighLight();
				current = current.PathFrom;
			}
			current.DisableHighLight();
			currentPathExists = false;
		}
		else if (currentPathFrom) {
			currentPathFrom.DisableHighLight();
			currentPathTo.DisableHighLight();
		}
		currentPathFrom = currentPathTo = null;
	}
}