using UnityEngine;

namespace ClusterVR.CreatorKit.Exhibit.AvatarDisplay.Implements
{
    public sealed class AvatarDisplayTemporaryObject : MonoBehaviour, IAvatarDisplayTemporaryObject
    {
        public void SetActive(bool isActive)
        {
            // interface経由でのコールを考慮し生存確認
            if (this == null) { return; }

            this.gameObject.SetActive(isActive);
        }
    }
}
