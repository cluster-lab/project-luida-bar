#if UNITY_EDITOR

using UnityEngine;

public class PlacementSettings: MonoBehaviour
{
    public string dataPath = "";

    public Vector3 position = Vector3.zero;
    public Vector3 rotation = Vector3.zero;
    public Vector3 size = Vector3.one;

    public string coordSys = "World";
    public string myID = "";
    public string variableFilePath = "";
    public string shape = "Plane";

    public float maxDistance = 50f;
    public string volumeRolloff = "Logarithmic";
    public bool loop = true;
    public float volume = 1f;

    // Future methods to process placement settings
}

#endif
