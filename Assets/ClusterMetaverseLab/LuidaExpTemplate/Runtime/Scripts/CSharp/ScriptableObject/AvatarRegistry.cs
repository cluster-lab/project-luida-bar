#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

public enum AvatarScaleMode
{
    ScaleToPlayer,
    OriginalScale,
}

[Serializable]
public class AvatarEntry
{
    public string avatarID;
    public string displayName;
    public GameObject sourceVrmPrefab;
    public GameObject wrapperItemPrefab;
    public bool syncFingers;
    public bool syncFeetToes;
    public bool syncJaw;
    public AvatarScaleMode scaleMode = AvatarScaleMode.ScaleToPlayer;
    public bool syncHipsY = true;
    public float hipsYOffset = 0f;
    public bool needsRebuild;
}

[CreateAssetMenu(fileName = "AvatarRegistry", menuName = "LUIDA/Avatar Registry")]
public class AvatarRegistry : ScriptableObject
{
    public List<AvatarEntry> entries = new List<AvatarEntry>();

    public AvatarEntry FindByID(string avatarID)
    {
        return entries.Find(e => e.avatarID == avatarID);
    }

    public string[] GetAvatarIDs()
    {
        string[] ids = new string[entries.Count];
        for (int i = 0; i < entries.Count; i++)
            ids[i] = entries[i].avatarID;
        return ids;
    }
}
#endif
