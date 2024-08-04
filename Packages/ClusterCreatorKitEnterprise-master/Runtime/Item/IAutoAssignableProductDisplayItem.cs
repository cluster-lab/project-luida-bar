using System;
using ClusterVR.CreatorKit.ProductUgc;
using UnityEngine;

namespace ClusterVR.CreatorKit.Item
{
    public interface IAutoAssignableProductDisplayItem : IInteractableItem
    {
        DisplayContent DisplayContent { get; }
        int Order { get; }
        ProductId ProductId { get; }
        bool NeedsProductSample { get; }
        void UpdateId(ProductId productId);
        void SetOnInvokedCallback(Action onInvoked);
        void SetOnProductIdUpdatedCallback(Action onProductIdUpdated);
        void SetSample(GameObject productSample);
        void SetInteractable(bool isInteractable);
    }
}
