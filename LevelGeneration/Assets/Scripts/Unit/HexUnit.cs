﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class HexUnit : MonoBehaviour
{
    HexCell location;
    float orientation;
    public static HexUnit unitPrefab;

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

    public void ValidateLocation() {
        transform.localPosition = location.Position;
    }

    public float Orientation {
        get {
            return orientation;
        }
        set {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0f, value, 0f);
        }
    }
    public bool IsValidDestination(HexCell cell) {
        return !cell.isUnderwater && !cell.Unit;
    }

    public void Die() {
        location.Unit = null;
        Destroy(gameObject);
    }

    public void Save (BinaryWriter writer) {
        location.coordinates.Save(writer);
        writer.Write(orientation);
    }

    public static void Load (BinaryReader reader, HexGrid grid) {
        HexCoordinates coordinates = HexCoordinates.Load(reader);
        float orientation = reader.ReadSingle();
        grid.AddUnit(Instantiate(unitPrefab), grid.GetCell(coordinates), orientation);
    }

   
}
