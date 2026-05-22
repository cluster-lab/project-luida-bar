#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// Standalone editor window for configuring the LUIDA Data Collector.
///
/// Layout:
///   Mode toggle:  [Builder] [Code Mode]
///   Section A:    "Collected data items" — labels & types that the gimmick
///                 and the SendDataToCollector action can write.
///   Section B:    "Fields to be saved" — what shows up in the uploaded JSON.
///   Suffix:       optional Custom JS appended after the fields dict.
///
/// In Code mode the field list is replaced by a raw JS textarea; Section A
/// stays editable (it drives the CCK sync header).
///
/// Opened from the menu: LUIDA → Configure data collector.
/// </summary>
public class DataCollectorConfigTab : EditorWindow
{
    private const string ExperimentScenesPath = "Assets/_Experiment_/Scenes/";

    private LuidaDataCollectorConfig _config;
    private ReorderableList _labelList;
    private ReorderableList _fieldList;
    private Vector2 _scrollPosition;
    private string _lastTrackedScene;

    [MenuItem("LUIDA/Configure data collector")]
    public static void ShowWindow()
    {
        GetWindow<DataCollectorConfigTab>("LUIDA Data Collector");
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("LUIDA Data Collector");
        EditorSceneManager.activeSceneChangedInEditMode -= HandleSceneChanged;
        EditorSceneManager.activeSceneChangedInEditMode += HandleSceneChanged;
        ReloadForActiveScene();
    }

    private void OnDisable()
    {
        EditorSceneManager.activeSceneChangedInEditMode -= HandleSceneChanged;
    }

    private void OnDestroy()
    {
        if (_config != null) SaveAndCombine();
    }

    private void HandleSceneChanged(UnityEngine.SceneManagement.Scene previous, UnityEngine.SceneManagement.Scene current)
    {
        if (_config != null) SaveAndCombine();
        ReloadForActiveScene();
        Repaint();
    }

    private void ReloadForActiveScene()
    {
        _config = null;
        _labelList = null;
        _fieldList = null;
        _lastTrackedScene = EditorSceneManager.GetActiveScene().name;
        EnsureConfigLoaded();
        BuildLabelList();
        BuildFieldList();
    }

    private void EnsureConfigLoaded()
    {
        if (_config != null) return;
        if (!IsExperimentSceneActive()) return;
        _config = DataCollectorCreateMenu.FindOrCreateBuilderConfig();
        if (_config != null) LuidaDataCollectorConfigMigrator.Migrate(_config);
    }

    private bool IsExperimentSceneActive()
    {
        string scenePath = EditorSceneManager.GetActiveScene().path;
        return !string.IsNullOrEmpty(scenePath) && scenePath.StartsWith(ExperimentScenesPath);
    }

    // ─── OnGUI ──────────────────────────────────────────────────────────

    private void OnGUI()
    {
        string activeSceneName = EditorSceneManager.GetActiveScene().name;
        if (activeSceneName != _lastTrackedScene)
        {
            ReloadForActiveScene();
        }

        if (!IsExperimentSceneActive())
        {
            EditorGUILayout.HelpBox(
                "Open a LUIDA experiment scene (under Assets/_Experiment_/Scenes/) to configure the data collector.\n" +
                "Use LUIDA → Configure experiment automation to create one.",
                MessageType.Info);
            return;
        }

        EnsureConfigLoaded();
        if (_config == null)
        {
            EditorGUILayout.HelpBox(
                "Could not locate or create the DataCollector config asset. " +
                "Save the scene first, then reopen this window.",
                MessageType.Warning);
            return;
        }

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        DrawModeToggle();
        EditorGUILayout.Space();
        DrawDataCollectorPresenceCheck();
        EditorGUILayout.Space();

        DrawLabelSection();    // Section A — always visible in both modes
        EditorGUILayout.Space();

        if (_config.useCustomCodeMode)
        {
            DrawCodeModeUI();
        }
        else
        {
            DrawBuilderModeUI();
        }

        EditorGUILayout.Space();
        DrawSaveActions();

        EditorGUILayout.EndScrollView();
    }

    // ─── Mode toggle ────────────────────────────────────────────────────

