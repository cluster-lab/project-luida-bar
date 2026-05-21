using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

public class EventHandlerActionsWindow : EditorWindow
{
    private StateListeningItemData _data;
    private int _handlerIndex;
    private GameObject _itemGO;
    private ItemsManagerConfigTab _editor;
    private ReorderableList _rl;
    private Vector2 _scroll;
    private string _cachedEventType;

    public static void Show(StateListeningItemData data, int handlerIndex,
                            GameObject itemGO, ItemsManagerConfigTab editor)
    {
        if (data == null) return;
        if (data.eventHandlers == null) data.eventHandlers = new List<EventHandlerData>();
        if (handlerIndex < 0 || handlerIndex >= data.eventHandlers.Count) return;

        var w = GetWindow<EventHandlerActionsWindow>(true,
                    GetWindowTitle(data, handlerIndex), true);
        w._data = data;
        w._handlerIndex = handlerIndex;
        w._itemGO = itemGO;
        w._editor = editor;
        w._cachedEventType = data.eventHandlers[handlerIndex].eventType;
        w.minSize = new Vector2(520, 360);
        w.BuildList();
        // ShowUtility (not ShowPopup): keeps focus across child GenericMenu dropdowns
        // for the in-list action-type selector. ShowPopup would auto-dismiss on focus loss.
        w.ShowUtility();
        w.Focus();
    }

    private static string GetWindowTitle(StateListeningItemData data, int idx)
    {
        if (data?.eventHandlers != null && idx >= 0 && idx < data.eventHandlers.Count)
        {
            var h = data.eventHandlers[idx];
            string et = string.IsNullOrEmpty(h.eventType) ? "(unset)" : h.eventType;
            return $"Edit {et} actions";
        }
        return "Edit event handler";
    }

    private void BuildList()
    {
        if (_data == null) return;
        if (_handlerIndex < 0 || _handlerIndex >= _data.eventHandlers.Count) return;
        var handler = _data.eventHandlers[_handlerIndex];
        if (handler.actions == null) handler.actions = new List<StateListenerAction>();
        _rl = ItemsManagerUIDrawer.CreateEventHandlerReorderableList(_editor, _itemGO, _data, handler);
    }

    void OnGUI()
    {
        if (_data == null || _handlerIndex < 0 || _handlerIndex >= _data.eventHandlers.Count)
        {
            Close();
            return;
        }

        var handler = _data.eventHandlers[_handlerIndex];
        // Rebuild list if the handler was swapped under us (e.g. undo/redo)
        if (handler.eventType != _cachedEventType)
        {
            _cachedEventType = handler.eventType;
            BuildList();
        }

        ItemsManagerUIDrawer.TryGetEventDefinition(handler.eventType, out var def);
        string paramSig = def.parameterSignature ?? "";
        string sig = handler.eventType != null && handler.eventType.StartsWith("$.")
            ? $"{handler.eventType}({paramSig.Trim('(', ')')} => {{ ... }})"
            : $"function {handler.eventType}{paramSig} {{ ... }}";

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(sig, EditorStyles.boldLabel);
        if (!string.IsNullOrEmpty(def.description))
        {
            EditorGUILayout.HelpBox(def.description, MessageType.Info);
        }
        if (!string.IsNullOrEmpty(paramSig))
        {
            EditorGUILayout.LabelField(
                $"In-scope identifiers: {paramSig.Trim('(', ')')}",
                EditorStyles.miniLabel);
        }
        if (handler.eventType == "Start")
        {
            EditorGUILayout.HelpBox(
                "Start fires from $.onStart before participants are enrolled and before " +
                "$.groupState.currentCondition is set by ConditionManager. CONDITION and PARTICIPANTS " +
                "are auto-primed at the top of Start so conditional actions don't throw, but values " +
                "may still be empty in the first frame. Prefer onInteract or Update for " +
                "participant-aware logic.",
                MessageType.Warning);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        _rl?.DoLayoutList();
        EditorGUILayout.EndScrollView();
    }

    void OnLostFocus()
    {
        Close();
    }

    void OnDestroy()
    {
        if (_data != null) EditorUtility.SetDirty(_data);

        // Skip JS regen if the parent LUIDA window itself has been closed —
        // _editor may be a stale reference and ApplyAssetsToScripts walks its cached state.
        if (_editor == null) return;
        if (!EditorWindow.HasOpenInstances<LuidaConfigWindow>()) return;
        try
        {
            ItemsManagerAssetUtil.ApplyAssetsToScripts(_editor);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[EventHandlerActionsWindow] regen after close failed: {e.Message}");
        }
    }
}
