using ClusterVR.CreatorKit.Constants;
using ClusterVR.CreatorKit.Extensions;
using UnityEngine;

namespace ClusterVR.CreatorKit.Exhibit.AvatarProduct.Implements
{
    [RequireComponent(typeof(Collider))]
    public sealed class AvatarProduct : MonoBehaviour, IAvatarProduct
    {
        [SerializeField] string id;

        public string Id => id;

        // レイヤー設定はResetでもやったほうが良いかも (enableをあんまり気にしなくて良くなる & Editorでもランタイムの見た目に近くなる)
        void Start()
        {
            gameObject.SetLayerRecursively(LayerName.InteractableExhibit);
        }
    }
}
