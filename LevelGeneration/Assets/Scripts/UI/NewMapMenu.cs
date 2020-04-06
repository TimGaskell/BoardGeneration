using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewMapMenu : MonoBehaviour
{

    public HexGrid hexgGrid;
    public HexMapGenerator mapGenerator;

    bool generateMaps = true;
    bool wrapping = true;

    /// <summary>
    /// Opens the new map UI and locks the camera
    /// </summary>
    public void Open() {
        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    /// <summary>
    /// Closes the new map UI and unlocks the camera
    /// </summary>
    public void Close() {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }

    /// <summary>
    /// Used for creating a map based on its size in the x and z direction. 
    /// </summary>
    /// <param name="x"> How many cells in the x direction </param>
    /// <param name="z"> How many cells in the z direction </param>
    void CreateMap(int x, int z) {
        if (generateMaps) {
            mapGenerator.GenerateMap(x, z, wrapping);
        }
        else {
            hexgGrid.CreateMap(x, z, wrapping);
        }
        HexMapCamera.ValidatePosition();
        Close();
    }

    /// <summary>
    /// Standard small map size for map creation. Used in the UI
    /// </summary>
    public void CreateSmallMap() {
        CreateMap(20, 15);
        HexMapCamera.Locked = false;
    }

    /// <summary>
    /// Standard medium map size for map creation. Used in the UI
    /// </summary>
    public void CreateMediumMap() {
        CreateMap(40, 30);
        HexMapCamera.Locked = false;
    }

    /// <summary>
    /// Standard large map size for map creation. Used in the UI
    /// </summary>
    public void CreateLargeMap() {
        CreateMap(80, 60);
        HexMapCamera.Locked = false;
    }

    /// <summary>
    /// Toggles whether the map will be randomly generated or a standard map
    /// </summary>
    /// <param name="toggle"> Yes or no to random generation </param>
    public void ToggleMapGeneration(bool toggle) {
        generateMaps = toggle;
    }

    /// <summary>
    /// Toggles whether the map will have wrapping
    /// </summary>
    /// <param name="toggle"> Yes or no to wrapping map </param>
    public void ToggleWrapping (bool toggle) {
        wrapping = toggle;
    }


}
