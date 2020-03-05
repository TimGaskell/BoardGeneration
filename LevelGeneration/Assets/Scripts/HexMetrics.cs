using UnityEngine;

public static class HexMetrics {

	public const float outerRadius = 10f;

	public const float innerRadius = outerRadius * 0.866025404f;

	public const float solidFactor = 0.75f;

	public const float blendFactor = 1f - solidFactor;


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
}