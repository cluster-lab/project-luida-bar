using System.Collections.Generic;
using ClusterVR.CreatorKit.Constants;
using ClusterVR.CreatorKit.Extensions;
using ClusterVR.CreatorKit.Item;
using ClusterVR.CreatorKit.ProductUgc;
using UnityEngine;

namespace ClusterVR.CreatorKit.Editor.Preview.Item
{
    public sealed class AutoAssignableProductDisplayItemManager
    {
        public AutoAssignableProductDisplayItemManager(ItemCreator itemCreator,
            IEnumerable<IAutoAssignableProductDisplayItem> itemList)
        {
            itemCreator.OnCreate += OnCreate;
            Register(itemList);
        }

        void Register(IEnumerable<IAutoAssignableProductDisplayItem> itemList)
        {
            foreach (var productDisplayItem in itemList)
            {
                Register(productDisplayItem);
            }
        }

        void Register(IAutoAssignableProductDisplayItem item)
        {
            if (item.NeedsProductSample)
            {
                item.SetSample(CreateProductSample(item));
            }
            item.SetOnInvokedCallback(() => OnInvoked(item));
            item.SetInteractable(true);
        }

        void OnInvoked(IAutoAssignableProductDisplayItem item)
        {
            Debug.Log($"{item.DisplayContent}の{item.Order}に相当する商品の詳細ページが開かれます。");
        }

        void OnCreate(IItem item)
        {
            var productDisplayItem = item.gameObject.GetComponent<IAutoAssignableProductDisplayItem>();
            if (productDisplayItem != null)
            {
                Register(productDisplayItem);
            }
        }

        static GameObject CreateProductSample(IAutoAssignableProductDisplayItem item)
        {
            var rootObject = new GameObject($"ProductSample ({item.DisplayContent}) {item.Order}");
            var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.transform.SetParent(rootObject.transform);
            model.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            rootObject.SetLayerRecursively(LayerName.InteractableItem);
            return rootObject;
        }
    }
}
