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

	List<HexUnit> units = new List<HexUnit>();
	public HexUnit unitPrefab;

	public int seed;

	int searchFrontierPhase;

	HexCellShaderData cellShaderData;

	public bool wrapping;

	Transform[] columns;

	int currentCenterColumnIndex = -1;

	public bool HasPath {
		get {
			return currentPathExists;
		}
	}


	/// <summary>
	/// Used to determine how many cells are in total are in the x direction and z direction.
	/// Creates the chunks then the cells
	/// </summary>
	public void Awake () {

		HexMetrics.noiseSource = noiseSource;
		HexMetrics.InitializeHashGrid(seed);
		HexUnit.unitPrefab = unitPrefab;
		cellShaderData = gameObject.AddComponent<HexCellShaderData>();
		cellShaderData.Grid = this;
		CreateMap(cellCountX,cellCountZ, wrapping);

	}

	/// <summary>
	/// Function used for creating a new map into the scene. The map size needs to be an appropriate number so that each chunk contains the same amount of cells in each.
	/// Destroys all previous chunks in the scene and creates them again along with new cells.
	/// </summary>
	/// <param name="x"> How many cells in the X direction </param>
	/// <param name="z"> How many cells in the Y direction </param>
	/// <returns></returns>
	public bool CreateMap(int x, int z, bool wrapping) {
		
		if( x <= 0 || x % HexMetrics.chunkSizeX != 0 || 
			z <= 0 || z % HexMetrics.chunkSizeZ != 0) {
			Debug.LogError("Unsupported map size.");
			return false;
		}

		ClearPath();
		ClearUnits();

		if(columns != null) {
			for(int i = 0 ; i < columns.Length ; i++) {
				Destroy(columns[i].gameObject);
			}
		}
		cellCountX = x;
		cellCountZ = z;
		this.wrapping = wrapping;
		currentCenterColumnIndex = -1;
		HexMetrics.wrapSize = wrapping ? cellCountX : 0;
		chunkCountX = cellCountX / HexMetrics.chunkSizeX;
		chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;

		cellShaderData.Initialize(cellCountX, cellCountZ);

		CreateChunks();
		CreateCells();

		return true;
	}
	private void OnEnable()
	{
		if (!HexMetrics.noiseSource) {
			HexMetrics.noiseSource = noiseSource;
			HexMetrics.InitializeHashGrid(seed);
			HexUnit.unitPrefab = unitPrefab;
			HexMetrics.wrapSize = wrapping ? cellCountX : 0;
			ResetVisibility();
		
		}
	}

	/// <summary>
	/// Used for creating a set amount of chunks based on how many chunks are set or its X amount and Y amount. 
	/// </summary>
	void CreateChunks()
	{
		columns = new Transform[chunkCountX];
		for (int x = 0; x < chunkCountX; x++) {
			columns[x] = new GameObject("Column").transform;
			columns[x].SetParent(transform, false);
		}

		chunks = new HexGridChunk[chunkCountX * chunkCountZ];
		for (int z = 0, i = 0; z < chunkCountZ; z++) {
			for (int x = 0; x < chunkCountX; x++) {
				HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
				chunk.transform.SetParent(columns[x], false);
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
		return GetCell(coordinates);
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
	/// Based on a ray cast ray inputed. Will return a Hex cell based on that rays vector3 converted to hex coordinates.
	/// </summary>
	/// <param name="ray"> Ray cast </param>
	/// <returns> Hex cell from ray cast </returns>
	public HexCell GetCell(Ray ray) {
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit)) {
			return GetCell(hit.point);
		}
		return null;
	}

	/// <summary>
	/// Returns the cell located at the offsets of x and z
	/// </summary>
	/// <param name="xOffset"> Hex cell x offset </param>
	/// <param name="zOffset"> Hex cell z offset </param>
	/// <returns> Hex cell located at the x and z offset </returns>
	public HexCell GetCell(int xOffset, int zOffset) {
		return cells[xOffset + zOffset * cellCountX];
	}

	/// <summary>
	/// Returns hexcell based on its index
	/// </summary>
	/// <param name="cellIndex"> Hexcell index</param>
	/// <returns> Hexcell at index</returns>
	public HexCell GetCell(int cellIndex) {
		return cells[cellIndex];
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
		position.x = (x + z * 0.5f - z / 2) * HexMetrics.innerDiameter;
		position.y = 0f;
		position.z = z * (HexMetrics.outerRadius * 1.5f);

		HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
		cell.transform.localPosition = position;
		cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
		cell.Index = i;
		cell.ColumnIndex = x / HexMetrics.chunkSizeX;
		cell.ShaderData = cellShaderData;

		if (wrapping) {
			cell.Explorable = z > 0 && z < cellCountZ - 1;
		}
		else {
			cell.Explorable =
				x > 0 && z > 0 && x < cellCountX - 1 && z < cellCountZ - 1;
		}

		if (x > 0) {
			cell.SetNeighbor(HexDirection.W, cells[i - 1]);
			if (wrapping && x == cellCountX - 1) {
				cell.SetNeighbor(HexDirection.E, cells[i - x]);
			}
		}
		if (z > 0) {
			if ((z & 1) == 0) {
				cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
				if (x > 0) {
					cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
				}
				else if (wrapping) {
					cell.SetNeighbor(HexDirection.SW, cells[i - 1]);
				}
			}
			else {
				cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
				if (x < cellCountX - 1) {
					cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
				}
				else if (wrapping) {
					cell.SetNeighbor(
						HexDirection.SE, cells[i - cellCountX * 2 + 1]
					);
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
		writer.Write(wrapping);

		for(int i = 0; i < cells.Length; i++) {
			cells[i].Save(writer);
		}
		writer.Write(units.Count);
		for(int i = 0; i < units.Count; i++) {
			units[i].Save(writer);
		}
	}

	/// <summary>
	/// Each cell reads data from the binary reader
	/// </summary>
	/// <param name="reader"> Binary reader passed through </param>
	public void Load(BinaryReader reader, int header) {
		ClearPath();
		ClearUnits();
		int x = 20, z = 15;
		if(header >= 1) {
			x = reader.ReadInt32();
			z = reader.ReadInt32();
		}

		bool wrapping = header >= 5 ? reader.ReadBoolean() : false;
		if (x != cellCountX || z != cellCountZ || this.wrapping != wrapping) {
			if (!CreateMap(x, z, wrapping)) {
				return;
			}
		}

		bool originalImmediateMode = cellShaderData.ImmediateMode;
		cellShaderData.ImmediateMode = true;

		for (int i = 0; i < cells.Length; i++) {
			cells[i].Load(reader,header);
		}
		for(int i = 0; i < chunks.Length; i++) {
			chunks[i].Refresh();
		}
		if (header >= 2) {
			int unitCount = reader.ReadInt32();
			for (int i = 0; i < unitCount; i++) {
				HexUnit.Load(reader, this);
			}
		}

		cellShaderData.ImmediateMode = originalImmediateMode;
	}

	/// <summary>
	/// Removes all units on the map from the list. Destroys all their game objects as well
	/// </summary>
	void ClearUnits() {
		for(int i = 0; i <units.Count; i++) {
			units[i].Die();
		}
		units.Clear();
	}

	/// <summary>
	/// Adds Hex unit to list. Sets its transform, location and orientation
	/// </summary>
	/// <param name="unit"> Unit on map </param>
	/// <param name="location"> Hex cell location for unit </param>
	/// <param name="orientation"> Y rotation of unit</param>
	public void AddUnit(HexUnit unit, HexCell location, float orientation) {
		units.Add(unit);
		unit.Grid = this;
		unit.Location = location;
		unit.Orientation = orientation;
	}

	/// <summary>
	/// Removes a unit from the list. Destroys the unit
	/// </summary>
	/// <param name="unit"> Unit thats being removed </param>
	public void RemoveUnit (HexUnit unit) {
		units.Remove(unit);
		unit.Die();
	}

	public void MakeChildOfColumn (Transform child, int columnIndex) {
		child.SetParent(columns[columnIndex], false);
	}

	/// <summary>
	/// Starts the searching algorithm of cell distances
	/// </summary>
	/// <param name="cell"> Starting cell of search </param>
	public void FindPath(HexCell fromCell, HexCell toCell, HexUnit unit) {	
		ClearPath();
		currentPathFrom = fromCell;
		currentPathTo = toCell;
		currentPathExists = Search(fromCell, toCell, unit);
		ShowPath(unit.Speed);
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
	bool Search(HexCell fromCell, HexCell toCell, HexUnit unit) {

		int speed = unit.Speed;
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

			int currentTurn = (current.Distance - 1) / speed;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbour = current.GetNeighbor(d);

				if (neighbour == null ||neighbour.SearchPhase > searchFrontierPhase) {
					continue;
				}

				if (!unit.IsValidDestination(neighbour)) {
					continue;
				}

				int moveCost = unit.GetMoveCost(current, neighbour, d);
			
				if(moveCost < 0) {
					continue;
				}

				int distance = current.Distance + moveCost;
				int turn = (distance - 1) / speed;

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
				int turn = (current.Distance - 1) / speed;
				current.SetLabel(turn.ToString());
				current.EnableHighlight(Color.white);
				current = current.PathFrom;
			}
		}
		currentPathFrom.EnableHighlight(Color.blue);
		currentPathTo.EnableHighlight(Color.red);
	}

	/// <summary>
	/// Clears any previously made path, restoring it to default values
	/// </summary>
	public void ClearPath() {
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

	/// <summary>
	/// Returns a list of Hex cells which create a path towards the end point of the map. 
	/// </summary>
	/// <returns> Returns list of hexes that lead to end point </returns>
	public List<HexCell> GetPath() {
		if (!currentPathExists) {
			return null;
		}
		List<HexCell> path = ListPool<HexCell>.Get();
		for(HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom) {
			path.Add(c);
		}
		path.Add(currentPathFrom);
		path.Reverse();
		return path;
	}

	/// <summary>
	/// Gets all hex cells in a given range from a specified point.
	/// </summary>
	/// <param name="fromCell"> Starting cell </param>
	/// <param name="range"> How many hexes from the origin it will search in a circle </param>
	/// <returns> List of Hex cells in a radius from the center hex </returns>
	List<HexCell> GetVisibleCells (HexCell fromCell,int range) {
		List<HexCell> visibleCells = ListPool<HexCell>.Get();

		searchFrontierPhase += 2;

		if (searchFrontier == null) {
			searchFrontier = new HexCellPriorityQueue();
		}
		else {
			searchFrontier.Clear();
		}

		range += fromCell.ViewElevation;
		fromCell.SearchPhase = searchFrontierPhase;
		fromCell.Distance = 0;

		HexCoordinates fromCoordinates = fromCell.coordinates;

		searchFrontier.Enqueue(fromCell);
		while (searchFrontier.Count > 0) {

			HexCell current = searchFrontier.Dequeue();
			current.SearchPhase += 1;

			visibleCells.Add(current);

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbour = current.GetNeighbor(d);

				if (neighbour == null || neighbour.SearchPhase > searchFrontierPhase || !neighbour.Explorable) {
					continue;
				}			
				
				int distance = current.Distance + 1;
				
				if(distance + neighbour.ViewElevation > range || distance > fromCoordinates.DistanceTo(neighbour.coordinates)) {
					continue;
				}

				if (neighbour.SearchPhase < searchFrontierPhase) {
					neighbour.Distance = distance;
					neighbour.SearchPhase = searchFrontierPhase;
					neighbour.SearchHeuristic = 0;
					searchFrontier.Enqueue(neighbour);
				}
				else if (distance < neighbour.Distance) {
					int oldPriority = neighbour.SearchPriority;
					neighbour.Distance = distance;

					searchFrontier.Change(neighbour, oldPriority);
				}

			}

		}
		return visibleCells;
	}

	/// <summary>
	/// Starts the visibility search of cells in the radius of the origin cell and range. Increases all cells visibility that is returned by the list
	/// </summary>
	/// <param name="fromCell"> Starting cell </param>
	/// <param name="range"> Radius of search </param>
	public void IncreaseVisibility(HexCell fromCell, int range) {
		List<HexCell> cells = GetVisibleCells(fromCell, range);
		for(int i = 0; i < cells.Count; i++) {
			cells[i].IncreaseVisibility();
		}
		ListPool<HexCell>.Add(cells);
	}

	/// <summary>
	/// Starts the visibility search of cells in the radius of the origin cell and range. Decreases all cells visibility that is returned by the list
	/// </summary>
	/// <param name="fromCell"> Starting cell </param>
	/// <param name="range"> Radius of search </param>
	public void DecreaseVisibility(HexCell fromCell,int range) {
		List<HexCell> cells = GetVisibleCells(fromCell, range);
		for (int i = 0; i < cells.Count; i++) {
			cells[i].DecreaseVisibility();
		}
		ListPool<HexCell>.Add(cells);
	}

	/// <summary>
	/// Goes through each cell and resets their visibility. Once complete it re adds visibility for all units that are on the map
	/// </summary>
	public void ResetVisibility() {
		for(int i = 0; i < cells.Length; i++) {
			cells[i].ResetVisibility();
		}
		for(int i = 0; i < units.Count; i++) {
			HexUnit unit = units[i];
			IncreaseVisibility(unit.Location, unit.VisionRange);
		}
	}

	public void CenterMap(float xPosition) {
		int centerColumnIndex = (int)
			(xPosition / (HexMetrics.innerDiameter * HexMetrics.chunkSizeX));

		if (centerColumnIndex == currentCenterColumnIndex) {
			return;
		}
		currentCenterColumnIndex = centerColumnIndex;

		int minColumnIndex = centerColumnIndex - chunkCountX / 2;
		int maxColumnIndex = centerColumnIndex + chunkCountX / 2;

		Vector3 position;
		position.y = position.z = 0f;
		for (int i = 0; i < columns.Length; i++) {
			if (i < minColumnIndex) {
				position.x = chunkCountX *
					(HexMetrics.innerDiameter * HexMetrics.chunkSizeX);
			}
			else if (i > maxColumnIndex) {
				position.x = chunkCountX *
					-(HexMetrics.innerDiameter * HexMetrics.chunkSizeX);
			}
			else {
				position.x = 0f;
			}
			columns[i].localPosition = position;
		}
	}

}