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
    [SerializeField] public bool removeAvatar;
    [SerializeField] public string avatarID;
    [SerializeField] public int participantNumber = 1; // 1-based

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
            // Assign mode: look up avatar index from this gimmick's scene
            // registry. Avatars are scene-scoped (Assets/_Experiment_/Avatars/<scene>/),
            // and a gimmick can only address avatars from its own scene.
            //
            // The path is inlined rather than fetched from AvatarsConfigAssetUtil
            // because this class compiles into Assembly-CSharp (runtime), which
            // can't reference editor-folder types like that util. Keep this
            // convention in sync with AvatarsConfigAssetUtil.GetRegistryPath.
            int avatarIndex = 0;
            string sceneName = gameObject.scene.name;
            AvatarRegistry registry = null;
            if (!string.IsNullOrEmpty(sceneName))
            {
                string sceneFolder = System.Text.RegularExpressions.Regex.Replace(sceneName, @"[^A-Za-z0-9_\-]", "_");
                registry = AssetDatabase.LoadAssetAtPath<AvatarRegistry>(
                    $"Assets/_Experiment_/Avatars/{sceneFolder}/AvatarRegistry.asset");
            }
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

    protected override void CollectExtraHiddenLogics(List<GlobalLogic> list)
    {
        if (_participantLogic != null) list.Add(_participantLogic);
    }

    // Cluster's venue validator rejects Player-target gimmick keys outside of
    // PlayerLocalUI canvases, which the avatar spawner is not. Force any other
    // value (including legacy Player serialized in older scenes) onto Item.
    protected override CustomGimmickTarget ResolveTarget(CustomGimmickTarget configured)
    {
        return configured == CustomGimmickTarget.Player ? CustomGimmickTarget.Item : configured;
    }
}
#endif
