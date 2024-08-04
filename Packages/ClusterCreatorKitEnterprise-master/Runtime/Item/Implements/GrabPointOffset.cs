using ClusterVR.CreatorKit.Item.Implements;
using UnityEngine;

namespace ClusterVR.CreatorKit.Item
{
    [RequireComponent(typeof(GrabbableItem)), DisallowMultipleComponent]
    public sealed class GrabPointOffset : MonoBehaviour, IGrabPointOffset
    {
        [SerializeField, Tooltip("アイテムを持つときの手の位置のオフセット(アバターの head local 空間)")] Vector3 offset;

        public Vector3 Offset => offset;
    }
}
