#if UNITY_EDITOR
using ClusterVR.CreatorKit.Constants;
using ClusterVR.CreatorKit.Exhibit.AvatarProduct;
using ClusterVR.CreatorKit.Exhibit.ExternalUrl;
using ClusterVR.CreatorKit.Exhibit.Goods;
using ClusterVR.CreatorKit.Preview.PlayerController;
using UnityEngine;

namespace ClusterVR.CreatorKit.Preview.Exhibit
{
    public sealed class InteractableExhibitSelector : MonoBehaviour
    {
        const float RaycastDistance = 10f;
        Camera mainCamera;
        [SerializeField] DesktopPointerEventListener pointerEventListener;

        void Start()
        {
            // TODO: enterprise -> cck移行の際にSerializeFieldに設定するようにする
            if (transform.root.GetComponentInChildren<DesktopPointerEventListener>() is DesktopPointerEventListener listener)
            {
                pointerEventListener = listener;
                pointerEventListener.OnClicked += OnClicked;
            }
        }

        bool RaycastInteractatbles(Vector2 raycastPoint, out Collider hitCollider)
        {
            bool RayCast(Vector2 point, int layerMask, out RaycastHit hitInfo)
            {
                if (mainCamera == null)
                {
                    mainCamera = Camera.main;
                    if (mainCamera == null)
                    {
                        hitInfo = default;
                        return false;
                    }
                }

                var ray = mainCamera.ScreenPointToRay(point);
                return Physics.Raycast(ray, out hitInfo, RaycastDistance, layerMask);
            }

            hitCollider = default;
            // note: 本当はLayerUsagesと共通化したいが、ほとんどがアプリ内定義のレイヤーだったので諦め……
            if (!RayCast(raycastPoint, ~LayerName.PostProcessingMask, out var hit))
            {
                return false;
            }

            hitCollider = hit.collider;
            return true;
        }

        static bool TryGetAvatarProductAt(Collider hitCollider, out IAvatarProduct avatarProduct)
        {
            avatarProduct = hitCollider.GetComponentInParent<IAvatarProduct>();

            return avatarProduct != null;
        }

        static bool TryGetGoodsAt(Collider hitCollider, out IGoods goods)
        {
            goods = hitCollider.GetComponentInParent<IGoods>();

            return goods != null;
        }

        static bool TryGetExternalUrlAt(Collider hitCollider, out IExternalUrlLink externalUrlLink)
        {
            externalUrlLink = hitCollider.GetComponentInParent<IExternalUrlLink>();

            return externalUrlLink != null;
        }

        void OnClicked(Vector2 raycastPoint)
        {
            if (!RaycastInteractatbles(raycastPoint, out var hitCollider))
            {
                return;
            }

            if (TryGetAvatarProductAt(hitCollider, out var avatarProduct))
            {
                Debug.Log($"AvatarProduct: {avatarProduct.Id} が選択されました");
            }

            if (TryGetGoodsAt(hitCollider, out var goods))
            {
                Debug.Log($"GoodsId: {goods.Id} が選択されました");
            }

            if (TryGetExternalUrlAt(hitCollider, out var externalUrl))
            {
                Debug.Log($"Title:{externalUrl.Title}, Url: {externalUrl.Url} が開かれました。");
                Application.OpenURL(externalUrl.Url);
            }
        }

        void OnDestroy()
        {
            if (pointerEventListener == null) return;
            pointerEventListener.OnClicked -= OnClicked;
        }
    }
}
#endif
