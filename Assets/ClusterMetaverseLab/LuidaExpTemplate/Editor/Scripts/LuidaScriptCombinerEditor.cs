using ClusterMetaverseLab.Luida.Scripting;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LuidaScriptCombiner))]
public class LuidaScriptCombinerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var combiner = serializedObject.targetObject as LuidaScriptCombiner;

        if (combiner.HasScriptableItem())
        {
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("itemScripts"),
                new GUIContent("Item Scripts"),
                true);
            if (combiner.HasScriptableItemSourceAsset())
            {
                EditorGUILayout.HelpBox(
                    "Ignored: this Scriptable Item reads its code from an assigned .js asset instead.",
                    MessageType.Warning);
            }
        }

        if (combiner.HasPlayerScript())
        {
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("playerScripts"),
                new GUIContent("Player Scripts"),
                true);
            if (combiner.HasPlayerScriptSourceAsset())
            {
                EditorGUILayout.HelpBox(
                    "Ignored: this Player Script reads its code from an assigned .js asset instead.",
                    MessageType.Warning);
            }
        }

        EditorGUILayout.Space();

        if (GUILayout.Button(new GUIContent(
                "Combine Now",
                "Rebuilds the combined code of this object only.")))
        {
            combiner.Combine();
        }

        if (GUILayout.Button(new GUIContent(
                "Combine Everything",
                "Rebuilds the combined code of every object in the open scene and every prefab in the project.")))
        {
            LuidaScriptCombiner.CombineAll();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
