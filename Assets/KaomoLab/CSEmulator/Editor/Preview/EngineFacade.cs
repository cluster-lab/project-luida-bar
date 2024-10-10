using Assets.KaomoLab.CSEmulator.Editor.EmulateClasses;
using Assets.KaomoLab.CSEmulator.Editor.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.KaomoLab.CSEmulator.Editor.Preview
{
    public class EngineFacade
    {
        readonly ItemCollector itemCollector;
        readonly VrmPreparer vrmPreparer;

        readonly PrefabItemStore prefabItemStore;
        readonly ItemMessageRouter itemMessageRouter;
        readonly TextInputRouter textInputRouter;
        readonly PlayerHandleFactoryBuilder playerHandleFactoryBuilder;
        readonly UserInterfacePreparer userInterfacePreparer;
        readonly ProductPurchaser productPurchaser;
        readonly FogSettingsBridge fogSettingsBridge;
        readonly PlayerStorageSerDes playerStorageSerDes;
        readonly OptionBridge optionBridge;

        List<CodeRunner> codeRunners = new List<CodeRunner>();
        PlayerCodeRunner playerCodeRunner = null;
        UnityEngine.GameObject vrm = null;

        bool isRunning = false;

        public EngineFacade(
            OptionBridge optionBridge,
            ClusterVR.CreatorKit.Editor.Preview.Item.ItemCreator itemCreator,
            ClusterVR.CreatorKit.Editor.Preview.Item.ItemDestroyer itemDestroyer,
            ClusterVR.CreatorKit.Editor.Preview.World.SpawnPointManager spawnPointManager
        )
        {
            this.optionBridge = optionBridge;
            var previewFinder = new CckPreviewFinder();
            this.itemCollector = new ItemCollector(
                itemCreator
            );
            this.vrmPreparer = new VrmPreparer(
                previewFinder,
                optionBridge.raw.vrm,
                optionBridge,
                optionBridge,
                optionBridge.playerMeasurementsHolder
            );

            prefabItemStore = new PrefabItemStore(
                itemCollector.GetAllItemPrefabs()
            );
            itemMessageRouter = new ItemMessageRouter();
            textInputRouter = new TextInputRouter();
            userInterfacePreparer = new UserInterfacePreparer(
                previewFinder
            );
            productPurchaser = new ProductPurchaser(
                optionBridge
            );
            fogSettingsBridge = new FogSettingsBridge();
            playerHandleFactoryBuilder = new PlayerHandleFactoryBuilder(
                userInterfacePreparer,
                textInputRouter,
                productPurchaser,
                optionBridge,
                itemMessageRouter,
                optionBridge,
                fogSettingsBridge,
                optionBridge,
                spawnPointManager
            );
            this.optionBridge = optionBridge;

            playerCodeRunner = new PlayerCodeRunner(
                userInterfacePreparer, itemMessageRouter, optionBridge,
                new DebugLogFactory(new UnityEngine.GameObject("CSEmulator PlayerScript"), optionBridge.raw)
            );

            itemCollector.OnScriptableItemCreated += i =>
            {
                codeRunners.Add(StartRunner(playerCodeRunner, i));
            };
            itemCollector.OnItemCreated += i =>
            {
                var csItemHandler = CSEmulator.Commons.AddComponent<Components.CSEmulatorItemHandler>(i.gameObject);
                csItemHandler.Construct(true);
                CSEmulator.Commons.AddComponent<Components.CSEmulatorStateWatcher>(i.gameObject);
            };
            itemDestroyer.OnDestroy += i =>
            {
                var destoryed = codeRunners
                    .FirstOrDefault(c => c.csItemHandler.item.Id.Value == i.Id.Value);
                if (destoryed == null) return;
                destoryed.Shutdown();
                codeRunners.Remove(destoryed);
            };
        }

        public void Start()
        {
            if (!optionBridge.raw.enable) return;

            foreach(var i in itemCollector.GetAllItems())
            {
                var csItemHandler = CSEmulator.Commons.AddComponent<Components.CSEmulatorItemHandler>(i.gameObject);
                csItemHandler.Construct(false);
                CSEmulator.Commons.AddComponent<Components.CSEmulatorStateWatcher>(i.gameObject);
            }

            //StartRunner前にVRMをInstantinateするのは合ってる。
            //しかし複数プレイヤーを考えるとこれは雑なのでそのうち何とかする。
            vrm = vrmPreparer.InstantiateVrm();
            playerHandleFactoryBuilder.AddPlayer(vrm, optionBridge);

            fogSettingsBridge.Start();

            //各種コンポーネントを付けてから実行した方がいい気がする。
            var newRunners = itemCollector
                .GetAllScriptableItem()
                .Select(i => StartRunner(playerCodeRunner, i));
            codeRunners.AddRange(newRunners);
            isRunning = true;
        }

        CodeRunner StartRunner(
            PlayerCodeRunner playerCodeRunner,
            ClusterVR.CreatorKit.Item.IScriptableItem scriptableItem
        )
        {
            var itemHandler = scriptableItem.Item.gameObject.GetComponent<Components.CSEmulatorItemHandler>();
            var stateWatcher = scriptableItem.Item.gameObject.GetComponent<Components.CSEmulatorStateWatcher>();
            //見やすさ重視で上に持っていく仕組みだったけど、ウィンドウあるしそこまでしなくていいかと
            //while (UnityEditorInternal.ComponentUtility.MoveComponentUp(stateWatcher)) { }
            var loggerFactory = new DebugLogFactory(itemHandler.gameObject, optionBridge.raw);
            var ret = new CodeRunner(
                scriptableItem,
                itemHandler,
                stateWatcher,
                prefabItemStore,
                itemCollector,
                itemMessageRouter,
                textInputRouter,
                productPurchaser,
                optionBridge,
                playerHandleFactoryBuilder,
                playerCodeRunner,
                optionBridge,
                optionBridge,
                loggerFactory
            );
            ret.Start();
            return ret;
        }

        public void Update()
        {
            //Update中にDestroyされて減ることがあるのでToArray
            foreach (var runner in codeRunners.ToArray())
            {
                runner.Update();
            }
            playerCodeRunner.Update();
            itemMessageRouter.Routing();
            textInputRouter.Routing();
            productPurchaser.Routing();
        }

        public void Shutdown()
        {
            if (!isRunning) return;

            foreach (var runner in codeRunners)
            {
                runner.Shutdown();
            }
            codeRunners.Clear();

            isRunning = false;
            vrm = null;
        }
    }
}
