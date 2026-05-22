#if UNITY_EDITOR
using UnityEngine;

[System.Obsolete("Use LuidaDataCollectionGimmick (the merged data-collection gimmick) instead. The 'Upload saved data' phase covers what this component does. Existing instances still work.", false)]
[ExecuteInEditMode]
public class LuidaUploadCollectedDataGimmick : LuidaFakeGimmick
{
    protected override string TargetPrefabPath => "ClusterMetaverseLab/LuidaExpTemplate/FakeGimmickSources/UploadCollectedData";
}
#endif
