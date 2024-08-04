using System.Collections.Generic;
using ClusterVR.CreatorKit.Editor.Preview.Item;
using ClusterVR.CreatorKit.Item;
using ClusterVR.CreatorKit.ProductUgc;
using ClusterVR.CreatorKit.Trigger;

namespace ClusterVR.CreatorKit.Editor.Preview.Trigger
{
    public sealed class OnProductPurchasedPlayerTriggerManager
    {
        readonly Dictionary<ProductId, HashSet<IOnProductPurchasedPlayerTrigger>> triggers = new();

        public OnProductPurchasedPlayerTriggerManager(
            ItemCreator itemCreator,
            ItemDestroyer itemDestroyer)
        {
            itemCreator.OnCreate += OnCreateItem;
            itemDestroyer.OnDestroy += OnDestroyItem;
        }

        void OnCreateItem(IItem item)
        {
            var productDisplayItem = item.gameObject.GetComponent<IProductDisplayItem>();
            if (productDisplayItem != null)
            {
                SubscribeDisplayItemInteract(productDisplayItem);
            }

            var triggers = item.gameObject.GetComponentsInChildren<IOnProductPurchasedPlayerTrigger>(true);
            AddTriggers(triggers);
        }

        void OnDestroyItem(IItem item)
        {
            var triggers = item.gameObject.GetComponentsInChildren<IOnProductPurchasedPlayerTrigger>(true);
            foreach (var trigger in triggers)
            {
                RemoveTrigger(trigger);
            }
        }

        public void SubscribeDisplayItemInteractInScene(IEnumerable<IProductDisplayItem> productDisplayItems)
        {
            foreach (var productDisplayItem in productDisplayItems)
            {
                SubscribeDisplayItemInteract(productDisplayItem);
            }
        }

        public void AddTriggers(IEnumerable<IOnProductPurchasedPlayerTrigger> triggers)
        {
            foreach (var trigger in triggers)
            {
                AddTrigger(trigger);
            }
        }

        void SubscribeDisplayItemInteract(IProductDisplayItem productDisplayItem)
        {
            productDisplayItem.OnInvoked += () => Invoke(productDisplayItem.ProductId);
        }

        void AddTrigger(IOnProductPurchasedPlayerTrigger trigger)
        {
            var key = trigger.ProductId;
            if (triggers.TryGetValue(key, out var triggerSet))
            {
                triggerSet.Add(trigger);
            }
            else
            {
                triggers.Add(key, new HashSet<IOnProductPurchasedPlayerTrigger> { trigger });
            }
        }

        void RemoveTrigger(IOnProductPurchasedPlayerTrigger trigger)
        {
            var key = trigger.ProductId;
            if (triggers.TryGetValue(key, out var triggerSet))
            {
                triggerSet.Remove(trigger);
                if (triggerSet.Count == 0)
                {
                    triggers.Remove(key);
                }
            }
        }

        void Invoke(ProductId productId)
        {
            if (!triggers.TryGetValue(productId, out var triggerSet))
            {
                return;
            }

            foreach (var trigger in triggerSet)
            {
                trigger.Invoke();
            }
        }
    }
}
