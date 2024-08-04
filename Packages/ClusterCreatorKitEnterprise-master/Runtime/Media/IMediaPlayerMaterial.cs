using UnityEngine;

namespace ClusterVR.CreatorKit.Media
{
    public interface IMediaPlayerMaterial
    {
        Material Material { get; }
        string TexturePropertyName { get; }
    }
}
