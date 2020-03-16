using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexFeatureManager : MonoBehaviour {
    public HexFeatureCollection[] urbanCollections, farmCollections, plantCollection;
    Transform container;

    public HexMesh walls;

    /// <summary>
    /// Destroys the previous container if there is already one.
    /// After which it creates a new container for feature game objects
    /// </summary>
    public void Clear() {
        if (container) {
            Destroy(container.gameObject);
        }
        container = new GameObject("Features Container").transform;
        container.SetParent(transform, false);

        walls.Clear();

    }

    public void Apply() {

        walls.Apply();

    }

    /// <summary>
    /// Picks a random prefab based on the collection that is being accessed. The level at which the collection is being accessed ("small" , "medium" or "large" features).
    /// Determines if there is going to be an object chosen based on its hash value threshold. Can sample from small, medium or large features if threshold allows.
    /// Basically chooses between two prefabs of that collection based on the choice value hash rounded.
    /// </summary>
    /// <param name="collection"> Feature collection of prefabs </param>
    /// <param name="level"> Level of collection ("small" , "medium" , "large" </param>
    /// <param name="hash"> random value between 0-0.99, determines if it can be selected </param>
    /// <param name="choice"> index value for picking a specific game object in collection </param>
    /// <returns> Prefab that is chosen </returns>
    Transform PickPrefab (HexFeatureCollection[] collection, int level, float hash, float choice) {
        if( level > 0) {
            float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);
            for(int i = 0; i< thresholds.Length; i++) {
                if (hash < thresholds[i]) {
                    return collection[i].Pick(choice);
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Adds feature onto the hex.
    /// Determines whether to add other types of prefabs from other collections on the same hex based on its hash values.
    /// Provides each prefab with a random orientation and slight positional difference to allow different patterns in each hex.
    /// Hash a used for urban level 
    /// Hash b used for farm level
    /// Hash c used for plant level
    /// Hash d used for random choice for collection between 0 and 1
    /// Hash e used for random rotation of prefab scaler 
    /// </summary>
    /// <param name="cell"> current hex </param>
    /// <param name="Position"> position on the hex </param>
    public void AddFeature(HexCell cell, Vector3 Position) {
       
        HexHash hash = HexMetrics.SampleHashGrid(Position);

        Transform prefab = PickPrefab(urbanCollections,cell.UrbanLevel, hash.a, hash.d);
        Transform otherPrefab = PickPrefab(farmCollections, cell.FarmLevel, hash.b, hash.d);
        float usedHash = hash.a;

        if (prefab) {
            if(otherPrefab && hash.b < hash.a) {
                prefab = otherPrefab;
                usedHash = hash.b;
            }
        }
        else if (otherPrefab) {
            prefab = otherPrefab;
            usedHash = hash.b;
        }

        otherPrefab = PickPrefab(plantCollection, cell.PlantLevel, hash.c, hash.d);

        if (prefab) {
            if (otherPrefab && hash.c < usedHash) {
                prefab = otherPrefab;
            }
        }
        else if (otherPrefab) {
            prefab = otherPrefab;
        }
        else {
            return;
        }

        if (!prefab) {
            return;
        }

        Transform instance = Instantiate(prefab);
        Position.y += instance.localScale.y * 0.5f;
        instance.localPosition = HexMetrics.Perturb(Position);
        instance.localRotation = Quaternion.Euler(0f, 360 * hash.e, 0f);
        instance.SetParent(container, false);
    }

    /// <summary>
    /// Adds a wall segment to a hexagon edge based on whether or not:
    /// - The hex or joining hex is not underwater and not connecting to a cliff
    /// IF there is a river or road running through where that wall will go, create a gap where it will pass through
    /// </summary>
    /// <param name="near"> Near edge of hex</param>
    /// <param name="nearCell"> Near hex </param>
    /// <param name="far"> Far Edge of joining hex</param>
    /// <param name="farCell"> Far hex of neighbor hex</param>
    /// <param name="hasRiver"> has river in same direction </param>
    /// <param name="hasRoad"> has road in same direction </param>
    public void AddWall(EdgeVertices near, HexCell nearCell, EdgeVertices far, HexCell farCell, bool hasRiver, bool hasRoad) {

        if(nearCell.Walled != farCell.Walled &&
            !nearCell.isUnderwater && !farCell.isUnderwater &&
            nearCell.GetEdgeType(farCell) != HexEdgeType.Cliff) {
            AddWallSegment(near.v1, far.v1, near.v2, far.v2);
            if(hasRiver || hasRoad) {
                AddWallCap(near.v2, far.v2);
                AddWallCap(far.v4, near.v4);
            }
            else {
                AddWallSegment(near.v2, far.v2, near.v3, far.v3);
                AddWallSegment(near.v3, far.v3, near.v4, far.v4);
            }
            AddWallSegment(near.v4, far.v4, near.v5, far.v5);
        }

    }

    /// <summary>
    /// Adds Wall segments to a Hex in a order which point out which hex has the corner of a wall. Based on a upside down triangle where it has a left, right, and bottom hex
    /// </summary>
    /// <param name="c1"> Bottom Hex vector </param>
    /// <param name="cell1"> Bottom Hex</param>
    /// <param name="c2"> Left Hex vector </param>
    /// <param name="cell2"> Left Hex</param>
    /// <param name="c3"> Right Hex vector </param>
    /// <param name="cell3"> Right Hex </param>
    public void AddWall(Vector3 c1, HexCell cell1, Vector3 c2, HexCell cell2, Vector3 c3, HexCell cell3) {

        if (cell1.Walled) {
            if (cell2.Walled) {
                if (!cell3.Walled) {
                    AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
                }
            }
            else if (cell3.Walled) {
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
            else {
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
        }
        else if(cell2.Walled) {
            if (cell3.Walled) {
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
            else {
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
        }
        else if (cell3.Walled) {
            AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
        }
    }

    /// <summary>
    /// Creates the appropriate Quads in order to create a wall segment in a specific direction.
    /// </summary>
    /// <param name="nearLeft"> Near vector 3 point for left wall </param>
    /// <param name="farLeft"> Far vector 3 point for left of wall</param>
    /// <param name="nearRight">  Near vector 3 point for right wall</param>
    /// <param name="farRight">  Far vector 3 point for right wall</param>
    void AddWallSegment(Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight) {

        nearLeft = HexMetrics.Perturb(nearLeft);
        farLeft = HexMetrics.Perturb(farLeft);
        nearRight = HexMetrics.Perturb(nearRight);
        farRight = HexMetrics.Perturb(farRight);

        Vector3 left = HexMetrics.WallLerp(nearLeft, farLeft);
        Vector3 right = HexMetrics.WallLerp(nearRight, farRight);

        Vector3 leftThicknessOffset = HexMetrics.WallThicknessOffset(nearLeft, farLeft);
        Vector3 rightThicknessOffset = HexMetrics.WallThicknessOffset(nearRight, farRight);

        float leftTop = left.y + HexMetrics.wallHeight;
        float rightTop = right.y + HexMetrics.wallHeight;

        Vector3 v1, v2, v3, v4;
        v1 = v3 = left - leftThicknessOffset;
        v2 = v4 = right - rightThicknessOffset;
        v3.y = leftTop;
        v4.y = rightTop;
        walls.AddQuadUnperturbed(v1, v2, v3, v4);

        Vector3 t1 = v3, t2 = v4;

        v1 = v3 = left + leftThicknessOffset;
        v2 = v4 = right + rightThicknessOffset;
        v3.y = leftTop;
        v4.y = rightTop;
        walls.AddQuadUnperturbed(v2, v1, v4, v3);

        walls.AddQuadUnperturbed(t1, t2, v3, v4);

    }

    /// <summary>
    /// Used for creating walls whilst creating a corner segment between them to join the walls.
    /// Only makes corners and walls if there is no water or cliffs to the left or right of hex.
    /// </summary>
    /// <param name="pivot"> Hex location for corner </param>
    /// <param name="PivotCell"> Hex Cell that is the pivot cell </param>
    /// <param name="left"> Left edge vector of left cell </param>
    /// <param name="leftcell"> left Hex cell from the pivot cell</param>
    /// <param name="right"> right edge vector of the right cell </param>
    /// <param name="rightCell"> right hex cell from the pivot cell</param>
    void AddWallSegment( Vector3 pivot, HexCell PivotCell, Vector3 left, HexCell leftcell, Vector3 right, HexCell rightCell) {

        if (PivotCell.isUnderwater) {
            return;
        }

        bool hasLeftWall = !leftcell.isUnderwater && PivotCell.GetEdgeType(leftcell) != HexEdgeType.Cliff;
        bool hasRightWall = !rightCell.isUnderwater && PivotCell.GetEdgeType(rightCell) != HexEdgeType.Cliff;

        if (hasLeftWall) {
            if (hasRightWall) {
                AddWallSegment(pivot, left, pivot, right);
            }
            else if (leftcell.Elevation < rightCell.Elevation) {
                AddWallWedge(pivot, left, right);
            }
            else {
                AddWallCap(pivot, left);
            }
        }
        else if (hasRightWall) {
            if (rightCell.Elevation < leftcell.Elevation) {
                AddWallWedge(right, pivot, left);
            }
            else {
                AddWallCap(right, pivot);
            }
        }
    }

    /// <summary>
    /// Creates a quad to seal up created gaps between walls where roads and river intersect.
    /// </summary>
    /// <param name="near"> Near vector3 point for wall </param>
    /// <param name="far"> Far Vector 3 point for wall</param>
    void AddWallCap (Vector3 near, Vector3 far) {

        near = HexMetrics.Perturb(near);
        far = HexMetrics.Perturb(far);

        Vector3 center = HexMetrics.WallLerp(near, far);
        Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

        Vector3 v1, v2, v3, v4;

        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.y = v4.y = center.y + HexMetrics.wallHeight;
        walls.AddQuadUnperturbed(v1, v2, v3, v4);
    }

    /// <summary>
    /// Creates an additional quads and triangle to fill the space when a wall connects next to a cliff face so there is no gap.
    /// </summary>
    /// <param name="near"> Near vector3 point for wall </param>
    /// <param name="far"> Far Vector 3 point for wall</param>
    /// <param name="point"> vector 3 of cliff </param>
    void AddWallWedge(Vector3 near, Vector3 far, Vector3 point) {

        near = HexMetrics.Perturb(near);
        far = HexMetrics.Perturb(far);
        point = HexMetrics.Perturb(point);

        Vector3 center = HexMetrics.WallLerp(near, far);
        Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

        Vector3 v1, v2, v3, v4;
        Vector3 pointTop = point;
        point.y = center.y;

        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.y = v4.y = pointTop.y = center.y + HexMetrics.wallHeight;

        walls.AddQuadUnperturbed(v1, point, v3, pointTop);
        walls.AddQuadUnperturbed(point, v2, pointTop, v4);
        walls.AddTriangleUnPerturbed(pointTop, v3, v4);

    }
}
