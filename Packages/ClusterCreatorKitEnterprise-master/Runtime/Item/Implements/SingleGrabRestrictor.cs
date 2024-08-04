using UnityEngine;

namespace ClusterVR.CreatorKit.Item.Implements
{
    [RequireComponent(typeof(GrabbableItem)), DisallowMultipleComponent]
    public sealed class SingleGrabRestrictor : MonoBehaviour, ISingleGrabRestrictor
    {
    }
}
