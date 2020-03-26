using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class HexUnit : MonoBehaviour
{
    HexCell location;
    float orientation;

    public static HexUnit unitPrefab;

    const float travelSpeed = 4f;
    const float rotationSpeed = 180f;

    List<HexCell> pathToTravel;

    private void OnEnable() {
        if (location) {
            transform.localPosition = location.Position;
        }
    }

    /// <summary>
    /// Get: returns location of Hex unit
    /// Set: Deassigns its previous location and assigns a new one. The hex cell value being set gains a reference to the hex unit on it and sets it location to that hex.
    /// </summary>
    public HexCell Location {
        get {
            return location;
        }
        set {
            if (location) {
                location.Unit = null;
            }
            location = value;
            value.Unit = this;
            transform.localPosition = value.Position;
        }
    }


    /// <summary>
    /// Refreshes the local position to the hex cell location
    /// </summary>
    public void ValidateLocation() {
        transform.localPosition = location.Position;
    }

    /// <summary>
    /// Get: Returns the float orientation of the Y angle
    /// Set: Sets the orientation to new value and rotates the local rotation to the new value
    /// </summary>
    public float Orientation {
        get {
            return orientation;
        }
        set {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0f, value, 0f);
        }
    }
    /// <summary>
    /// Returns a bool value of whether a unit can travel to specific hex. It can't have water or have a unit on it.
    /// </summary>
    /// <param name="cell"> Cell it is checking </param>
    /// <returns> True or false if it can move to hex </returns>
    public bool IsValidDestination(HexCell cell) {
        return !cell.isUnderwater && !cell.Unit;
    }

    /// <summary>
    /// Removes Unit from its hex. Destroys the object
    /// </summary>
    public void Die() {
        location.Unit = null;
        Destroy(gameObject);
    }

    /// <summary>
    /// Initiates movement of hex unit based on a list of Hex cells pass through. Instantly has the hex unit define its location by the end point
    /// </summary>
    /// <param name="path"> List of Hex Cells the unit will travel through </param>
    public void Travel(List<HexCell> path) {
        Location = path[path.Count - 1];
        pathToTravel = path;
        StopAllCoroutines();
        StartCoroutine(TravelPath());
    }

    /// <summary>
    /// Method used for the movement of a unit through a path defined in a list of Hex Cells. It starts by looking at the forward position of where it will begin traveling to.
    /// After which the function will look through all positions in the list and travel through them at a rate defined by speed and delta time. It also looks in the direction that it is currently traveling.
    /// The second loop ensures that the unit ends up in the final hexes center and looks in the forward direction.
    /// After all that is complete, it assigns the position to hexes location, making sure it is the definitive coordinate of the hex. It also goes back to its original rotation.
    /// </summary>
    /// <returns> Nothing .</returns>
    IEnumerator TravelPath() {
        Vector3 a, b, c = pathToTravel[0].Position;
        transform.localPosition = c;
        yield return LookAt(pathToTravel[1].Position);

        float t = Time.deltaTime * travelSpeed;

        for(int i = 1; i < pathToTravel.Count; i++) {
          
            a = c;
            b = pathToTravel[i - 1].Position;
            c = (b + pathToTravel[i].Position) * 0.5f;
            for (; t < 1f; t += Time.deltaTime * travelSpeed) {
                transform.localPosition = Bezier.GetPoint(a,b,c,t);
                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0f;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }
            t -= 1f;
        }

        a = c;
        b = pathToTravel[pathToTravel.Count - 1].Position;
        c = b;

        for (; t < 1f; t += Time.deltaTime * travelSpeed) {
           
            transform.localPosition = Bezier.GetPoint(a, b, c, t);
            Vector3 d = Bezier.GetDerivative(a, b, c, t);
            d.y = 0f;
            transform.localRotation = Quaternion.LookRotation(d);
          
            yield return null;
        }

        transform.localPosition = location.Position;
        orientation = transform.localRotation.eulerAngles.y;
        ListPool<HexCell>.Add(pathToTravel);
        pathToTravel = null;
    }

    //void OnDrawGizmos() {
    //    if (pathToTravel == null || pathToTravel.Count == 0) {
    //        return;
    //    }

    //    Vector3 a, b,c = pathToTravel[0].Position;

    //    for (int i = 1; i < pathToTravel.Count; i++) {

    //        a = c;
    //        b = pathToTravel[i - 1].Position;
    //        c = (b + pathToTravel[i].Position) * 0.5f;

    //        for (float t = 0f; t < 1f; t += 0.1f) {
    //            Gizmos.DrawSphere(Bezier.GetPoint(a,b,c,t), 2f);
    //        }

    //    }
    //    a = c;
    //    b = pathToTravel[pathToTravel.Count - 1].Position;
    //    c = b;
    //    for (float t = 0f; t < 1f; t += 0.1f) {
    //        Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
    //    }
    //}


    /// <summary>
    /// Method used for having Unit looking in the direction of the cell that it traveling to.
    /// Determines the difference in angle from its rotation to the rotation of point. This determines the angle it needs to change and the speed it rotates.
    /// It spherically lerps the original rotation and the rotation it needs to be and updates by time.
    /// Then sets the rotation to face the point just in case.
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    IEnumerator LookAt(Vector3 point) {
        point.y = transform.localRotation.y;
        Quaternion fromRotation = transform.localRotation;
        Quaternion toRotation = Quaternion.LookRotation(point - transform.localPosition);

        float angle = Quaternion.Angle(fromRotation, toRotation);
        float speed = rotationSpeed / angle;

        if (angle > 0) {
            for (float t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed) {
                transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
                yield return null;
            }
        }

        transform.LookAt(point);
        orientation = transform.localRotation.eulerAngles.y;
    }

    /// <summary>
    /// Saves coordinates for hex cell unit is on. Saves its orientation 
    /// </summary>
    /// <param name="writer"> Passed in Writer parameter </param>
    public void Save (BinaryWriter writer) {
        location.coordinates.Save(writer);
        writer.Write(orientation);
    }

    /// <summary>
    /// Loads the previously saved Hex coordinates and orientation of hex unit. Creates the unit on those saved coordinates and adds it back to the list.
    /// </summary>
    /// <param name="reader"> Passed in reader parameter </param>
    /// <param name="grid"> Hex grid script </param>
    public static void Load (BinaryReader reader, HexGrid grid) {
        HexCoordinates coordinates = HexCoordinates.Load(reader);
        float orientation = reader.ReadSingle();
        grid.AddUnit(Instantiate(unitPrefab), grid.GetCell(coordinates), orientation);
    }

   
}
