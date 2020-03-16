﻿using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour {

	public Color[] colors;

	public HexGrid hexGrid;

	private Color activeColor;
	int activeElevation;
	int activeWaterLevel;
	int activeUrbanLevel, activeFarmLevel, activePlantLevel;
	bool applyColor;
	bool applyElevation = true;
	bool applyUrbanLevel, applyFarmLevel, applyPlantLevel;
	bool applyWaterLevel = true;

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
		SelectColor(0);
	}

	void Update () {
		if (
			Input.GetMouseButton(0) &&
			!EventSystem.current.IsPointerOverGameObject()
		) {
			HandleInput();
		}
		else
		{
			previousCell = null;
		}
	}

	/// <summary>
	/// Handles Input of Mouse button down. If it clicks on a hexagon, it will change its color to the one currently selected
	/// </summary>
	void HandleInput () {
		Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(inputRay, out hit)) {
			HexCell CurrentCell = hexGrid.GetCell(hit.point);

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
		else
		{
			previousCell = null;
		}
	}

	/// <summary>
	/// Edits the cells current color and elevation. Causes the mesh renderer to redraw the mesh. 
	/// </summary>
	/// <param name="cell"> Hex that is going to be changed </param>
	void EditCell(HexCell cell)
	{
		if (cell)
		{
			if (applyColor)
			{
				cell.color = activeColor;
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
	/// Changes active color to that of another in its array
	/// </summary>
	/// <param name="index"> Index of color array </param>
	public void SelectColor (int index) {
		applyColor = index >= 0;
		if (applyColor)
		{
			activeColor = colors[index];
		}		
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
	/// UI toggle for showing label UI elements on the hexes
	/// </summary>
	/// <param name="visible"> Bool for switching it on or off </param>
	public void ShowUI(bool visible)
	{
		hexGrid.ShowUI(visible);
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
}