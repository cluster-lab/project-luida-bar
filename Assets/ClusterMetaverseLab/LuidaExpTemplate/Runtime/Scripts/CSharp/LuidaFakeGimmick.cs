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
