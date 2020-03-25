using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class HexUnit : MonoBehaviour
{
    HexCell location;
    float orientation;
    public static HexUnit unitPrefab;


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
