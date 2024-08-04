using ClusterVR.CreatorKit.Constants;
using ClusterVR.CreatorKit.Extensions;
using UnityEngine;

namespace ClusterVR.CreatorKit.Exhibit.Goods.Implements
{
    public sealed class GoodsDisplay : MonoBehaviour, IGoodsDisplay
    {
        [SerializeField] string id;
        [SerializeField] string storeDomainUrl;

        string IGoodsDisplay.Id => id;
        string IGoodsDisplay.StoreDomainUrl => storeDomainUrl;

        void Reset()
        {
            gameObject.SetLayerRecursively(LayerName.InteractableExhibit);
        }

        void OnValidate()
        {
            gameObject.SetLayerRecursively(LayerName.InteractableExhibit);
            foreach (var col in GetComponentsInChildren<Collider>(true))
            {
                col.isTrigger = true;
            }
        }
    }
}
