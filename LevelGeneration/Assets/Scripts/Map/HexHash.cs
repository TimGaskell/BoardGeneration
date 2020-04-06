using UnityEngine;

public struct HexHash {

    public float a, b, c, d, e;

    /// <summary>
    /// Series of random numbers between 0 and 0.999f.
    /// Used for determining chances of something happening
    /// </summary>
    /// <returns> Set of random values </returns>
    public static HexHash Create() {

        HexHash hash;
        hash.a = Random.value * 0.999f;
        hash.b = Random.value * 0.999f;
        hash.c = Random.value * 0.999f;
        hash.d = Random.value * 0.999f;
        hash.e = Random.value * 0.999f;
        return hash;
    }


}
