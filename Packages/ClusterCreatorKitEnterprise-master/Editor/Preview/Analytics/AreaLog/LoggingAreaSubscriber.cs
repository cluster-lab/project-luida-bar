using System.Collections.Generic;
using System.Linq;
using ClusterVR.CreatorKit.Analytics.AreaLog;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClusterVR.CreatorKit.Editor.Preview.Analytics.AreaLog
{
    public sealed class LoggingAreaSubscriber
    {
        readonly GameObject playerGameObject;
        readonly Dictionary<string, AreaLogBundler> areaLogBundlers = new Dictionary<string, AreaLogBundler>();

        public LoggingAreaSubscriber(Transform playerTransform)
        {
            playerGameObject = playerTransform.gameObject;
        }

        public void Run()
        {
            var rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var loggingArea in rootGameObjects.SelectMany(o => o.GetComponentsInChildren<ILoggingArea>(true)))
            {
                Subscribe(loggingArea);
            }
        }

        void Subscribe(ILoggingArea loggingArea)
        {
            loggingArea.OnEnter += OnEnter;
            loggingArea.OnExit += OnExit;
        }

        void OnEnter(LoggingAreaEventArgs e)
        {
            if (!IsPlayerCharacterColliderGameObject(e.OtherGameObject)) return;
            GetOrCreateBundler(e.AreaId).OnEnter();
        }

        void OnExit(LoggingAreaEventArgs e)
        {
            if (!IsPlayerCharacterColliderGameObject(e.OtherGameObject)) return;
            GetOrCreateBundler(e.AreaId).OnExit();
        }
        
        AreaLogBundler GetOrCreateBundler(string areaId)
        {
            if (!areaLogBundlers.TryGetValue(areaId, out var bundler))
            {
                bundler = new AreaLogBundler(areaId);
                areaLogBundlers[areaId] = bundler;
            }
            return bundler;
        }

        bool IsPlayerCharacterColliderGameObject(GameObject gameObject) => gameObject == playerGameObject;
    }
}
