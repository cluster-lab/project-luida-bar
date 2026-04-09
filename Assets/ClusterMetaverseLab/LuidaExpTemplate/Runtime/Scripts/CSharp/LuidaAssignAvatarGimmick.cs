#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[ExecuteInEditMode]
public class LuidaAssignAvatarGimmick : LuidaFakeGimmick
{
    protected override string TargetPrefabPath => "ClusterMetaverseLab/LuidaExpTemplate/FakeGimmickSources/AssignAvatar";

    [Header("Avatar Assignment Parameters")]
    [SerializeField] public string avatarID;
    [SerializeField] public int participantIndex;
    [SerializeField] public List<BoneOffsetData> boneOffsets = new List<BoneOffsetData>();

    protected override void OnAfterCopiedComponentSetup()
    {
        PatchLogicStatementTargetKey(CopiedComponent);
    }

    /// <summary>
    /// Patches the copied GlobalLogic's logic statement targetState key
    /// to match this gimmick instance's key, so each instance sets a unique global state.
    /// </summary>
    private void PatchLogicStatementTargetKey(object globalLogic)
    {
        if (globalLogic == null) return;

        try
        {
            // Access: globalLogic.logic.statements[0].singleStatement.targetState
            var logicField = globalLogic.GetType().GetField("logic", BindingFlags.NonPublic | BindingFlags.Instance);
            if (logicField == null) return;
            var logic = logicField.GetValue(globalLogic);
            if (logic == null) return;

            var statementsField = logic.GetType().GetField("statements", BindingFlags.NonPublic | BindingFlags.Instance);
            if (statementsField == null) return;
            var statements = statementsField.GetValue(logic);
            if (statements == null) return;

            // statements is a List or array — get first element
            var statementsType = statements.GetType();
            var countProp = statementsType.GetProperty("Count") ?? statementsType.GetProperty("Length");
            if (countProp == null) return;
            int count = (int)countProp.GetValue(statements);
            if (count == 0) return;

            var indexer = statementsType.GetProperty("Item");
            object firstStatement;
            if (indexer != null)
                firstStatement = indexer.GetValue(statements, new object[] { 0 });
            else
                firstStatement = ((System.Array)statements).GetValue(0);
            if (firstStatement == null) return;

            // Navigate singleStatement.targetState.key
            var singleStatementField = firstStatement.GetType().GetField("singleStatement", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (singleStatementField == null) return;
            var singleStatement = singleStatementField.GetValue(firstStatement);
            if (singleStatement == null) return;

            var targetStateField = singleStatement.GetType().GetField("targetState", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (targetStateField == null) return;
            var targetState = targetStateField.GetValue(singleStatement);
            if (targetState == null) return;

            // Set the key field on targetState
            var keyField = targetState.GetType().GetField("key", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (keyField == null) return;

            // Read the gimmick's key field from the base class
            var baseKeyField = typeof(LuidaFakeGimmick).GetField("key", BindingFlags.NonPublic | BindingFlags.Instance);
            string gimmickKey = baseKeyField != null ? (string)baseKeyField.GetValue(this) : null;
            if (string.IsNullOrEmpty(gimmickKey)) return;

            keyField.SetValue(targetState, gimmickKey);

            // Write back the modified structs (value types need re-assignment up the chain)
            targetStateField.SetValue(singleStatement, targetState);
            singleStatementField.SetValue(firstStatement, singleStatement);

            // Write back into the list
            if (indexer != null)
                indexer.SetValue(statements, firstStatement, new object[] { 0 });

            statementsField.SetValue(logic, statements);
            logicField.SetValue(globalLogic, logic);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[LuidaAssignAvatarGimmick] Could not patch logic statement target key: {e.Message}");
        }
    }
}
#endif
