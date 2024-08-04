using UnityEngine;

namespace ClusterVR.CreatorKit.Exhibit.AvatarDisplay.Implements
{
    [RequireComponent(typeof(BoxCollider))]
    [DisallowMultipleComponent]
    public sealed class AvatarDisplayBounds : MonoBehaviour, IAvatarDisplayBounds
    {
        BoxCollider boxColliderCache = null;
        public BoxCollider BoxCollider => boxColliderCache ??= GetComponent<BoxCollider>();

        void OnValidate()
        {
            BoxCollider.isTrigger = true;
        }
    }
}
