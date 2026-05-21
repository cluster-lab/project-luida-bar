#if UNITY_EDITOR
using UnityEngine;

[System.Obsolete("Use LuidaDataCollectionGimmick (the merged data-collection gimmick) instead. The 'Save pushed data' phase covers what this component does. Existing instances still work.", false)]
[ExecuteInEditMode]
public class LuidaProcessDataAndSaveToCollectionGimmick : LuidaFakeGimmick
{
    protected override string TargetPrefabPath => "ClusterMetaverseLab/LuidaExpTemplate/FakeGimmickSources/CaptureDataToCollection";
}
#endif
