using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public struct StateListeningAction
{
    /// <summary>Serialization key. Used for selection lookup + special-case dispatch in the drawer. Do not rename.</summary>
    public string actionType;
    /// <summary>Optional UI label shown in the action dropdown. Falls back to actionType when null/empty.</summary>
    public string displayLabel;
    public string codeSnippet;
    public string[] variables;
    public StateListeningAction(string _actionType, string _codeSnippet, string[] _variables = null, string _displayLabel = null)
    {
        actionType = _actionType;
        codeSnippet = _codeSnippet;
        variables = _variables ?? Array.Empty<string>();
        displayLabel = _displayLabel;
    }

    public string GetDisplayLabel()
        => string.IsNullOrEmpty(displayLabel) ? actionType : displayLabel;
}

[Serializable]
public class StateListenerAction: ISerializationCallbackReceiver
{
    public StateListeningAction predefinedActionTemplate;
    public string customAction;
    public Dictionary<string, string> variableValues = new Dictionary<string, string>();

    public bool isConditional;
    public string conditionVariable;
    public string conditionValue;
    
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
        isConditional = false;
        conditionVariable = null;
        conditionValue = null;
    }

    public StateListenerAction()
    {
        predefinedActionTemplate = default;
        customAction = "";
        variableValues = new Dictionary<string, string>();
        isConditional = false;
        conditionVariable = null;
        conditionValue = null;
    }

    private string GetDefaultValueForVariable(string varName, string actionType) {
        if (actionType == "Set text" && varName == "text") return "";
        if (actionType == "Sleep" && varName == "seconds") return "0"; // Default for Sleep's duration
        
        // Default for numeric vector components and haptics parameters
        string[] numericVars = { "x", "y", "z", "posX", "posY", "posZ", "rotX", "rotY", "rotZ", "frequency", "amplitude", "duration" };
        if (numericVars.Contains(varName)) return "0";
        
        if (varName == "target") return "\"right\""; // Default for haptics target (JS string literal)

        // Defaults for avatar assignment
        if (varName == "participantIndex") return "1";
        if (varName == "avatarID") return "";

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
		string baseSnippet;
		if (predefinedActionTemplate.actionType == "Customized Action" || string.IsNullOrEmpty(predefinedActionTemplate.actionType))
        {
            baseSnippet = customAction ?? "";
        }
        else
        {
    		baseSnippet = predefinedActionTemplate.codeSnippet; 
            if (predefinedActionTemplate.variables != null)
            {
                foreach (var varName in predefinedActionTemplate.variables)
                {
                    if (variableValues.TryGetValue(varName, out string value))
                    {
                        baseSnippet = baseSnippet.Replace($"{{_{varName}_}}", value);
                    }
                    else // Handle case where a variable might be missing from the dictionary
                    {
                        baseSnippet = baseSnippet.Replace($"{{_{varName}_}}", GetDefaultValueForVariable(varName, predefinedActionTemplate.actionType) ?? "");
                    }
                }
            }
        }

        if (isConditional && !string.IsNullOrEmpty(conditionVariable) && conditionValue != null)
        {
            string jsConditionValue = $"{conditionValue.Replace("'", "\\'")}"; // Default to string literal

            // Attempt to format as number or boolean if applicable for JS comparison
            if (double.TryParse(conditionValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double numValue))
            {
                jsConditionValue = numValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else if (bool.TryParse(conditionValue, out bool boolValue))
            {
                jsConditionValue = boolValue.ToString().ToLowerInvariant();
            }
            
            // Indent the base snippet
            string indentedBaseSnippet = string.Join("\n", baseSnippet.Split('\n').Select(line => $"  {line}"));
            return $"if (CONDITION['{conditionVariable}'] === '{jsConditionValue}') {{\n{indentedBaseSnippet}\n}}";
        }
        return baseSnippet;
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

    public StateListenerAction Clone()
    {
        return new StateListenerAction
        {
            predefinedActionTemplate = this.predefinedActionTemplate,
            customAction = this.customAction,
            isConditional = this.isConditional,
            conditionVariable = this.conditionVariable,
            conditionValue = this.conditionValue,
            variableValues = new Dictionary<string, string>(this.variableValues ?? new Dictionary<string, string>())
        };
    }
}

[Serializable]
public class StateListener
{
    public int stateID;
    public List<StateListenerAction> onStateStartedActions = new List<StateListenerAction>();
    public List<StateListenerAction> duringStateActions = new List<StateListenerAction>();
    public List<StateListenerAction> onStateExitedActions = new List<StateListenerAction>();

    public StateListener DeepClone(int? newStateID = null)
    {
        var copy = new StateListener { stateID = newStateID ?? this.stateID };
        if (onStateStartedActions != null) foreach (var a in onStateStartedActions) copy.onStateStartedActions.Add(a.Clone());
        if (duringStateActions != null) foreach (var a in duringStateActions) copy.duringStateActions.Add(a.Clone());
        if (onStateExitedActions != null) foreach (var a in onStateExitedActions) copy.onStateExitedActions.Add(a.Clone());
        return copy;
    }
}

[Serializable]
public class StateListeningItemData : ScriptableObject
{
    public StateListener[] stateListeners;
    public string otherImplementation;
}
