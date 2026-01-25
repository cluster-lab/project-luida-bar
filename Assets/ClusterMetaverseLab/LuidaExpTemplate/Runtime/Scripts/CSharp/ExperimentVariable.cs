using JetBrains.Annotations;

[System.Serializable]
public class ExperimentVariable
{
    public string name;
    public string[] values;
    public bool isRandom;
    [CanBeNull] public string debugValue = null;
}
