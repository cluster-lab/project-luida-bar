using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ClusterVR.CreatorKit.Operation.Implements;
using ClusterVR.CreatorKit.Gimmick;
using ClusterVR.CreatorKit.Gimmick.Implements;
using ClusterVR.CreatorKit.Item.Implements;

[CustomEditor(typeof(LuidaFakeGimmick), true)]
public class LuidaFakeGimmickEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((LuidaFakeGimmick)target), typeof(LuidaFakeGimmick), false);
        GUI.enabled = true;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("target"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("key"));

        var targetProperty = serializedObject.FindProperty("target");
        if ((GimmickTarget)targetProperty.enumValueIndex == GimmickTarget.Item)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("item"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
