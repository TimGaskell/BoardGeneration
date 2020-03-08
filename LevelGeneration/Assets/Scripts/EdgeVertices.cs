
using UnityEngine;

public struct EdgeVertices {

    public Vector3 v1, v2, v3, v4;


    /// <summary>
    /// Returns all vertex for an edge by looking at its corners
    /// </summary>
    /// <param name="corner1"> First corner of the edge </param>
    /// <param name="corner2"> Second Corner of the edge</param>
    public EdgeVertices (Vector3 corner1, Vector3 corner2)
    {
        v1 = corner1;
        v2 = Vector3.Lerp(corner1, corner2, 1f / 3f);
        v3 = Vector3.Lerp(corner1, corner2, 2f / 3f);
        v4 = corner2;
    }

    /// <summary>
    /// Returns all vertexes for  an edge between two Hexes defined in Edge Vertices's
    /// </summary>
    /// <param name="a"> Set 1 of EdgeVertices for a hexagon </param>
    /// <param name="b"> Set 2 of EdgeVertices for a hexagon</param>
    /// <param name="step"> Current step of a terrace </param>
    /// <returns> new edge vertices's as points between the two edges </returns>
    public static EdgeVertices TerraceLerp(EdgeVertices a, EdgeVertices b, int step)
    {
        EdgeVertices result;
        result.v1 = HexMetrics.TerraceLerp(a.v1, b.v1, step);
        result.v2 = HexMetrics.TerraceLerp(a.v2, b.v2, step);
        result.v3 = HexMetrics.TerraceLerp(a.v3, b.v3, step);
        result.v4 = HexMetrics.TerraceLerp(a.v4, b.v4, step);
        return result;
    }

}
