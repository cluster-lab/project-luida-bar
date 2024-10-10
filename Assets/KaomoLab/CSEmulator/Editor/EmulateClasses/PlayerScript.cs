using ClusterVR.CreatorKit.World.Implements.WorldRuntimeSetting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.KaomoLab.CSEmulator.Editor.EmulateClasses
{
    public class PlayerScript
    {
        public class ClusterWorldRuntimeSettings
        {
            public readonly bool useClusterHudV2;

            public ClusterWorldRuntimeSettings(WorldRuntimeSetting worldRuntimeSetting)
            {
                var hudType = worldRuntimeSetting?.UseHUDType ?? WorldRuntimeSetting.DefaultValues.HUDType;
                useClusterHudV2 = hudType == ClusterVR.CreatorKit.Proto.WorldRuntimeSetting.Types.HUDType.ClusterHudV2;
            }
        }

        readonly ClusterWorldRuntimeSettings clusterWorldRuntimeSettings;
        readonly IMessageSender messageSender;
        readonly IPlayerReceiveListenerBinder playerReceiveListenerBinder;
        readonly IPlayerSendableSanitizer playerSendableSanitizer;
        readonly IPlayerStorager playerStorager;
        readonly IItemExceptionFactory itemExceptionFactory;
        readonly IRayDrawer rayDrawer;
        readonly ILogger logger;
        readonly PlayerHandle playerHandle;
        readonly ClusterScript clusterScript;
        public GameObject gameObject => clusterScript.gameObject;

        public PlayerScript(
            ClusterWorldRuntimeSettings clusterWorldRuntimeSettings,
            IMessageSender messageSender,
            IPlayerReceiveListenerBinder playerReceiveListenerBinder,
            IPlayerSendableSanitizer playerSendableSanitizer,
            IPlayerStorager playerStorager,
            IItemExceptionFactory itemExceptionFactory,
            IRayDrawer rayDrawer,
            ILogger logger,
            PlayerHandle playerHandle,
            ClusterScript clusterScript,
            string code
        )
        {
            this.clusterWorldRuntimeSettings = clusterWorldRuntimeSettings;
            this.messageSender = messageSender;
            this.playerReceiveListenerBinder = playerReceiveListenerBinder;
            this.playerSendableSanitizer = playerSendableSanitizer;
            this.playerStorager = playerStorager;
            this.itemExceptionFactory = itemExceptionFactory;
            this.rayDrawer = rayDrawer;
            this.logger = logger;
            this.playerHandle = playerHandle;
            this.clusterScript = clusterScript;
            this.cameraHandle = new CameraHandle(playerHandle.playerController);
            this.sourceItemId = new ItemId(clusterScript.csItemHandler);
            this._code = code;
        }

        public CameraHandle cameraHandle { get; private set; }
        public bool isAndroid => playerHandle.playerMeta.isAndroid;
        public bool isDesktop => playerHandle.playerMeta.isDesktop;
        public bool isIos => playerHandle.playerMeta.isIos;
        public bool isMacOs => playerHandle.playerMeta.isMacOs;
        public bool isMobile => playerHandle.playerMeta.isMobile;
        public bool isVr => playerHandle.playerMeta.isVr;
        public bool isWindows => playerHandle.playerMeta.isWindows;
        public ItemId sourceItemId { get; private set; }

        public string _code { get; private set; }

        IStartListenerBinder startListenerBinder;
        IUpdateListenerBinder updateListenerBinder;
        IButtonInterfaceHandler buttonInterfaceHandler;

        readonly BurstableThrottle sendToThrottle = new BurstableThrottle(0.09d, 5);

        public int computeSendableSize(object arg)
        {
            //CSETODO 一旦非対応で
            return 0;
        }

        public int getAvatarMovementFlags()
        {
            return playerHandle.playerController.movementFlags;
        }

        public EmulateVector3 getHumanoidBonePosition(HumanoidBone bone)
        {
            return playerHandle.getHumanoidBonePosition(bone);
        }

        public EmulateQuaternion getHumanoidBoneRotation(HumanoidBone bone)
        {
            return playerHandle.getHumanoidBoneRotation(bone);
        }

        public object getPlayerStorageData()
        {
            return playerStorager.Load();
        }

        public EmulateVector3 getPosition()
        {
            return playerHandle.getPosition();
        }

        public EmulateQuaternion getRotation()
        {
            return playerHandle.getRotation();
        }

        public void hideButton(int index)
        {
            if (!clusterWorldRuntimeSettings.useClusterHudV2) return;

            buttonInterfaceHandler.HideButton(index);
            if (index == 0) clusterScript.cckComponentFacade.ResumeUseItemTrigger();
        }

        public HumanoidAnimation humanoidAnimation(string humanoidAnimationId)
        {
            return clusterScript.humanoidAnimation(humanoidAnimationId);
        }

        public IconAsset iconAsset(string iconId)
        {
            var list = gameObject.GetComponent<ClusterVR.CreatorKit.Item.IIconAssetList>();
            if (list == null)
                return new IconAsset();

            var icon = list.IconAssets.FirstOrDefault(i => i.Id == iconId);
            if (icon == null)
                return new IconAsset();

            var ret = new IconAsset(icon.IconAsset.GetTexture());
            return ret;
        }

        public void log(object v)
        {
            clusterScript.log(v);
        }

        public void onButton(int index, Action<bool> Callback)
        {
            if (!clusterWorldRuntimeSettings.useClusterHudV2) return;

            if (index < 0 || index > 3)
                throw clusterScript.itemExceptionFactory.CreateGeneral(String.Format("indexの範囲外です:{0}", index));
            buttonInterfaceHandler.SetButtonCallback(index, Callback);
        }

        public void onFrame(Action<double> Callback)
        {
            updateListenerBinder.SetUpdateCallback(
                clusterScript.gameObject.name, clusterScript.gameObject, Callback
            );
        }

        public void onReceive(Action<string, object, object> Callback)
        {
            dynamic option = new System.Dynamic.ExpandoObject();
            option.item = true;
            option.player = true;
            onReceive(Callback, option);
        }
        public void onReceive(Action<string, object, object> Callback, object option)
        {
            //ExpandoObjectで来る
            var opt = (IDictionary<string, object>)option;
            var receiveItem = opt.ContainsKey("item") ? (bool)opt["item"] : true;
            var receivePlayer = opt.ContainsKey("player") ? (bool)opt["player"] : true;

            var CheckedCallback = new Action<string, object, object>((id, arg, sender) =>
            {
                if (sender is ItemId && receiveItem)
                    Callback(id, arg, sender);
                if (sender is PlayerId && receivePlayer)
                    Callback(id, arg, sender);
            });

            playerReceiveListenerBinder.SetPlayerReceiveCallback(
                playerHandle.id,
                playerSendableSanitizer,
                CheckedCallback
            );
        }

        public void onStart(Action Callback)
        {
            startListenerBinder.SetUpdateCallback(Callback);
        }

        public PlayerLocalObject playerLocalObject(string id)
        {
            var list = gameObject.GetComponent<ClusterVR.CreatorKit.Item.IPlayerLocalObjectReferenceList>();
            if (list == null)
                return null;

            var entry = list.PlayerLocalObjectReferences.FirstOrDefault(i => i.Id == id);
            if (entry == null)
                return null;

            var go = entry.GameObject;
            if(go.GetComponentInParent<ClusterVR.CreatorKit.World.IPlayerLocalUI>() == null)
            {
                //アップロード時にエラーになるので例外にしておく
                itemExceptionFactory.CreateGeneral(String.Format("PlayerLocalUIの子ではありません。{0}:{1}", id, go.name));
            }
            if (go.GetComponent<ClusterVR.CreatorKit.Item.IItem>() != null)
            {
                UnityEngine.Debug.LogWarning(String.Format("Itemが付いています。{0}:{1}", id, go.name));
                return null;
            }
            if (go.GetComponentInParent<ClusterVR.CreatorKit.Item.IItem>() != null)
            {
                UnityEngine.Debug.LogWarning(String.Format("Itemの子です。{0}:{1}", id, go.name));
                return null;
            }
            if (go.GetComponentInChildren<ClusterVR.CreatorKit.Item.IItem>() != null)
            {
                itemExceptionFactory.CreateGeneral(String.Format("子にItemがあります。{0}:{1}", id, go.name));
            }

            var ret = new PlayerLocalObject(entry.GameObject, itemExceptionFactory);
            return ret;
        }

        public PlayerScriptRaycastResult raycast(
            EmulateVector3 position, EmulateVector3 direction, float maxDistance
        )
        {
            var ret = raycastAllConsiderShape(
                position, direction, maxDistance
            );
            if (ret.Length == 0) return null;
            return ret[0];
        }

        public PlayerScriptRaycastResult[] raycastAll(
            EmulateVector3 position, EmulateVector3 direction, float maxDistance
        )
        {
            var ret = raycastAllConsiderShape(
                position, direction, maxDistance
            );
            return ret;
        }

        PlayerScriptRaycastResult[] raycastAllConsiderShape(
            EmulateVector3 origin, EmulateVector3 direction, float maxDistance
        )
        {
            var raycastHits = Physics.RaycastAll(
                origin._ToUnityEngine(),
                direction._ToUnityEngine(),
                maxDistance,
                -1,
                QueryTriggerInteraction.Collide
            );

            var ret = raycastHits
                .OrderBy(raycastHit =>
                {
                    var distance = (raycastHit.point - origin._ToUnityEngine()).magnitude;
                    return distance;
                })
                .Where(raycastHit =>
                {
                    var target = raycastHit.transform.gameObject;
                    if (target.TryGetComponent<ClusterVR.CreatorKit.Item.IPhysicalShape>(out _))
                    {
                        return true;
                    }
                    if (target.TryGetComponent<ClusterVR.CreatorKit.Item.IOverlapSourceShape>(out _))
                    {
                        return true;
                    }
                    if (raycastHit.collider.isTrigger)
                    {
                        //Shape無しのisTriggerはNG
                        return false;
                    }
                    return true;
                })
                .Select(raycastHit =>
                {
                    var hit = new Hit(
                        new EmulateVector3(raycastHit.normal),
                        new EmulateVector3(raycastHit.point)
                    );
                    var raycastResult = new PlayerScriptRaycastResult(hit);
                    return raycastResult;
                }).ToArray();

            {
                var o = origin._ToUnityEngine();
                var d = direction._ToUnityEngine().normalized * maxDistance;
                rayDrawer.DrawRay(o, o + d, ret.Length == 0 ? Color.green : Color.magenta);
            }

            return ret;
        }


        public void sendTo(object id, string messageType, object arg)
        {
            CheckSendToOperationLimit();

            if (id is PlayerId playerId)
            {
                messageSender.SendToPlayer(playerId, messageType, arg, playerHandle, null);
            }
            if (id is ItemId itemId)
            {
                messageSender.SendToItem(itemId.id, messageType, arg, playerHandle, null);
            }
        }

        public void setHumanoidBoneRotationOnFrame(HumanoidBone bone, EmulateQuaternion rotation)
        {
            playerHandle.playerController.OverwriteHumanoidBoneRotation((HumanBodyBones)bone, rotation._ToUnityEngine());
        }

        public void setHumanoidPoseOnFrame(HumanoidPose pose)
        {
            setHumanoidPoseOnFrame(pose, 1f);
        }
        public void setHumanoidPoseOnFrame(HumanoidPose pose, float weight)
        {
            var position = pose?.centerPosition?._ToUnityEngine();
            var rotation = pose?.centerRotation?._ToUnityEngine();
            var muscles = pose?.muscles.muscles;
            var hasMascles = pose?.muscles.changed;
            playerHandle.playerController.MergeHumanPoseOnFrame(
                position, rotation, muscles, hasMascles, weight
            );
        }

        public void setPlayerStorageData(Jint.Native.JsValue data)
        {
            playerStorager.Save(data);
        }

        public void setPosition(EmulateVector3 position)
        {
            //ドキュメント通り、実行回数制限はない
            playerHandle._setPosition(position, false);
        }

        public void setRotation(EmulateQuaternion rotation)
        {
            playerHandle._setRotation(rotation, false);
        }

        public void showButton(int index, IconAsset icon)
        {
            if (!clusterWorldRuntimeSettings.useClusterHudV2) return;
            if (isVr) return;
            buttonInterfaceHandler.ShowButton(index, icon);
            if (index == 0) clusterScript.cckComponentFacade.InvalidUseItemTrigger();
        }

        public ItemId worldItemReference(string worldItemReferenceId)
        {
            var itemList = gameObject.GetComponent<ClusterVR.CreatorKit.Item.IWorldItemReferenceList>();
            if (itemList == null)
            {
                logger.Warning("WorldItemReferenceListが指定されていません。");
                return new ItemId();
            }
            var set = itemList.WorldItemReferences.FirstOrDefault(set => set.Id == worldItemReferenceId);
            if (set == null || set.Item == null)
            {
                logger.Warning(String.Format("{1}:{0}が無効です。", worldItemReferenceId, nameof(worldItemReferenceId)));
                return new ItemId();
            }

            var h = set.Item.gameObject.GetComponent<Components.CSEmulatorItemHandler>();
            var ret = new ItemId(h);
            return ret;
        }

        void CheckSendToOperationLimit()
        {
            var result = sendToThrottle.TryCharge();
            if (result) return;

            throw itemExceptionFactory.CreateRateLimitExceeded(
                String.Format("[{0}]", gameObject.name)
            );
        }
        void DischargeOperateLimit(double time)
        {
            sendToThrottle.Discharge(time);
        }

        public void _InjectEventHandler(
            IStartListenerBinder startListenerBinder,
            IUpdateListenerBinder updateListenerBinder,
            IButtonInterfaceHandler buttonInterfaceHandler
        )
        {
            this.startListenerBinder = startListenerBinder;
            this.updateListenerBinder = updateListenerBinder;
            this.buttonInterfaceHandler = buttonInterfaceHandler;

            this.updateListenerBinder.SetLateUpdateCallback(
                clusterScript.gameObject.name + "_throttle",
                clusterScript.gameObject,
                (dt) =>
                {
                    DischargeOperateLimit(dt);
                }
            );
        }

        public void _Shutdown()
        {
            startListenerBinder.DeleteStartCallback();
            updateListenerBinder.DeleteUpdateCallback(clusterScript.gameObject.name);
            updateListenerBinder.DeleteLateUpdateCallback(clusterScript.gameObject.name + "_throttle");
            buttonInterfaceHandler.HideAllButtons();
            buttonInterfaceHandler.DeleteAllButtonCallbacks();
            playerReceiveListenerBinder.DeletePlayerReceiveCallback(playerHandle.id);
            cameraHandle._Shutdown();
            clusterScript.cckComponentFacade.ResumeUseItemTrigger();
        }

        public object toJSON(string key)
        {
            dynamic o = new System.Dynamic.ExpandoObject();
            o.cameraHandle = cameraHandle;
            o.isVr = isVr;
            o.sourceItemId = sourceItemId;
            return o;
        }
        public override string ToString()
        {
            return string.Format("[PlayerScript]");
        }
    }
}
