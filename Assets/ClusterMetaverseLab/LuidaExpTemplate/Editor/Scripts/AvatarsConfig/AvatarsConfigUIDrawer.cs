using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;

/// <summary>
/// IMGUI drawer for the LUIDA Avatars editor window.
/// Renders: drop zone, avatar list, spawner installation controls.
/// </summary>
public static class AvatarsConfigUIDrawer
{
    private static Vector2 _scrollPosition;

    // Spawner config state
    private static int _spawnerModeIndex = 0;
    private static int _defaultAvatarIndex = 0;
    private static readonly string[] SpawnerModes = { "messageDriven", "autoAssignOnJoin" };
    private static readonly string[] SpawnerModeLabels = { "Message-driven (for LUIDA state actions)", "Auto-assign on player join" };

    // Persisted config (what's actually saved on disk)
    private static string _persistedMode = null;
    private static string _persistedDefaultAvatarID = null;
    private static bool _spawnerConfigInitialized = false;

    /// <summary>
    /// Call this when the window opens or the scene changes to sync dropdown state
    /// with the actual persisted config on disk.
    /// </summary>
    public static void ReloadSpawnerConfig()
    {
        var (mode, defaultAvatarID) = AvatarsConfigAssetUtil.ReadCurrentSpawnerConfig();
        _persistedMode = mode;
        _persistedDefaultAvatarID = defaultAvatarID;

        _spawnerModeIndex = System.Array.IndexOf(SpawnerModes, mode);
        if (_spawnerModeIndex < 0) _spawnerModeIndex = 0;

        // defaultAvatarIndex will be resolved against the registry in DrawSpawnerSection
        _spawnerConfigInitialized = true;
    }

    public static void DrawGUI(LuidaAvatarsWindow window)
    {
        var registry = window.Registry;
        if (registry == null)
        {
            EditorGUILayout.HelpBox("AvatarRegistry asset not found. It will be created on next enable.", MessageType.Warning);
            return;
        }

        if (!_spawnerConfigInitialized)
            ReloadSpawnerConfig();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        DrawDropZone(registry);
        EditorGUILayout.Space(10);
        DrawAvatarList(registry, window);
        EditorGUILayout.Space(15);
        DrawSpawnerSection(registry);

        EditorGUILayout.EndScrollView();
    }

    #region Drop Zone

