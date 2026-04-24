#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using ClusterVR.CreatorKit.Operation.Implements;
using ClusterVR.CreatorKit.Gimmick;
using ClusterVR.CreatorKit.Gimmick.Implements;
using ClusterVR.CreatorKit.Item.Implements;

[ExecuteInEditMode]
public abstract class LuidaFakeGimmick : MonoBehaviour
{
    protected abstract string TargetPrefabPath { get; }

    [SerializeField]
    private GlobalLogic copiedComponent;

    [SerializeField]
    private CustomGimmickTarget target;

    [SerializeField]
    private string key;

    [SerializeField]
    private Item item;

    protected GlobalLogic CopiedComponent => copiedComponent;

    private void OnValidate()
    {
        if (this == null)
        {
            RemoveCopiedComponent();
            return;
        }

        if (!copiedComponent)
        {
            GameObject targetPrefab = (GameObject)Resources.Load(TargetPrefabPath);

            if (targetPrefab == null)
            {
                Debug.LogError($"Prefab with path '{TargetPrefabPath}' not found in Resources.");
                return;
            }

            GlobalLogic targetComponent = targetPrefab.GetComponent<GlobalLogic>();

            if (targetComponent == null)
            {
                Debug.LogError($"Component 'GlobalLogic' not found on prefab at '{TargetPrefabPath}'.");
                return;
            }

            copiedComponent = CopyComponent(targetComponent, gameObject);
        }

        if (target == CustomGimmickTarget.This)
        {
            item = gameObject.GetComponent<Item>();
            if (item == null)
            {
                Debug.LogError($"The current GameObject does not have an Item component.");
                return;
            }
        }

        var gimmickKey = Activator.CreateInstance(typeof(GlobalGimmickKey));
        var keyField = typeof(GlobalGimmickKey).GetField("key", BindingFlags.NonPublic | BindingFlags.Instance);
        var itemField = typeof(GlobalGimmickKey).GetField("item", BindingFlags.NonPublic | BindingFlags.Instance);

        if (keyField != null)
        {
            GimmickTarget parsedTarget = target == CustomGimmickTarget.This ? GimmickTarget.Item : (GimmickTarget)target;
            keyField.SetValue(gimmickKey, new GimmickKey(parsedTarget, key));
        }
        if (itemField != null)
        {
            itemField.SetValue(gimmickKey, item);
        }

        copiedComponent.GetType().GetField("globalGimmickKey", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(copiedComponent, gimmickKey);
        copiedComponent.hideFlags = HideFlags.HideInInspector;

        OnAfterCopiedComponentSetup();
    }

    protected virtual void OnAfterCopiedComponentSetup() { }

    /// <summary>
    /// Creates an additional hidden GlobalLogic on this GameObject with the same gimmick key as the main one.
    /// Uses EditorUtility.CopySerialized for deep copy, avoiding shared reference issues.
    /// </summary>
    protected GlobalLogic CreateAdditionalLogic()
    {
        GameObject templatePrefab = (GameObject)Resources.Load(TargetPrefabPath);
        if (templatePrefab == null) return null;
        GlobalLogic templateComponent = templatePrefab.GetComponent<GlobalLogic>();
        if (templateComponent == null) return null;

        GlobalLogic extra = gameObject.AddComponent<GlobalLogic>();
        EditorUtility.CopySerialized(templateComponent, extra);

        // Sync the gimmick key from the main component
        var gkField = typeof(GlobalLogic).GetField("globalGimmickKey", BindingFlags.NonPublic | BindingFlags.Instance);
        if (gkField != null && copiedComponent != null)
            gkField.SetValue(extra, gkField.GetValue(copiedComponent));

        extra.hideFlags = HideFlags.HideInInspector;
        EditorUtility.SetDirty(extra);
        return extra;
    }

    /// <summary>
    /// Patches the first statement in a GlobalLogic to set a global integer state.
    /// Changes targetState key, parameterType to integer, and expression constant to the given value.
    /// </summary>
    public static void PatchStatementToInteger(object globalLogic, string stateKey, int value)
    {
        if (globalLogic == null) return;
        try
        {
            var logicField = globalLogic.GetType().GetField("logic", BindingFlags.NonPublic | BindingFlags.Instance);
            if (logicField == null) return;
            var logic = logicField.GetValue(globalLogic);
            if (logic == null) return;

            var statementsField = logic.GetType().GetField("statements", BindingFlags.NonPublic | BindingFlags.Instance);
            if (statementsField == null) return;
            var statements = statementsField.GetValue(logic);
            if (statements == null) return;

            var statementsType = statements.GetType();
            var countProp = statementsType.GetProperty("Count") ?? statementsType.GetProperty("Length");
            if (countProp == null || (int)countProp.GetValue(statements) == 0) return;

            var indexer = statementsType.GetProperty("Item");
            object stmt = indexer != null
                ? indexer.GetValue(statements, new object[] { 0 })
                : ((System.Array)statements).GetValue(0);
            if (stmt == null) return;

            var singleField = stmt.GetType().GetField("singleStatement", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (singleField == null) return;
            var single = singleField.GetValue(stmt);
            if (single == null) return;

            // Patch targetState: key + parameterType = 2 (integer)
            var tsField = single.GetType().GetField("targetState", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (tsField != null)
            {
                var ts = tsField.GetValue(single);
                var tsKey = ts.GetType().GetField("key", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (tsKey != null) tsKey.SetValue(ts, stateKey);
                var tsPT = ts.GetType().GetField("parameterType", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (tsPT != null) tsPT.SetValue(ts, 2); // 2 = integer
                tsField.SetValue(single, ts);
            }

            // Patch expression constant: type = 3 (integer), integerValue = value
            var exprField = single.GetType().GetField("expression", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (exprField != null)
            {
                var expr = exprField.GetValue(single);
                var valField = expr.GetType().GetField("value", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (valField != null)
                {
                    var val = valField.GetValue(expr);
                    var constField = val.GetType().GetField("constant", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (constField != null)
                    {
                        var c = constField.GetValue(val);
                        var cType = c.GetType().GetField("type", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (cType != null) cType.SetValue(c, 3); // 3 = integer
                        var cInt = c.GetType().GetField("integerValue", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (cInt != null) cInt.SetValue(c, value);
                        constField.SetValue(val, c);
                    }
                    valField.SetValue(expr, val);
                }
                exprField.SetValue(single, expr);
            }

            singleField.SetValue(stmt, single);
            if (indexer != null)
                indexer.SetValue(statements, stmt, new object[] { 0 });
            statementsField.SetValue(logic, statements);
            logicField.SetValue(globalLogic, logic);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[LuidaFakeGimmick] PatchStatementToInteger failed: {e.Message}");
        }
    }

    private T CopyComponent<T>(T original, GameObject destination) where T : Component
    {
        T copy = destination.AddComponent<T>();
        foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            field.SetValue(copy, field.GetValue(original));
        }
        return copy;
    }

    private void OnDestroy()
    {
        /*
        if (!Application.isPlaying)
        {
            RemoveCopiedComponent();
        }
        */
    }

    private void RemoveCopiedComponent()
    {
        if (copiedComponent != null)
        {
            DestroyImmediate(copiedComponent);
            copiedComponent = null;
            Debug.Log("Copied GlobalLogic component removed.");
        }
    }
}

public enum CustomGimmickTarget
{
    Item,
    Player,
	Global,
    This
}
#endif
