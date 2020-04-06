using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Bezier 
{
    /// <summary>
    /// Returns points on a hex that create a curved movement for units.
    /// </summary>
    /// <param name="a"> Current Hex location </param>
    /// <param name="b"> Hex unit is moving to</param>
    /// <param name="c"> The mid point between the two hexes </param>
    /// <param name="t"> Delta time </param>
    /// <returns> Vector3 points leading towards b </returns>
    public static Vector3 GetPoint ( Vector3 a, Vector3 b, Vector3 c, float t) {
        float r = 1f - t;
        return r * r * a + 2f * r * t * b + t * t * c;
    }

    /// <summary>
    /// Returns the orientation the unit is facing whilst turning on a curve 
    /// </summary>
    /// <param name="a"> Current Hex location </param>
    /// <param name="b"> Hex unit is moving to </param>
    /// <param name="c">  The mid point between the two hexes </param>
    /// <param name="t"> Delta time</param>
    /// <returns> Vector3 direction to look at for the unit </returns>
    public static Vector3 GetDerivative(Vector3 a, Vector3 b, Vector3 c, float t) {
        return 2f * ((1f - t) * (b - a) + t * (c - b));
    }
}