    private static void DrawDropZone(AvatarRegistry registry)
    {
        EditorGUILayout.LabelField("Register Avatars", EditorStyles.boldLabel);

        Rect dropRect = GUILayoutUtility.GetRect(0, 60, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, "Drop .vrm file or humanoid .prefab here", EditorStyles.helpBox);

        var evt = Event.current;
        if (evt.type == EventType.DragUpdated && dropRect.Contains(evt.mousePosition))
        {
            bool valid = DragAndDrop.paths.Any(p =>
                p.EndsWith(".vrm", System.StringComparison.OrdinalIgnoreCase) ||
                p.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase));
            if (!valid)
                valid = DragAndDrop.objectReferences.Any(o => o is GameObject);

            DragAndDrop.visualMode = valid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            evt.Use();
        }
        else if (evt.type == EventType.DragPerform && dropRect.Contains(evt.mousePosition))
        {
            DragAndDrop.AcceptDrag();
            AvatarsConfigAssetUtil.HandleDrop(DragAndDrop.objectReferences, DragAndDrop.paths, registry);
            evt.Use();
        }
    }

    #endregion

    #region Avatar List

    private static void DrawAvatarList(AvatarRegistry registry, LuidaAvatarsWindow window)
    {
        EditorGUILayout.LabelField($"Registered Avatars ({registry.entries.Count})", EditorStyles.boldLabel);

        if (registry.entries.Count == 0)
        {
            EditorGUILayout.HelpBox("No avatars registered yet. Drag a .vrm or humanoid prefab into the drop zone above.", MessageType.Info);
            return;
        }

        for (int i = registry.entries.Count - 1; i >= 0; i--)
        {
            var entry = registry.entries[i];
            if (entry == null) continue;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            // Thumbnail
            Texture2D preview = null;
            if (entry.wrapperItemPrefab != null)
                preview = AssetPreview.GetAssetPreview(entry.wrapperItemPrefab);
            if (preview == null && entry.sourceVrmPrefab != null)
                preview = AssetPreview.GetAssetPreview(entry.sourceVrmPrefab);
            if (preview != null)
                GUILayout.Label(preview, GUILayout.Width(48), GUILayout.Height(48));
            else
                GUILayout.Label("", GUILayout.Width(48), GUILayout.Height(48));

            EditorGUILayout.BeginVertical();

            // Avatar ID (editable) — marks dirty on change
            EditorGUI.BeginChangeCheck();
            string newID = EditorGUILayout.TextField("Avatar ID", entry.avatarID);
            if (EditorGUI.EndChangeCheck())
            {
                string sanitized = AvatarsConfigAssetUtil.SanitizeAvatarID(newID);
                if (sanitized != entry.avatarID && registry.FindByID(sanitized) == null)
                {
                    Undo.RecordObject(registry, "Change Avatar ID");
                    entry.avatarID = sanitized;
                    entry.needsRebuild = true;
                    EditorUtility.SetDirty(registry);
                }
            }

            // Display name
            EditorGUI.BeginChangeCheck();
            entry.displayName = EditorGUILayout.TextField("Display Name", entry.displayName);
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(registry);

            // Source prefab (read-only)
            GUI.enabled = false;
            EditorGUILayout.ObjectField("Source Prefab", entry.sourceVrmPrefab, typeof(GameObject), false);
            EditorGUILayout.ObjectField("Wrapper Prefab", entry.wrapperItemPrefab, typeof(GameObject), false);
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            // Bone sync checkboxes — marks needsRebuild on change
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Optional bone groups:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            entry.syncFeetToes = EditorGUILayout.ToggleLeft("Feet / Toes", entry.syncFeetToes, GUILayout.Width(100));
            entry.syncFingers = EditorGUILayout.ToggleLeft("Fingers", entry.syncFingers, GUILayout.Width(80));
            entry.syncJaw = EditorGUILayout.ToggleLeft("Jaw", entry.syncJaw, GUILayout.Width(60));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(registry, "Change Bone Sync Options");
                entry.needsRebuild = true;
                EditorUtility.SetDirty(registry);
            }

            EditorGUILayout.EndHorizontal();

            // Rebuild button — only shown when entry has pending changes
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();

            if (entry.needsRebuild)
            {
                var oldBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.9f, 0.4f);
                if (GUILayout.Button("Rebuild Wrapper (pending changes)", GUILayout.Width(240)))
                {
                    AvatarsConfigAssetUtil.RebuildEntry(entry, registry);
                    entry.needsRebuild = false;
                    EditorUtility.SetDirty(registry);
                    GUI.backgroundColor = oldBg;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    GUIUtility.ExitGUI();
                    return;
                }
                GUI.backgroundColor = oldBg;
            }

            GUILayout.FlexibleSpace();

            var oldColor = GUI.color;
            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Remove", GUILayout.Width(70)))
            {
                if (EditorUtility.DisplayDialog("Remove Avatar",
                    $"Remove avatar '{entry.avatarID}' and delete its wrapper prefab?",
                    "Remove", "Cancel"))
                {
                    AvatarsConfigAssetUtil.RemoveEntry(entry.avatarID, registry);
                    GUI.color = oldColor;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    GUIUtility.ExitGUI();
                    return;
                }
            }
            GUI.color = oldColor;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }
    }

    #endregion

    #region Spawner Section

    private static void DrawSpawnerSection(AvatarRegistry registry)
    {
        EditorGUILayout.LabelField("Avatar Spawner (Current Scene)", EditorStyles.boldLabel);

        var existingSpawner = AvatarsConfigAssetUtil.FindSpawnerInScene();

        if (existingSpawner != null)
        {
            EditorGUILayout.HelpBox("LUIDA-AvatarSpawner is present in the current scene.", MessageType.Info);

            GUI.enabled = false;
            EditorGUILayout.ObjectField("Spawner Object", existingSpawner, typeof(GameObject), true);
            GUI.enabled = true;

            EditorGUILayout.Space(4);

            // Resolve _defaultAvatarIndex from persisted ID
            var ids = registry.GetAvatarIDs();
            if (_persistedDefaultAvatarID != null && ids.Length > 0)
            {
                int idx = System.Array.IndexOf(ids, _persistedDefaultAvatarID);
                if (idx >= 0) _defaultAvatarIndex = idx;
            }

            // Mode dropdown
            _spawnerModeIndex = EditorGUILayout.Popup("Spawner Mode", _spawnerModeIndex, SpawnerModeLabels);

            if (SpawnerModes[_spawnerModeIndex] == "autoAssignOnJoin")
            {
                if (ids.Length > 0)
                {
                    _defaultAvatarIndex = Mathf.Clamp(_defaultAvatarIndex, 0, ids.Length - 1);
                    _defaultAvatarIndex = EditorGUILayout.Popup("Default Avatar", _defaultAvatarIndex, ids);
                }
                else
                {
                    EditorGUILayout.HelpBox("Register at least one avatar to use auto-assign mode.", MessageType.Warning);
                }
            }

            // Detect if current UI state differs from persisted config
            string currentMode = SpawnerModes[_spawnerModeIndex];
            string currentDefaultID = null;
            if (currentMode == "autoAssignOnJoin" && ids.Length > 0)
                currentDefaultID = ids[Mathf.Clamp(_defaultAvatarIndex, 0, ids.Length - 1)];

            bool configChanged = currentMode != _persistedMode
                || currentDefaultID != _persistedDefaultAvatarID;

            if (configChanged)
            {
                EditorGUILayout.Space(4);
                var oldBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.9f, 0.4f);
                if (GUILayout.Button("Apply Spawner Config Changes"))
                {
                    AvatarsConfigAssetUtil.UpdateSpawnerConfig(currentMode, currentDefaultID);
                    _persistedMode = currentMode;
                    _persistedDefaultAvatarID = currentDefaultID;
                }
                GUI.backgroundColor = oldBg;
            }

        }
        else
        {
            EditorGUILayout.HelpBox(
                "No AvatarSpawner in the current scene.\nAdd one to enable avatar spawning at runtime.",
                MessageType.Info);

            EditorGUILayout.Space(4);
            _spawnerModeIndex = EditorGUILayout.Popup("Spawner Mode", _spawnerModeIndex, SpawnerModeLabels);

            if (SpawnerModes[_spawnerModeIndex] == "autoAssignOnJoin")
            {
                var ids = registry.GetAvatarIDs();
                if (ids.Length > 0)
                {
                    _defaultAvatarIndex = Mathf.Clamp(_defaultAvatarIndex, 0, ids.Length - 1);
                    _defaultAvatarIndex = EditorGUILayout.Popup("Default Avatar", _defaultAvatarIndex, ids);
                }
                else
                {
                    EditorGUILayout.HelpBox("Register at least one avatar first.", MessageType.Warning);
                }
            }

            if (GUILayout.Button("Add Avatar Spawner to Current Scene", GUILayout.Height(30)))
            {
                string defaultID = null;
                if (SpawnerModes[_spawnerModeIndex] == "autoAssignOnJoin")
                {
                    var ids = registry.GetAvatarIDs();
                    if (ids.Length > 0)
                        defaultID = ids[_defaultAvatarIndex];
                }
                AvatarsConfigAssetUtil.InstallSpawnerInActiveScene(
                    SpawnerModes[_spawnerModeIndex], defaultID, registry);

                // Update persisted state after install
                _persistedMode = SpawnerModes[_spawnerModeIndex];
                _persistedDefaultAvatarID = defaultID;
            }
        }
    }

    #endregion
}
