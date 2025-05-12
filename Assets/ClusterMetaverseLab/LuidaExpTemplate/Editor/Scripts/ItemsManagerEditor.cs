using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ClusterVR.CreatorKit.Item.Implements;

public class ItemsManagerEditor : EditorWindow
{
    // ——— Caching & ReorderableList storage ———
    private bool _needsRebuild = true;
    private string[] _cachedStateNames = Array.Empty<string>();
    private GameObject[] _cachedItems = Array.Empty<GameObject>();
    private Dictionary<string, ReorderableList> _reorderableLists = new Dictionary<string, ReorderableList>();

    // ——— Existing fields ———
    private static StateListeningAction[] AvailableStateListeningActions = {
        new StateListeningAction("Show item", "$.setStateCompat('this', 'exp_showItem', true);"),
        new StateListeningAction("Hide item", "$.setStateCompat('this', 'exp_showItem', false);"),
        new StateListeningAction("To next state", "$.sendSignalCompat('this', 'state_triggerTransition');"),
        new StateListeningAction("Record custom data", "$.sendSignalCompat('this', 'exp_recordCustomData');"),
        new StateListeningAction("Upload recorded data", "$.sendSignalCompat('this', 'exp_uploadCustomData');"),
        new StateListeningAction("Set text", "$.subNode('Text').setText('xxx');"),
        new StateListeningAction("Sleep", "0"),
    };

    private string newItemName = string.Empty;
    private GameObject referenceObject = null;

    private const string PrefabPath               = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/StateManagement/StateListeningItem.prefab";
    private const string ScriptFolderFormat       = "Assets/_Experiment_/Scripts/StateManagement/{0}";
    private const string ScriptTemplatePath       = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Scripts/StateManagement/StateListeningItemTemplate.js";
    private const string WrapperPrefabPath        = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ExpTemplateRequiredObjects.prefab";
    private const string ConditionManagerPrefabPath = "Assets/ClusterMetaverseLab/LuidaExpTemplate/Runtime/Prefabs/ConditionManagement/ConditionManager.prefab";

    private List<GameObject> stateListeningItems = new List<GameObject>();
    private StateList stateList = null;
    private SerializedObject serializedStateList = null;
    private SerializedProperty statesProperty = null;
    private Dictionary<GameObject, List<StateListener>> stateListenersByItem = new Dictionary<GameObject, List<StateListener>>();
    private Dictionary<GameObject, string> otherImplementationByItem = new Dictionary<GameObject, string>();

    private Vector2 scrollPosition;
    private Vector2 scrollPositionX;
    private Vector2 scrollPositionY;
    private int selectedActionIndex = 0;
    private Dictionary<List<StateListenerAction>, bool> isAddingActionState = new Dictionary<List<StateListenerAction>, bool>();
    private string setTextInput = string.Empty;
    private double sleepTimeInput = 0.0;
    private bool isSubscribed = false;

    // [MenuItem("Window/Luida Editor/Items Manager")]
    // public static void ShowWindow() => GetWindow<ItemsManagerEditor>("Items Manager");

    #region Unity Callbacks

    public void OnEnable()
    {
        // mark for rebuild
        _needsRebuild = true;
        // listen for hierarchy/project changes
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        EditorApplication.projectChanged   += OnProjectChanged;
        // subscribe once
        if (!isSubscribed)
        {
            TabbedEditor.OnEditorClosed             += ApplyAssetsToScripts;
            TabbedEditor.OnItemsManagerTabLostFocus += ApplyAssetsToScripts;
            isSubscribed = true;
        }
    }

    public void OnDisable()
    {
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        EditorApplication.projectChanged   -= OnProjectChanged;
        ApplyAssetsToScripts();
        if (isSubscribed)
        {
            TabbedEditor.OnEditorClosed             -= ApplyAssetsToScripts;
            TabbedEditor.OnItemsManagerTabLostFocus -= ApplyAssetsToScripts;
            isSubscribed = false;
        }
    }

    private void OnHierarchyChanged() => _needsRebuild = true;
    private void OnProjectChanged()   => _needsRebuild = true;

    public void OnLostFocus() => ApplyAssetsToScripts();
    public void OnDestroy()   => Debug.Log("ItemsManagerEditor destroyed");

    #endregion

