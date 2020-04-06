﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMapCamera : MonoBehaviour
{

    Transform swivel, stick;
    public float stickMinZoom, stickMaxZoom;
    public float swivelMinZoom, swivelMaxZoom;
    public float moveSpeedMinZoom, moveSpeedMaxZoom;
    float zoom = 1f;
    public float rotationSpeed;
    float rotationAngle;
    public HexGrid grid;

    static HexMapCamera instance;

    private void Awake()
    {
        instance = this;
        ValidatePosition();
        HexMapCamera.Locked = true;
        swivel = transform.GetChild(0);
        stick = swivel.GetChild(0);
    }

    private void Update()
    {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if(zoomDelta != 0f)
        {
            AdjustZoom(zoomDelta);
        }

        float rotationDelta = Input.GetAxis("Rotation");
        if(rotationDelta != 0f)
        {
            AdjustRotation(rotationDelta);
        }

        float xDelta = Input.GetAxis("Horizontal");
        float zDelta = Input.GetAxis("Vertical");

        if(xDelta != 0f || zDelta != 0f)
        {
            AdjustPosition(xDelta, zDelta);
        }
    }

    /// <summary>
    /// Determines how much zoom for the camera between the min and max zoom. Assigns it to the camera stick. Also assigns swivel to the camera so tilt it when zooming in and out
    /// </summary>
    /// <param name="delta"></param>
    void AdjustZoom(float delta)
    {
        zoom = Mathf.Clamp01(zoom + delta);

        float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
        stick.localPosition = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
        swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);

    }

    /// <summary>
    /// Assigns rotation for the camera is the y plane.
    /// </summary>
    /// <param name="delta"> change in rotation </param>
    void AdjustRotation(float delta)
    {
        rotationAngle += delta * rotationSpeed * Time.deltaTime;
        if(rotationAngle < 0f)
        {
            rotationAngle += 360f;
        }
        else if(rotationAngle >= 360f)
        {
            rotationAngle -= 360;
        }
        transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);

    }

    /// <summary>
    /// Function for moving the location of the camera. Limits the camera to the boundary of the hex map.
    /// </summary>
    /// <param name="xDelta"> Change in the x plane </param>
    /// <param name="zDelta"> Change in the z plane </param>
    void AdjustPosition(float xDelta, float zDelta)
    {
        Vector3 direction = transform.localRotation * new Vector3(xDelta, 0f, zDelta).normalized;
        float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
        float distance =Mathf.Lerp(moveSpeedMinZoom,moveSpeedMaxZoom,zoom) * damping * Time.deltaTime;

        Vector3 position = transform.localPosition;
        position += direction * distance;
        transform.localPosition = grid.wrapping ? WrapPosition(position) : ClampPosition(position);
    }

    /// <summary>
    /// Calculates the size of the map to limit the cameras movement
    /// </summary>
    /// <param name="position"> position of the camera</param>
    /// <returns> vector3 for the camera position </returns>
    Vector3 ClampPosition(Vector3 position)
    {
        float xMax = (grid.cellCountX  -0.5f) * HexMetrics.innerDiameter;
        position.x = Mathf.Clamp(position.x, 0f, xMax);

        float zMax = (grid.cellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
        position.z = Mathf.Clamp(position.z, 0f, zMax);
        
        return position;
    }

    /// <summary>
    /// Setter for allowing the camera to move or not by turning off this script
    /// </summary>
    public static bool Locked {

        set {
            instance.enabled = !value;
        }
    }

    /// <summary>
    /// Readjusts the camera so that it moves back to the origin point of the scene. Used for when creating a new map
    /// </summary>
    public static void ValidatePosition() {
        instance.AdjustPosition(0f, 0f);
    }

    /// <summary>
    /// Wraps camera to either left or right of map once it goes off the edge of the map. Makes it look like the user went in a circle
    /// </summary>
    /// <param name="position"> Position of camera </param>
    /// <returns> New camera position </returns>
    Vector3 WrapPosition(Vector3 position) {
        float width = grid.cellCountX * HexMetrics.innerDiameter;
        while (position.x < 0f) {
            position.x += width;
        }
        while (position.x > width) {
            position.x -= width;
        }

        float zMax = (grid.cellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
        position.z = Mathf.Clamp(position.z, 0f, zMax);

        grid.CenterMap(position.x);
        return position;
    }

}


