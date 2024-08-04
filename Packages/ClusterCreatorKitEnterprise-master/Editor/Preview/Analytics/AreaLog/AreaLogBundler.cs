using UnityEngine;

namespace ClusterVR.CreatorKit.Editor.Preview.Analytics.AreaLog
{
    public sealed class AreaLogBundler
    {
        readonly string areaId;

        int enteringCount;

        public AreaLogBundler(string areaId)
        {
            this.areaId = areaId;
        }

        public void OnEnter()
        {
            if (enteringCount == 0)
            {
                Debug.Log($"On enter LoggingArea (AreaId: {areaId})");
            }

            ++enteringCount;
        }

        public void OnExit()
        {
            if (enteringCount <= 1)
            {
                Debug.Log($"On exit LoggingArea (AreaId: {areaId})");
            }

            if (enteringCount > 0) --enteringCount;
        }
    }
}
