#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using ClusterVR.CreatorKit.Operation.Implements;

[ExecuteInEditMode]
public class LuidaAssignAvatarGimmick : LuidaFakeGimmick
{
    protected override string TargetPrefabPath => "ClusterMetaverseLab/LuidaExpTemplate/FakeGimmickSources/AssignAvatar";

    [Header("Avatar Parameters")]
    [SerializeField] public bool removeAvatar; // When true, removes avatar instead of assigning
    [SerializeField] public string avatarID;
    [SerializeField] public int participantNumber = 1; // 1-based
    [SerializeField] public List<BoneOffsetData> boneOffsets = new List<BoneOffsetData>();

    [SerializeField] private GlobalLogic _participantLogic;

    protected override void OnAfterCopiedComponentSetup()
    {
        if (removeAvatar)
        {
            // Unassign mode: set global integer "luida_avatar_cmd" = -1
            PatchStatementToInteger(CopiedComponent, "luida_avatar_cmd", -1);
        }
        else
        {
            // Assign mode: look up avatar index from registry
            int avatarIndex = 0;
            var registry = AssetDatabase.LoadAssetAtPath<AvatarRegistry>("Assets/_Experiment_/Avatars/AvatarRegistry.asset");
            if (registry != null)
            {
                for (int i = 0; i < registry.entries.Count; i++)
                {
                    if (registry.entries[i].avatarID == avatarID)
                    {
                        avatarIndex = i;
                        break;
                    }
                }
            }
            PatchStatementToInteger(CopiedComponent, "luida_avatar_cmd", avatarIndex + 1);
        }

        // Ensure second GlobalLogic for participant number
        if (_participantLogic == null)
            _participantLogic = CreateAdditionalLogic();

        if (_participantLogic != null)
            PatchStatementToInteger(_participantLogic, "luida_avatar_participant", participantNumber);
    }
}
#endif