    public void OnGUI()
    {
        // 1) Rebuild caches only when needed
        if (_needsRebuild)
        {
            RefreshStateListeningItems();
            _cachedStateNames = stateList != null
                ? stateList.States.Select(s => s.StateName).ToArray()
                : Array.Empty<string>();
            _cachedItems = stateListeningItems.ToArray();
            SetupReorderableLists();
            _needsRebuild = false;
        }

        // 2) If no StateList, early-out
        if (stateList == null)
        {
            EditorGUILayout.HelpBox(
                "No StateList asset found for this scene. Create one under the StateList tab.",
                MessageType.Warning
            );
            return;
        }

        EditorGUI.BeginChangeCheck();

        // 3) Add-item controls
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("New Item Name", GUILayout.Width(120));
        newItemName = EditorGUILayout.TextField(newItemName, GUILayout.Width(180));
        EditorGUI.BeginDisabledGroup(
            string.IsNullOrEmpty(newItemName) ||
            stateListeningItems.Any(i => i.name == newItemName)
        );
        if (GUILayout.Button("+ Add state-listening item", GUILayout.Width(180)))
            CreateStateListeningItem(referenceObject);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10);

        // 4) Scrollable Y
        scrollPositionY = EditorGUILayout.BeginScrollView(
            scrollPositionY, false, true,
            GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true)
        );

        EditorGUILayout.BeginHorizontal();

        // 5a) State names column
        EditorGUILayout.BeginVertical(GUILayout.Width(120));
        EditorGUILayout.LabelField("State \\ Item", EditorStyles.boldLabel, GUILayout.MinHeight(30));
        foreach (var name in _cachedStateNames)
        {
            EditorGUILayout.BeginVertical(
                GUI.skin.box,
                GUILayout.MinHeight(60),
                GUILayout.ExpandHeight(false)
            );
            EditorGUILayout.LabelField(name, GUILayout.ExpandHeight(true));
            GUILayout.Space(50);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();

        // 5b) Items columns (horizontal scroll)
        scrollPositionX = EditorGUILayout.BeginScrollView(
            scrollPositionX, true, false,
            GUILayout.ExpandWidth(true),
            GUILayout.ExpandHeight(true)
        );
        EditorGUILayout.BeginHorizontal();
        foreach (var item in _cachedItems)
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(200));
            EditorGUILayout.LabelField(item.name, EditorStyles.boldLabel, GUILayout.MinHeight(30));

            for (int si = 0; si < _cachedStateNames.Length; si++)
            {
                EditorGUILayout.BeginVertical(
                    GUI.skin.box,
                    GUILayout.MinHeight(60),
                    GUILayout.ExpandHeight(false)
                );

                stateListenersByItem.TryGetValue(item, out var list);
                var listener = list?.FirstOrDefault(l => l.stateID == si);

                if (listener != null)
                {
                    DrawReorderableList(item, si, "OnStateStart", "On State Start");
                    DrawReorderableList(item, si, "DuringState",  "During State");
                    DrawReorderableList(item, si, "OnStateExit",  "On State End");
                }
                else
                {
                    if (GUILayout.Button("Add", GUILayout.Height(20)))
                        AddStateListener(si, item);
                }

                GUILayout.Space(50);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();

        // 6) Save if user changed something
        if (EditorGUI.EndChangeCheck())
        {
            // SaveAllItemsToAssets();
        }
    }

    #region ReorderableList Setup & Draw

    private void SetupReorderableLists()
    {
        _reorderableLists.Clear();
        foreach (var item in stateListeningItems)
        {
            if (!stateListenersByItem.TryGetValue(item, out var listeners)) continue;
            foreach (var listener in listeners)
            {
                CreateReorderableList(item, listener.stateID, listener.onStateStartedActions, "On State Start", "OnStateStart");
                CreateReorderableList(item, listener.stateID, listener.duringStateActions,    "During State",  "DuringState");
                CreateReorderableList(item, listener.stateID, listener.onStateExitedActions,  "On State End",  "OnStateExit");
            }
        }
    }

    private void CreateReorderableList(GameObject item, int stateID, List<StateListenerAction> actions, string header, string keySuffix)
    {
        var key = $"{item.GetInstanceID()}_{stateID}_{keySuffix}";
        var rl = new ReorderableList(actions, typeof(StateListenerAction), true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, header, EditorStyles.boldLabel),
            drawElementCallback = (rect, index, active, focused) =>
            {
                var action = actions[index];
                var label = action.predefinedAction.actionType == "Sleep"
                    ? $"Sleep {action.predefinedAction.codeSnippet} seconds"
                    : action.GetActionLabel();
                EditorGUI.LabelField(rect, label);
            }
        };
        rl.onAddCallback = list =>
        {
            actions.Add(new StateListenerAction());
            SaveItemToAsset(item);
            _needsRebuild = true;
        };
        rl.onRemoveCallback = list =>
        {
            actions.RemoveAt(list.index);
            SaveItemToAsset(item);
        };
        rl.onReorderCallback = list => SaveItemToAsset(item);

        _reorderableLists[key] = rl;
    }

    private void DrawReorderableList(GameObject item, int stateID, string keySuffix, string header)
    {
        var key = $"{item.GetInstanceID()}_{stateID}_{keySuffix}";
        if (_reorderableLists.TryGetValue(key, out var rl))
        {
            rl.DoLayoutList();
        }
    }

    #endregion

    #region Unchanged helper methods

    private void RefreshStateListeningItems()
    {
        stateListeningItems.Clear();
        stateListenersByItem.Clear();
        otherImplementationByItem.Clear();

        var allObjs = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjs)
        {
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            if (prefab != null && AssetDatabase.GetAssetPath(prefab) == PrefabPath)
                stateListeningItems.Add(obj);
        }

        string sceneName = SceneManager.GetActiveScene().name;
        string folder = string.Format(ScriptFolderFormat, sceneName) + "/StateListeners";

        foreach (var item in stateListeningItems)
        {
            string assetPath = Path.Combine(folder, item.name + ".asset");
            var data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(assetPath);
            if (data != null)
            {
                stateListenersByItem[item] = data.stateListeners.ToList();
                otherImplementationByItem[item] = data.otherImplementation;
            }
            else
            {
                stateListenersByItem[item] = new List<StateListener>();
                otherImplementationByItem[item] = string.Empty;
            }
        }

        string listPath = $"Assets/_Experiment_/Settings/StateList/{sceneName}.asset";
        stateList = AssetDatabase.LoadAssetAtPath<StateList>(listPath);
        if (stateList != null)
        {
            serializedStateList = new SerializedObject(stateList);
            statesProperty = serializedStateList.FindProperty("States");
        }
    }

    private void AddStateListener(int stateIndex, GameObject item)
    {
        if (!stateListenersByItem.ContainsKey(item))
            stateListenersByItem[item] = new List<StateListener>();

        if (stateListenersByItem[item].Any(l => l.stateID == stateIndex))
        {
            EditorUtility.DisplayDialog("Error", $"Listener for state {stateIndex} already exists on {item.name}.", "OK");
            return;
        }

        stateListenersByItem[item].Add(new StateListener { stateID = stateIndex });
        SaveItemToAsset(item);
    }

    private void CreateStateListeningItem(GameObject reference)
    {
        if (string.IsNullOrEmpty(newItemName)) return;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = newItemName;
        Undo.RegisterCreatedObjectUndo(go, "Create StateListeningItem");

        EnableAccessToConditions(go);

        string scene = SceneManager.GetActiveScene().name;
        string scriptFolder = string.Format(ScriptFolderFormat, scene);
        if (!AssetDatabase.IsValidFolder(scriptFolder))
            Directory.CreateDirectory(scriptFolder);

        string jsPath = $"{scriptFolder}/{newItemName}.js";
        AssetDatabase.CopyAsset(ScriptTemplatePath, jsPath);
        AssetDatabase.Refresh();

        RefreshStateListeningItems();
        SaveItemToAsset(go);

        newItemName = string.Empty;
        referenceObject = null;
    }

    private JavaScriptAsset GetClusterScriptFromItem(GameObject item)
    {
        var combiner = item.GetComponent<ScriptableClusterScriptCombiner>();
        var scripts = combiner.GetClusterScripts();
        return scripts.Count > 1 ? scripts[1] as JavaScriptAsset : null;
    }

    private string GenerateStateFunction(string name, Func<StateListener, List<StateListenerAction>> sel, string extra = "")
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"function {name}({extra}) {{");
        sb.AppendLine("  const STATE_ID = $.state.state_id;");
        sb.AppendLine("  const CONDITION = $.groupState.currentCondition;");
        foreach (var kv in stateListenersByItem)
        {
            foreach (var lst in kv.Value)
            {
                var acts = sel(lst);
                if (acts.Count == 0) continue;
                sb.AppendLine($"  if (STATE_ID === {lst.stateID}) {{");
                foreach (var a in acts)
                    sb.AppendLine($"    {a.GetActionContent()}");
                sb.AppendLine("  }");
            }
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    private string GenerateActionsObjects()
    {
        return
            GenerateActionsObject(l => l.onStateStartedActions, "stateEnterActions") + "\n" +
            GenerateActionsObject(l => l.duringStateActions,    "duringStateActions") + "\n" +
            GenerateActionsObject(l => l.onStateExitedActions,  "stateExitActions");
    }

    private string GenerateActionsObject(Func<StateListener, List<StateListenerAction>> sel, string objName)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"const {objName} = {{");
        foreach (var kv in stateListenersByItem)
        {
            foreach (var lst in kv.Value)
            {
                var acts = sel(lst);
                if (acts.Count == 0) continue;
                sb.AppendLine($"    {lst.stateID}: [");
                foreach (var a in acts)
                    sb.AppendLine($"        {GenerateActionObject(a)},");
                sb.AppendLine("    ],");
            }
        }
        sb.AppendLine("};");
        return sb.ToString();
    }

    private string GenerateActionObject(StateListenerAction action)
    {
        if (action.predefinedAction.actionType.Equals("Sleep", StringComparison.OrdinalIgnoreCase))
            return $"{{ type: \"sleep\", value: {action.predefinedAction.codeSnippet} }}";

        var code = string.IsNullOrEmpty(action.customAction)
            ? action.predefinedAction.codeSnippet
            : action.customAction;
        code = code.Trim().Replace("\n", "\n            ");
        return $"{{ type: \"exec\", action: () => {{\n            {code}\n        }} }}";
    }

    private string GetSetTextValue(string content)
    {
        var m = Regex.Match(content, @"\.setText\(`([^`]*)`\)");
        return m.Success ? m.Groups[1].Value : string.Empty;
    }

    private void EnableAccessToConditions(GameObject item)
    {
        var member = item.GetComponent<ItemGroupMember>();
        var wrapper = Resources.FindObjectsOfTypeAll<GameObject>()
            .FirstOrDefault(o => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(o) == WrapperPrefabPath);
        if (member == null || wrapper == null) return;
        var host = wrapper.GetComponentInChildren<ItemGroupHost>();
        if (host == null) return;
        var so = new SerializedObject(member);
        var prop = so.FindProperty("host");
        prop.objectReferenceValue = host;
        so.ApplyModifiedProperties();
    }

    private void SaveItemToAsset(GameObject item)
    {
        string scene = SceneManager.GetActiveScene().name;
        string folder = string.Format(ScriptFolderFormat, scene);
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        var lines = new[] {
            GenerateActionsObjects(),
            otherImplementationByItem.GetValueOrDefault(item, "")
        };
        string jsPath = Path.Combine(folder, item.name + ".js");
        File.WriteAllText(jsPath, string.Join("\n", lines));
        AssetDatabase.ImportAsset(jsPath);

        string assetFolder = folder + "/StateListeners";
        if (!Directory.Exists(assetFolder)) Directory.CreateDirectory(assetFolder);

        string assetPath = Path.Combine(assetFolder, item.name + ".asset");
        var data = AssetDatabase.LoadAssetAtPath<StateListeningItemData>(assetPath)
                   ?? ScriptableObject.CreateInstance<StateListeningItemData>();
        data.stateListeners = stateListenersByItem[item].ToArray();
        data.otherImplementation = otherImplementationByItem.GetValueOrDefault(item, "");
        if (!AssetDatabase.Contains(data))
            AssetDatabase.CreateAsset(data, assetPath);
        else
            EditorUtility.SetDirty(data);
    }

    private void SaveAllItemsToAssets()
    {
        foreach (var item in stateListeningItems)
            SaveItemToAsset(item);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void ApplyAssetsToScripts()
    {
        SaveAllItemsToAssets();
        Assets.KaomoLab.CSCombiner.CSCombiner.CombineAll();
    }

    #endregion
}
