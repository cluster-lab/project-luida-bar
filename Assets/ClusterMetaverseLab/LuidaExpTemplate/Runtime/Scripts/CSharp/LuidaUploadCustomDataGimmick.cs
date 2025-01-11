#if UNITY_EDITOR
using UnityEngine;

[ExecuteInEditMode]
public class LuidaUploadCustomDataGimmick : LuidaFakeGimmick
{
    protected override string TargetPrefabPath => "ClusterMetaverseLab/LuidaExpTemplate/FakeGimmickSources/UploadCustomData";
}
#endif
