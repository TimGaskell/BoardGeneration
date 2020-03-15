using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexFeatureManager : MonoBehaviour {
    public HexFeatureCollection[] urbanCollections, farmCollections, plantCollection;
    Transform container;

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

    }

    public void Apply() {

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
}
