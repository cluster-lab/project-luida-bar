using System.Collections;
using System.Collections.Generic;
using ClusterVR.CreatorKit.Watermark;
using UnityEngine;

namespace ClusterVR.CreatorKit.WaterMark.Implements
{
    public sealed class Watermark : MonoBehaviour, IWatermark
    {
        [SerializeField] Texture2D image;
        Texture2D IWatermark.Image => image;
    }
}