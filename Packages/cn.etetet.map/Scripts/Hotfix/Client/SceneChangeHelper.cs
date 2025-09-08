namespace ET.Client
{
    public static partial class SceneChangeHelper
    {
        // 场景切换协程
        public static async ETTask SceneChangeTo(Scene root, string sceneName, long sceneInstanceId)
        {
            EntityRef<Scene> rootRef = root;
            CurrentScenesComponent currentScenesComponent = root.GetComponent<CurrentScenesComponent>();
            EntityRef<CurrentScenesComponent> currentScenesComponentRef = currentScenesComponent;
            
            bool changeScene = TransferSceneHelper.IsChangeScene(currentScenesComponent.Scene?.Name, sceneName);
            
            if (changeScene)
            {
                // 先卸载当前场景
                currentScenesComponent.Scene?.Dispose(); // 删除之前的CurrentScene，创建新的
                Scene currentScene = CurrentSceneFactory.Create(sceneInstanceId, sceneName, currentScenesComponent);
                currentScene.AddComponent<UnitComponent>();
                await WaitUnitCreateFinish(root, currentScenesComponent.Scene);
            }

            root = rootRef;
            EventSystem.Instance.Publish(root, new SceneChangeStart() {ChangeScene = changeScene});          // 可以订阅这个事件中创建Loading界面

            if (changeScene)
            {
                // 加载场景寻路数据
                await NavmeshComponent.Instance.Load(sceneName);
            }
            
            root = rootRef;
            EventSystem.Instance.Publish(root, new SceneChangeFinish());
            
            using CoroutineLock coroutineLock = await root.GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.SceneChange, 0);
            // 通知等待场景切换的协程
            root = rootRef;
            root.GetComponent<ObjectWait>().Notify(new Wait_SceneChangeFinish());

            currentScenesComponent = currentScenesComponentRef;
            currentScenesComponent.Progress = 100;
        }

        private static async ETTask WaitUnitCreateFinish(Scene root, Scene currentScene)
        {
            EntityRef<Scene> currentSceneRef = currentScene;
            UnitComponent unitComponent = currentScene.GetComponent<UnitComponent>();
            EntityRef<UnitComponent> unitComponentRef = unitComponent;
            
            // 等待CreateMyUnit的消息
            Wait_CreateMyUnit waitCreateMyUnit = await root.GetComponent<ObjectWait>().Wait<Wait_CreateMyUnit>();
            M2C_CreateMyUnit m2CCreateMyUnit = waitCreateMyUnit.Message;
            currentScene = currentSceneRef;
            Unit unit = UnitFactory.Create(currentScene, m2CCreateMyUnit.Unit);
            unit.AddComponent<TargetComponent>();
            unit.AddComponent<SpellComponent>();
            unitComponent = unitComponentRef;
            unitComponent.Add(unit);
            
            EventSystem.Instance.Publish(currentScene, new AfterMyUnitCreate() {Unit = unit});
        }
    }
}