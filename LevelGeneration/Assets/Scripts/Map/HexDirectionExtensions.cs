using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Directions for each edge of the Hexagon 
/// </summary>
public enum HexDirection
{
    NE, E, SE, SW, W, NW
}

public static class HexDirectionExtensions 
{
    /// <summary>
    /// Grabs the opposite direction of the inputed direction
    /// </summary>
    /// <param name="direction">  Direction of the hexagon vertex e.g. NE, SE etc </param>
    /// <returns> int value of ENUM Direction </returns>
    public static HexDirection Opposite (this HexDirection direction)
    {
        return (int)direction < 3 ? (direction + 3) : (direction - 3);
    }

    /// <summary>
    /// Grabs the previous direction of the direction inputed
    /// </summary>
    /// <param name="direction">  Direction of the hexagon vertex e.g. NE, SE etc </param>
    /// <returns> Previous direction  </returns>
    public static HexDirection Previous (this HexDirection direction)
    {
        return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
    }

    /// <summary>
    /// Grabs the second previous direction of the direction inputed
    /// </summary>
    /// <param name="direction">  Direction of the hexagon vertex e.g. NE, SE etc </param>
    /// <returns> Second previous direction  </returns>
    public static HexDirection Previous2(this HexDirection direction)
    {
        direction -= 2;
        return direction >= HexDirection.NE ? direction : (direction + 6);
    }

    /// <summary>
    /// Grabs the next direction of the direction inputed
    /// </summary>
    /// <param name="direction">  Direction of the hexagon vertex e.g. NE, SE etc </param>
    /// <returns> next direction </returns>
    public static HexDirection Next (this HexDirection direction)
    {
        return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
    }

    /// <summary>
    /// Grabs the second next direction of the direction inputed
    /// </summary>
    /// <param name="direction">  Direction of the hexagon vertex e.g. NE, SE etc </param>
    /// <returns> second next direction </returns>
    public static HexDirection Next2(this HexDirection direction)
    {
        direction += 2;
        return direction <= HexDirection.NW ? direction : (direction - 6);
    }



}
