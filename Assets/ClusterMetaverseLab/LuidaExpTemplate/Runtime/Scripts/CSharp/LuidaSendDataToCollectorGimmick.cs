#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// Fake gimmick that writes a (label, value) pair into the LUIDA DataCollector
/// when a CCK trigger fires this gimmick's key. The value flows through CCK
/// global state (key: luida_collect_&lt;label&gt;) and is picked up by the
/// DataCollector's auto-generated sync snippet at calculator runtime.
///
/// Value type is restricted to what CCK ConstantValue can carry (see
/// Library/PackageCache/.../Runtime/Operation/Logic.cs:513): Bool, Float,
/// Integer, Vector2, Vector3. Strings are not supported here — use the
/// state-listening item "Send Data To Collector" action or call
/// SendDataToCollector("label", "stringValue") in custom code instead.
/// </summary>
[System.Obsolete("Use LuidaDataCollectionGimmick (the merged data-collection gimmick) instead. The 'Push data' phase covers what this component does. Existing instances still work.", false)]
[ExecuteInEditMode]
public class LuidaSendDataToCollectorGimmick : LuidaFakeGimmick
{
    protected override string TargetPrefabPath
        => "ClusterMetaverseLab/LuidaExpTemplate/FakeGimmickSources/SendDataToCollector";

    [Header("Collected Data")]
    [SerializeField] public string label;
    [SerializeField] public CckCollectedValueType valueType = CckCollectedValueType.Integer;

    [SerializeField] public bool boolValue;
    [SerializeField] public float floatValue;
    [SerializeField] public int integerValue;
    [SerializeField] public Vector2 vector2Value;
    [SerializeField] public Vector3 vector3Value;

    public const string GlobalStateKeyPrefix = "luida_collect_";

    public static string ComposeStateKey(string label)
    {
        return string.IsNullOrEmpty(label) ? null : GlobalStateKeyPrefix + label;
    }

    protected override void OnAfterCopiedComponentSetup()
    {
        if (string.IsNullOrEmpty(label)) return;

        object typedValue;
        switch (valueType)
        {
            case CckCollectedValueType.Bool:    typedValue = boolValue; break;
            case CckCollectedValueType.Float:   typedValue = floatValue; break;
            case CckCollectedValueType.Integer: typedValue = integerValue; break;
            case CckCollectedValueType.Vector2: typedValue = vector2Value; break;
            case CckCollectedValueType.Vector3: typedValue = vector3Value; break;
            default: return;
        }

        PatchStatementToConstant(CopiedComponent, ComposeStateKey(label), (int)valueType, typedValue);
    }

    // Player target is not meaningful here (the data sink is a singleton item),
    // so coerce any legacy Player serialization to Item — matches the pattern
    // LuidaAssignAvatarGimmick uses (see its ResolveTarget override).
    protected override CustomGimmickTarget ResolveTarget(CustomGimmickTarget configured)
    {
        return configured == CustomGimmickTarget.Player ? CustomGimmickTarget.Item : configured;
    }
}
#endif
