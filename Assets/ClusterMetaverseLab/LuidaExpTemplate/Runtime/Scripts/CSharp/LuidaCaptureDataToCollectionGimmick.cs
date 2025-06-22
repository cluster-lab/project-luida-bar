#if UNITY_EDITOR
using UnityEngine;

[ExecuteInEditMode]
public class LuidaCaptureDataToCollectionGimmick : LuidaFakeGimmick
{
    protected override string TargetPrefabPath => "ClusterMetaverseLab/LuidaExpTemplate/FakeGimmickSources/CaptureDataToCollection";
}
#endif
