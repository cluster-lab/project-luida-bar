using UnityEngine;

namespace ClusterVR.CreatorKit.Media
{
    public interface IMediaPlayerRenderer
    {
        Renderer Renderer { get; }
        string TexturePropertyName { get; }
    }
}
