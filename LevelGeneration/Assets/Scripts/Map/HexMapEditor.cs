﻿using UnityEngine;
using UnityEngine.EventSystems;
using System.IO;

public class HexMapEditor : MonoBehaviour {

	public HexGrid hexGrid;

	public Material terrainMaterial;

	bool editMode;

	int activeElevation;
	int activeWaterLevel;
	int activeUrbanLevel, activeFarmLevel, activePlantLevel, activeSpecialIndex;

	int activeTerrainTypeIndex;

	bool applyElevation = false;
	bool applyUrbanLevel, applyFarmLevel, applyPlantLevel, applySpecialIndex;
	bool applyWaterLevel = false;

	bool isDrag;
	HexDirection dragDirection;
	HexCell previousCell;

	int brushSize;

	enum OptionalToggle
	{
		Ignore, Yes, No
	}

	OptionalToggle riverMode, roadMode, walledMode;

	void Awake () {

		terrainMaterial.DisableKeyword("GRID_ON");
		Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
		SetEditMode(true);
	}

	void Update () {

		if (!EventSystem.current.IsPointerOverGameObject()) {
			if (Input.GetMouseButton(0)) {
				HandleInput();
				return;
			}
			if (Input.GetKeyDown(KeyCode.U)) {
				if (Input.GetKey(KeyCode.LeftShift)) {
					DestroyUnit();
				}
				else {
					CreateUnit();
				}
			}
		}
		previousCell = null;
	}

	/// <summary>
	/// Handles Input of Mouse button down. If it clicks on a hexagon, it will change its color to the one currently selected
	/// </summary>
	void HandleInput () {

		HexCell CurrentCell = GetCellUnderCursor();

		if (CurrentCell) { 

			if(previousCell && previousCell != CurrentCell)
			{
				ValidateDrag(CurrentCell);
			}
			else
			{
				isDrag = false;
			}		
			EditCells(CurrentCell);
			
			previousCell = CurrentCell;
		}
		else {
			previousCell = null;
		}
	}

	/// <summary>
	/// Edits the cells current color and elevation. Causes the mesh renderer to redraw the mesh. 
	/// </summary>
	/// <param name="cell"> Hex that is going to be changed </param>
	void EditCell(HexCell cell)
	{
		if (cell) {
			if (activeTerrainTypeIndex >= 0) {
				cell.TerrainTypeIndex = activeTerrainTypeIndex;
			}
			if (applyElevation)
			{
				cell.Elevation = activeElevation;
			}
			if (applyWaterLevel)
			{
				cell.WaterLevel = activeWaterLevel;
			}
			if (applyUrbanLevel) {
				cell.UrbanLevel = activeUrbanLevel;
			}
			if (applyFarmLevel) {
				cell.FarmLevel = activeFarmLevel;
			}
			if (applyPlantLevel) {
				cell.PlantLevel = activePlantLevel;
			}
			if (applySpecialIndex) {
				cell.SpecialIndex = activeSpecialIndex;
			}
			if (riverMode == OptionalToggle.No)
			{
				cell.RemoveRiver();
			}
			if(roadMode == OptionalToggle.No)
			{
				cell.RemoveRoads();
			}
			if(walledMode != OptionalToggle.Ignore) {
				cell.Walled = walledMode == OptionalToggle.Yes;
			}
			if (isDrag)
			{
				HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
				if (otherCell)
				{
					if(riverMode == OptionalToggle.Yes)
					{
						otherCell.SetOutgoingRiver(dragDirection);
					}
					if(roadMode == OptionalToggle.Yes)
					{
						otherCell.AddRoad(dragDirection);
					}
				}
			}
			
		}
	}

	/// <summary>
	/// Used for editing multiple cells at a time. Determines its center and edits each individual cell in a radius to that center depending on the brush size
	/// </summary>
	/// <param name="center"> Middle Cell for the start </param>
	void EditCells(HexCell center)
	{
		int centerX = center.coordinates.X;
		int centerZ = center.coordinates.Z;

		for(int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++)
		{
			for(int x = centerX - r; x <= centerX + brushSize; x++)
			{
				EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
			}

		}
		for(int r =0, z= centerZ + brushSize; z > centerZ; z--, r++)
		{
			for(int x = centerX - brushSize; x <= centerX + r; x++)
			{
				EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
			}
		}
	}

	/// <summary>
	/// UI toggle for allowing Elevation changes in the level
	/// </summary>
	/// <param name="toggle"> Bool for switching it on and off </param>
	public void SetApplyElevation(bool toggle)
	{
		applyElevation = toggle;
	}

	/// <summary>
	/// Used by the UI to set the elevation that will be used.
	/// </summary>
	/// <param name="elevation"> Height for which the cell will become </param>
	public void SetElevation(float elevation)
	{
		activeElevation = (int)elevation;
	}

	/// <summary>
	/// UI setter for changing the size of the brush being used
	/// </summary>
	/// <param name="size"> Size of brush </param>
	public void SetBrushSize(float size)
	{
		brushSize = (int)size;
	}

	/// <summary>
	/// Sets whether to draw rivers or not
	/// </summary>
	/// <param name="mode"> On, off or ignore </param>
	public void SetRiverMode (int mode)
	{
		riverMode = (OptionalToggle)mode;
	}

