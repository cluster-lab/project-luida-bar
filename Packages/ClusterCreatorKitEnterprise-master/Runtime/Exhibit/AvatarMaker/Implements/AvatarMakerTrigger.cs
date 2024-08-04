using ClusterVR.CreatorKit.Constants;
using ClusterVR.CreatorKit.Extensions;
using UnityEngine;

namespace ClusterVR.CreatorKit.Exhibit.AvatarMaker.Implements
{
    [RequireComponent(typeof(Collider))]
    public sealed class AvatarMakerTrigger : MonoBehaviour, IAvatarMakerTrigger
    {
        // レイヤー設定はResetでもやったほうが良いかも (enableをあんまり気にしなくて良くなる & Editorでもランタイムの見た目に近くなる)
        void Start()
        {
            gameObject.SetLayerRecursively(LayerName.InteractableExhibit);
        }

        void OnValidate()
        {
            foreach (var col in GetComponentsInChildren<Collider>(true))
            {
                col.isTrigger = true;
            }
        }
    }
}
