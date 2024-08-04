using ClusterVR.CreatorKit.Constants;
using ClusterVR.CreatorKit.Extensions;
using UnityEngine;

namespace ClusterVR.CreatorKit.Exhibit.ExchangeTicketForAvatar
{
    [RequireComponent(typeof(Collider))]
    public sealed class TicketExchangeableAvatar : MonoBehaviour, ITicketExchangeableAvatar
    {
        [SerializeField] string productUgcId;

        public string ProductUgcId => productUgcId;
        bool ITicketExchangeableAvatar.IsAssigned => !string.IsNullOrEmpty(productUgcId);

        // レイヤー設定はResetでもやったほうが良いかも (enableをあんまり気にしなくて良くなる & Editorでもランタイムの見た目に近くなる)
        void Start()
        {
            gameObject.SetLayerRecursively(LayerName.InteractableExhibit);
        }
    }
}
