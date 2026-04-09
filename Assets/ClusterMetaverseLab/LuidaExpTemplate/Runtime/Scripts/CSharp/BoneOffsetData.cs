#if UNITY_EDITOR
using UnityEngine;

[System.Serializable]
public class BoneOffsetData
{
    public string boneName = "";
    public Vector3 posOffset;
    public Vector3 rotOffset;
}
#endif
