using UnityEngine;

[CreateAssetMenu(fileName = "StateList", menuName = "StateDependent/StateList", order = 1)]
public class StateList : ScriptableObject
{
    public string[] States;
}