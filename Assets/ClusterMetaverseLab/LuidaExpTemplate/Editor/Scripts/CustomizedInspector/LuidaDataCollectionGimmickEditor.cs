#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LuidaDataCollectionGimmick))]
public class LuidaDataCollectionGimmickEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var gimmick = (LuidaDataCollectionGimmick)target;

        GUI.enabled = false;
        EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour(gimmick), typeof(LuidaDataCollectionGimmick), false);
        GUI.enabled = true;

        EditorGUILayout.Space();
        DataCollectorGimmickShared.DrawTriggerSignalSection(serializedObject);

        EditorGUILayout.Space();
        DataCollectorGimmickShared.DrawSectionHeaderWithConfigButton("Send to Data Collector");

        DataCollectorGimmickShared.DrawSceneSanityWarnings();
        var config = DataCollectorGimmickShared.FindBuilderConfig();

        DrawPipelinePhases(gimmick, config);

        serializedObject.ApplyModifiedProperties();
    }

    void DrawPipelinePhases(LuidaDataCollectionGimmick gimmick, LuidaDataCollectorConfig config)
    {
        EditorGUILayout.LabelField("Pipeline phases (run in order)", EditorStyles.miniBoldLabel);

        var doAddProp    = serializedObject.FindProperty("doAdd");
        var doSaveProp   = serializedObject.FindProperty("doSave");
        var doSubmitProp = serializedObject.FindProperty("doSubmit");

        doAddProp.boolValue    = EditorGUILayout.ToggleLeft("Push data", doAddProp.boolValue);
        if (doAddProp.boolValue)
        {
            EditorGUI.indentLevel++;
            DataCollectorGimmickShared.DrawLabelField(serializedObject, config);

            // Only show value-type + value editor + Register button when a label
            // is actually selected. Avoids dangling controls when no candidates
            // exist (or the user hasn't picked one yet).
            string currentLabel = serializedObject.FindProperty("label").stringValue;
            if (!string.IsNullOrEmpty(currentLabel))
            {
                DataCollectorGimmickShared.DrawValueTypeField(serializedObject, currentLabel, config);
                var currentType = (CckCollectedValueType)serializedObject.FindProperty("valueType").intValue;
                DataCollectorGimmickShared.DrawTypedValueField(serializedObject, currentType);
            }
            EditorGUI.indentLevel--;
        }

        doSaveProp.boolValue   = EditorGUILayout.ToggleLeft("Save pushed data",  doSaveProp.boolValue);
        doSubmitProp.boolValue = EditorGUILayout.ToggleLeft("Upload saved data", doSubmitProp.boolValue);

        bool anyEnabled = doAddProp.boolValue || doSaveProp.boolValue || doSubmitProp.boolValue;
        if (!anyEnabled)
        {
            EditorGUILayout.HelpBox(
                "All three phases are disabled — this gimmick will not do anything when triggered. " +
                "Enable at least one phase.",
                MessageType.Error);
        }

        if (doAddProp.boolValue && !string.IsNullOrEmpty(gimmick.label))
        {
            DataCollectorGimmickShared.DrawRegisterControls(gimmick.label, gimmick.valueType, config);
        }
    }
}
#endif
