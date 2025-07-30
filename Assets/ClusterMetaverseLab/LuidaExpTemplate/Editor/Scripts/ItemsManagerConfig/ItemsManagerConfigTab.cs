using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.Linq;

public class ItemsManagerConfigTab : LuidaAutomationConfigTab
{
    protected override LuidaConfigWindow.TabIndex TabIndex => LuidaConfigWindow.TabIndex.StateListeningItems;
    
    // State variables accessed by helper classes
    public bool _needsRebuild = true;
    public string[] _cachedStateNames = Array.Empty<string>();
    public GameObject[] _cachedItems = Array.Empty<GameObject>();
    public Dictionary<string, ReorderableList> _reorderableLists = new Dictionary<string, ReorderableList>();
    public string newItemName = string.Empty;

    public List<GameObject> stateListeningItems = new List<GameObject>();
    public StateList stateList = null;
    public Dictionary<GameObject, List<StateListener>> stateListenersByItem = new Dictionary<GameObject, List<StateListener>>();

    [Serializable]
    public class EditorExperimentVariable
    {
        public string name;
        public string[] values;
    }
    public List<EditorExperimentVariable> _cachedExperimentVariables = new List<EditorExperimentVariable>();
    public string _experimentVariablesAssetPath;

    // Scroll positions
    public Vector2 scrollPositionY;
    public Vector2 docScrollPositionY;
    public Vector2 _horizontalScrollPosition;

    private bool isInitialized = false;

    #region Unity Callbacks & Event Handling

    public static void ShowWindow()
    {
        GetWindow<ItemsManagerConfigTab>("Items Manager");
    }

    public void OnEnable()
    {
        _needsRebuild = true;

        if (!isInitialized)
        {
            ItemsManagerAssetUtil.RefreshStateList(this);
            ItemsManagerAssetUtil.RefreshExperimentVariablesCache(this);
            
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.projectChanged -= OnProjectChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            LuidaConfigWindow.OnEditorClosed -= HandleCloseOrFocusLost;
            LuidaConfigWindow.OnEditorClosed -= OnDisable;
            LuidaConfigWindow.OnTabSwitched -= HandleTabSwitched;
            
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.projectChanged += OnProjectChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            LuidaConfigWindow.OnEditorClosed += HandleCloseOrFocusLost;
            LuidaConfigWindow.OnEditorClosed += OnDisable;
            LuidaConfigWindow.OnTabSwitched += HandleTabSwitched;
            isInitialized = true;
        }
    }

    public void OnDisable()
    {
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        EditorApplication.projectChanged -= OnProjectChanged;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

        LuidaConfigWindow.OnEditorClosed -= HandleCloseOrFocusLost;
        LuidaConfigWindow.OnEditorClosed -= OnDisable;
        LuidaConfigWindow.OnTabSwitched -= HandleTabSwitched;
    }

    private void HandleCloseOrFocusLost() => ItemsManagerAssetUtil.ApplyAssetsToScripts(this);

    private void OnHierarchyChanged() => _needsRebuild = true;
    private void OnProjectChanged() => _needsRebuild = true;

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            ItemsManagerAssetUtil.RefreshStateListeningItems(this);
            ItemsManagerAssetUtil.ApplyAssetsToScripts(this);
        }
    }

    #endregion

    public void OnGUI()
    {
        if (_needsRebuild)
        {
            RebuildCache();
            _needsRebuild = false;
        }

        if (stateList == null)
        {
            EditorGUILayout.HelpBox("No StateList asset found for the current scene. Please create one or check the asset path.", MessageType.Warning);
            if (GUILayout.Button("Attempt to reload StateList")) ItemsManagerAssetUtil.RefreshStateList(this);
            return;
        }
        if (_cachedStateNames.Length == 0 && stateList != null)
        {
            EditorGUILayout.HelpBox("The StateList is loaded, but no states have been defined in it.", MessageType.Info);
        }

        // Delegate all drawing to the helper class
        ItemsManagerUIDrawer.DrawGUI(this);
    }

    private void HandleTabSwitched(LuidaConfigWindow.TabIndex prevTab, LuidaConfigWindow.TabIndex nextTab)
    {
        if (prevTab == TabIndex && nextTab != TabIndex) HandleCloseOrFocusLost();
    }

    private void RebuildCache()
    {
        ItemsManagerAssetUtil.RefreshStateList(this);
        ItemsManagerAssetUtil.RefreshExperimentVariablesCache(this);
        ItemsManagerAssetUtil.RefreshStateListeningItems(this);

        _cachedStateNames = stateList != null && stateList.States != null
            ? stateList.States.Select(s => s.StateName).ToArray()
            : Array.Empty<string>();
        _cachedItems = stateListeningItems.ToArray();

        ItemsManagerUIDrawer.SetupReorderableLists(this);
    }
}