	/// <summary>
	/// Sets where roads are to be drawn or not
	/// </summary>
	/// <param name="mode">  On, off or ignore </param>
	public void SetRoadMode (int mode)
	{
		roadMode = (OptionalToggle)mode;
	}

	/// <summary>
	/// Used for determining whether the use is currently dragging their mouse through multiple cells
	/// </summary>
	/// <param name="currentCell"> Initial Hex the drag started from </param>
	void ValidateDrag(HexCell currentCell)
	{
		for(dragDirection = HexDirection.NE; dragDirection <= HexDirection.NW; dragDirection++)
		{
			if(previousCell.GetNeighbor(dragDirection) == currentCell)
			{
				isDrag = true;
				return;
			}
		}
		isDrag = false;
	}

	/// <summary>
	/// UI toggle for changing the water level of a hex
	/// </summary>
	/// <param name="toggle">  On or off </param>
	public void SetApplyWaterLevel (bool toggle)
	{
		applyWaterLevel = toggle;
	}

	/// <summary>
	/// UI slider for changing the active water level of the hex.
	/// </summary>
	/// <param name="level"> Height of water </param>
	public void SetWaterLevel(float level)
	{
		activeWaterLevel = (int)level;
	}

	/// <summary>
	/// UI toggle for changing the urban level of a hex
	/// </summary>
	/// <param name="toggle">  On or off </param>
	public void SetApplyUrbanLevel(bool toggle) {
		applyUrbanLevel = toggle;
	}

	/// <summary>
	/// UI slider for changing the urban level of the hex.
	/// </summary>
	/// <param name="level"> Type of prefabs to choose from in level e.g small, medium , large </param>
	public void SetUrbanLevel(float level) {
		activeUrbanLevel = (int)level;
	}

	/// <summary>
	/// UI toggle for changing the farm level of a hex
	/// </summary>
	/// <param name="toggle">  On or off </param>
	public void SetApplyFarmLevel(bool toggle) {
		applyFarmLevel = toggle;
	}

	/// <summary>
	/// UI slider for changing the farm level of the hex.
	/// </summary>
	/// <param name="level">  Type of prefabs to choose from in level e.g small, medium , large </param>
	public void SetFarmLevel(float level) {
		activeFarmLevel = (int)level;
	}

	/// <summary>
	/// UI toggle for changing the plant level of a hex
	/// </summary>
	/// <param name="toggle">  On or off </param>
	public void SetApplyPlantLevel (bool toggle) {
		applyPlantLevel = toggle;
	}

	/// <summary>
	/// UI slider for changing the plant level of the hex.
	/// </summary>
	/// <param name="level"> Type of prefabs to choose from in level e.g small, medium , large </param>
	public void SetPlantLevel(float level) {
		activePlantLevel = (int)level;
	}

	/// <summary>
	/// UI toggle for allowing walls to be drawn on a hex
	/// </summary>
	/// <param name="mode">  On, off or ignore  </param>
	public void SetWalledMode (int mode) {
		walledMode = (OptionalToggle)mode;
	}

	/// <summary>
	/// UI toggle for allowing special building to be created 
	/// </summary>
	/// <param name="toggle"> On or off </param>
	public void SetApplySpecialIndex (bool toggle) {
		applySpecialIndex = toggle;
	}

	/// <summary>
	/// UI slider for changing the index of which special building to create
	/// </summary>
	/// <param name="index"> index of special array </param>
	public void SetSpecialIndex(float index) {
		activeSpecialIndex = (int)index;
	}

	/// <summary>
	/// UI element for setting the type of terrain that is currently selected.
	/// </summary>
	/// <param name="index"> Index value for terrain type of cell </param>
	public void SetTerrainTypeIndex(int index) {
		activeTerrainTypeIndex = index;
	}

	/// <summary>
	/// UI element that enables or disables a hex outline of each cell.
	/// </summary>
	/// <param name="visible"> bool value of on or off </param>
	public void ShowGrid(bool visible) {
		if (visible) {
			terrainMaterial.EnableKeyword("GRID_ON");
		}
		else {
			terrainMaterial.DisableKeyword("GRID_ON");
		}
	}
	/// <summary>
	/// UI element that toggles the user can edit the map and displays the text distance of all cells.
	/// </summary>
	/// <param name="toggle"> bool value of on or off </param>
	public void SetEditMode(bool toggle) {
		enabled = toggle;
	}

	/// <summary>
	/// Returns the current Hex under the cursor
	/// </summary>
	/// <returns> Hex cell under cursor </returns>
	HexCell GetCellUnderCursor() {
		return hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
	}

	/// <summary>
	/// Adds and creates a unit on a hex if there currently isn't one already on the  same hex. 
	/// </summary>
	void CreateUnit() {
		HexCell cell = GetCellUnderCursor();
		if (cell && !cell.Unit) {
			hexGrid.AddUnit(Instantiate(HexUnit.unitPrefab), cell, Random.Range(0f, 360f));
		}
	}

	/// <summary>
	/// Destroys the unit that is currently under the mouse position
	/// </summary>
	void DestroyUnit() {
		HexCell cell = GetCellUnderCursor();
		if(cell && cell.Unit) {
			hexGrid.RemoveUnit(cell.Unit);
		}
	}


	
}