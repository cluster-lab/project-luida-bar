#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

#pragma warning disable CS0618 // legacy editors for [Obsolete] components

[CustomEditor(typeof(LuidaProcessDataAndSaveToCollectionGimmick))]
public class LuidaProcessDataAndSaveToCollectionGimmickEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "This gimmick is deprecated. For new setups, use the merged " +
            "LuidaDataCollectionGimmick with the 'Save row' phase enabled. " +
            "Existing instances still work.",
            MessageType.Warning);
        DrawDefaultInspector();
    }
}

[CustomEditor(typeof(LuidaUploadCollectedDataGimmick))]
public class LuidaUploadCollectedDataGimmickEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "This gimmick is deprecated. For new setups, use the merged " +
            "LuidaDataCollectionGimmick with the 'Submit' phase enabled. " +
            "Existing instances still work.",
            MessageType.Warning);
        DrawDefaultInspector();
    }
}

#pragma warning restore CS0618
#endif
