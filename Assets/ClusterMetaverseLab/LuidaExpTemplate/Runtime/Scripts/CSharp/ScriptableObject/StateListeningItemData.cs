using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public struct StateListeningAction
{
    public string actionType;
    public string codeSnippet;
    public string[] variables;
    public StateListeningAction(string _actionType, string _codeSnippet, string[] _variables = null)
    {
        actionType = _actionType;
        codeSnippet = _codeSnippet;
        variables = _variables ?? Array.Empty<string>();
    }
}

[Serializable]
public class StateListenerAction: ISerializationCallbackReceiver
{
    public StateListeningAction predefinedActionTemplate;
    public string customAction;
    public Dictionary<string, string> variableValues = new Dictionary<string, string>();

	[SerializeField]
    private List<string> _variableKeys = new List<string>();
    [SerializeField]
    private List<string> _variableValuesList = new List<string>();

    public StateListenerAction(StateListeningAction template)
    {
        predefinedActionTemplate = template;
        customAction = "";
		variableValues = new Dictionary<string, string>();
        if (template.variables != null)
        {
            foreach (var varName in template.variables)
            {
                variableValues[varName] = GetDefaultValueForVariable(varName, template.actionType);
            }
        }
    }

    public StateListenerAction()
    {
        predefinedActionTemplate = default;
        customAction = "";
        variableValues = new Dictionary<string, string>();
    }

    private string GetDefaultValueForVariable(string varName, string actionType) {
        if (actionType == "Set text" && varName == "text") return "";
        if (actionType == "Sleep" && varName == "seconds") return "0"; // Default for Sleep's duration
        
        // Default for numeric vector components and haptics parameters
        string[] numericVars = { "x", "y", "z", "frequency", "amplitude", "duration" };
        if (numericVars.Contains(varName)) return "0";
        
        if (varName == "target") return "\"right\""; // Default for haptics target (JS string literal)
        
        return ""; // Default for any other unhandled variable
    }

    public string GetActionLabel()
    {
        if (string.IsNullOrEmpty(predefinedActionTemplate.actionType) || predefinedActionTemplate.actionType == "Customized Action")
        {
            if (predefinedActionTemplate.actionType == "Customized Action" || !string.IsNullOrEmpty(customAction))
                 return "Customized Action";
            return "Select Action";
        }
        return predefinedActionTemplate.actionType;
    }

    public string GetActionContent()
    {
		if (predefinedActionTemplate.actionType == "Customized Action" || string.IsNullOrEmpty(predefinedActionTemplate.actionType))
        {
            return customAction;
        }

		string snippet = predefinedActionTemplate.codeSnippet; // Get template
        if (predefinedActionTemplate.variables != null)
        {
            foreach (var varName in predefinedActionTemplate.variables)
            {
                if (variableValues.TryGetValue(varName, out string value))
                {
                    snippet = snippet.Replace($"{{_{varName}_}}", value);
                }
            }
        }
        return snippet;
    }

	public void OnBeforeSerialize()
    {
        _variableKeys.Clear();
        _variableValuesList.Clear();

        foreach (var kvp in variableValues)
        {
            _variableKeys.Add(kvp.Key);
            _variableValuesList.Add(kvp.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        variableValues = new Dictionary<string, string>();

        if (_variableKeys.Count != _variableValuesList.Count)
        {
            Debug.LogError("Mismatch between keys and values count after deserializing StateListenerAction.variableValues. Data may be lost.");
            return;
        }

        for (int i = 0; i < _variableKeys.Count; i++)
        {
            variableValues[_variableKeys[i]] = _variableValuesList[i];
        }
    }
}

[Serializable]
public class StateListener
{
    public int stateID;
    public List<StateListenerAction> onStateStartedActions = new List<StateListenerAction>();
    public List<StateListenerAction> duringStateActions = new List<StateListenerAction>();
    public List<StateListenerAction> onStateExitedActions = new List<StateListenerAction>();
}

[Serializable]
public class StateListeningItemData : ScriptableObject
{
    public StateListener[] stateListeners;
    public string otherImplementation;
}
