using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LuidaAssignAvatarGimmick))]
public class LuidaAssignAvatarGimmickEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var gimmick = (LuidaAssignAvatarGimmick)target;

        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour(gimmick), typeof(LuidaAssignAvatarGimmick), false);
        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Gimmick Signal", EditorStyles.boldLabel);

        var targetProp = serializedObject.FindProperty("target");
        // Player target is unsupported here: CCK rejects Player keys outside PlayerLocalUI.
        if (targetProp.enumValueIndex == (int)CustomGimmickTarget.Player)
        {
            targetProp.enumValueIndex = (int)CustomGimmickTarget.Item;
        }
        var allowedValues = new[] { CustomGimmickTarget.Item, CustomGimmickTarget.Global, CustomGimmickTarget.This };
        var allowedLabels = new[] { "Item", "Global", "This" };
        int activeIdx = System.Array.IndexOf(allowedValues, (CustomGimmickTarget)targetProp.enumValueIndex);
        if (activeIdx < 0) activeIdx = 0;
        int newActiveIdx = EditorGUILayout.Popup("Trigger Target", activeIdx, allowedLabels);
        if (newActiveIdx != activeIdx)
        {
            targetProp.enumValueIndex = (int)allowedValues[newActiveIdx];
        }
        EditorGUILayout.PropertyField(serializedObject.FindProperty("key"), new GUIContent("Trigger Key"));

        var targetEnum = (CustomGimmickTarget)targetProp.enumValueIndex;
        if (targetEnum == CustomGimmickTarget.Item)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("item"), new GUIContent("Target Item"));
        }

        string targetHint = targetEnum == CustomGimmickTarget.Global
            ? "Triggered by a global signal.\nUse $.setStateCompat('global', '<key>', true) to fire."
            : "Triggered from the same item.\nUse $.sendSignalCompat('this', '<key>') from ClusterScript, or wire a CCK trigger gimmick to this signal.";
        EditorGUILayout.HelpBox(targetHint, MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Avatar Action", EditorStyles.boldLabel);

        // Participant number (always shown, min 1)
        var participantProp = serializedObject.FindProperty("participantNumber");
        EditorGUILayout.PropertyField(participantProp, new GUIContent("Participant Number"));
        if (participantProp.intValue < 1)
            participantProp.intValue = 1;

        // Remove avatar checkbox
        EditorGUILayout.PropertyField(serializedObject.FindProperty("removeAvatar"), new GUIContent("Remove Avatar From This Player"));

        if (!gimmick.removeAvatar)
        {
            // Assign mode: show avatar ID
            DrawAvatarIDField(gimmick);
        }
        else
        {
            EditorGUILayout.HelpBox("When triggered, all avatars assigned to this participant will be removed.", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawAvatarIDField(LuidaAssignAvatarGimmick gimmick)
    {
        // Registry is scoped to the gimmick's own scene — a gimmick in scene X
        // can only assign avatars from X's registry. The active scene may differ
        // when scenes are loaded additively, so we read from gimmick.gameObject.scene
        // rather than SceneManager.GetActiveScene().
        string sceneFolder = AvatarsConfigAssetUtil.SanitizeSceneFolderName(gimmick.gameObject.scene.name);
        string registryPath = sceneFolder != null ? AvatarsConfigAssetUtil.GetRegistryPath(sceneFolder) : null;
        var avatarRegistry = registryPath != null
            ? AssetDatabase.LoadAssetAtPath<AvatarRegistry>(registryPath)
            : null;

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
            string sceneLabel = string.IsNullOrEmpty(gimmick.gameObject.scene.name) ? "(unsaved)" : gimmick.gameObject.scene.name;
            EditorGUILayout.HelpBox(
                $"No avatars registered for scene '{sceneLabel}'. Open LUIDA > Configure avatars with this scene active to add avatars.",
                MessageType.Warning);
        }
    }
}
