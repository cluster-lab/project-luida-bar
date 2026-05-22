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

    /// <summary>
    /// Call this when the window opens or the scene changes.
    /// </summary>
    public static void ReloadSpawnerConfig()
    {
        // No-op — spawner is always message-driven, no persisted config to reload.
    }

    public static void DrawGUI(LuidaAvatarsWindow window)
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        DrawSceneHeader();

        string sceneFolder = AvatarsConfigAssetUtil.GetActiveSceneFolderName();
        if (sceneFolder == null)
        {
            EditorGUILayout.HelpBox(
                "No saved scene is active. Save the current scene first — avatars are stored per-scene under Assets/_Experiment_/Avatars/<scene_name>/.",
                MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        var registry = window.Registry;
        if (registry == null)
        {
            EditorGUILayout.HelpBox(
                $"No AvatarRegistry yet for scene '{sceneFolder}'. Drop a .vrm or humanoid .prefab below to create one.",
                MessageType.Info);
        }

        // Drop zone always rendered — drops create the registry+folder lazily.
        DrawDropZone(registry, sceneFolder, window);
        EditorGUILayout.Space(10);

        if (registry != null)
        {
            DrawAvatarList(registry, window);
            EditorGUILayout.Space(15);
            DrawSpawnerSection(registry);
        }

        EditorGUILayout.EndScrollView();
    }

    private static void DrawSceneHeader()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        string label = string.IsNullOrEmpty(scene.name) ? "(unsaved scene)" : scene.name;
        EditorGUILayout.LabelField($"Active Scene: {label}", EditorStyles.miniBoldLabel);
        EditorGUILayout.Space(4);
    }

    #region Drop Zone

    private static void DrawDropZone(AvatarRegistry registry, string sceneFolder, LuidaAvatarsWindow window)
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
            // Lazy-create the scene's registry on first drop. Avoids leaving an
            // empty AvatarRegistry behind in scenes the user never wired up.
            if (registry == null)
            {
                registry = AvatarsConfigAssetUtil.EnsureRegistryAsset();
                if (registry == null) { evt.Use(); return; }
                window.ReloadForActiveScene();
            }
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

            // Scale mode (locked to ScaleToPlayer for now)
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = false;
            EditorGUILayout.EnumPopup("Scale Mode", AvatarScaleMode.ScaleToPlayer);
            GUI.enabled = true;
            if (entry.scaleMode != AvatarScaleMode.ScaleToPlayer)
            {
                Undo.RecordObject(registry, "Fix Scale Mode");
                entry.scaleMode = AvatarScaleMode.ScaleToPlayer;
                entry.needsRebuild = true;
                EditorUtility.SetDirty(registry);
            }
            var infoIcon = EditorGUIUtility.IconContent("_Help");
            var iconRect = GUILayoutUtility.GetRect(infoIcon, GUIStyle.none, GUILayout.Width(18), GUILayout.Height(18));
            GUI.Label(iconRect, new GUIContent(infoIcon.image,
                "It is not yet available to adjust the player's viewpoint to match the avatar's original size. If needed, use the deprecated avatar configuration form on the LUIDA web console."));
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel++;
            entry.syncHipsY = EditorGUILayout.ToggleLeft("Sync Hips Y Position", entry.syncHipsY);
            if (!entry.syncHipsY)
            {
                entry.hipsYOffset = EditorGUILayout.FloatField("Hips Y Offset", entry.hipsYOffset);
            }
            EditorGUI.indentLevel--;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(registry, "Change Hips Y Settings");
                entry.needsRebuild = true;
                EditorUtility.SetDirty(registry);
            }

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

        }
        else
        {
            EditorGUILayout.HelpBox(
                "No AvatarSpawner in the current scene.\nAdd one to enable avatar spawning at runtime.",
                MessageType.Info);

            if (GUILayout.Button("Add Avatar Spawner to Current Scene", GUILayout.Height(30)))
            {
                AvatarsConfigAssetUtil.InstallSpawnerInActiveScene(registry);
            }
        }
    }

    #endregion
}
