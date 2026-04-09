using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(LuidaAssignAvatarGimmick))]
public class LuidaAssignAvatarGimmickEditor : Editor
{
    private static readonly string[] AvailableBoneNames = {
        "Hips", "Spine", "Chest", "Neck", "Head",
        "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand",
        "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand",
        "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "LeftToes",
        "RightUpperLeg", "RightLowerLeg", "RightFoot", "RightToes",
        "Jaw",
        "LeftThumbProximal", "LeftThumbIntermediate", "LeftThumbDistal",
        "LeftIndexProximal", "LeftIndexIntermediate", "LeftIndexDistal",
        "LeftMiddleProximal", "LeftMiddleIntermediate", "LeftMiddleDistal",
        "LeftRingProximal", "LeftRingIntermediate", "LeftRingDistal",
        "LeftLittleProximal", "LeftLittleIntermediate", "LeftLittleDistal",
        "RightThumbProximal", "RightThumbIntermediate", "RightThumbDistal",
        "RightIndexProximal", "RightIndexIntermediate", "RightIndexDistal",
        "RightMiddleProximal", "RightMiddleIntermediate", "RightMiddleDistal",
        "RightRingProximal", "RightRingIntermediate", "RightRingDistal",
        "RightLittleProximal", "RightLittleIntermediate", "RightLittleDistal",
    };

    private bool _boneOffsetsFoldout = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var gimmick = (LuidaAssignAvatarGimmick)target;

        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour(gimmick), typeof(LuidaAssignAvatarGimmick), false);
        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Gimmick Signal", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("key"), new GUIContent("Trigger Key"));
        EditorGUILayout.HelpBox(
            "This key is used as both the CCK signal name and the global state trigger.\n" +
            "Use $.sendSignalCompat('this', '<key>') from ClusterScript, or wire a CCK trigger gimmick to this signal.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Avatar Assignment", EditorStyles.boldLabel);

        // Avatar ID dropdown
        DrawAvatarIDField(gimmick);

        // Participant index
        EditorGUILayout.PropertyField(serializedObject.FindProperty("participantIndex"), new GUIContent("Participant Index"));

        // Bone offsets
        EditorGUILayout.Space();
        DrawBoneOffsets(gimmick);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawAvatarIDField(LuidaAssignAvatarGimmick gimmick)
    {
        var avatarRegistry = AssetDatabase.LoadAssetAtPath<AvatarRegistry>(AvatarsConfigAssetUtil.RegistryPath);
        if (avatarRegistry != null && avatarRegistry.entries.Count > 0)
        {
            string[] avatarIDs = avatarRegistry.GetAvatarIDs();
            int selectedIdx = System.Array.IndexOf(avatarIDs, gimmick.avatarID);
            if (selectedIdx < 0) selectedIdx = 0;

            int newIdx = EditorGUILayout.Popup("Avatar ID", selectedIdx, avatarIDs);
            string newAvatarID = avatarIDs[newIdx];
            if (newAvatarID != gimmick.avatarID)
            {
                Undo.RecordObject(gimmick, "Change Avatar ID");
                gimmick.avatarID = newAvatarID;
                EditorUtility.SetDirty(gimmick);
            }
        }
        else
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("avatarID"), new GUIContent("Avatar ID"));
            EditorGUILayout.HelpBox("No avatars registered yet. Open LUIDA > Configure avatars to add avatars.", MessageType.Warning);
        }
    }

    private void DrawBoneOffsets(LuidaAssignAvatarGimmick gimmick)
    {
        _boneOffsetsFoldout = EditorGUILayout.Foldout(_boneOffsetsFoldout, "Bone Offsets", true);
        if (!_boneOffsetsFoldout) return;

        EditorGUI.indentLevel++;

        if (gimmick.boneOffsets == null)
            gimmick.boneOffsets = new List<BoneOffsetData>();

        for (int i = gimmick.boneOffsets.Count - 1; i >= 0; i--)
        {
            var entry = gimmick.boneOffsets[i];

            EditorGUILayout.BeginHorizontal();

            // Bone name dropdown
            int boneIdx = System.Array.IndexOf(AvailableBoneNames, entry.boneName);
            if (boneIdx < 0) boneIdx = 0;
            int newBoneIdx = EditorGUILayout.Popup("Bone", boneIdx, AvailableBoneNames);
            if (AvailableBoneNames[newBoneIdx] != entry.boneName)
            {
                Undo.RecordObject(gimmick, "Change Bone Name");
                entry.boneName = AvailableBoneNames[newBoneIdx];
                EditorUtility.SetDirty(gimmick);
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                Undo.RecordObject(gimmick, "Remove Bone Offset");
                gimmick.boneOffsets.RemoveAt(i);
                EditorUtility.SetDirty(gimmick);
                EditorGUILayout.EndHorizontal();
                continue;
            }
            EditorGUILayout.EndHorizontal();

            // Position offset
            EditorGUI.BeginChangeCheck();
            Vector3 newPos = EditorGUILayout.Vector3Field("  Pos Offset", entry.posOffset);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(gimmick, "Change Bone Pos Offset");
                entry.posOffset = newPos;
                EditorUtility.SetDirty(gimmick);
            }

            // Rotation offset
            EditorGUI.BeginChangeCheck();
            Vector3 newRot = EditorGUILayout.Vector3Field("  Rot Offset", entry.rotOffset);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(gimmick, "Change Bone Rot Offset");
                entry.rotOffset = newRot;
                EditorUtility.SetDirty(gimmick);
            }

            EditorGUILayout.Space(2);
        }

        if (GUILayout.Button("+ Add Bone Offset"))
        {
            Undo.RecordObject(gimmick, "Add Bone Offset");
            gimmick.boneOffsets.Add(new BoneOffsetData());
            EditorUtility.SetDirty(gimmick);
        }

        EditorGUI.indentLevel--;
    }
}

[CustomEditor(typeof(LuidaUnassignAvatarGimmick))]
public class LuidaUnassignAvatarGimmickEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var gimmick = (LuidaUnassignAvatarGimmick)target;

        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour(gimmick), typeof(LuidaUnassignAvatarGimmick), false);
        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Gimmick Signal", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("key"), new GUIContent("Trigger Key"));
        EditorGUILayout.HelpBox(
            "This key is used as both the CCK signal name and the global state trigger.\n" +
            "Use $.sendSignalCompat('this', '<key>') from ClusterScript, or wire a CCK trigger gimmick to this signal.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Avatar Unassignment", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("participantIndex"), new GUIContent("Participant Index"));

        serializedObject.ApplyModifiedProperties();
    }
}
