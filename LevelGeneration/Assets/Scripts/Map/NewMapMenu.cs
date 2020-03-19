using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewMapMenu : MonoBehaviour
{

    public HexGrid hexgGrid;


    public void Open() {
        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    public void Close() {
        gameObject.SetActive(false);
    }

    void CreateMap(int x, int z) {
        hexgGrid.CreateMap(x, z);
        HexMapCamera.ValidatePosition();
        Close();
    }

    public void CreateSmallMap() {
        CreateMap(20, 15);
    }
    public void CreateMediumMap() {
        CreateMap(40, 30);
    }

    public void CreateLargeMap() {
        CreateMap(80, 60);
        HexMapCamera.Locked = false;
    }

}
