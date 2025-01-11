#if UNITY_EDITOR
using UnityEngine;

[ExecuteInEditMode]
public class LuidaToNextStateGimmick : LuidaFakeGimmick
{
    protected override string TargetPrefabPath => "ClusterMetaverseLab/LuidaExpTemplate/FakeGimmickSources/ToNextState";
}
#endif
