using System;
using ClusterVR.CreatorKit.Constants;
using ClusterVR.CreatorKit.Extensions;
using ClusterVR.CreatorKit.ProductUgc;
using UnityEngine;

namespace ClusterVR.CreatorKit.Item.Implements
{
    [RequireComponent(typeof(Item)), DisallowMultipleComponent]
    public sealed class AutoAssignableProductDisplayItem : ContactableItem, IAutoAssignableProductDisplayItem
    {
        [SerializeField, HideInInspector] Item item;
        [SerializeField, Tooltip("商品を表示する位置（任意）")] Transform productDisplayRoot;
        [SerializeField, Tooltip("IDの割り振り順")] int order;
        [SerializeField, Tooltip("展示する商品の種類")] DisplayContent displayContent;

        public override IItem Item
        {
            get
            {
                if (item != null)
                {
                    return item;
                }
                if (this == null)
                {
                    return null;
                }
                return item = GetComponent<Item>();
            }
        }

        public DisplayContent DisplayContent => displayContent;

        public int Order => order;

        ProductId productId = new("");

        bool isInteractable;
        public override bool IsContactable => isInteractable;
        public override bool RequireOwnership => false;
        ProductId IAutoAssignableProductDisplayItem.ProductId => productId;
        bool IAutoAssignableProductDisplayItem.NeedsProductSample => productDisplayRoot != null;

        Action onProductIdUpdated;
        Action onInvoked;
        GameObject currentProductSample;

        void IAutoAssignableProductDisplayItem.SetOnProductIdUpdatedCallback(Action onProductIdUpdated)
        {
            this.onProductIdUpdated = onProductIdUpdated;
        }

        void IAutoAssignableProductDisplayItem.UpdateId(ProductId productId)
        {
            if (!productId.IsValid())
            {
                return;
            }

            this.productId = productId;
            onProductIdUpdated?.Invoke();
        }

        void IAutoAssignableProductDisplayItem.SetOnInvokedCallback(Action onInvoked)
        {
            this.onInvoked = onInvoked;
        }

        public Transform ProductDisplayRoot => productDisplayRoot;

        void IAutoAssignableProductDisplayItem.SetInteractable(bool isInteractable)
        {
            this.isInteractable = isInteractable;
        }

        void IAutoAssignableProductDisplayItem.SetSample(GameObject productSample)
        {
            if (currentProductSample != null && currentProductSample != productSample)
            {
                Destroy(currentProductSample);
                currentProductSample = null;
            }

            currentProductSample = productSample;

            if (currentProductSample != null)
            {
                currentProductSample.transform.SetParent(productDisplayRoot, false);
            }
        }

        void IInteractableItem.Invoke()
        {
            if (isInteractable)
            {
                onInvoked?.Invoke();
            }
        }

        void Reset()
        {
            item = GetComponent<Item>();
            gameObject.SetLayerRecursively(LayerName.InteractableItem);
        }

        void OnValidate()
        {
            if (item == null || item.gameObject != gameObject)
            {
                item = GetComponent<Item>();
            }

            if (productDisplayRoot != null && !IsSiblingOrSelf(productDisplayRoot, transform))
            {
                productDisplayRoot = null;
            }
        }

        static bool IsSiblingOrSelf(Transform target, Transform mayParent)
        {
            if (target == mayParent) return true;
            var parent = target.parent;
            if (parent == null) return false;
            return IsSiblingOrSelf(parent, mayParent);
        }
    }
}
