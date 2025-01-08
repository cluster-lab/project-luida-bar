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
    public StateListeningAction(string _actionType, string _codeSnippet)
    {
        actionType = _actionType;
        codeSnippet = _codeSnippet;
    }
}

[Serializable]
public class StateListenerAction
{
    public StateListeningAction predefinedAction;
    public string customAction;
    public bool showCustomActionFoldout;

    public StateListenerAction(StateListeningAction action)
    {
        predefinedAction = action;
        customAction = null;
    }

    public StateListenerAction()
    {
        predefinedAction = default;
        customAction = "";
    }

    public string GetActionLabel()
    {
        if (customAction != null && customAction.Length > 0)
        {
            return "Custom Action";
        }
        else
        {
            return predefinedAction.actionType;
        }
    }

    public string GetActionContent()
    {
        if (customAction != null && customAction.Length > 0)
        {
            return customAction;
        }
        else
        {
            return predefinedAction.codeSnippet;
        }
    }
}

[Serializable]
public class StateListener
{
    public int stateID;

    public List<StateListenerAction> onStateStartedActions = new List<StateListenerAction>();
    public bool onStateStartedFoldout = false;

    public List<StateListenerAction> duringStateActions = new List<StateListenerAction>();
    public bool duringStateFoldout = false;

    public List<StateListenerAction> onStateExitedActions = new List<StateListenerAction>();
    public bool onStateExitedFoldout = false;
}

[Serializable]
public class StateListenersList : ScriptableObject
{
    public StateListener[] listeners;
}
