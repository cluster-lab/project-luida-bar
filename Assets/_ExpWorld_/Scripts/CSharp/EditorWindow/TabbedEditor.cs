using UnityEditor;
using UnityEngine;

public class TabbedEditor : EditorWindow
{
    private int currentTab = 0;
    private string[] tabNames = { "Experiment Identifiers", "State List Editor", "Experiment Variables Editor", "Questionnaire Setter" };

    private ExpIdentifierEditor expIdentifierEditor;
    private StateListEditor stateListEditor;
    private ExperimentVariablesEditor experimentVariablesEditor;
    private QuestionnaireSetter questionnaireSetter;

    [MenuItem("Window/Luida Editor")]
    public static void ShowWindow()
    {
        GetWindow<TabbedEditor>("Luida Editor");
    }

    private void OnEnable()
    {
        expIdentifierEditor = new ExpIdentifierEditor();
        stateListEditor = new StateListEditor();
        experimentVariablesEditor = new ExperimentVariablesEditor();
        questionnaireSetter = new QuestionnaireSetter();

        expIdentifierEditor.OnEnable();
        stateListEditor.OnEnable();
        experimentVariablesEditor.OnEnable();
        questionnaireSetter.OnEnable();
    }

    private void OnGUI()
    {
        currentTab = GUILayout.Toolbar(currentTab, tabNames);

        switch (currentTab)
        {
            case 0:
                expIdentifierEditor.OnGUI();
                break;
            case 1:
                stateListEditor.OnGUI();
                break;
            case 2:
                experimentVariablesEditor.OnGUI();
                break;
            case 3:
                questionnaireSetter.OnGUI();
                break;
        }
    }
}