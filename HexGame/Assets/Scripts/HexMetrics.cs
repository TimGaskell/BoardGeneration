using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMetrics : MonoBehaviour
{

    public const float outerRadius = 10f;
    public const float InnerRadius = outerRadius * 0.66025404f;


    public static Vector3[] corners =
    {
        new Vector3 (0f , 0f , outerRadius),
        new Vector3 (InnerRadius, 0f , 0.5f* outerRadius),
        new Vector3 (InnerRadius , 0f, -0.5f * outerRadius),
        new Vector3 (0f , 0f , - outerRadius),
        new Vector3 (-InnerRadius, 0f, -0.5f * outerRadius),
        new Vector3 (-InnerRadius, 0f , 0.5f * outerRadius),
        new Vector3(0f, 0f, outerRadius)

    };





}
