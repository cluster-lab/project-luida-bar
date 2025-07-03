using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ClusterVR.CreatorKit.Item.Implements;
using ClusterVR.CreatorKit.Operation.Implements;

[CustomEditor(typeof(LuidaDataCollector))]
public class LuidaDataCollectorEditor : Editor
{
    private static readonly System.Type[] TypesToHide =
    {
        typeof(ItemLogic),
        typeof(ScriptableItem),
        typeof(ItemGroupMember),
        typeof(ScriptableClusterScriptCombiner)
    };
    private readonly List<Component> hiddenComponents = new List<Component>();

    private void OnEnable()
    {
        LuidaDataCollector dataCollector = (LuidaDataCollector)target;
        hiddenComponents.Clear();
        foreach (var typeToHide in TypesToHide)
        {
            Component[] components = dataCollector.GetComponents(typeToHide);
            foreach (var component in components)
            {
                if (component != null)
                {
                    component.hideFlags |= HideFlags.HideInInspector;
                    hiddenComponents.Add(component);
                }
            }
        }
    }

    private void OnDisable()
    {
        foreach (Component component in hiddenComponents)
        {
            if (component != null)
            {
                component.hideFlags &= ~HideFlags.HideInInspector;
            }
        }
        hiddenComponents.Clear();
    }
}
