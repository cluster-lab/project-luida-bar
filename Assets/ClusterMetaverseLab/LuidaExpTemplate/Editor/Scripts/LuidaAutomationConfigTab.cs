using UnityEditor;

public abstract class LuidaAutomationConfigTab: EditorWindow
{
    protected abstract LuidaConfigWindow.TabIndex TabIndex { get; }
}