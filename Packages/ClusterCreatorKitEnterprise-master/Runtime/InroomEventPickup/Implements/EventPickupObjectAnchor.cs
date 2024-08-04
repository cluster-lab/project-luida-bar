using UnityEngine;

namespace ClusterVR.CreatorKit.InroomEventPickup.Implements
{
    public sealed class EventPickupObjectAnchor : MonoBehaviour, IEventPickupObjectAnchor
    {
        [SerializeField] float width;
        [SerializeField] string customId;

        public Transform Transform => transform;
        public float Width => width;
        public string CustomId => customId;
    }
}
