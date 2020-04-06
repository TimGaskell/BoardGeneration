using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{

    public HexGrid grid;
    int cellCount, landCells;

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

	[Range(0, 20)]
	public int riverPercentage = 10;

	[Range(0f, 1f)]
	public float extraLakeProbabiliity = 0.25f;

	[Range(0f, 1f)]
	public float lowTemperature = 0f;

	[Range(0f, 1f)]
	public float highTemperature = 0f;

	[Range(0f, 1f)]
	public float temperatureJitter = 0.1f;

	int temperatureJitterChannel;

	public enum HemisphereMode {
		Both,North,South
	}

	public HemisphereMode hemisphere;

	HexCellPriorityQueue searchFrontier;
    int searchFrontierPhase;

	struct MapRegion {
		public int xMin, xMax, zMin, zMax;
	}

	struct ClimateData {
		public float clouds, moisture;
	}

	struct Biome {
		public int terrain, plant;

		public Biome(int terrain, int plant) {
			this.terrain = terrain;
			this.plant = plant;
		}
	}

	List<MapRegion> regions;

	List<ClimateData> climate = new List<ClimateData>();
	List<ClimateData> nextClimate = new List<ClimateData>();
	List<HexDirection> flowDirections = new List<HexDirection>();

	static float[] temperatureBands = { 0.1f, 0.3f, 0.6f };

	static float[] moistureBands = { 0.12f, 0.28f, 0.85f };

	//x axis = moisture, y axis = temperature ,  x= terrain type  y = plant level
	static Biome[] biomes = {
		new Biome(0, 0), new Biome(4, 0), new Biome(4, 0), new Biome(4, 0),
		new Biome(0, 0), new Biome(2, 0), new Biome(2, 1), new Biome(2, 2),
		new Biome(0, 0), new Biome(1, 0), new Biome(1, 1), new Biome(1, 2),
		new Biome(0, 0), new Biome(1, 1), new Biome(1, 2), new Biome(1, 3)
	};


	/// <summary>
	/// Creates a randomly generated map based on the size of the map and the values assigned to the different aspects of the map. Contains a seed for what random generator it uses. Wont create the same map if the sliders are changed to that what they were previously
	/// </summary>
	/// <param name="x"> X size of map </param>
	/// <param name="z"> Z size of map </param>
	public void GenerateMap(int x, int z, bool wrapping) {
		Random.State originalRandomState = Random.state;
		if (!useFixedSeed) {
			seed = Random.Range(0, int.MaxValue);
			seed ^= (int)System.DateTime.Now.Ticks;
			seed ^= (int)Time.unscaledTime;
			seed &= int.MaxValue;
		}
		Random.InitState(seed);

		cellCount = x * z;
		grid.CreateMap(x, z, wrapping);
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
		CreateRivers();
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

		int borderX = grid.wrapping ? regionBorder : mapBorderX;
		MapRegion region;
		switch (regionCount) {
			default:
				if (grid.wrapping) {
					borderX = 0;
				}
				region.xMin = borderX;
				region.xMax = grid.cellCountX -borderX;
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
					if (grid.wrapping) {
						borderX = 0;
					}
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
		landCells = landBudget;
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
			landCells -= landBudget;
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
	/// Function responsible for generating rivers onto the map. It adds every cell into a list, adding extra copies if the cell has a high moisture rating and elevation. These will be river origin points
	/// The function then attempts to draw a number of rivers based on the percentage of the map rated to have rivers.
	/// If river origins are too close or are underwater then it wont try to create a river on that hex.
	/// </summary>
	void CreateRivers() {
		List<HexCell> riverOrigins = ListPool<HexCell>.Get();
		for (int i = 0; i < cellCount; i++) {
			HexCell cell = grid.GetCell(i);
			if (cell.isUnderwater) {
				continue;
			}
			ClimateData data = climate[i];
			float weight = data.moisture * (cell.Elevation - waterLevel) / (elevationMaximum - waterLevel);

			if (weight > 0.75f) {
				riverOrigins.Add(cell);
				riverOrigins.Add(cell);
			}
			if (weight > 0.5f) {
				riverOrigins.Add(cell);
			}
			if (weight > 0.25f) {
				riverOrigins.Add(cell);
			}
		}

		int riverBudget = Mathf.RoundToInt(landCells * riverPercentage * 0.01f);

		while(riverBudget > 0 && riverOrigins.Count > 0) {
			int index = Random.Range(0, riverOrigins.Count);
			int lastIndex = riverOrigins.Count - 1;
			HexCell origin = riverOrigins[index];
			riverOrigins[index] = riverOrigins[lastIndex];
			riverOrigins.RemoveAt(lastIndex);

			if (!origin.HasRiver) {
				bool isValidOrigin = true;
				for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
					HexCell neighbor = origin.GetNeighbor(d);
					if (neighbor && (neighbor.HasRiver || neighbor.isUnderwater)) {
						isValidOrigin = false;
						break;
					}
				}
				if (isValidOrigin) {
					riverBudget -= CreateRiver(origin);
				}
			}
		}

		if(riverBudget > 0) {
			Debug.LogWarning("Failed to use up river budget.");
		}
		ListPool<HexCell>.Add(riverOrigins);
	}

	/// <summary>
	/// Creates the segments of the river flowing through multiple cells. There are some specifications to creating a river:
	/// - The cell a river is joined onto must be level or a lower elevation
	/// - River cannot loop onto itself.
	/// - River will priorities cells that aren't directly next to itself (Harsh turns on the river)
	/// - If it runs into another river origin then it will join the two rivers together.
	/// - If the surrounding cells of the river end are higher than the current, it will create lake
	/// - There is a random chance of lakes being created along the river if the cell is of lower elevation
	/// </summary>
	/// <param name="origin"> Origin cell of river </param>
	/// <returns> Amount of cells used in river creation </returns>
	int CreateRiver(HexCell origin) {
		int length = 1;
		HexCell cell = origin;
		HexDirection direction = HexDirection.NE;
		while (!cell.isUnderwater) {
			int minNeighborElevation = int.MaxValue;
			flowDirections.Clear();
			for(HexDirection d = HexDirection.NE; d<= HexDirection.NW; d++) {
				HexCell neighbor = cell.GetNeighbor(d);

				if (!neighbor) {
					continue;
				}
				if (neighbor.Elevation < minNeighborElevation) {
					minNeighborElevation = neighbor.Elevation;
				}

				if (neighbor == origin || neighbor.HasIncomingRiver) {
					continue;
				}
				int delta = neighbor.Elevation - cell.Elevation;
				if(delta > 0) {
					continue;
				}
				if(delta < 0) { //Increases chance of river choosing a cell which goes downhill
					flowDirections.Add(d);
					flowDirections.Add(d);
					flowDirections.Add(d);
				}
				if(length == 1 || (d != direction.Next2() && d != direction.Previous2())) { //Increases chance of river choosing cell that has a gentler turn 
					flowDirections.Add(d);
				}
				if (neighbor.HasOutgoingRiver) { // If river runs into another origin river, join the rivers together. Early Return
					cell.SetOutgoingRiver(d);
					return length;
				}


				flowDirections.Add(d);
			}
			if(flowDirections.Count == 0) {
				if(length == 1) {
					return 0;
				}

				if(minNeighborElevation >= cell.Elevation) {
					cell.WaterLevel = minNeighborElevation;
					if(minNeighborElevation == cell.Elevation) {
						cell.Elevation = minNeighborElevation - 1;
					}
				}
				break;
			}

			direction= flowDirections[Random.Range(0, flowDirections.Count)];
			cell.SetOutgoingRiver(direction);
			length += 1;

			if(minNeighborElevation >= cell.Elevation && Random.value < extraLakeProbabiliity) {
				cell.WaterLevel = cell.Elevation;
				cell.Elevation -= 1;
			}

			cell = cell.GetNeighbor(direction);
		}
		return length;
	}

	/// <summary>
	/// Determines the temperature of cells based off of latitude and cell height. This changes based on hemisphere mode, since the equator changes  
	/// </summary>
	/// <param name="cell"> Current cell </param>
	/// <returns> Temperature of cell </returns>
	float DetermineTemperature(HexCell cell) {
		float latitude = (float)cell.coordinates.Z / grid.cellCountZ;
		if(hemisphere == HemisphereMode.Both) {
			latitude *= 2;
			if(latitude > 1f) {
				latitude = 2f - latitude;
			}
		}
		else if(hemisphere == HemisphereMode.North) {
			latitude = 1f - latitude;
		}
		float temperature = Mathf.LerpUnclamped(lowTemperature, highTemperature, latitude);

		temperature *= 1f - (cell.ViewElevation - waterLevel) / (elevationMaximum - waterLevel + 1f); //Makes it colder based on cells height. Higher means colder

		float jitter = HexMetrics.SampleNoise(cell.Position * 0.1f)[temperatureJitterChannel];
		temperature += (jitter * 2f - 1f) * temperatureJitter; //Adds randomness to a cells temperature. Displaces it by a little
		return temperature;
	}

	/// <summary>
	/// Sets the terrain type based on its elevation, moisture and temperature. These values are used to determine the biome based on the biome array
	/// </summary>
	void SetTerrainType() {

		temperatureJitterChannel = Random.Range(0, 4);
		int rockDesertElevation = elevationMaximum - (elevationMaximum - waterLevel) / 2;

		for (int i = 0; i < cellCount; i++) {
			
			HexCell cell = grid.GetCell(i);
			float temperature = DetermineTemperature(cell);
			float moisture = climate[i].moisture;

			if (!cell.isUnderwater) {

				int t = 0;
				for (; t < temperatureBands.Length; t++) { // loops through temperatures to find index temperature falls under
					if (temperature < temperatureBands[t]) {
						break;
					}
				}

				int m = 0;
				for (; m < moistureBands.Length; m++) { // loops through moisture to find index moisture falls under
					if (moisture < moistureBands[m]) {
						break;
					}
				}

				Biome cellBiome = biomes[t * 4 + m];
				if (cellBiome.terrain == 0) {
					if (cell.Elevation >= rockDesertElevation) {
						cellBiome.terrain = 3;
					}
				}
				else if (cell.Elevation == elevationMaximum) {
					cellBiome.terrain = 4;
				}

				if (cellBiome.terrain == 4) {
					cellBiome.plant = 0;
				}
				else if (cellBiome.plant < 3 && cell.HasRiver) {
					cellBiome.plant += 1;
				}

				cell.TerrainTypeIndex = cellBiome.terrain;
				cell.PlantLevel = cellBiome.plant;
			}
			else {
				int terrain;
				if (cell.Elevation == waterLevel - 1) {
					int cliffs = 0, slopes = 0;

					for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
						HexCell neighbor = cell.GetNeighbor(d);
						if (!neighbor) {
							continue;
						}
						int delta = neighbor.Elevation - cell.WaterLevel;
						if (delta == 0) {
							slopes += 1;
						}
						else if (delta > 0) {
							cliffs += 1;
						}
					}

					if (cliffs + slopes > 3) {
						terrain = 1;
					}
					else if (cliffs > 0) {
						terrain = 3;
					}
					else if (slopes > 0) {
						terrain = 0;
					}
					else {
						terrain = 1;
					}

				}
				else if (cell.Elevation >= waterLevel) {
					terrain = 1;
				}
				else if (cell.Elevation < 0) {
					terrain = 3;
				}
				else {
					terrain = 2;
				}

				if (terrain == 1 && temperature < temperatureBands[0]) {
					terrain = 2;
				}
				cell.TerrainTypeIndex = terrain;
			}
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