using UnityEngine;

namespace ClusterVR.CreatorKit.InroomEventPickup
{
    public interface IEventPickupObjectAnchor
    {
        Transform Transform { get; }
        float Width { get; }
        string CustomId { get; }
    }
}
