using UnityEngine;
using System.Collections;
using System.IO;

[System.Serializable]
public struct HexCoordinates {

	[SerializeField]
	private int x, z;

	public int X {
		get {
			return x;
		}
	}

	public int Z {
		get {
			return z;
		}
	}

	public int Y {
		get {
			return -X - Z;
		}
	}

	/// <summary>
	/// Constructor of HexCoordinates. Assigns the x and z values of a hex. If wrapping is enabled, it changes the x and z values to the appropriate amount
	/// </summary>
	/// <param name="x"> X coordinate of Hex </param>
	/// <param name="z"> Z Coordinate of Hex </param>
	public HexCoordinates (int x, int z) {
		if (HexMetrics.Wrapping) {
			int oX = x + z / 2;
			if (oX < 0) {
				x += HexMetrics.wrapSize;
			}
			else if (oX >= HexMetrics.wrapSize) {
				x -= HexMetrics.wrapSize;
			}
		}
		this.x = x;
		this.z = z;
	}
	
	/// <summary>
	/// Alligns Hex positions along the X Axis
	/// </summary>
	/// <param name="x"> X Coordinate of Hex </param>
	/// <param name="z"> Y Coordinate of Hex</param>
	/// <returns> New Hex Coordinates of X and Z </returns>
	public static HexCoordinates FromOffsetCoordinates (int x, int z) {
		return new HexCoordinates(x - z / 2, z);
	}

	/// <summary>
	/// Converts a vector3 input to HexCoordinates to determine which Hex is being interacted with
	/// </summary>
	/// <param name="position"> Vector3 World coordinates</param>
	/// <returns> New Hex Coordinates of X and Z </returns>
	public static HexCoordinates FromPosition (Vector3 position) {
		float x = position.x /  HexMetrics.innerDiameter;
		float y = -x;

		float offset = position.z / (HexMetrics.outerRadius * 3f);
		x -= offset;
		y -= offset;

		int iX = Mathf.RoundToInt(x);
		int iY = Mathf.RoundToInt(y);
		int iZ = Mathf.RoundToInt(-x -y);

		if (iX + iY + iZ != 0) {
			float dX = Mathf.Abs(x - iX);
			float dY = Mathf.Abs(y - iY);
			float dZ = Mathf.Abs(-x -y - iZ);

			if (dX > dY && dX > dZ) {
				iX = -iY - iZ;
			}
			else if (dZ > dY) {
				iZ = -iX - iY;
			}
		}

		return new HexCoordinates(iX, iZ);
	}

	/// <summary>
	/// Override methods of To string to better show the coordinates of the cell
	/// </summary>
	/// <returns> string of coordinates </returns>
	public override string ToString () {
		return "(" +
			X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
	}
	
	/// <summary>
	/// Quick way to print out coordinates of cell
	/// </summary>
	/// <returns> string of coordinates separated </returns>
	public string ToStringOnSeparateLines () {
		return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
	}


	/// <summary>
	/// Finds the distance between two cells. Prevents it from being a negative number
	/// </summary>
	/// <param name="other"> Another hex cells hex coordinates </param>
	/// <returns> Distance between two cells </returns>
	public int DistanceTo (HexCoordinates other) {

		int xy =
			(x < other.x ? other.x - x : x - other.x) +
			(Y < other.Y ? other.Y - Y : Y - other.Y);

		if (HexMetrics.Wrapping) {
			other.x += HexMetrics.wrapSize;
			int xyWrapped = (x < other.x ? other.x - x : x - other.x) +
							(Y < other.Y ? other.Y - Y : Y - other.Y);

			if(xyWrapped < xy) {
				xy = xyWrapped;
			}
			else {
				other.x -= 2 * HexMetrics.wrapSize;
				xyWrapped =
					(x < other.x ? other.x - x : x - other.x) +
					(Y < other.Y ? other.Y - Y : Y - other.Y);
				if (xyWrapped < xy) {
					xy = xyWrapped;
				}
			}
		}

		return (xy + (z < other.z ? other.z - z : z - other.z)) / 2;

	}

	/// <summary>
	/// Saves X and Z Coordinates for Hex Cell
	/// </summary>
	/// <param name="writer"> Passed in writer parameter </param>
	public void Save(BinaryWriter writer) {
		writer.Write(x);
		writer.Write(z);
	}

	/// <summary>
	/// Loads previously saved Hex coordinates for a hex. Returns them as a hex coordinate
	/// </summary>
	/// <param name="reader"> Passed in reader parameter </param>
	/// <returns> Hex coordinate of cell </returns>
	public static HexCoordinates Load(BinaryReader reader) {
		HexCoordinates c;
		c.x = reader.ReadInt32();
		c.z = reader.ReadInt32();
		return c;
	}
}