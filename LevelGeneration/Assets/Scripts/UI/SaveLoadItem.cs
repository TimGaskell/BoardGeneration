using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadItem : MonoBehaviour
{
    public SaveLoadMenu menu;
    string mapName;

    /// <summary>
    /// Get: returns the name of the map assigned to this  game object
    /// Set: Changes the name of the map and text object of this game object
    /// </summary>
    public string MapName {
        get {
            return mapName;
        }
        set {
            mapName = value;
            transform.GetChild(0).GetComponent<Text>().text = value;
        }
    }

    /// <summary>
    /// UI button click on this item. Changes the text in the name input to the name of this map saved here
    /// </summary>
    public void Select() {
        menu.SelectItem(mapName);
    }
}
