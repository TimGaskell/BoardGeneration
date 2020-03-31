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

    HexCellPriorityQueue searchFrontier;
    int searchFrontierPhase;


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
		CreateLand();
		SetTerrainType();
		for (int i = 0; i < cellCount; i++) {
			grid.GetCell(i).SearchPhase = 0;
		}

		Random.state = originalRandomState;
	}

	/// <summary>
	/// Function used for creating land masses on the map. Is responsible for raising land up to the percentage of land required in the slider.
	/// Also sinks cells so that they are beneath the water level to give land masses more variety.
	/// </summary>
    void CreateLand() {
		int landBudget = Mathf.RoundToInt(cellCount * landPercentage * 0.01f);
		while (landBudget > 0) {
			int chunkSize = Random.Range(chunkSizeMin, chunkSizeMax - 1);
			if (Random.value < sinkProbability) {
				landBudget = SinkTerrain(chunkSize, landBudget);
			}
			else {
				landBudget = RaiseTerrain(chunkSize, landBudget);
			}
		}
	}

	/// <summary>
	/// Gets a random point on the map and begins to raise terrain around it. Has the possibility to loop over same cells so that their height goes even greater.
	/// It slowly decreases the land budget until there is none left. If a cell is above water level, it reduces the budget. Once budget is empty map is generated.
	/// </summary>
	/// <param name="chunkSize"> Size of land mass being created </param>
	/// <param name="budget"> How much land is allowed in this chunk </param>
	/// <returns> How much land is left to create </returns>
	int RaiseTerrain(int chunkSize, int budget) {
		searchFrontierPhase += 1;
		HexCell firstCell = GetRandomCell();
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
	int SinkTerrain(int chunkSize, int budget) {
		searchFrontierPhase += 1;
		HexCell firstCell = GetRandomCell();
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
	/// Sets the terrain types of all cells to that of the height that they have. Found by looking at the difference between water level and cell elevation.
	/// </summary>
	void SetTerrainType() {
		for (int i = 0; i < cellCount; i++) {
			HexCell cell = grid.GetCell(i);
			if (!cell.isUnderwater) {
				cell.TerrainTypeIndex = cell.Elevation - cell.WaterLevel;
			}
		}
	}

	/// <summary>
	/// Gets a random cell from all cells on map
	/// </summary>
	/// <returns> Random cell on map </returns>
	HexCell GetRandomCell() {
		return grid.GetCell(Random.Range(0, cellCount));
	}
}