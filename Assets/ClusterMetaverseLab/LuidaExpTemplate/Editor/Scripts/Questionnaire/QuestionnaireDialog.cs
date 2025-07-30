using UnityEditor;
using UnityEngine;

/// <summary>
/// A simple dialog window to get a qID from the user before creating a questionnaire object.
/// </summary>
public class QuestionnaireDialog : EditorWindow
{
    private int qIdToCreate = 1; // Default qID value

    /// <summary>
    /// Opens this dialog window.
    /// </summary>
    public static void ShowWindow()
    {
        // Get existing open window or if none, make a new one.
        QuestionnaireDialog window = GetWindow<QuestionnaireDialog>("Create Questionnaire");
        window.minSize = new Vector2(350, 120); // Set a fixed size for the dialog
        window.maxSize = new Vector2(350, 120);
        window.Show();
    }

    /// <summary>
    /// Renders the GUI for the dialog window.
    /// </summary>
    void OnGUI()
    {
        EditorGUILayout.LabelField("Enter the Questionnaire ID for the new questionnaire object.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);

        // Integer field for qID input
        qIdToCreate = EditorGUILayout.IntField("Questionnaire ID (qID)", qIdToCreate);

        GUILayout.Space(15);

        // "Create" button
        if (GUILayout.Button("Create"))
        {
            // When clicked, call the creation logic with the entered qID
            QuestionnaireEditorManager.CreateQuestionnaireDirectly(qIdToCreate);
            
            // Close this dialog window
            this.Close();
        }
    }
}
