using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;

public class SaveLoadMenu : MonoBehaviour
{
    public HexGrid hexGrid;
    public Text menuLabel, actionButtonLabel;
    public InputField nameInput;
    public RectTransform listContent;
    public SaveLoadItem itemPrefab;

    const int mapFileVersion =3;
    bool saveMode;

    /// <summary>
    /// Used for opening the save load menu UI. Determines if the game is saving or loading and edits the text and buttons appropriately.
    /// Fills the item menu with all maps that have been saved and locks the camera from moving.
    /// </summary>
    /// <param name="saveMode"></param>
    public void Open(bool saveMode) {
        this.saveMode = saveMode;
        if (saveMode) {
            menuLabel.text = "Save Map";
            actionButtonLabel.text = "Save";
        }
        else {
            menuLabel.text = "Load Map";
            actionButtonLabel.text = "Load";
        }
        FillList();
        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    /// <summary>
    /// Used for closing the save load menu UI. Unlocks the camera so it can move.
    /// </summary>
    public void Close() {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }

    /// <summary>
    /// Uses the name input text of the UI to create a path leading to where saved maps can be found. Only returns if there is more than 1 saved map in that location
    /// </summary>
    /// <returns> String path of file location of map name in name input </returns>
    string GetSelectedPath() {
        string mapName = nameInput.text;
        if(mapName.Length == 0) {
            return null;
        }
        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }

    /// <summary>
    /// UI action element that either saves or loads a map based on its file path.
    /// If a file name doesn't exist in the save folder then it will do nothing.
    /// </summary>
    public void Action() {
        string path = GetSelectedPath();
        if(path == null) {
            return;
        }
        if (saveMode) {
            Save(path);
        }
        else {
            Load(path);
        }
        Close();
    }

    /// <summary>
    /// When a map name button is pressed. Assign that maps name to the name input text
    /// </summary>
    /// <param name="name"></param>
    public void SelectItem(string name) {
        nameInput.text = name;
    }

    /// <summary>
    /// Refreshes the list by destroying old map name game objects.
    /// Fills the List of all .map files it can find in the local save area as button game objects .Sorts them by alphabetical order.
    /// Assigns the button the name of the map it is holding
    /// </summary>
    void FillList() {
        
        for (int i = 0; i < listContent.childCount; i++) {
            Destroy(listContent.GetChild(i).gameObject);
        }
        string[] paths = Directory.GetFiles(Application.persistentDataPath, "*.map");
        Array.Sort(paths);
        for(int i = 0; i < paths.Length; i++) {
            SaveLoadItem item = Instantiate(itemPrefab);
            item.menu = this;
            item.MapName = Path.GetFileNameWithoutExtension(paths[i]);
            item.transform.SetParent(listContent, false);
        }
    }

    /// <summary>
    /// UI element for deleting a file based on the name inputed in the name input UI element. Refreshes the list.
    /// If there is no file that exists of that name. Return from function.
    /// </summary>
    public void Delete() {
        string path = GetSelectedPath();
        if (path == null) {
            return;
        }
        if (File.Exists(path)) {
            File.Delete(path);
        }
        nameInput.text = "";
        FillList();
    }

    /// <summary>
	/// UI Element used to create a save file as a binary format of all the hex cell data on the map.
	/// Starts off with a header of 0 to denote the version type of this saving system.
	/// </summary>
	public void Save(string path) {
     
        using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create))) {
            writer.Write(mapFileVersion);
            hexGrid.Save(writer);

        }
    }

    /// <summary>
    /// UI element used to reading the previously created saved binary file. Each hex grid and cell will read the data and assign its cells with the information needed
    /// to recreate the previous map.
    /// If the header is not 0 then it was made with a different version and shouldn't be loaded.
    /// </summary>
    public void Load(string path) {

        if (!File.Exists(path)) {
            Debug.LogError("File does not exist " + path);
            return;
        }

        using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open))) {
            int header = reader.ReadInt32();
            if (header <= mapFileVersion) {
                hexGrid.Load(reader, header);
                HexMapCamera.ValidatePosition();
            }
            else {
                Debug.LogWarning("Unknown map format " + header);
            }
        }
    }


}
