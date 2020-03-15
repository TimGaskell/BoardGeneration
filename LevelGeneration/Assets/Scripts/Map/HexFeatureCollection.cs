
using UnityEngine;

[System.Serializable]
public struct HexFeatureCollection 
{
    public Transform[] prefabs;

    /// <summary>
    /// Picks a prefab from the array based on the choice index of array
    /// </summary>
    /// <param name="choice"> Index of array for which prefab is wanted </param>
    /// <returns> Prefab game object as a transform </returns>
    public Transform Pick (float choice) {
        return prefabs[(int)(choice * prefabs.Length)];
    }
}
