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

        var resolved = ResolveTarget(target);
        if (resolved != target) target = resolved;

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
            keyField.SetValue(gimmickKey, new GimmickKey(parsedTarget, key ?? string.Empty));
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

    /// <summary>
    /// Patches the first statement in a GlobalLogic to set a typed Global state.
    /// Thin wrapper around PatchStatementAt with statementIndex = 0, kept for
    /// the existing single-statement fake gimmicks.
    /// </summary>
    public static void PatchStatementToConstant(object globalLogic, string stateKey, int parameterTypeIndex, object value)
    {
        PatchStatementAt(globalLogic, 0, stateKey, parameterTypeIndex, value);
    }

    /// <summary>
    /// Patches statements[statementIndex] to fire a Signal (parameterType 0) at the
    /// given state key. The expression's constant is forced to Bool true (the "should
    /// we fire?" condition the executor evaluates for Signal targets).
    ///
    /// Signal — not Bool — is required for trigger-style writes that need to fire
    /// listeners every time. CCK's Logic.Run() rejects events whose TimeStamp is not
    /// strictly greater than lastTriggeredAt; for a Bool target, the GimmickValue's
    /// TimeStamp is derived from the bool's underlying double (true → epoch+1ms), so
    /// writing Bool true repeatedly stops firing after the first invocation. Signal
    /// targets always carry a fresh DateTime, so listeners fire on every write.
    /// </summary>
    public static void PatchStatementAtSignal(object globalLogic, int statementIndex, string stateKey)
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
            if (countProp == null) return;
            int count = (int)countProp.GetValue(statements);
            if (statementIndex < 0 || statementIndex >= count)
            {
                Debug.LogWarning($"[LuidaFakeGimmick] PatchStatementAtSignal: statementIndex {statementIndex} out of range (count={count}).");
                return;
            }

            var indexer = statementsType.GetProperty("Item");
            object stmt = indexer != null
                ? indexer.GetValue(statements, new object[] { statementIndex })
                : ((System.Array)statements).GetValue(statementIndex);
            if (stmt == null) return;

            var singleField = stmt.GetType().GetField("singleStatement", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (singleField == null) return;
            var single = singleField.GetValue(stmt);
            if (single == null) return;

            // targetState: key + parameterType = 0 (Signal).
            var tsField = single.GetType().GetField("targetState", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (tsField != null)
            {
                var ts = tsField.GetValue(single);
                var tsKey = ts.GetType().GetField("key", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (tsKey != null) tsKey.SetValue(ts, stateKey);
                var tsPT = ts.GetType().GetField("parameterType", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (tsPT != null) tsPT.SetValue(ts, 0);
                tsField.SetValue(single, ts);
            }

            // expression constant: Bool true — the condition the executor checks
            // before emitting the signal (see LogicExecutor.RunSingleStatement).
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
                        if (cType != null) cType.SetValue(c, 1);
                        var cBool = c.GetType().GetField("boolValue", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (cBool != null) cBool.SetValue(c, true);
                        constField.SetValue(val, c);
                    }
                    valField.SetValue(expr, val);
                }
                exprField.SetValue(single, expr);
            }

            singleField.SetValue(stmt, single);
            if (indexer != null)
                indexer.SetValue(statements, stmt, new object[] { statementIndex });
            statementsField.SetValue(logic, statements);
            logicField.SetValue(globalLogic, logic);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[LuidaFakeGimmick] PatchStatementAtSignal failed: {e.Message}");
        }
    }

    /// <summary>
    /// Patches statements[statementIndex] in a GlobalLogic to set a typed Global
    /// state. Used by the merged data-collection gimmick which needs to write
    /// multiple statements (Push → Save → Upload) from one GlobalLogic.
    /// </summary>
    /// <param name="globalLogic">The GlobalLogic component to patch (object so reflection callers can pass any type).</param>
    /// <param name="statementIndex">Index into logic.statements (0-based).</param>
    /// <param name="stateKey">The global state key the statement should write to.</param>
    /// <param name="parameterTypeIndex">ParameterType enum value: 1=Bool, 2=Float, 3=Integer, 4=Vector2, 5=Vector3.</param>
    /// <param name="value">A value whose runtime type matches parameterTypeIndex (bool/float/int/Vector2/Vector3).</param>
    public static void PatchStatementAt(object globalLogic, int statementIndex, string stateKey, int parameterTypeIndex, object value)
    {
        if (globalLogic == null) return;
        string typedFieldName = parameterTypeIndex switch
        {
            1 => "boolValue",
            2 => "floatValue",
            3 => "integerValue",
            4 => "vector2Value",
            5 => "vector3Value",
            _ => null,
        };
        if (typedFieldName == null)
        {
            Debug.LogWarning($"[LuidaFakeGimmick] PatchStatementAt: unsupported parameterTypeIndex={parameterTypeIndex}.");
            return;
        }

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
            if (countProp == null) return;
            int count = (int)countProp.GetValue(statements);
            if (statementIndex < 0 || statementIndex >= count)
            {
                Debug.LogWarning($"[LuidaFakeGimmick] PatchStatementAt: statementIndex {statementIndex} out of range (count={count}).");
                return;
            }

            var indexer = statementsType.GetProperty("Item");
            object stmt = indexer != null
                ? indexer.GetValue(statements, new object[] { statementIndex })
                : ((System.Array)statements).GetValue(statementIndex);
            if (stmt == null) return;

            var singleField = stmt.GetType().GetField("singleStatement", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (singleField == null) return;
            var single = singleField.GetValue(stmt);
            if (single == null) return;

            // Patch targetState: key + parameterType matching value type.
            var tsField = single.GetType().GetField("targetState", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (tsField != null)
            {
                var ts = tsField.GetValue(single);
                var tsKey = ts.GetType().GetField("key", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (tsKey != null) tsKey.SetValue(ts, stateKey);
                var tsPT = ts.GetType().GetField("parameterType", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (tsPT != null) tsPT.SetValue(ts, parameterTypeIndex);
                tsField.SetValue(single, ts);
            }

            // Patch expression constant: type + typed value field.
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
                        if (cType != null) cType.SetValue(c, parameterTypeIndex);
                        var typedField = c.GetType().GetField(typedFieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (typedField != null) typedField.SetValue(c, value);
                        constField.SetValue(val, c);
                    }
                    valField.SetValue(expr, val);
                }
                exprField.SetValue(single, expr);
            }

            singleField.SetValue(stmt, single);
            if (indexer != null)
                indexer.SetValue(statements, stmt, new object[] { statementIndex });
            statementsField.SetValue(logic, statements);
            logicField.SetValue(globalLogic, logic);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[LuidaFakeGimmick] PatchStatementAt failed: {e.Message}");
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
        if (Application.isPlaying) return;

        var owner = gameObject;
        var toDestroy = new System.Collections.Generic.List<GlobalLogic>();
        if (copiedComponent != null) toDestroy.Add(copiedComponent);
        CollectExtraHiddenLogics(toDestroy);
        if (toDestroy.Count == 0) return;

        // Defer so we can tell apart "user removed this component" (owner survives)
        // from "scene/GameObject is being unloaded" (owner becomes Unity-null).
        EditorApplication.delayCall += () =>
        {
            if (owner == null) return;
            foreach (var c in toDestroy)
            {
                if (c != null) DestroyImmediate(c);
            }
        };
    }

    protected virtual void CollectExtraHiddenLogics(System.Collections.Generic.List<GlobalLogic> list) { }

    protected virtual CustomGimmickTarget ResolveTarget(CustomGimmickTarget configured) => configured;

    private void RemoveCopiedComponent()
    {
        if (copiedComponent != null)
        {
            DestroyImmediate(copiedComponent);
            copiedComponent = null;
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
