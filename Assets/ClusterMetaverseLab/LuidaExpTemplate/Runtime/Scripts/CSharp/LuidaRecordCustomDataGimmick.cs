#if UNITY_EDITOR
using UnityEngine;

[ExecuteInEditMode]
public class LuidaRecordCustomDataGimmick : LuidaFakeGimmick
{
    protected override string TargetPrefabPath => "ClusterMetaverseLab/LuidaExpTemplate/FakeGimmickSources/RecordCustomData";
}
#endif