    private void DrawModeToggle()
    {
        EditorGUILayout.BeginHorizontal();
        bool isBuilder = !_config.useCustomCodeMode;

        // Plain Buttons (not Toggles) so a click unambiguously "requests this mode".
        // The active one is highlighted via backgroundColor.
        GUI.backgroundColor = isBuilder ? new Color(0.4f, 0.8f, 0.5f) : Color.white;
        if (GUILayout.Button("  Builder  ", GUILayout.Height(24), GUILayout.Width(110)))
        {
            if (!isBuilder) SwitchToBuilder();
        }
        GUI.backgroundColor = !isBuilder ? new Color(0.95f, 0.7f, 0.3f) : Color.white;
        if (GUILayout.Button("  Code Mode  ", GUILayout.Height(24), GUILayout.Width(110)))
        {
            if (isBuilder) SwitchToCode();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField(
            _config.useCustomCodeMode ? "Editing raw JS body" : "Editing fields visually",
            EditorStyles.miniLabel,
            GUILayout.Width(160));
        EditorGUILayout.EndHorizontal();
    }

    private void SwitchToCode()
    {
        if (!EditorUtility.DisplayDialog(
            "Switch to Code Mode?",
            "Switching to Code Mode will seed the raw JS area with the current Builder output. " +
            "After that, the Builder field list will no longer drive saved JS — your raw JS will. " +
            "Switch back later to discard the raw JS and use Builder again.",
            "Switch", "Cancel")) return;

        Undo.RecordObject(_config, "Switch to Code Mode");
        _config.rawJs = LuidaDataCollectorJsGenerator.GenerateBuilderBodyOnly(_config);
        _config.useCustomCodeMode = true;
        EditorUtility.SetDirty(_config);
    }

    private void SwitchToBuilder()
    {
        if (!EditorUtility.DisplayDialog(
            "Switch to Builder Mode?",
            "Switching back to Builder Mode will discard your hand-edited raw JS. " +
            "The field list will drive the saved JS instead. Continue?",
            "Discard raw JS", "Cancel")) return;

        Undo.RecordObject(_config, "Switch to Builder Mode");
        _config.useCustomCodeMode = false;
        _config.rawJs = string.Empty;
        EditorUtility.SetDirty(_config);
    }

    // ─── Section A — Collected data items ───────────────────────────────

    private void BuildLabelList()
    {
        if (_config == null) return;
        _labelList = new ReorderableList(_config.collectedLabels, typeof(CollectedLabel), true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Label                          Type"),
            elementHeight = CalculatorFieldDrawers.LineH + 6f,
            drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (index < 0 || index >= _config.collectedLabels.Count) return;
                var entry = _config.collectedLabels[index];
                if (entry == null) return;

                float typeWidth = 110f, badgeWidth = 46f, gap = 6f;
                float labelWidth = rect.width - typeWidth - badgeWidth - 2 * gap;

                Rect labelRect = new Rect(rect.x, rect.y + 2, labelWidth, CalculatorFieldDrawers.LineH);
                Rect typeRect  = new Rect(labelRect.xMax + gap, rect.y + 2, typeWidth, CalculatorFieldDrawers.LineH);
                Rect badgeRect = new Rect(typeRect.xMax + gap, rect.y + 2, badgeWidth, CalculatorFieldDrawers.LineH);

                EditorGUI.BeginChangeCheck();
                string newLabel = EditorGUI.TextField(labelRect, entry.label ?? string.Empty);
                if (EditorGUI.EndChangeCheck() && newLabel != entry.label)
                {
                    Undo.RecordObject(_config, "Edit collected label");
                    entry.label = newLabel;
                    EditorUtility.SetDirty(_config);
                }

                EditorGUI.BeginChangeCheck();
                var picked = (CollectedValueType)EditorGUI.EnumPopup(typeRect, entry.type);
                if (EditorGUI.EndChangeCheck() && picked != entry.type)
                {
                    Undo.RecordObject(_config, "Edit collected type");
                    entry.type = picked;
                    EditorUtility.SetDirty(_config);
                }

                var (badgeText, color) = CalculatorTypeBadge.ForCollectedValueType(entry.type);
                CalculatorTypeBadge.Draw(badgeRect, badgeText, color);
            },
            onAddCallback = list =>
            {
                Undo.RecordObject(_config, "Add collected label");
                _config.collectedLabels.Add(new CollectedLabel
                {
                    label = $"label{_config.collectedLabels.Count + 1}",
                    type = CollectedValueType.Integer,
                });
                EditorUtility.SetDirty(_config);
            },
            onRemoveCallback = list =>
            {
                if (list.index < 0 || list.index >= _config.collectedLabels.Count) return;
                Undo.RecordObject(_config, "Remove collected label");
                _config.collectedLabels.RemoveAt(list.index);
                EditorUtility.SetDirty(_config);
            },
            onReorderCallback = list => EditorUtility.SetDirty(_config),
        };
    }

