using UnityEngine;

namespace ClusterVR.CreatorKit.Item.Implements
{
    [RequireComponent(typeof(RidableItem)), DisallowMultipleComponent]
    public sealed class ProhibitedGetOffItem : MonoBehaviour, IProhibitedGetOffItem
    {
    }
}
