using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StateList", menuName = "StateDependent/StateList", order = 1)]
public class StateList : ScriptableObject
{
    [Serializable]
    public struct State {
        public string StateName;
        public string DestStateName;
    }

    public State[] States;
}