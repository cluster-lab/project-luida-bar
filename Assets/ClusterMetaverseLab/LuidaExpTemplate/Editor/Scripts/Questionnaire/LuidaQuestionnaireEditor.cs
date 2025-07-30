using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector for the QuestionnaireIdSync component.
/// Detects when the qId field is changed and applies the new ID to all child questionnaire forms.
/// </summary>
[CustomEditor(typeof(LuidaQuestionnaire))]
public class LuidaQuestionnaireEditor : Editor
{
    public override void OnInspectorGUI()
    {
        LuidaQuestionnaire syncComponent = (LuidaQuestionnaire)target;
        EditorGUI.BeginChangeCheck();
        var newQId = EditorGUILayout.IntField("Questionnaire ID (qID)", syncComponent.qId);
        
        EditorGUILayout.Space();
        var messageEN = "When this questionnaire is completed, a CCK global signal with the key 'exp_questionnaireCompleted' is sent. You can have your other CCK gimmicks or logic listen for this signal.";
        EditorGUILayout.HelpBox(messageEN, MessageType.Info);
        var messageJA = "このアンケートが完了すると、キー「exp_questionnaireCompleted」を持つglobal向けのシグナルが送信されます。ご自身で設定した他のCCKのロジックやギミックでは、このシグナルを読み取るように設定できます。";
        EditorGUILayout.HelpBox(messageJA, MessageType.Info);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Key for you to copy", GUILayout.Width(EditorGUIUtility.labelWidth - 4));
        EditorGUILayout.SelectableLabel("exp_questionnaireCompleted", EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(syncComponent, "Change Questionnaire qID");
            Undo.RecordObject(syncComponent.gameObject, "Rename Questionnaire by qID");

            syncComponent.qId = newQId;
            syncComponent.gameObject.name = $"Questionnaire_{newQId}";

            foreach (Transform childForm in syncComponent.transform)
            {
                var formController = childForm.Find("FormController");
                if (formController != null)
                {
                    QuestionnaireEditorManager.UpdateID(formController.gameObject, "qID", newQId);
                }
            }
            
            EditorUtility.SetDirty(syncComponent);
            Debug.Log($"Questionnaire qID updated to {newQId} and GameObject renamed.");
        }
    }
}
