#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ClusterVR.CreatorKit.Item.Implements;

[ExecuteInEditMode]
public class LuidaStateListeningItem : MonoBehaviour
{
    private void OnValidate()
    {
        Component[] components = GetComponents<Component>();

        foreach (Component component in components)
        {
            if (component is Transform || component is LuidaStateListeningItem || component is Item)
                continue;

            if (PrefabUtility.IsPartOfPrefabInstance(component) || component is ItemGroupMember)
            {
                component.hideFlags = HideFlags.HideInInspector;
            }
            else
            {
                component.hideFlags = HideFlags.None;
            }
        }

        EditorApplication.RepaintHierarchyWindow();
        EditorApplication.RepaintProjectWindow();
    }
}
#endif