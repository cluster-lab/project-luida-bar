using UnityEngine;

namespace ClusterVR.CreatorKit.Tutorial
{
    public interface IOnboardingComponentAnchor
    {
        Transform Transform { get; }
        AnchorType AnchorType { get; }
        string CustomId { get; }
    }

    public enum AnchorType
    {
        Custom,
        FirstSetupSpawnPoint,
        VROnlyObject,
        DisabledObjectOnAccountSetup,
    }
}