    private void DrawLabelSection()
    {
        GUILayout.Label("Collected data items", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox(
            "Labels & types that LuidaSendDataToCollectorGimmick and the state-listening " +
            "\"Push data to collector\" action can write into $.groupState.collectedData. " +
            "These appear as dropdown options in the field sources below and in the gimmick inspector.\n\n" +
            "⚠ String-typed items can only be set via the state-listening action — the data-collection " +
            "fake gimmick cannot transport strings through CCK (ConstantValue has no string slot).",
            MessageType.Info);

        if (_labelList == null) BuildLabelList();
        if (_labelList != null) _labelList.DoLayoutList();

        WarnLabelIssues();
    }

    private void WarnLabelIssues()
    {
        var seen = new HashSet<string>();
        var collisions = new HashSet<string>();
        var invalid = new List<string>();
        var stringLabels = new List<string>();
        foreach (var l in _config.collectedLabels)
        {
            if (l == null) continue;
            string n = l.label ?? string.Empty;
            if (!string.IsNullOrEmpty(n) && !seen.Add(n)) collisions.Add(n);
            if (!string.IsNullOrEmpty(n) && !LuidaDataCollectorJsGenerator.IsValidFieldName(n)) invalid.Add(n);
            if (!string.IsNullOrEmpty(n) && l.type == CollectedValueType.String) stringLabels.Add(n);
        }
        if (collisions.Count > 0)
            EditorGUILayout.HelpBox("Duplicate labels: " + string.Join(", ", collisions), MessageType.Error);
        if (invalid.Count > 0)
            EditorGUILayout.HelpBox(
                "Invalid label names (must be letters/digits/underscores; cannot start with a digit): " +
                string.Join(", ", invalid),
                MessageType.Warning);
        if (stringLabels.Count > 0)
            EditorGUILayout.HelpBox(
                $"String-typed labels are write-only via the state-listening \"Push data to collector\" action — " +
                $"the data-collection fake gimmick can't transport strings through CCK ConstantValue. " +
                $"Affected: {string.Join(", ", stringLabels)}.",
                MessageType.Warning);
    }

    // ─── Section B — Fields to be saved (Builder mode) ──────────────────

    private void DrawBuilderModeUI()
    {
        GUILayout.Label("Fields to be saved", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox(
            "Each row becomes one entry of the JSON object uploaded per recording tick. " +
            "Pick a Source per field — Builder maps to ClusterScript types: " +
            "Bool (green), Int/Num (blue), Vec2/Vec3 (purple), Str (orange). " +
            "Use Arithmetic for + − × ÷ on multiple operands, Conditional for if/else.",
            MessageType.Info);

        DrawFieldList();
    }

    private void BuildFieldList()
    {
        if (_config == null) return;
        _fieldList = new ReorderableList(_config.fields, typeof(DataCollectorField), true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Name           Source                       Type"),
            elementHeightCallback = index =>
            {
                if (index < 0 || index >= _config.fields.Count) return CalculatorFieldDrawers.LineStep;
                return CalculatorFieldDrawers.ComputeFieldHeight(_config.fields[index]);
            },
            drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (index < 0 || index >= _config.fields.Count) return;
                var field = _config.fields[index];
                if (field == null) return;

                int captured = index;
                CalculatorFieldDrawers.DrawField(rect, field, _config, () =>
                {
                    Undo.RecordObject(_config, "Remove field");
                    if (captured >= 0 && captured < _config.fields.Count)
                        _config.fields.RemoveAt(captured);
                    EditorUtility.SetDirty(_config);
                    GUIUtility.ExitGUI();
                });
            },
            onAddCallback = list =>
            {
                Undo.RecordObject(_config, "Add field");
                string defaultLabel = _config.collectedLabels.Count > 0 ? _config.collectedLabels[0].label : "";
                _config.fields.Add(new DataCollectorField
                {
                    fieldName = $"field{_config.fields.Count + 1}",
                    source = DataFieldSourceKind.Collected,
                    collectedLabel = defaultLabel,
                });
                EditorUtility.SetDirty(_config);
            },
            onRemoveCallback = list =>
            {
                if (list.index < 0 || list.index >= _config.fields.Count) return;
                Undo.RecordObject(_config, "Remove field");
                _config.fields.RemoveAt(list.index);
                EditorUtility.SetDirty(_config);
            },
            onReorderCallback = list => EditorUtility.SetDirty(_config),
        };
    }

    private void DrawFieldList()
    {
        if (_fieldList == null) BuildFieldList();
        if (_fieldList == null) return;
        _fieldList.DoLayoutList();

        var seen = new HashSet<string>();
        var collisions = new HashSet<string>();
        var invalid = new List<string>();
        foreach (var f in _config.fields)
        {
            if (f == null) continue;
            string n = f.fieldName ?? string.Empty;
            if (!string.IsNullOrEmpty(n) && !seen.Add(n)) collisions.Add(n);
            if (!string.IsNullOrEmpty(n) && !LuidaDataCollectorJsGenerator.IsValidFieldName(n)) invalid.Add(n);
        }
        if (collisions.Count > 0)
            EditorGUILayout.HelpBox("Duplicate field names: " + string.Join(", ", collisions), MessageType.Error);
        if (invalid.Count > 0)
            EditorGUILayout.HelpBox(
                "Invalid field names (must be letters/digits/underscores; cannot start with a digit): " +
                string.Join(", ", invalid) + ". These fields are skipped during JS generation.",
                MessageType.Warning);

        if (LuidaAutomationStatus.IsActiveForActiveScene() &&
            LuidaDataCollectorJsGenerator.HasUserStateLogField(_config))
        {
            EditorGUILayout.HelpBox(
                $"Field name \"{LuidaDataCollectorJsGenerator.AutoStateLogFieldName}\" collides with the auto-injected " +
                "state-machine log (LUIDA automation is active in this scene). Auto-injection is skipped while this entry " +
                "exists — your field's expression is used instead. Rename or remove this entry to get the auto-injected " +
                "state log back.",
                MessageType.Warning);
        }
    }

    // ─── Code mode ──────────────────────────────────────────────────────

    private void DrawCodeModeUI()
    {
        GUILayout.Label("Calculator body (Code mode)", EditorStyles.largeLabel);
        EditorGUILayout.HelpBox(
            "This is the full body of saveData(). The CCK sync header is still auto-prepended " +
            "on Save for registered labels — do not edit between the AUTO-GENERATED markers " +
            "in the written .js file. End with `return { ... };`.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        var newRaw = EditorGUILayout.TextArea(_config.rawJs ?? string.Empty, GUILayout.MinHeight(360));
        if (EditorGUI.EndChangeCheck() && newRaw != _config.rawJs)
        {
            Undo.RecordObject(_config, "Edit raw calculator JS");
            _config.rawJs = newRaw;
            EditorUtility.SetDirty(_config);
        }
    }

    // ─── Top-of-window status ───────────────────────────────────────────

    private void DrawDataCollectorPresenceCheck()
    {
        var collector = Object.FindObjectOfType<LuidaDataCollector>();
        if (collector == null)
        {
            EditorGUILayout.HelpBox(
                "No LUIDA-DataCollector is present in this scene. " +
                "Use GameObject → LUIDA → Data Collector to add one. " +
                "Until then, any gimmick writes are silent no-ops.",
                MessageType.Warning);

            if (GUILayout.Button("Create LUIDA-DataCollector in this scene", GUILayout.Height(24)))
            {
                DataCollectorCreateMenu.CreateDataCollectorInScene(registerUndo: true, selectObject: false);
                ReloadForActiveScene();
                GUIUtility.ExitGUI();
            }
        }
    }

    // ─── Save / preview ─────────────────────────────────────────────────

    private void DrawSaveActions()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Preview generated JS", GUILayout.Height(26)))
        {
            string preview = LuidaDataCollectorJsGenerator.Generate(_config);
            OpenPreviewWindow(preview);
        }
        if (GUILayout.Button("Save & Combine", GUILayout.Height(26)))
        {
            SaveAndCombine();
        }
        EditorGUILayout.EndHorizontal();
    }

    private static void OpenPreviewWindow(string content)
    {
        if (EditorWindow.HasOpenInstances<TextAreaOverlayWindow>()) return;
        var mouse = Event.current != null ? GUIUtility.GUIToScreenPoint(Event.current.mousePosition)
                                          : new Vector2(400, 400);
        Rect popupRect = new Rect(mouse.x - 320, mouse.y, 640, 480);
        var style = new GUIStyle(EditorStyles.textArea)
        {
            wordWrap = false,
            richText = false,
            font = EditorStyles.miniFont,
        };
        TextAreaOverlayWindow.Show(popupRect, content, _ => { /* read-only preview */ }, style);
    }

    private void SaveAndCombine()
    {
        if (_config == null) return;
        DataCollectorJsSaver.WriteAndCombine(_config);
        AssetDatabase.SaveAssets();
    }
}
#endif
