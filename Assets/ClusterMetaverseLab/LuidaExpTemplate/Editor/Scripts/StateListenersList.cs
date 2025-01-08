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
    public StateListeningAction(string _actionType, string _codeSnippet) {
        actionType = _actionType;
        codeSnippet = _codeSnippet;
    }
}

[Serializable]
public class StateListener
{
    public int stateID;

    public List<StateListeningAction> onStateStartedActions = new List<StateListeningAction>();
    public string onStateStartedCustomAction = "";
    public bool onStateStartedFoldout = false;

    public List<StateListeningAction> duringStateActions = new List<StateListeningAction>();
    public string duringStateCustomAction = "";
    public bool duringStateFoldout = false;

    public List<StateListeningAction> onStateExitedActions = new List<StateListeningAction>();
    public string onStateExitedCustomAction = "";
    public bool onStateExitedFoldout = false;
}

[Serializable]
public class StateListenersList: ScriptableObject
{
    public StateListener[] listeners;
}