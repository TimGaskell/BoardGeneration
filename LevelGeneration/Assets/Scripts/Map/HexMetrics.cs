using UnityEngine;

public enum HexEdgeType
{
	Flat, Slope, Cliff
}

public static class HexMetrics {

	public const float outerToInner = 0.866025404f;

	public const float innerToOuter = 1f / outerToInner;

	public const float outerRadius = 10f;

	public const float innerRadius = outerRadius * outerToInner;

	public const float solidFactor = 0.8f;

	public const float blendFactor = 1f - solidFactor;

	public const float elevationStep = 3f;

	public const int terracesPerSlope = 2;

	public const int terraceSteps = terracesPerSlope * 2 + 1;

	public const float horizontalTerraceStepSize = 1f / terraceSteps;

	public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);

	public static Texture2D noiseSource;

	public const float cellPerturbStrength = 4f;

	public const float elevationPerturbStrength = 1.5f;

	public const float noiseScale = 0.003f;

	public const int chunkSizeX = 5, chunkSizeZ = 5;

	public const float streamBedElevationOffset = -1.75f;

	public const float waterElevationOffset = -0.5f;

	public const float waterFactor = 0.6f;

	public const float waterBlendFactor = 1f - waterFactor;


	/// <summary>
	/// Defined corners of a hexagon for which the triangles can be drawn upon
	/// </summary>
	static Vector3[] corners = {
		new Vector3(0f, 0f, outerRadius),
		new Vector3(innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(0f, 0f, -outerRadius),
		new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(0f, 0f, outerRadius)
	};

	/// <summary>
	/// Grabs the first corner associated with the direction
	/// </summary>
	/// <param name="direction"> Direction of the hexagon vertex e.g. NE, SE etc.</param>
	/// <returns> Vector3 of Corner </returns>
	public static Vector3 GetFirstCorner (HexDirection direction) {
		return corners[(int)direction];
	}

	/// <summary>
	/// Grabs the second corner associated with the direction
	/// </summary>
	/// <param name="direction"> Direction of the hexagon vertex e.g. NE, SE etc </param>
	/// <returns> Vector3 of Corner </returns>
	public static Vector3 GetSecondCorner (HexDirection direction) {
		return corners[(int)direction + 1];
	}

	/// <summary>
	/// First inside corner of the hexagon where the color will not be blended 
	/// </summary>
	/// <param name="direction"> Direction of the hexagon vertex e.g. NE, SE etc </param>
	/// <returns> Vector3 of Corner </returns>
	public static Vector3 GetFirstSolidCorner (HexDirection direction) {
		return corners[(int)direction] * solidFactor;
	}

	/// <summary>
	/// Second inside corner of the hexagon where the color will not be blended 
	/// </summary>
	/// <param name="direction"> Direction of the hexagon vertex e.g. NE, SE etc </param>
	/// <returns> Vector3 of Corner </returns>
	public static Vector3 GetSecondSolidCorner (HexDirection direction) {
		return corners[(int)direction + 1] * solidFactor;
	}

	/// <summary>
	/// Gets the connecting area between two hexes
	/// </summary>
	/// <param name="direction">  Direction of the hexagon vertex e.g. NE, SE etc </param>
	/// <returns> Vector3 of bridge location </returns>
	public static Vector3 GetBridge (HexDirection direction) {
		return (corners[(int)direction] + corners[(int)direction + 1]) *
			blendFactor;
	}

	/// <summary>
	/// Used to create vector locations for where a step might be drawn. Having it only increment its height if its an odd step.
	/// </summary>
	/// <param name="a"> Starting Vector </param>
	/// <param name="b"> Ending Vector </param>
	/// <param name="step"> Current Terrace Step </param>
	/// <returns> Vector3 location for the position of the next step </returns>
	public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
	{
		float h = step * HexMetrics.horizontalTerraceStepSize;
		a.x += (b.x - a.x) * h;
		a.z += (b.z - a.z) * h;
		float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
		a.y += (b.y - a.y) * v;
		return a;
	}

	/// <summary>
	/// Creates a color that is a blend between to two Colors based on size of the step and current step.
	/// </summary>
	/// <param name="a"> Bottom Color </param>
	/// <param name="b"> Top Color</param>
	/// <param name="step"> Current Terrace Step </param>
	/// <returns> Blended Color for the Terrace Step </returns>
	public static Color TerraceLerp(Color a, Color b, int step)
	{
		float h = step * HexMetrics.horizontalTerraceStepSize;
		return Color.Lerp(a, b, h);
	}

	/// <summary>
	/// Based on the elevations between two cells, it will return the step relationship between them.
	/// </summary>
	/// <param name="elevation1"> Elevation of Hex 1 </param>
	/// <param name="elevation2"> Elevation of Hex 2 </param>
	/// <returns> Hex edge type between the two Hexes </returns>
	public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
	{
		if(elevation1 == elevation2)
		{
			return HexEdgeType.Flat;
		}
		int delta = elevation2 - elevation1;
		if(delta == 1 || delta == -1)
		{
			return HexEdgeType.Slope;
		}
		return HexEdgeType.Cliff;
	}

	/// <summary>
	/// Samples the noise Texture to produce X and Z vectors for extruding the hexagons.
	/// </summary>
	/// <param name="Position"> World Coordinates </param>
	/// <returns> Vector4 with changed X and Z values for scaling </returns>
	public static Vector4 SampleNoise(Vector3 Position)
	{
		return noiseSource.GetPixelBilinear(Position.x * noiseScale, Position.z * noiseScale);
	}

	/// <summary>
	/// Compares Two corner values of a hex going in its current direction and the next direction after it. This  combined it taking into account the solid factor, locates a vector3 middle edge where for the direction to meet up
	/// </summary>
	/// <param name="direction"> Direction of the hexagon vertex e.g. NE, SE etc </param>
	/// <returns> A middle edge between two hexes depending on its direction </returns>
	public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
	{
		return (corners[(int)direction] + corners[(int)direction + 1]) * (0.5f * solidFactor);
	}


	/// <summary>
	/// Samples the noise texture to determine how much to modify the x and z values on a vector.
	/// Used for modifying triangles and bridge positions.
	/// </summary>
	/// <param name="position"> Vector3 position for point where a triangle will be drawn</param>
	/// <returns> new vector3 with perturbed x and z values </returns>
	public static Vector3 Perturb(Vector3 position)
	{
		Vector4 sample = SampleNoise(position);
		position.x += (sample.x * 2f - 1f) * cellPerturbStrength;
		position.z += (sample.z * 2f - 1f) * cellPerturbStrength;
		return position;
	}

	/// <summary>
	/// Gets first inside corner of the hexagon, proportionate by how much water is to be scaled 
	/// </summary>
	/// <param name="direction"> Direction of the hexagon vertex e.g. NE, SE etc</param>
	/// <returns> vector 3 position of the first point for a water triangle in that direction </returns>
	public static Vector3 GetFirstWaterCorner (HexDirection direction)
	{
		return corners[(int)direction] * waterFactor;
	}
	/// <summary>
	/// Gets second inside corner of the hexagon, proportionate by how much water is to be scaled 
	/// </summary>
	/// <param name="direction"> Direction of the hexagon vertex e.g. NE, SE etc</param>
	/// <returns> vector 3 position of the second point for a water triangle in that direction </returns>
	public static Vector3 GetSecondWaterCorner(HexDirection direction)
	{
		return corners[(int)direction + 1] * waterFactor;
	}

	/// <summary>
	/// Gets the connecting area between two hexes
	/// </summary>
	/// <param name="direction">  Direction of the hexagon vertex e.g. NE, SE etc </param>
	/// <returns> Vector3 of bridge location </returns>
	public static Vector3 GetWaterBridge(HexDirection direction)
	{
		return (corners[(int)direction] + corners[(int)direction + 1]) * waterBlendFactor;
	}
}