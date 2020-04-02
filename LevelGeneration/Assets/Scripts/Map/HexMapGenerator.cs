using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{

    public HexGrid grid;
    int cellCount;

	public bool useFixedSeed;
	public int seed;

    [Range(0f, 0.5f)]
    public float jitterProbability = 0.25f;

    [Range(20, 200)]
    public int chunkSizeMin = 30;

    [Range(20, 200)]
    public int chunkSizeMax = 200;

    [Range(5, 95)]
    public int landPercentage = 50;

    [Range(1, 5)]
    public int waterLevel = 3;

	[Range(0f, 1f)]
	public float highRiseProbability = 0.25f;

	[Range(0f, 0.4f)]
	public float sinkProbability = 0.2f;

	[Range(-4, 0)]
	public int elevationMinimum = -2;

	[Range(6, 10)]
	public int elevationMaximum = 8;

	[Range(0, 10)]
	public int mapBorderX = 5;

	[Range(0, 10)]
	public int mapBorderZ = 5;

	[Range(0, 10)]
	public int regionBorder = 5;

	[Range(1, 4)]
	public int regionCount = 1;

	[Range(0, 100)]
	public int erosionPercentage = 50;

	[Range(0f, 1f)]
	public float evaporationFactor = 0.5f;

	[Range(0f, 1f)]
	public float precipitationFactor = 0.25f;

	[Range(0f, 1f)]
	public float runoffFactor = 0.25f;

	[Range(0f, 1f)]
	public float seepageFactor = 0.125f;

	[Range(0f, 1f)]
	public float startingMoisture = 0.1f;

	public HexDirection windDirection = HexDirection.NW;

	[Range(1f, 10f)]
	public float windStrength = 4f;

	HexCellPriorityQueue searchFrontier;
    int searchFrontierPhase;

	struct MapRegion {
		public int xMin, xMax, zMin, zMax;
	}

	struct ClimateData {
		public float clouds, moisture;
	}

	List<MapRegion> regions;

	List<ClimateData> climate = new List<ClimateData>();
	List<ClimateData> nextClimate = new List<ClimateData>();


	/// <summary>
	/// Creates a randomly generated map based on the size of the map and the values assigned to the different aspects of the map. Contains a seed for what random generator it uses. Wont create the same map if the sliders are changed to that what they were previously
	/// </summary>
	/// <param name="x"> X size of map </param>
	/// <param name="z"> Z size of map </param>
	public void GenerateMap(int x, int z) {
		Random.State originalRandomState = Random.state;
		if (!useFixedSeed) {
			seed = Random.Range(0, int.MaxValue);
			seed ^= (int)System.DateTime.Now.Ticks;
			seed ^= (int)Time.unscaledTime;
			seed &= int.MaxValue;
		}
		Random.InitState(seed);

		cellCount = x * z;
		grid.CreateMap(x, z);
		if (searchFrontier == null) {
			searchFrontier = new HexCellPriorityQueue();
		}
		for (int i = 0; i < cellCount; i++) {
			grid.GetCell(i).WaterLevel = waterLevel;
		}

		CreateRegions();
		CreateLand();
		ErodeLand();
		CreateClimate();
		SetTerrainType();
		for (int i = 0; i < cellCount; i++) {
			grid.GetCell(i).SearchPhase = 0;
		}

		Random.state = originalRandomState;
	}

	/// <summary>
	/// Splits the map in 1 - 4 regions depending regionCount. Divides it based on map edge border size, horizontal or vertical devisions and how many there are.
	/// These provide min and max values of X and Z where a land can come above water. Not 100% set boundaries, potential for land masses to join anyway.
	/// </summary>
	void CreateRegions() {
		if (regions == null) {
			regions = new List<MapRegion>();
		}
		else {
			regions.Clear();
		}

		MapRegion region;
		switch (regionCount) {
			default:
				region.xMin = mapBorderX;
				region.xMax = grid.cellCountX - mapBorderX;
				region.zMin = mapBorderZ;
				region.zMax = grid.cellCountZ - mapBorderZ;
				regions.Add(region);
				break;
			case 2:
				if (Random.value < 0.5f) { //vertical split
					region.xMin = mapBorderX;
					region.xMax = grid.cellCountX / 2 - regionBorder;
					region.zMin = mapBorderZ;
					region.zMax = grid.cellCountZ - mapBorderZ;
					regions.Add(region);
					region.xMin = grid.cellCountX / 2 + regionBorder;
					region.xMax = grid.cellCountX - mapBorderX;
					regions.Add(region);
				}
				else { //horizontal split or vertical split
					region.xMin = mapBorderX;
					region.xMax = grid.cellCountX - mapBorderX;
					region.zMin = mapBorderZ;
					region.zMax = grid.cellCountZ / 2 - regionBorder;
					regions.Add(region);
					region.zMin = grid.cellCountZ / 2 + regionBorder;
					region.zMax = grid.cellCountZ - mapBorderZ;
					regions.Add(region);
				}
				break;
			case 3: // 2 vertical splits
				region.xMin = mapBorderX;
				region.xMax = grid.cellCountX / 3 - regionBorder;
				region.zMin = mapBorderZ;
				region.zMax = grid.cellCountZ - mapBorderZ;
				regions.Add(region);
				region.xMin = grid.cellCountX / 3 + regionBorder;
				region.xMax = grid.cellCountX * 2 / 3 - regionBorder;
				regions.Add(region);
				region.xMin = grid.cellCountX * 2 / 3 + regionBorder;
				region.xMax = grid.cellCountX - mapBorderX;
				regions.Add(region);
				break;

			case 4: // 2 vertical splits and a horizontal split
				region.xMin = mapBorderX;
				region.xMax = grid.cellCountX / 2 - regionBorder;
				region.zMin = mapBorderZ;
				region.zMax = grid.cellCountZ / 2 - regionBorder;
				regions.Add(region);
				region.xMin = grid.cellCountX / 2 + regionBorder;
				region.xMax = grid.cellCountX - mapBorderX;
				regions.Add(region);
				region.zMin = grid.cellCountZ / 2 + regionBorder;
				region.zMax = grid.cellCountZ - mapBorderZ;
				regions.Add(region);
				region.xMin = mapBorderX;
				region.xMax = grid.cellCountX / 2 - regionBorder;
				regions.Add(region);
				break;
		}	
	}

	/// <summary>
	/// Function used for creating land masses on the map. Is responsible for raising land up to the percentage of land required in the slider.
	/// Also sinks cells so that they are beneath the water level to give land masses more variety.
	/// </summary>
	void CreateLand() {
		int landBudget = Mathf.RoundToInt(cellCount * landPercentage * 0.01f);
		for (int guard = 0;  guard < 10000; guard++) {
			bool sink = Random.value < sinkProbability;
			for (int i = 0; i < regions.Count; i++) {
				MapRegion region = regions[i];
				int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax - 1);
				if (sink) {
					landBudget = SinkTerrain(chunkSize, landBudget,region);
				}
				else {
					landBudget = RaiseTerrain(chunkSize, landBudget,region);
					if (landBudget == 0) {
						return;
					}
				}
			}
		}
		if (landBudget > 0) {
			Debug.LogWarning("Failed to use up " + landBudget + " land budget.");
		}
	}
	/// <summary>
	/// Gets a random point on the map and begins to raise terrain around it. Has the possibility to loop over same cells so that their height goes even greater.
	/// It slowly decreases the land budget until there is none left. If a cell is above water level, it reduces the budget. Once budget is empty map is generated.
	/// </summary>
	/// <param name="chunkSize"> Size of land mass being created </param>
	/// <param name="budget"> How much land is allowed in this chunk </param>
	/// <returns> How much land is left to create </returns>
	int RaiseTerrain(int chunkSize, int budget, MapRegion region) {
		searchFrontierPhase += 1;
		HexCell firstCell = GetRandomCell(region);
		firstCell.SearchPhase = searchFrontierPhase;
		firstCell.Distance = 0;
		firstCell.SearchHeuristic = 0;
		searchFrontier.Enqueue(firstCell);
		HexCoordinates center = firstCell.coordinates;

		int rise = Random.value < highRiseProbability ? 2 : 1;
		int size = 0;
		while (size < chunkSize && searchFrontier.Count > 0) {
			HexCell current = searchFrontier.Dequeue();
			int originalElevation = current.Elevation;
			int newElevation = originalElevation + rise;
			if (newElevation > elevationMaximum) {
				continue;
			}
			current.Elevation = newElevation;
			if (
				originalElevation < waterLevel &&
				newElevation >= waterLevel && --budget == 0
			) {
				break;
			}
			size += 1;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbor = current.GetNeighbor(d);
				if (neighbor && neighbor.SearchPhase < searchFrontierPhase) {
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = neighbor.coordinates.DistanceTo(center);
					neighbor.SearchHeuristic =
						Random.value < jitterProbability ? 1 : 0;
					searchFrontier.Enqueue(neighbor);
				}
			}
		}
		searchFrontier.Clear();
		return budget;
	}

	/// <summary>
	/// Gets random point on the map and begins to decrease its terrain height around it. Has the possibility to loop over same cells and reduce their heights even greater.
	/// Slowly increases the land budget once the land cell is bellow water level. 
	/// </summary>
	/// <param name="chunkSize"> Size of land mass being created </param>
	/// <param name="budget"> How much land is allowed in this chunk </param>
	/// <returns> How much land is left to create </returns>
	int SinkTerrain(int chunkSize, int budget, MapRegion region) {
		searchFrontierPhase += 1;
		HexCell firstCell = GetRandomCell(region);
		firstCell.SearchPhase = searchFrontierPhase;
		firstCell.Distance = 0;
		firstCell.SearchHeuristic = 0;
		searchFrontier.Enqueue(firstCell);
		HexCoordinates center = firstCell.coordinates;

		int sink = Random.value < highRiseProbability ? 2 : 1;
		int size = 0;
		while (size < chunkSize && searchFrontier.Count > 0) {
			HexCell current = searchFrontier.Dequeue();
			int originalElevation = current.Elevation;
			int newElevation = current.Elevation - sink;
			if (newElevation < elevationMinimum) {
				continue;
			}
			current.Elevation = newElevation;
			if (
				originalElevation >= waterLevel &&
				newElevation < waterLevel
			) {
				budget += 1;
			}
			size += 1;

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbor = current.GetNeighbor(d);
				if (neighbor && neighbor.SearchPhase < searchFrontierPhase) {
					neighbor.SearchPhase = searchFrontierPhase;
					neighbor.Distance = neighbor.coordinates.DistanceTo(center);
					neighbor.SearchHeuristic =
						Random.value < jitterProbability ? 1 : 0;
					searchFrontier.Enqueue(neighbor);
				}
			}
		}
		searchFrontier.Clear();
		return budget;
	}

	/// <summary>
	/// Erodes the land so that cliffs are lowered, displacing their height to lower parts of the land mass. This provides a smoother land mass in general.
	/// The amount of cliffs lowered is dependent on the erodible percentage set. This percentage represents how many cliffs will be eroded.
	/// </summary>
	void ErodeLand() {
		List<HexCell> erodibleCells = ListPool<HexCell>.Get();
		for(int i = 0; i < cellCount; i++) {
			HexCell cell = grid.GetCell(i);
			if (IsErodible(cell)) {
				erodibleCells.Add(cell);
			}
		}
		int targetErodibleCount = (int)(erodibleCells.Count * (100 - erosionPercentage) * 0.01f);

		while (erodibleCells.Count > targetErodibleCount) {
			int index = Random.Range(0, erodibleCells.Count);
			HexCell cell = erodibleCells[index];
			HexCell targetCell = GetErosionTarget(cell);
			cell.Elevation -= 1;
			targetCell.Elevation += 1;
			if (!IsErodible(cell)) {
				erodibleCells[index] = erodibleCells[erodibleCells.Count - 1];
				erodibleCells.RemoveAt(erodibleCells.Count - 1);
			}

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbor = cell.GetNeighbor(d);
				if (neighbor && neighbor.Elevation == cell.Elevation + 2 && !erodibleCells.Contains(neighbor)) {
					erodibleCells.Add(neighbor);
				}
			}

			if (IsErodible(targetCell) && !erodibleCells.Contains(targetCell)) {
				erodibleCells.Add(targetCell);
			}

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
				HexCell neighbor = targetCell.GetNeighbor(d);
				if (neighbor && neighbor != cell &&  neighbor.Elevation == targetCell.Elevation + 1&&!IsErodible(neighbor)) {
					erodibleCells.Remove(neighbor);
				}
			}
		}
		ListPool<HexCell>.Add(erodibleCells);
	}

	/// <summary>
	/// Determines if the current cell is 2 elevations above its neighbors. If so, the cell can be eroded in the generation of the map.
	/// </summary>
	/// <param name="cell"> Current Cell </param>
	/// <returns> If the cell is erodible </returns>
	bool IsErodible(HexCell cell) {
		int erodibleElevation = cell.Elevation - 2;
		for(HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
			HexCell neighbor = cell.GetNeighbor(d);
			if(neighbor && neighbor.Elevation <= erodibleElevation) {
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Determines how many cell neighbors and lower than the current cell. These cells are added to a list and a random one is chosen from the list.
	/// This is used to represent a cell that will have its elevation raised whilst the current cell falls. Distributes heights better.
	/// </summary>
	/// <param name="cell"> Current cell </param>
	/// <returns> Hex cell to have elevation raised </returns>
	HexCell GetErosionTarget(HexCell cell) {
		List<HexCell> candidates = ListPool<HexCell>.Get();
		int erodibleElevation = cell.Elevation - 2;
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
			HexCell neighbor = cell.GetNeighbor(d);
			if (neighbor && neighbor.Elevation <= erodibleElevation) {
				candidates.Add(neighbor);
			}
		}
		HexCell target = candidates[Random.Range(0, candidates.Count)];
		ListPool<HexCell>.Add(candidates);
		return target;

	}

	/// <summary>
	/// Creates lists of climates for each cell on the map. Each cell is looped through multiple times to simulate a water cycle, producing clouds and moisture.
	/// </summary>
	void CreateClimate() {
		climate.Clear();
		nextClimate.Clear();
		ClimateData initialData = new ClimateData();
		initialData.moisture = startingMoisture;
		ClimateData clearData = new ClimateData();

		for(int i =0; i < cellCount; i++) {
			climate.Add(initialData);
			nextClimate.Add(clearData);
		}

		for (int cycle = 0; cycle < 40; cycle++) {
			for (int i = 0; i < cellCount; i++) {
				EvolveClimate(i);
			}
			List<ClimateData> swap = climate;
			climate = nextClimate;
			nextClimate = swap;
		}
	}

	/// <summary>
	/// Determines the moisture and clouds for a given hex.There are multiple factors that play into getting the final moisture of a cell:
	/// - Cells underwater will have maximum moisture
	/// - Cells gain clouds based on moisture in the cell. This lessens the moisture on the cell
	/// - Clouds then precipitate and lesson their amount. Moisture is then increased.
	/// - Cells at higher elevation can't hold as much cloud and will let out any excess cloud as moisture onto the cell.
	/// - Clouds are then dispersed in every direction but magnified in a specified wind direction.
	///  - Moisture either spreads itself to adjacent level terrain or lower terrain, increasing neighbor hexes moisture.
	///  This produces an end moisture level for a cell which will determine the biome it belongs to.
	/// </summary>
	/// <param name="cellIndex"> Current cell index </param>
	void EvolveClimate (int cellIndex) {
		HexCell cell = grid.GetCell(cellIndex);
		ClimateData cellClimate = climate[cellIndex];

		if (cell.isUnderwater) {
			cellClimate.moisture = 1f;
			cellClimate.clouds += evaporationFactor;
		}
		else {
			float evaporation = cellClimate.moisture * evaporationFactor;
			cellClimate.moisture -= evaporation;
			cellClimate.clouds += evaporation;
		}

		float precipitation = cellClimate.clouds * precipitationFactor;
		cellClimate.clouds -= precipitation;
		cellClimate.moisture += precipitation;

		float cloudMaximum = 1f - cell.ViewElevation / (elevationMaximum + 1f);
		if(cellClimate.clouds > cloudMaximum) {
			cellClimate.moisture += cellClimate.clouds - cloudMaximum;
			cellClimate.clouds = cloudMaximum;
		}

		HexDirection mainDispersalDirection = windDirection.Opposite();
		float cloudDispersal = cellClimate.clouds * (1f / (5f + windStrength));
		float runOff = cellClimate.moisture * runoffFactor * (1f / 6f);

		float seepage = cellClimate.moisture * seepageFactor * (1f / 6f);

		for(HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
			HexCell neighbor = cell.GetNeighbor(d);
			if (!neighbor) {
				continue;
			}

			ClimateData neighborClimate = nextClimate[neighbor.Index];
			if(d == mainDispersalDirection) {
				neighborClimate.clouds += cloudDispersal * windStrength;
			}
			else {
				neighborClimate.clouds += cloudDispersal;
			}

			int elevationDelta = neighbor.ViewElevation - cell.ViewElevation;
			if(elevationDelta < 0) {
				cellClimate.moisture -= runOff;
				neighborClimate.moisture += runOff;
			}
			else if( elevationDelta == 0) {
				cellClimate.moisture -= seepage;
				neighborClimate.moisture += seepage;
			}


			nextClimate[neighbor.Index] = neighborClimate;
		}
		ClimateData nextCellClimate = nextClimate[cellIndex];
		nextCellClimate.moisture += cellClimate.moisture;
		if(nextCellClimate.moisture > 1f) {
			nextCellClimate.moisture = 1f;
		}
		nextClimate[cellIndex] = nextCellClimate;
		climate[cellIndex] = new ClimateData();
	}

	/// <summary>
	/// Sets the terrain types of all cells based on the amount of moisture in the cell.
	/// </summary>
	void SetTerrainType() {
		for (int i = 0; i < cellCount; i++) {
			HexCell cell = grid.GetCell(i);
			float moisture = climate[i].moisture;
			if (!cell.isUnderwater) {
				if(moisture < 0.05f) {
					cell.TerrainTypeIndex = 4;
				}
				else if( moisture < 0.12f) {
					cell.TerrainTypeIndex = 0;
				}
				else if(moisture < 0.28f) {
					cell.TerrainTypeIndex = 3;
				}
				else if(moisture < 0.85f) {
					cell.TerrainTypeIndex = 1;
				}
				else {
					cell.TerrainTypeIndex = 2;
				}
			}
			else {
				cell.TerrainTypeIndex = 2;
			}
			cell.SetMapData(moisture);
		}
	}


	/// <summary>
	/// Gets a random cell from all cells on map
	/// </summary>
	/// <returns> Random cell on map </returns>
	HexCell GetRandomCell(MapRegion region) {
		return grid.GetCell(Random.Range(region.xMin, region.xMax),Random.Range(region.zMin, region.zMax));
	}
}