#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

#pragma warning disable CS0618 // type is obsolete — we keep the legacy editor for backward compat

[CustomEditor(typeof(LuidaSendDataToCollectorGimmick))]
public class LuidaSendDataToCollectorGimmickEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var gimmick = (LuidaSendDataToCollectorGimmick)target;

        EditorGUILayout.HelpBox(
            "This gimmick is deprecated. For new setups, use the merged " +
            "LuidaDataCollectionGimmick with the 'Push data' phase enabled. " +
            "Existing instances still work.",
            MessageType.Warning);

        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour(gimmick), typeof(LuidaSendDataToCollectorGimmick), false);
        GUI.enabled = true;

        EditorGUILayout.Space();
        DataCollectorGimmickShared.DrawTriggerSignalSection(serializedObject);

        EditorGUILayout.Space();
        DataCollectorGimmickShared.DrawSectionHeaderWithConfigButton("Send to Data Collector");

        DataCollectorGimmickShared.DrawSceneSanityWarnings();

        var config = DataCollectorGimmickShared.FindBuilderConfig();
        DataCollectorGimmickShared.DrawLabelField(serializedObject, config);
        DataCollectorGimmickShared.DrawValueTypeField(serializedObject, gimmick.label, config);

        var currentType = (CckCollectedValueType)serializedObject.FindProperty("valueType").intValue;
        DataCollectorGimmickShared.DrawTypedValueField(serializedObject, currentType);

        DataCollectorGimmickShared.DrawRegisterControls(gimmick.label, gimmick.valueType, config);

        serializedObject.ApplyModifiedProperties();
    }
}

#pragma warning restore CS0618
#endif
