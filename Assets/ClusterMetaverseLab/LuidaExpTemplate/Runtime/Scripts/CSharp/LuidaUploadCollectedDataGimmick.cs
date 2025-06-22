#if UNITY_EDITOR
using UnityEngine;

[ExecuteInEditMode]
public class LuidaUploadCollectedDataGimmick : LuidaFakeGimmick
{
    protected override string TargetPrefabPath => "ClusterMetaverseLab/LuidaExpTemplate/FakeGimmickSources/UploadCollectedData";
}
#endif
