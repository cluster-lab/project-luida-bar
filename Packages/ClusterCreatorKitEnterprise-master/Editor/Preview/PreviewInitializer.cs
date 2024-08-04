using System.Collections.Generic;
using System.Linq;
using ClusterVR.CreatorKit.Constants;
using ClusterVR.CreatorKit.Editor.Preview.Analytics.AreaLog;
using ClusterVR.CreatorKit.Editor.Preview.Gimmick;
using ClusterVR.CreatorKit.Editor.Preview.Item;
using ClusterVR.CreatorKit.Editor.Preview.Trigger;
using ClusterVR.CreatorKit.Gimmick;
using ClusterVR.CreatorKit.Item;
using ClusterVR.CreatorKit.Media;
using ClusterVR.CreatorKit.Preview.Exhibit;
using ClusterVR.CreatorKit.Trigger;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClusterVR.CreatorKit.Editor.Preview
{
    [InitializeOnLoad]
    public static class PreviewInitializer
    {
        static OnProductPurchasedPlayerTriggerManager productPurchasedPlayerTriggerManager;
        static SendAnalyticsPlayerGimmickManager sendAnalyticsPlayerGimmickManager;

        static PreviewInitializer()
        {
#if !CLUSTER_CREATOR_KIT_DISABLE_PREVIEW
            Bootstrap.OnInitializedEvent += Initialize;
            EditorApplication.playModeStateChanged += playMode =>
            {
                OnChangePlayMode(playMode);
            };
#endif
        }

        static void OnChangePlayMode(PlayModeStateChange playMode)
        {
            if (playMode == PlayModeStateChange.ExitingPlayMode)
            {
                Bootstrap.OnInitializedEvent -= Initialize;
            }
        }

        static void Initialize()
        {
            var playerTransform = Bootstrap.PlayerPresenter.PlayerTransform;
            new LoggingAreaSubscriber(playerTransform).Run();
            playerTransform.gameObject.AddComponent<InteractableExhibitSelector>();

            SetupLayer();
            SetupTriggers();
            SetupGimmicks();
            SetupMediaPlayers();
            SetUpProductDisplayAssignedAutomatically();
        }

        static void SetupLayer()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
            var layersProperty = tagManager.FindProperty("layers");
            var serializedProperty = layersProperty.GetArrayElementAtIndex(LayerName.InteractableExhibit);
            if (serializedProperty != null && serializedProperty.stringValue != nameof(LayerName.InteractableExhibit))
            {
                serializedProperty.stringValue = nameof(LayerName.InteractableExhibit);
                tagManager.ApplyModifiedProperties();
            }

            if (Camera.main != null) Camera.main.cullingMask |= LayerName.InteractableExhibitMask;
        }

        static void SetupTriggers()
        {
            var rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();

            productPurchasedPlayerTriggerManager = new OnProductPurchasedPlayerTriggerManager(Bootstrap.ItemCreator, Bootstrap.ItemDestroyer);
            productPurchasedPlayerTriggerManager.AddTriggers(GetComponentsInGameObjectsChildren<IOnProductPurchasedPlayerTrigger>(rootGameObjects));
            productPurchasedPlayerTriggerManager.SubscribeDisplayItemInteractInScene(GetComponentsInGameObjectsChildren<IProductDisplayItem>(rootGameObjects));
        }

        static void SetupGimmicks()
        {
            var rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();

            sendAnalyticsPlayerGimmickManager = new SendAnalyticsPlayerGimmickManager(Bootstrap.ItemCreator);
            sendAnalyticsPlayerGimmickManager.Register(GetComponentsInGameObjectsChildren<ISendAnalyticsPlayerGimmick>(rootGameObjects));
        }

        static void SetupMediaPlayers()
        {
            var rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var mediaPlayer in GetComponentsInGameObjectsChildren<IMediaPlayer>(rootGameObjects))
            {
                mediaPlayer.Play();
            }
        }

        static void SetUpProductDisplayAssignedAutomatically()
        {
            var rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            new AutoAssignableProductDisplayItemManager(Bootstrap.ItemCreator, GetComponentsInGameObjectsChildren<IAutoAssignableProductDisplayItem>(rootGameObjects));
        }

        static IEnumerable<T> GetComponentsInGameObjectsChildren<T>(IEnumerable<GameObject> rootGameObjects)
        {
            return rootGameObjects.SelectMany(x =>
                x.GetComponentsInChildren<T>(true));
        }
    }
}
