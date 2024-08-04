using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClusterVR.CreatorKit.Watermark
{
    public interface IWatermark
    {
        Texture2D Image { get; }
    }
}